using Xunit;
using AssignmentManagement.Core.Exceptions;
using AssignmentManagement.Core.Rules;

namespace AssignmentManagement.Tests;

public sealed class AssignmentRuleTests
{
    [Fact]
    public void Future_Deadline_Is_Accepted()
    {
        var now = new DateTime(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc);
        AssignmentRules.ValidateDeadline(now.AddDays(1), now);
    }

    [Fact]
    public void Expired_Deadline_Is_Rejected()
    {
        var now = new DateTime(2026, 8, 8, 10, 0, 0, DateTimeKind.Utc);
        Assert.Throws<AppException>(() => AssignmentRules.ValidateDeadline(now.AddMinutes(-1), now));
    }

    [Fact]
    public void NonUtc_Deadline_Is_Rejected()
    {
        var now = DateTime.UtcNow;
        var deadline = DateTime.SpecifyKind(now.AddDays(1), DateTimeKind.Unspecified);
        Assert.Throws<AppException>(() => AssignmentRules.ValidateDeadline(deadline, now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void Invalid_Maximum_Marks_Are_Rejected(decimal marks)
    {
        Assert.Throws<AppException>(() => AssignmentRules.ValidateMaximumMarks(marks));
    }
}
