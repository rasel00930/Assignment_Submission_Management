import { Badge } from "@/components/ui/badge";
import { assignmentStatusLabel, submissionStatusLabel } from "@/lib/constants";
import { AssignmentStatus, SubmissionStatus } from "@/lib/types";

export function AssignmentStatusBadge({ status }: { status: AssignmentStatus }) {
  const tone = status === AssignmentStatus.Published ? "emerald" : status === AssignmentStatus.Closed ? "rose" : "amber";
  return <Badge tone={tone}>{assignmentStatusLabel[status]}</Badge>;
}

export function SubmissionStatusBadge({ status }: { status: SubmissionStatus }) {
  const tones: Record<SubmissionStatus, "slate" | "indigo" | "emerald" | "amber" | "rose" | "sky"> = {
    [SubmissionStatus.Submitted]: "sky",
    [SubmissionStatus.Resubmitted]: "indigo",
    [SubmissionStatus.UnderReview]: "amber",
    [SubmissionStatus.Graded]: "emerald",
    [SubmissionStatus.Returned]: "rose",
    [SubmissionStatus.Late]: "rose",
  };
  return <Badge tone={tones[status]}>{submissionStatusLabel[status]}</Badge>;
}
