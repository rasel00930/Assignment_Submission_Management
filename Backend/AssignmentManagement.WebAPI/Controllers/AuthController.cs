using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.DTO;
using AssignmentManagement.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssignmentManagement.WebAPI.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<GeneralResponse<TokenResponse>>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        return Ok(GeneralResponse<TokenResponse>.Ok(result, "Login successful."));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<ActionResult<GeneralResponse<TokenResponse>>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(request, cancellationToken);
        return Ok(GeneralResponse<TokenResponse>.Ok(result, "Token refreshed."));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<ActionResult<GeneralResponse<object>>> Logout(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Logged out."));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<GeneralResponse<object>>> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _authService.ChangePasswordAsync(request, cancellationToken);
        return Ok(GeneralResponse<object>.Ok(null, "Password changed. Please log in again."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<GeneralResponse<UserSummary>>> Me(CancellationToken cancellationToken)
    {
        var result = await _authService.GetCurrentUserAsync(cancellationToken);
        return Ok(GeneralResponse<UserSummary>.Ok(result));
    }
}
