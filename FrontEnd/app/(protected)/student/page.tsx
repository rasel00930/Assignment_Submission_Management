"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ArrowUpRight, Award, ClipboardCheck, Clock3, FileText, MessageSquare } from "lucide-react";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { AssignmentCard } from "@/components/common/assignment-card";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { InlineLoader } from "@/components/ui/loading";
import { StatCard } from "@/components/ui/stat-card";
import { assignmentService, submissionService } from "@/lib/services";
import { AssignmentStatus, SubmissionStatus, type AssignmentResponse, type SubmissionResponse } from "@/lib/types";
import { errorMessage, formatDate } from "@/lib/utils";

export default function StudentDashboardPage() {
  const [loading, setLoading] = useState(true);
  const [assignments, setAssignments] = useState<AssignmentResponse[]>([]);
  const [recentResults, setRecentResults] = useState<SubmissionResponse[]>([]);
  const [counts, setCounts] = useState({ available: 0, submitted: 0, graded: 0, pending: 0 });

  useEffect(() => {
    const load = async () => {
      try {
        const [assignmentData, submissions] = await Promise.all([
          assignmentService.get({ pageNumber: 1, pageSize: 6, status: AssignmentStatus.Published }),
          submissionService.get({ pageNumber: 1, pageSize: 100 }),
        ]);
        setAssignments(assignmentData.items);
        setRecentResults(submissions.items.filter((item) => item.marks != null || item.feedback || item.status === SubmissionStatus.Graded).slice(0, 3));
        setCounts({
          available: assignmentData.totalCount,
          submitted: submissions.totalCount,
          graded: submissions.items.filter((x) => x.status === SubmissionStatus.Graded).length,
          pending: submissions.items.filter((x) => x.status !== SubmissionStatus.Graded).length,
        });
      } catch (error) {
        toast.error(errorMessage(error));
      } finally { setLoading(false); }
    };
    void load();
  }, []);

  return (
    <RolePage role="Student">
      {loading ? <InlineLoader label="Loading dashboard..." /> : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <StatCard label="Available assignments" value={counts.available} icon={FileText} tone="indigo" />
            <StatCard label="My submissions" value={counts.submitted} icon={ClipboardCheck} tone="sky" />
            <StatCard label="Awaiting review" value={counts.pending} icon={Clock3} tone="amber" />
            <StatCard label="Graded" value={counts.graded} icon={Award} tone="emerald" />
          </div>
          <Card className="mt-6">
            <CardHeader className="flex flex-row items-center justify-between">
              <div><h2 className="font-extrabold text-slate-900">Current assignments</h2><p className="mt-1 text-sm text-slate-500">Published assignments for your class.</p></div>
              <Link href="/student/assignments" className="text-sm font-bold text-indigo-600">View all</Link>
            </CardHeader>
            <CardContent>
              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                {assignments.length === 0 ? <p className="col-span-full py-12 text-center text-sm text-slate-500">No published assignments are available.</p> : assignments.map((item) => <AssignmentCard key={item.id} assignment={item} href={`/student/assignments/${item.id}`} />)}
              </div>
            </CardContent>
          </Card>
          <Card className="mt-6 overflow-hidden">
            <CardHeader className="flex flex-row items-center justify-between gap-4 bg-gradient-to-r from-emerald-50/80 to-sky-50/80">
              <div><h2 className="font-extrabold text-slate-900">Recent results</h2><p className="mt-1 text-sm text-slate-500">Your latest marks and teacher feedback.</p></div>
              <Link href="/student/submissions" className="inline-flex items-center gap-1 text-sm font-bold text-indigo-600">View all <ArrowUpRight className="h-4 w-4" /></Link>
            </CardHeader>
            <CardContent>
              {recentResults.length === 0 ? <div className="py-10 text-center"><Award className="mx-auto h-10 w-10 text-slate-300"/><p className="mt-3 font-bold text-slate-700">No results published yet</p><p className="mt-1 text-sm text-slate-500">Marks and feedback will appear here after your teacher reviews a submission.</p></div> : <div className="grid gap-4 lg:grid-cols-3">{recentResults.map((result) => { const percentage=result.marks!=null&&result.assignmentMaximumMarks>0?Math.round((result.marks/result.assignmentMaximumMarks)*100):null; return <Link key={result.id} href={`/student/assignments/${result.assignmentId}`} className="group rounded-2xl border border-slate-200 bg-white p-4 transition hover:-translate-y-0.5 hover:border-emerald-200 hover:shadow-lg"><div className="flex items-start justify-between gap-3"><div className="flex h-10 w-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600"><Award className="h-5 w-5"/></div>{percentage!=null&&<span className="rounded-full bg-emerald-100 px-2.5 py-1 text-xs font-extrabold text-emerald-700">{percentage}%</span>}</div><h3 className="mt-4 line-clamp-2 font-extrabold text-slate-900 group-hover:text-indigo-700">{result.assignmentTitle}</h3><div className="mt-3 flex items-end gap-1"><span className="text-3xl font-black text-emerald-700">{result.marks??"—"}</span><span className="pb-1 text-sm font-bold text-slate-400">/ {result.assignmentMaximumMarks}</span></div>{result.feedback&&<p className="mt-3 line-clamp-2 flex gap-2 text-xs leading-5 text-slate-500"><MessageSquare className="mt-0.5 h-3.5 w-3.5 shrink-0"/>{result.feedback}</p>}<p className="mt-3 text-xs font-medium text-slate-400">Reviewed {formatDate(result.reviewedAtUtc??result.submittedAtUtc)}</p></Link>;})}</div>}
            </CardContent>
          </Card>
        </>
      )}
    </RolePage>
  );
}
