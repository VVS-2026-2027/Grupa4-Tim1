using System.Globalization;
using Earn_Learn.Controllers;
using Earn_Learn.Data;
using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Earn_Learn.Tests
{
    [TestClass]
    public class TransakcijasControllerTests
    {
        private ApplicationDbContext _context = null!;
        private TransakcijasController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _context = TestHelper.CreateInMemoryContext();
            _controller = new TransakcijasController(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
        }

        public static IEnumerable<object[]> UcitajTransakcijeIzCsv()
        {
            var putanja = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "transakcije.csv");
            var linije = File.ReadAllLines(putanja).Skip(1);
            foreach (var linija in linije)
            {
                if (string.IsNullOrWhiteSpace(linija)) continue;
                var dijelovi = linija.Split(',');
                yield return new object[]
                {
                    dijelovi[0],
                    dijelovi[1],
                    double.Parse(dijelovi[2], CultureInfo.InvariantCulture),
                    Enum.Parse<NacinPlacanja>(dijelovi[3]),
                    Enum.Parse<StatusPlacanja>(dijelovi[4])
                };
            }
        }

        [TestMethod]
        public async Task Index_VracaViewSaListomTransakcija()
        {
            _context.Transakcija.Add(new Transakcija { id = 1, idStudenta = "s1", idTutora = "t1", iznos = 30, datumUplate = DateTime.UtcNow });
            await _context.SaveChangesAsync();

            var result = await _controller.Index() as ViewResult;
            var model = result?.Model as List<Transakcija>;

            Assert.IsNotNull(result);
            Assert.IsNotNull(model);
            Assert.AreEqual(1, model.Count);
        }

        [TestMethod]
        public async Task Details_NullId_VracaNotFound()
        {
            var result = await _controller.Details(null);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            var result = await _controller.Details(999);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_ValidanId_VracaViewSaTransakcijom()
        {
            _context.Transakcija.Add(new Transakcija { id = 5, idStudenta = "s1", idTutora = "t1", iznos = 45 });
            await _context.SaveChangesAsync();

            var result = await _controller.Details(5) as ViewResult;
            var model = result?.Model as Transakcija;

            Assert.IsNotNull(result);
            Assert.AreEqual(45, model?.iznos);
        }

        [TestMethod]
        public void Create_Get_VracaView()
        {
            var result = _controller.Create();
            Assert.IsInstanceOfType(result, typeof(ViewResult));
        }

        [DataTestMethod]
        [DynamicData(nameof(UcitajTransakcijeIzCsv), DynamicDataSourceType.Method)]
        public async Task Create_Post_DataDrivenCsv_DodajeTransakcijuIRedirecta(
            string idStudenta, string idTutora, double iznos, NacinPlacanja nacin, StatusPlacanja status)
        {
            var transakcija = new Transakcija
            {
                idStudenta = idStudenta,
                idTutora = idTutora,
                iznos = iznos,
                datumUplate = DateTime.UtcNow,
                nacinPlacanja = nacin,
                statusPlacanja = status
            };

            var result = await _controller.Create(transakcija) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(1, await _context.Transakcija.CountAsync());
        }

        [TestMethod]
        public async Task Create_Post_NevalidanModelState_VracaView()
        {
            _controller.ModelState.AddModelError("iznos", "Greška");
            var t = new Transakcija { id = 1, iznos = -5 };

            var result = await _controller.Create(t) as ViewResult;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Edit_Get_NullINepostojeciId_VracaNotFound()
        {
            Assert.IsInstanceOfType(await _controller.Edit(null), typeof(NotFoundResult));
            Assert.IsInstanceOfType(await _controller.Edit(999), typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Get_ValidanId_VracaView()
        {
            _context.Transakcija.Add(new Transakcija { id = 2, iznos = 20 });
            await _context.SaveChangesAsync();

            var result = await _controller.Edit(2) as ViewResult;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Edit_Post_RazlicitId_VracaNotFound()
        {
            var result = await _controller.Edit(1, new Transakcija { id = 2 });
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Post_ValidanModel_AzuriraIRedirecta()
        {
            var t = new Transakcija { id = 3, idStudenta = "s1", idTutora = "t1", iznos = 50 };
            _context.Transakcija.Add(t);
            await _context.SaveChangesAsync();
            _context.Entry(t).State = EntityState.Detached;

            t.iznos = 75;
            var result = await _controller.Edit(3, t) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(75, (await _context.Transakcija.FindAsync(3))?.iznos);
        }

        [TestMethod]
        public async Task Edit_Post_NepostojeciEntitetConcurrency_VracaNotFound()
        {
            var nepostojeca = new Transakcija { id = 999, iznos = 10 };
            var result = await _controller.Edit(999, nepostojeca);
            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModelState_VracaView()
        {
            _controller.ModelState.AddModelError("iznos", "Greška");
            var t = new Transakcija { id = 3, iznos = 50 };

            var result = await _controller.Edit(3, t) as ViewResult;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task Delete_Get_NullNepostojeciIValidanId()
        {
            Assert.IsInstanceOfType(await _controller.Delete(null), typeof(NotFoundResult));
            Assert.IsInstanceOfType(await _controller.Delete(888), typeof(NotFoundResult));

            _context.Transakcija.Add(new Transakcija { id = 10, iznos = 40 });
            await _context.SaveChangesAsync();

            var result = await _controller.Delete(10) as ViewResult;
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task DeleteConfirmed_BriseTransakcijuIRedirecta()
        {
            _context.Transakcija.Add(new Transakcija { id = 11, iznos = 40 });
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteConfirmed(11) as RedirectToActionResult;

            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.ActionName);
            Assert.AreEqual(0, await _context.Transakcija.CountAsync());
        }
    }
}