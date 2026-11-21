using Earn_Learn.Controllers;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Mvc;
using Task21Tests.Infrastructure;

namespace Earn_Learn.Tests;

[TestClass]
public class HomeControllerTests
{
    private TestFixtureTask21 _fx = null!;

    [TestInitialize]
    public void Setup() => _fx = new TestFixtureTask21();

    [TestCleanup]
    public void Cleanup() => _fx.Dispose();

    private HomeController CreateController() =>
        _fx.Controller(new HomeController(_fx.Users.Object));

    [TestMethod]
    public async Task Index_NeprijavljenKorisnik_VracaPocetniView()
    {
        _fx.LoginAs(null);
        _fx.Http.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity());
        var result = await CreateController().Index();
        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task Index_PrijavljenStudent_PreusmjeravaNaDashboard()
    {
        var user = _fx.AddUser("prijavljen-student", Uloga.Student);
        _fx.LoginAs(user);

        var result = await CreateController().Index();

        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Dashboard", redirect.ActionName);
        Assert.AreEqual("Student", redirect.ControllerName);
    }

    [TestMethod]
    public async Task RedirectDashboard_NeautentificiranKorisnik_PreusmjeravaNaIndex()
    {
        var controller = CreateController();
        controller.ControllerContext.HttpContext.User =
            new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity());

        var result = await controller.RedirectDashboard();

        Assert.AreEqual("Index", ((RedirectToActionResult)result).ActionName);
    }

    [DataTestMethod]
    [DataRow(Uloga.Student, "Student")]
    [DataRow(Uloga.Tutor, "Tutor")]
    [DataRow(Uloga.SystemManager, "SystemManager")]
    public async Task RedirectDashboard_PrijavljenKorisnik_PreusmjeravaPremaUlozi(
        Uloga uloga, string controller)
    {
        var user = _fx.AddUser("korisnik-" + uloga, uloga);
        _fx.LoginAs(user);

        var result = await CreateController().RedirectDashboard();

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        var redirect = (RedirectToActionResult)result;
        Assert.AreEqual("Dashboard", redirect.ActionName);
        Assert.AreEqual(controller, redirect.ControllerName);
    }

    [TestMethod]
    public async Task RedirectDashboard_NepostojeciKorisnik_PreusmjeravaNaIndex()
    {
        _fx.LoginAs(null);
        var controller = CreateController();
        controller.ControllerContext.HttpContext.User =
            new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity("TestAuth"));

        var result = await controller.RedirectDashboard();

        Assert.AreEqual("Index", ((RedirectToActionResult)result).ActionName);
    }

    [TestMethod]
    public void Contact_Post_NedostajePolje_PostavljaPorukuGreske()
    {
        var controller = CreateController();
        var result = controller.Contact("Ime", "", "Poruka");

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.AreEqual("Sva polja su obavezna.", controller.ViewBag.Greska);
    }

    [TestMethod]
    public void Contact_Post_SvaPoljaPopunjena_PostavljaPorukuUspjeha()
    {
        var controller = CreateController();
        var result = controller.Contact("Ime", "ime@test.ba", "Poruka");

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.AreEqual("Vaša poruka je uspješno poslana! Javit ćemo vam se uskoro.", controller.ViewBag.Uspjeh);
    }

    [TestMethod]
    public void About_VracaView()
    {
        Assert.IsInstanceOfType(CreateController().About(), typeof(ViewResult));
    }

    [TestMethod]
    public void Privacy_VracaView()
    {
        Assert.IsInstanceOfType(CreateController().Privacy(), typeof(ViewResult));
    }

    [TestMethod]
    public void Terms_VracaView()
    {
        Assert.IsInstanceOfType(CreateController().Terms(), typeof(ViewResult));
    }

    [TestMethod]
    public void Contact_Get_VracaView()
    {
        Assert.IsInstanceOfType(CreateController().Contact(), typeof(ViewResult));
    }

    [TestMethod]
    public void PristupOdbijen_VracaView()
    {
        Assert.IsInstanceOfType(CreateController().PristupOdbijen(), typeof(ViewResult));
    }

    [TestMethod]
    public void Error_VracaModelSaTraceIdentifierom()
    {
        var controller = CreateController();
        controller.ControllerContext.HttpContext.TraceIdentifier = "request-123";

        var result = (ViewResult)controller.Error();
        var model = (ErrorViewModel)result.Model!;

        Assert.AreEqual("request-123", model.RequestId);
        Assert.IsTrue(model.ShowRequestId);
    }

    [TestMethod]
    public void ErrorViewModel_PrazanRequestId_NePrikazujeRequestId()
    {
        var model = new ErrorViewModel { RequestId = "" };

        Assert.IsFalse(model.ShowRequestId);
    }
}
