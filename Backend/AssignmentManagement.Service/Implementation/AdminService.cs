using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Core.Interfaces;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Core.Models.Entities;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Service.Implementation;

public sealed class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IEmailService _emailService;

    public AdminService(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IPasswordHasher<AppUser> passwordHasher,
        IDateTimeProvider dateTimeProvider,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
        _emailService = emailService;
    }

    public async Task<PagedResponse<UserResponse>> GetUsersAsync(
        UserQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Users.Table
            .AsNoTracking()
            .Where(x => x.InstitutionId == _currentUser.InstitutionId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(search) ||
                x.UserName.ToLower().Contains(search) ||
                x.Email.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var role = NormalizeRole(request.Role);
            query = query.Where(x => x.UserRoles.Any(r => r.Role.Name == role));
        }

        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);

        if (request.AcademicClassId.HasValue)
            query = query.Where(x => x.AcademicClassId == request.AcademicClassId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .Include(x => x.AcademicClass)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .OrderBy(x => x.FullName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResponse<UserResponse>(
            users.Select(MapUser).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public async Task<UserResponse> GetUserByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(id, cancellationToken)
            ?? throw new AppException(404, "User was not found.");
        return MapUser(user);
    }

    public async Task<UserResponse> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var roleName = NormalizeRole(request.Role);
        var role = await GetRoleAsync(roleName, cancellationToken);
        var academicClassId = await ValidateAcademicClassForRoleAsync(
            roleName,
            request.AcademicClassId,
            cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var userName = request.UserName.Trim().ToLowerInvariant();
        await EnsureUniqueUserAsync(0, email, userName, cancellationToken);

        var user = new AppUser
        {
            FullName = request.FullName.Trim(),
            Email = email,
            UserName = userName,
            InstitutionId = _currentUser.InstitutionId,
            AcademicClassId = academicClassId,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        user.UserRoles.Add(new UserRole
        {
            RoleId = role.Id
        });

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        try
        {
            await _emailService.SendAccountCredentialsAsync(
                user.Email,
                user.FullName,
                user.UserName,
                request.Password,
                cancellationToken);
        }
        catch
        {
            _unitOfWork.Users.Delete(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateUserAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.Table
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(
                x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
                cancellationToken)
            ?? throw new AppException(404, "User was not found.");

        var roleName = NormalizeRole(request.Role);
        if (id == _currentUser.UserId && roleName != AppRoles.Admin)
            throw new AppException(409, "You cannot remove your own Admin role.");

        var role = await GetRoleAsync(roleName, cancellationToken);
        var academicClassId = await ValidateAcademicClassForRoleAsync(
            roleName,
            request.AcademicClassId,
            cancellationToken);

        var email = request.Email.Trim().ToLowerInvariant();
        var userName = request.UserName.Trim().ToLowerInvariant();
        await EnsureUniqueUserAsync(id, email, userName, cancellationToken);

        var existingRoleName = user.UserRoles.Select(x => x.Role.Name).FirstOrDefault();
        if (existingRoleName == AppRoles.Teacher && roleName != AppRoles.Teacher &&
            await _unitOfWork.TeacherClassSubjects.AnyAsync(
                x => x.TeacherId == user.Id && x.IsActive,
                cancellationToken))
            throw new AppException(409, "Deactivate the teacher's class-subject assignments before changing the role.");

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.UserName = userName;
        user.AcademicClassId = academicClassId;
        user.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        user.UpdatedByUserId = _currentUser.UserId;

        if (!string.Equals(existingRoleName, roleName, StringComparison.Ordinal))
        {
            foreach (var userRole in user.UserRoles.ToList())
                _unitOfWork.UserRoles.Delete(userRole);

            await _unitOfWork.UserRoles.AddAsync(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task SetUserActiveAsync(
        long id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (id == _currentUser.UserId && !isActive)
            throw new AppException(409, "You cannot deactivate your own account.");

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "User was not found.");

        user.IsActive = isActive;
        user.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        user.UpdatedByUserId = _currentUser.UserId;

        if (!isActive)
        {
            var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(
                x => x.UserId == id && x.RevokedAtUtc == null,
                trackChanges: true,
                cancellationToken);
            foreach (var token in tokens)
                token.RevokedAtUtc = _dateTimeProvider.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(
        long id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "User was not found.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        user.UpdatedByUserId = _currentUser.UserId;

        var tokens = await _unitOfWork.RefreshTokens.GetAllAsync(
            x => x.UserId == id && x.RevokedAtUtc == null,
            trackChanges: true,
            cancellationToken: cancellationToken);
        foreach (var token in tokens)
            token.RevokedAtUtc = _dateTimeProvider.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<InstitutionResponse> GetInstitutionAsync(CancellationToken cancellationToken = default)
    {
        var institution = await _unitOfWork.Institutions.FirstOrDefaultAsync(
            x => x.Id == _currentUser.InstitutionId,
            cancellationToken: cancellationToken)
            ?? throw new AppException(404, "Institution was not found.");
        return MapInstitution(institution);
    }

    public async Task<InstitutionResponse> UpdateInstitutionAsync(
        UpdateInstitutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var institution = await _unitOfWork.Institutions.FirstOrDefaultAsync(
            x => x.Id == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "Institution was not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _unitOfWork.Institutions.AnyAsync(
                x => x.Id != institution.Id && x.Code == code,
                cancellationToken))
            throw new AppException(409, "Institution code already exists.");

        institution.Name = request.Name.Trim();
        institution.Code = code;
        institution.Type = request.Type;
        institution.Address = request.Address?.Trim();
        institution.Email = request.Email?.Trim().ToLowerInvariant();
        institution.Phone = request.Phone?.Trim();
        institution.LogoUrl = request.LogoUrl?.Trim();
        institution.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        institution.UpdatedByUserId = _currentUser.UserId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapInstitution(institution);
    }

    public async Task<IReadOnlyCollection<ClassResponse>> GetClassesAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.AcademicClasses.Table
            .AsNoTracking()
            .Where(x => x.InstitutionId == _currentUser.InstitutionId);
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderByDescending(x => x.AcademicYear)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Section)
            .Select(x => new ClassResponse(
                x.Id,
                x.Name,
                x.Section,
                x.AcademicYear,
                x.IsActive,
                x.Students.Count(s => s.IsActive)))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClassResponse> GetClassByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.AcademicClasses.Table
            .AsNoTracking()
            .Where(x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId)
            .Select(x => new ClassResponse(
                x.Id,
                x.Name,
                x.Section,
                x.AcademicYear,
                x.IsActive,
                x.Students.Count(s => s.IsActive)))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException(404, "Class/course was not found.");
    }

    public async Task<ClassResponse> CreateClassAsync(
        CreateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var section = string.IsNullOrWhiteSpace(request.Section) ? null : request.Section.Trim();
        var academicYear = request.AcademicYear.Trim();

        if (await _unitOfWork.AcademicClasses.AnyAsync(
                x => x.InstitutionId == _currentUser.InstitutionId &&
                     x.Name == name && x.Section == section && x.AcademicYear == academicYear,
                cancellationToken))
            throw new AppException(409, "This class/course already exists.");

        var entity = new AcademicClass
        {
            Name = name,
            Section = section,
            AcademicYear = academicYear,
            InstitutionId = _currentUser.InstitutionId,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };
        await _unitOfWork.AcademicClasses.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetClassByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<ClassResponse> UpdateClassAsync(
        long id,
        UpdateClassRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.AcademicClasses.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "Class/course was not found.");

        var name = request.Name.Trim();
        var section = string.IsNullOrWhiteSpace(request.Section) ? null : request.Section.Trim();
        var academicYear = request.AcademicYear.Trim();

        if (await _unitOfWork.AcademicClasses.AnyAsync(
                x => x.Id != id && x.InstitutionId == _currentUser.InstitutionId &&
                     x.Name == name && x.Section == section && x.AcademicYear == academicYear,
                cancellationToken))
            throw new AppException(409, "This class/course already exists.");

        entity.Name = name;
        entity.Section = section;
        entity.AcademicYear = academicYear;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetClassByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeactivateClassAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.AcademicClasses.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "Class/course was not found.");

        if (await _unitOfWork.Users.AnyAsync(x => x.AcademicClassId == id && x.IsActive, cancellationToken))
            throw new AppException(409, "Move or deactivate the active students before deactivating this class/course.");

        entity.IsActive = false;
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SubjectResponse>> GetSubjectsAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Subjects.Table
            .AsNoTracking()
            .Where(x => x.InstitutionId == _currentUser.InstitutionId);
        if (!includeInactive)
            query = query.Where(x => x.IsActive);

        return await query
            .OrderBy(x => x.Name)
            .Select(x => new SubjectResponse(x.Id, x.Name, x.Code, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<SubjectResponse> GetSubjectByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Subjects.Table
            .AsNoTracking()
            .Where(x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId)
            .Select(x => new SubjectResponse(x.Id, x.Name, x.Code, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AppException(404, "Subject was not found.");
    }

    public async Task<SubjectResponse> CreateSubjectAsync(
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _unitOfWork.Subjects.AnyAsync(
                x => x.InstitutionId == _currentUser.InstitutionId && x.Code == code,
                cancellationToken))
            throw new AppException(409, "Subject code already exists.");

        var entity = new Subject
        {
            Name = request.Name.Trim(),
            Code = code,
            InstitutionId = _currentUser.InstitutionId,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };
        await _unitOfWork.Subjects.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetSubjectByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<SubjectResponse> UpdateSubjectAsync(
        long id,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subjects.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "Subject was not found.");

        var code = request.Code.Trim().ToUpperInvariant();
        if (await _unitOfWork.Subjects.AnyAsync(
                x => x.Id != id && x.InstitutionId == _currentUser.InstitutionId && x.Code == code,
                cancellationToken))
            throw new AppException(409, "Subject code already exists.");

        entity.Name = request.Name.Trim();
        entity.Code = code;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetSubjectByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeactivateSubjectAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.Subjects.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "Subject was not found.");

        entity.IsActive = false;
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResponse<TeacherAssignmentResponse>> GetTeacherAssignmentsAsync(
        TeacherAssignmentQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.TeacherClassSubjects.Table
            .AsNoTracking()
            .Where(x => x.InstitutionId == _currentUser.InstitutionId);

        if (_currentUser.IsInRole(AppRoles.Teacher))
            query = query.Where(x => x.TeacherId == _currentUser.UserId);

        if (request.TeacherId.HasValue)
            query = query.Where(x => x.TeacherId == request.TeacherId.Value);
        if (request.AcademicClassId.HasValue)
            query = query.Where(x => x.AcademicClassId == request.AcademicClassId.Value);
        if (request.SubjectId.HasValue)
            query = query.Where(x => x.SubjectId == request.SubjectId.Value);
        if (request.IsActive.HasValue)
            query = query.Where(x => x.IsActive == request.IsActive.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Teacher.FullName.ToLower().Contains(search) ||
                x.AcademicClass.Name.ToLower().Contains(search) ||
                x.Subject.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.Teacher.FullName)
            .ThenBy(x => x.AcademicClass.Name)
            .ThenBy(x => x.Subject.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new TeacherAssignmentResponse(
                x.Id,
                x.TeacherId,
                x.Teacher.FullName,
                x.AcademicClassId,
                x.AcademicClass.Name + (x.AcademicClass.Section == null ? "" : " - " + x.AcademicClass.Section),
                x.SubjectId,
                x.Subject.Name,
                x.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResponse<TeacherAssignmentResponse>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public async Task<TeacherAssignmentResponse> CreateTeacherAssignmentAsync(
        AssignTeacherRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateTeacherAssignmentAsync(0, request, cancellationToken);

        var entity = new TeacherClassSubject
        {
            TeacherId = request.TeacherId,
            AcademicClassId = request.AcademicClassId,
            SubjectId = request.SubjectId,
            InstitutionId = _currentUser.InstitutionId,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };
        await _unitOfWork.TeacherClassSubjects.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetTeacherAssignmentByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<TeacherAssignmentResponse> UpdateTeacherAssignmentAsync(
        long id,
        UpdateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.TeacherClassSubjects.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "Teacher assignment was not found.");

        await ValidateTeacherAssignmentAsync(id, request, cancellationToken);
        entity.TeacherId = request.TeacherId;
        entity.AcademicClassId = request.AcademicClassId;
        entity.SubjectId = request.SubjectId;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetTeacherAssignmentByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeactivateTeacherAssignmentAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _unitOfWork.TeacherClassSubjects.FirstOrDefaultAsync(
            x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "Teacher assignment was not found.");

        entity.IsActive = false;
        entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        entity.UpdatedByUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<SettingResponse>> GetSettingsAsync(
        CancellationToken cancellationToken = default) =>
        await _unitOfWork.Settings.Table
            .AsNoTracking()
            .Where(x => x.InstitutionId == _currentUser.InstitutionId && x.IsActive)
            .OrderBy(x => x.Key)
            .Select(x => new SettingResponse(x.Id, x.Key, x.Value, x.Description))
            .ToListAsync(cancellationToken);

    public async Task<SettingResponse> UpsertSettingAsync(
        SettingRequest request,
        CancellationToken cancellationToken = default)
    {
        var key = request.Key.Trim();
        var entity = await _unitOfWork.Settings.FirstOrDefaultAsync(
            x => x.InstitutionId == _currentUser.InstitutionId && x.Key == key,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (entity is null)
        {
            entity = new ApplicationSetting
            {
                InstitutionId = _currentUser.InstitutionId,
                Key = key,
                Value = request.Value.Trim(),
                Description = request.Description?.Trim(),
                CreatedAtUtc = _dateTimeProvider.UtcNow,
                CreatedByUserId = _currentUser.UserId
            };
            await _unitOfWork.Settings.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Value = request.Value.Trim();
            entity.Description = request.Description?.Trim();
            entity.IsActive = true;
            entity.UpdatedAtUtc = _dateTimeProvider.UtcNow;
            entity.UpdatedByUserId = _currentUser.UserId;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new SettingResponse(entity.Id, entity.Key, entity.Value, entity.Description);
    }

    private async Task<AppRole> GetRoleAsync(string roleName, CancellationToken cancellationToken) =>
        await _unitOfWork.Roles.FirstOrDefaultAsync(
            x => x.Name == roleName && x.IsActive,
            cancellationToken: cancellationToken)
        ?? throw new AppException(400, "The selected role is not configured.");

    private async Task<long?> ValidateAcademicClassForRoleAsync(
        string roleName,
        long? academicClassId,
        CancellationToken cancellationToken)
    {
        if (roleName != AppRoles.Student)
            return null;

        if (!academicClassId.HasValue)
            throw new AppException(400, "A class/course is required for a student.");

        var exists = await _unitOfWork.AcademicClasses.AnyAsync(
            x => x.Id == academicClassId.Value &&
                 x.InstitutionId == _currentUser.InstitutionId &&
                 x.IsActive,
            cancellationToken);
        if (!exists)
            throw new AppException(400, "The selected class/course is invalid.");

        return academicClassId;
    }

    private async Task EnsureUniqueUserAsync(
        long currentUserId,
        string email,
        string userName,
        CancellationToken cancellationToken)
    {
        if (await _unitOfWork.Users.AnyAsync(
                x => x.Id != currentUserId && x.Email == email,
                cancellationToken))
            throw new AppException(409, "Email address already exists.");

        if (await _unitOfWork.Users.AnyAsync(
                x => x.Id != currentUserId && x.UserName == userName,
                cancellationToken))
            throw new AppException(409, "Username already exists.");
    }

    private async Task ValidateTeacherAssignmentAsync(
        long currentMappingId,
        AssignTeacherRequest request,
        CancellationToken cancellationToken)
    {
        var teacherIsValid = await _unitOfWork.Users.AnyAsync(
            x => x.Id == request.TeacherId &&
                 x.InstitutionId == _currentUser.InstitutionId &&
                 x.IsActive &&
                 x.UserRoles.Any(r => r.Role.Name == AppRoles.Teacher),
            cancellationToken);
        if (!teacherIsValid)
            throw new AppException(400, "The selected user is not an active teacher.");

        var classIsValid = await _unitOfWork.AcademicClasses.AnyAsync(
            x => x.Id == request.AcademicClassId &&
                 x.InstitutionId == _currentUser.InstitutionId &&
                 x.IsActive,
            cancellationToken);
        if (!classIsValid)
            throw new AppException(400, "The selected class/course is invalid.");

        var subjectIsValid = await _unitOfWork.Subjects.AnyAsync(
            x => x.Id == request.SubjectId &&
                 x.InstitutionId == _currentUser.InstitutionId &&
                 x.IsActive,
            cancellationToken);
        if (!subjectIsValid)
            throw new AppException(400, "The selected subject is invalid.");

        if (await _unitOfWork.TeacherClassSubjects.AnyAsync(
                x => x.Id != currentMappingId &&
                     x.TeacherId == request.TeacherId &&
                     x.AcademicClassId == request.AcademicClassId &&
                     x.SubjectId == request.SubjectId,
                cancellationToken))
            throw new AppException(409, "This teacher is already assigned to the selected class and subject.");
    }

    private async Task<TeacherAssignmentResponse> GetTeacherAssignmentByIdAsync(
        long id,
        CancellationToken cancellationToken) =>
        await _unitOfWork.TeacherClassSubjects.Table
            .AsNoTracking()
            .Where(x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId)
            .Select(x => new TeacherAssignmentResponse(
                x.Id,
                x.TeacherId,
                x.Teacher.FullName,
                x.AcademicClassId,
                x.AcademicClass.Name + (x.AcademicClass.Section == null ? "" : " - " + x.AcademicClass.Section),
                x.SubjectId,
                x.Subject.Name,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken)
        ?? throw new AppException(404, "Teacher assignment was not found.");

    private Task<AppUser?> LoadUserAsync(long id, CancellationToken cancellationToken) =>
        _unitOfWork.Users.Table
            .AsNoTracking()
            .Include(x => x.AcademicClass)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .AsSplitQuery()
            .FirstOrDefaultAsync(
                x => x.Id == id && x.InstitutionId == _currentUser.InstitutionId,
                cancellationToken);

    private static string NormalizeRole(string role)
    {
        var normalized = AppRoles.All.FirstOrDefault(x =>
            string.Equals(x, role.Trim(), StringComparison.OrdinalIgnoreCase));
        return normalized ?? throw new AppException(400, "Role must be Admin, Teacher, or Student.");
    }

    private static UserResponse MapUser(AppUser user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.UserName,
            user.IsActive,
            user.UserRoles.Select(x => x.Role.Name).ToArray(),
            user.AcademicClassId,
            user.AcademicClass?.Name,
            user.CreatedAtUtc);

    private static InstitutionResponse MapInstitution(Institution institution) =>
        new(
            institution.Id,
            institution.Name,
            institution.Code,
            institution.Type,
            institution.Address,
            institution.Email,
            institution.Phone,
            institution.LogoUrl);
}
