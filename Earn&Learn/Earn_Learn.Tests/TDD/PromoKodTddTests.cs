using Microsoft.VisualStudio.TestTools.UnitTesting;
using Earn_Learn.Services;
using System;

namespace Earn_Learn.Tests
{
    [TestClass]
    public class PromoKodTddTests
    {
        private PromoKodService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new PromoKodService();
        }

        [TestMethod]
        public void PrimijeniPopust_ValidanProcentualniKod_UmanjujeCijenu()
        {
            // Arrange
            decimal osnovnaCijena = 50m;
            string kod = "STUDENT20";
            DateTime datumIsteka = DateTime.UtcNow.AddDays(5);

            // Act
            decimal novaCijena = _service.IzracunajCijenu(osnovnaCijena, kod, datumIsteka);

            // Assert
            Assert.AreEqual(40m, novaCijena);
        }

        [TestMethod]
        public void PrimijeniPopust_IstekaoKod_BacaArgumentException()
        {
            // Arrange
            decimal osnovnaCijena = 50m;
            string kod = "STUDENT20";
            DateTime datumIsteka = DateTime.UtcNow.AddDays(-1);

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _service.IzracunajCijenu(osnovnaCijena, kod, datumIsteka));
        }

        [TestMethod]
        public void PrimijeniPopust_NepoznatKod_BacaArgumentException()
        {
            // Arrange
            decimal osnovnaCijena = 50m;
            string kod = "NEPOSTOJECI";
            DateTime datumIsteka = DateTime.UtcNow.AddDays(5);

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                _service.IzracunajCijenu(osnovnaCijena, kod, datumIsteka));
        }

        [TestMethod]
public void PrimijeniPopust_PrazanKod_BacaArgumentException()
{
    // Arrange
    decimal osnovnaCijena = 50m;
    DateTime datumIsteka = DateTime.UtcNow.AddDays(5);

    // Act & Assert
    Assert.ThrowsException<ArgumentException>(() =>
        _service.IzracunajCijenu(osnovnaCijena, "", datumIsteka));

    Assert.ThrowsException<ArgumentException>(() =>
        _service.IzracunajCijenu(osnovnaCijena, "   ", datumIsteka));
}

[TestMethod]
public void PrimijeniPopust_NullKod_BacaArgumentException()
{
    // Arrange
    decimal osnovnaCijena = 50m;
    DateTime datumIsteka = DateTime.UtcNow.AddDays(5);

    // Act & Assert
    Assert.ThrowsException<ArgumentException>(() =>
        _service.IzracunajCijenu(osnovnaCijena, null!, datumIsteka));
}

[TestMethod]
public void PrimijeniPopust_MalaSlovaKoda_PrepoznajeKodIUmanjujeCijenu()
{
    // Arrange
    decimal osnovnaCijena = 50m;
    string kod = "student20";
    DateTime datumIsteka = DateTime.UtcNow.AddDays(5);

    // Act
    decimal novaCijena = _service.IzracunajCijenu(
        osnovnaCijena, kod, datumIsteka);

    // Assert
    Assert.AreEqual(40m, novaCijena);
}

[TestMethod]
public void PrimijeniPopust_KodSa100PostoPopusta_VracaNulaBezNegativneCijene()
{
    // Arrange
    decimal osnovnaCijena = 50m;
    string kod = "BESPLATNO100";
    DateTime datumIsteka = DateTime.UtcNow.AddDays(5);

    // Act
    decimal novaCijena = _service.IzracunajCijenu(
        osnovnaCijena, kod, datumIsteka);

    // Assert
    Assert.AreEqual(0m, novaCijena);
}
    }
}