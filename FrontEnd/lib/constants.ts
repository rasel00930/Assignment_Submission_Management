import { AssignmentStatus, SubmissionStatus, type Role } from "@/lib/types";

export const roles: Role[] = ["Admin", "Teacher", "Student"];

export const assignmentStatusLabel: Record<AssignmentStatus, string> = {
  [AssignmentStatus.Draft]: "Draft",
  [AssignmentStatus.Published]: "Published",
  [AssignmentStatus.Closed]: "Closed",
};

export const submissionStatusLabel: Record<SubmissionStatus, string> = {
  [SubmissionStatus.Submitted]: "Submitted",
  [SubmissionStatus.Resubmitted]: "Resubmitted",
  [SubmissionStatus.UnderReview]: "Under review",
  [SubmissionStatus.Graded]: "Graded",
  [SubmissionStatus.Returned]: "Returned",
  [SubmissionStatus.Late]: "Late",
};

export const defaultPageSize = 10;
