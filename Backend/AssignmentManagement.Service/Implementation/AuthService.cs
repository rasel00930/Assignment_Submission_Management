using System.Security.Cryptography;
using System.Text;
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
    private readonly IEmailService _emailService;

    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPasswordHasher<AppUser> passwordHasher,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _configuration = configuration;
        _emailService = emailService;
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

    public async Task RequestPasswordResetAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Email == email && x.IsActive,
            trackChanges: true,
            cancellationToken: cancellationToken);

        // Do not reveal whether an email address belongs to an account.
        if (user is null)
            return;

        var now = _dateTimeProvider.UtcNow;
        var minimumRequestInterval = Math.Clamp(
            _configuration.GetValue("PasswordReset:MinimumSecondsBetweenRequests", 60),
            30,
            600);
        var latestCode = await _unitOfWork.PasswordResetCodes.Table
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestCode is not null &&
            latestCode.CreatedAtUtc > now.AddSeconds(-minimumRequestInterval))
            return;

        var activeCodes = await _unitOfWork.PasswordResetCodes.GetAllAsync(
            x => x.UserId == user.Id && x.IsActive && x.UsedAtUtc == null,
            trackChanges: true,
            cancellationToken: cancellationToken);
        foreach (var activeCode in activeCodes)
        {
            activeCode.IsActive = false;
            activeCode.UpdatedAtUtc = now;
        }

        var codeLifetimeMinutes = Math.Clamp(
            _configuration.GetValue("PasswordReset:CodeLifetimeMinutes", 10),
            5,
            30);
        var rawCode = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var resetCode = new PasswordResetCode
        {
            UserId = user.Id,
            CodeHash = HashResetCode(email, rawCode),
            ExpiresAtUtc = now.AddMinutes(codeLifetimeMinutes),
            CreatedAtUtc = now,
            CreatedByUserId = user.Id
        };

        await _unitOfWork.PasswordResetCodes.AddAsync(resetCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        try
        {
            await _emailService.SendPasswordResetCodeAsync(
                user.Email,
                user.FullName,
                rawCode,
                codeLifetimeMinutes,
                cancellationToken);
        }
        catch
        {
            resetCode.IsActive = false;
            resetCode.UpdatedAtUtc = now;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task ResetPasswordWithCodeAsync(
        ResetPasswordWithCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _unitOfWork.Users.FirstOrDefaultAsync(
            x => x.Email == email && x.IsActive,
            trackChanges: true,
            cancellationToken: cancellationToken);

        if (user is null)
            throw InvalidResetCode();

        var now = _dateTimeProvider.UtcNow;
        var maxFailedAttempts = Math.Clamp(
            _configuration.GetValue("PasswordReset:MaxFailedAttempts", 5),
            3,
            10);
        var resetCode = await _unitOfWork.PasswordResetCodes.Table
            .Where(x => x.UserId == user.Id && x.IsActive && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetCode is null || !resetCode.IsUsable(now, maxFailedAttempts))
            throw InvalidResetCode();

        if (!ResetCodeMatches(resetCode.CodeHash, email, request.VerificationCode))
        {
            resetCode.FailedAttempts++;
            resetCode.UpdatedAtUtc = now;
            if (resetCode.FailedAttempts >= maxFailedAttempts)
                resetCode.IsActive = false;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw InvalidResetCode();
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = now;
        user.UpdatedByUserId = user.Id;
        resetCode.UsedAtUtc = now;
        resetCode.IsActive = false;
        resetCode.UpdatedAtUtc = now;
        resetCode.UpdatedByUserId = user.Id;

        var activeTokens = await _unitOfWork.RefreshTokens.GetAllAsync(
            x => x.UserId == user.Id && x.RevokedAtUtc == null,
            trackChanges: true,
            cancellationToken: cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.UpdatedAtUtc = now;
            token.UpdatedByUserId = user.Id;
        }

        var otherCodes = await _unitOfWork.PasswordResetCodes.GetAllAsync(
            x => x.UserId == user.Id && x.Id != resetCode.Id && x.IsActive,
            trackChanges: true,
            cancellationToken: cancellationToken);
        foreach (var code in otherCodes)
        {
            code.IsActive = false;
            code.UpdatedAtUtc = now;
            code.UpdatedByUserId = user.Id;
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

    private string HashResetCode(string email, string code)
    {
        var hashKey = _configuration["PasswordReset:HashKey"]
            ?? _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("A password-reset hash key is required.");
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(hashKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes($"{email}:{code}")));
    }

    private bool ResetCodeMatches(string storedHash, string email, string suppliedCode)
    {
        try
        {
            var storedBytes = Convert.FromHexString(storedHash);
            var suppliedBytes = Convert.FromHexString(HashResetCode(email, suppliedCode.Trim()));
            return CryptographicOperations.FixedTimeEquals(storedBytes, suppliedBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static AppException InvalidResetCode() =>
        new(400, "The verification code is invalid or expired.");
}
