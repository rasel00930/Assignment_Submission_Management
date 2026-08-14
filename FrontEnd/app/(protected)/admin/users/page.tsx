"use client";

import { useCallback, useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { KeyRound, Pencil, Plus, Power, Search, UserRound } from "lucide-react";
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
import { Pagination } from "@/components/ui/pagination";
import { Select } from "@/components/ui/select";
import { adminService } from "@/lib/services";
import type { ClassResponse, Role, UserResponse } from "@/lib/types";
import { defaultPageSize, roles } from "@/lib/constants";
import { errorMessage, formatDate, getInitials } from "@/lib/utils";

const schema = z.object({
  fullName: z.string().min(2, "Full name is required").max(100),
  email: z.string().email("Enter a valid email"),
  userName: z.string().min(3, "Username must be at least 3 characters").max(100),
  password: z.string().optional(),
  role: z.enum(["Admin", "Teacher", "Student"]),
  academicClassId: z.string().optional(),
});
type UserForm = z.infer<typeof schema>;

const passwordSchema = z.object({ newPassword: z.string().min(8, "Password must be at least 8 characters") });
type PasswordForm = z.infer<typeof passwordSchema>;

export default function UsersPage() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [classes, setClasses] = useState<ClassResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState("");
  const [roleFilter, setRoleFilter] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<UserResponse | null>(null);
  const [statusTarget, setStatusTarget] = useState<UserResponse | null>(null);
  const [passwordTarget, setPasswordTarget] = useState<UserResponse | null>(null);

  const form = useForm<UserForm>({ resolver: zodResolver(schema), defaultValues: { fullName: "", email: "", userName: "", password: "", role: "Student", academicClassId: "" } });
  const passwordForm = useForm<PasswordForm>({ resolver: zodResolver(passwordSchema), defaultValues: { newPassword: "" } });
  const selectedRole = form.watch("role");

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const result = await adminService.getUsers({
        pageNumber: page,
        pageSize: defaultPageSize,
        search: search || undefined,
        role: roleFilter || undefined,
        isActive: activeFilter === "" ? undefined : activeFilter === "true",
      });
      setUsers(result.items);
      setTotalPages(result.totalPages);
      setTotalCount(result.totalCount);
    } catch (error) { toast.error(errorMessage(error)); }
    finally { setLoading(false); }
  }, [activeFilter, page, roleFilter, search]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { adminService.getClasses(false).then(setClasses).catch((error) => toast.error(errorMessage(error))); }, []);

  const openCreate = () => {
    setEditing(null);
    form.reset({ fullName: "", email: "", userName: "", password: "", role: "Student", academicClassId: "" });
    setModalOpen(true);
  };
  const openEdit = (user: UserResponse) => {
    setEditing(user);
    form.reset({ fullName: user.fullName, email: user.email, userName: user.userName, password: "", role: user.roles[0] ?? "Student", academicClassId: user.academicClassId?.toString() ?? "" });
    setModalOpen(true);
  };

  const save = async (values: UserForm) => {
    if (!editing && (!values.password || values.password.length < 8)) {
      form.setError("password", { message: "Password must be at least 8 characters" });
      return;
    }
    if (values.role === "Student" && !values.academicClassId) {
      form.setError("academicClassId", { message: "Class is required for a student" });
      return;
    }
    const payload = { fullName: values.fullName, email: values.email, userName: values.userName, role: values.role, academicClassId: values.role === "Student" ? Number(values.academicClassId) : null };
    try {
      if (editing) await adminService.updateUser(editing.id, payload);
      else await adminService.createUser({ ...payload, password: values.password });
      toast.success(editing ? "User updated" : "User created and login credentials emailed");
      setModalOpen(false);
      await load();
    } catch (error) { toast.error(errorMessage(error)); }
  };

  const changeStatus = async () => {
    if (!statusTarget) return;
    try {
      await adminService.setUserActive(statusTarget.id, !statusTarget.isActive);
      toast.success(`User ${statusTarget.isActive ? "deactivated" : "activated"}`);
      setStatusTarget(null);
      await load();
    } catch (error) { toast.error(errorMessage(error)); }
  };

  const resetPassword = async (values: PasswordForm) => {
    if (!passwordTarget) return;
    try {
      await adminService.resetPassword(passwordTarget.id, values.newPassword);
      toast.success("Password reset successfully");
      setPasswordTarget(null);
      passwordForm.reset();
    } catch (error) { toast.error(errorMessage(error)); }
  };

  return (
    <RolePage role="Admin">
      <PageHeader title="User management" description="Create and manage administrator, teacher and student accounts." actions={<Button onClick={openCreate}><Plus className="h-4 w-4" /> Add user</Button>} />
      <Card>
        <div className="flex flex-col gap-3 border-b border-slate-100 p-4 md:flex-row">
          <div className="relative flex-1"><Search className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400" /><Input className="pl-10" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} placeholder="Search name, username or email..." /></div>
          <Select className="md:w-44" value={roleFilter} onChange={(e) => { setRoleFilter(e.target.value); setPage(1); }}><option value="">All roles</option>{roles.map((role) => <option key={role} value={role}>{role}</option>)}</Select>
          <Select className="md:w-44" value={activeFilter} onChange={(e) => { setActiveFilter(e.target.value); setPage(1); }}><option value="">All statuses</option><option value="true">Active</option><option value="false">Inactive</option></Select>
        </div>
        {loading ? <InlineLoader /> : users.length === 0 ? <EmptyState title="No users found" description="Create an account or change your search filters." /> : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px]">
              <thead className="table-head"><tr><th className="px-4 py-3">User</th><th className="px-4 py-3">Role</th><th className="px-4 py-3">Class</th><th className="px-4 py-3">Status</th><th className="px-4 py-3">Created</th><th className="px-4 py-3 text-right">Actions</th></tr></thead>
              <tbody>{users.map((user) => (
                <tr key={user.id} className="hover:bg-slate-50/70">
                  <td className="table-cell"><div className="flex items-center gap-3"><div className="flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-100 text-xs font-bold text-indigo-700">{getInitials(user.fullName)}</div><div><p className="font-bold text-slate-800">{user.fullName}</p><p className="text-xs text-slate-500">@{user.userName} · {user.email}</p></div></div></td>
                  <td className="table-cell"><Badge tone="indigo">{user.roles.join(", ")}</Badge></td>
                  <td className="table-cell">{user.academicClassName || "—"}</td>
                  <td className="table-cell"><Badge tone={user.isActive ? "emerald" : "rose"}>{user.isActive ? "Active" : "Inactive"}</Badge></td>
                  <td className="table-cell">{formatDate(user.createdAtUtc)}</td>
                  <td className="table-cell"><div className="flex justify-end gap-1"><Button variant="ghost" size="icon" title="Edit" onClick={() => openEdit(user)}><Pencil className="h-4 w-4" /></Button><Button variant="ghost" size="icon" title="Reset password" onClick={() => { setPasswordTarget(user); passwordForm.reset(); }}><KeyRound className="h-4 w-4" /></Button><Button variant="ghost" size="icon" title={user.isActive ? "Deactivate" : "Activate"} onClick={() => setStatusTarget(user)}><Power className={user.isActive ? "h-4 w-4 text-rose-600" : "h-4 w-4 text-emerald-600"} /></Button></div></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        )}
        <Pagination page={page} totalPages={totalPages} totalCount={totalCount} onPageChange={setPage} />
      </Card>

      <Modal open={modalOpen} onClose={() => setModalOpen(false)} title={editing ? "Edit user" : "Create user"} description={editing ? "Update account details and access." : "Assign access and email the temporary login credentials to the user."}>
        <form className="grid gap-4 sm:grid-cols-2" onSubmit={form.handleSubmit(save)}>
          <div className="sm:col-span-2"><label className="field-label">Full name</label><Input {...form.register("fullName")} />{form.formState.errors.fullName && <p className="field-error">{form.formState.errors.fullName.message}</p>}</div>
          <div><label className="field-label">Email</label><Input type="email" {...form.register("email")} />{form.formState.errors.email && <p className="field-error">{form.formState.errors.email.message}</p>}</div>
          <div><label className="field-label">Username</label><Input {...form.register("userName")} />{form.formState.errors.userName && <p className="field-error">{form.formState.errors.userName.message}</p>}</div>
          {!editing && <div className="sm:col-span-2"><label className="field-label">Temporary password</label><Input type="password" {...form.register("password")} />{form.formState.errors.password && <p className="field-error">{form.formState.errors.password.message}</p>}</div>}
          <div><label className="field-label">Role</label><Select {...form.register("role")}><option value="Admin">Admin</option><option value="Teacher">Teacher</option><option value="Student">Student</option></Select></div>
          <div><label className="field-label">Class / Course {selectedRole === "Student" && "*"}</label><Select disabled={selectedRole !== "Student"} {...form.register("academicClassId")}><option value="">Select class</option>{classes.map((item) => <option key={item.id} value={item.id}>{item.name}{item.section ? ` - ${item.section}` : ""} ({item.academicYear})</option>)}</Select>{form.formState.errors.academicClassId && <p className="field-error">{form.formState.errors.academicClassId.message}</p>}</div>
          <div className="sm:col-span-2 flex justify-end gap-3 pt-2"><Button type="button" variant="secondary" onClick={() => setModalOpen(false)}>Cancel</Button><Button type="submit" loading={form.formState.isSubmitting}>{editing ? "Save changes" : "Create user"}</Button></div>
        </form>
      </Modal>

      <Modal open={!!passwordTarget} onClose={() => setPasswordTarget(null)} title="Reset password" description={`Set a new temporary password for ${passwordTarget?.fullName ?? "this user"}.`} size="sm">
        <form onSubmit={passwordForm.handleSubmit(resetPassword)} className="space-y-4"><div><label className="field-label">New password</label><Input type="password" {...passwordForm.register("newPassword")} />{passwordForm.formState.errors.newPassword && <p className="field-error">{passwordForm.formState.errors.newPassword.message}</p>}</div><div className="flex justify-end gap-3"><Button type="button" variant="secondary" onClick={() => setPasswordTarget(null)}>Cancel</Button><Button type="submit" loading={passwordForm.formState.isSubmitting}><KeyRound className="h-4 w-4" /> Reset</Button></div></form>
      </Modal>

      <ConfirmDialog open={!!statusTarget} onClose={() => setStatusTarget(null)} onConfirm={changeStatus} title={`${statusTarget?.isActive ? "Deactivate" : "Activate"} user`} description={`This will ${statusTarget?.isActive ? "block" : "restore"} access for ${statusTarget?.fullName ?? "the selected user"}.`} confirmText={statusTarget?.isActive ? "Deactivate" : "Activate"} danger={statusTarget?.isActive} />
    </RolePage>
  );
}
