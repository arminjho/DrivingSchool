# eAutoškola – Prijava teme seminarskog rada iz RS II

**Fakultet Informacijskih Tehnologija**

Prijava teme za seminarski rad iz predmeta Razvoj softvera II

**Predmetni nastavnik:** Prof. dr. sc. Elmir Babović, prof. dr. sc. Denis Mušić
**Student:** Armin Muratspahić – IB210044
**Mostar, April 2026.**

---

## 1. Uvod

Osnovna svrha aplikacije "eAutoškola" je digitalizacija i centralizacija svih poslovnih i edukacijskih procesa unutar jedne autoškole. Sistem pruža jedinstvenu platformu koja povezuje administrativno osoblje, instruktore vožnje i same kandidate (učenike). Kroz ovaj sistem eliminiše se potreba za tradicionalnim papirnim vođenjem evidencije časova, ručnim provjerama rasporeda i slanjem pojedinačnih obavijesti, čime se drastično ubrzava rad i smanjuje mogućnost ljudske greške.

## 2. Ciljevi projekta

Projekt "eAutoškola" ima dva glavna cilja:

- Kreirati robusnu desktop aplikaciju koja će administrativnom osoblju pružiti potpunu kontrolu nad resursima, voznim parkom, evidencijama i poslovnim izvještajima autoškole.
- Razviti mobilnu aplikaciju sa dva prilagođena interfejsa: jedan za učenike (koji omogućava brzo zakazivanje časova, učenje i polaganje probnih testova) i drugi za instruktore (koji služi za efikasno vođenje rasporeda i ocjenjivanje kandidata na terenu).

## 3. Funkcionalnosti

### 3.1. Desktop funkcionalnosti (Administrativni modul)

Desktop strana aplikacije namijenjena je administratorima autoškole i osigurava sljedeće funkcionalnosti:

- **Prijava na aplikaciju i autorizacija:** Pristup sistemu je zaštićen i vrši se u skladu sa administratorskom ulogom (Role-based access). Sistem prepoznaje nivo privilegija i otvara odgovarajući administrativni dashboard.
- **Upravljanje referentnim podacima (CRUD):** Administrator ima mogućnost potpunog upravljanja (unos, izmjena, brisanje i pregled) osnovnim šifrarnicima aplikacije. To uključuje evidenciju država, gradova, kategorija vozila (A, B, C, D, E), statusa zahtjeva i tipova usluga koje autoškola nudi.
- **Upravljanje korisnicima (CRUD):** Centralizovan sistem za kreiranje i ažuriranje korisničkih računa. Administrator pravi profile za nove učenike i instruktore, dodjeljuje im odgovarajuće uloge i upravlja njihovim statusom aktivnosti unutar sistema.
- **Upravljanje voznim parkom:** Detaljna evidencija svih automobila koji se koriste za obuku. Modul omogućava vođenje istorije servisa za svako vozilo (unos datuma servisa, opisa kvara ili redovnog održavanja, te povezivanje sa konkretnim vozilom).
- **Modul za izvještavanje i štampanje (PDF):** Sistem omogućava dinamičko kreiranje i preuzimanje minimalno dva poslovna izvještaja u PDF formatu:
  1. **Izvještaj o statusu časova vožnje:** Generisanje statistike o broju odrađenih, otkazanih i zakazanih časova, uz mogućnost grupisanja i filtriranja po specifičnom instruktoru u zadatom vremenskom periodu.
  2. **Izvještaj o vozilima i servisima:** Kreiranje detaljnog pregleda servisne istorije i održavanja, filtriranog po kategoriji vozila.

### 3.2. Mobilne funkcionalnosti klijenta (Učenik)

Mobilna strana za učenike obuhvata klijentski dio aplikacije i pruža sljedeće mogućnosti:

- **Kreiranje korisničkog računa i prijava:** Mogućnost samostalne registracije novih učenika i sigurna prijava na postojeći nalog putem kredencijala.
- **Pregled i uređivanje ličnih podataka:** Učenik može u bilo kojem trenutku ažurirati svoje kontakt informacije, adresu i profilnu sliku.
- **Resetovanje lozinke:** Implementiran siguran proces za izmjenu izgubljenih ili zaboravljenih pristupnih podataka.
- **Zakazivanje časova vožnje:** Modul koji učenicima omogućava odabir slobodnih termina kod dodijeljenog instruktora putem integrisanog kalendara. Cjelokupna logika strogo prati "State machine" pravila na backendu, provodeći zahtjev kroz stanja: Pending -> Confirmed -> Cancelled / Completed.
- **Online plaćanje (PayPal):** Integracija sa stvarnim PayPal sandbox okruženjem koja učenicima omogućava online plaćanje časova vožnje ili cjelokupnih paketa obuke direktno iz mobilne aplikacije. Omogućeno je evidentiranje uplata i procesiranje povrata novca (refund) u slučaju blagovremenog otkazivanja časa.
- **Pregled historije obuke:** Detaljan uvid u sve prethodno odvožene časove, upisane statuse i pregled povratnih informacija (bilješki i ocjena) koje je instruktor ostavio.
- **Probni testovi (Master-Details):** Interaktivni modul za rješavanje teorijskih ispita dizajniran kroz master-details formu. Krovni test predstavlja "Master" entitet, dok se pojedinačna pitanja, ponuđeni odgovori i grafički prikazi raskrsnica prikazuju kao detalji ("Details").
- **Pregled lekcija i materijala:** Direktan pristup edukativnom sadržaju, literaturi i skriptama koje se redovno ažuriraju za svaku kalendarsku godinu.

### 3.3. Mobilne funkcionalnosti (Instruktor)

Mobilni interfejs prilagođen instruktorima vožnje omogućava rad na terenu:

- **Prijava na aplikaciju:** Sigurna autentifikacija instruktora pomoću JWT tokena.
- **Upravljanje zahtjevima za časove:** Instruktor ima jasan pregled svih pristiglih učeničkih zahtjeva (Pending). Omogućeno je odobravanje (Confirmed) ili odbijanje (Cancelled) termina, uz obavezno upisivanje razloga ukoliko se termin odbija.
- **Evidencija i završetak časa (Ocjenjivanje):** Interfejs za označavanje početka i kraja vožnje. Prilikom završetka, instruktor mijenja status u Completed, unosi brojčanu ocjenu kandidata (1-5 zvjezdica) i upisuje detaljne tekstualne bilješke o vještinama koje je učenik na tom času usavršio.
- **Pregled lične statistike i rasporeda:** Modul koji instruktoru pruža uvid u digitalni kalendar sa predstojećim obavezama, kao i statistiku o ukupnom broju odrađenih časova kroz tekući mjesec.

## 4. Skice / Mockups

> **Napomena prevodioca dokumentacije:** Tekstualni opisi ispod svakog naslova su izvorni opisi iz prijave (Bosanski, verbatim). Odmah nakon njih slijedi odjeljak **"Precizan opis mockup-a"** koji doslovno opisuje elemente prikazane na slici (polja, dugmad, labele, tabovi, bedževi, boje) na osnovu stvarnog mockup screenshota iz .docx/.pdf prijave. Gdje se tekstualni opis i mockup razlikuju u detalju (npr. tačan naziv dugmeta), oba izvora su navedena — implementacija treba zadovoljiti oba (vidjeti CLAUDE.md, pravilo "kada se tekst i mockup razlikuju, implementirati oba").

### 4.1. Mockups Desktop strane (Administracija)

#### 4.1.1. Pregled početne stranice (Dashboard)

Nakon uspješne prijave na sistem, administratoru se prikazuje centralna kontrolna tabla (Dashboard) sa ključnim statističkim podacima u realnom vremenu. Na vrhu su istaknute kartice sa ukupnim brojem aktivnih učenika (120), instruktora (15), aktivnih vozila na terenu (25) i zakazanih časova za tekući period (34). Centralni dio zauzima stubni grafikon koji prikazuje intenzitet i broj odrađenih časova po mjesecima tokom godine, dok se desno nalazi sekcija sa nedavnim aktivnostima unutar sistema (novi zahtjevi, odobreni časovi, dodavanje servisa i novih korisnika). Na dnu ekrana pozicioniran je tabelarni pregled predstojećih časova za tekući dan sa informacijama o učeniku, instruktoru i zoni vožnje, kao i kružni grafikon (Pie Chart) koji grafički prikazuje strukturu i trenutni status svih zahtjeva u sistemu (Pending, Confirmed, Cancelled, Completed).

**Precizan opis mockup-a:**

- Tema: tamni (dark) layout cijele desktop administracije.
- Lijevi sidebar (fiksni, tamniji od glavnog panela): logo/naziv "eAutoškola" + "Admin Panel" na vrhu; stavke menija: **Dashboard** (aktivna, istaknuta), **Korisnici**, **Vozni park**, **Izvještaji**, **Referentni podaci**.
- Gornja traka (header): breadcrumb "eAutoškola > Dashboard", ikona zvona (notifikacije) i avatar/inicijal korisnika u gornjem desnom uglu.
- Naslov sekcije: "Dashboard" + podnaslov "Pregled aktivnosti — eAutoškola".
- Red od 4 KPI kartice: "Ukupno učenika" = 120 (+8 ovaj mjesec, ikona osobe, plava), "Instruktori" = 15 (Svi aktivni, ikona trenda, zelena), "Vozila" = 25 (3 na servisu, ikona automobila, ljubičasta), "Časovi ovaj mj." = 34 (↑12% vs prošli, ikona sata, narandžasta).
- Grafikon "Časovi po mjesecima" (podnaslov "Broj realizovanih časova 2026") — plavi stubni (bar) grafikon, 12 stubaca (Jan–Dec), vrijednosti ~35–100.
- Donut/pie grafikon "Status časova" (podnaslov "Raspodjela po statusima") sa 4 segmenta u boji i legendom: Cancelled (crvena), Completed (zelena), Confirmed (plava), Pending (narandžasta).
- Red od 3 manje kartice ispod grafikona: "Pending" = 56 (žuta ikona sata), "Confirmed" = 89 (plava ikona kvačice), "Cancelled" = 34 (crvena ikona X).

#### 4.1.2. Upravljanje korisnicima

Ovaj ekran omogućava administratorima potpunu kontrolu i pretragu korisničkih računa u sistemu. Sadrži naprednu traku za tekstualnu pretragu korisnika po imenu ili e-mailu, kao i padajući meni (Dropdown) za filtriranje korisnika prema njihovoj specifičnoj ulozi (Učenik, Instruktor, Admin). Centralna tabela prikazuje osnovne informacije o registrovanim korisnicima: ime i prezime, e-mail adresu, dodijeljenu ulogu i trenutni status računa (Aktivan/Neaktivan). U koloni "Akcije" administratorima su na raspolaganju brze opcije za uređivanje podataka ili brisanje korisnika iz sistema. U gornjem desnom uglu nalazi se istaknuto dugme "+ Novi korisnik" koje otvara formu za kreiranje novog profila, dok je na dnu implementirana paginacija za lakšu navigaciju kroz velike liste.

**Precizan opis mockup-a:**

- Naslov "Korisnici" + podnaslov "Upravljanje korisničkim nalozima"; dugme "+ Novi korisnik" gore desno.
- Traka filtera: textbox "Pretraži korisnike..." (ikona lupe) + dropdown "Sve uloge" (otvoren prikazuje opcije: Sve uloge, Učenik, Instruktor, Admin).
- Tabela, kolone: Ime i prezime, Email, Uloga (bedž), Status (bedž), Akcije.
- Primjeri redova: Amina Hodžić / Učenik bedž (plavi) / Aktivan bedž (zeleni); Damir Kovačević / Instruktor bedž (ljubičasti) / Aktivan; Lejla Bašić / Učenik / Neaktivan (sivi bedž); Emir Mustafić / Učenik / Aktivan; Sanja Perić / Instruktor / Aktivan; Adnan Zukić / Učenik / Aktivan; Mirela Tahić / Admin bedž (crveni) / Aktivan.
- Kolona Akcije: linkovi "Uredi" i "Obriši" (Obriši u crvenoj boji) za svaki red.

#### 4.1.3. Upravljanje voznim parkom

Stranica za upravljanje voznim parkom podijeljena je na dva pod-taba: "Vozila" i "Servisi". Ekran prikazuje tabelarni pregled svih automobila koji se koriste za obuku kandidata. Administratori mogu pretraživati vozila putem registarskih oznaka ili ih filtrirati prema kategoriji vozila (A, B, C). Tabela obuhvata podatke o registraciji, marki i modelu vozila (npr. VW Polo, Opel Corsa), pripadajućoj kategoriji, godini proizvodnje i trenutnom statusu vozila (Aktivan, Na servisu). Kroz kolonu sa akcijama omogućeno je ažuriranje podataka o vozilu ili njegovo uklanjanje iz evidencije, dok se u gornjem desnom uglu nalazi dugme "+ Novo vozilo" za proširenje voznog parka.

**Precizan opis mockup-a (tab "Vozila"):**

- Naslov "Vozni park" + podnaslov "Pregled vozila i servisnih zapisa"; dugme "+ Novo vozilo" gore desno.
- Dva pod-taba ispod naslova: "Vozila" (aktivan) / "Servisi".
- Tabela, kolone: Registracija, Marka/Model, Kategorija (bedž slova), Godina, Status (bedž).
- Redovi: E22-A-123 / VW Polo / B / 2021 / Aktivan (zeleni); E54-K-456 / Škoda Octavia / B / 2020 / Aktivan; E88-M-789 / Ford Focus / B / 2019 / Servis (žuti bedž); E31-T-321 / Mercedes Sprinter / C / 2022 / Aktivan; E33-B-654 / VW Crafter / D / 2021 / Aktivan.

**Precizan opis mockup-a (tab "Servisi"):**

- Isti header, pod-tab "Servisi" aktivan; dugme gore desno mijenja se u "+ Novi servis".
- Tabela, kolone: Vozilo, Datum, Vrsta servisa, Status (bedž).
- Redovi: VW Polo / 10.04.2026 / Redovni servis / Završen (zeleni bedž); Ford Focus / 02.05.2026 / Zamjena ulja / U toku (plavi bedž); Škoda Octavia / 20.05.2026 / Tehnički pregled / Zakazan (ljubičasti bedž).

#### 4.1.4. Pregled izvještaja

Administrativni modul za izvještavanje omogućava generisanje i preuzimanje službenih dokumenata u PDF formatu i podijeljen je u dvije glavne sekcije:

1. **Izvještaj o statusu časova vožnje:** Sadrži grafički prikaz odrađenih časova, sa opcijom biranja vremenskog perioda ("Od-Do") i mogućnošću grupisanja i filtriranja podataka po specifičnom instruktoru. Klikom na dugme "Preuzmi PDF", sistem generiše dokument spreman za štampu.
2. **Izvještaj o vozilima i servisima:** Prikazuje historiju održavanja voznog parka, gdje administrator bira kategoriju vozila iz padajućeg menija i definiše period servisa, nakon čega klikom na "Preuzmi PDF" dobija detaljan finansijski i tehnički pregled održavanja automobila.

**Precizan opis mockup-a:**

- Naslov "Izvještaji" + podnaslov "Generisanje i preuzimanje izvještaja".
- Dvije kartice jedna pored druge (split screen 2 kolone):
  - Lijevo: "📄 Izvještaj o statusu časova" (podnaslov "Generiši izvještaj prema filteru"), polja: "Datum od" (date input, "01/01/2026"), "Datum do" (date input, "12/31/2026"), "Instruktor" (dropdown, "Svi instruktori"); dugme "⬇ Preuzmi PDF" na dnu kartice (puna širina).
  - Desno: "🚗 Izvještaj o vozilima" (podnaslov "Pregled stanja voznog parka"), polja: "Kategorija vozila" (dropdown, "Sve kategorije"), "Datum od" (date input, "01/01/2026"), "Datum do" (date input, "12/31/2026"); dugme "⬇ Preuzmi PDF" na dnu kartice.

#### 4.1.5. Upravljanje referentnim podacima

Ovaj ekran je namijenjen za upravljanje šifriranim, statičnim podacima koji standardizuju unose u cijeloj aplikaciji. Podijeljen je na tabove za upravljanje Državama, Gradovima, Kategorijama vozila, Statusima zahtjeva i Tipovima usluga. Na slici je prikazan aktivan tab "Kategorije vozila" koji u tabeli izlistava definisane šifre (A, B, C, D), nazive kategorija i kratak opis (Motocikli, Putnička vozila, Teretna vozila, Autobusi), uz pripadajuće akcije za brzu izmjenu ili brisanje, te dugme "+ Dodaj kategoriju".

**Precizan opis mockup-a:**

- Naslov "Referentni podaci" + podnaslov "Šifarnici i katalozi"; dugme "+ Dodaj unos" gore desno (globalno), pored toga svaki tab ima i vlastito kontekstualno dugme za dodavanje.
- Tabovi (ikona + naziv): "Države", "Gradovi", "Kategorije vozila" (prikazano kao aktivno u 3 varijante mockupa).
- **Tab "Kategorije vozila" (naslov sekcije "Kategorije vozačkih dozvola", dugme "+ Dodaj kategoriju"):** tabela sa kolonama Šifra (kružni bedž: A, B, C, D), Naziv, Opis, Akcije ("Uredi" / "Obriši"). Redovi: A — Motocikli — "Vozila kategorije A - motocikli svih vrsta"; B — Osobna vozila — "Osobni automobili do 3.5t i 8 mjesta"; C — Teretna vozila — "Teretna vozila iznad 3.5t"; D — Autobusi — "Autobusi za prijevoz više od 8 putnika".
- **Tab "Gradovi":** tabela sa kolonama Grad, Poštanski broj, Država. Redovi: Sarajevo/71000/BA, Mostar/88000/BA, Banja Luka/78000/BA, Tuzla/75000/BA.
- **Tab "Države":** tabela sa kolonama Šifra, Naziv. Redovi: BA — Bosna i Hercegovina, HR — Hrvatska, RS — Srbija.
- Napomena: mockup ne prikazuje posebno tabove "Statusi zahtjeva" i "Tipovi usluga" vizuelno, ali su navedeni u tekstualnom opisu — implementirati i njih kao dodatne tabove/CRUD ekrane istog obrasca (RequestStatus, ServiceType), u skladu sa upute.md zahtjevom za CRUD nad svim referentnim podacima.

### 4.2. Mockups Mobile strane (Učenik)

#### 4.2.1. Početni ekran aplikacije

Nakon prijave u mobilnu aplikaciju, učenika dočekuje personalizovani početni ekran sa jasnim i intuitivnim pregledom. Na samom vrhu se nalazi pozdravna poruka sa imenom učenika i ikonom za sistemske notifikacije. Centralni dio ekrana zauzima velika kartica "Sljedeći čas" koja jasno ističe detalje prve naredne zakazane vožnje: datum, tačno vrijeme trajanja, ime dodijeljenog instruktora, zonu vožnje i potvrđeni status časa (Confirmed). Ispod ove kartice pozicioniran je vertikalni meni za brzu navigaciju kroz glavne module aplikacije: Zakazivanje časa, Moji časovi, Probni testovi, Lekcije i Preporuke za učenje. Na samom dnu ekrana nalazi se trajna navigaciona traka (Bottom Nav Bar) sa prepoznatljivim ikonama za brz skok na Početnu, Časove, Testove, Učenje ili Profil.

**Precizan opis mockup-a:**

- Svijetla (light) tema za mobilnu aplikaciju učenika (razlikuje se od tamne desktop administracije).
- Header: ikona auta + "eAutoškola" naziv aplikacije, ikona zvona (notifikacije) desno.
- Pozdrav: "Učenik A".
- Kartica "Sljedeći čas" (plavi gradient): "📅 15.05.2026, 10:00", "👤 Instruktor X", "🚗 VW Polo · Centar", bedž "Potvrđen" (svijetlo-plavi), link "Detalji →" desno.
- Vertikalna lista menija (kartice u boji): "📅 Zakazivanje časa" (plava pozadina), "💳 Finansije i plaćanje" (zelena), "📋 Moji časovi" (ljubičasta), "📝 Testovi iz teorije" (magenta/pink), "💡 Preporučene lekcije" (žuta/narandžasta) — svaka kartica ima ikonu, naziv i strelicu ">" desno.
- Traka sa 3 statistike na dnu sadržaja: "12 Časovi — Odrađenih", "4 Testovi — Riješenih", "72% Prosjek — Na testovima".
- Bottom Nav Bar (5 ikona): Početna, Časovi, Testovi, Učenje, Profil.
- Napomena: mockup liste modula ("Zakazivanje časa", "Finansije i plaćanje", "Moji časovi", "Testovi iz teorije", "Preporučene lekcije") sadrži i "Finansije i plaćanje" koje tekstualni opis ne pominje eksplicitno na ovom ekranu, ali se poklapa sa 4.2.10 — implementirati kao prečicu sa početnog ekrana.

#### 4.2.2. Zakazivanje časa

Ovaj ekran omogućava učeniku samostalno kreiranje novih zahtjeva za časove vožnje. Korisnički interfejs je strukturiran kroz niz padajućih menija (Dropdown) i kontrola koji osiguravaju tačnost unosa podataka: odabir slobodnog instruktora, odabir datuma putem DateTime pickera, te odabir slobodnog termina i željene zone vožnje. Na dnu forme nalazi se tekstualno polje za neobavezne napomene instruktoru (npr. specifično mjesto sastanka). Klikom na istaknuto dugme "Pošalji zahtjev", podaci se šalju na backend gdje prolaze validaciju zauzetosti i ulaze u sistem sa početnim statusom Pending.

**Precizan opis mockup-a:**

- Tamna tema na ovom ekranu (napomena: nekonzistentnost tema light/dark između screenova u originalnoj prijavi — vidjeti CLAUDE.md UI napomenu).
- Header: strelica nazad, naslov "Zakazivanje časa", ikona zvona.
- Podnaslov "Ispunite formu za zakazivanje vožnje".
- Polja (labela velikim slovima iznad svakog inputa): "INSTRUKTOR" (dropdown/select, vrijednost "Instruktor X (Damir K.)"), "DATUM" (date picker input, "05/20/2026"), "TERMIN" (select, "08:00 - 09:00"), "ZONA VOŽNJE" (select, "Centar"), "NAPOMENA" (textarea, placeholder tekst primjer "Ako moze parking da vjezbamo").
- Dugme preko cijele širine na dnu: "Pošalji zahtjev" (svijetlo dugme na tamnoj pozadini).
- Bottom Nav Bar: Početna, Časovi (aktivan), Testovi, Učenje, Profil.

#### 4.2.3. Moji časovi (Historija)

Ekran "Moji časovi" pruža učeniku hronološki i transparentan uvid u sve njegove zahtjeve i odvožene časove, podijeljene kroz tri taba na vrhu: "Aktivni", "Završeni" i "Otkazani". Svaki čas je predstavljen u formi kartice koja sadrži ikonu kalendara, tačan datum i vrijeme, ime instruktora, zonu i istaknuti bedž sa trenutnim statusom iz State Machine logike (Pending - narandžasta, Confirmed - plava, Completed - zelena, Cancelled - crvena). Klikom na bilo koju karticu, učenik se preusmjerava na detaljan pregled tog časa.

**Precizan opis mockup-a:**

- Header: naslov "Moji časovi"; tri taba ispod headera: "Aktivni" / "Završeni" / "Otkazani".
- **Tab "Aktivni":** kartica #1 — "15.05.2026, 10:00 - 11:00", "👤 Instruktor X", "📍 Centar", "🚗 VW Polo", bedž "Potvrđen" (zeleni, gore desno na kartici), dugme "Pogledaj detalje" (puna širina, na dnu kartice); kartica #2 — "18.05.2026, 14:00 - 15:00", "👤 Instruktor Y", "📍 Novi Grad", "🚗 Škoda Octavia", bedž "Na čekanju" (narandžasti/žuti).
- **Tab "Završeni":** kartica — "10.04.2026, 09:00 - 10:00", "👤 Instruktor X", "📍 Stari Grad", "🚗 VW Polo", bedž "Završen" (plavi).
- **Tab "Otkazani":** kartica — "05.04.2026, 16:00 - 17:00", "👤 Instruktor Y", "📍 Centar", "🚗 Škoda Octavia", bedž "Otkazan" (crveni).
- Napomena na bedž boje: mockup koristi Potvrđen=zelena, Na čekanju=narandžasta, Završen=plava, Otkazan=crvena — što se RAZLIKUJE od tekstualnog opisa (Pending-narandžasta, Confirmed-plava, Completed-zelena, Cancelled-crvena). Implementirati finalnu bojnu shemu po dogovoru u fazi UI, ali status-enum nazivi (Pending/Confirmed/Completed/Cancelled) ostaju kako je definisano u upute.md state machine.

#### 4.2.4. Detalji časa

Kada učenik odabere određeni čas iz istorije, otvara se ekran sa detaljnim informacijama o tom zapisu. Na vrhu se nalaze podaci o instruktoru i zakazanom terminu, praćeni bedžom trenutnog statusa (npr. Pending). Središnji dio sadrži uokvirenu sekciju "Informacije o času" sa strukturiranim prikazom datuma, vremena, zone i statusa, dok se ispod nalazi prostor za tekstualne napomene instruktora. Na dnu ekrana učeniku su ponuđene brze akcije "Otkaži zahtjev" ili "Izmijeni termin", u zavisnosti od trenutnog stanja časa i dozvoljenih poslovnih pravila.

**Precizan opis mockup-a:**

- Naslov "Detalji časa".
- Kartica naslova: ikona auta, "Čas vožnje #1", bedž "Potvrđen" (zeleni).
- Lista informacija (ikona + labela + vrijednost, jedna po redu): "📅 Datum i termin — 15.05.2026, 10:00 - 11:00", "📍 Zona vožnje — Centar Sarajeva", "👤 Instruktor — Instruktor X (Damir Kovačević)", "🚗 Vozilo — VW Polo (E22-A-123)", "⏱ Trajanje — ..." (presječeno u mockupu, polje postoji ali vrijednost nije vidljiva).
- Žuta/krem kutija "Napomena: Molimo budite na lokaciji 5 minuta ranije."
- Dugme na dnu: "Otkaži zahtjev" (crveni obrub, crveni tekst, transparentna pozadina).
- Napomena: tekstualni opis spominje i akciju "Izmijeni termin" koja se ne vidi na ovom konkretnom screenshotu (vjerovatno uslovno prikazana zavisno od statusa) — implementirati oba dugmeta prema stanju časa (status Pending/Confirmed dozvoljava otkazivanje i/ili izmjenu termina u skladu sa poslovnim pravilima iz upute.md §7).

#### 4.2.5. Probni testovi (Master)

Ovaj ekran predstavlja "Master" dio master-details forme za teorijsku obuku. Učeniku se prikazuje lista dostupnih probnih ispita koje može polagati ili koje je već riješio. Svaka stavka u listi sadrži naziv testa (npr. Test 1 - Saobraćajni znakovi, Test 2 - Pravila vožnje) i jasan ispis ostvarenog rezultata u formi procenta i broja tačnih odgovora (npr. Rezultat: 70% (7/10)). Za testove koje učenik još uvijek nije pokretao, stoji sivi ispis "Nije riješeno". Klikom na bilo koji aktivni test iz liste, otvara se detaljan pregled pitanja.

**Precizan opis mockup-a:**

- Naslov "Testovi", podnaslov "Vaši rezultati i dostupni testovi".
- Lista kartica testova, svaka sa: naziv testa, bedž rezultata ("Položen" zeleni / "Nije položen" crveni), red "X/10 tačnih · 10 pitanja", progres-bar (zeleni ili crveni ovisno o rezultatu) i procenat na kraju trake.
- Primjeri: "Test 1 - Saobraćajni znakovi" — 7/10 tačnih, 70%, bedž Položen; "Test 2 - Pravila prednosti" — 9/10, 90%, Položen; "Test 3 - Brzine i razmaci" — 5/10, 50%, bedž "Nije položen" (crveni); "Test 4 - Parkiranje" — 8/10 (djelomično vidljivo).
- Bottom nav: Početna, Časovi, Testovi (aktivan), Učenje, Profil.

#### 4.2.6. Rješavanje testa - Pitanje (Details)

Ovaj ekran vizuelno prikazuje "Details" komponentu probnog ispita tokom njegovog stvarnog rješavanja. Na vrhu je jasno naznačen naziv testa i trenutni progres (npr. Pitanje 1/10). Ispod naslova se nalazi tekstualno formulisano pitanje (npr. "Koji znak označava obavezno skretanje udesno?") i uokvireni prostor namijenjen za grafički prikaz (slika raskrsnice, saobraćajne situacije ili znaka). Korisnik daje odgovor odabirom jedne od ponuđenih opcija (A, B, C, D) putem "Radio button" kontrola. Na dnu ekrana se nalaze navigaciona dugmad "Prethodno" i "Sljedeće" za kretanje kroz ispitna pitanja.

**Precizan opis mockup-a:**

- Header: "Pitanje 1 / 3" (napomena: primjer u mockupu ima 3 pitanja, tekst opisuje 1/10 — broj pitanja po testu je konfigurabilan).
- Traka progresa (segmentirana, 3 segmenta, prvi popunjen).
- Uokvireno polje za sliku: placeholder ikona auta + tekst "Slika znaka" (sivi placeholder box).
- Pitanje (bold): "Koji znak označava obavezno skretanje udesno?"
- 4 opcije kao zaobljena dugmad/kartice sa radio-krugom i slovom: A "Zabrana skretanja", B "Obavezno skretanje udesno" (odabrana opcija — bijeli obrub, istaknuta), C "Jednosmjerni saobraćaj", D "Opasna krivina".
- Dugmad na dnu: "< Prethodno" (disabled/sivo na prvom pitanju) i "Sljedeće >" (aktivno, svijetlo dugme).

#### 4.2.7. Rezultat testa

Nakon što učenik završi i pošalje test na serversku validaciju, aplikacija generiše ekran sa detaljnom analizom uspjeha. U gornjem dijelu dominira veliki kružni grafikon koji prikazuje ukupni procenat tačnosti (npr. 70% - 7/10), ispod kojeg su numerički izlistani tačni (7) i netačni (3) odgovori. Uokvirena sekcija "Oblasti za poboljšanje" automatski mapira i ispisuje kategorije pitanja u kojima je učenik napravio greške (npr. Saobraćajni znakovi, Pravila vožnje). Na dnu ekrana se nalaze dva dugmeta: "Pogledaj test" (za analizu netačnih odgovora) i "Preporuke" koje vodi direktno na modul sistema preporuke.

**Precizan opis mockup-a:**

- Naslov "Rezultat testa".
- Veliki kružni (donut) grafikon, zeleni luk, sredina: "70%" / "Rezultat".
- Naziv testa ispod grafikona: "Test 1 - Saobraćajni znakovi", status "Položen".
- Dvije kartice jedna pored druge: zelena kartica sa kvačicom "7 — Tačnih odgovora", crvena kartica sa X "3 — Netačnih odgovora".
- Sekcija "Slabe oblasti:" — lista sa ikonom upozorenja (⚠): "Obavezni i zabranjujući znakovi", "Znakovi opasnosti na putu", "Dopunske table i oznake".
- Dugme na dnu: "Ponovi test" (puna širina, svijetlo).
- Napomena: mockup prikazuje samo dugme "Ponovi test", dok tekstualni opis navodi dugmad "Pogledaj test" i "Preporuke" — implementirati sva tri: "Ponovi test" (restart), "Pogledaj test" (pregled netačnih odgovora) i "Preporuke" (link na Recommender, 4.2.8).

#### 4.2.8. Preporuke za učenje (Recommender)

Ovaj ekran demonstrira rad "Explainable AI" sistema preporuke koji je direktno povezan sa bazom podataka i rezultatima testova. Na vrhu ekrana, pored ikone sijalice, ispisuje se jasno i razumljivo tekstualno obrazloženje: "Preporučujemo ovu lekciju jer ste na testu imali netačne odgovore iz oblasti saobraćajnih znakova". Ispod objašnjenja, algoritam izlistava personalizovane kartice sa preporučenim materijalima za učenje koji direktno gađaju slabe tačke učenika (npr. "Saobraćajni znakovi - Obavezni - 8 min čitanja", "Pravila prvenstva prolaza - 6 min čitanja"), omogućavajući učeniku da jednim klikom otvori potrebnu skriptu.

**Precizan opis mockup-a:**

- Naslov "Preporuke".
- Žuta/krem kartica obrazloženja: ikona sijalice 💡, naslov "Preporuka AI asistenta", tekst: "Preporučujemo ovu lekciju jer ste na testu imali netačne odgovore iz oblasti saobraćajnih znakova. Prolaznost u ovoj oblasti je ispod prosjeka."
- Podnaslov "Preporučene lekcije za čitanje:".
- Lista kartica lekcija (ikona knjige + naziv + metapodaci + strelica ">"): "Obavezni saobraćajni znakovi — Poglavlje 3.1 · 8 min čitanja"; "Zabranjujući znakovi i ograničenja — Poglavlje 3.2 · 10 min čitanja"; "Znakovi opasnosti — Poglavlje 3.3 · 6 min čitanja".
- Bottom nav: Početna, Časovi, Testovi, Učenje (aktivan), Profil.

#### 4.2.9. Pregled lekcija

Ekran sa edukativnim materijalima podijeljen je na tabove "Sve lekcije" i "Moje lekcije". Prikazuje strukturiranu i preglednu listu svih dostupnih teorijskih lekcija koje su ažurirane za tekuću godinu. Svaka stavka u listi sadrži prepoznatljivu ikonu knjige, jasan naziv nastavne jedinice (npr. Saobraćajni znakovi, Pravila vožnje, Situacije u saobraćaju, Prva pomoć, Održavanje vozila) i podatak o ukupnom broju pod-lekcija ili stranica unutar te oblasti, što olakšava sistematično učenje.

**Precizan opis mockup-a:**

- Naslov "Učenje"; dva taba: "Sve lekcije" (aktivan u lijevom screenshotu) / "Moje lekcije" (aktivan u desnom screenshotu).
- **Tab "Sve lekcije":** lista kartica, svaka: ikona knjige + naziv + broj pod-lekcija/stranica + strelica ">". Stavke: "Saobraćajni znakovi — 12 pod-lekcija", "Pravila vožnje — 8 stranica", "Situacije u saobraćaju — 10 pod-lekcija", "Prva pomoć — 5 pod-lekcija", "Održavanje vozila — 6 stranica".
- **Tab "Moje lekcije":** prikazuje samo podskup lekcija koje je učenik otvorio/pratio — u mockupu: "Saobraćajni znakovi — 12 pod-lekcija", "Pravila vožnje — 8 stranica" (odgovara entitetu StudentStudyLesson — evidencija napretka/pristupa po učeniku).

#### 4.2.10. Finansije i plaćanje (PayPal)

Ekran "Finansije i plaćanje" predstavlja centralni finansijski dashboard učenika, dizajniran da pruži transparentan uvid u sve troškove obuke. Ekran je logički podijeljen u tri glavne sekcije:

1. **Pregled troškova:** Detaljna specifikacija svih zaduženja prema autoškoli, koja uključuje stavke poput teorijske obuke, praktične vožnje i troškova administracije i ispita, uz prikaz tačnih iznosa za svaku stavku.
2. **Historija plaćanja:** Hronološki prikaz svih dosadašnjih uplata (rata), gdje svaka stavka jasno indicira uplaćeni iznos i tačan datum procesiranja uplate (npr. Plaćeno 15.04.2026.).
3. **Preostalo za platiti:** Istaknuta kartica koja prikazuje ukupan preostali dug učenika prema autoškoli, sa jasno naznačenim rokom za sljedeću uplatu.

Na samom dnu ekrana pozicionirano je veliko, primarno dugme "Plati sljedeću ratu". Klikom na ovo dugme, aplikacija pokreće integrisani proces online plaćanja, otvarajući sigurno PayPal sandbox okruženje, preko kojeg učenik može izvršiti elektronsku uplatu. Sistem automatski evidentira uspješnu transakciju na backendu i ažurira preostali dug i historiju plaćanja u realnom vremenu.

**Precizan opis mockup-a:**

- Naslov "Finansije i plaćanje".
- Sekcija "🧾 PREGLED TROŠKOVA" (kartice sa nazivom stavke i iznosom desno): "Teorijska obuka (20h) — 200.00 KM", "Praktična vožnja (30h) — 1200.00 KM", "Administracija i ispit — 150.00 KM".
- Sekcija "✅ HISTORIJA PLAĆANJA": "Rata 1 — Plaćeno 15.04.2026. — 350.00 KM", "Rata 2 — Plaćeno 01.05.2026. — 350.00 KM" (iznosi prikazani kao negativni/odbici, zelenom bojom).
- Žuta/krem istaknuta kartica "PREOSTALO ZA PLATITI": "500.00 KM", "Sljedeći rok: 01.06.2026.", ikona novčanika desno.
- Dugme preko cijele širine "Plati sljedeću ratu".
- **Drugi ekran (nakon klika na dugme, PayPal checkout prikaz):** ista stranica skrolana dole, pojavljuje se bijela kartica sa PayPal logom, red "Iznos za plaćanje — 166.67 KM" (= rata, npr. 500/3), žuto dugme "⚡ Plaćanje putem PayPal-a", ispod sitni tekst "🔒 Sigurno plaćanje putem PayPal Sandbox servisa", link "Odustani" na dnu.

#### 4.2.11. Korisnički profil

Ekran "Moj profil" omogućava učeniku upravljanje ličnim podacima i postavkama računa. Na vrhu se nalazi kružni okvir sa profilnom slikom i opcijom za njeno ažuriranje. Ispod su pozicionirana jasno poravnata tekstualna polja za unos i izmjenu osnovnih informacija: ime i prezime, e-mail adresa i broj telefona. Na dnu ekrana nalaze se funkcionalne stavke za navigaciju: "Izmijeni podatke", "Promijeni lozinku" (koja otvara namjenska validacijska polja) i istaknuto dugme "Odjavi se" za siguran izlazak iz aplikacije i uništavanje JWT tokena na klijentu.

**Precizan opis mockup-a:**

- Naslov "Moj profil".
- Kružni avatar sa inicijalima "UA", ime "Učenik A", email "ucenik.a@email.com" ispod.
- Polja (labela + input): "IME I PREZIME" = "Učenik A", "EMAIL ADRESA" = "ucenik.a@email.com", "BROJ TELEFONA" = "+387 61 123 456", "KATEGORIJA" = "B" (kategorija vozačke dozvole koju učenik polaže — FK na VehicleCategory).
- Dva dugmeta jedno pored drugog: "✏ Izmijeni podatke" i "🔑 Promijeni lozinku".
- Klikom na "Promijeni lozinku" otvara se dodatna sekcija "Promjena lozinke" sa poljima: "STARA LOZINKA" (password input sa ikonom oka za prikaz), "NOVA LOZINKA" (password input sa ikonom oka), "POTVRDI LOZINKU" (password input sa ikonom oka), i dugme "Sačuvaj lozinku".
- Dugme na dnu, crveni tekst: "⎋ Odjavi se".

### 4.3. Mockups Mobile strane (Instruktor)

#### 4.3.1. Početni ekran instruktora

Početni ekran za instruktore pruža brz i sažet uvid u njihove dnevne obaveze na terenu. Nakon pozdravne poruke, centralno mjesto zauzima uokvirena kartica "Danas" koja ispisuje ukupan broj zakazanih časova za taj dan (npr. 3 zakazana časa) uz pripadajuće dugme "Pogledaj raspored". Ispod toga nalazi se vertikalni meni za upravljanje poslovnim procesima: Zahtjevi za časove (uz jasan indikator broja novih zahtjeva), Moji časovi (raspored), Učenici (popis kandidata), Izvještaji i Profil. Dno ekrana je opremljeno trajnom navigacionom trakom sa prepoznatljivim ikonama za lak rad u pokretu.

**Precizan opis mockup-a:**

- Tamna tema (instruktorski mobilni interfejs koristi dark theme, za razliku od svijetle teme kod učenika na početnom ekranu — implementirati konzistentnu temu po aplikaciji/ulozi u fazi dizajna, uz poštovanje upute.md pravila o čitljivosti).
- Header: ikona auta + "eAutoškola".
- Pozdrav: "Dobro jutro, Instruktor X" (avatar inicijali "IX").
- Kartica "Danas" (plavi gradient): "Danas, 15. maj 2026.", "3 zakazana časa", "10:00 · 12:00 · 16:00", bedž "2 novih zahtjeva" (narandžasti).
- Tri trake menija (sa bojom pozadine i ikonom): "🔔 Zahtjevi za časove" (žuta pozadina) + crveni brojčani bedž "2" + strelica; "📅 Moj raspored" (plava pozadina) + strelica; "👥 Moji učenici" (zelena pozadina) + strelica.
- Sekcija "Sljedeći časovi danas" (lista): "09:00 Učenik C — Završeno" (plavi bedž), "10:00 Učenik A — Odobreno" (zeleni bedž), "12:00 Učenik B — Odobreno" (zeleni bedž).
- Bottom Nav Bar (4 ikone, NE 5): Početna, Raspored, Učenici, Profil. (Napomena: tekstualni opis pominje i "Izvještaji" u vertikalnom meniju — na screenshotu nema posebne "Izvještaji" stavke niti nav-ikone; implementirati kao dodatnu stavku unutar Profil ekrana ili kao petu nav ikonu po potrebi.)

#### 4.3.2. Zahtjevi za časove

Ovaj ekran omogućava instruktoru upravljanje dolaznim zahtjevima za vožnju koje su učenici kreirali. Na vrhu se nalaze filteri za brzo razvrstavanje zahtjeva: "Sve", "Pending", "Odobreni" i "Odbijeni", sa brojačem pored svakog statusa. Zahtjevi su prikazani kao kartice koje sadrže ime učenika, tačan datum, vrijeme i zonu vožnje, te istaknuti bedž sa trenutnim statusom (npr. Pending ili Confirmed). Na karticama koje su u stanju Pending, instruktoru su direktno ponuđena dva brza akcijska dugmeta: "Odobri" i "Odbij", čime se mijenja stanje entiteta na backendu.

**Precizan opis mockup-a:**

- Naslov "Zahtjevi za časove".
- Filter chips (zaobljena dugmad): "Sve" (aktivan/odabran), "Na čekanju", "Odobreni" (mockup prikazuje 2 vidljiva chip-a plus "Sve"; tekstualni opis navodi i "Odbijeni" — implementirati sva 4 filtera: Sve / Pending / Odobreni / Odbijeni).
- Kartica #1: "Učenik A", "15.05.2026 · 10:00 - 11:00", "📍 Centar", bedž "Na čekanju" (narandžasti), citat napomene učenika u kurzivu: "\"Molim da vježbamo parkiranje.\"", dva dugmeta: "✕ Odbij" (crveni obrub) / "✓ Odobri" (zeleno puno dugme).
- Kartica #2: "Učenik B", "16.05.2026 · 12:00 - 13:00", "📍 Novi Grad", bedž "Odobreno" (zeleni), link "Pogledaj detalje zahtjeva".
- Kartica #3: "Učenik C", "14.05.2026 · 09:00 - 10:00", "📍 Stari Grad", bedž "Odobreno", link "Pogledaj detalje zahtjeva".

#### 4.3.3. Detalji zahtjeva

Kada instruktor klikne na pojedinačni zahtjev učenika, otvara se ekran sa kompletnim specifikacijama tog zapisa. Gornji dio sadrži karticu sa profilnom slikom učenika, njegovim imenom, brojem telefona i e-mail adresom radi lakšeg stupanja u kontakt. Središnji dio, pod nazivom "Informacije o času", prikazuje tačan datum, definisano vrijeme, zonu i trenutni status zahtjeva. Na dnu se nalazi uokvireni prostor u kojem instruktor može pročitati napomene koje je učenik ostavio prilikom zakazivanja, kao i dva velika dugmeta "✓ Odobri" i "X Odbij" za konačno procesiranje zahtjeva.

**Precizan opis mockup-a:**

- Naslov "Detalji zahtjeva".
- Kartica učenika: avatar inicijal "C", "Učenik C", email "ucenik.c@email.com", telefon "+387 61 234 567".
- Lista informacija (ikona + labela + vrijednost): "📅 Datum i termin — 14.05.2026, 09:00 - 10:00", "📍 Zona vožnje — Stari Grad", "🚗 Vozilo — VW Polo (E22-A-123)", "🔢 Broj časa — 18. čas".
- Sekcija "Napomena učenika:" (kurziv): "\"Molim da vježbamo parkiranje i kružni tok.\"".
- Dva dugmeta na dnu: "✕ Odbij" (crveni obrub, lijevo) i "✓ Potvrdi čas" (zeleno puno dugme, desno). Napomena: mockup koristi natpis "Potvrdi čas", dok tekstualni opis navodi "Odobri" — implementirati akciju kao potvrdu termina (status → Confirmed) bez obzira na tačan naziv dugmeta; UI labela treba biti jedna od ove dvije varijante, konzistentno kroz aplikaciju.

#### 4.3.4. Moji časovi (Raspored / Kalendar)

Ovaj ekran predstavlja digitalni raspored instruktora organizovan u obliku kalendara. Na vrhu se nalazi horizontalna traka sa danima u sedmici i datumima, gdje instruktor dodirom na određeni dan (npr. četvrtak, 15.) učitava hronološku listu časova za taj specifičan datum. Časovi su poređani po satnicama (npr. 10:00 - 11:00, 14:00 - 15:00, 16:00 - 17:00), prikazuju ime učenika, zonu vožnje i bedž sa statusom časa. Klikom na čas koji je u statusu Confirmed, instruktor otvara formu za unošenje rezultata i završetak vožnje.

**Precizan opis mockup-a:**

- Naslov "Raspored".
- "Maj 2026" + horizontalna traka 7 dana (Pon–Ned sa datumima 12–18), aktivni dan "Čet 15" istaknut (bijeli krug).
- Datumski podnaslov: "Čet, 15. maj 2026."
- Vremenska lista (satnica lijevo, kartica desno):
  - "09:00 — Učenik C, 📍 Stari Grad, bedž 'Završeno' (plavi), ✓ Čas završen (zeleni tekst)".
  - "10:00 — Učenik A, 📍 Centar, bedž 'Odobreno' (zeleni), dugme '■ Završi čas' (bijelo/sivo dugme preko cijele širine kartice)".
  - "12:00 — Učenik B, 📍 Novi Grad, bedž 'Odobreno', dugme '▶ Počni čas' (zeleno puno dugme)".
  - "14:00 — Slobodan termin" (isprekidani okvir, placeholder za prazan slot).
- Napomena: mockup uvodi dvostepenu akciju "Počni čas" → "Završi čas" za vožnju u statusu Confirmed, što je UI-only tok (ne uvodi novi status u state machine iz upute.md — status i dalje prelazi direktno Confirmed → Completed kada instruktor sačuva rezultat na ekranu 4.3.5; "Počni čas"/"Završi čas" mogu biti čisto vizuelni koraci na klijentu ili evidentirati pomoćno vrijeme početka/kraja unutar zapisa Lesson, bez dodatnog statusa).

#### 4.3.5. Završetak časa i ocjenjivanje kandidata

Ovaj ekran se otvara kada instruktor završi termin vožnje sa učenikom i služi za konačnu evidenciju obuke. Na vrhu su prikazani osnovni podaci o odrađenom času (Učenik, Datum, Vrijeme, Zona). Centralni dio sadrži interaktivnu kontrolu sa 5 zvjezdica ("Ocjena učenika") gdje instruktor dodirom dodjeljuje ocjenu za pokazano znanje na tom času. Ispod ocjene nalazi se veliko tekstualno polje "Bilješke" u koje instruktor upisuje detaljan komentar o vještinama koje je učenik savladao ili stavkama koje treba popraviti. Na dnu se nalazi padajući meni za promjenu statusa časa u Completed i dugme "Sačuvaj" koje trajno upisuje podatke u bazu i zatvara transakciju.

**Precizan opis mockup-a:**

- Prikazano kao modal/bottom-sheet (zaobljeni gornji uglovi, "handle" traka na vrhu): naslov "Završetak časa" + dugme "X" za zatvaranje u gornjem desnom uglu.
- Kartica učenika: avatar "U", "Učenik A", "10:00", "Centar".
- Sekcija "Ocjena učenika": 5 praznih zvjezdica (☆☆☆☆☆, interaktivne) + tekst "Kliknite za ocjenu".
- Textarea "BILJEŠKE O ČASU", placeholder "Upišite bilješke o napretku učenika, greškama, itd...".
- Dropdown "STATUS ČASA" = "Završen — Čas odrađen".
- Dugme preko cijele širine: "💾 Sačuvaj".

## 5. Opis Web servisa i arhitekture

Glavni web servis je zasnovan na REST arhitekturi razvijenoj putem .NET Core Web API tehnologije uz programski jezik C#, dok se za skladištenje podataka koristi relacioni Microsoft SQL Server. Komunikacija s bazom podataka se oslanja na Entity Framework Core koji obezbjeđuje referencijalni integritet preko stranih ključeva.

Sistem je postavljen kroz mikroservisnu arhitekturu koja uključuje dva potpuno odvojena servisa u Docker kontejnerima definisana unutar docker-compose.yml datoteke:

1. **Glavni API servis:** Upravlja poslovnom logikom, vrši server-side validacije korisničkih unosa i provjerava autorizaciju putem JWT tokena.
2. **Pomoćni (Worker) servis:** Pokreće se u zasebnom kontejneru i pozadinskom procesu. On asinhrono osluškuje poruke poslate sa glavnog servisa preko RabbitMQ message brokera i izvršava stvarne zadatke kao što je slanje email obavijesti učenicima (npr. kada im instruktor potvrdi ili odbije termin vožnje).

## 6. Sistem preporuke (Recommender System)

Sistem preporuke predstavlja naprednu komponentu edukacijskog modula aplikacije "eAutoškola", kreiranu sa ciljem da učenicima pruži personalizovano iskustvo učenja. Zasniva se na content-based filtering metodi koja kontinuirano analizira rezultate probnih testova učenika i uspoređuje ih sa dostupnim lekcijama u bazi podataka.

Specifični algoritam koji će biti implementiran za izračunavanje podudarnosti jeste algoritam kosinusne sličnosti (Cosine Similarity).

### 6.1. Analitički podaci kao osnova sistema

Za razliku od jednostavnih sistema koji učenicima samo preporučuju lekcije hronološkim redom, naš sistem gradi preporuke na osnovu specifičnih edukacijskih metrika. Podaci koji ulaze u recommender se evidentiraju u tabelama RezultatTesta i Pitanje. Algoritam obrađuje sljedeće varijable:

- **Stopa netačnosti (Error Rate):** Procenat netačnih odgovora unutar svake specifične oblasti (npr. "Raskrsnice", "Saobraćajni znakovi", "Prva pomoć"). Oblast sa najvećom stopom netačnosti dobija najveći prioritet.
- **Frekvencija grešaka (Error Frequency):** Broj ponavljanja iste greške kroz više različitih testova. Ako učenik konstantno griješi na istom tipu pitanja, algoritam tu oblast označava kao "kritičnu".

> Napomena (glosar engleskih naziva za implementaciju): RezultatTesta = `TestAttempt`, Pitanje = `Question`, oblast pitanja = `QuestionArea`. Vidjeti CLAUDE.md za kompletan glosar naziva entiteta.

### 6.2. Algoritam Kosinusne sličnosti

Algoritam kosinusne sličnosti (Cosine Similarity) funkcioniše tako što kreira dva vektora. Prvi vektor predstavlja "Korisnički profil grešaka" (izračunat na osnovu gore navedenih metrika iz RezultatTesta), dok drugi vektor predstavlja "Metapodatke lekcija" (oznake/tagovi koje svaka lekcija nosi u bazi). Izračunavanjem ugla između ova dva vektora, algoritam pronalazi lekciju čiji se sadržaj najviše podudara (ima najveću kosinusnu sličnost) sa oblastima u kojima je učenik ostvario najlošije rezultate, te je pozicionira na vrh liste preporuka.

### 6.3. Objašnjiva preporuka (Explainable AI)

Kako bi se povećalo povjerenje korisnika u sistem i ubrzao proces učenja, implementiran je mehanizam objašnjive preporuke (Explainable AI). To znači da aplikacija ne izbacuje samo sirovu listu linkova, već učeniku ispisuje generisano tekstualno obrazloženje koje objašnjava zašto mu je algoritam ponudio taj sadržaj.

Primjer generisanog obrazloženja: "Preporučujemo vam da ponovite lekciju 'Prvenstvo prolaza na raskrsnicama' jer ste na posljednjem probnom testu imali 75% netačnih odgovora iz ove specifične kategorije."

## 7. Zaključak

Aplikacija "eAutoškola" pruža kompletno, zaokruženo i visoko efikasno softversko rješenje koje pokriva ključne segmente poslovanja moderne autoškole. Kroz napredne mehanizme poput centralizovane state machine logike za upravljanje terminima, asinhronog sistema obavještavanja i inteligentnog sistema za preporuku obrazovnog sadržaja, aplikacija podiže nivo kvaliteta obuke i pruža vrhunsko korisničko iskustvo svim profilima korisnika. Savremena mikroservisna arhitektura i Docker kontejnerizacija garantuju stabilnost, skalabilnost i lako održavanje sistema u produkcijskom okruženju.
