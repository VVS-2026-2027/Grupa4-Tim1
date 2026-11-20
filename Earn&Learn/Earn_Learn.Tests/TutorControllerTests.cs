using System.Text.Json;
using Task21Tests.Infrastructure;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Task21Tests.Controllers;

[TestClass]
public sealed class TutorControllerTests
{
    private TestFixtureTask21 _fx = null!;
    [TestInitialize] public void Init() => _fx = new TestFixtureTask21();
    [TestCleanup] public void Cleanup() => _fx.Dispose();

    [TestMethod]
    public async Task Dashboard_BezPrijavljenogTutora_PreusmjeravaNaPocetnu()
    {
        var result = (RedirectToActionResult)await _fx.Tutor().Dashboard();
        Assert.AreEqual("Home", result.ControllerName);
    }

    [TestMethod]
    public async Task Dashboard_PogresnaUloga_PreusmjeravaNaPocetnu()
    {
        _fx.LoginAs(_fx.AddUser("student"));
        var result = (RedirectToActionResult)await _fx.Tutor().Dashboard();
        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    public async Task Dashboard_NeverifikovanTutor_PrivremenaStranica()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor, verified: false));
        var result = (ViewResult)await _fx.Tutor().Dashboard();
        Assert.AreEqual("VerifikacijaNaCekanju", result.ViewName);
    }

    [TestMethod]
    public async Task Dashboard_VerifikovanTutor_VracaDashboardModel()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, balance: 45, hourPrice: 20);
        _fx.LoginAs(tutor);
        _fx.AddTermin(tutor.Id);
        var result = (ViewResult)await _fx.Tutor().Dashboard();
        Assert.IsInstanceOfType(result.Model, typeof(TutorDashboardViewModel));
        Assert.AreEqual(1, ((TutorDashboardViewModel)result.Model!).NadolazaciTermini.Count);
    }

    [TestMethod]
    public async Task StudentDashboard_Neprijavljen_Pocetna()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().StudentDashboard(), typeof(RedirectToActionResult));
    }

    [TestMethod]
    public async Task StudentDashboard_NeverifikovanTutor_Cekanje()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor, verified: false));
        Assert.AreEqual("VerifikacijaNaCekanju", ((ViewResult)await _fx.Tutor().StudentDashboard()).ViewName);
    }

    [TestMethod]
    public async Task StudentDashboard_Verifikovan_VracaStudentDashboard()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor));
        var result = (ViewResult)await _fx.Tutor().StudentDashboard();
        Assert.IsInstanceOfType(result.Model, typeof(StudentDashboardViewModel));
    }

    [TestMethod]
    public async Task OtkaziTermin_Neprijavljen_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().OtkaziTermin(12), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task OtkaziTermin_Nepostojeci_NotFound()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor));
        Assert.IsInstanceOfType(await _fx.Tutor().OtkaziTermin(999), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task OtkaziTermin_TudjiTermin_NotFound()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.AddTermin("nekoDrugi");
        _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().OtkaziTermin(_fx.Db.Termin.Single().id), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task OtkaziTermin_Rezervisan_RefundiraINotificira()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var student = _fx.AddUser("student", balance: 10);
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan, student.Id, 30);
        _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().OtkaziTermin(termin.id), typeof(RedirectToActionResult));
        Assert.AreEqual(40d, student.StanjeRacuna);
        Assert.AreEqual(0, _fx.Db.Termin.Count());
        Assert.AreEqual(1, _fx.Db.Obavjestenje.Count());
        _fx.Users.Verify(x => x.UpdateAsync(student), Times.Once);
    }

    [TestMethod]
    public async Task OtkaziTermin_Slobodan_BriseBezRefundacije()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(tutor);
        await _fx.Tutor().OtkaziTermin(termin.id);
        Assert.AreEqual(0, _fx.Db.Termin.Count());
        Assert.AreEqual(0, _fx.Db.Obavjestenje.Count());
    }

    [TestMethod]
    public async Task Profil_NepostojeciTutor_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Profil("nema"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Profil_StudentUmjestoTutora_NotFound()
    {
        var student = _fx.AddUser("student");
        Assert.IsInstanceOfType(await _fx.Tutor().Profil(student.Id), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Profil_PostojeciTutor_PopunjavaPodatke()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.Recenzija.Add(new Recenzija { idStudenta="unknown", idTutora=tutor.Id, ocjena=4 });
        _fx.Db.SaveChanges();
        var result = (ViewResult)await _fx.Tutor().Profil(tutor.Id);
        var model = (TutorProfilViewModel)result.Model!;
        Assert.AreEqual(1, model.UkupnoRecenzija);
        Assert.AreEqual(4d, model.ProsjecnaOcjena);
        Assert.AreEqual("Student", model.Recenzije.Single().ImeStudenta);
    }

    [TestMethod]
    public async Task RezervisiTermin_BezPrijave_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().RezervisiTermin(1, 1, TipInstrukcija.Online), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task RezervisiTermin_NedostajeUBazi_Unauthorized()
    {
        _fx.LoginAs(new Korisnik { Id="missing", Uloga=Uloga.Student });
        Assert.IsInstanceOfType(await _fx.Tutor().RezervisiTermin(1, 1, TipInstrukcija.Online), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task RezervisiTermin_NepostojeciTermin_ProfilGreska()
    {
        _fx.LoginAs(_fx.AddUser("student", balance: 100));
        Assert.IsInstanceOfType(await _fx.Tutor().RezervisiTermin(999, 1, TipInstrukcija.Online), typeof(RedirectToActionResult));
    }

    [TestMethod]
    public async Task RezervisiTermin_ProsliTermin_NijeDostupan()
    {
        var student = _fx.AddUser("student", balance: 100);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 10);
        var termin = _fx.AddTermin(tutor.Id, future: false);
        _fx.LoginAs(student);
        await _fx.Tutor().RezervisiTermin(termin.id, 1, TipInstrukcija.Online);
        Assert.AreEqual(StatusTermina.Slobodan, termin.status);
    }

    [TestMethod]
    public async Task RezervisiTermin_SvojTermin_Odbijen()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, balance:100);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(tutor);
        await _fx.Tutor().RezervisiTermin(termin.id, 1, TipInstrukcija.Online);
        Assert.AreEqual(StatusTermina.Slobodan, termin.status);
    }

    [TestMethod]
    public async Task RezervisiTermin_NepostojeciTutor_NotFound()
    {
        var student = _fx.AddUser("student", balance: 100);
        var termin = _fx.AddTermin("no-tutor");
        _fx.LoginAs(student);
        Assert.IsInstanceOfType(await _fx.Tutor().RezervisiTermin(termin.id, 1, TipInstrukcija.Online), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task RezervisiTermin_NedovoljnoSredstava_ProfilTutora()
    {
        var student = _fx.AddUser("student", balance: 5);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 20);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(student);
        var result = (RedirectToActionResult)await _fx.Tutor().RezervisiTermin(termin.id, 1, TipInstrukcija.Uzivo);
        Assert.AreEqual("Profil", result.ActionName);
        Assert.AreEqual(StatusTermina.Slobodan, termin.status);
    }

    [TestMethod]
    public async Task RezervisiTermin_ValidanStudent_AzuriraTerminIObavjestenje()
    {
        var student = _fx.AddUser("student", balance: 100);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 30);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(student);
        var result = (RedirectToActionResult)await _fx.Tutor().RezervisiTermin(termin.id, 55, TipInstrukcija.Online);
        Assert.AreEqual("Student", result.ControllerName);
        Assert.AreEqual(StatusTermina.Rezervisan, termin.status);
        Assert.AreEqual(student.Id, termin.idStudenta);
        Assert.AreEqual(30d, termin.cijena);
        Assert.AreEqual(1, _fx.Db.Obavjestenje.Count());
    }

    [TestMethod]
    public async Task RezervisiTermin_TutorKojiJeStudent_StudentDashboard()
    {
        var studentTutor = _fx.AddUser("studentTutor", Uloga.Tutor, balance: 100);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 20);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(studentTutor);
        var result = (RedirectToActionResult)await _fx.Tutor().RezervisiTermin(termin.id, 1, TipInstrukcija.Online);
        Assert.AreEqual("StudentDashboard", result.ActionName);
    }

    [TestMethod]
    public async Task OstaviRecenziju_BezPrijave_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().OstaviRecenziju("tutor", 5, "x"), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task OstaviRecenziju_BezOdrzanogCasa_Odbija()
    {
        var student = _fx.AddUser("student");
        _fx.LoginAs(student);
        var result = (RedirectToActionResult)await _fx.Tutor().OstaviRecenziju("tutor", 4, "x");
        Assert.AreEqual("Profil", result.ActionName);
        Assert.AreEqual(0, _fx.Db.Recenzija.Count());
    }

    [TestMethod]
    public async Task OstaviRecenziju_PonovljenaRecenzija_Odbija()
    {
        var student = _fx.AddUser("student");
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.AddTermin(tutor.Id, StatusTermina.Odrzan, student.Id);
        _fx.Db.Recenzija.Add(new Recenzija { idStudenta=student.Id, idTutora=tutor.Id, ocjena=5 });
        _fx.Db.SaveChanges();
        _fx.LoginAs(student);
        await _fx.Tutor().OstaviRecenziju(tutor.Id, 3, "duplikat");
        Assert.AreEqual(1, _fx.Db.Recenzija.Count());
    }

    public sealed class RatingCase
    {
        public int ocjena { get; set; }
        public int ocekivanaOcjena { get; set; }
    }

    public static IEnumerable<object[]> JsonOcjene()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "recenzije.json");
        return (JsonSerializer.Deserialize<List<RatingCase>>(File.ReadAllText(path)) ?? new())
            .Select(c => new object[] { c.ocjena, c.ocekivanaOcjena });
    }

    [DataTestMethod]
    [DynamicData(nameof(JsonOcjene), DynamicDataSourceType.Method)]
    public async Task OstaviRecenziju_DataDrivenJson_OcjenaSeOgranicavaNa1Do5(int ocjena, int ocekivana)
    {
        var student = _fx.AddUser("student");
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.AddTermin(tutor.Id, StatusTermina.Odrzan, student.Id);
        _fx.LoginAs(student);
        await _fx.Tutor().OstaviRecenziju(tutor.Id, ocjena, "Komentar");
        Assert.AreEqual(ocekivana, _fx.Db.Recenzija.Single().ocjena);
        Assert.AreEqual((double)ocekivana, tutor.ProsjecnaOcjena!.Value);
    }

    [TestMethod]
    public async Task Index_VracaSamoVerifikovaneTutore()
    {
        _fx.AddUser("t1", Uloga.Tutor);
        _fx.AddUser("t2", Uloga.Tutor, verified: false);
        _fx.AddUser("s1", Uloga.Student);
        var result = (ViewResult)await _fx.Tutor().Index();
        Assert.AreEqual(1, ((IEnumerable<Korisnik>)result.Model!).Count());
    }

    [TestMethod]
    public async Task Detalji_Nepostojeci_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Detalji("nema"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Detalji_PostojeciTutor_VracaModel()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var result = (ViewResult)await _fx.Tutor().Detalji(tutor.Id);
        Assert.AreSame(tutor, result.Model);
    }

    [TestMethod]
    public void Kreiraj_Get_VracaView()
    {
        Assert.IsInstanceOfType(_fx.Tutor().Kreiraj(), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Kreiraj_Post_Uspjesno_Redirecta()
    {
        var result = (RedirectToActionResult)await _fx.Tutor().Kreiraj("A", "B", "a@b.ba", "Test123!", 20);
        Assert.AreEqual("Index", result.ActionName);
        _fx.Users.Verify(x => x.CreateAsync(It.Is<Korisnik>(u => u.Uloga == Uloga.Tutor), "Test123!"), Times.Once);
    }

    [TestMethod]
    public async Task Kreiraj_Post_NeuspioIdentity_VracaViewIGresku()
    {
        _fx.Users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description="Greška registracije" }));
        var controller = _fx.Tutor();
        Assert.IsInstanceOfType(await controller.Kreiraj("A", "B", "x@x.ba", "a", 20), typeof(ViewResult));
        Assert.IsFalse(controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Uredi_Get_Nepostojeci_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Uredi("nema"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Uredi_Get_Postojeci_VracaModel()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var result = (ViewResult)await _fx.Tutor().Uredi(tutor.Id);
        Assert.AreSame(tutor, result.Model);
    }

    [TestMethod]
    public async Task Uredi_Post_Nepostojeci_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Uredi("nema", 25), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Uredi_Post_Postojeci_MijenjaCijenu()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 10);
        var result = (RedirectToActionResult)await _fx.Tutor().Uredi(tutor.Id, 30);
        Assert.AreEqual(30d, tutor.CijenaPoSatu!.Value);
        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    public async Task Obrisi_Nepostojeci_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Obrisi("nema"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Obrisi_Postojeci_PozivaUserManager()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var result = (RedirectToActionResult)await _fx.Tutor().Obrisi(tutor.Id);
        Assert.AreEqual("Index", result.ActionName);
        _fx.Users.Verify(x => x.DeleteAsync(tutor), Times.Once);
    }

    [TestMethod]
    public async Task Pretraga_PrazanTekst_PreusmjeravaNaStudentDashboard()
    {
        var result = (RedirectToActionResult)await _fx.Tutor().Pretraga(" ");
        Assert.AreEqual("Dashboard", result.ActionName);
    }

    [TestMethod]
    public async Task Pretraga_ImeTutora_PronalaziTutora()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        tutor.Ime = "Lejla"; _fx.Db.SaveChanges();
        var result = (ViewResult)await _fx.Tutor().Pretraga("Lejla");
        Assert.AreEqual(1, ((IEnumerable<Korisnik>)result.Model!).Count());
    }

    [TestMethod]
    public async Task RezervacijaTermina_NepostojeciTutor_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().RezervacijaTermina("nema"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task RezervacijaTermina_PostojeciTutor_VracaSlobodneTermine()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.AddTermin(tutor.Id);
        _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan);
        var result = (ViewResult)await _fx.Tutor().RezervacijaTermina(tutor.Id);
        Assert.AreEqual(1, ((TutorProfilViewModel)result.Model!).DostupniTermini.Count);
    }

    [TestMethod]
    public async Task Recenzije_NepostojeciTutor_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Recenzije("nema"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Recenzije_PostojeciTutor_VracaProsjek()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.Recenzija.Add(new Recenzija { idStudenta="guest", idTutora=tutor.Id, ocjena=3 });
        _fx.Db.SaveChanges();
        var result = (ViewResult)await _fx.Tutor().Recenzije(tutor.Id);
        Assert.AreEqual(3d, ((TutorProfilViewModel)result.Model!).ProsjecnaOcjena);
    }

    [TestMethod]
    public async Task MojiCasovi_BezPrijave_Preusmjerava()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().MojiCasovi(), typeof(RedirectToActionResult));
    }

    [TestMethod]
    public async Task MojiCasovi_Neverifikovan_Cekanje()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor, verified: false));
        Assert.AreEqual("VerifikacijaNaCekanju", ((ViewResult)await _fx.Tutor().MojiCasovi()).ViewName);
    }

    [TestMethod]
    public async Task MojiCasovi_Verifikovan_VracaTermine()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.AddTermin(tutor.Id);
        _fx.LoginAs(tutor);
        var result = (ViewResult)await _fx.Tutor().MojiCasovi();
        Assert.AreEqual(1, ((System.Collections.ICollection)result.Model!).Count);
    }

    [TestMethod]
    public async Task KreirajTermin_Get_BezPrijave_Forbid()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().KreirajTermin(), typeof(ForbidResult));
    }

    [TestMethod]
    public async Task KreirajTermin_Get_Neverifikovan_Cekanje()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor, verified: false));
        Assert.AreEqual("VerifikacijaNaCekanju", ((ViewResult)await _fx.Tutor().KreirajTermin()).ViewName);
    }

    [TestMethod]
    public async Task KreirajTermin_Post_BezPrijave_Forbid()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().KreirajTermin(DateTime.Now.AddDays(1), 1, TipInstrukcija.Online), typeof(ForbidResult));
    }

    [TestMethod]
    public async Task KreirajTermin_Post_Verifikovan_DodajeTermin()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 35);
        _fx.LoginAs(tutor);
        var result = (RedirectToActionResult)await _fx.Tutor().KreirajTermin(DateTime.Now.AddDays(2), 1, TipInstrukcija.Online);
        Assert.AreEqual("MojiCasovi", result.ActionName);
        Assert.AreEqual(35d, _fx.Db.Termin.Single().cijena);
    }

    [TestMethod]
    public async Task UnesMjestoCasa_Get_BezPrijave_Forbid()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().UnesMjestoCasa(1), typeof(ForbidResult));
    }

    [TestMethod]
    public async Task UnesMjestoCasa_Get_TudjiTermin_NotFound()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor));
        var termin = _fx.AddTermin("drugi");
        Assert.IsInstanceOfType(await _fx.Tutor().UnesMjestoCasa(termin.id), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task UnesMjestoCasa_Post_ValidanTermin_SpremaLokaciju()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(tutor);
        var result = (RedirectToActionResult)await _fx.Tutor().UnesMjestoCasa(termin.id, "Sala 4");
        Assert.AreEqual("MojiCasovi", result.ActionName);
        Assert.AreEqual("Sala 4", termin.mjestoCasa);
    }

    [TestMethod]
    public async Task QrKod_BezPrijave_PreusmjeravaNaPocetnu()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().QrKod(1), typeof(RedirectToActionResult));
    }

    [TestMethod]
    public async Task QrKod_NepostojeciTermin_NotFound()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor));
        Assert.IsInstanceOfType(await _fx.Tutor().QrKod(1), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task QrKod_BezPostojecegKoda_KreiraKod()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().QrKod(termin.id), typeof(ViewResult));
        StringAssert.StartsWith(termin.qrKod!, "EARNLEARN-");
    }

    [TestMethod]
    public async Task ZavrsiCas_BezPrijave_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().ZavrsiCas(1), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task ZavrsiCas_NepostojeciTermin_NotFound()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor));
        Assert.IsInstanceOfType(await _fx.Tutor().ZavrsiCas(99), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task ZavrsiCas_ValidanTermin_PovecavaBrojCasova()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(tutor);
        var result = (RedirectToActionResult)await _fx.Tutor().ZavrsiCas(termin.id);
        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual(StatusTermina.Odrzan, termin.status);
        Assert.AreEqual(1, tutor.BrojOdrzanihCasova!.Value);
    }

    [TestMethod]
    public async Task Novcanik_BezPrijave_Preusmjerava()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Novcanik(), typeof(RedirectToActionResult));
    }

    [TestMethod]
    public async Task Novcanik_Tutor_VracaTransakcije()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, balance: 50);
        _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().Novcanik(), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Obavjestenja_BezPrijave_Preusmjerava()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().Obavjestenja(), typeof(RedirectToActionResult));
    }

    [TestMethod]
    public async Task Obavjestenja_Tutor_OznacavaProcitanim()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.Obavjestenje.Add(new Obavjestenje { idKorisnika=tutor.Id, naslov="Obavijest", procitano=false });
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().Obavjestenja(), typeof(ViewResult));
        Assert.IsTrue(_fx.Db.Obavjestenje.Single().procitano);
    }

    [TestMethod]
    public async Task DohvatiNotifikacije_BezPrijave_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().DohvatiNotifikacije(), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task DohvatiNotifikacije_Prijavljen_VracaJson()
    {
        _fx.LoginAs(_fx.AddUser("tutor", Uloga.Tutor));
        Assert.IsInstanceOfType(await _fx.Tutor().DohvatiNotifikacije(), typeof(JsonResult));
    }

    [TestMethod]
    public async Task BrojNotifikacija_BezPrijave_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().BrojNotifikacija(), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task BrojNotifikacija_Prijavljen_BrojiNeprocitane()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.Obavjestenje.Add(new Obavjestenje { idKorisnika=tutor.Id, naslov="Nova", procitano=false });
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        var result = (JsonResult)await _fx.Tutor().BrojNotifikacija();
        Assert.IsNotNull(result.Value);
    }

    [TestMethod]
    public async Task OznaciProcitanim_BezPrijave_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().OznaciProcitanim(1), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task OznaciProcitanim_SamoSvojeObavjestenje()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.Obavjestenje.Add(new Obavjestenje { idKorisnika=tutor.Id, naslov="Nova", procitano=false });
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        var obavjestenje = _fx.Db.Obavjestenje.Single();
        Assert.IsInstanceOfType(await _fx.Tutor().OznaciProcitanim(obavjestenje.id), typeof(OkResult));
        Assert.IsTrue(obavjestenje.procitano);
    }

    [TestMethod]
    public async Task OznaciSveProcitanim_BezPrijave_Unauthorized()
    {
        Assert.IsInstanceOfType(await _fx.Tutor().OznaciSveProcitanim(), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task OznaciSveProcitanim_Prijavljen_SvaNjegovaProcitana()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var notice = new Obavjestenje { idKorisnika=tutor.Id, naslov="Nova", procitano=false };
        _fx.Db.Obavjestenje.Add(notice); _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().OznaciSveProcitanim(), typeof(OkResult));
        Assert.IsTrue(notice.procitano);
    }

    [TestMethod]
    public async Task Dashboard_SPredmetomIStudentom_DetaljiPopunjeni()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var student = _fx.AddUser("student");
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan, student.Id);
        termin.idPredmeta = 1; _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        var result = (ViewResult)await _fx.Tutor().Dashboard();
        Assert.IsNotNull(((TutorDashboardViewModel)result.Model!).NadolazaciTermini.Single().ImePredmeta);
    }

    [TestMethod]
    public async Task StudentDashboard_SaTerminima_PrikazujeTutorovuRezervaciju()
    {
        var tutorStudent = _fx.AddUser("tutorStudent", Uloga.Tutor);
        var tutor = _fx.AddUser("drugi", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan, tutorStudent.Id);
        termin.idPredmeta = 1; _fx.Db.SaveChanges(); _fx.LoginAs(tutorStudent);
        var result = (ViewResult)await _fx.Tutor().StudentDashboard();
        Assert.AreEqual(1, ((StudentDashboardViewModel)result.Model!).NadolazaciTermini.Count);
    }

    [TestMethod]
    public async Task OtkaziTermin_BezStudenta_KoristiGranuBezRefundacije()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan, "nepostojeci", 20);
        _fx.LoginAs(tutor);
        await _fx.Tutor().OtkaziTermin(termin.id);
        Assert.AreEqual(0, _fx.Db.Obavjestenje.Count());
    }

    [TestMethod]
    public async Task Profil_SPovezanimPredmetomITerminom_VracaPripadajucePodatke()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.KorisnikPredmet.Add(new KorisnikPredmet { idKorisnika=tutor.Id, idPredmeta=1 });
        var termin = _fx.AddTermin(tutor.Id); termin.idPredmeta = 1;
        _fx.Db.SaveChanges();
        var result = (ViewResult)await _fx.Tutor().Profil(tutor.Id);
        Assert.AreEqual(1, ((TutorProfilViewModel)result.Model!).Predmeti.Count);
        Assert.AreEqual(1, ((TutorProfilViewModel)result.Model!).DostupniTermini.Count);
    }

    [TestMethod]
    public async Task RezervisiTermin_VecRezervisan_VracaGresku()
    {
        var student = _fx.AddUser("student", balance: 100);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 10);
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan);
        _fx.LoginAs(student);
        await _fx.Tutor().RezervisiTermin(termin.id, 1, TipInstrukcija.Online);
        Assert.AreEqual(StatusTermina.Rezervisan, termin.status);
    }

    [TestMethod]
    public async Task RezervisiTermin_NeHibridniTermin_NeMijenjaTipInstrukcije()
    {
        var student = _fx.AddUser("student", balance: 100);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 10);
        var termin = _fx.AddTermin(tutor.Id); termin.tipInstrukcija = TipInstrukcija.Uzivo;
        _fx.Db.SaveChanges(); _fx.LoginAs(student);
        await _fx.Tutor().RezervisiTermin(termin.id, 1, TipInstrukcija.Online);
        Assert.AreEqual(TipInstrukcija.Uzivo, termin.tipInstrukcija);
    }

    [TestMethod]
    public async Task Pretraga_PoPredmetu_VracaPovezanogTutora()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.KorisnikPredmet.Add(new KorisnikPredmet { idKorisnika=tutor.Id, idPredmeta=1 });
        _fx.Db.SaveChanges();
        var result = (ViewResult)await _fx.Tutor().Pretraga("Administracija računarskih mreža");
        Assert.AreEqual(1, ((IEnumerable<Korisnik>)result.Model!).Count());
    }

    [TestMethod]
    public async Task Recenzije_PostojeciStudent_PrikazujeIme()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var student = _fx.AddUser("student"); student.Ime = "Studentica";
        _fx.Db.Recenzija.Add(new Recenzija { idStudenta=student.Id, idTutora=tutor.Id, ocjena=4 });
        _fx.Db.SaveChanges();
        var result = (ViewResult)await _fx.Tutor().Recenzije(tutor.Id);
        Assert.AreEqual("Studentica", ((TutorProfilViewModel)result.Model!).Recenzije.Single().ImeStudenta);
    }

    [TestMethod]
    public async Task KreirajTermin_Get_VerifikovanTutor_VracaFormu()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.KorisnikPredmet.Add(new KorisnikPredmet { idKorisnika=tutor.Id, idPredmeta=1 });
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().KreirajTermin(), typeof(ViewResult));
    }

    [TestMethod]
    public async Task UnesMjestoCasa_Get_PostojiTermin_VracaTermin()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(tutor);
        var result = (ViewResult)await _fx.Tutor().UnesMjestoCasa(termin.id);
        Assert.AreSame(termin, result.Model);
    }

    [TestMethod]
    public async Task UnesMjestoCasa_Post_OnlineSaStudentom_SaljeObavjestenje()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var student = _fx.AddUser("student");
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan, student.Id);
        termin.tipInstrukcija = TipInstrukcija.Online; _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        await _fx.Tutor().UnesMjestoCasa(termin.id, "https://meet.test");
        StringAssert.Contains(_fx.Db.Obavjestenje.Single().sadrzaj, "online");
    }

    [TestMethod]
    public async Task UnesMjestoCasa_Post_UzivoSaStudentom_SaljeLokaciju()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var student = _fx.AddUser("student");
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan, student.Id);
        termin.tipInstrukcija = TipInstrukcija.Uzivo; _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        await _fx.Tutor().UnesMjestoCasa(termin.id, "Fakultet");
        StringAssert.Contains(_fx.Db.Obavjestenje.Single().sadrzaj, "lokaciju");
    }

    [TestMethod]
    public async Task QrKod_PostojiKod_NeGeneriseNovi()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id); termin.qrKod = "EARNLEARN-EXISTING";
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        await _fx.Tutor().QrKod(termin.id);
        Assert.AreEqual("EARNLEARN-EXISTING", termin.qrKod);
    }

    [TestMethod]
    public async Task Novcanik_PrikazujeUspjesneTransakcije()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Db.Transakcija.Add(new Transakcija {
            idTutora=tutor.Id, iznos=30, datumUplate=DateTime.UtcNow,
            statusPlacanja=StatusPlacanja.Uspjesno
        });
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        var result = (ViewResult)await _fx.Tutor().Novcanik();
        Assert.AreEqual(1, ((IEnumerable<Transakcija>)result.Model!).Count());
    }

    [TestMethod]
    public async Task Obavjestenja_PostojiTermin_SpajaSaLokacijom()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id); termin.mjestoCasa = "Ucionica";
        _fx.Db.Obavjestenje.Add(new Obavjestenje {
            idKorisnika=tutor.Id, naslov="Obavijest", idTermina=termin.id
        });
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        var result = (ViewResult)await _fx.Tutor().Obavjestenja();
        Assert.AreEqual(1, ((IEnumerable<ObavjestenjeViewModel>)result.Model!).Count());
    }

    [TestMethod]
    public async Task DohvatiNotifikacije_SaTerminom_VracaJson()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.Db.Obavjestenje.Add(new Obavjestenje {
            idKorisnika=tutor.Id, naslov="Obavijest", idTermina=termin.id
        });
        _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().DohvatiNotifikacije(), typeof(JsonResult));
    }

    [TestMethod]
    public async Task OznaciProcitanim_TudjeObavjestenje_OstajeNeprocitano()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var notice = new Obavjestenje { idKorisnika="drugi", naslov="Tudja", procitano=false };
        _fx.Db.Obavjestenje.Add(notice); _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        Assert.IsInstanceOfType(await _fx.Tutor().OznaciProcitanim(notice.id), typeof(OkResult));
        Assert.IsFalse(notice.procitano);
    }

    [TestMethod]
    public async Task OznaciSveProcitanim_TudjaObavjestenjaOstajuNeprocitana()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        var notice = new Obavjestenje { idKorisnika="drugi", naslov="Tudja", procitano=false };
        _fx.Db.Obavjestenje.Add(notice); _fx.Db.SaveChanges(); _fx.LoginAs(tutor);
        await _fx.Tutor().OznaciSveProcitanim();
        Assert.IsFalse(notice.procitano);
    }


    [TestMethod]
    public async Task Uredi_Post_KadIdentityServisBaciIzuzetak_PrenosiGresku()
    {
        var tutor = _fx.AddUser("tutor", Uloga.Tutor);
        _fx.Users.Setup(x => x.UpdateAsync(tutor))
            .ThrowsAsync(new InvalidOperationException("Test greska servisa"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
            await _fx.Tutor().Uredi(tutor.Id, 45));
    }

    [TestMethod]
    public async Task OstaviRecenziju_TutorNePostojiUIdentity_RecenzijaSeIpakSacuvava()
    {
        var student = _fx.AddUser("student");
        _fx.AddTermin("nepostojeci", StatusTermina.Odrzan, student.Id);
        _fx.LoginAs(student);
        Assert.IsInstanceOfType(await _fx.Tutor().OstaviRecenziju("nepostojeci", 4, "Dobar"), typeof(RedirectToActionResult));
        Assert.AreEqual(1, _fx.Db.Recenzija.Count());
    }

}
