using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AssignmentManagement.Core.Interfaces;
using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AssignmentManagement.Service.Implementation;

public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public TokenService(
        IConfiguration configuration,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AccessTokenResult> CreateAccessTokenAsync(
        AppUser user,
        CancellationToken cancellationToken = default)
    {
        var roles = await _unitOfWork.UserRoles.Table
            .AsNoTracking()
            .Where(x => x.UserId == user.Id)
            .Select(x => x.Role.Name)
            .ToListAsync(cancellationToken);

        var keyText = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is missing.");
        if (Encoding.UTF8.GetByteCount(keyText) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes long.");

        var expiresAtUtc = _dateTimeProvider.UtcNow.AddMinutes(
            _configuration.GetValue<int>("Jwt:AccessTokenMinutes", 60));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("institutionId", user.InstitutionId.ToString())
        };

        if (user.AcademicClassId.HasValue)
            claims.Add(new Claim("academicClassId", user.AcademicClassId.Value.ToString()));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: _dateTimeProvider.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public string GenerateRefreshToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes);
    }
}
