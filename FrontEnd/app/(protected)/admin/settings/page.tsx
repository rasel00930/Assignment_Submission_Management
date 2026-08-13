"use client";

import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Pencil, Plus, Settings2 } from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { InlineLoader } from "@/components/ui/loading";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { Textarea } from "@/components/ui/textarea";
import { adminService } from "@/lib/services";
import type { SettingResponse } from "@/lib/types";
import { errorMessage } from "@/lib/utils";

const schema=z.object({key:z.string().min(1).max(100),value:z.string().min(1),description:z.string().max(300).optional()});type FormValues=z.infer<typeof schema>;
export default function SettingsPage(){const[items,setItems]=useState<SettingResponse[]>([]);const[loading,setLoading]=useState(true);const[open,setOpen]=useState(false);const form=useForm<FormValues>({resolver:zodResolver(schema),defaultValues:{key:"",value:"",description:""}});const load=async()=>{setLoading(true);try{setItems(await adminService.getSettings());}catch(e){toast.error(errorMessage(e));}finally{setLoading(false);}};useEffect(()=>{void load();},[]);const edit=(item?:SettingResponse)=>{form.reset(item?{key:item.key,value:item.value,description:item.description??""}:{key:"",value:"",description:""});setOpen(true);};const save=async(v:FormValues)=>{try{await adminService.upsertSetting({...v,description:v.description||null});toast.success("Setting saved");setOpen(false);await load();}catch(e){toast.error(errorMessage(e));}};return <RolePage role="Admin"><PageHeader title="Application settings" description="Store configurable values such as late submission policies." actions={<Button onClick={()=>edit()}><Plus className="h-4 w-4"/> Add setting</Button>}/>{loading?<InlineLoader/>:items.length===0?<Card><EmptyState title="No settings configured"/></Card>:<div className="grid gap-4 md:grid-cols-2">{items.map(item=><Card key={item.id} className="p-5"><div className="flex items-start justify-between gap-3"><div className="flex h-10 w-10 items-center justify-center rounded-xl bg-slate-100 text-slate-600"><Settings2 className="h-5 w-5"/></div><Button size="icon" variant="ghost" onClick={()=>edit(item)}><Pencil className="h-4 w-4"/></Button></div><h3 className="mt-4 font-extrabold text-slate-900">{item.key}</h3><div className="mt-2 rounded-xl bg-slate-50 px-3 py-2 font-mono text-sm text-indigo-700">{item.value}</div>{item.description&&<p className="mt-3 text-sm leading-6 text-slate-500">{item.description}</p>}</Card>)}</div>}<Modal open={open} onClose={()=>setOpen(false)} title="Save application setting"><form className="space-y-4" onSubmit={form.handleSubmit(save)}><div><label className="field-label">Key</label><Input placeholder="AllowLateSubmission" {...form.register("key")}/>{form.formState.errors.key&&<p className="field-error">{form.formState.errors.key.message}</p>}</div><div><label className="field-label">Value</label><Input placeholder="true" {...form.register("value")}/>{form.formState.errors.value&&<p className="field-error">{form.formState.errors.value.message}</p>}</div><div><label className="field-label">Description</label><Textarea {...form.register("description")}/></div><div className="flex justify-end gap-3"><Button type="button" variant="secondary" onClick={()=>setOpen(false)}>Cancel</Button><Button type="submit" loading={form.formState.isSubmitting}>Save</Button></div></form></Modal></RolePage>}
