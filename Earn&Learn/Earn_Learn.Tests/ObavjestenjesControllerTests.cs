using System;
using System.Linq;
using System.Threading.Tasks;
using Earn_Learn.Controllers;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Task21Tests.Infrastructure;

namespace Task21Tests.Controllers;

[TestClass]
public sealed class ObavjestenjesControllerTests
{
    private TestFixtureTask21 _fx = null!;

    [TestInitialize]
    public void Init() => _fx = new TestFixtureTask21();

    [TestCleanup]
    public void Cleanup() => _fx.Dispose();

    private ObavjestenjesController Controller() =>
        _fx.Controller(new ObavjestenjesController(_fx.Db));

    private Obavjestenje Add(string naslov = "Test obavještenje")
    {
        var obavjestenje = new Obavjestenje
        {
            idKorisnika = "korisnik-test",
            naslov = naslov,
            sadrzaj = "Sadržaj testnog obavještenja",
            datumSlanja = DateTime.UtcNow,
            procitano = false
        };

        _fx.Db.Obavjestenje.Add(obavjestenje);
        _fx.Db.SaveChanges();
        return obavjestenje;
    }



    [TestMethod]
    public async Task Index_VracaSvaObavjestenja()
    {
        Add("Prvo");
        Add("Drugo");

        var result = (ViewResult)await Controller().Index();

        Assert.AreEqual(2, ((System.Collections.Generic.IEnumerable<Obavjestenje>)result.Model!).Count());
    }

    [TestMethod]
    public async Task Index_BezObavjestenja_VracaPraznuListu()
    {
        var result = (ViewResult)await Controller().Index();

        Assert.AreEqual(0, ((System.Collections.Generic.IEnumerable<Obavjestenje>)result.Model!).Count());
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
        var result = await Controller().Details(9999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Details_PostojeciId_VracaObavjestenje()
    {
        var obavjestenje = Add();

        var result = (ViewResult)await Controller().Details(obavjestenje.id);

        Assert.AreEqual(obavjestenje.id, ((Obavjestenje)result.Model!).id);
    }



    [TestMethod]
    public void Create_Get_VracaFormu()
    {
        var result = Controller().Create();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task Create_Post_ValidnoObavjestenje_SpremaIPreusmjerava()
    {
        var obavjestenje = new Obavjestenje
        {
            idKorisnika = "korisnik-1",
            naslov = "Nova poruka",
            sadrzaj = "Tekst poruke",
            datumSlanja = DateTime.UtcNow,
            procitano = false
        };

        var result = await Controller().Create(obavjestenje);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual(1, _fx.Db.Obavjestenje.Count());
    }

    [TestMethod]
    public async Task Create_Post_NevalidanModelState_VracaFormu()
    {
        var controller = Controller();
        controller.ModelState.AddModelError("naslov", "Naslov je obavezan.");

        var result = await controller.Create(new Obavjestenje());

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.AreEqual(0, _fx.Db.Obavjestenje.Count());
    }

   

    [TestMethod]
    public async Task Edit_Get_NullId_NotFound()
    {
        var result = await Controller().Edit(null);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_Get_NepostojeciId_NotFound()
    {
        var result = await Controller().Edit(9999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_Get_PostojeciId_VracaObavjestenje()
    {
        var obavjestenje = Add();

        var result = (ViewResult)await Controller().Edit(obavjestenje.id);

        Assert.AreEqual(obavjestenje.id, ((Obavjestenje)result.Model!).id);
    }

    [TestMethod]
    public async Task Edit_Post_NepodudaranId_NotFound()
    {
        var obavjestenje = new Obavjestenje { id = 1 };

        var result = await Controller().Edit(2, obavjestenje);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_Post_NevalidanModelState_VracaView()
    {
        var obavjestenje = Add();
        var controller = Controller();
        controller.ModelState.AddModelError("naslov", "Naslov nije validan.");

        var result = await controller.Edit(obavjestenje.id, obavjestenje);

        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task Edit_Post_ValidnaIzmjena_SpremaIPreusmjerava()
    {
        var obavjestenje = Add();
        obavjestenje.naslov = "Izmijenjen naslov";

        var result = await Controller().Edit(obavjestenje.id, obavjestenje);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual("Izmijenjen naslov", _fx.Db.Obavjestenje.Single().naslov);
    }

    // DELETE

    [TestMethod]
    public async Task Delete_Get_NullId_NotFound()
    {
        var result = await Controller().Delete(null);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Delete_Get_NepostojeciId_NotFound()
    {
        var result = await Controller().Delete(9999);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Delete_Get_PostojeciId_VracaObavjestenje()
    {
        var obavjestenje = Add();

        var result = (ViewResult)await Controller().Delete(obavjestenje.id);

        Assert.AreEqual(obavjestenje.id, ((Obavjestenje)result.Model!).id);
    }

    [TestMethod]
    public async Task DeleteConfirmed_PostojeciId_BriseObavjestenje()
    {
        var obavjestenje = Add();

        var result = await Controller().DeleteConfirmed(obavjestenje.id);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual(0, _fx.Db.Obavjestenje.Count());
    }

    [TestMethod]
    public async Task DeleteConfirmed_NepostojeciId_NeBacaIzuzetak()
    {
        var result = await Controller().DeleteConfirmed(9999);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual(0, _fx.Db.Obavjestenje.Count());
    }
}
