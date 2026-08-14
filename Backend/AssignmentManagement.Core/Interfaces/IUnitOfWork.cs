using AssignmentManagement.Core.Models.Entities;

namespace AssignmentManagement.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<Institution> Institutions { get; }
    IGenericRepository<AppRole> Roles { get; }
    IGenericRepository<AppUser> Users { get; }
    IGenericRepository<UserRole> UserRoles { get; }
    IGenericRepository<RefreshToken> RefreshTokens { get; }
    IGenericRepository<PasswordResetCode> PasswordResetCodes { get; }
    IGenericRepository<AcademicClass> AcademicClasses { get; }
    IGenericRepository<Subject> Subjects { get; }
    IGenericRepository<TeacherClassSubject> TeacherClassSubjects { get; }
    IGenericRepository<Assignment> Assignments { get; }
    IGenericRepository<Submission> Submissions { get; }
    IGenericRepository<ApplicationSetting> Settings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
