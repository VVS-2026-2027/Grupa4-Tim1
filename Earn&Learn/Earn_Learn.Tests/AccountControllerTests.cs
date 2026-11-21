using Earn_Learn.Controllers;
using Earn_Learn.Data;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Earn_Learn.Tests;

[TestClass]
public class AccountControllerTests
{
    private ApplicationDbContext _db = null!;
    private Mock<UserManager<Korisnik>> _users = null!;
    private Mock<SignInManager<Korisnik>> _signIn = null!;
    private Mock<IWebHostEnvironment> _env = null!;
    private AccountController _controller = null!;
    private string _webRootPath = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<Korisnik>>();
        _users = new Mock<UserManager<Korisnik>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _signIn = new Mock<SignInManager<Korisnik>>(
     _users.Object,
     new Mock<IHttpContextAccessor>().Object,
     new Mock<IUserClaimsPrincipalFactory<Korisnik>>().Object,
     null!,
     null!,
     null!,
     null!);

        _webRootPath = Path.Combine(AppContext.BaseDirectory, "test-webroot-" + Guid.NewGuid().ToString("N"));
        _env = new Mock<IWebHostEnvironment>();
        _env.SetupGet(x => x.WebRootPath).Returns(_webRootPath);

        _controller = new AccountController(
            _signIn.Object, _users.Object, _db, _env.Object);

        var http = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = http
        };

        var tempDataProvider = new Mock<ITempDataProvider>();
        tempDataProvider
            .Setup(x => x.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(new Dictionary<string, object>());
        _controller.TempData = new TempDataDictionary(http, tempDataProvider.Object);
    }

    [TestMethod]
    public async Task Login_PrazanEmail_PreusmjeravaNaPrijavuIGresku()
    {
        var result = await _controller.Login("", "lozinka", null);

        Assert.IsInstanceOfType(result, typeof(LocalRedirectResult));
        Assert.AreEqual("/Identity/Account/Login", ((LocalRedirectResult)result).Url);
        Assert.AreEqual("Unesite email i šifru.", _controller.TempData["LoginError"]);
    }

    [TestMethod]
    public async Task Login_PraznaLozinka_PreusmjeravaNaPrijavuIGresku()
    {
        var result = await _controller.Login("student@test.ba", "", null);

        Assert.AreEqual("/Identity/Account/Login", ((LocalRedirectResult)result).Url);
        Assert.AreEqual("Unesite email i šifru.", _controller.TempData["LoginError"]);
        _signIn.Verify(x => x.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Login_PogresnaLozinka_PreusmjeravaNaPrijavuIGresku()
    {
        _signIn
            .Setup(x => x.PasswordSignInAsync(
                "student@test.ba", "pogresna", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var result = await _controller.Login(
            "student@test.ba", "pogresna", null);

        Assert.AreEqual("/Identity/Account/Login", ((LocalRedirectResult)result).Url);
        Assert.AreEqual("Pogrešan email ili šifra.", _controller.TempData["LoginError"]);
    }

    [TestMethod]
    public async Task Login_UspjesnaPrijavaStudenta_PreusmjeravaNaDashboard()
    {
        _signIn
            .Setup(x => x.PasswordSignInAsync(
                "student@test.ba", "lozinka", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _users
            .Setup(x => x.FindByEmailAsync("student@test.ba"))
            .ReturnsAsync(new Korisnik { Uloga = Uloga.Student });

        var result = await _controller.Login(
            "student@test.ba", "lozinka", null);

        Assert.AreEqual("/Student/Dashboard", ((LocalRedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Login_UspjesnaPrijavaTutora_PreusmjeravaNaDashboard()
    {
        _signIn
            .Setup(x => x.PasswordSignInAsync(
                "tutor@test.ba", "lozinka", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _users
            .Setup(x => x.FindByEmailAsync("tutor@test.ba"))
            .ReturnsAsync(new Korisnik { Uloga = Uloga.Tutor });

        var result = await _controller.Login("tutor@test.ba", "lozinka", null);

        Assert.AreEqual("/Tutor/Dashboard", ((LocalRedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Login_UspjesnaPrijavaSystemManagera_PreusmjeravaNaDashboard()
    {
        _signIn
            .Setup(x => x.PasswordSignInAsync(
                "manager@test.ba", "lozinka", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _users
            .Setup(x => x.FindByEmailAsync("manager@test.ba"))
            .ReturnsAsync(new Korisnik { Uloga = Uloga.SystemManager });

        var result = await _controller.Login("manager@test.ba", "lozinka", null);

        Assert.AreEqual("/SystemManager/Dashboard", ((LocalRedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Login_KorisnikNePostoji_KoristiReturnUrl()
    {
        _signIn
            .Setup(x => x.PasswordSignInAsync(
                "nepoznat@test.ba", "lozinka", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _users
            .Setup(x => x.FindByEmailAsync("nepoznat@test.ba"))
            .ReturnsAsync((Korisnik?)null);

        var result = await _controller.Login("nepoznat@test.ba", "lozinka", "/Courses");

        Assert.AreEqual("/Courses", ((LocalRedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Login_KorisnikNePostojiIPrazanReturnUrl_PreusmjeravaNaPocetnu()
    {
        _signIn
            .Setup(x => x.PasswordSignInAsync(
                "nepoznat@test.ba", "lozinka", false, false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _users
            .Setup(x => x.FindByEmailAsync("nepoznat@test.ba"))
            .ReturnsAsync((Korisnik?)null);

        var result = await _controller.Login("nepoznat@test.ba", "lozinka", null);

        Assert.AreEqual("/", ((LocalRedirectResult)result).Url);
    }

    [TestMethod]
    public async Task Register_SystemManager_OdbijaRegistraciju()
    {
        var result = await _controller.Register(
            "Ime", "Prezime", "admin@test.ba", "lozinka", Uloga.SystemManager, null, null);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual("Register", ((RedirectToActionResult)result).ActionName);
        Assert.AreEqual("Nije moguće registrovati System Manager nalog.", _controller.TempData["RegError"]);
        _users.Verify(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Register_UspjesanStudent_KreiraNalogPrijaviIPreusmjeri()
    {
        _users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), "lozinka"))
            .ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.SignInAsync(It.IsAny<Korisnik>(), false))
            .Returns(Task.CompletedTask);

        var result = await _controller.Register(
            "Test", "Student", "student@test.ba", "lozinka", Uloga.Student, 123, null);

        Assert.AreEqual("/Student/Dashboard", ((LocalRedirectResult)result).Url);
        _users.Verify(x => x.CreateAsync(
            It.Is<Korisnik>(u => u.Email == "student@test.ba" && u.BrojIndeksa == 123), "lozinka"), Times.Once);
        _signIn.Verify(x => x.SignInAsync(It.IsAny<Korisnik>(), false), Times.Once);
    }

    [TestMethod]
    public async Task Register_NeuspioCreate_PostavljaGreskuIPonovoPrikazujeFormu()
    {
        _users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Neispravan nalog" }));

        var result = await _controller.Register(
            "Test", "Student", "student@test.ba", "lozinka", Uloga.Student, null, null);

        Assert.AreEqual("Register", ((RedirectToActionResult)result).ActionName);
        Assert.AreEqual("Neispravan nalog", _controller.TempData["RegError"]);
        _signIn.Verify(x => x.SignInAsync(It.IsAny<Korisnik>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task Register_TutorSaNedozvoljenimPrilogom_OdbijaPrilog()
    {
        var file = new FormFile(new MemoryStream(new byte[4]), 0, 4, "file", "doc.exe")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/octet-stream"
        };

        var result = await _controller.Register(
            "Test", "Tutor", "tutor@test.ba", "lozinka", Uloga.Tutor, null, file);

        Assert.AreEqual("Register", ((RedirectToActionResult)result).ActionName);
        Assert.AreEqual("Dozvoljeni formati su JPG, PNG i PDF.", _controller.TempData["RegError"]);
        _users.Verify(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Logout_OdjavljujeKorisnikaIPreusmjeravaNaPocetnu()
    {
        _signIn.Setup(x => x.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        Assert.AreEqual("/", ((LocalRedirectResult)result).Url);
        _signIn.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [TestMethod]
    public async Task Register_TutorSaPrevelikimPrilogom_OdbijaPrilog()
    {
        var bytes = new byte[5 * 1024 * 1024 + 1];
        var file = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "ocjene.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        var result = await _controller.Register(
            "Test", "Tutor", "tutor@test.ba", "lozinka", Uloga.Tutor, null, file);

        Assert.AreEqual("Register", ((RedirectToActionResult)result).ActionName);
        Assert.AreEqual("Prilog ne smije biti veći od 5MB.", _controller.TempData["RegError"]);
        _users.Verify(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Register_TutorBezPriloga_UspjesnoKreiraNalog()
    {
        _users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), "lozinka"))
            .ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.SignInAsync(It.IsAny<Korisnik>(), false))
            .Returns(Task.CompletedTask);

        var result = await _controller.Register(
            "Test", "Tutor", "tutor@test.ba", "lozinka", Uloga.Tutor, null, null);

        Assert.AreEqual("/Tutor/Dashboard", ((LocalRedirectResult)result).Url);
        _users.Verify(x => x.CreateAsync(
            It.Is<Korisnik>(u => u.Uloga == Uloga.Tutor && u.VerifikovanTutor == false), "lozinka"), Times.Once);
        _signIn.Verify(x => x.SignInAsync(It.IsAny<Korisnik>(), false), Times.Once);
    }

    [TestMethod]
    public async Task Register_UspjesanTutorSaValidnimPrilogom_SnimiPrilogIPutanjuKorisniku()
    {
        _users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), "lozinka"))
            .ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.SignInAsync(It.IsAny<Korisnik>(), false))
            .Returns(Task.CompletedTask);
        var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "ocjene.pdf")
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };

        var result = await _controller.Register(
            "Test", "Tutor", "tutor-file@test.ba", "lozinka", Uloga.Tutor, null, file);

        Assert.AreEqual("/Tutor/Dashboard", ((LocalRedirectResult)result).Url);
        var createdUser = _users.Invocations
            .Where(i => i.Method.Name == nameof(UserManager<Korisnik>.CreateAsync))
            .Select(i => i.Arguments[0])
            .OfType<Korisnik>()
            .Single();
        Assert.IsTrue(createdUser.PrilogOcjene!.StartsWith("/uploads/prilozi/"));
        var savedFile = Path.Combine(_webRootPath, createdUser.PrilogOcjene.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.IsTrue(File.Exists(savedFile));
    }

    [TestMethod]
    public async Task RegisterIdentity_UspjesanTutorSaPredmetima_SnimiPredmeteIPrijavi()
    {
        _users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), "lozinka"))
            .ReturnsAsync(IdentityResult.Success);
        _users.Setup(x => x.UpdateAsync(It.IsAny<Korisnik>()))
            .ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.SignInAsync(It.IsAny<Korisnik>(), false))
            .Returns(Task.CompletedTask);

        var result = await _controller.RegisterIdentity(
            "Test", "Tutor", "tutor-id@test.ba", "lozinka",
            true, new List<int> { 2, 5 }, null, null, null);

        Assert.AreEqual("/Tutor/Dashboard", ((LocalRedirectResult)result).Url);
        _users.Verify(x => x.UpdateAsync(It.Is<Korisnik>(u =>
            u.PredmetiNaCekanjuJson == "[2,5]" && u.Uloga == Uloga.Tutor)), Times.Once);
        _signIn.Verify(x => x.SignInAsync(It.IsAny<Korisnik>(), false), Times.Once);
    }

    [TestMethod]
    public async Task RegisterIdentity_CreateNijeUspio_PostavljaGresku()
    {
        _users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email zauzet" }));

        var result = await _controller.RegisterIdentity(
            "Test", "Korisnik", "zauzet@test.ba", "lozinka",
            false, null, null, null, null);

        Assert.AreEqual("/Identity/Account/Register", ((LocalRedirectResult)result).Url);
        Assert.AreEqual("Email zauzet", _controller.TempData["RegError"]);
        _signIn.Verify(x => x.SignInAsync(It.IsAny<Korisnik>(), It.IsAny<bool>()), Times.Never);
    }

    [TestMethod]
    public async Task RegisterIdentity_UlogaTutorIzForme_BiraTutorskuUlogu()
    {
        _users.Setup(x => x.CreateAsync(
                It.Is<Korisnik>(u => u.Uloga == Uloga.Tutor), "lozinka"))
            .ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.SignInAsync(It.IsAny<Korisnik>(), false))
            .Returns(Task.CompletedTask);

        var result = await _controller.RegisterIdentity(
            "Test", "Tutor", "tutor-form@test.ba", "lozinka",
            false, null, null, 1, null);

        Assert.AreEqual("/Tutor/Dashboard", ((LocalRedirectResult)result).Url);
        _users.Verify(x => x.CreateAsync(
            It.Is<Korisnik>(u => u.Uloga == Uloga.Tutor), "lozinka"), Times.Once);
    }

    [TestMethod]
    public async Task RegisterIdentity_TutorSaValidnimPrilogom_SnimiPrilog()
    {
        _users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), "lozinka"))
            .ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.SignInAsync(It.IsAny<Korisnik>(), false))
            .Returns(Task.CompletedTask);
        var file = new FormFile(new MemoryStream(new byte[] { 9, 8, 7 }), 0, 3, "file", "ocjene.jpg")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };

        var result = await _controller.RegisterIdentity(
            "Test", "Tutor", "identity-tutor@test.ba", "lozinka",
            true, null, file, null, null);

        Assert.AreEqual("/Tutor/Dashboard", ((LocalRedirectResult)result).Url);
        var createdUser = _users.Invocations
            .Where(i => i.Method.Name == nameof(UserManager<Korisnik>.CreateAsync))
            .Select(i => i.Arguments[0])
            .OfType<Korisnik>()
            .Single();
        Assert.IsTrue(createdUser.PrilogOcjene!.StartsWith("/uploads/prilozi/"));
        var savedFile = Path.Combine(_webRootPath, createdUser.PrilogOcjene.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.IsTrue(File.Exists(savedFile));
    }


    public static IEnumerable<object[]> RequiredFieldsFromCsv()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory, "TestData", "account_required_fields.csv");

        return File.ReadAllLines(path)
            .Skip(1)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Split(','))
            .Select(columns => new object[]
            {
            columns[0],
            columns[1],
            bool.Parse(columns[2])
            });
    }

    [DataTestMethod]
    [DynamicData(nameof(RequiredFieldsFromCsv), DynamicDataSourceType.Method)]
    public async Task RegisterIdentity_ObaveznaPolja_CsvProvjeravaValidnost(
        string email, string password, bool valid)
    {
        if (valid)
        {
            _users
                .Setup(x => x.CreateAsync(
                    It.IsAny<Korisnik>(), password))
                .ReturnsAsync(IdentityResult.Success);

            _signIn
                .Setup(x => x.SignInAsync(
                    It.IsAny<Korisnik>(), false))
                .Returns(Task.CompletedTask);
        }

        var result = await _controller.RegisterIdentity(
            "Test", "Korisnik", email, password,
            false, null, null, null, null);

        if (!valid)
        {
            Assert.AreEqual(
                "/Identity/Account/Register",
                ((LocalRedirectResult)result).Url);
            Assert.AreEqual("Unesite sve podatke.", _controller.TempData["RegError"]);
            _users.Verify(
                x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()),
                Times.Never);
        }
        else
        {
            Assert.AreEqual(
                "/Student/Dashboard",
                ((LocalRedirectResult)result).Url);
            _users.Verify(
                x => x.CreateAsync(It.IsAny<Korisnik>(), password),
                Times.Once);
        }
    }





    [TestCleanup]
    public void Cleanup()
    {
        _db.Dispose();
        if (Directory.Exists(_webRootPath))
            Directory.Delete(_webRootPath, true);
    }

}
