using Earn_Learn.Enums;
using Earn_Learn.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Task21Tests.Models;

[TestClass]
public sealed class ModelTests
{
    [TestMethod]
    public void Prijava_PodrazumijevaneVrijednosti_Ispravne()
    {
        var p = new Prijava();
        Assert.AreEqual(StatusPrijave.Prijavljeno, p.Status);
        Assert.AreEqual(string.Empty, p.Naslov);
        Assert.AreEqual(string.Empty, p.Opis);
        Assert.IsTrue(p.DatumPrijave <= DateTime.UtcNow);
    }

    [TestMethod]
    public void Prijava_SvaSvojstva_MoguSePostaviti()
    {
        var p = new Prijava { Id = 9, Naslov = "Prijava", Opis = "Opis", IdPrijavitelja="s",
            PrijavljeniTutorId="t", RecenzijaId=12, Status=StatusPrijave.NaCekanju };
        Assert.AreEqual(9, p.Id);
        Assert.AreEqual("Prijava", p.Naslov);
        Assert.AreEqual("Opis", p.Opis);
        Assert.AreEqual("s", p.IdPrijavitelja);
        Assert.AreEqual("t", p.PrijavljeniTutorId);
        Assert.AreEqual(12, p.RecenzijaId);
        Assert.AreEqual(StatusPrijave.NaCekanju, p.Status);
    }

    [TestMethod]
    public void Recenzija_PodaciMoguSePostaviti()
    {
        var r = new Recenzija { id=2, idStudenta="student", idTutora="tutor", ocjena=5, komentar="Odlicno" };
        Assert.AreEqual(2, r.id);
        Assert.AreEqual("student", r.idStudenta);
        Assert.AreEqual("tutor", r.idTutora);
        Assert.AreEqual(5, r.ocjena);
        Assert.AreEqual("Odlicno", r.komentar);
        Assert.IsTrue(r.datumRecenzije <= DateTime.UtcNow);
    }

    [TestMethod]
    public void KorisnikPredmet_ZadatiIdentifikatori_Ispravni()
    {
        var model = new KorisnikPredmet { idKorisnika="tutor", idPredmeta=12 };
        Assert.AreEqual("tutor", model.idKorisnika);
        Assert.AreEqual(12, model.idPredmeta);
    }

    [TestMethod]
    public void Predmet_IdINaziv_Ispravni()
    {
        var p = new Predmet { id=77, naziv="VVS" };
        Assert.AreEqual(77, p.id);
        Assert.AreEqual("VVS", p.naziv);
    }

    [TestMethod]
    public void TutorDashboardViewModel_PodrazumijevaniParametri_Ispravni()
    {
        var m = new TutorDashboardViewModel();
        Assert.AreEqual(string.Empty, m.Ime);
        Assert.AreEqual(0, m.BrojCasova);
        Assert.AreEqual(0d, m.Balans);
        Assert.AreEqual(0d, m.ProsjecnaOcjena);
        Assert.IsNotNull(m.NadolazaciTermini);
        Assert.AreEqual(0, m.NadolazaciTermini.Count);
    }

    [TestMethod]
    public void TutorDashboardViewModel_SvojstvaMoguSePostaviti()
    {
        var m = new TutorDashboardViewModel { Ime="Tutor", BrojCasova=5, Balans=120.5, ProsjecnaOcjena=4.5 };
        m.NadolazaciTermini.Add(new TerminDetaljiViewModel { Termin=new Termin { id=1 } });
        Assert.AreEqual("Tutor", m.Ime);
        Assert.AreEqual(5, m.BrojCasova);
        Assert.AreEqual(120.5, m.Balans);
        Assert.AreEqual(4.5, m.ProsjecnaOcjena);
        Assert.AreEqual(1, m.NadolazaciTermini.Count);
    }

    [TestMethod]
    public void Sesija_KreiranjePraznogModela_Uspjesno()
    {
        var sesija = new Sesija();
        Assert.IsNotNull(sesija);
    }
}
