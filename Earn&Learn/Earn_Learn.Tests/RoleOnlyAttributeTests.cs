using Earn_Learn.Enums;
using Earn_Learn.Models;
using Earn_Learn.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace Earn_Learn.Tests;

[TestClass]
public class RoleOnlyAttributeTests
{
    private Mock<UserManager<Korisnik>> _users = null!;

    [TestInitialize]
    public void Setup()
    {
        var store = new Mock<IUserStore<Korisnik>>();
        _users = new Mock<UserManager<Korisnik>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private ActionExecutingContext CreateContext(bool allowAnonymous = false)
    {
        var http = new DefaultHttpContext();
        http.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") }, "TestAuth"));
        var services = new ServiceCollection();
        services.AddSingleton(_users.Object);
        http.RequestServices = services.BuildServiceProvider();

        var descriptor = new ControllerActionDescriptor
        {
            EndpointMetadata = allowAnonymous
                ? new List<object> { new AllowAnonymousAttribute() }
                : new List<object>()
        };
        var actionContext = new ActionContext(http, new RouteData(), descriptor, new ModelStateDictionary());
        return new ActionExecutingContext(
            actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    }

    [TestMethod]
    public void OnActionExecuting_KorisnikImaDozvoljenuUlogu_NastavljaAkciju()
    {
        var user = new Korisnik { Uloga = Uloga.Student };
        _users.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        var context = CreateContext();

        new RoleOnlyAttribute(Uloga.Student).OnActionExecuting(context);

        Assert.IsNull(context.Result);
    }

    [TestMethod]
    public void OnActionExecuting_KorisnikNemaDozvoljenuUlogu_Vraca403View()
    {
        var user = new Korisnik { Uloga = Uloga.Tutor };
        _users.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        var context = CreateContext();

        new RoleOnlyAttribute(Uloga.Student).OnActionExecuting(context);

        Assert.IsInstanceOfType(context.Result, typeof(ViewResult));
        var result = (ViewResult)context.Result!;
        Assert.AreEqual("PristupOdbijen", result.ViewName);
        Assert.AreEqual(403, result.StatusCode);
    }

    [TestMethod]
    public void OnActionExecuting_AnonimnaAkcija_PreskaceProvjeruUloge()
    {
        var context = CreateContext(allowAnonymous: true);

        new RoleOnlyAttribute(Uloga.Student).OnActionExecuting(context);

        Assert.IsNull(context.Result);
        _users.Verify(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
    }
}
