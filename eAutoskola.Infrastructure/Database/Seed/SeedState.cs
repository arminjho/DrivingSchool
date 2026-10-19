using eAutoskola.Domain.Entities;

namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>Saved reference rows the later seeding steps point at.</summary>
internal sealed record SeedReference(
    City[] Cities,
    Dictionary<string, VehicleCategory> Categories,
    Dictionary<string, RequestStatus> RequestStatuses,
    ServiceType[] ServiceTypes,
    VehicleServiceType[] VehicleServiceTypes,
    DrivingZone[] Zones,
    QuestionArea[] QuestionAreas);

/// <summary>The seeded users, in the same order as SeedFleetAndPeopleData's arrays.</summary>
internal sealed record SeedPeople(User[] Admins, User[] Instructors, User[] Students);

/// <summary>The seeded vehicles, in the same order as SeedFleetAndPeopleData.Vehicles.</summary>
internal sealed record SeedFleet(Vehicle[] Vehicles);

/// <summary>What the lesson step created. The two Adnan lessons are the payment scenarios the
/// finance step attaches charges to (README "Provjera plaćanja").</summary>
internal sealed record SeedLessons(List<Lesson> All, Lesson AdnanRefundedLesson, Lesson AdnanUpcomingLesson);
