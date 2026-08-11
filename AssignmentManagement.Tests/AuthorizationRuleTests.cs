using Xunit;
using AssignmentManagement.Core.Enums;
using AssignmentManagement.Core.Rules;

namespace AssignmentManagement.Tests;

public sealed class AuthorizationRuleTests
{
    [Fact]
    public void Admin_Can_View_Any_Assignment()
    {
        var allowed = AuthorizationRules.CanViewAssignment(
            new[] { AppRoles.Admin }, 1, null, 99, 5, AssignmentStatus.Draft);
        Assert.True(allowed);
    }

    [Fact]
    public void Teacher_Can_View_Only_Own_Assignment()
    {
        var own = AuthorizationRules.CanViewAssignment(
            new[] { AppRoles.Teacher }, 10, null, 10, 5, AssignmentStatus.Draft);
        var other = AuthorizationRules.CanViewAssignment(
            new[] { AppRoles.Teacher }, 10, null, 11, 5, AssignmentStatus.Published);

        Assert.True(own);
        Assert.False(other);
    }

    [Fact]
    public void Student_Can_View_Published_Assignment_For_Own_Class()
    {
        var allowed = AuthorizationRules.CanViewAssignment(
            new[] { AppRoles.Student }, 20, 5, 10, 5, AssignmentStatus.Published);
        Assert.True(allowed);
    }

    [Fact]
    public void Student_Cannot_View_Draft_Or_Other_Class_Assignment()
    {
        var draft = AuthorizationRules.CanViewAssignment(
            new[] { AppRoles.Student }, 20, 5, 10, 5, AssignmentStatus.Draft);
        var otherClass = AuthorizationRules.CanViewAssignment(
            new[] { AppRoles.Student }, 20, 5, 10, 6, AssignmentStatus.Published);

        Assert.False(draft);
        Assert.False(otherClass);
    }

    [Fact]
    public void Submission_Access_Is_Isolated_By_Role_And_Ownership()
    {
        Assert.True(AuthorizationRules.CanViewSubmission(new[] { AppRoles.Admin }, 1, 30, 40));
        Assert.True(AuthorizationRules.CanViewSubmission(new[] { AppRoles.Student }, 30, 30, 40));
        Assert.False(AuthorizationRules.CanViewSubmission(new[] { AppRoles.Student }, 31, 30, 40));
        Assert.True(AuthorizationRules.CanViewSubmission(new[] { AppRoles.Teacher }, 40, 30, 40));
        Assert.False(AuthorizationRules.CanViewSubmission(new[] { AppRoles.Teacher }, 41, 30, 40));
    }
}
