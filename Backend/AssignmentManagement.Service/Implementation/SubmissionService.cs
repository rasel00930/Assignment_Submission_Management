using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Core.Interfaces;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Core.Models.Entities;
using AssignmentManagement.Core.Rules;
using AssignmentManagement.Service.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Service.Implementation;

public sealed class SubmissionService : ISubmissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ISubmissionFileStorage _fileStorage;

    public SubmissionService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider,
        ISubmissionFileStorage fileStorage)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
    }

    public async Task<PagedResponse<SubmissionResponse>> GetAsync(
    SubmissionQueryRequest request,
    CancellationToken cancellationToken = default)
    {
        var policies = await GetApplicationPoliciesAsync(cancellationToken);
        var query = ApplyCurrentUserAccess(
            _unitOfWork.Submissions.Table.AsNoTracking());

        if (request.AssignmentId.HasValue)
        {
            query = query.Where(x =>
                x.AssignmentId == request.AssignmentId.Value);
        }

        if (request.StudentId.HasValue)
        {
            query = query.Where(x =>
                x.StudentId == request.StudentId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x =>
                x.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();

            query = query.Where(x =>
                x.Assignment.Title.ToLower().Contains(search) ||
                x.Student.FullName.ToLower().Contains(search) ||
                x.Student.UserName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await Project(
                query
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize),
                policies,
                _currentUser.IsInRole(AppRoles.Student))
            .ToListAsync(cancellationToken);

        return new PagedResponse<SubmissionResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public async Task<SubmissionResponse> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var accessData = await _unitOfWork.Submissions.Table
            .AsNoTracking()
            .Where(x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId && x.IsActive)
            .Select(x => new
            {
                x.StudentId,
                AssignmentTeacherId = x.Assignment.CreatedByTeacherId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (accessData is null || !AuthorizationRules.CanViewSubmission(
                _currentUser.Roles,
                _currentUser.UserId,
                accessData.StudentId,
                accessData.AssignmentTeacherId))
            throw new AppException(404, "Submission was not found or you do not have access to it.");

        var policies = await GetApplicationPoliciesAsync(cancellationToken);
        return await Project(
                _unitOfWork.Submissions.Table.AsNoTracking().Where(x => x.Id == id),
                policies,
                _currentUser.IsInRole(AppRoles.Student))
            .FirstAsync(cancellationToken);
    }

    public async Task<SubmissionResponse> SubmitAsync(
        long assignmentId,
        SubmitAssignmentRequest request,
        SubmissionFileUpload? file,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.AcademicClassId.HasValue)
            throw new AppException(409, "The student is not assigned to a class/course.");

        var assignment = await _unitOfWork.Assignments.Table
            .AsNoTracking()
            .Include(x => x.TeacherClassSubject)
            .FirstOrDefaultAsync(
                x => x.Id == assignmentId &&
                     x.InstitutionId == _currentUser.InstitutionId &&
                     x.IsActive,
                cancellationToken)
            ?? throw new AppException(404, "Assignment was not found.");

        if (assignment.Status != AssignmentStatus.Published)
            throw new AppException(409, "Only a published assignment can receive submissions.");

        if (assignment.TeacherClassSubject.AcademicClassId != _currentUser.AcademicClassId.Value)
            throw new AppException(403, "This assignment is not assigned to your class/course.");

        var policies = await GetApplicationPoliciesAsync(cancellationToken);

        if (file is not null && !(assignment.AllowFileUpload && policies.AllowSubmissionFileUpload))
            throw new AppException(409, "File upload is not allowed for this assignment.");

        var allowLateSubmission = SubmissionRules.IsLateSubmissionAllowed(
            policies.AllowLateSubmission,
            assignment.AllowLateSubmission);

        var existing = await _unitOfWork.Submissions.FirstOrDefaultAsync(
            x => x.AssignmentId == assignmentId && x.StudentId == _currentUser.UserId,
            trackChanges: true,
            cancellationToken: cancellationToken);

        var answerText = request.AnswerText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(answerText) && file is null && existing?.StoredFilePath is null)
            throw new AppException(400, "Write an answer or attach a file before submitting.");

        StoredSubmissionFile? storedFile = null;
        if (file is not null)
        {
            storedFile = await _fileStorage.SaveAsync(
                file,
                _currentUser.InstitutionId,
                assignment.Id,
                cancellationToken);
        }

        var previousStoredFilePath = existing?.StoredFilePath;

        try
        {
            if (existing is null)
            {
                var status = SubmissionRules.GetInitialStatus(
                    assignment.DeadlineUtc,
                    _dateTimeProvider.UtcNow,
                    allowLateSubmission);

                existing = new Submission
                {
                    AnswerText = answerText,
                    SubmittedAtUtc = _dateTimeProvider.UtcNow,
                    Status = status,
                    InstitutionId = _currentUser.InstitutionId,
                    AssignmentId = assignment.Id,
                    StudentId = _currentUser.UserId,
                    CreatedAtUtc = _dateTimeProvider.UtcNow,
                    CreatedByUserId = _currentUser.UserId
                };
                ApplyFile(existing, storedFile);
                await _unitOfWork.Submissions.AddAsync(existing, cancellationToken);
            }
            else
            {
                SubmissionRules.ValidateResubmission(
                    assignment.AllowResubmission,
                    policies.AllowStudentSubmissionUpdate,
                    assignment.DeadlineUtc,
                    _dateTimeProvider.UtcNow,
                    existing.Status);

                existing.AnswerText = answerText;
                existing.SubmittedAtUtc = _dateTimeProvider.UtcNow;
                existing.Status = SubmissionStatus.Resubmitted;
                existing.Marks = null;
                existing.Feedback = null;
                existing.ReviewedAtUtc = null;
                existing.ReviewedByTeacherId = null;
                existing.UpdatedAtUtc = _dateTimeProvider.UtcNow;
                existing.UpdatedByUserId = _currentUser.UserId;
                if (storedFile is not null)
                    ApplyFile(existing, storedFile);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            if (storedFile is not null)
                await _fileStorage.DeleteAsync(storedFile.StoredFilePath, CancellationToken.None);
            throw;
        }

        if (storedFile is not null && previousStoredFilePath is not null)
            await _fileStorage.DeleteAsync(previousStoredFilePath, CancellationToken.None);

        return await GetByIdAsync(existing.Id, cancellationToken);
    }

    public async Task<SubmissionFileDownload> DownloadFileAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var submission = await ApplyCurrentUserAccess(_unitOfWork.Submissions.Table.AsNoTracking())
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.FileName,
                x.StoredFilePath,
                x.FileContentType
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException(404, "Submission was not found or you do not have access to it.");

        if (string.IsNullOrWhiteSpace(submission.StoredFilePath) || string.IsNullOrWhiteSpace(submission.FileName))
            throw new AppException(404, "This submission does not have an attached file.");

        var stream = await _fileStorage.OpenReadAsync(submission.StoredFilePath, cancellationToken);
        return new SubmissionFileDownload(
            stream,
            submission.FileName,
            submission.FileContentType ?? "application/octet-stream");
    }

    public async Task<SubmissionResponse> ReviewAsync(
        long id,
        ReviewSubmissionRequest request,
        CancellationToken cancellationToken = default)
    {
        var submission = await _unitOfWork.Submissions.Table
            .Include(x => x.Assignment)
            .FirstOrDefaultAsync(
                x => x.Id == id &&
                     x.InstitutionId == _currentUser.InstitutionId &&
                     x.Assignment.CreatedByTeacherId == _currentUser.UserId,
                cancellationToken)
            ?? throw new AppException(404, "Submission was not found or the assignment does not belong to you.");

        SubmissionRules.ValidateReview(
            request.Marks,
            submission.Assignment.MaximumMarks,
            request.Feedback,
            request.Status,
            submission.Assignment.RequireFeedbackForGrading &&
            (await GetApplicationPoliciesAsync(cancellationToken)).RequireFeedbackForGrading);

        submission.Marks = request.Marks;
        submission.Feedback = string.IsNullOrWhiteSpace(request.Feedback) ? null : request.Feedback.Trim();
        submission.Status = request.Status;
        submission.ReviewedAtUtc = _dateTimeProvider.UtcNow;
        submission.ReviewedByTeacherId = _currentUser.UserId;
        submission.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        submission.UpdatedByUserId = _currentUser.UserId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(submission.Id, cancellationToken);
    }

    private IQueryable<Submission> ApplyCurrentUserAccess(IQueryable<Submission> query)
    {
        query = query.Where(x => x.InstitutionId == _currentUser.InstitutionId && x.IsActive);

        if (_currentUser.IsInRole(AppRoles.Admin))
            return query;

        if (_currentUser.IsInRole(AppRoles.Teacher))
            return query.Where(x => x.Assignment.CreatedByTeacherId == _currentUser.UserId);

        if (_currentUser.IsInRole(AppRoles.Student))
            return query.Where(x => x.StudentId == _currentUser.UserId);

        return query.Where(_ => false);
    }

    private async Task<ApplicationPolicies> GetApplicationPoliciesAsync(
        CancellationToken cancellationToken)
    {
        var values = await _unitOfWork.Settings.Table
            .AsNoTracking()
            .Where(x =>
                x.InstitutionId == _currentUser.InstitutionId &&
                x.IsActive &&
                ApplicationSettingKeys.Supported.Contains(x.Key))
            .Select(x => new { x.Key, x.Value })
            .ToListAsync(cancellationToken);

        bool ReadBoolean(string key, bool defaultValue)
        {
            var value = values.FirstOrDefault(x => x.Key == key)?.Value;
            return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
        }

        return new ApplicationPolicies(
            ReadBoolean(ApplicationSettingKeys.AllowLateSubmission, false),
            ReadBoolean(ApplicationSettingKeys.AllowStudentSubmissionUpdate, true),
            ReadBoolean(ApplicationSettingKeys.AllowSubmissionFileUpload, false),
            ReadBoolean(ApplicationSettingKeys.RequireFeedbackForGrading, false),
            ReadBoolean(ApplicationSettingKeys.ShowGradesImmediately, false));
    }

    private static IQueryable<SubmissionResponse> Project(
        IQueryable<Submission> query,
        ApplicationPolicies policies,
        bool hideUnreleasedGrades)
    {
        return query.Select(x => new SubmissionResponse(
            x.Id,
            x.AssignmentId,
            x.Assignment.Title,
            x.Assignment.MaximumMarks,
            x.Assignment.RequireFeedbackForGrading && policies.RequireFeedbackForGrading,
            x.StudentId,
            x.Student.FullName,
            x.Student.UserName,
            x.AnswerText,
            x.FileName,
            x.FileContentType,
            x.FileSize,
            x.SubmittedAtUtc,
            hideUnreleasedGrades &&
            x.Status == SubmissionStatus.Graded &&
            x.Assignment.Status != AssignmentStatus.Closed &&
            !(x.Assignment.ShowGradesImmediately && policies.ShowGradesImmediately)
                ? SubmissionStatus.UnderReview
                : x.Status,
            hideUnreleasedGrades &&
            x.Status == SubmissionStatus.Graded &&
            x.Assignment.Status != AssignmentStatus.Closed &&
            !(x.Assignment.ShowGradesImmediately && policies.ShowGradesImmediately)
                ? null
                : x.Marks,
            hideUnreleasedGrades &&
            x.Status == SubmissionStatus.Graded &&
            x.Assignment.Status != AssignmentStatus.Closed &&
            !(x.Assignment.ShowGradesImmediately && policies.ShowGradesImmediately)
                ? null
                : x.Feedback,
            hideUnreleasedGrades &&
            x.Status == SubmissionStatus.Graded &&
            x.Assignment.Status != AssignmentStatus.Closed &&
            !(x.Assignment.ShowGradesImmediately && policies.ShowGradesImmediately)
                ? null
                : x.ReviewedAtUtc,
            !hideUnreleasedGrades ||
            x.Status != SubmissionStatus.Graded ||
            x.Assignment.Status == AssignmentStatus.Closed ||
            (x.Assignment.ShowGradesImmediately && policies.ShowGradesImmediately)
                ? x.ReviewedByTeacherId.HasValue
                ? x.ReviewedByTeacher!.FullName
                    : null
                : null));
    }

    private sealed record ApplicationPolicies(
        bool AllowLateSubmission,
        bool AllowStudentSubmissionUpdate,
        bool AllowSubmissionFileUpload,
        bool RequireFeedbackForGrading,
        bool ShowGradesImmediately);

    private static void ApplyFile(Submission submission, StoredSubmissionFile? file)
    {
        if (file is null)
            return;

        submission.FileName = file.FileName;
        submission.StoredFilePath = file.StoredFilePath;
        submission.FileContentType = file.ContentType;
        submission.FileSize = file.FileSize;
    }
}
