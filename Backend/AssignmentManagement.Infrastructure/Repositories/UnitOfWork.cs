using AssignmentManagement.Core.Interfaces;
using AssignmentManagement.Core.Models.Entities;
using AssignmentManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace AssignmentManagement.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AssignmentDbContext _dbContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AssignmentDbContext dbContext)
    {
        _dbContext = dbContext;
        Institutions = new GenericRepository<Institution>(dbContext);
        Roles = new GenericRepository<AppRole>(dbContext);
        Users = new GenericRepository<AppUser>(dbContext);
        UserRoles = new GenericRepository<UserRole>(dbContext);
        RefreshTokens = new GenericRepository<RefreshToken>(dbContext);
        PasswordResetCodes = new GenericRepository<PasswordResetCode>(dbContext);
        AcademicClasses = new GenericRepository<AcademicClass>(dbContext);
        Subjects = new GenericRepository<Subject>(dbContext);
        TeacherClassSubjects = new GenericRepository<TeacherClassSubject>(dbContext);
        Assignments = new GenericRepository<Assignment>(dbContext);
        Submissions = new GenericRepository<Submission>(dbContext);
        Settings = new GenericRepository<ApplicationSetting>(dbContext);
    }

    public IGenericRepository<Institution> Institutions { get; }
    public IGenericRepository<AppRole> Roles { get; }
    public IGenericRepository<AppUser> Users { get; }
    public IGenericRepository<UserRole> UserRoles { get; }
    public IGenericRepository<RefreshToken> RefreshTokens { get; }
    public IGenericRepository<PasswordResetCode> PasswordResetCodes { get; }
    public IGenericRepository<AcademicClass> AcademicClasses { get; }
    public IGenericRepository<Subject> Subjects { get; }
    public IGenericRepository<TeacherClassSubject> TeacherClassSubjects { get; }
    public IGenericRepository<Assignment> Assignments { get; }
    public IGenericRepository<Submission> Submissions { get; }
    public IGenericRepository<ApplicationSetting> Settings { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            return;

        _transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _dbContext.Dispose();
    }
}
