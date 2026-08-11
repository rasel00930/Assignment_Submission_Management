using AssignmentManagement.Core.Enums;

namespace AssignmentManagement.Core.Rules;

public static class AuthorizationRules
{
    public static bool CanViewAssignment(
        IReadOnlyCollection<string> roles,
        long currentUserId,
        long? currentClassId,
        long teacherId,
        long classId,
        AssignmentStatus status)
    {
        if (roles.Contains(AppRoles.Admin))
            return true;

        if (roles.Contains(AppRoles.Teacher))
            return teacherId == currentUserId;

        return roles.Contains(AppRoles.Student)
               && currentClassId == classId
               && status is AssignmentStatus.Published or AssignmentStatus.Closed;
    }

    public static bool CanViewSubmission(
        IReadOnlyCollection<string> roles,
        long currentUserId,
        long studentId,
        long assignmentTeacherId)
    {
        return roles.Contains(AppRoles.Admin)
               || roles.Contains(AppRoles.Student) && currentUserId == studentId
               || roles.Contains(AppRoles.Teacher) && currentUserId == assignmentTeacherId;
    }
}
