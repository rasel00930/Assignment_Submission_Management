"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  ArrowLeft,
  Award,
  CalendarClock,
  CheckCircle2,
  MessageSquare,
  RefreshCw,
  Save,
  Send,
  UserRound,
} from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { AssignmentStatusBadge, SubmissionStatusBadge } from "@/components/common/status-badges";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { InlineLoader } from "@/components/ui/loading";
import { PageHeader } from "@/components/ui/page-header";
import { Textarea } from "@/components/ui/textarea";
import { assignmentService, submissionService } from "@/lib/services";
import { SubmissionStatus, type AssignmentResponse, type SubmissionResponse } from "@/lib/types";
import { deadlineState, errorMessage, formatDate } from "@/lib/utils";

const schema = z.object({
  answerText: z.string().trim().min(1, "Answer is required").max(20000),
});
type FormValues = z.infer<typeof schema>;

export default function StudentAssignmentDetail() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const assignmentId = Number(id);
  const [item, setItem] = useState<AssignmentResponse | null>(null);
  const [submission, setSubmission] = useState<SubmissionResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { answerText: "" },
  });

  const load = useCallback(async (showInitialLoader = false) => {
    if (showInitialLoader) setLoading(true);
    try {
      const [assignment, submissions] = await Promise.all([
        assignmentService.getById(assignmentId),
        submissionService.get({ assignmentId, pageNumber: 1, pageSize: 5 }),
      ]);
      const existing = submissions.items[0] ?? null;
      setItem(assignment);
      setSubmission(existing);
      form.reset({ answerText: existing?.answerText ?? "" });
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setLoading(false);
    }
  }, [assignmentId, form]);

  useEffect(() => {
    void load(true);
  }, [load]);

  const refreshResult = async () => {
    setRefreshing(true);
    await load();
    setRefreshing(false);
    toast.success("Submission result refreshed");
  };

  const save = async (values: FormValues) => {
    try {
      const result = await submissionService.submit(assignmentId, values.answerText);
      setSubmission(result);
      form.reset({ answerText: result.answerText });
      toast.success(submission ? "Submission updated" : "Assignment submitted");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  if (loading) return <InlineLoader label="Loading assignment..." />;
  if (!item) return <Card><EmptyState title="Assignment not found" /></Card>;

  const deadline = deadlineState(item.deadlineUtc);
  const graded = submission?.status === SubmissionStatus.Graded;
  const canEdit = !deadline.expired && !graded && (!submission || item.allowResubmission);
  const percentage = submission?.marks != null && submission.assignmentMaximumMarks > 0
    ? Math.round((submission.marks / submission.assignmentMaximumMarks) * 100)
    : null;

  return (
    <RolePage role="Student">
      <PageHeader
        title={item.title}
        description={`${item.subjectName} · ${item.className}`}
        actions={<Button variant="secondary" onClick={() => router.back()}><ArrowLeft className="h-4 w-4" /> Back</Button>}
      />

      <div className="grid gap-6 xl:grid-cols-[1.15fr_.85fr]">
        <Card>
          <CardHeader className="flex flex-row items-start justify-between gap-4">
            <div><p className="text-sm text-slate-500">Assignment instructions</p><h2 className="mt-1 text-xl font-extrabold">{item.title}</h2></div>
            <AssignmentStatusBadge status={item.status} />
          </CardHeader>
          <CardContent>
            <div className="whitespace-pre-wrap text-sm leading-7 text-slate-700">{item.description}</div>
            <div className="mt-6 grid gap-3 sm:grid-cols-3">
              <Mini icon={CalendarClock} label="Deadline" value={formatDate(item.deadlineUtc)} />
              <Mini icon={CheckCircle2} label="Maximum marks" value={String(item.maximumMarks)} />
              <Mini icon={UserRound} label="Teacher" value={item.teacherName} />
            </div>
            <div className={`mt-5 rounded-xl p-4 text-sm font-semibold ${deadline.expired ? "bg-rose-50 text-rose-700" : "bg-emerald-50 text-emerald-700"}`}>
              {deadline.expired ? "The deadline has passed." : `Deadline ${deadline.label}.`}
            </div>
          </CardContent>
        </Card>

        <Card className="overflow-hidden">
          <CardHeader>
            <div className="flex items-center justify-between gap-3">
              <div><h2 className="font-extrabold">Your submission</h2><p className="mt-1 text-sm text-slate-500">{submission ? `Last saved ${formatDate(submission.submittedAtUtc)}` : "No answer submitted yet."}</p></div>
              <div className="flex items-center gap-2">
                {submission && <Button type="button" variant="ghost" size="icon" loading={refreshing} aria-label="Refresh result" onClick={() => void refreshResult()}><RefreshCw className="h-4 w-4" /></Button>}
                {submission && <SubmissionStatusBadge status={submission.status} />}
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-5">
            {submission && (
              <div className={submission.marks != null ? "rounded-2xl border border-emerald-200 bg-gradient-to-br from-emerald-50 to-sky-50 p-5" : "rounded-2xl border border-amber-200 bg-amber-50 p-4"}>
                {submission.marks != null ? <>
                  <div className="flex items-start justify-between gap-4"><div><p className="text-sm font-bold text-emerald-700">Marks awarded</p><p className="mt-1 text-4xl font-black tracking-tight text-emerald-900">{submission.marks}<span className="ml-1 text-lg font-bold text-emerald-600">/ {submission.assignmentMaximumMarks}</span></p></div><div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-white text-emerald-600 shadow-sm"><Award className="h-6 w-6" /></div></div>
                  {percentage != null && <div className="mt-4"><div className="mb-1.5 flex justify-between text-xs font-bold text-emerald-700"><span>Result</span><span>{percentage}%</span></div><div className="h-2 overflow-hidden rounded-full bg-emerald-100"><div className="h-full rounded-full bg-gradient-to-r from-emerald-500 to-sky-500" style={{ width: `${Math.min(100, percentage)}%` }} /></div></div>}
                  {submission.reviewedByTeacherName && <p className="mt-3 text-xs font-medium text-slate-500">Reviewed by {submission.reviewedByTeacherName}{submission.reviewedAtUtc ? ` · ${formatDate(submission.reviewedAtUtc)}` : ""}</p>}
                </> : <div className="flex gap-3"><Award className="mt-0.5 h-5 w-5 shrink-0 text-amber-600" /><div><p className="font-bold text-amber-900">Awaiting teacher review</p><p className="mt-1 text-sm leading-6 text-amber-700">Your marks and feedback will appear here after the review is saved.</p></div></div>}
              </div>
            )}

            {submission?.feedback && <div className="rounded-2xl border border-indigo-100 bg-indigo-50/60 p-4"><p className="flex items-center gap-2 text-sm font-bold text-indigo-900"><MessageSquare className="h-4 w-4 text-indigo-600" /> Teacher feedback</p><p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-slate-700">{submission.feedback}</p></div>}

            <form onSubmit={form.handleSubmit(save)} className="space-y-4">
              <div><label className="field-label">Your answer</label><Textarea className="min-h-64" disabled={!canEdit} placeholder="Write your complete answer here..." {...form.register("answerText")} />{form.formState.errors.answerText && <p className="field-error">{form.formState.errors.answerText.message}</p>}</div>
              {canEdit ? <Button type="submit" className="w-full" loading={form.formState.isSubmitting}>{submission ? <Save className="h-4 w-4" /> : <Send className="h-4 w-4" />}{submission ? "Update submission" : "Submit assignment"}</Button> : <p className="rounded-xl bg-slate-100 p-3 text-center text-sm font-semibold text-slate-600">{graded ? "This submission has been graded and can no longer be edited." : "This submission can no longer be edited."}</p>}
            </form>
          </CardContent>
        </Card>
      </div>
    </RolePage>
  );
}

function Mini({ icon: Icon, label, value }: { icon: React.ComponentType<{ className?: string }>; label: string; value: string }) {
  return <div className="rounded-xl border border-slate-100 bg-slate-50 p-3"><Icon className="h-4 w-4 text-indigo-600" /><p className="mt-2 text-xs font-semibold text-slate-500">{label}</p><p className="mt-1 text-sm font-bold text-slate-800">{value}</p></div>;
}
