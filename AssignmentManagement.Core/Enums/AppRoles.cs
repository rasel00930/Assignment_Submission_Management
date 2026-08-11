namespace AssignmentManagement.Core.Enums;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string AdminOrTeacher = Admin + "," + Teacher;

    public static readonly IReadOnlyCollection<string> All = new[] { Admin, Teacher, Student };
}
