using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.IO;
using Microsoft.AspNetCore.Http;
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
        var p = new Prijava
        {
            Id = 9,
            Naslov = "Prijava",
            Opis = "Opis",
            IdPrijavitelja = "s",
            PrijavljeniTutorId = "t",
            RecenzijaId = 12,
            Status = StatusPrijave.NaCekanju
        };
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
        var r = new Recenzija
        {
            id = 2,
            idStudenta = "student",
            idTutora = "tutor",
            ocjena = 5,
            komentar = "Odlicno"
        };
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
        var model = new KorisnikPredmet
        {
            idKorisnika = "tutor",
            idPredmeta = 12
        };
        Assert.AreEqual("tutor", model.idKorisnika);
        Assert.AreEqual(12, model.idPredmeta);
    }
    [TestMethod]
    public void Predmet_IdINaziv_Ispravni()
    {
        var p = new Predmet { id = 77, naziv = "VVS" };
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
        var m = new TutorDashboardViewModel
        {
            Ime = "Tutor",
            BrojCasova = 5,
            Balans = 120.5,
            ProsjecnaOcjena = 4.5
        };
        m.NadolazaciTermini.Add(
            new TerminDetaljiViewModel { Termin = new Termin { id = 1 } });
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
    // TERMIN
    [TestMethod]
    public void Termin_PodrazumijevaneVrijednosti_Ispravne()
    {
        var termin = new Termin();
        Assert.AreEqual(0, termin.id);
        Assert.AreEqual(string.Empty, termin.idStudenta);
        Assert.AreEqual(string.Empty, termin.idTutora);
        Assert.IsNull(termin.idPredmeta);
        Assert.AreEqual(DateTime.MinValue, termin.datumIVrijeme);
        Assert.AreEqual(default(TipInstrukcija), termin.tipInstrukcija);
        Assert.AreEqual(default(StatusTermina), termin.status);
        Assert.IsNull(termin.qrKod);
        Assert.AreEqual(0d, termin.cijena);
        Assert.IsNull(termin.mjestoCasa);
        Assert.IsFalse(termin.prisustvoPotvrdjeno);
    }
    [TestMethod]
    public void Termin_SvaSvojstva_MoguSePostaviti()
    {
        var datum = new DateTime(2026, 11, 20, 14, 30, 0);
        var tip = (TipInstrukcija)1;
        var status = (StatusTermina)2;
        var termin = new Termin
        {
            id = 15,
            idStudenta = "student-1",
            idTutora = "tutor-1",
            idPredmeta = 7,
            datumIVrijeme = datum,
            tipInstrukcija = tip,
            status = status,
            qrKod = "QR-TEST-123",
            cijena = 25.50,
            mjestoCasa = "Online",
            prisustvoPotvrdjeno = true
        };
        Assert.AreEqual(15, termin.id);
        Assert.AreEqual("student-1", termin.idStudenta);
        Assert.AreEqual("tutor-1", termin.idTutora);
        Assert.AreEqual(7, termin.idPredmeta);
        Assert.AreEqual(datum, termin.datumIVrijeme);
        Assert.AreEqual(tip, termin.tipInstrukcija);
        Assert.AreEqual(status, termin.status);
        Assert.AreEqual("QR-TEST-123", termin.qrKod);
        Assert.AreEqual(25.50, termin.cijena);
        Assert.AreEqual("Online", termin.mjestoCasa);
        Assert.IsTrue(termin.prisustvoPotvrdjeno);
    }
    [TestMethod]
    public void Termin_IdImaKeyAtribut()
    {
        var property = typeof(Termin).GetProperty(nameof(Termin.id));
        Assert.IsNotNull(property);
        Assert.IsTrue(property!.GetCustomAttributes(typeof(KeyAttribute), true).Any());
    }
    // TERMIN DETALJI VIEW MODEL
    [TestMethod]
    public void TerminDetaljiViewModel_PodrazumijevaneVrijednosti_Ispravne()
    {
        var model = new TerminDetaljiViewModel();
        Assert.IsNull(model.Termin);
        Assert.IsNull(model.ImePredmeta);
        Assert.IsNull(model.ImeStudenta);
    }
    [TestMethod]
    public void TerminDetaljiViewModel_SvojstvaMoguSePostaviti()
    {
        var termin = new Termin { id = 10 };
        var model = new TerminDetaljiViewModel
        {
            Termin = termin,
            ImePredmeta = "Matematika",
            ImeStudenta = "Student Test"
        };
        Assert.AreSame(termin, model.Termin);
        Assert.AreEqual("Matematika", model.ImePredmeta);
        Assert.AreEqual("Student Test", model.ImeStudenta);
    }
    // PROFIL VIEW MODEL
    [TestMethod]
    public void ProfilViewModel_PodrazumijevaneVrijednosti_Ispravne()
    {
        var model = new ProfilViewModel();
        Assert.AreEqual(string.Empty, model.Ime);
        Assert.AreEqual(string.Empty, model.Prezime);
        Assert.AreEqual(string.Empty, model.Email);
        Assert.AreEqual(default(Uloga), model.Uloga);
        Assert.IsNull(model.BrojIndeksa);
        Assert.IsNull(model.GodinaStudija);
        Assert.IsNull(model.BrojRecenzija);
        Assert.IsNull(model.BrojCasova);
        Assert.IsNotNull(model.Predmeti);
        Assert.IsNotNull(model.PredmetiObjekti);
        Assert.IsNotNull(model.Recenzije);
        Assert.IsNotNull(model.SviPredmeti);
        Assert.IsNotNull(model.OdabraniPredmeti);
        Assert.AreEqual(0, model.Predmeti.Count);
        Assert.AreEqual(0, model.PredmetiObjekti.Count);
        Assert.AreEqual(0, model.Recenzije.Count);
        Assert.AreEqual(0, model.SviPredmeti.Count);
        Assert.AreEqual(0, model.OdabraniPredmeti.Count);
        Assert.IsNull(model.PrilogOcjene);
    }
    [TestMethod]
    public void ProfilViewModel_SvojstvaMoguSePostaviti()
    {
        var predmet = new Predmet { id = 5, naziv = "Fizika" };
        var recenzija = new RecenzijaViewModel
        {
            ImeStudenta = "Student",
            Recenzija = new Recenzija()
        };
        var prilog = new FormFile(new MemoryStream(new byte[] { 1 }), 0, 1, "prilog", "ocjene.pdf");
        var model = new ProfilViewModel
        {
            Ime = "Test",
            Prezime = "Korisnik",
            Email = "test@test.local",
            Uloga = Uloga.Tutor,
            BrojIndeksa = 12345,
            GodinaStudija = 3,
            BrojRecenzija = 4,
            BrojCasova = 8,
            PrilogOcjene = prilog
        };
        model.Predmeti.Add("Fizika");
        model.PredmetiObjekti.Add(predmet);
        model.Recenzije.Add(recenzija);
        model.SviPredmeti.Add(predmet);
        model.OdabraniPredmeti.Add(5);
        Assert.AreEqual("Test", model.Ime);
        Assert.AreEqual("Korisnik", model.Prezime);
        Assert.AreEqual("test@test.local", model.Email);
        Assert.AreEqual(Uloga.Tutor, model.Uloga);
        Assert.AreEqual(12345, model.BrojIndeksa);
        Assert.AreEqual(3, model.GodinaStudija);
        Assert.AreEqual(4, model.BrojRecenzija);
        Assert.AreEqual(8, model.BrojCasova);
        Assert.AreEqual("Fizika", model.Predmeti.Single());
        Assert.AreSame(predmet, model.PredmetiObjekti.Single());
        Assert.AreSame(recenzija, model.Recenzije.Single());
        Assert.AreSame(predmet, model.SviPredmeti.Single());
        Assert.AreEqual(5, model.OdabraniPredmeti.Single());
        Assert.AreSame(prilog, model.PrilogOcjene);
    }
    // STUDENT PROFIL VIEW MODEL
    [TestMethod]
    public void StudentProfilViewModel_PodrazumijevaneListe_Ispravne()
    {
        var model = new StudentProfilViewModel();
        Assert.IsNull(model.Student);
        Assert.IsNotNull(model.Predmeti);
        Assert.IsNotNull(model.Termini);
        Assert.IsNotNull(model.Recenzije);
        Assert.AreEqual(0, model.Predmeti.Count);
        Assert.AreEqual(0, model.Termini.Count);
        Assert.AreEqual(0, model.Recenzije.Count);
    }
    [TestMethod]
    public void StudentProfilViewModel_SvojstvaMoguSePostaviti()
    {
        var student = new Korisnik();
        var predmet = new Predmet { id = 2, naziv = "Programiranje" };
        var termin = new Termin { id = 3 };
        var recenzija = new RecenzijaViewModel { Recenzija = new Recenzija() };
        var model = new StudentProfilViewModel
        {
            Student = student
        };
        model.Predmeti.Add(predmet);
        model.Termini.Add(termin);
        model.Recenzije.Add(recenzija);
        Assert.AreSame(student, model.Student);
        Assert.AreSame(predmet, model.Predmeti.Single());
        Assert.AreSame(termin, model.Termini.Single());
        Assert.AreSame(recenzija, model.Recenzije.Single());
    }
    // TUTOR PROFIL VIEW MODEL
    [TestMethod]
    public void TutorProfilViewModel_PodrazumijevaneVrijednosti_Ispravne()
    {
        var model = new TutorProfilViewModel();
        Assert.IsNull(model.Tutor);
        Assert.IsNotNull(model.Predmeti);
        Assert.IsNotNull(model.Recenzije);
        Assert.IsNotNull(model.DostupniTermini);
        Assert.AreEqual(0, model.Predmeti.Count);
        Assert.AreEqual(0, model.Recenzije.Count);
        Assert.AreEqual(0, model.DostupniTermini.Count);
        Assert.AreEqual(0d, model.ProsjecnaOcjena);
        Assert.AreEqual(0, model.UkupnoRecenzija);
    }
    [TestMethod]
    public void TutorProfilViewModel_SvojstvaMoguSePostaviti()
    {
        var tutor = new Korisnik();
        var predmet = new Predmet { id = 8, naziv = "Baze podataka" };
        var recenzija = new RecenzijaViewModel { Recenzija = new Recenzija() };
        var termin = new Termin { id = 12 };
        var model = new TutorProfilViewModel
        {
            Tutor = tutor,
            ProsjecnaOcjena = 4.8,
            UkupnoRecenzija = 10
        };
        model.Predmeti.Add(predmet);
        model.Recenzije.Add(recenzija);
        model.DostupniTermini.Add(termin);
        Assert.AreSame(tutor, model.Tutor);
        Assert.AreSame(predmet, model.Predmeti.Single());
        Assert.AreSame(recenzija, model.Recenzije.Single());
        Assert.AreSame(termin, model.DostupniTermini.Single());
        Assert.AreEqual(4.8, model.ProsjecnaOcjena);
        Assert.AreEqual(10, model.UkupnoRecenzija);
    }
    // RECENZIJA VIEW MODEL
    [TestMethod]
    public void RecenzijaViewModel_PodrazumijevaneVrijednosti_Ispravne()
    {
        var model = new RecenzijaViewModel();
        Assert.IsNull(model.Recenzija);
        Assert.AreEqual("Student", model.ImeStudenta);
    }
    [TestMethod]
    public void RecenzijaViewModel_SvojstvaMoguSePostaviti()
    {
        var recenzija = new Recenzija { id = 20, ocjena = 5 };
        var model = new RecenzijaViewModel
        {
            Recenzija = recenzija,
            ImeStudenta = "Adnan"
        };
        Assert.AreSame(recenzija, model.Recenzija);
        Assert.AreEqual("Adnan", model.ImeStudenta);
    }
    // VERIFIKACIJA VIEW MODEL
    [TestMethod]
    public void VerifikacijaViewModel_PodrazumijevaneVrijednosti_Ispravne()
    {
        var model = new VerifikacijaViewModel();
        Assert.IsNotNull(model.TutoriNaCekanju);
        Assert.AreEqual(0, model.TutoriNaCekanju.Count);
        Assert.AreEqual(0, model.BrojVerifikovanih);
    }
    [TestMethod]
    public void VerifikacijaViewModel_SvojstvaMoguSePostaviti()
    {
        var tutor = new TutorSaPredmetima { Tutor = new Korisnik() };
        var model = new VerifikacijaViewModel
        {
            BrojVerifikovanih = 6
        };
        model.TutoriNaCekanju.Add(tutor);
        Assert.AreEqual(6, model.BrojVerifikovanih);
        Assert.AreSame(tutor, model.TutoriNaCekanju.Single());
    }
    // TUTOR SA PREDMETIMA
    [TestMethod]
    public void TutorSaPredmetima_PodrazumijevaneVrijednosti_Ispravne()
    {
        var model = new TutorSaPredmetima();
        Assert.IsNull(model.Tutor);
        Assert.IsNotNull(model.Predmeti);
        Assert.IsNotNull(model.PredmetiNaCekanju);
        Assert.AreEqual(0, model.Predmeti.Count);
        Assert.AreEqual(0, model.PredmetiNaCekanju.Count);
        Assert.IsNull(model.PredmetNaCekanju);
    }
    [TestMethod]
    public void TutorSaPredmetima_SvojstvaMoguSePostaviti()
    {
        var tutor = new Korisnik();
        var predmet = new Predmet { id = 11, naziv = "Matematika" };
        var predmetNaCekanju = new Predmet { id = 12, naziv = "Fizika" };
        var model = new TutorSaPredmetima
        {
            Tutor = tutor,
            PredmetNaCekanju = predmetNaCekanju
        };
        model.Predmeti.Add(predmet);
        model.PredmetiNaCekanju.Add(predmetNaCekanju);
        Assert.AreSame(tutor, model.Tutor);
        Assert.AreSame(predmet, model.Predmeti.Single());
        Assert.AreSame(predmetNaCekanju, model.PredmetiNaCekanju.Single());
        Assert.AreSame(predmetNaCekanju, model.PredmetNaCekanju);
    }
    // OBAVJESTENJE
    [TestMethod]
    public void Obavjestenje_PodrazumijevaneVrijednosti_Ispravne()
    {
        var prijeKreiranja = DateTime.UtcNow;
        var model = new Obavjestenje();
        var poslijeKreiranja = DateTime.UtcNow;
        Assert.AreEqual(0, model.id);
        Assert.AreEqual(string.Empty, model.idKorisnika);
        Assert.AreEqual(string.Empty, model.naslov);
        Assert.AreEqual(string.Empty, model.sadrzaj);
        Assert.IsTrue(model.datumSlanja >= prijeKreiranja);
        Assert.IsTrue(model.datumSlanja <= poslijeKreiranja);
        Assert.IsFalse(model.procitano);
        Assert.IsNull(model.idTermina);
    }
    [TestMethod]
    public void Obavjestenje_SvaSvojstva_MoguSePostaviti()
    {
        var datum = new DateTime(2026, 11, 20, 12, 0, 0);
        var model = new Obavjestenje
        {
            id = 21,
            idKorisnika = "korisnik-1",
            naslov = "Novi termin",
            sadrzaj = "Imate novi rezervisani termin.",
            datumSlanja = datum,
            procitano = true,
            idTermina = 15
        };
        Assert.AreEqual(21, model.id);
        Assert.AreEqual("korisnik-1", model.idKorisnika);
        Assert.AreEqual("Novi termin", model.naslov);
        Assert.AreEqual("Imate novi rezervisani termin.", model.sadrzaj);
        Assert.AreEqual(datum, model.datumSlanja);
        Assert.IsTrue(model.procitano);
        Assert.AreEqual(15, model.idTermina);
    }
    [TestMethod]
    public void Obavjestenje_IdImaKeyAtribut()
    {
        var property = typeof(Obavjestenje).GetProperty(nameof(Obavjestenje.id));
        Assert.IsNotNull(property);
        Assert.IsTrue(property!.GetCustomAttributes(typeof(KeyAttribute), true).Any());
    }
    // OBAVJESTENJE VIEW MODEL
    [TestMethod]
    public void ObavjestenjeViewModel_PodrazumijevaneVrijednosti_Ispravne()
    {
        var model = new ObavjestenjeViewModel();
        Assert.IsNull(model.Obavjestenje);
        Assert.IsFalse(model.MjestoCasaUneseno);
    }
    [TestMethod]
    public void ObavjestenjeViewModel_SvojstvaMoguSePostaviti()
    {
        var obavjestenje = new Obavjestenje { id = 30, naslov = "Podsjetnik" };
        var model = new ObavjestenjeViewModel
        {
            Obavjestenje = obavjestenje,
            MjestoCasaUneseno = true
        };
        Assert.AreSame(obavjestenje, model.Obavjestenje);
        Assert.IsTrue(model.MjestoCasaUneseno);
    }
}
