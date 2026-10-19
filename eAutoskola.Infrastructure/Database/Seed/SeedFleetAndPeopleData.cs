using eAutoskola.Domain.Enums;

namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>One seeded person. <paramref name="Login"/> overrides the e-mail for the demo accounts
/// (desktop / mobile / instruktor sign in with a short name); everyone else gets
/// firstname.lastname@eautoskola.ba.</summary>
internal sealed record SeedPerson(string FirstName, string LastName, string? Login, int CityIndex, string Category, bool Photo, bool Active = true);

internal sealed record SeedVehicle(string RegistrationNumber, string Make, string Model, string Category, int Year, VehicleStatus Status, string? Photo);

/// <summary>A maintenance record; <paramref name="DaysFromToday"/> is relative to the seeding day so
/// the data stays plausible whenever the database is created.</summary>
internal sealed record SeedMaintenance(int VehicleIndex, int TypeIndex, int DaysFromToday, string Description, decimal Cost, VehicleServiceStatus Status);

/// <summary>Names, vehicles and maintenance for the demo database (values follow the mockups in
/// docs/prijava.md where it shows concrete rows). Category codes are VehicleCategory.Code.</summary>
internal static class SeedFleetAndPeopleData
{
    public static readonly (string Code, string Name)[] Countries =
    [
        ("BA", "Bosna i Hercegovina"),
        ("HR", "Hrvatska"),
        ("RS", "Srbija"),
    ];

    /// <summary>(Name, PostalCode, CountryCode)</summary>
    public static readonly (string Name, string PostalCode, string CountryCode)[] Cities =
    [
        ("Sarajevo", "71000", "BA"),
        ("Mostar", "88000", "BA"),
        ("Banja Luka", "78000", "BA"),
        ("Tuzla", "75000", "BA"),
        ("Zenica", "72000", "BA"),
        ("Bijeljina", "76300", "BA"),
        ("Brčko", "76100", "BA"),
        ("Travnik", "72270", "BA"),
        ("Prijedor", "79101", "BA"),
        ("Trebinje", "89101", "BA"),
        ("Zagreb", "10000", "HR"),
        ("Beograd", "11000", "RS"),
    ];

    public static readonly (string Code, string Name, string Description)[] VehicleCategories =
    [
        ("A", "Motocikli", "Vozila kategorije A - motocikli svih vrsta"),
        ("B", "Osobna vozila", "Osobni automobili do 3.5t i 8 mjesta"),
        ("C", "Teretna vozila", "Teretna vozila iznad 3.5t"),
        ("D", "Autobusi", "Autobusi za prijevoz više od 8 putnika"),
        ("E", "Skupovi vozila", "Vučna vozila sa priključnim vozilom"),
    ];

    public static readonly string[] VehicleServiceTypes =
    [
        "Redovni servis", "Zamjena ulja", "Tehnički pregled", "Zamjena guma", "Popravka kočnica", "Zamjena kvačila", "Servis klima uređaja",
    ];

    public static readonly string[] DrivingZones = ["Centar", "Stari Grad", "Novi Grad", "Ilidža", "Otoka", "Dobrinja"];

    public static readonly string[] QuestionAreas =
    [
        "Saobraćajni znakovi", "Pravila vožnje", "Situacije u saobraćaju", "Prva pomoć", "Održavanje vozila",
    ];

    public static readonly SeedPerson[] Admins =
    [
        new("Admin", "Korisnik", "desktop", 0, "B", false),
        new("Mirela", "Tahić", null, 0, "B", true),
    ];

    /// <summary>Index 0 is the demo instructor account. Each instructor teaches one category.</summary>
    public static readonly SeedPerson[] Instructors =
    [
        new("Instruktor", "Test", "instruktor", 0, "B", false),
        new("Damir", "Kovačević", null, 0, "B", true),
        new("Sanja", "Perić", null, 1, "B", true),
        new("Selma", "Omerović", null, 3, "B", true),
        new("Haris", "Delić", null, 0, "C", true),
        new("Tarik", "Hadžić", null, 4, "A", true),
    ];

    /// <summary>Index 0 is the demo student account, index 1-4 are the students the README and
    /// the mockups mention by name. The last <see cref="NewStudentCount"/> registered this month.</summary>
    public static readonly SeedPerson[] Students =
    [
        new("Mobile", "Učenik", "mobile", 0, "B", false),
        new("Amina", "Hodžić", null, 0, "B", true),
        new("Emir", "Mustafić", null, 1, "B", false),
        new("Lejla", "Bašić", null, 0, "B", false, Active: false),
        new("Adnan", "Zukić", null, 1, "B", true),
        new("Mirza", "Begić", null, 2, "B", true),
        new("Nermina", "Salihović", null, 3, "B", false),
        new("Kenan", "Ibrahimović", null, 0, "A", true),
        new("Lamija", "Čolić", null, 1, "B", false),
        new("Almir", "Hrustić", null, 4, "B", true),
        new("Emina", "Kurtović", null, 0, "B", false),
        new("Tarik", "Mehmedović", null, 5, "C", false),
        new("Azra", "Dedić", null, 0, "B", true),
        new("Haris", "Šabanović", null, 3, "B", false),
        new("Ajla", "Karić", null, 1, "B", true),
        new("Dino", "Halilović", null, 2, "B", false),
        new("Belma", "Smajlović", null, 0, "B", false),
        new("Armin", "Pašić", null, 6, "A", true),
        new("Selma", "Alić", null, 0, "B", false),
        new("Jasmin", "Delić", null, 7, "B", true),
        new("Merima", "Žujo", null, 0, "B", false),
        new("Faruk", "Bajramović", null, 1, "B", false),
        new("Ilma", "Rizvanović", null, 3, "B", true),
        new("Dženana", "Spahić", null, 2, "B", false),
    ];

    public const int NewStudentCount = 4;

    public static readonly SeedVehicle[] Vehicles =
    [
        new("E22-A-123", "VW", "Polo", "B", 2021, VehicleStatus.Active, "vehicle-vw-polo.jpg"),
        new("E54-K-456", "Škoda", "Octavia", "B", 2020, VehicleStatus.Active, "vehicle-skoda-octavia.jpg"),
        new("E88-M-789", "Ford", "Focus", "B", 2019, VehicleStatus.InService, "vehicle-ford-focus.jpg"),
        new("E19-O-221", "Opel", "Corsa", "B", 2022, VehicleStatus.Active, "vehicle-opel-corsa.jpg"),
        new("E27-R-338", "Renault", "Clio", "B", 2021, VehicleStatus.InService, "vehicle-renault-clio.jpg"),
        new("E45-T-554", "Toyota", "Yaris", "B", 2023, VehicleStatus.Active, "vehicle-toyota-yaris.jpg"),
        new("E61-D-772", "Dacia", "Sandero", "B", 2020, VehicleStatus.Active, "vehicle-dacia-sandero.jpg"),
        new("E73-H-409", "Hyundai", "i20", "B", 2022, VehicleStatus.Active, "vehicle-hyundai-i20.jpg"),
        new("E90-P-615", "Peugeot", "208", "B", 2023, VehicleStatus.Active, "vehicle-peugeot-208.jpg"),
        new("E11-H-101", "Honda", "CB500F", "A", 2021, VehicleStatus.Active, "vehicle-honda-cb500f.jpg"),
        new("E12-Y-207", "Yamaha", "MT-07", "A", 2022, VehicleStatus.Active, "vehicle-yamaha-mt-07.jpg"),
        new("E31-T-321", "Mercedes", "Sprinter", "C", 2022, VehicleStatus.Active, "vehicle-mercedes-sprinter.jpg"),
        new("E36-M-880", "MAN", "TGL", "C", 2020, VehicleStatus.InService, "vehicle-man-tgl.jpg"),
        new("E33-B-654", "VW", "Crafter", "D", 2021, VehicleStatus.Active, "vehicle-vw-crafter.jpg"),
        new("E38-B-217", "Mercedes", "Tourismo", "D", 2019, VehicleStatus.Active, "vehicle-mercedes-tourismo.jpg"),
        new("E41-S-905", "DAF", "XF", "E", 2021, VehicleStatus.Active, "vehicle-daf-xf.jpg"),
    ];

    /// <summary>Vehicle indexes refer to <see cref="Vehicles"/>, type indexes to
    /// <see cref="VehicleServiceTypes"/>. The three InProgress records belong to the three
    /// vehicles seeded as InService.</summary>
    public static readonly SeedMaintenance[] Maintenance =
    [
        new(0, 0, -190, "Redovni servis - zamjena filtera i tekućina", 120.00m, VehicleServiceStatus.Completed),
        new(0, 2, 40, "Godišnji tehnički pregled", 80.00m, VehicleServiceStatus.Scheduled),
        new(1, 1, -95, "Zamjena motornog ulja i filtera", 85.00m, VehicleServiceStatus.Completed),
        new(1, 2, 25, "Godišnji tehnički pregled", 80.00m, VehicleServiceStatus.Scheduled),
        new(2, 1, -2, "Zamjena motornog ulja i filtera", 60.00m, VehicleServiceStatus.InProgress),
        new(2, 0, -140, "Redovni servis - veliki servis", 210.00m, VehicleServiceStatus.Completed),
        new(3, 3, -60, "Zamjena ljetnih guma zimskim", 220.00m, VehicleServiceStatus.Completed),
        new(4, 4, -1, "Zamjena kočionih pločica i diskova", 340.00m, VehicleServiceStatus.InProgress),
        new(5, 0, -30, "Redovni servis - 15.000 km", 135.00m, VehicleServiceStatus.Completed),
        new(6, 2, 12, "Tehnički pregled", 80.00m, VehicleServiceStatus.Scheduled),
        new(7, 6, -75, "Punjenje i dezinfekcija klima uređaja", 55.00m, VehicleServiceStatus.Completed),
        new(8, 0, 18, "Redovni servis - 10.000 km", 110.00m, VehicleServiceStatus.Scheduled),
        new(9, 1, -45, "Zamjena ulja i lanca", 95.00m, VehicleServiceStatus.Completed),
        new(10, 3, -110, "Zamjena prednje i zadnje gume", 180.00m, VehicleServiceStatus.Completed),
        new(11, 0, -20, "Redovni servis - 30.000 km", 290.00m, VehicleServiceStatus.Completed),
        new(12, 5, 0, "Zamjena kvačila", 780.00m, VehicleServiceStatus.InProgress),
        new(13, 2, 33, "Tehnički pregled autobusa", 150.00m, VehicleServiceStatus.Scheduled),
        new(14, 4, -85, "Pregled i zamjena kočnica", 410.00m, VehicleServiceStatus.Completed),
        new(15, 0, -160, "Redovni servis vučnog vozila", 520.00m, VehicleServiceStatus.Completed),
    ];

    /// <summary>"Ime.prezime@eautoskola.ba" without diacritics, or the short demo login.</summary>
    public static string LoginFor(SeedPerson person)
        => person.Login ?? $"{Fold(person.FirstName)}.{Fold(person.LastName)}@eautoskola.ba";

    /// <summary>File name of the seeded profile image, matching what SeedAssets contains.</summary>
    public static string PhotoFileFor(SeedPerson person)
        => $"profile-{Fold(person.FirstName)}-{Fold(person.LastName)}.jpg";

    /// <summary>"+387 6X XXX XXX" - always matches the phone rule the API validates.</summary>
    public static string PhoneFor(int ordinal)
        => $"+387 6{ordinal % 4 + 1} {(123 + ordinal * 37) % 900 + 100} {(456 + ordinal * 53) % 900 + 100}";

    private static string Fold(string text)
        => text.ToLowerInvariant().Replace('č', 'c').Replace('ć', 'c').Replace('š', 's').Replace('ž', 'z').Replace('đ', 'd');
}
