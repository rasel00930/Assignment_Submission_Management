"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import { CalendarClock, Eye, Plus, Search } from "lucide-react";
import { toast } from "sonner";
import { AssignmentCard } from "@/components/common/assignment-card";
import { AssignmentStatusBadge } from "@/components/common/status-badges";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { InlineLoader } from "@/components/ui/loading";
import { PageHeader } from "@/components/ui/page-header";
import { Pagination } from "@/components/ui/pagination";
import { Select } from "@/components/ui/select";
import { assignmentService, adminService } from "@/lib/services";
import { AssignmentStatus, type AssignmentResponse, type ClassResponse, type SubjectResponse } from "@/lib/types";
import { assignmentStatusLabel, defaultPageSize } from "@/lib/constants";
import { errorMessage, formatDate } from "@/lib/utils";

export function AssignmentListPage({ mode }: { mode: "admin" | "teacher" | "student" }) {
  const [items, setItems] = useState<AssignmentResponse[]>([]);
  const [classes, setClasses] = useState<ClassResponse[]>([]);
  const [subjects, setSubjects] = useState<SubjectResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [classId, setClassId] = useState("");
  const [subjectId, setSubjectId] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await assignmentService.get({
        pageNumber: page,
        pageSize: mode === "student" ? 9 : defaultPageSize,
        search: search || undefined,
        status: status ? status as AssignmentStatus : mode === "student" ? AssignmentStatus.Published : undefined,
        academicClassId: classId ? Number(classId) : undefined,
        subjectId: subjectId ? Number(subjectId) : undefined,
      });
      setItems(result.items); setTotalPages(result.totalPages); setTotalCount(result.totalCount);
    } catch (error) { toast.error(errorMessage(error)); }
    finally { setLoading(false); }
  }, [classId, mode, page, search, status, subjectId]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => {
    if (mode === "student") return;
    Promise.all([adminService.getClasses(false), adminService.getSubjects(false)])
      .then(([c, s]) => { setClasses(c); setSubjects(s); })
      .catch(() => undefined);
  }, [mode]);

  const base = `/${mode}/assignments`;
  const title = mode === "teacher" ? "My assignments" : mode === "student" ? "Assignments" : "All assignments";
  const description = mode === "teacher" ? "Create, publish and monitor work for your classes." : mode === "student" ? "View published work assigned to your class." : "Review assignments across the institution.";

  return <>
    <PageHeader title={title} description={description} actions={mode === "teacher" ? <Link href="/teacher/assignments/new"><Button><Plus className="h-4 w-4" /> New assignment</Button></Link> : undefined} />
    <Card>
      <div className="grid gap-3 border-b border-slate-100 p-4 md:grid-cols-2 xl:grid-cols-4">
        <div className="relative"><Search className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400" /><Input className="pl-10" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="Search assignments..." /></div>
        {mode !== "student" && <Select value={status} onChange={(e) => { setStatus(e.target.value); setPage(1); }}><option value="">All statuses</option>{Object.entries(assignmentStatusLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</Select>}
        {mode !== "student" && <Select value={classId} onChange={(e) => { setClassId(e.target.value); setPage(1); }}><option value="">All classes</option>{classes.map((x) => <option key={x.id} value={x.id}>{x.name}{x.section ? ` - ${x.section}` : ""}</option>)}</Select>}
        {mode !== "student" && <Select value={subjectId} onChange={(e) => { setSubjectId(e.target.value); setPage(1); }}><option value="">All subjects</option>{subjects.map((x) => <option key={x.id} value={x.id}>{x.name}</option>)}</Select>}
      </div>
      {loading ? <InlineLoader /> : items.length === 0 ? <EmptyState title="No assignments found" description={mode === "teacher" ? "Create your first assignment or adjust the filters." : "No assignments match your current filters."} /> : mode === "student" ? (
        <div className="grid gap-4 p-4 md:grid-cols-2 xl:grid-cols-3">{items.map((item) => <AssignmentCard key={item.id} assignment={item} href={`${base}/${item.id}`} />)}</div>
      ) : (
        <div className="overflow-x-auto"><table className="w-full min-w-[950px]"><thead className="table-head"><tr><th className="px-4 py-3">Assignment</th><th className="px-4 py-3">Class & subject</th><th className="px-4 py-3">Teacher</th><th className="px-4 py-3">Deadline</th><th className="px-4 py-3">Status</th><th className="px-4 py-3">Submissions</th><th className="px-4 py-3 text-right">View</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className="hover:bg-slate-50"><td className="table-cell"><p className="max-w-xs truncate font-bold text-slate-800">{item.title}</p><p className="mt-1 text-xs text-slate-500">{item.maximumMarks} marks</p></td><td className="table-cell"><p className="font-semibold">{item.className}{item.section ? ` · ${item.section}` : ""}</p><p className="text-xs text-slate-500">{item.subjectName}</p></td><td className="table-cell">{item.teacherName}</td><td className="table-cell"><div className="flex items-center gap-2"><CalendarClock className="h-4 w-4 text-slate-400" />{formatDate(item.deadlineUtc)}</div></td><td className="table-cell"><AssignmentStatusBadge status={item.status} /></td><td className="table-cell font-bold">{item.submissionCount}</td><td className="table-cell text-right"><Link href={`${base}/${item.id}`}><Button variant="ghost" size="icon"><Eye className="h-4 w-4" /></Button></Link></td></tr>)}</tbody></table></div>
      )}
      <Pagination page={page} totalPages={totalPages} totalCount={totalCount} onPageChange={setPage} />
    </Card>
  </>;
}
