using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Exceptions;

namespace AssignmentManagement.Core.Rules;

public static class SubmissionRules
{
    public static bool IsLateSubmissionAllowed(
        bool institutionAllowsLateSubmission,
        bool assignmentAllowsLateSubmission) =>
        institutionAllowsLateSubmission && assignmentAllowsLateSubmission;

    public static SubmissionStatus GetInitialStatus(DateTime deadlineUtc, DateTime nowUtc, bool allowLateSubmission)
    {
        if (nowUtc <= deadlineUtc)
            return SubmissionStatus.Submitted;

        if (!allowLateSubmission)
            throw new AppException(409, "The assignment deadline has passed.");

        return SubmissionStatus.Late;
    }

    public static void ValidateResubmission(
        bool allowResubmission,
        bool allowStudentUpdate,
        DateTime deadlineUtc,
        DateTime nowUtc,
        SubmissionStatus currentStatus)
    {
        if (!allowResubmission || !allowStudentUpdate)
            throw new AppException(409, "Updating this submission is not allowed.");

        if (nowUtc > deadlineUtc)
            throw new AppException(409, "A submission cannot be updated after the deadline.");

        if (currentStatus == SubmissionStatus.Graded)
            throw new AppException(409, "A graded submission cannot be updated.");
    }

    public static void ValidateReview(
        decimal? marks,
        decimal maximumMarks,
        string? feedback,
        SubmissionStatus status,
        bool requireFeedbackForGrading)
    {
        var allowed = status is SubmissionStatus.UnderReview or SubmissionStatus.Graded or SubmissionStatus.Returned;
        if (!allowed)
            throw new AppException(400, "Review status must be UnderReview, Graded, or Returned.");

        if (status == SubmissionStatus.Graded && marks is null)
            throw new AppException(400, "Marks are required when a submission is graded.");

        if (marks.HasValue && (marks.Value < 0 || marks.Value > maximumMarks))
            throw new AppException(400, $"Marks must be between 0 and {maximumMarks}.");

        if (status == SubmissionStatus.Returned && string.IsNullOrWhiteSpace(feedback))
            throw new AppException(400, "Feedback is required when a submission is returned.");

        if (status == SubmissionStatus.Graded && requireFeedbackForGrading && string.IsNullOrWhiteSpace(feedback))
            throw new AppException(400, "Feedback is required when a submission is graded.");
    }
}
