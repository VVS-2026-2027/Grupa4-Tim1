using System;
using System.Collections.Generic;
using System.Text;


using Microsoft.VisualStudio.TestTools.UnitTesting;
using Earn_Learn.Services;

namespace Earn_Learn.Tests
{
    [TestClass]
    public class TutorRangTddTests
    {
        private TutorRangService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new TutorRangService();
        }

        // TUTOR ISPUNJAVA TACNO OBA USLOVA

        [TestMethod]
        public void DaLiJeIstaknutiTutor_Tacno10CasovaIOcjena45_VracaTrue()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(10, 4.5);

            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        public void DaLiJeIstaknutiTutor_9CasovaITacnoOcjena45_VracaFalse()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(9, 4.5);

            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public void DaLiJeIstaknutiTutor_Tacno10CasovaIOcjena4499_VracaFalse()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(10, 4.499);

            Assert.IsFalse(rezultat);
        }

        // TUTOR IMA VISE OD MINIMALNOG BROJA CASOVA
        // I VISU PROSJECNU OCJENU

        [TestMethod]
        public void DaLiJeIstaknutiTutor_ViseOd10CasovaIOcjenaVecaOd45_VracaTrue()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(15, 4.8);

            Assert.IsTrue(rezultat);
        }

        // TUTOR IMA MANJE OD 10 ODRZANIH CASOVA

        [TestMethod]
        public void DaLiJeIstaknutiTutor_ManjeOd10Casova_VracaFalse()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(9, 5.0);

            Assert.IsFalse(rezultat);
        }

        // TUTOR IMA OCJENU MANJU OD 4.5

        [TestMethod]
        public void DaLiJeIstaknutiTutor_OcjenaManjaOd45_VracaFalse()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(10, 4.4);

            Assert.IsFalse(rezultat);
        }

        // TUTOR NE ISPUNJAVA NIJEDAN USLOV

        [TestMethod]
        public void DaLiJeIstaknutiTutor_ManjeCasovaIManjaOcjena_VracaFalse()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(5, 3.5);

            Assert.IsFalse(rezultat);
        }

        // PROVJERA GUBITKA STATUSA ZBOG MANJEG BROJA CASOVA

        [TestMethod]
        public void DaLiJeIstaknutiTutor_NakonPadaBrojaCasovaIspod10_VracaFalse()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(9, 4.8);

            Assert.IsFalse(rezultat);
        }

        // PROVJERA GUBITKA STATUSA ZBOG PADA PROSJECNE OCJENE

        [TestMethod]
        public void DaLiJeIstaknutiTutor_NakonPadaOcjeneIspod45_VracaFalse()
        {
            bool rezultat = _service.DaLiJeIstaknutiTutor(12, 4.3);

            Assert.IsFalse(rezultat);
        }
    }
}
