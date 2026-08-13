"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Award, Search } from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { SubmissionStatusBadge } from "@/components/common/status-badges";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { InlineLoader } from "@/components/ui/loading";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Pagination } from "@/components/ui/pagination";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { submissionService } from "@/lib/services";
import { defaultPageSize, submissionStatusLabel } from "@/lib/constants";
import { SubmissionStatus, type SubmissionResponse } from "@/lib/types";
import { errorMessage, formatDate } from "@/lib/utils";

const reviewSchema=z.object({marks:z.string().optional(),feedback:z.string().max(5000).optional(),status:z.string().min(1)});type ReviewForm=z.infer<typeof reviewSchema>;

export function SubmissionListPage({mode}:{mode:"admin"|"teacher"|"student"}){
 const[items,setItems]=useState<SubmissionResponse[]>([]);const[loading,setLoading]=useState(true);const[page,setPage]=useState(1);const[totalPages,setTotalPages]=useState(0);const[totalCount,setTotalCount]=useState(0);const[search,setSearch]=useState("");const[status,setStatus]=useState("");const[assignmentId,setAssignmentId]=useState("");const[selected,setSelected]=useState<SubmissionResponse|null>(null);
 const form=useForm<ReviewForm>({resolver:zodResolver(reviewSchema),defaultValues:{marks:"",feedback:"",status:String(SubmissionStatus.UnderReview)}});
 useEffect(()=>{if(typeof window!=="undefined"){setAssignmentId(new URLSearchParams(window.location.search).get("assignmentId")??"");}},[]);
 const load=useCallback(async()=>{setLoading(true);try{const r=await submissionService.get({pageNumber:page,pageSize:defaultPageSize,search:search||undefined,status:status?Number(status) as SubmissionStatus:undefined,assignmentId:assignmentId?Number(assignmentId):undefined});setItems(r.items);setTotalPages(r.totalPages);setTotalCount(r.totalCount);}catch(e){toast.error(errorMessage(e));}finally{setLoading(false);}},[assignmentId,page,search,status]);useEffect(()=>{void load();},[load]);
 const openReview=(item:SubmissionResponse)=>{setSelected(item);form.reset({marks:item.marks?.toString()??"",feedback:item.feedback??"",status:String(item.status)});};
 const review=async(v:ReviewForm)=>{if(!selected)return;const marks=v.marks===""?null:Number(v.marks);if(marks!=null&&(marks<0||marks>selected.assignmentMaximumMarks)){form.setError("marks",{message:`Marks must be between 0 and ${selected.assignmentMaximumMarks}`});return;}try{await submissionService.review(selected.id,{marks,feedback:v.feedback||null,status:Number(v.status) as SubmissionStatus});toast.success("Submission reviewed");setSelected(null);await load();}catch(e){toast.error(errorMessage(e));}};
 const title=mode==="student"?"My submissions":mode==="teacher"?"Student submissions":"All submissions";const description=mode==="student"?"Track your answers, marks and teacher feedback.":mode==="teacher"?"Review student work and provide marks and feedback.":"Monitor submission activity across the institution.";
 return <><PageHeader title={title} description={description}/><Card><div className="grid gap-3 border-b border-slate-100 p-4 md:grid-cols-3"><div className="relative"><Search className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400"/><Input className="pl-10" value={search} onChange={e=>{setSearch(e.target.value);setPage(1);}} placeholder="Search student or assignment..."/></div><Input value={assignmentId} onChange={e=>{setAssignmentId(e.target.value.replace(/\D/g,""));setPage(1);}} placeholder="Filter by assignment ID"/><Select value={status} onChange={e=>{setStatus(e.target.value);setPage(1);}}><option value="">All statuses</option>{Object.entries(submissionStatusLabel).map(([value,label])=><option key={value} value={value}>{label}</option>)}</Select></div>
 {loading?<InlineLoader/>:items.length===0?<EmptyState title="No submissions found" description="No records match your current filters."/>:<div className="overflow-x-auto"><table className="w-full min-w-[950px]"><thead className="table-head"><tr><th className="px-4 py-3">Assignment</th>{mode!=="student"&&<th className="px-4 py-3">Student</th>}<th className="px-4 py-3">Submitted</th><th className="px-4 py-3">Status</th><th className="px-4 py-3">Marks</th><th className="px-4 py-3">Feedback</th>{mode==="teacher"&&<th className="px-4 py-3 text-right">Review</th>}</tr></thead><tbody>{items.map(item=><tr key={item.id} className="hover:bg-slate-50"><td className="table-cell"><p className="max-w-xs truncate font-bold text-slate-800">{item.assignmentTitle}</p><p className="mt-1 text-xs text-slate-500">Assignment #{item.assignmentId}</p></td>{mode!=="student"&&<td className="table-cell"><p className="font-bold">{item.studentName}</p><p className="text-xs text-slate-500">@{item.studentUserName}</p></td>}<td className="table-cell">{formatDate(item.submittedAtUtc)}</td><td className="table-cell"><SubmissionStatusBadge status={item.status}/></td><td className="table-cell"><span className="font-extrabold">{item.marks??"—"}</span> / {item.assignmentMaximumMarks}</td><td className="table-cell"><p className="max-w-xs truncate text-sm text-slate-500">{item.feedback||"—"}</p></td>{mode==="teacher"&&<td className="table-cell text-right"><Button variant="secondary" size="sm" onClick={()=>openReview(item)}><Award className="h-4 w-4"/> Review</Button></td>}</tr>)}</tbody></table></div>}<Pagination page={page} totalPages={totalPages} totalCount={totalCount} onPageChange={setPage}/></Card>
 <Modal open={!!selected} onClose={()=>setSelected(null)} title="Review submission" description={`${selected?.studentName??"Student"} · ${selected?.assignmentTitle??"Assignment"}`} size="lg"><div className="mb-5 rounded-2xl bg-slate-50 p-4"><p className="text-xs font-bold uppercase tracking-wide text-slate-400">Student answer</p><p className="mt-3 whitespace-pre-wrap text-sm leading-7 text-slate-700">{selected?.answerText}</p></div><form className="space-y-4" onSubmit={form.handleSubmit(review)}><div className="grid gap-4 sm:grid-cols-2"><div><label className="field-label">Marks (max {selected?.assignmentMaximumMarks})</label><Input type="number" step="0.01" {...form.register("marks")}/>{form.formState.errors.marks&&<p className="field-error">{form.formState.errors.marks.message}</p>}</div><div><label className="field-label">Status</label><Select {...form.register("status")}>{Object.entries(submissionStatusLabel).map(([value,label])=><option key={value} value={value}>{label}</option>)}</Select></div></div><div><label className="field-label">Feedback</label><Textarea className="min-h-36" placeholder="Provide clear and constructive feedback..." {...form.register("feedback")}/></div><div className="flex justify-end gap-3"><Button type="button" variant="secondary" onClick={()=>setSelected(null)}>Cancel</Button><Button type="submit" loading={form.formState.isSubmitting}><Award className="h-4 w-4"/> Save review</Button></div></form></Modal></>;
}
