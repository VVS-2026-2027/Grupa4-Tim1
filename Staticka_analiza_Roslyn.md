# 1.2 Statička analiza koda (Roslyn analizatori)

## 1. Korišteni alat i konfiguracija

Za statičku analizu korišteni su **Microsoft Roslyn analizatori** (ugrađeni u .NET SDK i Visual Studio), jedan od lintera spomenutih u Predavanju 2 (pravila CS, CA i IDE). Analiza je provedena nad istim kodom (aplikacija OptiShape, .NET 8) koji je pregledan u ručnoj inspekciji.

Konfiguracija u `OptiShape.csproj`:

```xml
<AnalysisLevel>latest-all</AnalysisLevel>
<EnableNETAnalyzers>true</EnableNETAnalyzers>
<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
```

Analiza je pokrenuta sa `dotnet build --no-incremental` i kroz Visual Studio Error List (Entire Solution, Build + IntelliSense). `latest-all` uključuje sva dostupna CA pravila, uključujući ona koja su po defaultu isključena. Tokom analize nije se pojavilo nijedno IDE pravilo.

## 2. Rezultati

Alat je prijavio **300 upozorenja** (0 grešaka), u 22 različita pravila. Svaki nalaz ima kod pravila, datoteku i liniju. Ozbiljnost je dodijeljena po skali iz Predavanja 2 (1 do 5), a stavka check-liste prema listama korištenim u inspekciji.

| Kod | Opis | Br. | Primjeri lokacija | Stavka liste | Ozbiljnost |
|---|---|---|---|---|---|
| CA2007 | Nedostaje `ConfigureAwait` na awaited zadatku | 156 | TerminController (33), PlanIshraneITreningaController (30), StatistikeNapretkaController (27) | S3 | 1 |
| CA1515 | Javni tipovi koji mogu biti `internal` | 33 | HomeController:13, Models\Enums.cs:3 | S3 | 1 |
| CA5395 | Akcija bez eksplicitne HTTP metode | 29 | KorisnikController:28, TerminController:27 | SEC3 (CWE-352) | 3 |
| CA1062 | Parametar javne metode nije provjeren na null | 22 | PlacanjeController:83, TerminController:114 | D2 | 2 |
| CS8618 | Non-nullable svojstvo nije inicijalizirano | 11 | Korisnik.cs:18, EmailSender.cs:13 | D4 | 2 |
| CS8602 | Dereferenciranje moguće null reference | 7 | PlanIshraneITreningaController:118, StatistikeNapretkaController:384 | D4 | 3 |
| CA1848 | Koristiti `LoggerMessage` delegate | 7 | HomeController:67, Login.cshtml.cs:84 | S8 | 1 |
| CS8600 | Konverzija null vrijednosti u non-nullable tip | 5 | Dashboard.cshtml:16 do 49 | V2 | 2 |
| CA1054 | `returnUrl` parametar kao `string` umjesto `Uri` | 4 | Login.cshtml.cs:56, Register.cshtml.cs:146 | S3 | 1 |
| CA2000 | `SmtpClient` / `MailMessage` se ne oslobađaju (Dispose) | 3 | EmailSender.cs:29, 36; HomeController:82 | D5 | 3 |
| CA1031 | Hvatanje generičkog `Exception` | 3 | HomeController:107, 196; KorisnikController:187 | D6 / SEC5 (A10:2025) | 3 |
| CA2254 | Promjenjiv šablon poruke u logu | 3 | HomeController:67, 109, 198 | SEC5 | 2 |
| CA1034 | Ugniježđeni javni tip `InputModel` | 3 | Login:42, Manage\Index:55, Register:78 | S3 | 1 |
| NU1902 | Ranjivi paketi MailKit 4.12.1 i MimeKit 4.12.0 (umjerena ozbiljnost) | 2 | OptiShape.csproj | SEC6 (CWE-1395 / A03:2025) | 3 |
| CA2227 | Kolekcija `ExternalLogins` ima setter | 2 | Login:35, Register:72 | S3 | 1 |
| CA1873 | Skupa evaluacija argumenata u logu | 2 | HomeController:67, 103 | S8 | 1 |
| CA1305 | `int.ToString()` ovisi o lokalizaciji | 2 | TerminController:271, 283 | V2 | 1 |
| CA1056 | `ReturnUrl` svojstvo kao `string` | 2 | Login:37, Register:66 | S3 | 1 |
| CS8604 | Mogući null argument | 1 | HomeController:139 | D2 | 2 |
| CA2253 | Numerički placeholder u šablonu loga | 1 | HomeController:103 | SEC5 | 1 |
| CA1724 | Naziv tipa se poklapa s imenom namespace-a | 1 | Migrations\20250519213852_OptiShape.cs:9 | S3 | 1 |
| CA1725 | Naziv parametra ne odgovara baznoj metodi | 1 | ApplicationDbContext.cs:20 | V1 | 1 |
| | **Ukupno** | **300** | | | |

Po kategorijama: CA 262, CS 24, NU 2 (dva NuGet upozorenja), tj. 24 nullable upozorenja kompajlera i 262 CA pravila (+ 2 NU = 288 + 12 = 300 zbirno po tabeli).

## 3. Pareto analiza

| Rang | Kod | Br. | Udio | Kumulativno |
|---|---|---|---|---|
| 1 | CA2007 | 156 | 52,0% | 52,0% |
| 2 | CA1515 | 33 | 11,0% | 63,0% |
| 3 | CA5395 | 29 | 9,7% | 72,7% |
| 4 | CA1062 | 22 | 7,3% | 80,0% |
| 5 | CS8618 | 11 | 3,7% | 83,7% |
| 6 | CS8602, CA1848 | 14 | 4,7% | 88,3% |
| 7 | ostalih 15 pravila | 35 | 11,7% | 100% |

Četiri pravila (CA2007, CA1515, CA5395, CA1062) čine tačno 80% svih nalaza, a samo CA2007 više od polovine. Pareto princip se potvrđuje, ali vrijedi upozoriti da najbrojniji nalazi nisu i najvažniji.

## 4. Komentar analize alata

**Šum dominira rezultatima.** Većina nalaza su stilski i performansni savjeti, a ne defekti.
- **CA2007 (156):** u ASP.NET Core aplikaciji ne postoji sinhronizacijski kontekst, pa je `ConfigureAwait(false)` bez efekta. Pravilo je namijenjeno bibliotekama. Lažno pozitivan nalaz za ovu vrstu aplikacije.
- **CA1515 (33):** MVC kontroleri i modeli moraju biti `public` zbog otkrivanja i model bindinga. Lažno pozitivan.
- **CA1848 / CA1873 (9):** čista optimizacija logiranja, bez uticaja na ispravnost.

**Generisani kod.** Oko 67 nalaza (22%) odnosi se na kod koji tim nije ručno pisao: Identity stranice (Login, Register, Manage\Index: CA2007 30, CA1054 4, CA1056 2, CA1034 3, CA2227 2, CA1848 3, CA1515 8, CA1062 1) i EF migracije (CA1062 8, CA1515 5, CA1724 1). Nalazi u migracijama, npr. CA1724 za naziv migracije `OptiShape`, nisu relevantni.

**Stvarni nalazi vrijedni ispravke.**
- **CA2000 (3):** `SmtpClient` i `MailMessage` u `EmailSender` (linije 29, 36) te nalaz u HomeController:82 se ne oslobađaju. Prijeti curenje resursa (stavka D5). Rješenje: `using` blok.
- **CS8602 (7):** dereferenciranje moguće null reference u kontrolerima i pogledu može dati `NullReferenceException` u radu (stavka D4).
- **CA1031 (3):** hvatanje generičkog `Exception` može sakriti stvarne greške (stavka D6, kategorija A10:2025).
- **CA5395 (29):** akcije bez `[HttpGet]`/`[HttpPost]` atributa. Pravilo je sigurnosno (CSRF, CWE-352), a odnosi se i na akcije koje mijenjaju stanje (npr. `Create`, `Edit`, `Delete`).
- **NU1902 (2):** MailKit i MimeKit imaju poznate ranjivosti umjerene ozbiljnosti (SEC6, CWE-1395 / A03:2025). Preporuka je ažurirati pakete na zakrpljene verzije.
- **CA1062 (kontroleri) i CS8618:** djelimično opravdani. U kontrolerima MVC model binding rijetko šalje `null`, a u entitetima (`Korisnik`, `ApplicationUser`) svojstva popunjava EF Core, pa je CS8618 uglavnom šum.

**Sigurnosni nalazi.** Alat pokriva samo uzak dio sigurnosne liste: CSRF (CA5395), ranjive pakete (NU1902) i dijelom logiranje (CA2254, CA2253). CA2254 je u ovom kontekstu slab sigurnosni nalaz. CWE oznake navedene u tabeli (CWE-352, CWE-1395) potrebno je provjeriti na cwe.mitre.org, a OWASP kategorije na owasp.org/Top10.

**Šta alat ne vidi.** Roslyn analizatori nisu prijavili ništa od onoga što se otkriva samo ručnim pregledom i razumijevanjem poslovne logike: hardkodirane tajne (SEC2), kontrolu pristupa po ulozi i vlasniku podataka, odnosno IDOR (SEC3), način pohrane i hashiranje lozinki (SEC4), kao ni curenje stack tracea korisniku (SEC5). Nalazi koji se poklapaju s inspekcijom su stavke D2/D4 (null provjere), D5 (oslobađanje resursa), D6 (generički `catch`) i SEC6 (ranjivi paketi). Analiza je brza i konzistentna, ali je dopuna ručnoj inspekciji, a ne zamjena.

**Ograničenja i preporuke.** Prije korištenja u CI procesu treba isključiti ili smanjiti ozbiljnost šumnih pravila (CA2007, CA1515, CA1848) i isključiti generisani kod (`generated_code = true` u `.editorconfig`). Time bi broj upozorenja pao s 300 na oko 70, ostavljajući nalaze koji zaista traže pažnju.

## 5. Zaključak

Roslyn analizatori su za nekoliko minuta pregledali cijelu aplikaciju i dali 300 nalaza, od kojih je većina stilska ili u generisanom kodu. Vrijednih nalaza ima oko 45 (CA5395, CS8602, CA2000, CA1031, NU1902, dio CA1062), i oni se uglavnom poklapaju s defektima iz ručne inspekcije. Dok alat nije konfiguriran da filtrira šum, rezultate je potrebno ručno izdvajati.