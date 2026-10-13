namespace eAutoskola.Domain.Security;

public static class Role
{
    public const string Admin = "Admin";
    public const string Instructor = "Instructor";
    public const string Student = "Student";

    public static readonly IReadOnlyList<string> All = [Admin, Instructor, Student];
}
