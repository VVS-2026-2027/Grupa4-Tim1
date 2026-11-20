using Task21Tests.Infrastructure;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Task21Tests.Controllers;

[TestClass]
public sealed class RezervacijaControllerTests
{
    private TestFixtureTask21 _fx = null!;
    [TestInitialize] public void Init() => _fx = new TestFixtureTask21();
    [TestCleanup] public void Cleanup() => _fx.Dispose();

    [TestMethod]
    public async Task Potvrdi_NeprijavljenKorisnik_PristupOdbijen()
    {
        var result = (RedirectToActionResult)await _fx.Rezervacija().Potvrdi(1, TipInstrukcija.Online);
        Assert.AreEqual("PristupOdbijen", result.ActionName);
    }

    [TestMethod]
    public async Task Potvrdi_SystemManager_PristupOdbijen()
    {
        var admin = _fx.AddUser("admin", Uloga.SystemManager);
        _fx.LoginAs(admin);
        var result = (RedirectToActionResult)await _fx.Rezervacija().Potvrdi(1, TipInstrukcija.Online);
        Assert.AreEqual("PristupOdbijen", result.ActionName);
    }

    [TestMethod]
    public async Task Potvrdi_NepostojeciTermin_VracaDashboardIGresku()
    {
        var student = _fx.AddUser("student", Uloga.Student, balance: 100);
        _fx.LoginAs(student);
        var controller = _fx.Rezervacija();
        var result = (RedirectToActionResult)await controller.Potvrdi(9999, TipInstrukcija.Online);
        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual("Termin nije dostupan.", controller.TempData["Greska"]);
    }

    [TestMethod]
    public async Task Potvrdi_RezervisanTermin_NijeDostupan()
    {
        var student = _fx.AddUser("student", Uloga.Student, balance: 100);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 20);
        var termin = _fx.AddTermin(tutor.Id, StatusTermina.Rezervisan);
        _fx.LoginAs(student);
        var result = (RedirectToActionResult)await _fx.Rezervacija().Potvrdi(termin.id, TipInstrukcija.Online);
        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual(StatusTermina.Rezervisan, termin.status);
    }

    [TestMethod]
    public async Task Potvrdi_NedovoljnoSredstava_VracaProfilTutora()
    {
        var student = _fx.AddUser("student", Uloga.Student, balance: 9);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 20);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(student);
        var result = (RedirectToActionResult)await _fx.Rezervacija().Potvrdi(termin.id, TipInstrukcija.Online);
        Assert.AreEqual("Profil", result.ActionName);
        Assert.AreEqual(StatusTermina.Slobodan, termin.status);
        Assert.AreEqual(0, _fx.Db.Obavjestenje.Count());
    }

    [TestMethod]
    public async Task Potvrdi_UspjesnoRezervisanje_PostavljaStatusICijenuIObavijest()
    {
        var student = _fx.AddUser("student", Uloga.Student, balance: 50);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: 25);
        var termin = _fx.AddTermin(tutor.Id, price: 15);
        _fx.LoginAs(student);
        var result = (RedirectToActionResult)await _fx.Rezervacija().Potvrdi(termin.id, TipInstrukcija.Online);
        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual("Student", result.ControllerName);
        Assert.AreEqual(StatusTermina.Rezervisan, termin.status);
        Assert.AreEqual(student.Id, termin.idStudenta);
        Assert.AreEqual(25d, termin.cijena);
        Assert.AreEqual(TipInstrukcija.Online, termin.tipInstrukcija);
        Assert.AreEqual(1, _fx.Db.Obavjestenje.Count());
    }

    [TestMethod]
    public async Task Potvrdi_CijenaTutoraNedostaje_KoristiCijenuTermina()
    {
        var student = _fx.AddUser("student", Uloga.Student, balance: 50);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: null);
        var termin = _fx.AddTermin(tutor.Id, price: 12);
        _fx.LoginAs(student);
        await _fx.Rezervacija().Potvrdi(termin.id, TipInstrukcija.Hibridno);
        Assert.AreEqual(12d, termin.cijena);
    }

    [TestMethod]
    public async Task Potvrdi_TutorKojiRezervise_VracaTutorDashboard()
    {
        var studentTutor = _fx.AddUser("studentTutor", Uloga.Tutor, balance: 70);
        var tutor = _fx.AddUser("drugiTutor", Uloga.Tutor, hourPrice: 10);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(studentTutor);
        var result = (RedirectToActionResult)await _fx.Rezervacija().Potvrdi(termin.id, TipInstrukcija.Uzivo);
        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual("Tutor", result.ControllerName);
    }

    public static IEnumerable<object[]> CsvRezervacije()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", "rezervacije.csv");
        return File.ReadAllLines(path).Skip(1)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(line => line.Split(';'))
            .Select(columns => new object[] {
                double.Parse(columns[0], System.Globalization.CultureInfo.InvariantCulture),
                double.Parse(columns[1], System.Globalization.CultureInfo.InvariantCulture),
                bool.Parse(columns[2])
            });
    }

    [DataTestMethod]
    [DynamicData(nameof(CsvRezervacije), DynamicDataSourceType.Method)]
    public async Task Potvrdi_DataDrivenCsv_ProvjeravaGranicuSredstava(double stanje, double cijena, bool ocekujeUspjeh)
    {
        var student = _fx.AddUser("student", Uloga.Student, balance: stanje);
        var tutor = _fx.AddUser("tutor", Uloga.Tutor, hourPrice: cijena);
        var termin = _fx.AddTermin(tutor.Id);
        _fx.LoginAs(student);
        await _fx.Rezervacija().Potvrdi(termin.id, TipInstrukcija.Online);
        Assert.AreEqual(ocekujeUspjeh, termin.status == StatusTermina.Rezervisan);
    }
}
