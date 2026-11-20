using Task21Tests.Infrastructure;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Task21Tests.Controllers;

[TestClass]
public sealed class RecenzijasControllerTests
{
    private TestFixtureTask21 _fx = null!;
    [TestInitialize] public void Init() => _fx = new TestFixtureTask21();
    [TestCleanup] public void Cleanup() => _fx.Dispose();

    private Recenzija Add(int rating = 4)
    {
        var r = new Recenzija { idStudenta = "student", idTutora = "tutor", ocjena = rating, komentar = "Test" };
        _fx.Db.Recenzija.Add(r); _fx.Db.SaveChanges(); return r;
    }

    [TestMethod]
    public async Task Index_VracaSveRecenzije()
    {
        Add(); Add(5);
        var result = (ViewResult)await _fx.Recenzije().Index();
        Assert.AreEqual(2, ((IEnumerable<Recenzija>)result.Model!).Count());
    }

    [TestMethod]
    public async Task Index_BezRecenzija_VracaPraznuListu()
    {
        var result = (ViewResult)await _fx.Recenzije().Index();
        Assert.AreEqual(0, ((IEnumerable<Recenzija>)result.Model!).Count());
    }

    [TestMethod] public async Task Details_NullId_NotFound() =>
        Assert.IsInstanceOfType(await _fx.Recenzije().Details(null), typeof(NotFoundResult));

    [TestMethod] public async Task Details_NepostojeciId_NotFound() =>
        Assert.IsInstanceOfType(await _fx.Recenzije().Details(123456), typeof(NotFoundResult));

    [TestMethod]
    public async Task Details_PostojecaRecenzija_VracaModel()
    {
        var r = Add();
        var result = (ViewResult)await _fx.Recenzije().Details(r.id);
        Assert.AreEqual(r.id, ((Recenzija)result.Model!).id);
    }

    [TestMethod]
    public void Create_Get_VracaFormu() =>
        Assert.IsInstanceOfType(_fx.Recenzije().Create(), typeof(ViewResult));

    [TestMethod]
    public async Task Create_Post_ValidnaRecenzija_SpremaIPreusmjerava()
    {
        var r = new Recenzija { idStudenta = "s", idTutora = "t", ocjena = 5, komentar = "Odlicno" };
        var result = await _fx.Recenzije().Create(r);
        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual(1, _fx.Db.Recenzija.Count());
    }

    [TestMethod]
    public async Task Create_Post_NevalidanModelState_VracaFormu()
    {
        var controller = _fx.Recenzije();
        controller.ModelState.AddModelError("ocjena", "Nije validno");
        Assert.IsInstanceOfType(await controller.Create(new Recenzija()), typeof(ViewResult));
        Assert.AreEqual(0, _fx.Db.Recenzija.Count());
    }

    [TestMethod] public async Task Edit_Get_NullId_NotFound() =>
        Assert.IsInstanceOfType(await _fx.Recenzije().Edit((int?)null), typeof(NotFoundResult));

    [TestMethod] public async Task Edit_Get_NepostojeciId_NotFound() =>
        Assert.IsInstanceOfType(await _fx.Recenzije().Edit((int?)5555), typeof(NotFoundResult));

    [TestMethod]
    public async Task Edit_Get_PostojeciId_VracaRecenziju()
    {
        var recenzija = Add();
        var result = (ViewResult)await _fx.Recenzije().Edit((int?)recenzija.id);
        Assert.AreEqual(recenzija.id, ((Recenzija)result.Model!).id);
    }

    [TestMethod]
    public async Task Edit_Post_NepodudaranId_NotFound()
    {
        Assert.IsInstanceOfType(await _fx.Recenzije().Edit(99, new Recenzija { id = 1 }), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_Post_NevalidanModelState_VracaView()
    {
        var recenzija = Add();
        var controller = _fx.Recenzije();
        controller.ModelState.AddModelError("komentar", "Obavezno");
        Assert.IsInstanceOfType(await controller.Edit(recenzija.id, recenzija), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Edit_Post_ValidnaIzmjena_SpremaIPreusmjerava()
    {
        var recenzija = Add();
        recenzija.ocjena = 3;
        var result = await _fx.Recenzije().Edit(recenzija.id, recenzija);
        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual(3, _fx.Db.Recenzija.Single().ocjena);
    }

    [TestMethod] public async Task Delete_Get_NullId_NotFound() =>
        Assert.IsInstanceOfType(await _fx.Recenzije().Delete(null), typeof(NotFoundResult));

    [TestMethod] public async Task Delete_Get_NepostojeciId_NotFound() =>
        Assert.IsInstanceOfType(await _fx.Recenzije().Delete(9999), typeof(NotFoundResult));

    [TestMethod]
    public async Task Delete_Get_PostojeciId_PreusmjeravaNaPristupOdbijen()
    {
        var r = Add();
        var result = (RedirectToActionResult)await _fx.Recenzije().Delete(r.id);
        Assert.AreEqual("PristupOdbijen", result.ActionName);
    }

    [TestMethod]
    public async Task DeleteConfirmed_Postojece_BriseRecenziju()
    {
        var r = Add();
        Assert.IsInstanceOfType(await _fx.Recenzije().DeleteConfirmed(r.id), typeof(RedirectToActionResult));
        Assert.AreEqual(0, _fx.Db.Recenzija.Count());
    }

    [TestMethod]
    public async Task DeleteConfirmed_Nepostojece_NeBacaIzuzetak()
    {
        Assert.IsInstanceOfType(await _fx.Recenzije().DeleteConfirmed(123), typeof(RedirectToActionResult));
    }


[TestMethod]
public async Task Edit_Post_ObrisanaRecenzija_VracaNotFound()
{
    var recenzija = new Recenzija
    {
        id = 98765,
        idStudenta = "student",
        idTutora = "tutor",
        ocjena = 4,
        komentar = "Test"
    };

    var result = await _fx.Recenzije().Edit(
        recenzija.id, recenzija);

    Assert.IsInstanceOfType(
        result, typeof(NotFoundResult));
}

}
