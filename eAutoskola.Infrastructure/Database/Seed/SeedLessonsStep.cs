using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Payments;
using eAutoskola.Domain.Resources;

namespace eAutoskola.Infrastructure.Database.Seed;

/// <summary>
/// Lessons in every status with their status history (the audit trail). Nothing overlaps: the
/// step books instructor, student and vehicle per hour before it creates a lesson, exactly like
/// the API's overlap rule. Demo accounts get the lessons the mockups show (the student's "next
/// lesson", the instructor's day), the rest is spread over the year so the dashboard chart has
/// a shape.
/// </summary>
internal sealed class SeedLessonsStep
{
    private static readonly string[] StudentNotes =
    [
        "Molim da vježbamo parkiranje.", "Želim vježbati vožnju u gradu.", "Treba mi vježba kružnog toka.",
        "Ako može, polazimo od parkinga.", "Vježba uzbrdice i kretanja.", "Voljela bih više vožnje kroz raskrsnice.",
    ];

    private static readonly string[] InstructorNotes =
    [
        "Dobra vožnja, treba poraditi na paralelnom parkiranju.", "Sigurno upravljanje, bolje pratiti ogledala.",
        "Odlično na raskrsnicama, treba oprez pri kretanju na uzbrdici.", "Napredak u kružnom toku, ponoviti polukružno okretanje.",
        "Pravilno držanje razmaka, vježbati ulazak u garažu.", "Dobra koordinacija kvačila, treba stabilnije kočenje.",
    ];

    private static readonly string[] CancelReasons =
    [
        "Vozilo nije dostupno u terminu.", "Instruktor je spriječen, termin se prebacuje.", "Učenik je otkazao zbog obaveza.",
        "Nepovoljni vremenski uslovi.", "Otkazano na zahtjev učenika.",
    ];

    private readonly SeedPeople _people;
    private readonly DrivingZone[] _zones;
    private readonly DateTime _now;
    private readonly DateTime _today;

    private readonly Dictionary<string, User[]> _instructorsByCategory;
    private readonly Dictionary<string, Vehicle[]> _activeVehiclesByCategory;
    private readonly Dictionary<User, string> _studentCategory = [];
    private readonly Vehicle _polo;
    private readonly Vehicle _octavia;

    private readonly HashSet<(User, DateTime)> _instructorBusy = [];
    private readonly HashSet<(User, DateTime)> _studentBusy = [];
    private readonly HashSet<(Vehicle, DateTime)> _vehicleBusy = [];

    private readonly List<Lesson> _lessons = [];
    private readonly List<LessonStatusHistory> _history = [];
    private int _sequence;

    private SeedLessonsStep(SeedPeople people, SeedFleet fleet, DrivingZone[] zones, DateTime now)
    {
        _people = people;
        _zones = zones;
        _now = now;
        _today = now.Date;

        _instructorsByCategory = people.Instructors
            .Select((user, i) => (user, SeedFleetAndPeopleData.Instructors[i].Category))
            .GroupBy(x => x.Category)
            .ToDictionary(g => g.Key, g => g.Select(x => x.user).ToArray());

        _activeVehiclesByCategory = fleet.Vehicles
            .Select((vehicle, i) => (vehicle, SeedFleetAndPeopleData.Vehicles[i].Category))
            .Where(x => x.vehicle.Status == VehicleStatus.Active)
            .GroupBy(x => x.Category)
            .ToDictionary(g => g.Key, g => g.Select(x => x.vehicle).ToArray());

        for (var i = 0; i < people.Students.Length; i++)
        {
            _studentCategory[people.Students[i]] = SeedFleetAndPeopleData.Students[i].Category;
        }

        _polo = fleet.Vehicles[0];
        _octavia = fleet.Vehicles[1];
    }

    public static async Task<SeedLessons> RunAsync(
        EAutoskolaDbContext context, SeedReference reference, SeedPeople people, SeedFleet fleet, DateTime now, CancellationToken cancellationToken)
    {
        var step = new SeedLessonsStep(people, fleet, reference.Zones, now);
        var (refunded, upcoming) = step.Generate();

        step.AssignLessonNumbers();
        foreach (var lesson in step._lessons)
        {
            lesson.RequestStatus = reference.RequestStatuses[lesson.Status.ToString()];
        }
        context.Lessons.AddRange(step._lessons);
        context.LessonStatusHistories.AddRange(step._history);
        await context.SaveChangesAsync(cancellationToken);

        return new SeedLessons(step._lessons, refunded, upcoming);
    }

    private (Lesson Refunded, Lesson Upcoming) Generate()
    {
        var students = _people.Students;
        var instructors = _people.Instructors;
        var admin = _people.Admins[0];

        var damir = instructors[1];
        var sanja = instructors[2];
        var demoInstructor = instructors[0];
        var demoStudent = students[0];
        var adnan = students[4];

        // --- Adnan: already past the 30-lesson package quota, with the two extra-lesson cases the
        // payment flow needs (a paid-then-refunded one and an upcoming unpaid one) ---
        for (var i = 0; i < PaymentConstants.PackagePracticalLessonQuota; i++)
        {
            var start = _today.AddDays(-90 + i * 2).AddHours(9);
            Book(adnan, i % 2 == 0 ? damir : sanja, _polo, _zones[0], start, LessonStatus.Completed, _sequence++, createdAt: start.AddDays(-3));
        }

        var refundedStart = _today.AddDays(-12).AddHours(11);
        var refundedLesson = Book(adnan, sanja, _polo, _zones[0], refundedStart, LessonStatus.Cancelled, _sequence++,
            createdAt: refundedStart.AddDays(-6), cancelReason: "Otkazano od strane administracije.", cancelledBy: admin, confirmedFirst: true);

        var upcomingStart = _today.AddDays(5).AddHours(10);
        var upcomingLesson = Book(adnan, damir, _octavia, _zones[2], upcomingStart, LessonStatus.Confirmed, _sequence++, createdAt: _now.AddDays(-2));

        // --- mobile (the demo student): 12 completed lessons, the next confirmed one, a pending request and a cancelled one ---
        int[] demoCompletedDaysAgo = [4, 9, 14, 19, 26, 33, 41, 50, 60, 72, 85, 100];
        for (var i = 0; i < demoCompletedDaysAgo.Length; i++)
        {
            PlaceLesson(demoStudent, _today.AddDays(-demoCompletedDaysAgo[i]).AddHours(9 + i % 6), LessonStatus.Completed, _sequence++, backwards: true);
        }

        Book(demoStudent, damir, _polo, _zones[0], _today.AddDays(2).AddHours(10), LessonStatus.Confirmed, _sequence++, createdAt: _now.AddDays(-1));
        Book(demoStudent, demoInstructor, _octavia, _zones[1], _today.AddDays(4).AddHours(14), LessonStatus.Pending, _sequence++, createdAt: _now.AddHours(-5), note: StudentNotes[0]);
        Book(demoStudent, sanja, _polo, _zones[0], _today.AddDays(-6).AddHours(16), LessonStatus.Cancelled, _sequence++,
            createdAt: _today.AddDays(-9), cancelReason: CancelReasons[2], cancelledBy: demoStudent, confirmedFirst: true);

        // --- the demo instructor's day (dashboard table, instructor home): lessons in the next hours + two fresh requests ---
        var nextFullHour = new DateTime(_now.Year, _now.Month, _now.Day, _now.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        Book(students[1], demoInstructor, _polo, _zones[0], nextFullHour, LessonStatus.Confirmed, _sequence++, createdAt: _now.AddDays(-2));
        Book(students[2], demoInstructor, _octavia, _zones[2], nextFullHour.AddHours(2), LessonStatus.Confirmed, _sequence++, createdAt: _now.AddDays(-2));
        Book(students[5], demoInstructor, _activeVehiclesByCategory["B"][2], _zones[1], nextFullHour.AddHours(4), LessonStatus.Pending, _sequence++, createdAt: _now.AddHours(-3), note: StudentNotes[1]);
        Book(students[12], demoInstructor, _activeVehiclesByCategory["B"][3], _zones[0], _today.AddDays(1).AddHours(9), LessonStatus.Pending, _sequence++, createdAt: _now.AddHours(-8), note: StudentNotes[2]);
        Book(students[14], demoInstructor, _activeVehiclesByCategory["B"][4], _zones[3], _today.AddDays(1).AddHours(11), LessonStatus.Pending, _sequence++, createdAt: _now.AddHours(-1), note: StudentNotes[4]);

        // --- everyone else: history over the year, then what is booked for the next two weeks ---
        var historyStudents = students
            .Where((student, i) => i != 0 && i != 4 && i < students.Length - SeedFleetAndPeopleData.NewStudentCount)
            .ToArray();

        for (var k = 0; k < 150; k++)
        {
            var student = historyStudents[k % historyStudents.Length];
            var status = k % 6 == 5 ? LessonStatus.Cancelled : LessonStatus.Completed;
            var preferred = _today.AddDays(-(4 + k * 17 % 270)).AddHours(8 + k * 7 % 10);
            var earliest = student.CreatedAt.Date.AddDays(3 + k % 9);
            if (preferred < earliest)
            {
                preferred = earliest.AddHours(8 + k * 7 % 10);
            }

            PlaceLesson(student, preferred, status, _sequence++, backwards: true);
        }

        var activeStudents = historyStudents.Where(s => s.IsActive).ToArray();
        for (var k = 0; k < 38; k++)
        {
            var student = activeStudents[k * 5 % activeStudents.Length];
            var status = k % 8 < 5 ? LessonStatus.Confirmed : LessonStatus.Pending;
            PlaceLesson(student, _today.AddDays(1 + k * 3 % 13).AddHours(8 + k * 5 % 10), status, _sequence++, backwards: false);
        }

        foreach (var newcomer in students.Skip(students.Length - SeedFleetAndPeopleData.NewStudentCount))
        {
            PlaceLesson(newcomer, _today.AddDays(3 + _sequence % 4).AddHours(9 + _sequence % 6), LessonStatus.Pending, _sequence++, backwards: false);
        }

        return (refundedLesson, upcomingLesson);
    }

    /// <summary>Finds a free instructor / vehicle / hour at or near <paramref name="preferred"/>
    /// (moving away from today, Sundays skipped) and books the lesson there.</summary>
    private void PlaceLesson(User student, DateTime preferred, LessonStatus status, int seed, bool backwards)
    {
        var category = _studentCategory[student];
        var instructorPool = _instructorsByCategory[category];
        var vehiclePool = _activeVehiclesByCategory[category];
        var direction = backwards ? -1 : 1;

        for (var attempt = 0; attempt < 120; attempt++)
        {
            var hourSlot = (preferred.Hour - 8 + attempt) % 10;
            var dayShift = (preferred.Hour - 8 + attempt) / 10;
            var start = preferred.Date.AddDays(direction * dayShift).AddHours(8 + hourSlot);
            if (start.DayOfWeek == DayOfWeek.Sunday)
            {
                continue;
            }

            var instructor = instructorPool[(seed + attempt) % instructorPool.Length];
            var vehicle = vehiclePool[(seed / 2 + attempt) % vehiclePool.Length];
            if (IsFree(instructor, student, vehicle, start))
            {
                var createdAt = start > _now ? _now.AddHours(-(2 + seed % 60)) : start.AddDays(-3 - seed % 3);
                var note = seed % 3 == 0 && status != LessonStatus.Cancelled ? StudentNotes[seed % StudentNotes.Length] : null;
                Book(student, instructor, vehicle, _zones[seed % _zones.Length], start, status, seed, createdAt, note: note,
                    cancelReason: status == LessonStatus.Cancelled ? CancelReasons[seed % CancelReasons.Length] : null,
                    cancelledBy: status == LessonStatus.Cancelled ? (seed % 5 < 3 ? instructor : student) : null,
                    confirmedFirst: status == LessonStatus.Cancelled && seed % 5 >= 3);
                return;
            }
        }
    }

    private bool IsFree(User instructor, User student, Vehicle vehicle, DateTime start)
        => !_instructorBusy.Contains((instructor, start))
            && !_studentBusy.Contains((student, start))
            && !_vehicleBusy.Contains((vehicle, start));

    /// <summary>Creates the lesson and its history rows. A cancelled lesson is either rejected while
    /// pending (by the instructor) or cancelled after confirmation (<paramref name="confirmedFirst"/>).</summary>
    private Lesson Book(
        User student, User instructor, Vehicle vehicle, DrivingZone zone, DateTime start, LessonStatus status, int seed, DateTime createdAt,
        string? note = null, string? cancelReason = null, User? cancelledBy = null, bool confirmedFirst = false)
    {
        _instructorBusy.Add((instructor, start));
        _studentBusy.Add((student, start));
        _vehicleBusy.Add((vehicle, start));

        var lesson = new Lesson
        {
            Student = student,
            Instructor = instructor,
            Vehicle = vehicle,
            DrivingZone = zone,
            StartsAtUtc = start,
            EndsAtUtc = start.AddHours(1),
            Status = status,
            Note = note,
            CreatedAt = createdAt,
        };

        if (status == LessonStatus.Completed)
        {
            lesson.Rating = 3 + seed % 3;
            lesson.InstructorNotes = InstructorNotes[seed % InstructorNotes.Length];
            lesson.StartedAt = start;
            lesson.CompletedAt = start.AddHours(1);
        }
        else if (status == LessonStatus.Cancelled)
        {
            lesson.RejectionReason = cancelReason;
        }

        // The instructor answers a day later - or half an hour later for a request made recently.
        var answeredAt = createdAt.AddDays(1) < _now ? createdAt.AddDays(1) : createdAt.AddMinutes(30);

        Record(lesson, LessonStatus.Pending, LessonStatus.Pending, student, Messages.HistoryLessonRequested, createdAt);
        if (status is LessonStatus.Confirmed or LessonStatus.Completed || (status == LessonStatus.Cancelled && confirmedFirst))
        {
            Record(lesson, LessonStatus.Pending, LessonStatus.Confirmed, instructor, Messages.HistoryLessonConfirmed, answeredAt);
        }

        switch (status)
        {
            case LessonStatus.Completed:
                Record(lesson, LessonStatus.Confirmed, LessonStatus.Completed, instructor, Messages.HistoryLessonCompleted, start.AddHours(1));
                break;
            case LessonStatus.Cancelled:
                Record(lesson, confirmedFirst ? LessonStatus.Confirmed : LessonStatus.Pending, LessonStatus.Cancelled,
                    cancelledBy ?? instructor, cancelReason ?? Messages.HistoryLessonCancelled, answeredAt.AddDays(1) < _now ? answeredAt.AddDays(1) : answeredAt.AddMinutes(30));
                break;
        }

        _lessons.Add(lesson);
        return lesson;
    }

    private void Record(Lesson lesson, LessonStatus from, LessonStatus to, User changedBy, string reason, DateTime at)
        => _history.Add(new LessonStatusHistory { Lesson = lesson, FromStatus = from, ToStatus = to, ChangedByUser = changedBy, Reason = reason, ChangedAt = at });

    /// <summary>"Broj časa": the student's n-th lesson in time order.</summary>
    private void AssignLessonNumbers()
    {
        foreach (var group in _lessons.GroupBy(l => l.Student))
        {
            var number = 1;
            foreach (var lesson in group.OrderBy(l => l.StartsAtUtc))
            {
                lesson.LessonNumber = number++;
            }
        }
    }
}
