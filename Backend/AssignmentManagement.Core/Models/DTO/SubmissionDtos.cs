using System.ComponentModel.DataAnnotations;
using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Common;

namespace AssignmentManagement.Core.Models.DTO;

public sealed class SubmissionQueryRequest : PagingRequest
{
    public long? AssignmentId { get; set; }
    public long? StudentId { get; set; }
    public SubmissionStatus? Status { get; set; }
}

public sealed class SubmitAssignmentRequest
{
    [MaxLength(20000)]
    public string? AnswerText { get; set; }
}

public sealed class ReviewSubmissionRequest
{
    [Range(typeof(decimal), "0", "10000")]
    public decimal? Marks { get; set; }

    [MaxLength(5000)]
    public string? Feedback { get; set; }

    [EnumDataType(typeof(SubmissionStatus))]
    public SubmissionStatus Status { get; set; }
}

public sealed record SubmissionResponse(
    long Id,
    long AssignmentId,
    string AssignmentTitle,
    decimal AssignmentMaximumMarks,
    bool FeedbackRequiredForGrading,
    long StudentId,
    string StudentName,
    string StudentUserName,
    string AnswerText,
    string? FileName,
    string? FileContentType,
    long? FileSize,
    DateTime SubmittedAtUtc,
    SubmissionStatus Status,
    decimal? Marks,
    string? Feedback,
    DateTime? ReviewedAtUtc,
    string? ReviewedByTeacherName);
