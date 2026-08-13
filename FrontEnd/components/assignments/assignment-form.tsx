"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, Save, Send } from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { InlineLoader } from "@/components/ui/loading";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { adminService, assignmentService } from "@/lib/services";
import type { TeacherAssignmentResponse } from "@/lib/types";
import { errorMessage, toLocalInputDateTime } from "@/lib/utils";

const schema=z.object({title:z.string().min(3,"Title must be at least 3 characters").max(200),description:z.string().min(3,"Description is required"),deadlineUtc:z.string().min(1,"Deadline is required"),maximumMarks:z.coerce.number().min(0.01).max(10000),teacherClassSubjectId:z.string().min(1,"Select a class and subject mapping"),allowResubmission:z.boolean().default(true),publishNow:z.boolean().default(false)}).refine(v=>new Date(v.deadlineUtc)>new Date(),{path:["deadlineUtc"],message:"Deadline must be in the future"});
type FormValues=z.infer<typeof schema>;

export function AssignmentForm({ id }: { id?: number }) {
  const router=useRouter(); const[loading,setLoading]=useState(!!id); const[mappings,setMappings]=useState<TeacherAssignmentResponse[]>([]);
  const form=useForm<FormValues>({resolver:zodResolver(schema),defaultValues:{title:"",description:"",deadlineUtc:"",maximumMarks:100,teacherClassSubjectId:"",allowResubmission:true,publishNow:false}});
  useEffect(()=>{const load=async()=>{try{const maps=await adminService.getTeacherAssignments({pageNumber:1,pageSize:100,isActive:true});setMappings(maps.items);if(id){const item=await assignmentService.getById(id);form.reset({title:item.title,description:item.description,deadlineUtc:toLocalInputDateTime(item.deadlineUtc),maximumMarks:item.maximumMarks,teacherClassSubjectId:String(item.teacherClassSubjectId),allowResubmission:item.allowResubmission,publishNow:false});}}catch(e){toast.error(errorMessage(e));}finally{setLoading(false);}};void load();},[form,id]);
  const save=async(v:FormValues)=>{const payload={title:v.title,description:v.description,deadlineUtc:new Date(v.deadlineUtc).toISOString(),maximumMarks:v.maximumMarks,teacherClassSubjectId:Number(v.teacherClassSubjectId),allowResubmission:v.allowResubmission,...(!id?{publishNow:v.publishNow}:{})};try{const result=id?await assignmentService.update(id,payload):await assignmentService.create(payload);toast.success(id?"Assignment updated":"Assignment created");router.push(`/teacher/assignments/${result.id}`);}catch(e){toast.error(errorMessage(e));}};
  if(loading)return <InlineLoader label="Loading assignment..."/>;
  return <><PageHeader title={id?"Edit assignment":"Create assignment"} description="Define instructions, deadline, marks and publishing status." actions={<Button variant="secondary" onClick={()=>router.back()}><ArrowLeft className="h-4 w-4"/> Back</Button>}/><Card><CardHeader><h2 className="font-extrabold">Assignment information</h2><p className="mt-1 text-sm text-slate-500">Students will see this information after the assignment is published.</p></CardHeader><CardContent><form className="grid gap-5" onSubmit={form.handleSubmit(save)}><div><label className="field-label">Title</label><Input placeholder="e.g. Data Structures Lab Report" {...form.register("title")}/>{form.formState.errors.title&&<p className="field-error">{form.formState.errors.title.message}</p>}</div><div><label className="field-label">Description / instructions</label><Textarea className="min-h-44" placeholder="Explain the task, expected format and evaluation criteria..." {...form.register("description")}/>{form.formState.errors.description&&<p className="field-error">{form.formState.errors.description.message}</p>}</div><div className="grid gap-5 md:grid-cols-3"><div><label className="field-label">Class & subject</label><Select {...form.register("teacherClassSubjectId")}><option value="">Select mapping</option>{mappings.map(x=><option key={x.id} value={x.id}>{x.className} · {x.subjectName}</option>)}</Select>{form.formState.errors.teacherClassSubjectId&&<p className="field-error">{form.formState.errors.teacherClassSubjectId.message}</p>}</div><div><label className="field-label">Deadline</label><Input type="datetime-local" {...form.register("deadlineUtc")}/>{form.formState.errors.deadlineUtc&&<p className="field-error">{form.formState.errors.deadlineUtc.message}</p>}</div><div><label className="field-label">Maximum marks</label><Input type="number" step="0.01" {...form.register("maximumMarks")}/>{form.formState.errors.maximumMarks&&<p className="field-error">{form.formState.errors.maximumMarks.message}</p>}</div></div><div className="flex flex-col gap-3 rounded-2xl bg-slate-50 p-4 sm:flex-row sm:items-center sm:justify-between"><label className="flex items-center gap-3 text-sm font-semibold text-slate-700"><input type="checkbox" className="h-4 w-4" {...form.register("allowResubmission")}/> Allow students to update their answer before deadline</label>{!id&&<label className="flex items-center gap-3 text-sm font-semibold text-slate-700"><input type="checkbox" className="h-4 w-4" {...form.register("publishNow")}/> Publish immediately</label>}</div><div className="flex justify-end gap-3"><Button type="button" variant="secondary" onClick={()=>router.back()}>Cancel</Button><Button type="submit" loading={form.formState.isSubmitting}>{form.watch("publishNow")&&!id?<Send className="h-4 w-4"/>:<Save className="h-4 w-4"/>}{id?"Save changes":"Create assignment"}</Button></div></form></CardContent></Card></>;
}
