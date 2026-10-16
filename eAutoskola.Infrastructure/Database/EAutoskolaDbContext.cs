using eAutoskola.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eAutoskola.Infrastructure.Database;

public class EAutoskolaDbContext : DbContext
{
    public EAutoskolaDbContext(DbContextOptions<EAutoskolaDbContext> options) : base(options)
    {
    }

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<InstructorProfile> InstructorProfiles => Set<InstructorProfile>();

    // Reference tables
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<VehicleCategory> VehicleCategories => Set<VehicleCategory>();
    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();
    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<VehicleServiceType> VehicleServiceTypes => Set<VehicleServiceType>();
    public DbSet<DrivingZone> DrivingZones => Set<DrivingZone>();
    public DbSet<QuestionArea> QuestionAreas => Set<QuestionArea>();

    // Vehicles
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleService> VehicleServices => Set<VehicleService>();

    // Lessons
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonStatusHistory> LessonStatusHistories => Set<LessonStatusHistory>();

    // Finance
    public DbSet<Charge> Charges => Set<Charge>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Refund> Refunds => Set<Refund>();

    // Notifications & auth tokens
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Theory tests
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<AnswerOption> AnswerOptions => Set<AnswerOption>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();

    // Study lessons
    public DbSet<StudyLesson> StudyLessons => Set<StudyLesson>();
    public DbSet<StudyLessonSection> StudyLessonSections => Set<StudyLessonSection>();
    public DbSet<StudentStudyLesson> StudentStudyLessons => Set<StudentStudyLesson>();
    public DbSet<RecommendationLog> RecommendationLogs => Set<RecommendationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EAutoskolaDbContext).Assembly);
    }
}
