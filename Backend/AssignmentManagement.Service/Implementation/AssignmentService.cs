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

public sealed class AssignmentService : IAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignmentService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResponse<AssignmentResponse>> GetAsync(
        AssignmentQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var policies = await GetApplicationPoliciesAsync(cancellationToken);
        var query = ApplyCurrentUserAccess(_unitOfWork.Assignments.Table.AsNoTracking());

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        if (request.AcademicClassId.HasValue)
            query = query.Where(x => x.TeacherClassSubject.AcademicClassId == request.AcademicClassId.Value);
        if (request.SubjectId.HasValue)
            query = query.Where(x => x.TeacherClassSubject.SubjectId == request.SubjectId.Value);
        if (request.TeacherId.HasValue)
            query = query.Where(x => x.CreatedByTeacherId == request.TeacherId.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                x.Description.ToLower().Contains(search) ||
                x.TeacherClassSubject.Subject.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await Project(query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize), policies)
            .ToListAsync(cancellationToken);

        return new PagedResponse<AssignmentResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public async Task<AssignmentPolicyResponse> GetPoliciesAsync(
        CancellationToken cancellationToken = default)
    {
        var policies = await GetApplicationPoliciesAsync(cancellationToken);
        return new AssignmentPolicyResponse(
            policies.AllowLateSubmission,
            policies.AllowStudentSubmissionUpdate,
            policies.AllowSubmissionFileUpload,
            policies.RequireFeedbackForGrading,
            policies.ShowGradesImmediately);
    }

    public async Task<AssignmentResponse> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var accessData = await _unitOfWork.Assignments.Table
            .AsNoTracking()
            .Where(x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId && x.IsActive)
            .Select(x => new
            {
                x.CreatedByTeacherId,
                AcademicClassId = x.TeacherClassSubject.AcademicClassId,
                x.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (accessData is null || !AuthorizationRules.CanViewAssignment(
                _currentUser.Roles,
                _currentUser.UserId,
                _currentUser.AcademicClassId,
                accessData.CreatedByTeacherId,
                accessData.AcademicClassId,
                accessData.Status))
            throw new AppException(404, "Assignment was not found or you do not have access to it.");

        var policies = await GetApplicationPoliciesAsync(cancellationToken);
        return await Project(
                _unitOfWork.Assignments.Table.AsNoTracking().Where(x => x.Id == id),
                policies)
            .FirstAsync(cancellationToken);
    }

    public async Task<AssignmentResponse> CreateAsync(
        CreateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var deadlineUtc = NormalizeUtc(request.DeadlineUtc);
        AssignmentRules.ValidateDeadline(deadlineUtc, _dateTimeProvider.UtcNow);
        AssignmentRules.ValidateMaximumMarks(request.MaximumMarks);

        var mapping = await GetValidTeacherMappingAsync(request.TeacherClassSubjectId, cancellationToken);
        var entity = new Assignment
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            DeadlineUtc = deadlineUtc,
            MaximumMarks = request.MaximumMarks,
            Status = request.PublishNow ? AssignmentStatus.Published : AssignmentStatus.Draft,
            AllowResubmission = request.AllowResubmission,
            AllowLateSubmission = request.AllowLateSubmission,
            AllowFileUpload = request.AllowFileUpload,
            RequireFeedbackForGrading = request.RequireFeedbackForGrading,
            ShowGradesImmediately = request.ShowGradesImmediately,
            InstitutionId = _currentUser.InstitutionId,
            TeacherClassSubjectId = mapping.Id,
            CreatedByTeacherId = _currentUser.UserId,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };

        await _unitOfWork.Assignments.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<AssignmentResponse> UpdateAsync(
        long id,
        UpdateAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(id, cancellationToken);
        if (entity.Status == AssignmentStatus.Closed)
            throw new AppException(409, "A closed assignment cannot be edited.");

        var deadlineUtc = NormalizeUtc(request.DeadlineUtc);
        AssignmentRules.ValidateDeadline(deadlineUtc, _dateTimeProvider.UtcNow);
        AssignmentRules.ValidateMaximumMarks(request.MaximumMarks);

        var mapping = await GetValidTeacherMappingAsync(request.TeacherClassSubjectId, cancellationToken);
        var hasSubmissions = await _unitOfWork.Submissions.AnyAsync(
            x => x.AssignmentId == entity.Id,
            cancellationToken);

        if (hasSubmissions && entity.TeacherClassSubjectId != mapping.Id)
            throw new AppException(409, "Class or subject cannot be changed after students have submitted answers.");

        var highestMarks = await _unitOfWork.Submissions.Table
            .Where(x => x.AssignmentId == entity.Id && x.Marks != null)
            .MaxAsync(x => (decimal?)x.Marks, cancellationToken);
        if (highestMarks.HasValue && request.MaximumMarks < highestMarks.Value)
            throw new AppException(409, "Maximum marks cannot be lower than marks already awarded.");

        entity.Title = request.Title.Trim();
        entity.Description = request.Description.Trim();
        entity.DeadlineUtc = deadlineUtc;
        entity.MaximumMarks = request.MaximumMarks;
        entity.TeacherClassSubjectId = mapping.Id;
        entity.AllowResubmission = request.AllowResubmission;
        entity.AllowLateSubmission = request.AllowLateSubmission;
        entity.AllowFileUpload = request.AllowFileUpload;
        entity.RequireFeedbackForGrading = request.RequireFeedbackForGrading;
        entity.ShowGradesImmediately = request.ShowGradesImmediately;
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task PublishAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(id, cancellationToken);
        AssignmentRules.ValidateDeadline(entity.DeadlineUtc, _dateTimeProvider.UtcNow);

        var mappingIsActive = await _unitOfWork.TeacherClassSubjects.AnyAsync(
            x => x.Id == entity.TeacherClassSubjectId &&
                 x.TeacherId == _currentUser.UserId &&
                 x.InstitutionId == _currentUser.InstitutionId &&
                 x.IsActive,
            cancellationToken);
        if (!mappingIsActive)
            throw new AppException(409, "The teacher-class-subject assignment is inactive.");

        entity.Status = AssignmentStatus.Published;
        StampUpdated(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MoveToDraftAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(id, cancellationToken);
        if (await _unitOfWork.Submissions.AnyAsync(x => x.AssignmentId == id, cancellationToken))
            throw new AppException(409, "An assignment with submissions cannot be moved back to draft.");

        entity.Status = AssignmentStatus.Draft;
        StampUpdated(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(id, cancellationToken);
        if (entity.Status == AssignmentStatus.Draft)
            throw new AppException(409, "Publish the assignment before closing it.");

        entity.Status = AssignmentStatus.Closed;
        StampUpdated(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await GetOwnedAssignmentAsync(id, cancellationToken);
        if (await _unitOfWork.Submissions.AnyAsync(x => x.AssignmentId == id, cancellationToken))
            throw new AppException(409, "An assignment with submissions cannot be deleted.");

        _unitOfWork.Assignments.Delete(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Assignment> ApplyCurrentUserAccess(IQueryable<Assignment> query)
    {
        query = query.Where(x => x.InstitutionId == _currentUser.InstitutionId && x.IsActive);

        if (_currentUser.IsInRole(AppRoles.Admin))
            return query;

        if (_currentUser.IsInRole(AppRoles.Teacher))
            return query.Where(x => x.CreatedByTeacherId == _currentUser.UserId);

        if (_currentUser.IsInRole(AppRoles.Student))
        {
            if (!_currentUser.AcademicClassId.HasValue)
                return query.Where(_ => false);

            return query.Where(x =>
                x.TeacherClassSubject.AcademicClassId == _currentUser.AcademicClassId.Value &&
                (x.Status == AssignmentStatus.Published || x.Status == AssignmentStatus.Closed));
        }

        return query.Where(_ => false);
    }

    private async Task<Assignment> GetOwnedAssignmentAsync(
        long id,
        CancellationToken cancellationToken) =>
        await _unitOfWork.Assignments.FirstOrDefaultAsync(
            x => x.Id == id &&
                 x.InstitutionId == _currentUser.InstitutionId &&
                 x.CreatedByTeacherId == _currentUser.UserId,
            trackChanges: true,
            cancellationToken: cancellationToken)
        ?? throw new AppException(404, "Assignment was not found or does not belong to you.");

    private async Task<TeacherClassSubject> GetValidTeacherMappingAsync(
        long id,
        CancellationToken cancellationToken) =>
        await _unitOfWork.TeacherClassSubjects.FirstOrDefaultAsync(
            x => x.Id == id &&
                 x.TeacherId == _currentUser.UserId &&
                 x.InstitutionId == _currentUser.InstitutionId &&
                 x.IsActive &&
                 x.AcademicClass.IsActive &&
                 x.Subject.IsActive,
            cancellationToken: cancellationToken)
        ?? throw new AppException(400, "The selected teacher-class-subject assignment is invalid.");

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

    private static IQueryable<AssignmentResponse> Project(
        IQueryable<Assignment> query,
        ApplicationPolicies policies) =>
        query.Select(x => new AssignmentResponse(
            x.Id,
            x.Title,
            x.Description,
            x.DeadlineUtc,
            x.MaximumMarks,
            x.Status,
            x.AllowResubmission,
            x.AllowLateSubmission,
            x.AllowFileUpload,
            x.RequireFeedbackForGrading,
            x.ShowGradesImmediately,
            x.AllowLateSubmission && policies.AllowLateSubmission,
            x.AllowResubmission && policies.AllowStudentSubmissionUpdate,
            x.AllowFileUpload && policies.AllowSubmissionFileUpload,
            x.RequireFeedbackForGrading && policies.RequireFeedbackForGrading,
            x.ShowGradesImmediately && policies.ShowGradesImmediately,
            x.TeacherClassSubjectId,
            x.TeacherClassSubject.AcademicClassId,
            x.TeacherClassSubject.AcademicClass.Name,
            x.TeacherClassSubject.AcademicClass.Section,
            x.TeacherClassSubject.SubjectId,
            x.TeacherClassSubject.Subject.Name,
            x.CreatedByTeacherId,
            x.CreatedByTeacher.FullName,
            x.Submissions.Count,
            x.CreatedAtUtc));

    private sealed record ApplicationPolicies(
        bool AllowLateSubmission,
        bool AllowStudentSubmissionUpdate,
        bool AllowSubmissionFileUpload,
        bool RequireFeedbackForGrading,
        bool ShowGradesImmediately);

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

    private void StampUpdated(BaseEntity entity)
    {
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;
    }
}
