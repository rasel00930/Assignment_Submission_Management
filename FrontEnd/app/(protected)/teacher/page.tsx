"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { BookOpenCheck, ClipboardCheck, FileText, PlusCircle } from "lucide-react";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { DashboardWelcome } from "@/components/common/dashboard-welcome";
import { AssignmentStatusBadge } from "@/components/common/status-badges";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { InlineLoader } from "@/components/ui/loading";
import { StatCard } from "@/components/ui/stat-card";
import { adminService, assignmentService, submissionService } from "@/lib/services";
import type { AssignmentResponse } from "@/lib/types";
import { errorMessage, formatDate } from "@/lib/utils";

export default function TeacherDashboardPage() {
  const [loading, setLoading] = useState(true);
  const [assignments, setAssignments] = useState<AssignmentResponse[]>([]);
  const [counts, setCounts] = useState({ mappings: 0, assignments: 0, submissions: 0 });

  useEffect(() => {
    const load = async () => {
      try {
        const [mappings, assignmentData, submissions] = await Promise.all([
          adminService.getTeacherAssignments({ pageNumber: 1, pageSize: 100, isActive: true }),
          assignmentService.get({ pageNumber: 1, pageSize: 5 }),
          submissionService.get({ pageNumber: 1, pageSize: 1 }),
        ]);
        setAssignments(assignmentData.items);
        setCounts({ mappings: mappings.totalCount, assignments: assignmentData.totalCount, submissions: submissions.totalCount });
      } catch (error) {
        toast.error(errorMessage(error));
      } finally { setLoading(false); }
    };
    void load();
  }, []);

  return (
    <RolePage role="Teacher">
      <DashboardWelcome message="Create meaningful assignments, monitor student progress and provide timely feedback." />
      {loading ? <InlineLoader label="Loading dashboard..." /> : (
        <>
          <div className="grid gap-4 sm:grid-cols-3">
            <StatCard label="Teaching mappings" value={counts.mappings} icon={BookOpenCheck} tone="indigo" />
            <StatCard label="My assignments" value={counts.assignments} icon={FileText} tone="amber" />
            <StatCard label="Student submissions" value={counts.submissions} icon={ClipboardCheck} tone="emerald" />
          </div>
          <Card className="mt-6">
            <CardHeader className="flex flex-row items-center justify-between gap-3">
              <div><h2 className="font-extrabold text-slate-900">Recent assignments</h2><p className="mt-1 text-sm text-slate-500">Continue managing your latest work.</p></div>
              <Link href="/teacher/assignments/new"><Button size="sm"><PlusCircle className="h-4 w-4" /> New assignment</Button></Link>
            </CardHeader>
            <CardContent>
              <div className="grid gap-3 lg:grid-cols-2">
                {assignments.length === 0 ? <p className="col-span-full py-12 text-center text-sm text-slate-500">Create your first assignment to get started.</p> : assignments.map((item) => (
                  <Link key={item.id} href={`/teacher/assignments/${item.id}`} className="rounded-2xl border border-slate-100 p-4 transition hover:border-indigo-200 hover:bg-indigo-50/30">
                    <div className="flex items-start justify-between gap-3"><h3 className="font-extrabold text-slate-800">{item.title}</h3><AssignmentStatusBadge status={item.status} /></div>
                    <p className="mt-2 text-sm text-slate-500">{item.subjectName} · {item.className}</p>
                    <p className="mt-3 text-xs font-semibold text-slate-500">Due {formatDate(item.deadlineUtc)} · {item.submissionCount} submissions</p>
                  </Link>
                ))}
              </div>
            </CardContent>
          </Card>
        </>
      )}
    </RolePage>
  );
}
