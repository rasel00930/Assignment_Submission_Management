namespace AssignmentManagement.Core.Interfaces;

public interface ICurrentUserService
{
    long UserId { get; }
    long InstitutionId { get; }
    long? AcademicClassId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
