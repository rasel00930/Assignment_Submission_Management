"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Building2, Mail, MapPin, Phone, Save } from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { InlineLoader } from "@/components/ui/loading";
import { PageHeader } from "@/components/ui/page-header";
import { Select } from "@/components/ui/select";
import { adminService } from "@/lib/services";
import { InstitutionType } from "@/lib/types";
import { errorMessage } from "@/lib/utils";

const schema=z.object({name:z.string().min(2).max(200),code:z.string().min(2).max(30),type:z.string(),address:z.string().max(500).optional(),email:z.string().email().optional().or(z.literal("")),phone:z.string().max(30).optional(),logoUrl:z.string().url().optional().or(z.literal(""))});type FormValues=z.infer<typeof schema>;
export default function InstitutionPage(){const form=useForm<FormValues>({resolver:zodResolver(schema),defaultValues:{name:"",code:"",type:InstitutionType.College,address:"",email:"",phone:"",logoUrl:""}});useEffect(()=>{adminService.getInstitution().then(x=>form.reset({name:x.name,code:x.code,type:String(x.type),address:x.address??"",email:x.email??"",phone:x.phone??"",logoUrl:x.logoUrl??""})).catch(e=>toast.error(errorMessage(e)));},[form]);const save=async(v:FormValues)=>{try{await adminService.updateInstitution({...v,type:v.type as InstitutionType,address:v.address||null,email:v.email||null,phone:v.phone||null,logoUrl:v.logoUrl||null});toast.success("Institution configuration updated");}catch(e){toast.error(errorMessage(e));}};return <RolePage role="Admin"><PageHeader title="Institution configuration" description="Manage school, college or university identity and contact information."/><Card><CardHeader><div className="flex items-center gap-3"><div className="flex h-11 w-11 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600"><Building2 className="h-5 w-5"/></div><div><h2 className="font-extrabold">Institution profile</h2><p className="text-sm text-slate-500">This information appears across the application.</p></div></div></CardHeader><CardContent>{form.formState.isLoading?<InlineLoader/>:<form className="grid gap-5 sm:grid-cols-2" onSubmit={form.handleSubmit(save)}><div className="sm:col-span-2"><label className="field-label">Institution name</label><Input {...form.register("name")}/>{form.formState.errors.name&&<p className="field-error">{form.formState.errors.name.message}</p>}</div><div><label className="field-label">Code</label><Input {...form.register("code")}/>{form.formState.errors.code&&<p className="field-error">{form.formState.errors.code.message}</p>}</div><div><label className="field-label">Type</label><Select {...form.register("type")}>{Object.values(InstitutionType).map(type=><option key={type} value={type}>{type}</option>)}</Select></div><div className="sm:col-span-2"><label className="field-label"><MapPin className="mr-1 inline h-4 w-4"/>Address</label><Input {...form.register("address")}/></div><div><label className="field-label"><Mail className="mr-1 inline h-4 w-4"/>Email</label><Input type="email" {...form.register("email")}/>{form.formState.errors.email&&<p className="field-error">{form.formState.errors.email.message}</p>}</div><div><label className="field-label"><Phone className="mr-1 inline h-4 w-4"/>Phone</label><Input {...form.register("phone")}/></div><div className="sm:col-span-2"><label className="field-label">Logo URL</label><Input placeholder="https://..." {...form.register("logoUrl")}/>{form.formState.errors.logoUrl&&<p className="field-error">{form.formState.errors.logoUrl.message}</p>}</div><div className="sm:col-span-2 flex justify-end"><Button type="submit" loading={form.formState.isSubmitting}><Save className="h-4 w-4"/> Save configuration</Button></div></form>}</CardContent></Card></RolePage>}
