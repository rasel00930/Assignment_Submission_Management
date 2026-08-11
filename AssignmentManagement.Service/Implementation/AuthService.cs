using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Core.Interfaces;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Core.Models.Entities;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AssignmentManagement.Service.Implementation;

public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPasswordHasher<AppUser> passwordHasher,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _configuration = configuration;
    }

    public async Task<TokenResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = request.UserName.Trim().ToLowerInvariant();
        var user = await LoadUserAsync(
            x => x.UserName == normalizedUserName,
            cancellationToken);

        if (user is null || !user.IsActive)
            throw new AppException(401, "Invalid username or password.");

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
            throw new AppException(401, "Invalid username or password.");

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            user.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        }

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<TokenResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.Table
            .Include(x => x.User)
                .ThenInclude(x => x.Institution)
            .Include(x => x.User)
                .ThenInclude(x => x.AcademicClass)
            .Include(x => x.User)
                .ThenInclude(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsUsable(_dateTimeProvider.UtcNow) || !storedToken.User.IsActive)
            throw new AppException(401, "The refresh token is invalid or expired.");

        var rawNewRefreshToken = _tokenService.GenerateRefreshToken();
        var newRefreshHash = _tokenService.HashRefreshToken(rawNewRefreshToken);

        storedToken.RevokedAtUtc = _dateTimeProvider.UtcNow;
        storedToken.ReplacedByTokenHash = newRefreshHash;
        storedToken.UpdatedAtUtc = _dateTimeProvider.UtcNow;

        var accessToken = await _tokenService.CreateAccessTokenAsync(storedToken.User, cancellationToken);
        var newToken = new RefreshToken
        {
            TokenHash = newRefreshHash,
            UserId = storedToken.UserId,
            ExpiresAtUtc = _dateTimeProvider.UtcNow.AddDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenDays", 7)),
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByUserId = storedToken.UserId
        };

        await _unitOfWork.RefreshTokens.AddAsync(newToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenResponse(
            accessToken.Token,
            rawNewRefreshToken,
            accessToken.ExpiresAtUtc,
            MapUser(storedToken.User));
    }

    public async Task LogoutAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _unitOfWork.RefreshTokens.FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash && x.UserId == _currentUser.UserId,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (storedToken is null)
            return;

        storedToken.RevokedAtUtc = _dateTimeProvider.UtcNow;
        storedToken.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        storedToken.UpdatedByUserId = _currentUser.UserId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(
            _currentUser.UserId,
            trackChanges: true,
            cancellationToken: cancellationToken) ?? throw new AppException(404, "User was not found.");

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
            throw new AppException(400, "The current password is incorrect.");

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = _dateTimeProvider.UtcNow;
        user.UpdatedByUserId = user.Id;

        var activeTokens = await _unitOfWork.RefreshTokens.GetAllAsync(
            x => x.UserId == user.Id && x.RevokedAtUtc == null,
            trackChanges: true,
            cancellationToken: cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = _dateTimeProvider.UtcNow;
            token.UpdatedAtUtc = _dateTimeProvider.UtcNow;
            token.UpdatedByUserId = user.Id;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserSummary> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(
            x => x.Id == _currentUser.UserId && x.InstitutionId == _currentUser.InstitutionId,
            cancellationToken) ?? throw new AppException(404, "User was not found.");

        return MapUser(user);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(
        AppUser user,
        CancellationToken cancellationToken)
    {
        var accessToken = await _tokenService.CreateAccessTokenAsync(user, cancellationToken);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();

        await _unitOfWork.RefreshTokens.AddAsync(new RefreshToken
        {
            TokenHash = _tokenService.HashRefreshToken(rawRefreshToken),
            UserId = user.Id,
            ExpiresAtUtc = _dateTimeProvider.UtcNow.AddDays(
                _configuration.GetValue<int>("Jwt:RefreshTokenDays", 7)),
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByUserId = user.Id
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TokenResponse(
            accessToken.Token,
            rawRefreshToken,
            accessToken.ExpiresAtUtc,
            MapUser(user));
    }

    private Task<AppUser?> LoadUserAsync(
        System.Linq.Expressions.Expression<Func<AppUser, bool>> predicate,
        CancellationToken cancellationToken) =>
        _unitOfWork.Users.Table
            .Include(x => x.Institution)
            .Include(x => x.AcademicClass)
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(predicate, cancellationToken);

    private static UserSummary MapUser(AppUser user) =>
        new(
            user.Id,
            user.FullName,
            user.UserName,
            user.Email,
            user.InstitutionId,
            user.Institution.Name,
            user.UserRoles.Select(x => x.Role.Name).ToArray(),
            user.AcademicClassId,
            user.AcademicClass?.Name);
}
