import Link from "next/link";
import { CalendarClock, CheckCircle2, FileText, Users } from "lucide-react";
import { Card } from "@/components/ui/card";
import { AssignmentStatusBadge } from "@/components/common/status-badges";
import { deadlineState, formatDate } from "@/lib/utils";
import type { AssignmentResponse } from "@/lib/types";

export function AssignmentCard({ assignment, href }: { assignment: AssignmentResponse; href: string }) {
  const deadline = deadlineState(assignment.deadlineUtc);
  return (
    <Link href={href} className="group block">
      <Card className="h-full p-5 transition duration-200 hover:-translate-y-0.5 hover:border-indigo-200 hover:shadow-xl">
        <div className="flex items-start justify-between gap-3">
          <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600 transition group-hover:bg-indigo-600 group-hover:text-white">
            <FileText className="h-5 w-5" />
          </div>
          <AssignmentStatusBadge status={assignment.status} />
        </div>
        <h3 className="mt-4 line-clamp-2 text-lg font-extrabold text-slate-900 group-hover:text-indigo-700">{assignment.title}</h3>
        <p className="mt-2 line-clamp-2 text-sm leading-6 text-slate-500">{assignment.description}</p>
        <div className="mt-4 flex flex-wrap gap-2 text-xs font-semibold text-slate-600">
          <span className="rounded-lg bg-slate-100 px-2.5 py-1.5">{assignment.subjectName}</span>
          <span className="rounded-lg bg-slate-100 px-2.5 py-1.5">{assignment.className}{assignment.section ? ` · ${assignment.section}` : ""}</span>
        </div>
        <div className="mt-5 space-y-2 border-t border-slate-100 pt-4 text-xs text-slate-500">
          <div className="flex items-center gap-2"><CalendarClock className="h-4 w-4" /><span>{formatDate(assignment.deadlineUtc)}</span><span className={deadline.expired ? "font-semibold text-rose-600" : "font-semibold text-emerald-600"}>({deadline.label})</span></div>
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-2"><CheckCircle2 className="h-4 w-4" />{assignment.maximumMarks} marks</span>
            <span className="flex items-center gap-2"><Users className="h-4 w-4" />{assignment.submissionCount} submissions</span>
          </div>
        </div>
      </Card>
    </Link>
  );
}
