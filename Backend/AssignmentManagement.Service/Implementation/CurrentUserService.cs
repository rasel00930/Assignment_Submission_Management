using System.Security.Claims;
using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace AssignmentManagement.Service.Implementation;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal User => _httpContextAccessor.HttpContext?.User
        ?? throw new AppException(401, "Authentication is required.");

    public long UserId => ParseRequiredLong(ClaimTypes.NameIdentifier);
    public long InstitutionId => ParseRequiredLong("institutionId");
    public long? AcademicClassId => ParseOptionalLong("academicClassId");

    public IReadOnlyCollection<string> Roles => User.FindAll(ClaimTypes.Role)
        .Select(x => x.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public bool IsInRole(string role) => User.IsInRole(role);

    private long ParseRequiredLong(string claimType)
    {
        var value = User.FindFirstValue(claimType);
        if (!long.TryParse(value, out var parsed))
            throw new AppException(401, "The access token is invalid.");

        return parsed;
    }

    private long? ParseOptionalLong(string claimType)
    {
        var value = User.FindFirstValue(claimType);
        return long.TryParse(value, out var parsed) ? parsed : null;
    }
}
