using AssignmentManagement.Core.Models.Entities;
using Xunit;

namespace AssignmentManagement.Tests;

public sealed class PasswordResetCodeTests
{
    private static readonly DateTime Now = new(2026, 8, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Active_Unused_Unexpired_Code_Is_Usable()
    {
        var code = CreateCode();

        Assert.True(code.IsUsable(Now, maxFailedAttempts: 5));
    }

    [Fact]
    public void Expired_Code_Is_Not_Usable()
    {
        var code = CreateCode();
        code.ExpiresAtUtc = Now.AddSeconds(-1);

        Assert.False(code.IsUsable(Now, maxFailedAttempts: 5));
    }

    [Fact]
    public void Used_Code_Is_Not_Usable()
    {
        var code = CreateCode();
        code.UsedAtUtc = Now;

        Assert.False(code.IsUsable(Now, maxFailedAttempts: 5));
    }

    [Fact]
    public void Code_At_Attempt_Limit_Is_Not_Usable()
    {
        var code = CreateCode();
        code.FailedAttempts = 5;

        Assert.False(code.IsUsable(Now, maxFailedAttempts: 5));
    }

    private static PasswordResetCode CreateCode() =>
        new()
        {
            CodeHash = "HASH",
            ExpiresAtUtc = Now.AddMinutes(10),
            IsActive = true
        };
}
