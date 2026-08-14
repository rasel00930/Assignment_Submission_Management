"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { BookOpenCheck, ClipboardCheck, FileText, School, Users } from "lucide-react";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { AssignmentStatusBadge, SubmissionStatusBadge } from "@/components/common/status-badges";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { InlineLoader } from "@/components/ui/loading";
import { StatCard } from "@/components/ui/stat-card";
import { adminService, assignmentService, submissionService } from "@/lib/services";
import type { AssignmentResponse, InstitutionResponse, SubmissionResponse } from "@/lib/types";
import { errorMessage, formatDate } from "@/lib/utils";

interface State {
  users: number;
  classes: number;
  subjects: number;
  assignments: number;
  submissions: number;
  institution?: InstitutionResponse;
  recentAssignments: AssignmentResponse[];
  recentSubmissions: SubmissionResponse[];
}

export default function AdminDashboardPage() {
  const [state, setState] = useState<State | null>(null);
  useEffect(() => {
    const load = async () => {
      try {
        const [users, classes, subjects, assignments, submissions, institution] = await Promise.all([
          adminService.getUsers({ pageNumber: 1, pageSize: 5 }),
          adminService.getClasses(false),
          adminService.getSubjects(false),
          assignmentService.get({ pageNumber: 1, pageSize: 5 }),
          submissionService.get({ pageNumber: 1, pageSize: 5 }),
          adminService.getInstitution(),
        ]);
        setState({
          users: users.totalCount,
          classes: classes.length,
          subjects: subjects.length,
          assignments: assignments.totalCount,
          submissions: submissions.totalCount,
          institution,
          recentAssignments: assignments.items,
          recentSubmissions: submissions.items,
        });
      } catch (error) {
        toast.error(errorMessage(error));
      }
    };
    void load();
  }, []);

  return (
    <RolePage role="Admin">
      {!state ? <InlineLoader label="Loading dashboard..." /> : (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
            <StatCard label="Total users" value={state.users} icon={Users} tone="indigo" />
            <StatCard label="Active classes" value={state.classes} icon={School} tone="sky" />
            <StatCard label="Subjects" value={state.subjects} icon={BookOpenCheck} tone="emerald" />
            <StatCard label="Assignments" value={state.assignments} icon={FileText} tone="amber" />
            <StatCard label="Submissions" value={state.submissions} icon={ClipboardCheck} tone="rose" />
          </div>

          <div className="mt-6 grid gap-6 xl:grid-cols-2">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div><h2 className="font-extrabold text-slate-900">Recent assignments</h2><p className="mt-1 text-sm text-slate-500">Latest work created by teachers.</p></div>
                <Link href="/admin/assignments" className="text-sm font-bold text-indigo-600 hover:text-indigo-700">View all</Link>
              </CardHeader>
              <CardContent className="space-y-3">
                {state.recentAssignments.length === 0 ? <p className="py-10 text-center text-sm text-slate-500">No assignments yet.</p> : state.recentAssignments.map((item) => (
                  <Link key={item.id} href={`/admin/assignments/${item.id}`} className="flex items-center justify-between gap-3 rounded-xl border border-slate-100 p-3 transition hover:border-indigo-200 hover:bg-indigo-50/40">
                    <div className="min-w-0"><p className="truncate font-bold text-slate-800">{item.title}</p><p className="mt-1 truncate text-xs text-slate-500">{item.subjectName} · {item.className} · {item.teacherName}</p></div>
                    <AssignmentStatusBadge status={item.status} />
                  </Link>
                ))}
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between">
                <div><h2 className="font-extrabold text-slate-900">Recent submissions</h2><p className="mt-1 text-sm text-slate-500">Newest student responses.</p></div>
                <Link href="/admin/submissions" className="text-sm font-bold text-indigo-600 hover:text-indigo-700">View all</Link>
              </CardHeader>
              <CardContent className="space-y-3">
                {state.recentSubmissions.length === 0 ? <p className="py-10 text-center text-sm text-slate-500">No submissions yet.</p> : state.recentSubmissions.map((item) => (
                  <div key={item.id} className="flex items-center justify-between gap-3 rounded-xl border border-slate-100 p-3">
                    <div className="min-w-0"><p className="truncate font-bold text-slate-800">{item.studentName}</p><p className="mt-1 truncate text-xs text-slate-500">{item.assignmentTitle} · {formatDate(item.submittedAtUtc)}</p></div>
                    <SubmissionStatusBadge status={item.status} />
                  </div>
                ))}
              </CardContent>
            </Card>
          </div>
        </>
      )}
    </RolePage>
  );
}
