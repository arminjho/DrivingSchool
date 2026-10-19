# Sistem preporuke (Recommender System) — eAutoškola

**Tip:** Content-based filtering, kosinusna sličnost (cosine similarity), objašnjive preporuke (Explainable AI).
**Implementacija:** `eAutoskola.Application/Recommendations/`.
**Status:** ovaj dokument mora u potpunosti odgovarati kodu (upute.md §2.4) — svaka izmjena formule ili konstante u kodu zahtijeva odgovarajuću izmjenu ovdje.

---

## 1. Svrha i cilj

Sistem preporuke učeniku automatski predlaže koje teorijske lekcije (`StudyLesson`) treba ponoviti, na osnovu njegovih **stvarnih** rezultata na probnim testovima (`TestAttempt`/`AttemptAnswer`). Cilj nije ponuditi lekcije hronološkim redom, nego prepoznati oblasti (`QuestionArea`) u kojima učenik stvarno griješi i ponuditi sadržaj koji cilja te oblasti, uz tekstualno objašnjenje zašto je baš ta lekcija predložena.

## 2. Pristup i motivacija

### 2.1. Primarni pristup: content-based filtering

Odabran je content-based pristup (a ne collaborative filtering) jer:
- Ne zahtijeva podatke o drugim učenicima (nema cold-start problem na nivou cijele platforme — svaki učenik dobija preporuku čim riješi svoj prvi test, bez obzira na to koliko je drugih učenika aktivno).
- Prirodno se oslanja na podatke koji već postoje u domeni (rezultati testova po oblastima, metapodaci lekcija) — nema potrebe za dodatnim prikupljanjem signala.
- Rezultat je direktno objašnjiv (vidi §6) jer se zasniva na konkretnim, imenovanim dimenzijama (oblastima), a ne na latentnim faktorima iz matrične faktorizacije.

### 2.2. Fallback za cold start

Ako učenik još nije predao nijedan test, sistem ne izbacuje praznu listu niti nasumične preporuke — vraća jasnu poruku (vidi §9) da će preporuke biti dostupne nakon prvog testa. Nema popularity-based fallback liste, jer bi takva lista bila nepovezana sa stvarnim potrebama učenika i mogla bi se protumačiti kao "izmišljena" preporuka.

## 3. Izvori podataka i signali

| Signal | Izvorna tabela/polje | Kada se upisuje | Uloga u preporuci |
|---|---|---|---|
| Tačnost odgovora | `AttemptAnswer.IsCorrect` | Pri predaji testa (`TestAttemptService.SubmitAttemptAsync`) | Osnova za `ErrorRate` po oblasti |
| Oblast pitanja | `Question.QuestionAreaId` (preko `AttemptAnswer.QuestionId`) | Pri kreiranju pitanja (Admin CRUD) | Dimenzija vektora korisničkog profila grešaka |
| Pokušaj testa | `TestAttempt.Id`, `TestAttempt.SubmittedAt` | Pri predaji testa | Osnova za `ErrorFrequency` (broj različitih pokušaja sa greškom u oblasti) i za "posljednji pokušaj" u objašnjenju |
| Oblast i oznake lekcije | `StudyLesson.QuestionAreaId`, `StudyLesson.Tags` | Pri kreiranju lekcije (Admin CRUD) | Dimenzije vektora metapodataka lekcije |

Svi signali dolaze isključivo iz stvarno upisanih podataka u bazi — nema sintetičkih/testnih vrijednosti u produkcijskom kodu. Kôd: `StudyRecommendationService.GetRecommendationsAsync` (jedan upit nad `AttemptAnswers` sa `Include`/`Select` projekcijom, bez N+1 upita).

## 4. Izgradnja obilježja (feature engineering)

### 4.1. Vektor korisničkog profila grešaka

Dimenzije vektora = `QuestionArea.Id`. Za svaku oblast u kojoj je učenik odgovarao na pitanja:

```
ErrorRate(oblast)      = BrojNetačnih(oblast) / BrojOdgovorenih(oblast)
ErrorFrequency(oblast) = broj RAZLIČITIH pokušaja testa koji sadrže barem jedan netačan
                         odgovor iz te oblasti
```

Implementacija: `StudentErrorProfileBuilder.Build` (`eAutoskola.Application/Recommendations/StudentErrorProfile.cs`) — čista funkcija, bez pristupa bazi, testirana u `StudentErrorProfileBuilderTests`.

### 4.2. Vektor metapodataka lekcije

Dimenzije su iste (`QuestionArea.Id`), tako da su oba vektora direktno uporediva:

```
vector[lekcija.QuestionAreaId] = 1.0                     (primarna oblast)
vector[druga_oblast.Id]        = TagAssociationWeight     ako naziv te oblasti (bez obzira
                                  (0.3)                    na velika/mala slova) postoji kao
                                                            podniz u polju lekcija.Tags
```

Implementacija: `StudyLessonVectorBuilder.Build` (`eAutoskola.Application/Recommendations/StudyLessonVector.cs`), testirana u `StudyLessonVectorBuilderTests`. Ovo je tačka gdje `Tags` polje — ne samo `QuestionAreaId` — stvarno utiče na vektor, u skladu sa zahtjevom da se "oblast/tagovi" lekcije koriste pri izgradnji vektora.

## 5. Algoritam sličnosti i bodovanje

### 5.1. Kombinovana težina po oblasti (koristi OBA signala)

```
Weight(oblast) = ErrorRateWeight      × ErrorRate(oblast)
               + ErrorFrequencyWeight × ( ErrorFrequency(oblast) / MaxErrorFrequency )
```

gdje je `MaxErrorFrequency` najveća vrijednost `ErrorFrequency` među svim oblastima učenika (normalizacija na raspon [0,1], tako da je frekvencijski signal uporediv sa stopom greške). Konstante (`eAutoskola.Application/Recommendations/RecommenderConstants.cs`):

| Konstanta | Vrijednost | Značenje |
|---|---|---|
| `ErrorRateWeight` | 0.6 | Udio stope grešaka u konačnoj težini |
| `ErrorFrequencyWeight` | 0.4 | Udio (normalizirane) frekvencije grešaka |
| `CriticalErrorFrequencyThreshold` | 2 | Oblast postaje "kritična" kada su greške ponovljene u ≥ 2 različita pokušaja |
| `TagAssociationWeight` | 0.3 | Težina sekundarne (tag-based) asocijacije lekcije sa oblašću |

Oba signala (ErrorRate i ErrorFrequency) imaju nenulti koeficijent i oba su pokrivena jediničnim testovima koji dokazuju da svaki od njih samostalno mijenja konačni rezultat (`StudentErrorProfileBuilderTests.Build_BothSignalsInfluenceWeight_...`) — nijedan prikupljeni signal se ne ignoriše (upute.md §2.4).

### 5.2. Kosinusna sličnost

```
similarity(A, B) = (A · B) / (‖A‖ × ‖B‖)
```

gdje je `A` = korisnički profil grešaka, `B` = vektor lekcije. Implementacija: `VectorMath.CosineSimilarity` — čista, bez zavisnosti, testirana (`VectorMathTests`).

### 5.3. Rangiranje

1. Izračunaj `similarity` za svaku `StudyLesson`.
2. Izbaci lekcije sa `similarity == 0` (nema preklapanja dimenzija).
3. Sortiraj opadajuće po `similarity`.
4. Deterministički tie-break: pri jednakom rezultatu, manji `StudyLesson.Id` ide prvi (`OrderByDescending(Score).ThenBy(Lesson.Id)`).
5. Uzmi prvih `N` (podrazumijevano `DefaultRecommendationCount = 5`, klijent može tražiti drugi broj preko `?take=`).

## 6. Objašnjive preporuke (Explainable AI)

Tekst objašnjenja se generiše isključivo iz stvarnih brojeva te preporuke — nikad generička rečenica. Implementacija: `ExplanationGenerator.Generate` (testirana u `ExplanationGeneratorTests`).

```
"Preporučujemo vam da ponovite lekciju '{naziv lekcije}' jer ste na posljednjem probnom
testu imali {X}% netačnih odgovora iz ove specifične kategorije ({naziv oblasti})."
```

`{X}` = procenat netačnih odgovora **unutar te oblasti, u POSLJEDNJEM pokušaju testa koji je sadržavao pitanja iz nje** (ne životni prosjek — `ComputeLastAttemptAreaErrorPercent` u `StudyRecommendationService`), tako da rečenica zvuči prirodno ("na posljednjem probnom testu").

Ako je oblast označena kao kritična (§5.1), dodaje se:

```
" Ova oblast je označena kao kritična jer ste greške iz nje ponovili u više probnih testova."
```

Primjer stvarnog izlaza (iz seed podataka — učenik `mobile/test`; na posljednjem pokušaju testa "Test 1 - Saobraćajni znakovi" imao je 3 netačna odgovora od 10, a greške u oblasti je ponovio u tri različita pokušaja):

> *"Preporučujemo vam da ponovite lekciju 'Saobraćajni znakovi' jer ste na posljednjem probnom testu imali 30% netačnih odgovora iz ove specifične kategorije (Saobraćajni znakovi). Ova oblast je označena kao kritična jer ste greške iz nje ponovili u više probnih testova."*

## 7. Upravljanje modelom

Nema treniranog modela niti perzistiranih težina — sve vektore i sličnosti sistem računa **on-demand**, pri svakom pozivu `GET /api/recommendations`, direktno iz trenutnog stanja baze. Ovo je namjerna odluka: broj učenika/testova/lekcija u domeni autoškole je dovoljno mali da treniranje/čuvanje modela ne donosi korist, a izbjegava se i problem zastarjelog modela nakon što učenik riješi novi test. `StudyRecommendationService` je registrovan kao `Scoped` servis (upute.md §3.4).

## 8. Integracija u aplikaciju

- **Endpoint:** `GET /api/recommendations?take={N}` — `[Authorize(Roles = Role.Student)]`, `StudentId` se uvijek uzima iz JWT tokena preko `ICurrentUserService`, nikad iz parametra rute/tijela (upute.md §5).
- **Kontroler:** `RecommendationController` (`eAutoskola.API/Controllers/RecommendationController.cs`) — tanak, bez poslovne logike, direktno poziva `StudyRecommendationService`.
- **Logovanje:** svaka stvarno prikazana preporuka upisuje se u `RecommendationLogs` (StudentId, StudyLessonId, Score, Explanation, CreatedAt) prije vraćanja odgovora — jedan `SaveChangesAsync` poziv za sve preporuke u odgovoru. Pri prvom pokretanju baze `RecommendationLogSeeder` pokreće isti servis za nekoliko učenika koji su rješavali testove, pa je tabela popunjena stvarnim izlazom recommendera, ne ručno upisanim vrijednostima.
- **Klijentski ekran:** docs/prijava.md §4.2.8 ("Preporuke za učenje") — lista kartica sa naslovom lekcije i generisanim objašnjenjem.

## 9. Rubni slučajevi

| Slučaj | Ponašanje |
|---|---|
| Učenik nema nijedan predat pokušaj testa | `ColdStartMessage` se vraća umjesto liste: "Preporuke će biti dostupne nakon što riješite barem jedan probni test." |
| Učenik ima pokušaje, ali su svi odgovori tačni | Težine svih oblasti su 0 (`HasAnyData == true`, ali bez grešaka), pa nijedna lekcija nema sličnost > 0 → prazna lista preporuka, bez cold-start poruke (isto kao sljedeći red); klijent prikazuje „Trenutno nemamo preporuka za vas“ |
| Nijedna lekcija nema sličnost > 0 sa profilom učenika | Prazna lista preporuka (bez cold-start poruke, jer podaci o greškama postoje — samo nema odgovarajuće lekcije u katalogu) |
| Sve lekcije imaju identičnu sličnost | Deterministički tie-break po `StudyLesson.Id` (§5.3), tako da je redoslijed stabilan između poziva |
| Lekcija bez tagova koji se poklapaju sa drugim oblastima | Vektor ima samo primarnu dimenziju (težina 1.0) — i dalje validan za poređenje |

## 10. Ograničenja i moguća poboljšanja

- Formula za `Weight` koristi fiksne, ručno odabrane koeficijente (0.6/0.4) — u budućnosti bi se mogli kalibrisati na osnovu stvarne stope "učenik je otvorio preporučenu lekciju nakon što mu je prikazana" (podatak već postoji u `StudentStudyLesson`/`RecommendationLog`, samo nije iskorišten za kalibraciju).
- Tag-based sekundarna asocijacija (§4.2) koristi jednostavno podudaranje podniza — ne prepoznaje sinonime niti bosanske padežne oblike van tačnog navođenja naziva oblasti u `Tags`.
- Nema eksplicitnog "zaboravljanja" starih grešaka — stariji i noviji pokušaji imaju jednaku težinu u `ErrorRate`/`ErrorFrequency`. Vremenski opadajuća težina (noviji pokušaji važniji) je prirodno proširenje.

## 11. Mapiranje na odobrenu prijavu

| Naziv iz prijave (Bosanski) | Naziv u kodu (Glossary, CLAUDE.md §3) |
|---|---|
| RezultatTesta | `TestAttempt` (+ `AttemptAnswer` za pojedinačne odgovore) |
| Pitanje | `Question` |
| oblast | `QuestionArea` |
| lekcija | `StudyLesson` |

Implementacija prati `docs/prijava.md` §6 tačku po tačku: §6.1 → ovaj dokument §3–4, §6.2 (kosinusna sličnost) → §5.2, §6.3 (Explainable AI) → §6.

## 12. Radni numerički primjer

Primjer je stvarni slučaj iz seed podataka: učenik `mobile/test` ima 5 predanih pokušaja (testovi 1-4, test 1 dva puta). Odgovori po oblastima (pitanja jednog testa mogu pripadati različitim oblastima):

| Oblast | Odgovoreno | Netačno | ErrorRate | ErrorFrequency | Normalizirana frekvencija | Težina |
|---|---|---|---|---|---|---|
| Saobraćajni znakovi | 23 | 10 | 10/23 = 0.4348 | 3 (pokušaji sa greškom) | 3/3 = 1.0 | 0.6 × 0.4348 + 0.4 × 1.0 = **0.6609** |
| Pravila vožnje | 19 | 6 | 6/19 = 0.3158 | 2 | 2/3 = 0.6667 | 0.6 × 0.3158 + 0.4 × 0.6667 = **0.4561** |
| Situacije u saobraćaju | 8 | 1 | 1/8 = 0.125 | 1 | 1/3 = 0.3333 | 0.6 × 0.125 + 0.4 × 0.3333 = **0.2083** |

`MaxErrorFrequency = 3`. Kritične su oblasti sa `ErrorFrequency ≥ 2`: Saobraćajni znakovi i Pravila vožnje.

Korisnički vektor (dimenzije = Id oblasti): `{Znakovi: 0.6609, Pravila: 0.4561, Situacije: 0.2083}`, `‖A‖ = 0.8296`.

Lekcija "Saobraćajni znakovi" (primarna oblast Znakovi; njeni tagovi ne navode druge oblasti) → vektor `{Znakovi: 1.0}`:

```
similarity = (0.6609 × 1.0) / (0.8296 × 1.0) = 0.7966
```

Lekcija "Prvenstvo prolaza na raskrsnicama" (primarna oblast Pravila vožnje; tagovi sadrže "saobraćajni znakovi" i "situacije u saobraćaju", pa vektor dobija sekundarne dimenzije sa težinom `TagAssociationWeight = 0.3`) → vektor `{Pravila: 1.0, Znakovi: 0.3, Situacije: 0.3}`, `‖B‖ = 1.0863`:

```
dot        = 0.4561 × 1.0 + 0.6609 × 0.3 + 0.2083 × 0.3 = 0.7169
similarity = 0.7169 / (0.8296 × 1.0863) = 0.7955
```

Bez tagova ova lekcija bi imala sličnost samo 0.5498, pa se vidi da polje `Tags` stvarno utiče na redoslijed. Vrijednosti 0.7966 i 0.7955 vraća i `GET /api/recommendations` za ovog učenika; iste brojke se upisuju u `RecommendationLogs`.

Objašnjenje za prvu lekciju: *"Preporučujemo vam da ponovite lekciju 'Saobraćajni znakovi' jer ste na posljednjem probnom testu imali 30% netačnih odgovora iz ove specifične kategorije (Saobraćajni znakovi). Ova oblast je označena kao kritična jer ste greške iz nje ponovili u više probnih testova."* (30% = 3 netačna od 10 u zadnjem pokušaju koji sadrži pitanja iz te oblasti.)

## 13. Relevantne datoteke

```
eAutoskola.Application/Recommendations/
├── RecommenderConstants.cs          - sve nazvane konstante
├── VectorMath.cs                    - kosinusna sličnost (čista funkcija)
├── StudentErrorProfile.cs           - StudentErrorProfileBuilder (čista funkcija)
├── StudyLessonVector.cs             - StudyLessonVectorBuilder (čista funkcija)
├── ExplanationGenerator.cs          - generisanje teksta objašnjenja (čista funkcija)
├── RecommendationDto.cs             - DTO-ovi odgovora
└── StudyRecommendationService.cs    - orkestracija (DB upiti + logovanje)

eAutoskola.API/Controllers/RecommendationController.cs   - GET /api/recommendations

eAutoskola.Tests/Recommendations/
├── VectorMathTests.cs
├── StudentErrorProfileBuilderTests.cs
├── StudyLessonVectorBuilderTests.cs
└── ExplanationGeneratorTests.cs
```
