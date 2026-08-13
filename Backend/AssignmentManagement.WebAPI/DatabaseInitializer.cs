using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Models.Entities;
using AssignmentManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AssignmentManagement.WebAPI;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AssignmentDbContext>();

        var autoCreate = configuration.GetValue("Database:AutoCreate", true);
        if (autoCreate)
        {
            logger.LogInformation("Ensuring PostgreSQL database and tables exist...");
            await dbContext.Database.EnsureCreatedAsync();
        }

        if (!await dbContext.Database.CanConnectAsync())
            throw new InvalidOperationException("Cannot connect to PostgreSQL. Check the connection string and PostgreSQL service.");

        await SeedRolesAsync(dbContext);
        var institution = await SeedInstitutionAsync(dbContext, configuration);
        await SeedSettingsAsync(dbContext, institution.Id);
        await SeedAdminAsync(dbContext, configuration, institution.Id);
        await SeedDemoUsersAsync(dbContext, configuration, institution.Id);
    }

    private static async Task SeedRolesAsync(AssignmentDbContext dbContext)
    {
        foreach (var roleName in AppRoles.All)
        {
            if (!await dbContext.Roles.AnyAsync(x => x.Name == roleName))
                dbContext.Roles.Add(new AppRole { Name = roleName });
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task<Institution> SeedInstitutionAsync(
        AssignmentDbContext dbContext,
        IConfiguration configuration)
    {
        var code = (configuration["Seed:InstitutionCode"] ?? "DEMO").Trim().ToUpperInvariant();
        var institution = await dbContext.Institutions.FirstOrDefaultAsync(x => x.Code == code);
        if (institution is not null)
            return institution;

        var typeText = configuration["Seed:InstitutionType"] ?? nameof(InstitutionType.College);
        if (!Enum.TryParse<InstitutionType>(typeText, true, out var institutionType))
            institutionType = InstitutionType.College;

        institution = new Institution
        {
            Name = configuration["Seed:InstitutionName"] ?? "Demo College",
            Code = code,
            Type = institutionType,
            Address = configuration["Seed:InstitutionAddress"],
            Email = configuration["Seed:InstitutionEmail"],
            Phone = configuration["Seed:InstitutionPhone"]
        };
        dbContext.Institutions.Add(institution);
        await dbContext.SaveChangesAsync();
        return institution;
    }

    private static async Task SeedSettingsAsync(AssignmentDbContext dbContext, long institutionId)
    {
        var settings = new[]
        {
            new ApplicationSetting
            {
                InstitutionId = institutionId,
                Key = "AllowLateSubmission",
                Value = "false",
                Description = "Allow a student's first submission after the assignment deadline."
            },
            new ApplicationSetting
            {
                InstitutionId = institutionId,
                Key = "AllowStudentSubmissionUpdate",
                Value = "true",
                Description = "Allow students to update submissions before the deadline when the assignment permits resubmission."
            }
        };

        foreach (var setting in settings)
        {
            if (!await dbContext.ApplicationSettings.AnyAsync(
                    x => x.InstitutionId == institutionId && x.Key == setting.Key))
                dbContext.ApplicationSettings.Add(setting);
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAdminAsync(
        AssignmentDbContext dbContext,
        IConfiguration configuration,
        long institutionId)
    {
        var userName = (configuration["Seed:AdminUserName"] ?? "admin").Trim().ToLowerInvariant();
        var email = (configuration["Seed:AdminEmail"] ?? "admin@demo.local").Trim().ToLowerInvariant();
        var password = configuration["Seed:AdminPassword"] ?? "Admin@123";

        var admin = await dbContext.Users
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.UserName == userName);

        if (admin is null)
        {
            admin = new AppUser
            {
                FullName = configuration["Seed:AdminFullName"] ?? "System Admin",
                UserName = userName,
                Email = email,
                InstitutionId = institutionId
            };
            var passwordHasher = new PasswordHasher<AppUser>();
            admin.PasswordHash = passwordHasher.HashPassword(admin, password);
            dbContext.Users.Add(admin);
            await dbContext.SaveChangesAsync();
        }

        var adminRoleId = await dbContext.Roles
            .Where(x => x.Name == AppRoles.Admin)
            .Select(x => x.Id)
            .SingleAsync();

        if (!await dbContext.UserRoles.AnyAsync(x => x.UserId == admin.Id && x.RoleId == adminRoleId))
        {
            dbContext.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRoleId });
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task SeedDemoUsersAsync(
        AssignmentDbContext dbContext,
        IConfiguration configuration,
        long institutionId)
    {
        var academicClass = await dbContext.AcademicClasses.FirstOrDefaultAsync(x =>
            x.InstitutionId == institutionId &&
            x.Name == "Class 10" &&
            x.Section == "A" &&
            x.AcademicYear == "2026");

        if (academicClass is null)
        {
            academicClass = new AcademicClass
            {
                Name = "Class 10",
                Section = "A",
                AcademicYear = "2026",
                InstitutionId = institutionId
            };
            dbContext.AcademicClasses.Add(academicClass);
        }

        var subject = await dbContext.Subjects.FirstOrDefaultAsync(x =>
            x.InstitutionId == institutionId && x.Code == "MATH-101");

        if (subject is null)
        {
            subject = new Subject
            {
                Name = "Mathematics",
                Code = "MATH-101",
                InstitutionId = institutionId
            };
            dbContext.Subjects.Add(subject);
        }

        await dbContext.SaveChangesAsync();

        var teacher = await SeedUserAsync(
            dbContext,
            institutionId,
            AppRoles.Teacher,
            configuration["Seed:TeacherFullName"] ?? "Demo Teacher",
            configuration["Seed:TeacherUserName"] ?? "teacher",
            configuration["Seed:TeacherEmail"] ?? "teacher@demo.local",
            configuration["Seed:TeacherPassword"] ?? "Teacher@123");

        await SeedUserAsync(
            dbContext,
            institutionId,
            AppRoles.Student,
            configuration["Seed:StudentFullName"] ?? "Demo Student",
            configuration["Seed:StudentUserName"] ?? "student",
            configuration["Seed:StudentEmail"] ?? "student@demo.local",
            configuration["Seed:StudentPassword"] ?? "Student@123",
            academicClass.Id);

        if (!await dbContext.TeacherClassSubjects.AnyAsync(x =>
                x.TeacherId == teacher.Id &&
                x.AcademicClassId == academicClass.Id &&
                x.SubjectId == subject.Id))
        {
            dbContext.TeacherClassSubjects.Add(new TeacherClassSubject
            {
                TeacherId = teacher.Id,
                AcademicClassId = academicClass.Id,
                SubjectId = subject.Id,
                InstitutionId = institutionId
            });
            await dbContext.SaveChangesAsync();
        }
    }

    private static async Task<AppUser> SeedUserAsync(
        AssignmentDbContext dbContext,
        long institutionId,
        string roleName,
        string fullName,
        string userName,
        string email,
        string password,
        long? academicClassId = null)
    {
        userName = userName.Trim().ToLowerInvariant();
        email = email.Trim().ToLowerInvariant();

        var user = await dbContext.Users
            .Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.UserName == userName || x.Email == email);

        if (user is null)
        {
            user = new AppUser
            {
                FullName = fullName,
                UserName = userName,
                Email = email,
                InstitutionId = institutionId,
                AcademicClassId = academicClassId
            };
            var passwordHasher = new PasswordHasher<AppUser>();
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
        }
        else if (academicClassId.HasValue && user.AcademicClassId != academicClassId)
        {
            user.AcademicClassId = academicClassId;
            await dbContext.SaveChangesAsync();
        }

        var roleId = await dbContext.Roles
            .Where(x => x.Name == roleName)
            .Select(x => x.Id)
            .SingleAsync();

        if (!await dbContext.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == roleId))
        {
            dbContext.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
            await dbContext.SaveChangesAsync();
        }

        return user;
    }
}
