using AssignmentManagement.Core.Interfaces;

namespace AssignmentManagement.Service.Implementation;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
