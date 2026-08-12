using AssignmentManagement.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.Infrastructure.Data;

public sealed class AssignmentDbContext : DbContext
{
    public AssignmentDbContext(DbContextOptions<AssignmentDbContext> options) : base(options)
    {
    }

    public DbSet<Institution> Institutions => Set<Institution>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AcademicClass> AcademicClasses => Set<AcademicClass>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<TeacherClassSubject> TeacherClassSubjects => Set<TeacherClassSubject>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Institution>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<AppRole>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.UserName).IsUnique();
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<RefreshToken>().HasIndex(x => x.TokenHash).IsUnique();
        modelBuilder.Entity<AcademicClass>()
            .HasIndex(x => new { x.InstitutionId, x.Name, x.Section, x.AcademicYear })
            .IsUnique();
        modelBuilder.Entity<Subject>()
            .HasIndex(x => new { x.InstitutionId, x.Code })
            .IsUnique();
        modelBuilder.Entity<TeacherClassSubject>()
            .HasIndex(x => new { x.TeacherId, x.AcademicClassId, x.SubjectId })
            .IsUnique();
        modelBuilder.Entity<Submission>()
            .HasIndex(x => new { x.AssignmentId, x.StudentId })
            .IsUnique();
        modelBuilder.Entity<ApplicationSetting>()
            .HasIndex(x => new { x.InstitutionId, x.Key })
            .IsUnique();

        modelBuilder.Entity<UserRole>().HasKey(x => new { x.UserId, x.RoleId });

        modelBuilder.Entity<Assignment>().Property(x => x.MaximumMarks).HasPrecision(10, 2);
        modelBuilder.Entity<Submission>().Property(x => x.Marks).HasPrecision(10, 2);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.User)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(x => x.Role)
            .WithMany(x => x.UserRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.Institution)
            .WithMany(x => x.Users)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasOne(x => x.AcademicClass)
            .WithMany(x => x.Students)
            .HasForeignKey(x => x.AcademicClassId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AcademicClass>()
            .HasOne(x => x.Institution)
            .WithMany(x => x.AcademicClasses)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Subject>()
            .HasOne(x => x.Institution)
            .WithMany(x => x.Subjects)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeacherClassSubject>()
            .HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeacherClassSubject>()
            .HasOne(x => x.Teacher)
            .WithMany()
            .HasForeignKey(x => x.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeacherClassSubject>()
            .HasOne(x => x.AcademicClass)
            .WithMany(x => x.TeacherClassSubjects)
            .HasForeignKey(x => x.AcademicClassId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TeacherClassSubject>()
            .HasOne(x => x.Subject)
            .WithMany(x => x.TeacherClassSubjects)
            .HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assignment>()
            .HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assignment>()
            .HasOne(x => x.TeacherClassSubject)
            .WithMany(x => x.Assignments)
            .HasForeignKey(x => x.TeacherClassSubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assignment>()
            .HasOne(x => x.CreatedByTeacher)
            .WithMany()
            .HasForeignKey(x => x.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Submission>()
            .HasOne(x => x.Institution)
            .WithMany()
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Submission>()
            .HasOne(x => x.Assignment)
            .WithMany(x => x.Submissions)
            .HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Submission>()
            .HasOne(x => x.Student)
            .WithMany()
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Submission>()
            .HasOne(x => x.ReviewedByTeacher)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByTeacherId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(x => x.User)
            .WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationSetting>()
            .HasOne(x => x.Institution)
            .WithMany(x => x.Settings)
            .HasForeignKey(x => x.InstitutionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
