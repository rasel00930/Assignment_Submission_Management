namespace AssignmentManagement.Core.Enums;

public static class ApplicationSettingKeys
{
    public const string AllowLateSubmission = "AllowLateSubmission";
    public const string AllowStudentSubmissionUpdate = "AllowStudentSubmissionUpdate";
    public const string AllowSubmissionFileUpload = "AllowSubmissionFileUpload";
    public const string RequireFeedbackForGrading = "RequireFeedbackForGrading";
    public const string ShowGradesImmediately = "ShowGradesImmediately";

    public static readonly string[] Supported =
    [
        AllowLateSubmission,
        AllowStudentSubmissionUpdate,
        AllowSubmissionFileUpload,
        RequireFeedbackForGrading,
        ShowGradesImmediately
    ];
}

public sealed record ApplicationSettingDefinition(
    string Key,
    string Title,
    string Description,
    string Alignment,
    bool DefaultValue,
    bool SeedByDefault);

public static class ApplicationSettingCatalog
{
    public static readonly ApplicationSettingDefinition[] Definitions =
    [
        new(
            ApplicationSettingKeys.AllowLateSubmission,
            "Allow late submissions",
            "Let teachers decide whether students can submit an assignment for the first time after its deadline.",
            "Turn this on to show the late-submission option to teachers. A student can submit late only when the teacher also allows it for that assignment.",
            false,
            true),
        new(
            ApplicationSettingKeys.AllowStudentSubmissionUpdate,
            "Allow students to update submissions",
            "Let teachers decide whether students can update and resubmit their work before the deadline.",
            "Turn this on to show the resubmission option to teachers. A student can update their work only when the teacher also allows it for that assignment.",
            true,
            true),
        new(
            ApplicationSettingKeys.AllowSubmissionFileUpload,
            "Allow answer file uploads",
            "Let teachers accept an answer file, such as a PDF or image, with a student's submission.",
            "Turn this on to show the file-upload option to teachers. Students can attach a file only to assignments where the teacher also allows it.",
            false,
            false),
        new(
            ApplicationSettingKeys.RequireFeedbackForGrading,
            "Require written grading feedback",
            "Let teachers require written feedback before a student's work can be marked as graded.",
            "Turn this on to show the feedback requirement to teachers. Feedback becomes required only for assignments where the teacher also enables it.",
            false,
            false),
        new(
            ApplicationSettingKeys.ShowGradesImmediately,
            "Release grades immediately",
            "Let teachers show marks and feedback to students as soon as their submissions are graded.",
            "Turn this on to show the immediate-release option to teachers. Grades are shown right away only for assignments where the teacher also enables it; otherwise students see them after the assignment is closed.",
            false,
            false)
    ];
}
