using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Earn_Learn.Controllers;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Task21Tests.Infrastructure;

namespace Task21Tests.Controllers;

[TestClass]
public sealed class ProfilControllerTests
{
    private TestFixtureTask21 _fx = null!;
    private Mock<SignInManager<Korisnik>> _signInManager = null!;
    private Mock<IWebHostEnvironment> _env = null!;
    private string _webRootPath = null!;

    [TestInitialize]
    public void Init()
    {
        _fx = new TestFixtureTask21();

        _signInManager = new Mock<SignInManager<Korisnik>>(
            _fx.Users.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<Korisnik>>().Object,
            null!, null!, null!, null!);

        _signInManager
            .Setup(x => x.RefreshSignInAsync(It.IsAny<Korisnik>()))
            .Returns(Task.CompletedTask);

        _webRootPath = Path.Combine(Path.GetTempPath(), "ProfilControllerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_webRootPath);

        _env = new Mock<IWebHostEnvironment>();
        _env.SetupGet(x => x.WebRootPath).Returns(_webRootPath);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _fx.Dispose();

        if (Directory.Exists(_webRootPath))
            Directory.Delete(_webRootPath, true);
    }

    private ProfilController Profil()
    {
        var controller = new ProfilController(
            _fx.Users.Object,
            _signInManager.Object,
            _fx.Db,
            _env.Object);

        return _fx.Controller(controller);
    }

    private Predmet AddPredmet(string naziv = "Matematika")
    {
        var predmet = new Predmet { naziv = naziv };
        _fx.Db.Predmet.Add(predmet);
        _fx.Db.SaveChanges();
        return predmet;
    }

    private KorisnikPredmet AddKorisnikPredmet(string userId, int predmetId)
    {
        var veza = new KorisnikPredmet
        {
            idKorisnika = userId,
            idPredmeta = predmetId
        };

        _fx.Db.KorisnikPredmet.Add(veza);
        _fx.Db.SaveChanges();
        return veza;
    }

    private Recenzija AddRecenzija(string studentId, string tutorId, int ocjena = 5)
    {
        var recenzija = new Recenzija
        {
            idStudenta = studentId,
            idTutora = tutorId,
            ocjena = ocjena,
            komentar = "Test recenzija",
            datumRecenzije = DateTime.UtcNow
        };

        _fx.Db.Recenzija.Add(recenzija);
        _fx.Db.SaveChanges();
        return recenzija;
    }

    private static IFormFile CreateFile(
        string fileName,
        string contentType,
        int size = 10)
    {
        var bytes = new byte[size];
        var stream = new MemoryStream(bytes);

        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    // INDEX

    [TestMethod]
    public async Task Index_NeprijavljenKorisnik_PreusmjeravaNaHome()
    {
        _fx.LoginAs(null);

        var result = (RedirectToActionResult)await Profil().Index();

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual("Home", result.ControllerName);
    }

    [TestMethod]
    public async Task Index_Tutor_VracaPodatkeProfilaIPredmete()
    {
        var tutor = _fx.AddUser("tutor-index", Uloga.Tutor, hourPrice: 25);
        tutor.ProsjecnaOcjena = 4.5;
        _fx.Db.SaveChanges();
        _fx.LoginAs(tutor);

        var matematika = AddPredmet("Matematika");
        AddPredmet("Fizika");
        AddKorisnikPredmet(tutor.Id, matematika.id);

        var student = _fx.AddUser("student-index");
        AddRecenzija(student.Id, tutor.Id);
        _fx.AddTermin(tutor.Id, StatusTermina.Odrzan);

        var controller = Profil();
        var result = (ViewResult)await controller.Index();
        var model = (ProfilViewModel)result.Model!;

        Assert.AreEqual("Test", model.Ime);
        Assert.AreEqual("Korisnik", model.Prezime);
        Assert.AreEqual(Uloga.Tutor, model.Uloga);
        Assert.AreEqual(1, model.BrojRecenzija);
        Assert.AreEqual(1, model.BrojCasova);
        Assert.AreEqual("Matematika", model.Predmeti.Single());
        var naziviPredmeta = model.SviPredmeti
      .Select(p => p.naziv)
      .ToList();

        CollectionAssert.Contains(naziviPredmeta, "Matematika");
        CollectionAssert.Contains(naziviPredmeta, "Fizika");
        Assert.AreEqual(25d, (double)controller.ViewBag.CijenaPoSatu);
        Assert.AreEqual(4.5, (double)controller.ViewBag.ProsjecnaOcjena);
    }

    [TestMethod]
    public async Task Index_Student_VracaNjegoveRecenzijeIOdrzaneCasove()
    {
        var student = _fx.AddUser("student-index-2", Uloga.Student);
        var tutor = _fx.AddUser("tutor-index-2", Uloga.Tutor);
        _fx.LoginAs(student);

        AddRecenzija(student.Id, tutor.Id);
        _fx.AddTermin(tutor.Id, StatusTermina.Odrzan, student.Id);
        _fx.AddTermin(tutor.Id, StatusTermina.Slobodan, student.Id);

        var result = (ViewResult)await Profil().Index();
        var model = (ProfilViewModel)result.Model!;

        Assert.AreEqual(1, model.BrojRecenzija);
        Assert.AreEqual(1, model.BrojCasova);
        Assert.AreEqual("Test Korisnik", model.Recenzije.Single().ImeStudenta);
    }

    // UREDI GODINU STUDIJA

    [TestMethod]
    public async Task UrediGodinuStudija_NeprijavljenKorisnik_PreusmjeravaNaHome()
    {
        _fx.LoginAs(null);

        var result = (RedirectToActionResult)await Profil().UrediGodinuStudija(2);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual("Home", result.ControllerName);
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(-1)]
    public async Task UrediGodinuStudija_NevalidnaGodina_NeMijenjaPodatak(int godina)
    {
        var user = _fx.AddUser("student-godina");
        user.GodinaStudija = 3;
        _fx.LoginAs(user);

        var result = (RedirectToActionResult)await Profil().UrediGodinuStudija(godina);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(3, user.GodinaStudija);
    }

    [TestMethod]
    public async Task UrediGodinuStudija_ValidnaGodina_AzuriraPodatak()
    {
        var user = _fx.AddUser("student-godina-valid");
        _fx.LoginAs(user);

        var result = (RedirectToActionResult)await Profil().UrediGodinuStudija(4);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(4, user.GodinaStudija);
        _fx.Users.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    // POSTANI TUTOR

    [TestMethod]
    public async Task PostaniTutor_NeprijavljenKorisnik_PreusmjeravaNaIndex()
    {
        _fx.LoginAs(null);

        var result = (RedirectToActionResult)await Profil().PostaniTutor(new List<int>(), null);

        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    public async Task PostaniTutor_KorisnikNijeStudent_PreusmjeravaNaIndex()
    {
        var tutor = _fx.AddUser("vec-tutor", Uloga.Tutor);
        _fx.LoginAs(tutor);

        var result = (RedirectToActionResult)await Profil().PostaniTutor(new List<int> { 1 }, null);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(Uloga.Tutor, tutor.Uloga);
    }

    [TestMethod]
    public async Task PostaniTutor_BezOdabranihPredmeta_NeMijenjaUlogu()
    {
        var student = _fx.AddUser("student-no-subject");
        _fx.LoginAs(student);

        var controller = Profil();
        var result = (RedirectToActionResult)await controller.PostaniTutor(new List<int>(), null);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(Uloga.Student, student.Uloga);
        Assert.IsNotNull(controller.TempData["TutorGreska"]);
    }

    [TestMethod]
    public async Task PostaniTutor_NeispravanTipPriloga_OdbijaZahtjev()
    {
        var student = _fx.AddUser("student-bad-file");
        _fx.LoginAs(student);

        var result = (RedirectToActionResult)await Profil().PostaniTutor(
            new List<int> { 1 }, CreateFile("dokumento.exe", "application/octet-stream"));

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(Uloga.Student, student.Uloga);
    }

    [TestMethod]
    public async Task PostaniTutor_PrilogVeciOd5MB_OdbijaZahtjev()
    {
        var student = _fx.AddUser("student-large-file");
        _fx.LoginAs(student);

        var result = (RedirectToActionResult)await Profil().PostaniTutor(
            new List<int> { 1 }, CreateFile("ocjene.pdf", "application/pdf", 5 * 1024 * 1024 + 1));

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(Uloga.Student, student.Uloga);
    }

    [TestMethod]
    public async Task PostaniTutor_ValidanZahtjev_MijenjaUloguISpremaPredmete()
    {
        var student = _fx.AddUser("student-becomes-tutor");
        _fx.LoginAs(student);

        var result = (LocalRedirectResult)await Profil().PostaniTutor(new List<int> { 2, 4 }, null);

        Assert.AreEqual("/Tutor/Dashboard", result.Url);
        Assert.AreEqual(Uloga.Tutor, student.Uloga);
        Assert.IsFalse(student.VerifikovanTutor);
        CollectionAssert.AreEqual(new[] { 2, 4 },
            JsonSerializer.Deserialize<List<int>>(student.PredmetiNaCekanjuJson!)!);
        _fx.Users.Verify(x => x.UpdateAsync(student), Times.Once);
        _signInManager.Verify(x => x.RefreshSignInAsync(student), Times.Once);
    }

    [TestMethod]
    public async Task PostaniTutor_ValidanPDF_PohranjujePrilog()
    {
        var student = _fx.AddUser("student-pdf");
        _fx.LoginAs(student);

        await Profil().PostaniTutor(
            new List<int> { 1 }, CreateFile("ocjene.pdf", "application/pdf"));

        Assert.IsNotNull(student.PrilogOcjene);
        Assert.IsTrue(File.Exists(Path.Combine(
            _webRootPath, student.PrilogOcjene!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
    }

    // UREDI CIJENU

    [TestMethod]
    public async Task UrediCijenu_NeprijavljenKorisnik_PreusmjeravaNaHome()
    {
        _fx.LoginAs(null);

        var result = (RedirectToActionResult)await Profil().UrediCijenu(20);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual("Home", result.ControllerName);
    }

    [TestMethod]
    public async Task UrediCijenu_KorisnikNijeTutor_PreusmjeravaNaHome()
    {
        var student = _fx.AddUser("student-price");
        _fx.LoginAs(student);

        var result = (RedirectToActionResult)await Profil().UrediCijenu(20);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(0d, student.CijenaPoSatu ?? 0);
    }

    [TestMethod]
    public async Task UrediCijenu_NegativnaCijena_NeMijenjaCijenu()
    {
        var tutor = _fx.AddUser("tutor-negative-price", Uloga.Tutor, hourPrice: 20);
        _fx.LoginAs(tutor);

        var result = (RedirectToActionResult)await Profil().UrediCijenu(-5);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(20d, tutor.CijenaPoSatu);
    }

    [TestMethod]
    public async Task UrediCijenu_IspravnaCijena_AzuriraCijenu()
    {
        var tutor = _fx.AddUser("tutor-valid-price", Uloga.Tutor);
        _fx.LoginAs(tutor);

        var result = (RedirectToActionResult)await Profil().UrediCijenu(35.5);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(35.5, tutor.CijenaPoSatu);
        _fx.Users.Verify(x => x.UpdateAsync(tutor), Times.Once);
    }

    // DODAJ PREDMET

    [TestMethod]
    public async Task DodajPredmet_NeprijavljenKorisnik_PreusmjeravaNaIndex()
    {
        _fx.LoginAs(null);

        var result = (RedirectToActionResult)await Profil().DodajPredmet(1, null);

        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    public async Task DodajPredmet_KorisnikNijeTutor_PreusmjeravaNaIndex()
    {
        var student = _fx.AddUser("student-add-subject");
        _fx.LoginAs(student);

        var result = (RedirectToActionResult)await Profil().DodajPredmet(1, null);

        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    public async Task DodajPredmet_NepostojeciPredmet_NeSaljeZahtjev()
    {
        var tutor = _fx.AddUser("tutor-missing-subject", Uloga.Tutor);
        _fx.LoginAs(tutor);

        var result = (RedirectToActionResult)await Profil().DodajPredmet(999, null);

        Assert.AreEqual("Index", result.ActionName);
        Assert.IsNull(tutor.PredmetNaCekanjaId);
    }

    [TestMethod]
    public async Task DodajPredmet_PredmetVecPostoji_NeSaljeDupliZahtjev()
    {
        var tutor = _fx.AddUser("tutor-duplicate-subject", Uloga.Tutor);
        _fx.LoginAs(tutor);
        var predmet = AddPredmet();
        AddKorisnikPredmet(tutor.Id, predmet.id);

        var result = (RedirectToActionResult)await Profil().DodajPredmet(predmet.id, null);

        Assert.AreEqual("Index", result.ActionName);
        Assert.IsNull(tutor.PredmetNaCekanjaId);
    }

    [TestMethod]
    public async Task DodajPredmet_NeispravanTipPriloga_OdbijaZahtjev()
    {
        var tutor = _fx.AddUser("tutor-bad-subject-file", Uloga.Tutor);
        _fx.LoginAs(tutor);
        var predmet = AddPredmet();

        var result = (RedirectToActionResult)await Profil().DodajPredmet(
            predmet.id, CreateFile("prilog.exe", "application/octet-stream"));

        Assert.AreEqual("Index", result.ActionName);
        Assert.IsNull(tutor.PredmetNaCekanjaId);
    }

    [TestMethod]
    public async Task DodajPredmet_PrilogVeciOd5MB_OdbijaZahtjev()
    {
        var tutor = _fx.AddUser("tutor-large-subject-file", Uloga.Tutor);
        _fx.LoginAs(tutor);
        var predmet = AddPredmet();

        var result = (RedirectToActionResult)await Profil().DodajPredmet(
            predmet.id, CreateFile("prilog.pdf", "application/pdf", 5 * 1024 * 1024 + 1));

        Assert.AreEqual("Index", result.ActionName);
        Assert.IsNull(tutor.PredmetNaCekanjaId);
    }

    [TestMethod]
    public async Task DodajPredmet_ValidanZahtjev_PostavljaPredmetNaCekanju()
    {
        var tutor = _fx.AddUser("tutor-add-subject", Uloga.Tutor);
        _fx.LoginAs(tutor);
        var predmet = AddPredmet("Fizika");

        var result = (RedirectToActionResult)await Profil().DodajPredmet(predmet.id, null);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(predmet.id, tutor.PredmetNaCekanjaId);
        Assert.IsFalse(tutor.VerifikovanTutor);
        _fx.Users.Verify(x => x.UpdateAsync(tutor), Times.Once);
    }

    [TestMethod]
    public async Task DodajPredmet_ValidanPDF_PohranjujePrilog()
    {
        var tutor = _fx.AddUser("tutor-subject-pdf", Uloga.Tutor);
        _fx.LoginAs(tutor);
        var predmet = AddPredmet();

        await Profil().DodajPredmet(
            predmet.id, CreateFile("dokaz.pdf", "application/pdf"));

        Assert.IsNotNull(tutor.PrilogOcjene);
        Assert.IsTrue(File.Exists(Path.Combine(
            _webRootPath, tutor.PrilogOcjene!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar))));
    }

    // OBRISI PREDMET

    [TestMethod]
    public async Task ObrisiPredmet_NeprijavljenKorisnik_PreusmjeravaNaIndex()
    {
        _fx.LoginAs(null);

        var result = (RedirectToActionResult)await Profil().ObrisiPredmet(1);

        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    public async Task ObrisiPredmet_KorisnikNijeTutor_PreusmjeravaNaIndex()
    {
        var student = _fx.AddUser("student-delete-subject");
        _fx.LoginAs(student);

        var result = (RedirectToActionResult)await Profil().ObrisiPredmet(1);

        Assert.AreEqual("Index", result.ActionName);
    }

    [TestMethod]
    public async Task ObrisiPredmet_PostojeciPredmet_UklanjaVezu()
    {
        var tutor = _fx.AddUser("tutor-delete-subject", Uloga.Tutor);
        _fx.LoginAs(tutor);
        var predmet = AddPredmet();
        AddKorisnikPredmet(tutor.Id, predmet.id);

        var result = (RedirectToActionResult)await Profil().ObrisiPredmet(predmet.id);

        Assert.AreEqual("Index", result.ActionName);
        Assert.AreEqual(0, _fx.Db.KorisnikPredmet.Count());
    }

    [TestMethod]
    public async Task ObrisiPredmet_PredmetNePripadaTutoru_NeUklanjaTuđuVezu()
    {
        var tutor = _fx.AddUser("tutor-delete-other", Uloga.Tutor);
        var otherTutor = _fx.AddUser("other-tutor-delete", Uloga.Tutor);
        _fx.LoginAs(tutor);
        var predmet = AddPredmet();
        AddKorisnikPredmet(otherTutor.Id, predmet.id);

        await Profil().ObrisiPredmet(predmet.id);

        Assert.AreEqual(1, _fx.Db.KorisnikPredmet.Count());
        Assert.AreEqual(otherTutor.Id, _fx.Db.KorisnikPredmet.Single().idKorisnika);
    }
}
