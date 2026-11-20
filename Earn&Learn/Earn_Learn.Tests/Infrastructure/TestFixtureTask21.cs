using System.Security.Claims;
using Earn_Learn.Controllers;
using Earn_Learn.Data;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Task21Tests.Infrastructure;

/// <summary>Svaki test dobija vlastitu izolovanu EF InMemory bazu i Moq Identity servis.</summary>
internal sealed class TestFixtureTask21 : IDisposable
{
    public ApplicationDbContext Db { get; }
    public Mock<UserManager<Korisnik>> Users { get; }
    public DefaultHttpContext Http { get; } = new();
    public Korisnik? CurrentUser { get; private set; }

    public TestFixtureTask21()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"task21-{Guid.NewGuid():N}").Options;
        Db = new ApplicationDbContext(options);
        Db.Database.EnsureCreated();

        var store = new Mock<IUserStore<Korisnik>>();
        Users = new Mock<UserManager<Korisnik>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        Users.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .Returns(() => Task.FromResult(CurrentUser));
        Users.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns(() => CurrentUser?.Id);
        Users.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .Returns((string id) => Task.FromResult(Db.Users.Local.FirstOrDefault(u => u.Id == id)));
        Users.Setup(x => x.UpdateAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityResult.Success);
        Users.Setup(x => x.DeleteAsync(It.IsAny<Korisnik>())).ReturnsAsync(IdentityResult.Success);
        Users.Setup(x => x.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

        Http.Request.Scheme = "https";
        Http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "test") }, "test"));
    }

    public Korisnik AddUser(string id, Uloga role = Uloga.Student, double balance = 0,
        double? hourPrice = null, bool verified = true)
    {
        var user = new Korisnik
        {
            Id = id, UserName = id + "@test.local", Email = id + "@test.local",
            Ime = "Test", Prezime = "Korisnik", Uloga = role,
            StanjeRacuna = balance, CijenaPoSatu = hourPrice, VerifikovanTutor = verified
        };
        Db.Users.Add(user);
        Db.SaveChanges();
        return user;
    }

    public void LoginAs(Korisnik? user) => CurrentUser = user;

    public Termin AddTermin(string tutorId, StatusTermina status = StatusTermina.Slobodan,
        string studentId = "", double price = 20, bool future = true)
    {
        var termin = new Termin
        {
            idTutora = tutorId, idStudenta = studentId, status = status,
            datumIVrijeme = future ? DateTime.Now.AddDays(3) : DateTime.Now.AddDays(-3),
            cijena = price, tipInstrukcija = TipInstrukcija.Hibridno
        };
        Db.Termin.Add(termin);
        Db.SaveChanges();
        return termin;
    }

    public T Controller<T>(T controller) where T : Controller
    {
        controller.ControllerContext = new ControllerContext { HttpContext = Http };
        var provider = new Mock<ITempDataProvider>();
        provider.Setup(p => p.LoadTempData(It.IsAny<HttpContext>()))
            .Returns(new Dictionary<string, object>());
        controller.TempData = new TempDataDictionary(Http, provider.Object);
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://example.test/attendance");
        controller.Url = url.Object;
        return controller;
    }

    public RezervacijaController Rezervacija() => Controller(new RezervacijaController(Users.Object, Db));
    public RecenzijasController Recenzije() => Controller(new RecenzijasController(Db));
    public TutorController Tutor() => Controller(new TutorController(Users.Object, Db));

    public void Dispose() { Db.Database.EnsureDeleted(); Db.Dispose(); }
}
