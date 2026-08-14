"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { BookOpenCheck, Pencil, Plus, Trash2 } from "lucide-react";
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
import type { SubjectResponse } from "@/lib/types";
import { errorMessage } from "@/lib/utils";

const schema = z.object({ name: z.string().min(1, "Name is required").max(120), code: z.string().min(1, "Code is required").max(30), isActive: z.boolean().default(true) });
type FormValues = z.infer<typeof schema>;

export default function SubjectsPage() {
  const [items, setItems] = useState<SubjectResponse[]>([]); const [loading, setLoading] = useState(true); const [includeInactive, setIncludeInactive] = useState(false); const [open, setOpen] = useState(false); const [editing, setEditing] = useState<SubjectResponse | null>(null); const [target, setTarget] = useState<SubjectResponse | null>(null);
  const form = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { name: "", code: "", isActive: true } });
  const load = useCallback(async () => { setLoading(true); try { setItems(await adminService.getSubjects(includeInactive)); } catch (e) { toast.error(errorMessage(e)); } finally { setLoading(false); } }, [includeInactive]);
  useEffect(() => { void load(); }, [load]);
  const edit = (item?: SubjectResponse) => { setEditing(item ?? null); form.reset(item ? { name: item.name, code: item.code, isActive: item.isActive } : { name: "", code: "", isActive: true }); setOpen(true); };
  const save = async (values: FormValues) => { try { if (editing) await adminService.updateSubject(editing.id, values); else await adminService.createSubject({ name: values.name, code: values.code }); toast.success(editing ? "Subject updated" : "Subject created"); setOpen(false); await load(); } catch (e) { toast.error(errorMessage(e)); } };
  const deactivate = async () => { if (!target) return; try { await adminService.deactivateSubject(target.id); toast.success("Subject deactivated"); setTarget(null); await load(); } catch (e) { toast.error(errorMessage(e)); } };
  return <RolePage role="Admin"><PageHeader title="Subjects" description="Maintain subject names and unique subject codes." actions={<Button onClick={() => edit()}><Plus className="h-4 w-4" /> Add subject</Button>} /><SwitchField className="mb-4 max-w-sm" checked={includeInactive} onCheckedChange={setIncludeInactive} label="Show inactive subjects" description="Include deactivated subjects in this list." />
  {loading ? <InlineLoader /> : items.length === 0 ? <Card><EmptyState title="No subjects yet" /></Card> : <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">{items.map((item) => <Card key={item.id} className="p-5"><div className="flex items-start justify-between"><div className="flex h-11 w-11 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600"><BookOpenCheck className="h-5 w-5" /></div><Badge tone={item.isActive ? "emerald" : "rose"}>{item.isActive ? "Active" : "Inactive"}</Badge></div><h3 className="mt-4 font-extrabold text-slate-900">{item.name}</h3><p className="mt-1 text-sm font-semibold text-slate-500">Code: {item.code}</p><div className="mt-5 flex justify-end gap-2 border-t border-slate-100 pt-4"><Button variant="secondary" size="sm" onClick={() => edit(item)}><Pencil className="h-4 w-4" /> Edit</Button>{item.isActive && <Button variant="ghost" size="icon" onClick={() => setTarget(item)}><Trash2 className="h-4 w-4 text-rose-600" /></Button>}</div></Card>)}</div>}
  <Modal open={open} onClose={() => setOpen(false)} title={editing ? "Edit subject" : "Create subject"}><form className="space-y-4" onSubmit={form.handleSubmit(save)}><div><label className="field-label">Subject name</label><Input {...form.register("name")} />{form.formState.errors.name && <p className="field-error">{form.formState.errors.name.message}</p>}</div><div><label className="field-label">Subject code</label><Input placeholder="e.g. MAT101" {...form.register("code")} />{form.formState.errors.code && <p className="field-error">{form.formState.errors.code.message}</p>}</div>{editing && <SwitchField checked={form.watch("isActive")} onCheckedChange={(checked)=>form.setValue("isActive",checked,{shouldDirty:true})} label="Active subject" description="Active subjects can be used in teacher mappings and assignments." />}<div className="flex justify-end gap-3"><Button type="button" variant="secondary" onClick={() => setOpen(false)}>Cancel</Button><Button type="submit" loading={form.formState.isSubmitting}>Save</Button></div></form></Modal>
  <ConfirmDialog open={!!target} onClose={() => setTarget(null)} onConfirm={deactivate} title="Deactivate subject" description={`Teacher mappings for ${target?.name ?? "this subject"} may be affected.`} confirmText="Deactivate" /></RolePage>;
}
