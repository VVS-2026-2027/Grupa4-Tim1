using System.Security.Claims;
using System.Text.Json;
using Earn_Learn.Controllers;
using Earn_Learn.Data;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;


namespace Earn_Learn.Tests
{
    [TestClass]
    public class StudentControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<UserManager<Korisnik>> _mockUserManager = null!;
        private StudentController _controller = null!;
        private Korisnik _student = null!;
        private Korisnik _tutor = null!;

        [TestInitialize]
        public void Setup()
        {
            _context = TestHelper.CreateInMemoryContext();
            _mockUserManager = TestHelper.CreateMockUserManager();

            _student = new Korisnik
            {
                Id = "student-1",
                Ime = "Amar",
                Prezime = "Amarić",
                Email = "student@test.com",
                Uloga = Uloga.Student,
                StanjeRacuna = 100.0
            };

            _tutor = new Korisnik
            {
                Id = "tutor-1",
                Ime = "Selma",
                Prezime = "Selmić",
                Email = "tutor@test.com",
                Uloga = Uloga.Tutor,
                CijenaPoSatu = 25.0,
                ProsjecnaOcjena = 4.8,
                BrojOdrzanihCasova = 10,
                StanjeRacuna = 50.0,
                VerifikovanTutor = true
            };

            _context.Users.AddRange(_student, _tutor);
            _context.SaveChanges();

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_student);
            _mockUserManager.Setup(m => m.FindByIdAsync("student-1"))
                .ReturnsAsync(_student);
            _mockUserManager.Setup(m => m.FindByIdAsync("tutor-1"))
                .ReturnsAsync(_tutor);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<Korisnik>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.DeleteAsync(It.IsAny<Korisnik>()))
                .ReturnsAsync(IdentityResult.Success);

            _controller = new StudentController(_mockUserManager.Object, _context);
            TestHelper.SetupControllerContext(_controller, _student);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
        }

        private class UplataTestPodatak
        {
            public double Iznos { get; set; }
            public bool OcekivaniUspjeh { get; set; }
        }

        public static IEnumerable<object[]> UcitajUplateIzJson()
        {
            var putanja = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "uplate.json");
            var json = File.ReadAllText(putanja);
            var lista = JsonSerializer.Deserialize<List<UplataTestPodatak>>(json)!;
            foreach (var item in lista)
            {
                yield return new object[] { item.Iznos, item.OcekivaniUspjeh };
            }
        }

        [TestMethod]
        public async Task Dashboard_ValidanStudent_VracaViewSaPodacima()
        {
            _context.Termin.Add(new Termin
            {
                id = 1,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                idPredmeta = 1,
                datumIVrijeme = DateTime.Now.AddDays(2),
                status = StatusTermina.Rezervisan
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Dashboard() as ViewResult;
            var model = result?.Model as StudentDashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual("Amar", model.Ime);
            Assert.AreEqual(1, model.NadolazaciTermini.Count);
        }

        [TestMethod]
        public async Task Dashboard_NijeStudent_RedirectaNaHome()
        {
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((Korisnik?)null);

            var result = await _controller.Dashboard() as RedirectToActionResult;
            Assert.AreEqual("Home", result?.ControllerName);
        }

        [DataTestMethod]
        [DynamicData(nameof(UcitajUplateIzJson), DynamicDataSourceType.Method)]
        public async Task UplatiNovac_DataDrivenJson_ProvjeravaValidneINevalidneIznose(double iznos, bool ocekivaniUspjeh)
        {
            double pocetnoStanje = _student.StanjeRacuna;
            var result = await _controller.UplatiNovac(iznos) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Novcanik", result.ActionName);

            if (ocekivaniUspjeh)
            {
                Assert.AreEqual(pocetnoStanje + iznos, _student.StanjeRacuna);
                Assert.AreEqual(1, await _context.Transakcija.CountAsync());
            }
            else
            {
                Assert.AreEqual(pocetnoStanje, _student.StanjeRacuna);
                Assert.IsNotNull(_controller.TempData["Greska"]);
            }
        }

        [TestMethod]
        public async Task UplatiNovac_AjaxZahtjev_VracaJson()
        {
            TestHelper.SetupControllerContext(_controller, _student, isAjax: true);
            var result = await _controller.UplatiNovac(50) as JsonResult;
            Assert.IsNotNull(result);

            var invalidResult = await _controller.UplatiNovac(-10) as JsonResult;
            Assert.IsNotNull(invalidResult);
        }

        [TestMethod]
        public async Task PotvrdiPrisustvo_Validno_ObracunavaProvizijuIPlacanje()
        {
            var admin = new Korisnik { Id = "admin-1", Uloga = Uloga.SystemManager, StanjeRacuna = 0 };
            _context.Users.Add(admin);

            var termin = new Termin
            {
                id = 10,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                cijena = 30.0,
                status = StatusTermina.Rezervisan,
                datumIVrijeme = DateTime.Now
            };
            _context.Termin.Add(termin);
            await _context.SaveChangesAsync();

            var result = await _controller.PotvrdiPrisustvo(10) as JsonResult;

            Assert.IsNotNull(result);
            Assert.AreEqual(70.0, _student.StanjeRacuna);
            Assert.AreEqual(77.0, _tutor.StanjeRacuna); // 50 + (30 - 3)
            Assert.AreEqual(3.0, admin.StanjeRacuna);   // 10% provizija
            Assert.AreEqual(StatusTermina.Odrzan, termin.status);
            Assert.AreEqual(3, await _context.Transakcija.CountAsync());
        }

        [TestMethod]
        public async Task PotvrdiPrisustvo_NedovoljnoSredstavaINepostojeciTermin_VracaGresku()
        {
            Assert.IsInstanceOfType(await _controller.PotvrdiPrisustvo(999), typeof(JsonResult));

            _student.StanjeRacuna = 5.0;
            _context.Termin.Add(new Termin
            {
                id = 11,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                cijena = 50.0,
                status = StatusTermina.Rezervisan
            });
            await _context.SaveChangesAsync();

            var res = await _controller.PotvrdiPrisustvo(11) as JsonResult;
            Assert.IsNotNull(res);
            Assert.AreEqual(5.0, _student.StanjeRacuna);
        }

        [TestMethod]
        public async Task OtkaziTermin_RezervisanTermin_VracaNovacIStatusSlobodan()
        {
            var termin = new Termin
            {
                id = 20,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                cijena = 25.0,
                status = StatusTermina.Rezervisan,
                datumIVrijeme = DateTime.Now.AddDays(1)
            };
            _context.Termin.Add(termin);
            await _context.SaveChangesAsync();

            var result = await _controller.OtkaziTermin(20) as RedirectToActionResult;

            Assert.AreEqual("MojiCasovi", result?.ActionName);
            Assert.AreEqual(125.0, _student.StanjeRacuna);
            Assert.AreEqual(25.0, _tutor.StanjeRacuna);
            Assert.AreEqual(StatusTermina.Slobodan, termin.status);
        }

        [TestMethod]
        public async Task RezervisiTermin_GetIPost_UspjesnoINedovoljnoSredstava()
        {
            var termin = new Termin
            {
                id = 30,
                idTutora = _tutor.Id,
                idPredmeta = 1,
                status = StatusTermina.Slobodan,
                cijena = 25.0,
                datumIVrijeme = DateTime.Now.AddDays(3)
            };
            _context.Termin.Add(termin);
            await _context.SaveChangesAsync();

            var getRes = await _controller.RezervisiTermin(30) as ViewResult;
            Assert.IsNotNull(getRes);

            var postRes = await _controller.RezervisiTermin(30, TipInstrukcija.Online) as RedirectToActionResult;
            Assert.AreEqual("Dashboard", postRes?.ActionName);
            Assert.AreEqual(StatusTermina.Rezervisan, termin.status);
            Assert.AreEqual(_student.Id, termin.idStudenta);
        }

        [TestMethod]
        public async Task RezervisiTermin_Post_NedovoljnoSredstava_RedirectaNaProfilTutora()
        {
            _student.StanjeRacuna = 5.0;
            _context.Termin.Add(new Termin
            {
                id = 31,
                idTutora = _tutor.Id,
                status = StatusTermina.Slobodan,
                cijena = 25.0
            });
            await _context.SaveChangesAsync();

            var result = await _controller.RezervisiTermin(31, TipInstrukcija.Uzivo) as RedirectToActionResult;
            Assert.AreEqual("Profil", result?.ActionName);
            Assert.IsNotNull(_controller.TempData["GreskaNovcenik"]);
        }

        [TestMethod]
        public async Task PretraziTutore_ValidanIPrazanQuery_VracaJson()
        {
            var prazan = await _controller.PretraziTutore("") as JsonResult;
            Assert.IsNotNull(prazan);

            _context.Users.Add(new Korisnik
            {
                Id = "tutor-2",
                Ime = "Selma",
                Prezime = "Druga",
                Uloga = Uloga.Tutor,
                CijenaPoSatu = 20,
                ProsjecnaOcjena = 5.0,
                BrojOdrzanihCasova = 20
            });
            await _context.SaveChangesAsync();

            var rezultat = await _controller.PretraziTutore("Selma", 10, 30, 4.0) as JsonResult;
            Assert.IsNotNull(rezultat);
        }

   [TestMethod]
        public async Task OstaviRecenziju_OdrzanCas_DodajeRecenzijuIAzuriraProsjek()
        {
            _context.Termin.Add(new Termin
            {
                id = 40,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                status = StatusTermina.Odrzan
            });
            _context.Recenzija.Add(new Recenzija
            {
                id = 10,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                ocjena = 5,
                komentar = "Prethodni čas"
            });
            await _context.SaveChangesAsync();

            var result = await _controller.OstaviRecenziju(_tutor.Id, 5, "Odličan tutor!") as RedirectToActionResult;

            Assert.AreEqual("Profil", result?.ActionName);
            Assert.AreEqual(2, await _context.Recenzija.CountAsync());
            Assert.AreEqual(5.0, _tutor.ProsjecnaOcjena);
        }

        [TestMethod]
        public async Task OstaviRecenziju_BezOdrzanogCasa_PostavljaErrorPoruku()
        {
            var result = await _controller.OstaviRecenziju(_tutor.Id, 5, "Test") as RedirectToActionResult;
            Assert.IsNotNull(_controller.TempData["ErrorPoruka"]);
        }

        [TestMethod]
        public async Task OstaleAkcije_MojiCasoviNovcanikOdaberiTerminProfilObavjestenjaPrijave()
        {
            _context.Obavjestenje.Add(new Obavjestenje { id = 1, idKorisnika = _student.Id, naslov = "Test", procitano = false });
            _context.Termin.Add(new Termin { id = 50, idTutora = _tutor.Id, idPredmeta = 1, status = StatusTermina.Slobodan, datumIVrijeme = DateTime.Now.AddDays(2) });
            await _context.SaveChangesAsync();

            Assert.IsInstanceOfType(await _controller.MojiCasovi(), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.Novcanik(), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.OdaberiTermin(_tutor.Id), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.Profil(_student.Id), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.PrijaviTutora(_tutor.Id, "Naslov", "Opis"), typeof(RedirectToActionResult));
            Assert.IsInstanceOfType(await _controller.DohvatiObavjestenja(), typeof(JsonResult));
            Assert.IsInstanceOfType(await _controller.BrojObavjestenja(), typeof(JsonResult));
            Assert.IsInstanceOfType(await _controller.OznaciProcitanim(1), typeof(OkResult));
            Assert.IsInstanceOfType(await _controller.OznaciSveProcitanim(), typeof(OkResult));
            Assert.IsInstanceOfType(await _controller.Obavjestenja(), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.Obrisi(_student.Id), typeof(RedirectToActionResult));
        }

        [TestMethod]
        public async Task PrijaviRecenziju_ValidanTutorINeautorizovan_VracaOdgovarajuciRezultat()
        {
            // 1. Neautorizovan (trenutno je ulogovan student, a akcija traži tutora)
            var unauthResult = await _controller.PrijaviRecenziju(1, _tutor.Id, "Lažna recenzija", "Opis prijave");
            Assert.IsInstanceOfType(unauthResult, typeof(UnauthorizedResult));

            // 2. Validan tutor prijavljuje recenziju
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_tutor);
            var redirectResult = await _controller.PrijaviRecenziju(1, _tutor.Id, "Neprimjeren komentar", "Vrijeđanje") as RedirectToActionResult;

            Assert.IsNotNull(redirectResult);
            Assert.AreEqual("Recenzije", redirectResult.ActionName);
            Assert.AreEqual(1, await _context.Prijava.CountAsync());
        }

        [TestMethod]
        public async Task PokrivenostGrana_PredmetiTerminiRecenzijeINeautorizovaniPristupi()
        {
            // Kreiranje i čitanje Predmeta (pokriva Predmet.cs i grane u Dashboard / MojiCasovi / OdaberiTermin)
            var predmet = new Predmet { id = 910, naziv = "Matematika 1" };
            _context.Predmet.Add(predmet);

            _context.Termin.Add(new Termin
            {
                id = 950,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                idPredmeta = predmet.id,
                status = StatusTermina.Rezervisan,
                datumIVrijeme = DateTime.UtcNow.AddDays(2)
            });

            _context.Recenzija.Add(new Recenzija
            {
                id = 920,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                ocjena = 5,
                komentar = "Super",
                datumRecenzije = DateTime.UtcNow
            });

            _context.Transakcija.Add(new Transakcija
            {
                id = 930,
                idStudenta = _student.Id,
                idTutora = _tutor.Id,
                iznos = 40,
                statusPlacanja = StatusPlacanja.Uspjesno
            });
            await _context.SaveChangesAsync();

            // Provjera modela Predmet
            Assert.AreEqual(910, predmet.id);
            Assert.AreEqual("Matematika 1", predmet.naziv);

            // Izvršavanje metoda sa popunjenim Predmetom, Terminom, Recenzijom i Transakcijom
            var dash = await _controller.Dashboard();
            Assert.IsInstanceOfType(dash, typeof(ViewResult));

            var mojiCasovi = await _controller.MojiCasovi();
            Assert.IsInstanceOfType(mojiCasovi, typeof(ViewResult));

            var novcanik = await _controller.Novcanik();
            Assert.IsInstanceOfType(novcanik, typeof(ViewResult));

            var profil = await _controller.Profil(_student.Id);
            Assert.IsInstanceOfType(profil, typeof(ViewResult));

            var nedostupanTermin = await _controller.RezervisiTermin(9999);
            Assert.IsInstanceOfType(nedostupanTermin, typeof(NotFoundResult));
            
            var tudjiTerminOtkazi = await _controller.OtkaziTermin(9999);
            Assert.IsInstanceOfType(tudjiTerminOtkazi, typeof(NotFoundResult));

            // Neautorizovani pristupi (korisnik nije ulogovan -> null)
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((Korisnik?)null);

            Assert.IsInstanceOfType(await _controller.MojiCasovi(), typeof(RedirectToActionResult));
            Assert.IsInstanceOfType(await _controller.Novcanik(), typeof(RedirectToActionResult));
            Assert.IsInstanceOfType(await _controller.Profil(_student.Id), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.OtkaziTermin(950), typeof(UnauthorizedResult));
            Assert.IsInstanceOfType(await _controller.OdaberiTermin(_tutor.Id), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.RezervisiTermin(950), typeof(ViewResult));
        }
    }
}