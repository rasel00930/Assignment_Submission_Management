namespace AssignmentManagement.Core.Models.Common;

public sealed record AccessTokenResult(string Token, DateTime ExpiresAtUtc);
