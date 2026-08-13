"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Award, ClipboardCheck, Clock3, FileText } from "lucide-react";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { AssignmentCard } from "@/components/common/assignment-card";
import { DashboardWelcome } from "@/components/common/dashboard-welcome";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { InlineLoader } from "@/components/ui/loading";
import { StatCard } from "@/components/ui/stat-card";
import { assignmentService, submissionService } from "@/lib/services";
import { AssignmentStatus, SubmissionStatus, type AssignmentResponse } from "@/lib/types";
import { errorMessage } from "@/lib/utils";

export default function StudentDashboardPage() {
  const [loading, setLoading] = useState(true);
  const [assignments, setAssignments] = useState<AssignmentResponse[]>([]);
  const [counts, setCounts] = useState({ available: 0, submitted: 0, graded: 0, pending: 0 });

  useEffect(() => {
    const load = async () => {
      try {
        const [assignmentData, submissions] = await Promise.all([
          assignmentService.get({ pageNumber: 1, pageSize: 6, status: AssignmentStatus.Published }),
          submissionService.get({ pageNumber: 1, pageSize: 100 }),
        ]);
        setAssignments(assignmentData.items);
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
      <DashboardWelcome message="Stay on top of deadlines, submit your work and review teacher feedback." />
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
        </>
      )}
    </RolePage>
  );
}
