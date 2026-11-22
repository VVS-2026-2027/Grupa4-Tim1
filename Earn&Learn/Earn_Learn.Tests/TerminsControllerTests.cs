using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Earn_Learn.Controllers;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Earn_Learn.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Task21Tests.Infrastructure;

namespace Task21Tests.Controllers;

[TestClass]
public sealed class TerminsControllerTests
{
    private TestFixtureTask21 _fx = null!;

    [TestInitialize]
    public void Init() => _fx = new TestFixtureTask21();

    [TestCleanup]
    public void Cleanup() => _fx.Dispose();

   
    private TerminsController Controller() =>
        _fx.Controller(new TerminsController(_fx.Db, _fx.Users.Object, new TutorRangService()));

    private Termin AddTermin(
        string tutorId = "tutor-1",
        StatusTermina status = StatusTermina.Slobodan,
        string studentId = "",
        double price = 20,
        bool future = true,
        string qrKod = "QR-TEST")
    {
        var termin = _fx.AddTermin(tutorId, status, studentId, price, future);
        termin.qrKod = qrKod;
        _fx.Db.SaveChanges();
        return termin;
    }

    [TestMethod]
    public async Task Index_VracaSveTermine()
    {
        AddTermin();
        AddTermin("tutor-2");

        var result = (ViewResult)await Controller().Index();

        Assert.AreEqual(2, ((IEnumerable<Termin>)result.Model!).Count());
    }

    [TestMethod]
    public async Task MojiCasovi_NeprijavljenKorisnik_PreusmjeravaNaLogin()
    {
        var result = (RedirectToActionResult)await Controller().MojiCasovi();

        Assert.AreEqual("Login", result.ActionName);
        Assert.AreEqual("Account", result.ControllerName);
    }

    [TestMethod]
    public async Task MojiCasovi_Tutor_VracaSamoNjegoveTermineSortiranePoDatumu()
    {
        var tutor = _fx.AddUser("tutor-1", Uloga.Tutor);
        var ranijiTermin = AddTermin("tutor-1", future: false);
        AddTermin("tutor-2");
        var kasnijiTermin = AddTermin("tutor-1", future: true);
        ranijiTermin.datumIVrijeme = DateTime.Today.AddDays(1);
        kasnijiTermin.datumIVrijeme = DateTime.Today.AddDays(3);
        _fx.Db.SaveChanges();

        _fx.LoginAs(tutor);
        var result = (ViewResult)await Controller().MojiCasovi();
        var termini = ((IEnumerable<Termin>)result.Model!).ToList();

        Assert.AreEqual(2, termini.Count);
        Assert.IsTrue(termini[0].datumIVrijeme <= termini[1].datumIVrijeme);
    }

    [TestMethod]
    public async Task KreirajTermin_Get_NeprijavljenKorisnik_Forbid()
    {
        var result = await Controller().KreirajTermin();

        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task KreirajTermin_Get_Student_Forbid()
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student));

        var result = await Controller().KreirajTermin();

        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task KreirajTermin_Get_Tutor_VracaFormuICijenu()
    {
        var tutor = _fx.AddUser("tutor-1", Uloga.Tutor, hourPrice: 25);
        _fx.LoginAs(tutor);

        var result = (ViewResult)await Controller().KreirajTermin();

        Assert.AreEqual(25d, result.ViewData["CijenaPoSatu"]);
    }

    [TestMethod]
    public async Task RezervisiTermin_Get_NeprijavljenKorisnik_PristupOdbijen()
    {
        var result = (ViewResult)await Controller().RezervisiTermin(1);

        Assert.AreEqual("PristupOdbijen", result.ViewName);
    }

    [TestMethod]
    public async Task RezervisiTermin_Get_Tutor_PristupOdbijen()
    {
        _fx.LoginAs(_fx.AddUser("tutor-1", Uloga.Tutor));

        var result = (ViewResult)await Controller().RezervisiTermin(1);

        Assert.AreEqual("PristupOdbijen", result.ViewName);
    }

    [TestMethod]
    public async Task RezervisiTermin_Get_NepostojeciSlobodanTermin_NotFound()
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student));

        var result = await Controller().RezervisiTermin(12345);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task RezervisiTermin_Get_SlobodanTermin_VracaTermin()
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student));
        var termin = AddTermin();

        var result = (ViewResult)await Controller().RezervisiTermin(termin.id);

        Assert.AreEqual(termin.id, ((Termin)result.Model!).id);
    }

    [TestMethod]
    public async Task RezervisiTermin_Post_PogresnaUloga_PristupOdbijen()
    {
        _fx.LoginAs(_fx.AddUser("tutor-1", Uloga.Tutor));

        var result = (ViewResult)await Controller().RezervisiTermin(1, default);

        Assert.AreEqual("PristupOdbijen", result.ViewName);
    }

    [TestMethod]
    public async Task RezervisiTermin_Post_NedostupanTermin_PreusmjeravaNaIndexStudenta()
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student));

        var controller = Controller();
        var result = (RedirectToActionResult)await controller.RezervisiTermin(999, default);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual("Student", result.ControllerName);
        Assert.IsTrue(controller.TempData.ContainsKey("Greska"));
    }

    [DataTestMethod]
    [DataRow(0d)]
    [DataRow(5d)]
    [DataRow(19.99d)]
    public async Task RezervisiTermin_Post_NedovoljnoNovca_VracaNaRezervaciju(double stanjeRacuna)
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student, balance: stanjeRacuna));
        var termin = AddTermin(price: 20);
        var controller = Controller();

        var result = (RedirectToActionResult)await controller.RezervisiTermin(termin.id, default);

        Assert.AreEqual("RezervisiTermin", result.ActionName);
        Assert.IsTrue(controller.TempData.ContainsKey("GreskaNovcenik"));
    }

    [TestMethod]
    public async Task RezervisiTermin_Post_DovoljnoNovca_RezerviseTermin()
    {
        var student = _fx.AddUser("student-1", Uloga.Student, balance: 100);
        _fx.LoginAs(student);
        var termin = AddTermin(price: 20);

        var result = (RedirectToActionResult)await Controller().RezervisiTermin(termin.id, default);
        var sacuvaniTermin = _fx.Db.Termin.Single();

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual("Student", result.ControllerName);
        Assert.AreEqual(StatusTermina.Rezervisan, sacuvaniTermin.status);
        Assert.AreEqual(student.Id, sacuvaniTermin.idStudenta);
    }

    [TestMethod]
    public async Task KreirajTermin_Post_NeprijavljenKorisnik_Forbid()
    {
        var result = await Controller().KreirajTermin(DateTime.Today, 1, default);

        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task KreirajTermin_Post_Student_Forbid()
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student));

        var result = await Controller().KreirajTermin(DateTime.Today, 1, default);

        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task KreirajTermin_Post_Tutor_KreiraSlobodanTermin()
    {
        var tutor = _fx.AddUser("tutor-1", Uloga.Tutor, hourPrice: 25);
        _fx.LoginAs(tutor);
        var datum = new DateTime(2026, 12, 1, 12, 0, 0);

        var result = (RedirectToActionResult)await Controller().KreirajTermin(datum, 7, default);
        var termin = _fx.Db.Termin.Single();

        Assert.AreEqual("MojiCasovi", result.ActionName);
        Assert.AreEqual(tutor.Id, termin.idTutora);
        Assert.AreEqual(datum, termin.datumIVrijeme);
        Assert.AreEqual(StatusTermina.Slobodan, termin.status);
        Assert.AreEqual(25d, termin.cijena);
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_PrazanKod_BadRequest()
    {
        var result = await Controller().PotvrdPrisustvo("");

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_NeispravanKod_BadRequest()
    {
        var result = await Controller().PotvrdPrisustvo(null!);

        Assert.IsInstanceOfType(result, typeof(BadRequestObjectResult));
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_PogresnaUloga_PristupOdbijen()
    {
        _fx.LoginAs(_fx.AddUser("tutor-1", Uloga.Tutor));

        var result = (ViewResult)await Controller().PotvrdPrisustvo("QR-TEST");

        Assert.AreEqual("PristupOdbijen", result.ViewName);
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_NepostojeciKod_PreusmjeravaNaDashboard()
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student));
        var controller = Controller();

        var result = (RedirectToActionResult)await controller.PotvrdPrisustvo("NEPOSTOJECI");

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual("Student", result.ControllerName);
        Assert.IsTrue(controller.TempData.ContainsKey("Greska"));
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_KodDrugogStudenta_PreusmjeravaNaDashboard()
    {
        _fx.LoginAs(_fx.AddUser("student-1", Uloga.Student));
        _fx.AddUser("student-2", Uloga.Student);
        var termin = AddTermin(studentId: "student-2", status: StatusTermina.Rezervisan);
        var controller = Controller();

        var result = (RedirectToActionResult)await controller.PotvrdPrisustvo(termin.qrKod!);

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.IsTrue(controller.TempData.ContainsKey("Greska"));
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_TerminVecOdrzan_PreusmjeravaNaDashboard()
    {
        var student = _fx.AddUser("student-1", Uloga.Student);
        _fx.LoginAs(student);
        var termin = AddTermin(studentId: student.Id, status: StatusTermina.Odrzan);
        var controller = Controller();

        var result = (RedirectToActionResult)await controller.PotvrdPrisustvo(termin.qrKod!);

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.IsTrue(controller.TempData.ContainsKey("Info"));
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_TerminNijeRezervisan_PreusmjeravaNaDashboard()
    {
        var student = _fx.AddUser("student-1", Uloga.Student);
        _fx.LoginAs(student);
        var termin = AddTermin(studentId: student.Id, status: StatusTermina.Slobodan);
        var controller = Controller();

        var result = (RedirectToActionResult)await controller.PotvrdPrisustvo(termin.qrKod!);

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.IsTrue(controller.TempData.ContainsKey("Greska"));
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_NedovoljnoNovca_PreusmjeravaNaDashboard()
    {
        var student = _fx.AddUser("student-1", Uloga.Student, balance: 5);
        var tutor = _fx.AddUser("tutor-1", Uloga.Tutor);
        _fx.LoginAs(student);
        var termin = AddTermin(tutorId: tutor.Id, studentId: student.Id,
            status: StatusTermina.Rezervisan, price: 20);
        var controller = Controller();

        var result = (RedirectToActionResult)await controller.PotvrdPrisustvo(termin.qrKod!);

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.IsTrue(controller.TempData.ContainsKey("Greska"));
        Assert.AreEqual(StatusTermina.Rezervisan, _fx.Db.Termin.Single().status);
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_ValidanTermin_PotvrdjujePrisustvoIUplate()
    {
        var student = _fx.AddUser("student-1", Uloga.Student, balance: 100);
        var tutor = _fx.AddUser("tutor-1", Uloga.Tutor, balance: 0);
        var manager = _fx.AddUser("manager-1", Uloga.SystemManager, balance: 0);
        _fx.LoginAs(student);
        var termin = AddTermin(tutorId: tutor.Id, studentId: student.Id,
            status: StatusTermina.Rezervisan, price: 50);
        var controller = Controller();

        var result = (RedirectToActionResult)await controller.PotvrdPrisustvo(termin.qrKod!);
        var sacuvaniStudent = _fx.Db.Users.Single(u => u.Id == student.Id);
        var sacuvaniTutor = _fx.Db.Users.Single(u => u.Id == tutor.Id);
        var sacuvaniManager = _fx.Db.Users.Single(u => u.Id == manager.Id);

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual(StatusTermina.Odrzan, _fx.Db.Termin.Single().status);
        Assert.IsTrue(_fx.Db.Termin.Single().prisustvoPotvrdjeno);
        Assert.AreEqual(50d, sacuvaniStudent.StanjeRacuna);
        Assert.AreEqual(45d, sacuvaniTutor.StanjeRacuna);
        Assert.AreEqual(1, sacuvaniTutor.BrojOdrzanihCasova);
        Assert.AreEqual(5d, sacuvaniManager.StanjeRacuna);
        Assert.AreEqual(3, _fx.Db.Transakcija.Count());
        Assert.AreEqual(1, _fx.Db.Obavjestenje.Count());
        Assert.IsTrue(controller.TempData.ContainsKey("Uspjeh"));
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_IstaknutiTutor_ObracunavaPovlastenuProviziju()
    {
        var student = _fx.AddUser("student-featured", Uloga.Student, balance: 100);
        var tutor = _fx.AddUser("tutor-featured", Uloga.Tutor);
        tutor.BrojOdrzanihCasova = 10;
        tutor.ProsjecnaOcjena = 4.5;
        var manager = _fx.AddUser("manager-featured", Uloga.SystemManager);
        _fx.LoginAs(student);
        var termin = AddTermin(tutorId: tutor.Id, studentId: student.Id,
            status: StatusTermina.Rezervisan, price: 50);

        var result = (RedirectToActionResult)await Controller().PotvrdPrisustvo(termin.qrKod!);
        var savedTutor = _fx.Db.Users.Single(u => u.Id == tutor.Id);
        var savedManager = _fx.Db.Users.Single(u => u.Id == manager.Id);

        Assert.AreEqual("Dashboard", result.ActionName);
        Assert.AreEqual(50d, student.StanjeRacuna);
        Assert.AreEqual(47.5d, savedTutor.StanjeRacuna);
        Assert.AreEqual(2.5d, savedManager.StanjeRacuna);
        Assert.AreEqual(11, savedTutor.BrojOdrzanihCasova);
    }

    [TestMethod]
    public async Task PotvrdPrisustvo_BezSystemManagera_KreiraDvijeTransakcije()
    {
        var student = _fx.AddUser("student-1", Uloga.Student, balance: 100);
        var tutor = _fx.AddUser("tutor-1", Uloga.Tutor);
        _fx.LoginAs(student);
        var termin = AddTermin(tutorId: tutor.Id, studentId: student.Id,
            status: StatusTermina.Rezervisan, price: 50);

        await Controller().PotvrdPrisustvo(termin.qrKod!);

        Assert.AreEqual(2, _fx.Db.Transakcija.Count());
    }

    [TestMethod]
    public async Task Details_NullId_NotFound()
    {
        var result = await Controller().Details(null);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Details_NepostojeciId_NotFound()
    {
        var result = await Controller().Details(98765);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Details_PostojeciTermin_VracaModel()
    {
        var termin = AddTermin();

        var result = (ViewResult)await Controller().Details(termin.id);

        Assert.AreEqual(termin.id, ((Termin)result.Model!).id);
    }

    [TestMethod]
    public void Create_Get_VracaFormu()
    {
        var result = Controller().Create();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task Create_Post_ValidanTermin_SpremaIPreusmjerava()
    {
        var termin = new Termin
        {
            idTutora = "tutor-1",
            idStudenta = string.Empty,
            datumIVrijeme = new DateTime(2026, 12, 1, 12, 0, 0),
            tipInstrukcija = default,
            status = StatusTermina.Slobodan,
            qrKod = "QR-CREATE"
        };

        var result = (RedirectToActionResult)await Controller().Create(termin);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(1, _fx.Db.Termin.Count());
    }

    [TestMethod]
    public async Task Create_Post_NevalidanModelState_VracaFormu()
    {
        var controller = Controller();
        controller.ModelState.AddModelError("datumIVrijeme", "Neispravan datum");

        var result = await controller.Create(new Termin());

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.AreEqual(0, _fx.Db.Termin.Count());
    }

    [TestMethod]
    public async Task Edit_Get_NullId_NotFound()
    {
        var result = await Controller().Edit((int?)null);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_Get_NepostojeciId_NotFound()
    {
        var result = await Controller().Edit((int?)98765);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_Get_PostojeciId_VracaTermin()
    {
        var termin = AddTermin();

        var result = (ViewResult)await Controller().Edit((int?)termin.id);

        Assert.AreEqual(termin.id, ((Termin)result.Model!).id);
    }

    [TestMethod]
    public async Task Edit_Post_NepodudaranId_NotFound()
    {
        var termin = new Termin { id = 1 };

        var result = await Controller().Edit(2, termin);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_Post_NevalidanModelState_VracaView()
    {
        var termin = AddTermin();
        var controller = Controller();
        controller.ModelState.AddModelError("datumIVrijeme", "Neispravan datum");

        var result = await controller.Edit(termin.id, termin);

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task Edit_Post_ValidnaIzmjena_SpremaIPreusmjerava()
    {
        var termin = AddTermin();
        var novoVrijeme = new DateTime(2026, 12, 5, 14, 0, 0);
        termin.datumIVrijeme = novoVrijeme;

        var result = (RedirectToActionResult)await Controller().Edit(termin.id, termin);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(novoVrijeme, _fx.Db.Termin.Single().datumIVrijeme);
    }

    [TestMethod]
    public async Task Delete_Get_NullId_NotFound()
    {
        var result = await Controller().Delete((int?)null);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Delete_Get_NepostojeciId_NotFound()
    {
        var result = await Controller().Delete((int?)98765);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Delete_Get_PostojeciId_VracaTermin()
    {
        var termin = AddTermin();

        var result = (ViewResult)await Controller().Delete((int?)termin.id);

        Assert.AreEqual(termin.id, ((Termin)result.Model!).id);
    }

    [TestMethod]
    public async Task DeleteConfirmed_PostojeciTermin_BriseTermin()
    {
        var termin = AddTermin();

        var result = (RedirectToActionResult)await Controller().DeleteConfirmed(termin.id);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(0, _fx.Db.Termin.Count());
    }

    [TestMethod]
    public async Task DeleteConfirmed_NepostojeciTermin_NeBacaIzuzetak()
    {
        var result = await Controller().DeleteConfirmed(98765);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
    }
}
