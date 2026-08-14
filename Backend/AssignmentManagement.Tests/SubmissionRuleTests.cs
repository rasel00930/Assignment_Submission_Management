using Xunit;
using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Core.Rules;

namespace AssignmentManagement.Tests;

public sealed class SubmissionRuleTests
{
    private static readonly DateTime Deadline = new(2026, 8, 10, 10, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(false, false, false)]
    public void Late_Submission_Requires_Both_Institution_And_Assignment_Permission(
        bool institutionAllows,
        bool assignmentAllows,
        bool expected)
    {
        Assert.Equal(
            expected,
            SubmissionRules.IsLateSubmissionAllowed(institutionAllows, assignmentAllows));
    }

    [Fact]
    public void Submission_Before_Deadline_Is_Submitted()
    {
        var status = SubmissionRules.GetInitialStatus(Deadline, Deadline.AddMinutes(-1), false);
        Assert.Equal(SubmissionStatus.Submitted, status);
    }

    [Fact]
    public void Late_Submission_Is_Rejected_When_Disabled()
    {
        Assert.Throws<AppException>(() =>
            SubmissionRules.GetInitialStatus(Deadline, Deadline.AddMinutes(1), false));
    }

    [Fact]
    public void Late_Submission_Is_Marked_Late_When_Enabled()
    {
        var status = SubmissionRules.GetInitialStatus(Deadline, Deadline.AddMinutes(1), true);
        Assert.Equal(SubmissionStatus.Late, status);
    }

    [Fact]
    public void Resubmission_Before_Deadline_Is_Accepted()
    {
        SubmissionRules.ValidateResubmission(
            allowResubmission: true,
            allowStudentUpdate: true,
            Deadline,
            Deadline.AddMinutes(-1),
            SubmissionStatus.Submitted);
    }

    [Fact]
    public void Resubmission_After_Deadline_Is_Rejected()
    {
        Assert.Throws<AppException>(() => SubmissionRules.ValidateResubmission(
            allowResubmission: true,
            allowStudentUpdate: true,
            Deadline,
            Deadline.AddMinutes(1),
            SubmissionStatus.Submitted));
    }

    [Fact]
    public void Graded_Submission_Cannot_Be_Updated()
    {
        Assert.Throws<AppException>(() => SubmissionRules.ValidateResubmission(
            allowResubmission: true,
            allowStudentUpdate: true,
            Deadline,
            Deadline.AddMinutes(-1),
            SubmissionStatus.Graded));
    }

    [Fact]
    public void Marks_Cannot_Exceed_Maximum()
    {
        Assert.Throws<AppException>(() => SubmissionRules.ValidateReview(
            marks: 101,
            maximumMarks: 100,
            feedback: "Reviewed",
            status: SubmissionStatus.Graded,
            requireFeedbackForGrading: false));
    }

    [Fact]
    public void Grading_Requires_Marks()
    {
        Assert.Throws<AppException>(() => SubmissionRules.ValidateReview(
            marks: null,
            maximumMarks: 100,
            feedback: "Reviewed",
            status: SubmissionStatus.Graded,
            requireFeedbackForGrading: false));
    }

    [Fact]
    public void Returned_Submission_Requires_Feedback()
    {
        Assert.Throws<AppException>(() => SubmissionRules.ValidateReview(
            marks: null,
            maximumMarks: 100,
            feedback: null,
            status: SubmissionStatus.Returned,
            requireFeedbackForGrading: false));
    }

    [Fact]
    public void Grading_Feedback_Is_Required_When_Effective_Policy_Is_Enabled()
    {
        Assert.Throws<AppException>(() => SubmissionRules.ValidateReview(
            marks: 80,
            maximumMarks: 100,
            feedback: null,
            status: SubmissionStatus.Graded,
            requireFeedbackForGrading: true));
    }

    [Fact]
    public void Grading_Feedback_Is_Optional_When_Effective_Policy_Is_Disabled()
    {
        SubmissionRules.ValidateReview(
            marks: 80,
            maximumMarks: 100,
            feedback: null,
            status: SubmissionStatus.Graded,
            requireFeedbackForGrading: false);
    }
}
