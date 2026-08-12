using AssignmentManagement.Core.Exceptions;

namespace AssignmentManagement.Core.Rules;

public static class AssignmentRules
{
    public static void ValidateMaximumMarks(decimal maximumMarks)
    {
        if (maximumMarks <= 0 || maximumMarks > 10000)
            throw new AppException(400, "Maximum marks must be between 0.01 and 10000.");
    }

    public static void ValidateDeadline(DateTime deadlineUtc, DateTime nowUtc)
    {
        if (deadlineUtc.Kind != DateTimeKind.Utc)
            throw new AppException(400, "Deadline must be sent as UTC time.");

        if (deadlineUtc <= nowUtc)
            throw new AppException(400, "Deadline must be in the future.");
    }
}
