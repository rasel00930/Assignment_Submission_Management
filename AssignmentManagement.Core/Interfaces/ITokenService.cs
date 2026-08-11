using AssignmentManagement.Core.Models.Common;
using AssignmentManagement.Core.Models.Entities;

namespace AssignmentManagement.Core.Interfaces;

public interface ITokenService
{
    Task<AccessTokenResult> CreateAccessTokenAsync(AppUser user, CancellationToken cancellationToken = default);
    string GenerateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
