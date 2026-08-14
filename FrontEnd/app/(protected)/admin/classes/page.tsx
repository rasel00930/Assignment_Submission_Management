"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Pencil, Plus, School, Trash2, Users } from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { EmptyState } from "@/components/ui/empty-state";
import { Input } from "@/components/ui/input";
import { InlineLoader } from "@/components/ui/loading";
import { Modal } from "@/components/ui/modal";
import { PageHeader } from "@/components/ui/page-header";
import { SwitchField } from "@/components/ui/switch";
import { adminService } from "@/lib/services";
import type { ClassResponse } from "@/lib/types";
import { errorMessage } from "@/lib/utils";

const schema = z.object({ name: z.string().min(1, "Name is required").max(100), section: z.string().max(50).optional(), academicYear: z.string().min(1, "Academic year is required").max(30), isActive: z.boolean().default(true) });
type FormValues = z.infer<typeof schema>;

export default function ClassesPage() {
  const [items, setItems] = useState<ClassResponse[]>([]); const [loading, setLoading] = useState(true); const [includeInactive, setIncludeInactive] = useState(false); const [open, setOpen] = useState(false); const [editing, setEditing] = useState<ClassResponse | null>(null); const [target, setTarget] = useState<ClassResponse | null>(null);
  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { name: "", section: "", academicYear: new Date().getFullYear().toString(), isActive: true } });
  const load = useCallback(async () => { setLoading(true); try { setItems(await adminService.getClasses(includeInactive)); } catch (e) { toast.error(errorMessage(e)); } finally { setLoading(false); } }, [includeInactive]);
  useEffect(() => { void load(); }, [load]);
  const edit = (item?: ClassResponse) => { setEditing(item ?? null); form.reset(item ? { name: item.name, section: item.section ?? "", academicYear: item.academicYear, isActive: item.isActive } : { name: "", section: "", academicYear: new Date().getFullYear().toString(), isActive: true }); setOpen(true); };
  const save = async (values: FormValues) => { try { if (editing) await adminService.updateClass(editing.id, values); else await adminService.createClass({ name: values.name, section: values.section || null, academicYear: values.academicYear }); toast.success(editing ? "Class updated" : "Class created"); setOpen(false); await load(); } catch (e) { toast.error(errorMessage(e)); } };
  const deactivate = async () => { if (!target) return; try { await adminService.deactivateClass(target.id); toast.success("Class deactivated"); setTarget(null); await load(); } catch (e) { toast.error(errorMessage(e)); } };
  return <RolePage role="Admin"><PageHeader title="Classes & courses" description="Create academic groups, sections and academic years." actions={<Button onClick={() => edit()}><Plus className="h-4 w-4" /> Add class</Button>} />
    <SwitchField className="mb-4 max-w-sm" checked={includeInactive} onCheckedChange={setIncludeInactive} label="Show inactive classes" description="Include deactivated classes and courses in this list." />
    {loading ? <InlineLoader /> : items.length === 0 ? <Card><EmptyState title="No classes yet" description="Create the first class or course for your institution." /></Card> : <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{items.map((item) => <Card key={item.id} className="p-5"><div className="flex items-start justify-between"><div className="flex h-11 w-11 items-center justify-center rounded-xl bg-sky-50 text-sky-600"><School className="h-5 w-5" /></div><Badge tone={item.isActive ? "emerald" : "rose"}>{item.isActive ? "Active" : "Inactive"}</Badge></div><h3 className="mt-4 text-lg font-extrabold text-slate-900">{item.name}{item.section ? ` · ${item.section}` : ""}</h3><p className="mt-1 text-sm text-slate-500">Academic year: {item.academicYear}</p><div className="mt-4 flex items-center gap-2 text-sm font-semibold text-slate-600"><Users className="h-4 w-4" /> {item.studentCount} students</div><div className="mt-5 flex justify-end gap-2 border-t border-slate-100 pt-4"><Button variant="secondary" size="sm" onClick={() => edit(item)}><Pencil className="h-4 w-4" /> Edit</Button>{item.isActive && <Button variant="ghost" size="sm" onClick={() => setTarget(item)}><Trash2 className="h-4 w-4 text-rose-600" /> Deactivate</Button>}</div></Card>)}</div>}
    <Modal open={open} onClose={() => setOpen(false)} title={editing ? "Edit class / course" : "Create class / course"}><form className="space-y-4" onSubmit={form.handleSubmit(save)}><div><label className="field-label">Name</label><Input placeholder="e.g. Class 10 or BSc CSE" {...form.register("name")} />{form.formState.errors.name && <p className="field-error">{form.formState.errors.name.message}</p>}</div><div className="grid gap-4 sm:grid-cols-2"><div><label className="field-label">Section</label><Input placeholder="e.g. A" {...form.register("section")} /></div><div><label className="field-label">Academic year</label><Input placeholder="2026" {...form.register("academicYear")} />{form.formState.errors.academicYear && <p className="field-error">{form.formState.errors.academicYear.message}</p>}</div></div>{editing && <SwitchField checked={form.watch("isActive")} onCheckedChange={(checked)=>form.setValue("isActive",checked,{shouldDirty:true})} label="Active class" description="Active classes remain available for students, mappings and assignments." />}<div className="flex justify-end gap-3"><Button type="button" variant="secondary" onClick={() => setOpen(false)}>Cancel</Button><Button type="submit" loading={form.formState.isSubmitting}>Save</Button></div></form></Modal>
    <ConfirmDialog open={!!target} onClose={() => setTarget(null)} onConfirm={deactivate} title="Deactivate class" description={`Students and teacher mappings for ${target?.name ?? "this class"} may be affected.`} confirmText="Deactivate" />
  </RolePage>;
}
