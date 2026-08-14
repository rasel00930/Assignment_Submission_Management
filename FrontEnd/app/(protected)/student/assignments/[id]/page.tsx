"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  ArrowLeft,
  Award,
  CalendarClock,
  CheckCircle2,
  Download,
  Eye,
  FileUp,
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
import { AssignmentStatus, SubmissionStatus, type AssignmentResponse, type SubmissionResponse } from "@/lib/types";
import { deadlineState, errorMessage, formatDate } from "@/lib/utils";

const schema = z.object({
  answerText: z.string().max(20000),
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
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileAction, setFileAction] = useState<"view" | "download" | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
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
    if (!values.answerText.trim() && !selectedFile && !submission?.fileName) {
      form.setError("answerText", { message: "Write an answer or attach a file" });
      return;
    }

    try {
      const result = await submissionService.submit(assignmentId, values.answerText, selectedFile);
      setSubmission(result);
      setSelectedFile(null);
      if (fileInputRef.current) fileInputRef.current.value = "";
      form.reset({ answerText: result.answerText });
      toast.success(submission ? "Submission updated" : "Assignment submitted");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const chooseFile = (file: File | null) => {
    if (!file) {
      setSelectedFile(null);
      return;
    }
    if (!/\.(jpe?g|png|pdf)$/i.test(file.name)) {
      toast.error("Only JPG, JPEG, PNG, and PDF files are allowed.");
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }
    if (file.size > 10 * 1024 * 1024) {
      toast.error("The file cannot be larger than 10 MB.");
      if (fileInputRef.current) fileInputRef.current.value = "";
      return;
    }
    setSelectedFile(file);
    form.clearErrors("answerText");
  };

  const handleFileAction = async (action: "view" | "download") => {
    if (!submission?.fileName) return;
    setFileAction(action);
    try {
      if (action === "view") await submissionService.viewFile(submission.id);
      else await submissionService.downloadFile(submission.id, submission.fileName);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setFileAction(null);
    }
  };

  if (loading) return <InlineLoader label="Loading assignment..." />;
  if (!item) return <Card><EmptyState title="Assignment not found" /></Card>;

  const deadline = deadlineState(item.deadlineUtc);
  const graded = submission?.status === SubmissionStatus.Graded;
  const published = item.status === AssignmentStatus.Published;
  const canSubmitFirst = published && !submission && (!deadline.expired || item.lateSubmissionEnabled);
  const canResubmit = published && !!submission && !deadline.expired && !graded && item.resubmissionEnabled;
  const canEdit = canSubmitFirst || canResubmit;
  const isLateFirstSubmission = canSubmitFirst && deadline.expired;
  const blockedMessage = !published
    ? "This assignment is closed and no longer accepts submissions."
    : graded
      ? "This submission has been graded and can no longer be edited."
      : submission && deadline.expired
        ? "The deadline has passed. Existing submissions cannot be updated after the deadline."
        : submission && !item.resubmissionEnabled
          ? "Resubmission is disabled by the assignment or institution policy."
          : "The deadline has passed and late submission is not enabled for this assignment.";
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
            <div className={`mt-3 rounded-xl p-3 text-sm font-semibold ${item.fileUploadEnabled ? "bg-indigo-50 text-indigo-700" : "bg-slate-100 text-slate-600"}`}>
              {item.fileUploadEnabled ? "Answer file upload is allowed (JPG, JPEG, PNG, or PDF; maximum 10 MB)." : "This assignment does not accept answer file uploads."}
            </div>
            <div className="mt-3 grid gap-3 sm:grid-cols-3">
              <div className={`rounded-xl p-3 text-sm font-semibold ${item.resubmissionEnabled ? "bg-sky-50 text-sky-700" : "bg-slate-100 text-slate-600"}`}>
                {item.resubmissionEnabled ? "You may update your submission before the deadline." : "Resubmission is disabled for this assignment."}
              </div>
              <div className={`rounded-xl p-3 text-sm font-semibold ${item.lateSubmissionEnabled ? "bg-amber-50 text-amber-700" : "bg-slate-100 text-slate-600"}`}>
                {item.lateSubmissionEnabled ? "One late first submission is allowed after the deadline." : "Late submission is disabled for this assignment."}
              </div>
              <div className={`rounded-xl p-3 text-sm font-semibold ${item.gradesVisibleImmediately ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-600"}`}>
                {item.gradesVisibleImmediately ? "Graded results are visible immediately." : "Graded results become visible after the assignment is closed."}
              </div>
            </div>
            <div className={`mt-5 rounded-xl p-4 text-sm font-semibold ${deadline.expired ? item.lateSubmissionEnabled && !submission ? "bg-amber-50 text-amber-700" : "bg-rose-50 text-rose-700" : "bg-emerald-50 text-emerald-700"}`}>
              {deadline.expired
                ? item.lateSubmissionEnabled && !submission
                  ? "The deadline has passed, but this assignment allows one late first submission."
                  : "The deadline has passed."
                : `Deadline ${deadline.label}.`}
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
              <div><label className="field-label">Your answer</label><Textarea className="min-h-64" disabled={!canEdit} placeholder={item.fileUploadEnabled ? "Write your answer here, or attach an answer file below..." : "Write your complete answer here..."} {...form.register("answerText")} />{form.formState.errors.answerText && <p className="field-error">{form.formState.errors.answerText.message}</p>}</div>
              {submission?.fileName && <div className="rounded-2xl border border-indigo-100 bg-indigo-50/60 p-4"><div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><div className="min-w-0"><p className="flex items-center gap-2 text-sm font-bold text-indigo-900"><FileUp className="h-4 w-4" /> Submitted file</p><p className="mt-1 truncate text-sm text-slate-700">{submission.fileName}</p><p className="mt-1 text-xs text-slate-500">{formatFileSize(submission.fileSize)}</p></div><div className="flex gap-2"><Button type="button" variant="secondary" size="sm" loading={fileAction === "view"} onClick={() => void handleFileAction("view")}><Eye className="h-4 w-4" /> View</Button><Button type="button" variant="secondary" size="sm" loading={fileAction === "download"} onClick={() => void handleFileAction("download")}><Download className="h-4 w-4" /> Download</Button></div></div></div>}
              {item.fileUploadEnabled && canEdit && <div><label className="field-label">{submission?.fileName ? "Replace answer file (optional)" : "Answer file (optional)"}</label><label className="flex cursor-pointer flex-col items-center justify-center rounded-2xl border-2 border-dashed border-slate-200 bg-slate-50 px-5 py-6 text-center transition hover:border-indigo-300 hover:bg-indigo-50/40"><FileUp className="h-7 w-7 text-indigo-500"/><span className="mt-2 text-sm font-bold text-slate-700">{selectedFile ? selectedFile.name : "Choose a JPG, JPEG, PNG, or PDF file"}</span><span className="mt-1 text-xs text-slate-500">Maximum file size: 10 MB</span><input ref={fileInputRef} type="file" className="sr-only" accept=".jpg,.jpeg,.png,.pdf,image/jpeg,image/png,application/pdf" onChange={(event) => chooseFile(event.target.files?.[0] ?? null)} /></label>{selectedFile && <button type="button" className="mt-2 text-xs font-bold text-rose-600" onClick={() => { setSelectedFile(null); if (fileInputRef.current) fileInputRef.current.value = ""; }}>Remove selected file</button>}</div>}
              {canEdit ? <Button type="submit" className="w-full" loading={form.formState.isSubmitting}>{submission ? <Save className="h-4 w-4" /> : <Send className="h-4 w-4" />}{submission ? "Update submission" : isLateFirstSubmission ? "Submit late" : "Submit assignment"}</Button> : <p className="rounded-xl bg-slate-100 p-3 text-center text-sm font-semibold text-slate-600">{blockedMessage}</p>}
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

function formatFileSize(bytes?: number | null) {
  if (bytes == null) return "";
  return bytes < 1024 * 1024 ? `${Math.ceil(bytes / 1024)} KB` : `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
