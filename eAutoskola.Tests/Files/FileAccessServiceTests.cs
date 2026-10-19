using eAutoskola.Application.Files;
using eAutoskola.Domain.Entities;
using eAutoskola.Domain.Enums;
using eAutoskola.Domain.Exceptions;
using eAutoskola.Domain.Security;
using eAutoskola.Tests.Payments;

namespace eAutoskola.Tests.Files;

/// <summary>Who may download which stored image (authorization + ownership, upute.md §5).</summary>
public class FileAccessServiceTests
{
    private const string StudentPhoto = "uploads/student.jpg";
    private const string OtherStudentPhoto = "uploads/other.jpg";
    private const string InstructorPhoto = "uploads/instructor.jpg";
    private const string VehiclePhoto = "uploads/vehicle.jpg";
    private const string QuestionImage = "uploads/question.png";
    private const string OrphanFile = "uploads/orphan.jpg";

    private static (FileAccessService Service, PaymentTestFixture F) Create(int callerId, string role)
    {
        var f = new PaymentTestFixture(callerId, role);
        f.Context.Users.Find(PaymentTestFixture.StudentId)!.ProfileImagePath = StudentPhoto;
        f.Context.Users.Find(PaymentTestFixture.OtherStudentId)!.ProfileImagePath = OtherStudentPhoto;
        f.Context.Users.Find(PaymentTestFixture.InstructorId)!.ProfileImagePath = InstructorPhoto;
        f.Context.Vehicles.Add(new Vehicle { Id = 1, RegistrationNumber = "E22-A-123", Make = "VW", Model = "Polo", VehicleCategoryId = 1, ManufactureYear = 2021, ImagePath = VehiclePhoto });
        f.Context.Questions.Add(new Question { Id = 1, Text = "Pitanje", QuestionAreaId = 1, TestId = 1, ImagePath = QuestionImage });
        f.Context.SaveChanges();
        return (new FileAccessService(f.Context, f.CurrentUser.Object), f);
    }

    [Fact]
    public async Task Admin_MayReadAnyFile()
    {
        var (service, _) = Create(99, Role.Admin);

        await service.EnsureCanReadAsync(StudentPhoto, default);
        await service.EnsureCanReadAsync(OrphanFile, default);
    }

    [Fact]
    public async Task Student_MayReadOwnPhoto_InstructorPhotos_AndSharedTrainingImages()
    {
        var (service, _) = Create(PaymentTestFixture.StudentId, Role.Student);

        await service.EnsureCanReadAsync(StudentPhoto, default);
        await service.EnsureCanReadAsync(InstructorPhoto, default);
        await service.EnsureCanReadAsync(VehiclePhoto, default);
        await service.EnsureCanReadAsync(QuestionImage, default);
    }

    [Fact]
    public async Task Student_MayNotReadAnotherStudentsPhoto_OrAFileNoRecordPointsTo()
    {
        var (service, _) = Create(PaymentTestFixture.StudentId, Role.Student);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.EnsureCanReadAsync(OtherStudentPhoto, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => service.EnsureCanReadAsync(OrphanFile, default));
    }

    [Fact]
    public async Task Instructor_MayReadAStudentsPhotoOnlyWhenTheyTeachThatStudent()
    {
        var (service, f) = Create(PaymentTestFixture.InstructorId, Role.Instructor);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.EnsureCanReadAsync(StudentPhoto, default));

        f.AddLesson(DateTime.UtcNow.AddDays(2), LessonStatus.Pending, PaymentTestFixture.StudentId);
        await service.EnsureCanReadAsync(StudentPhoto, default);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.EnsureCanReadAsync(OtherStudentPhoto, default));
    }

    [Fact]
    public async Task BackslashesInThePathAreNormalizedBeforeTheCheck()
    {
        var (service, _) = Create(PaymentTestFixture.StudentId, Role.Student);

        await service.EnsureCanReadAsync(StudentPhoto.Replace('/', '\\'), default);
    }
}
