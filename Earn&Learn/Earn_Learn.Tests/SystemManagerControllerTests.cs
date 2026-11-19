using System.Security.Claims;
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
    public class SystemManagerControllerTests
    {
        private ApplicationDbContext _context = null!;
        private Mock<UserManager<Korisnik>> _mockUserManager = null!;
        private SystemManagerController _controller = null!;
        private Korisnik _admin = null!;
        private Korisnik _tutor = null!;

        [TestInitialize]
        public void Setup()
        {
            _context = TestHelper.CreateInMemoryContext();
            _mockUserManager = TestHelper.CreateMockUserManager();

            _admin = new Korisnik
            {
                Id = "admin-1",
                Ime = "Admin",
                Prezime = "Adminić",
                Email = "admin@test.com",
                Uloga = Uloga.SystemManager
            };

            _tutor = new Korisnik
            {
                Id = "tutor-1",
                Ime = "Tarik",
                Prezime = "Tarić",
                Email = "tarik@test.com",
                Uloga = Uloga.Tutor,
                VerifikovanTutor = false,
                PredmetiNaCekanjuJson = "[1, 2]",
                PredmetNaCekanjaId = 3
            };

            _context.Users.AddRange(_admin, _tutor);
            _context.SaveChanges();

            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_admin);
            _mockUserManager.Setup(m => m.FindByIdAsync("admin-1"))
                .ReturnsAsync(_admin);
            _mockUserManager.Setup(m => m.FindByIdAsync("tutor-1"))
                .ReturnsAsync(_tutor);
            _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<Korisnik>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.DeleteAsync(It.IsAny<Korisnik>()))
                .ReturnsAsync(IdentityResult.Success);
            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _controller = new SystemManagerController(_mockUserManager.Object, _context);
            TestHelper.SetupControllerContext(_controller, _admin);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
        }

        [TestMethod]
        public async Task Dashboard_ValidanAdmin_VracaStatistiku()
        {
            _context.Transakcija.Add(new Transakcija
            {
                id = 1,
                idTutora = _admin.Id,
                iznos = 15.0,
                datumUplate = DateTime.Now,
                statusPlacanja = StatusPlacanja.Uspjesno
            });
            await _context.SaveChangesAsync();

            var result = await _controller.Dashboard() as ViewResult;
            var model = result?.Model as AdminDashboardViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(15.0, model.UkupanPromet);
            Assert.AreEqual(1, model.ZahtjevaNCekanju);
        }

        [TestMethod]
        public async Task Dashboard_NeautorizovanKorisnik_RedirectaNaHome()
        {
            _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((Korisnik?)null);

            var result = await _controller.Dashboard() as RedirectToActionResult;
            Assert.AreEqual("Home", result?.ControllerName);
        }

        [TestMethod]
        public async Task ZahtjeviZaVerifikaciju_VracaListuNeverifikovanihTutora()
        {
            var result = await _controller.ZahtjeviZaVerifikaciju() as ViewResult;
            var model = result?.Model as VerifikacijaViewModel;

            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.TutoriNaCekanju.Count);
            Assert.AreEqual(2, model.TutoriNaCekanju[0].PredmetiNaCekanju.Count);
        }

        [TestMethod]
        public async Task VerifikujTutora_ValidanTutor_VerifikujeIDodajePredmete()
        {
            var result = await _controller.VerifikujTutora("tutor-1") as RedirectToActionResult;

            Assert.AreEqual("ZahtjeviZaVerifikaciju", result?.ActionName);
            Assert.IsTrue(_tutor.VerifikovanTutor);
            Assert.AreEqual(3, await _context.KorisnikPredmet.CountAsync(kp => kp.idKorisnika == "tutor-1"));
        }

        [TestMethod]
        public async Task VerifikujTutora_NepostojeciTutor_VracaNotFound()
        {
            var result = await _controller.VerifikujTutora("nepostojeci");
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task OdbijTutora_ValidanINepostojeci()
        {
            Assert.IsInstanceOfType(await _controller.OdbijTutora("nepostojeci"), typeof(NotFoundResult));

            var result = await _controller.OdbijTutora("tutor-1") as RedirectToActionResult;
            Assert.AreEqual("ZahtjeviZaVerifikaciju", result?.ActionName);
        }

        [TestMethod]
        public async Task Prijave_DodavanjePrikazIBrisanje()
        {
            await _controller.DodajPrijavu("Neprimjereno ponašanje", "Opis prijave");
            var prijava = await _context.Prijava.FirstAsync();
            Assert.AreEqual("Neprimjereno ponašanje", prijava.Naslov);

            var viewRes = await _controller.Prijave() as ViewResult;
            Assert.IsNotNull(viewRes);

            await _controller.ObrisiPrijavu(prijava.Id);
            Assert.AreEqual(0, await _context.Prijava.CountAsync());
        }

        [TestMethod]
        public async Task KreirajManager_UspjesnoINeuspjesno()
        {
            Assert.IsInstanceOfType(_controller.Kreiraj(), typeof(ViewResult));

            var okRes = await _controller.Kreiraj("Novi", "Admin", "novi@admin.com", "Pass123!") as RedirectToActionResult;
            Assert.AreEqual("Index", okRes?.ActionName);

            _mockUserManager.Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Slaba lozinka" }));

            var failRes = await _controller.Kreiraj("Novi", "Admin", "novi@admin.com", "123") as ViewResult;
            Assert.IsNotNull(failRes);
            Assert.IsFalse(_controller.ModelState.IsValid);
        }

        [TestMethod]
        public async Task UpravljajKorisnicima_PretragaUredjivanjeIBrisanje()
        {
            var lista = await _controller.UpravljajKorisnicima("Tarik") as ViewResult;
            Assert.IsNotNull(lista);

            var urediRes = await _controller.UrediKorisnika("tutor-1", "NovoIme", "NovoPrezime", "novo@test.com") as RedirectToActionResult;
            Assert.AreEqual("UpravljajKorisnicima", urediRes?.ActionName);
            Assert.AreEqual("NovoIme", _tutor.Ime);

            Assert.IsInstanceOfType(await _controller.UrediKorisnika("nepostojeci", "a", "b", "c"), typeof(NotFoundResult));

            var obrisiRes = await _controller.ObrisiKorisnika("tutor-1") as RedirectToActionResult;
            Assert.AreEqual("UpravljajKorisnicima", obrisiRes?.ActionName);
        }

        [TestMethod]
        public async Task UpravljajPredmetima_DodajUrediObrisiPretrazi()
        {
            await _controller.DodajPredmet("Novi testni predmet");
            var predmet = await _context.Predmet.FirstAsync(p => p.naziv == "Novi testni predmet");

            var view = await _controller.UpravljajPredmetima("Novi") as ViewResult;
            Assert.IsNotNull(view);

            await _controller.UrediPredmet(predmet.id, "Izmijenjen predmet");
            Assert.AreEqual("Izmijenjen predmet", (await _context.Predmet.FindAsync(predmet.id))?.naziv);
            Assert.IsInstanceOfType(await _controller.UrediPredmet(99999, "X"), typeof(NotFoundResult));

            await _controller.ObrisiPredmet(predmet.id);
            Assert.IsNull(await _context.Predmet.FindAsync(predmet.id));
        }

        [TestMethod]
        public async Task OstaleAkcije_ZahtjeviPrilogTutoraIndexDetaljiObrisi()
        {
            Assert.IsInstanceOfType(await _controller.Zahtjevi(), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.PrilogTutora("tutor-1"), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.PrilogTutora("nepostojeci"), typeof(NotFoundResult));
            Assert.IsInstanceOfType(await _controller.Index(), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.Detalji("admin-1"), typeof(ViewResult));
            Assert.IsInstanceOfType(await _controller.Detalji("nepostojeci"), typeof(NotFoundResult));
            Assert.IsInstanceOfType(await _controller.Obrisi("admin-1"), typeof(RedirectToActionResult));
        }
    }
}