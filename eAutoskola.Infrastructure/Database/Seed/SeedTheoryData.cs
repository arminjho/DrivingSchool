namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>One theory question with its four options; <paramref name="Correct"/> is the index of
/// the right option. <paramref name="AreaIndex"/> refers to
/// <see cref="SeedFleetAndPeopleData.QuestionAreas"/>.</summary>
internal sealed record SeedQuestion(string Text, string[] Options, int Correct, int AreaIndex, string? Image = null);

internal sealed record SeedTest(string Title, int AreaIndex, SeedQuestion[] Questions);

/// <summary>A study lesson and its sections ("pod-lekcije"); <paramref name="Sections"/> are the
/// section titles, the text is generated per area.</summary>
internal sealed record SeedStudyLesson(string Title, int AreaIndex, string Tags, int ReadingMinutes, string Chapter, string[] Sections);

/// <summary>Theory content of the demo database: six tests of ten questions each (as in the
/// "7/10" results of the mockups) and the study lessons the recommender chooses from.</summary>
internal static class SeedTheoryData
{
    public static readonly SeedTest[] Tests =
    [
        new("Test 1 - Saobraćajni znakovi", 0,
        [
            new("Koji znak označava obavezno skretanje udesno?", ["Zabrana skretanja", "Obavezno skretanje udesno", "Jednosmjerni saobraćaj", "Opasna krivina"], 1, 0, "sign-mandatory-right-turn.png"),
            new("Šta označava trougaoni znak sa crvenim okvirom?", ["Obavezu", "Zabranu", "Opasnost", "Obavještenje"], 2, 0, "sign-danger.png"),
            new("Okrugli znak sa plavom pozadinom označava:", ["Zabranu", "Opasnost", "Obavezu", "Prvenstvo prolaza"], 2, 0, "sign-roundabout.png"),
            new("Znak 'Stop' zahtijeva:", ["Usporavanje", "Potpuno zaustavljanje", "Promjenu trake", "Ubrzanje"], 1, 0, "sign-stop.png"),
            new("Dopunska tabla ispod znaka obično označava:", ["Dodatno pojašnjenje znaka", "Novi znak", "Prestanak važenja prethodnog znaka", "Ništa posebno"], 0, 0),
            new("Znak 'Ustupi prvenstvo prolaza' ima oblik:", ["Kruga", "Obrnutog trougla", "Kvadrata", "Osmougla"], 1, 0, "sign-yield.png"),
            new("Koji znak zabranjuje parkiranje?", ["Plavi krug sa slovom P", "Krug sa crvenim okvirom i kosom crtom na plavoj podlozi", "Žuti trougao", "Zeleni pravougaonik"], 1, 0, "sign-no-parking.png"),
            new("Znak pješačkog prelaza je obično u obliku:", ["Plavog kvadrata sa pješakom", "Crvenog kruga", "Žutog osmougla", "Bijelog trougla"], 0, 0, "sign-pedestrian.png"),
            new("Znakovi obavještenja su najčešće:", ["Crveni trouglovi", "Žuti krugovi", "Plavi pravougaonici", "Zeleni krugovi"], 2, 0),
            new("Koji znak upozorava na opasnu krivinu?", ["Crveni krug", "Trougao sa prikazom krivine", "Plavi pravougaonik", "Žuti krug"], 1, 0, "sign-curve.png"),
        ]),
        new("Test 2 - Pravila prednosti", 1,
        [
            new("Ko ima prvenstvo prolaza na raskrsnici bez signalizacije?", ["Vozilo sa lijeve strane", "Vozilo sa desne strane", "Brže vozilo", "Veće vozilo"], 1, 1),
            new("Na raskrsnici sa znakom 'Put sa prvenstvom prolaza' prvenstvo ima:", ["Vozilo sa sporednog puta", "Vozilo na putu sa prvenstvom prolaza", "Vozilo sa desne strane", "Vozilo koje prvo stigne"], 1, 0, "sign-priority-road.png"),
            new("U kružnom toku prvenstvo ima:", ["Vozilo koje ulazi", "Vozilo koje je već u kružnom toku", "Vozilo veće mase", "Vozilo koje skreće ulijevo"], 1, 1),
            new("Vozilo sa uključenim plavim rotacionim svjetlom i sirenom:", ["Mora čekati u koloni", "Ima prvenstvo prolaza uz dužnu pažnju ostalih učesnika", "Nema prednost nikada", "Ima prednost samo noću"], 1, 1),
            new("Pješaku na obilježenom pješačkom prelazu vozač mora:", ["Pojačati svjetla", "Zaustaviti vozilo i propustiti pješaka", "Zatrubiti i nastaviti", "Povećati brzinu"], 1, 1),
            new("Na raskrsnici sa semaforom čije žuto svjetlo trepće:", ["Obavezno se zaustavlja", "Prolazi se uz povećan oprez i po pravilima prvenstva", "Prolazi se punom brzinom", "Skreće se samo udesno"], 1, 1),
            new("Kada se dva vozila susretnu na uskom putu na strmom usponu, prvenstvo ima:", ["Vozilo koje ide nizbrdo", "Vozilo koje ide uzbrdo", "Teže vozilo", "Brže vozilo"], 1, 1),
            new("Tramvaj u odnosu na ostala vozila:", ["Ima prvenstvo prolaza po pravilu", "Nikad nema prvenstvo", "Ima prvenstvo samo na stanicama", "Uvijek čeka na raskrsnici"], 0, 1),
            new("Vozilo koje skreće ulijevo mora propustiti:", ["Vozilo iz suprotnog smjera koje ide pravo", "Samo biciklistu", "Samo pješaka", "Nikoga"], 0, 1),
            new("Pri uključivanju u saobraćaj sa privatnog posjeda vozač:", ["Ima prvenstvo prolaza", "Mora propustiti sve učesnike u saobraćaju na putu", "Propušta samo desno vozilo", "Nema posebnih obaveza"], 1, 1),
        ]),
        new("Test 3 - Brzine i razmaci", 1,
        [
            new("Najveća dozvoljena brzina u naselju, osim ako nije drugačije označeno, je:", ["50 km/h", "60 km/h", "70 km/h", "80 km/h"], 0, 1),
            new("Izvan naselja, osim ako nije drugačije označeno, najveća brzina automobila je:", ["60 km/h", "80 km/h", "100 km/h", "120 km/h"], 1, 1),
            new("Na autoputu najveća dozvoljena brzina automobila je:", ["100 km/h", "120 km/h", "130 km/h", "140 km/h"], 1, 1, "sign-speed-limit.png"),
            new("Sigurnosni razmak između vozila zavisi od:", ["Boje vozila", "Brzine kretanja i uslova na putu", "Marke vozila", "Broja putnika"], 1, 1),
            new("'Pravilo dvije sekunde' služi za procjenu:", ["Brzine vozila", "Sigurnosnog razmaka", "Potrošnje goriva", "Prvenstva prolaza"], 1, 1),
            new("Na mokrom kolovozu zaustavni put vozila se:", ["Skraćuje", "Produžava", "Ne mijenja", "Prepolovljava"], 1, 1),
            new("Brzinu kretanja vozač mora prilagoditi:", ["Samo ograničenju na znaku", "Uslovima na putu, vidljivosti i stanju vozila", "Samo vozaču iza sebe", "Samo najbržem vozilu"], 1, 1),
            new("Pri vožnji po magli vozač je dužan:", ["Uključiti dugo svjetlo", "Smanjiti brzinu i uključiti odgovarajuća svjetla", "Povećati brzinu", "Isključiti svjetla"], 1, 1),
            new("Na kolovozu sa ledom sigurnosni razmak treba:", ["Smanjiti", "Znatno povećati", "Ostaviti isti", "Zanemariti"], 1, 1),
            new("Zaustavni put čine:", ["Samo put kočenja", "Put reakcije i put kočenja", "Samo put reakcije", "Dužina vozila"], 1, 1),
        ]),
        new("Test 4 - Parkiranje", 2,
        [
            new("Parkiranje na pješačkom prelazu je:", ["Dozvoljeno do 5 minuta", "Zabranjeno", "Dozvoljeno noću", "Dozvoljeno uz upaljene pokazivače pravca"], 1, 2),
            new("Najmanja udaljenost za parkiranje od raskrsnice je:", ["5 metara", "Bez ograničenja", "1 metar", "Samo preko puta"], 0, 2),
            new("Paralelno parkiranje podrazumijeva vozilo postavljeno:", ["Paralelno ivici kolovoza", "Okomito na ivicu", "Pod uglom od 45°", "Na sredini puta"], 0, 2),
            new("Pri parkiranju na nizbrdici točkove treba okrenuti:", ["Prema sredini puta", "Prema ivičnjaku", "Ravno", "Svejedno je"], 1, 2),
            new("Parkiranje ispred ulaza u garažu je:", ["Dozvoljeno", "Zabranjeno", "Dozvoljeno ako vozač ostaje u vozilu", "Dozvoljeno samo noću"], 1, 2),
            new("Znak sa plavom podlogom i slovom P označava:", ["Zabranu parkiranja", "Parkiralište", "Policijsku stanicu", "Pješački prelaz"], 1, 0, "sign-parking.png"),
            new("Prije izlaska iz vozila na ulicu vozač treba:", ["Odmah otvoriti vrata", "Provjeriti da li nailazi vozilo ili biciklista", "Zatrubiti", "Uključiti sva svjetla"], 1, 2),
            new("Parkiranje na biciklističkoj stazi je:", ["Dozvoljeno kratko", "Zabranjeno", "Dozvoljeno vikendom", "Dozvoljeno za motocikle"], 1, 2),
            new("Ručna kočnica se pri parkiranju:", ["Ne koristi", "Povlači", "Koristi samo zimi", "Koristi samo na uzbrdici"], 1, 2),
            new("Zabrana zaustavljanja i parkiranja označava se:", ["Plavim krugom", "Crvenim krugom sa ukrštenim crtama na plavoj podlozi", "Žutim trouglom", "Zelenim pravougaonikom"], 1, 0, "sign-no-stopping.png"),
        ]),
        new("Test 5 - Prva pomoć", 3,
        [
            new("Prvi korak na mjestu saobraćajne nezgode je:", ["Pomjeriti povrijeđenog", "Osigurati mjesto nezgode", "Fotografisati vozila", "Napustiti mjesto"], 1, 3),
            new("Jedinstveni evropski broj za hitne službe je:", ["112", "911", "999", "150"], 0, 3),
            new("Kod jakog krvarenja iz rane postavlja se:", ["Kompresivni zavoj", "Led", "Mast", "Ništa"], 0, 3),
            new("Povrijeđenog koji je bez svijesti, a diše, treba:", ["Napojiti vodom", "Postaviti u bočni stabilni položaj", "Posjesti", "Pustiti da hoda"], 1, 3),
            new("Omjer vanjske masaže srca i udaha pri oživljavanju odraslih je:", ["15:2", "30:2", "5:1", "10:1"], 1, 3),
            new("Kacigu povrijeđenom motociklisti:", ["Skida se odmah", "Ne skida se osim ako je neophodno", "Skida je prolaznik", "Skida se uvijek"], 1, 3),
            new("Opekotinu treba hladiti:", ["Hladnom vodom", "Maslacem", "Uljem", "Ledom direktno"], 0, 3),
            new("Trougao upozorenja postavlja se:", ["Iza vozila na propisanoj udaljenosti", "Ispred vozila na 1 metar", "Na krov vozila", "Ne postavlja se"], 0, 3),
            new("Komplet prve pomoći u vozilu:", ["Mora postojati i biti kompletan", "Nije obavezan", "Obavezan samo za kamione", "Obavezan samo zimi"], 0, 3),
            new("Povrijeđenog iz vozila treba vaditi:", ["Uvijek odmah", "Samo ako postoji neposredna opasnost", "Nikada", "Samo uz pomoć policije"], 1, 3),
        ]),
        new("Test 6 - Održavanje vozila", 4,
        [
            new("Najmanja dozvoljena dubina profila ljetnih guma je:", ["0,5 mm", "1,6 mm", "3 mm", "5 mm"], 1, 4),
            new("Pritisak u gumama se provjerava:", ["Na zagrijanim gumama", "Na hladnim gumama", "Samo ljeti", "Nikada"], 1, 4),
            new("Svjetlo ulja na instrument tabli znači:", ["Nizak pritisak ulja - zaustaviti vozilo", "Zamjena guma", "Pun rezervoar", "Uključena klima"], 0, 4),
            new("Nivo rashladne tečnosti se provjerava:", ["Na vrućem motoru", "Na ohlađenom motoru", "Tokom vožnje", "Nije potrebno"], 1, 4),
            new("Motorno ulje se mijenja:", ["Prema uputstvu proizvođača", "Svake godine bez obzira na pređene kilometre", "Nikada", "Samo kad pukne"], 0, 4),
            new("Istrošene kočione pločice:", ["Ne utiču na kočenje", "Produžavaju zaustavni put", "Skraćuju zaustavni put", "Poboljšavaju kočenje"], 1, 4),
            new("Akumulator treba održavati:", ["Provjerom stanja i polova", "Nikada", "Pranjem vodom", "Hlađenjem"], 0, 4),
            new("Brisači vjetrobrana se mijenjaju:", ["Kada ostavljaju tragove", "Nikada", "Svake sedmice", "Samo ljeti"], 0, 4),
            new("Tehnički pregled vozila obavlja se:", ["Jednom u životu", "Periodično prema propisima", "Samo pri kupovini", "Nikada"], 1, 4),
            new("Svjetlo za maglu pozadi se uključuje:", ["Uvijek", "Pri slaboj vidljivosti zbog magle", "Pri kiši", "Danju"], 1, 4),
        ]),
    ];

    /// <summary>Titles follow docs/prijava.md 4.2.8 / 4.2.9. Tags deliberately name other areas
    /// where the lesson overlaps them, so the recommender's tag signal has something to use.</summary>
    public static readonly SeedStudyLesson[] StudyLessons =
    [
        new("Saobraćajni znakovi", 0, "saobraćajni znakovi, zabrana, obaveza, opasnost", 15, "Poglavlje 3",
            ["Uvod u saobraćajne znakove", "Znakovi opasnosti", "Znakovi izričitih naredbi", "Znakovi obavještenja", "Dopunske table", "Horizontalna signalizacija",
             "Svjetlosni znakovi (semafori)", "Znakovi na putu pod radovima", "Znakovi za biciklistički saobraćaj", "Znakovi za pješake", "Znakovi za parkiranje", "Provjera znanja"]),
        new("Obavezni saobraćajni znakovi", 0, "obaveza, obavezno skretanje, saobraćajni znakovi", 8, "Poglavlje 3.1",
            ["Značenje obaveznih znakova", "Obavezan smjer kretanja", "Obavezno zaustavljanje", "Kružni tok"]),
        new("Zabranjujući znakovi i ograničenja", 0, "zabrana, ograničenje brzine, zabrana parkiranja, saobraćajni znakovi", 10, "Poglavlje 3.2",
            ["Zabrane prolaza", "Zabrane preticanja", "Ograničenja brzine", "Zabrane zaustavljanja i parkiranja"]),
        new("Znakovi opasnosti", 0, "opasnost, krivina, saobraćajni znakovi", 6, "Poglavlje 3.3",
            ["Znakovi opasnih krivina", "Znakovi opasnih mjesta na putu", "Znakovi vremenskih opasnosti"]),
        new("Pravila vožnje", 1, "pravila vožnje, prvenstvo prolaza, brzina", 10, "Poglavlje 2",
            ["Osnovna pravila kretanja", "Vožnja desnom stranom", "Mimoilaženje", "Preticanje", "Brzina kretanja", "Sigurnosni razmak", "Korištenje svjetala", "Provjera znanja"]),
        new("Prvenstvo prolaza na raskrsnicama", 1, "raskrsnice, prvenstvo prolaza, saobraćajni znakovi, situacije u saobraćaju", 6, "Poglavlje 2.1",
            ["Raskrsnica bez znakova", "Raskrsnica sa znakovima", "Raskrsnica sa semaforom"]),
        new("Brzine i sigurnosni razmaci", 1, "brzina, razmak, zaustavni put, pravila vožnje", 7, "Poglavlje 2.2",
            ["Ograničenja brzine", "Zaustavni put", "Sigurnosni razmak"]),
        new("Situacije u saobraćaju", 2, "raskrsnice, situacije, pješaci", 12, "Poglavlje 4",
            ["Vožnja kroz naselje", "Vožnja van naselja", "Vožnja noću", "Vožnja po kiši", "Vožnja po magli", "Vožnja po snijegu i ledu", "Vožnja kroz tunel", "Pješaci i biciklisti", "Kružni tok", "Hitne situacije"]),
        new("Parkiranje i zaustavljanje", 2, "parkiranje, zaustavljanje, saobraćajni znakovi", 5, "Poglavlje 4.2",
            ["Gdje je parkiranje zabranjeno", "Paralelno parkiranje", "Parkiranje na nagibu"]),
        new("Prva pomoć", 3, "prva pomoć, povrede, hitna pomoć", 9, "Poglavlje 5",
            ["Mjesto nezgode", "Pregled povrijeđenog", "Krvarenje", "Oživljavanje", "Bočni stabilni položaj"]),
        new("Održavanje vozila", 4, "održavanje, motor, gume, tečnosti", 7, "Poglavlje 6",
            ["Dnevna provjera vozila", "Gume", "Kočnice", "Svjetla", "Tečnosti", "Akumulator"]),
        new("Gume i kočnice", 4, "gume, kočnice, održavanje vozila", 4, "Poglavlje 6.1",
            ["Pritisak i profil guma", "Znaci istrošenih kočnica"]),
    ];

    /// <summary>Generic but on-topic text for a section, by question area.</summary>
    public static string SectionContent(int areaIndex, string lessonTitle, string sectionTitle)
    {
        var paragraph = areaIndex switch
        {
            0 => "Saobraćajni znakovi su podijeljeni u grupe prema značenju, a vozač je dužan poštovati ih bez obzira na vlastitu procjenu situacije. Oblik i boja znaka govore o njegovoj vrsti.",
            1 => "Pravila saobraćaja određuju redoslijed kretanja učesnika. Vozač prilagođava brzinu i razmak uslovima na putu i uvijek ostavlja dovoljno prostora za zaustavljanje.",
            2 => "U složenim saobraćajnim situacijama vozač prvo procjenjuje okolinu, zatim bira brzinu i putanju te pravovremeno daje znak pokazivačima pravca.",
            3 => "Pri pružanju prve pomoći najprije se osigurava mjesto nezgode, zatim se poziva hitna služba i pruža pomoć u granicama osposobljenosti.",
            _ => "Redovno održavanje vozila smanjuje rizik od kvara u vožnji. Gume, kočnice, svjetla i nivo tečnosti provjeravaju se prema uputstvu proizvođača.",
        };
        return $"{sectionTitle} ({lessonTitle}). {paragraph}";
    }
}
