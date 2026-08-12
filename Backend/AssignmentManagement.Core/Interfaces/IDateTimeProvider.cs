namespace AssignmentManagement.Core.Interfaces;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
