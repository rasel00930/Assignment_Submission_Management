using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;

namespace AssignmentManagement.Service.Interfaces;

public interface IAuthService
{
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordWithCodeAsync(ResetPasswordWithCodeRequest request, CancellationToken cancellationToken = default);
    Task<UserSummary> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task SendAccountCredentialsAsync(
        string recipientEmail,
        string recipientName,
        string userName,
        string temporaryPassword,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetCodeAsync(
        string recipientEmail,
        string recipientName,
        string verificationCode,
        int expiresInMinutes,
        CancellationToken cancellationToken = default);
}

public interface IAdminService
{
    Task<PagedResponse<UserResponse>> GetUsersAsync(UserQueryRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetUserByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> UpdateUserAsync(long id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task SetUserActiveAsync(long id, bool isActive, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(long id, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task<InstitutionResponse> GetInstitutionAsync(CancellationToken cancellationToken = default);
    Task<InstitutionResponse> UpdateInstitutionAsync(UpdateInstitutionRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ClassResponse>> GetClassesAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<ClassResponse> GetClassByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken cancellationToken = default);
    Task<ClassResponse> UpdateClassAsync(long id, UpdateClassRequest request, CancellationToken cancellationToken = default);
    Task DeactivateClassAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SubjectResponse>> GetSubjectsAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<SubjectResponse> GetSubjectByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SubjectResponse> CreateSubjectAsync(CreateSubjectRequest request, CancellationToken cancellationToken = default);
    Task<SubjectResponse> UpdateSubjectAsync(long id, UpdateSubjectRequest request, CancellationToken cancellationToken = default);
    Task DeactivateSubjectAsync(long id, CancellationToken cancellationToken = default);

    Task<PagedResponse<TeacherAssignmentResponse>> GetTeacherAssignmentsAsync(
        TeacherAssignmentQueryRequest request,
        CancellationToken cancellationToken = default);
    Task<TeacherAssignmentResponse> CreateTeacherAssignmentAsync(
        AssignTeacherRequest request,
        CancellationToken cancellationToken = default);
    Task<TeacherAssignmentResponse> UpdateTeacherAssignmentAsync(
        long id,
        UpdateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default);
    Task DeactivateTeacherAssignmentAsync(long id, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SettingResponse>> GetSettingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<SettingCatalogResponse>> GetSettingCatalogAsync(CancellationToken cancellationToken = default);
    Task<SettingResponse> UpsertSettingAsync(SettingRequest request, CancellationToken cancellationToken = default);
}

public interface IAssignmentService
{
    Task<PagedResponse<AssignmentResponse>> GetAsync(
        AssignmentQueryRequest request,
        CancellationToken cancellationToken = default);
    Task<AssignmentPolicyResponse> GetPoliciesAsync(CancellationToken cancellationToken = default);
    Task<AssignmentResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<AssignmentResponse> CreateAsync(CreateAssignmentRequest request, CancellationToken cancellationToken = default);
    Task<AssignmentResponse> UpdateAsync(long id, UpdateAssignmentRequest request, CancellationToken cancellationToken = default);
    Task PublishAsync(long id, CancellationToken cancellationToken = default);
    Task MoveToDraftAsync(long id, CancellationToken cancellationToken = default);
    Task CloseAsync(long id, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}

public interface ISubmissionService
{
    Task<PagedResponse<SubmissionResponse>> GetAsync(
        SubmissionQueryRequest request,
        CancellationToken cancellationToken = default);
    Task<SubmissionResponse> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<SubmissionResponse> SubmitAsync(
        long assignmentId,
        SubmitAssignmentRequest request,
        SubmissionFileUpload? file,
        CancellationToken cancellationToken = default);
    Task<SubmissionFileDownload> DownloadFileAsync(
        long id,
        CancellationToken cancellationToken = default);
    Task<SubmissionResponse> ReviewAsync(
        long id,
        ReviewSubmissionRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record SubmissionFileUpload(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);

public sealed record SubmissionFileDownload(
    Stream Content,
    string FileName,
    string ContentType);

public sealed record StoredSubmissionFile(
    string FileName,
    string StoredFilePath,
    string ContentType,
    long FileSize);

public interface ISubmissionFileStorage
{
    Task<StoredSubmissionFile> SaveAsync(
        SubmissionFileUpload file,
        long institutionId,
        long assignmentId,
        CancellationToken cancellationToken = default);
    Task<Stream> OpenReadAsync(string storedFilePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? storedFilePath, CancellationToken cancellationToken = default);
}
