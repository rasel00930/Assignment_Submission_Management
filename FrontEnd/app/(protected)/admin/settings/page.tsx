"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  AlignLeft,
  CheckCircle2,
  Loader2,
  Pencil,
  Plus,
  Settings2,
  SlidersHorizontal,
  Tag,
} from "lucide-react";
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
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import { adminService } from "@/lib/services";
import type { SettingResponse } from "@/lib/types";
import { errorMessage } from "@/lib/utils";

const schema = z.object({
  key: z.string().min(1, "Key is required").max(100),
  description: z.string().max(300).optional(),
});

type FormValues = z.infer<typeof schema>;

const isBooleanValue = (value: string) =>
  value.trim().toLowerCase() === "true" || value.trim().toLowerCase() === "false";

const isEnabled = (value: string) => value.trim().toLowerCase() === "true";

const titleFromKey = (key: string) =>
  key
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/[_-]+/g, " ")
    .replace(/^./, (character) => character.toUpperCase());

export default function SettingsPage() {
  const [items, setItems] = useState<SettingResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [open, setOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<SettingResponse | null>(null);
  const [savingKeys, setSavingKeys] = useState<Set<string>>(new Set());

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { key: "", description: "" },
  });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setItems(await adminService.getSettings());
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const enabledCount = useMemo(
    () => items.filter((item) => isBooleanValue(item.value) && isEnabled(item.value)).length,
    [items],
  );

  const edit = (item?: SettingResponse) => {
    setEditingItem(item ?? null);
    form.reset(
      item
        ? { key: item.key, description: item.description ?? "" }
        : { key: "", description: "" },
    );
    setOpen(true);
  };

  const closeModal = () => {
    setOpen(false);
    setEditingItem(null);
    form.reset({ key: "", description: "" });
  };

  const save = async (values: FormValues) => {
    try {
      const saved = await adminService.upsertSetting({
        ...values,
        value: editingItem?.value ?? "false",
        description: values.description || null,
      });
      setItems((current) => {
        const exists = current.some((item) => item.id === saved.id);
        return exists
          ? current.map((item) => (item.id === saved.id ? saved : item))
          : [...current, saved];
      });
      toast.success("Setting saved successfully");
      closeModal();
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const toggleSetting = async (item: SettingResponse, checked: boolean) => {
    setSavingKeys((current) => new Set(current).add(item.key));
    try {
      const saved = await adminService.upsertSetting({
        key: item.key,
        value: checked ? "true" : "false",
        description: item.description ?? null,
      });
      setItems((current) =>
        current.map((setting) => (setting.id === item.id ? saved : setting)),
      );
      toast.success(titleFromKey(item.key) + (checked ? " enabled" : " disabled"));
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setSavingKeys((current) => {
        const next = new Set(current);
        next.delete(item.key);
        return next;
      });
    }
  };

  return (
    <RolePage role="Admin">
      <PageHeader
        title="Application settings"
        description="Control platform policies and institution-wide application behaviour."
        actions={
          <Button onClick={() => edit()}>
            <Plus className="h-4 w-4" /> Add setting
          </Button>
        }
      />

      {!loading && items.length > 0 && (
        <div className="mb-5 flex flex-col gap-3 rounded-2xl border border-indigo-100 bg-gradient-to-r from-indigo-50 via-white to-sky-50 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white text-indigo-600 shadow-sm ring-1 ring-indigo-100">
              <SlidersHorizontal className="h-5 w-5" />
            </div>
            <div>
              <p className="text-sm font-extrabold text-slate-900">Policy controls</p>
              <p className="text-xs text-slate-500">Changes are applied across the platform immediately.</p>
            </div>
          </div>
          <div className="flex items-center gap-2 text-xs font-bold">
            <span className="rounded-full bg-white px-3 py-1.5 text-slate-600 shadow-sm ring-1 ring-slate-200">
              {items.length} total
            </span>
            <span className="rounded-full bg-emerald-100 px-3 py-1.5 text-emerald-700">
              {enabledCount} enabled
            </span>
          </div>
        </div>
      )}

      {loading ? (
        <InlineLoader />
      ) : items.length === 0 ? (
        <Card>
          <EmptyState
            title="No settings configured"
            description="Create your first policy setting to control application behaviour."
          />
        </Card>
      ) : (
        <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {items.map((item) => {
            const booleanSetting = isBooleanValue(item.value);
            const checked = isEnabled(item.value);
            const saving = savingKeys.has(item.key);

            return (
              <Card
                key={item.id}
                className="group flex min-h-64 flex-col overflow-hidden border-slate-200/80 transition duration-200 hover:-translate-y-0.5 hover:border-indigo-200 hover:shadow-lg hover:shadow-indigo-100/60"
              >
                <div
                  className={
                    "h-1.5 w-full " +
                    (booleanSetting && checked
                      ? "bg-gradient-to-r from-indigo-500 via-violet-500 to-sky-500"
                      : "bg-gradient-to-r from-slate-200 via-slate-100 to-slate-200")
                  }
                />
                <div className="flex flex-1 flex-col p-5">
                  <div className="flex items-start justify-between gap-4">
                    <div
                      className={
                        "flex h-11 w-11 items-center justify-center rounded-2xl " +
                        (checked
                          ? "bg-indigo-100 text-indigo-600 ring-4 ring-indigo-50"
                          : "bg-slate-100 text-slate-500 ring-4 ring-slate-50")
                      }
                    >
                      <Settings2 className="h-5 w-5" />
                    </div>
                    <div className="flex items-center gap-2">
                      <span
                        className={
                          "rounded-full px-2.5 py-1 text-[10px] font-extrabold uppercase tracking-wider " +
                          (booleanSetting
                            ? checked
                              ? "bg-emerald-100 text-emerald-700"
                              : "bg-slate-100 text-slate-500"
                            : "bg-sky-100 text-sky-700")
                        }
                      >
                        {booleanSetting ? (checked ? "Active" : "Inactive") : "Custom"}
                      </span>
                      <Button
                        size="icon"
                        variant="ghost"
                        title={"Edit " + titleFromKey(item.key)}
                        aria-label={"Edit " + titleFromKey(item.key)}
                        onClick={() => edit(item)}
                        className="opacity-70 group-hover:opacity-100"
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>

                  <div className="mt-5">
                    <h3 className="text-lg font-black tracking-tight text-slate-900">
                      {titleFromKey(item.key)}
                    </h3>
                    <p className="mt-1 font-mono text-[11px] font-semibold uppercase tracking-wide text-slate-400">
                      {item.key}
                    </p>
                  </div>

                  <p className="mt-4 flex-1 text-sm leading-6 text-slate-500">
                    {item.description || "No description has been provided for this setting."}
                  </p>
                </div>

                <div className="flex min-h-20 items-center justify-between gap-4 border-t border-slate-100 bg-slate-50/80 px-5 py-4">
                  <div>
                    <div className="flex items-center gap-2">
                      {booleanSetting && checked && (
                        <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                      )}
                      <p className="text-sm font-extrabold text-slate-800">
                        {booleanSetting ? (checked ? "Enabled" : "Disabled") : "Current value"}
                      </p>
                      {saving && <Loader2 className="h-3.5 w-3.5 animate-spin text-indigo-500" />}
                    </div>
                    <p className="mt-0.5 text-xs text-slate-500">
                      {booleanSetting ? (checked ? "Value: true" : "Value: false") : item.value}
                    </p>
                  </div>

                  {booleanSetting ? (
                    <Switch
                      checked={checked}
                      disabled={saving}
                      aria-label={"Toggle " + titleFromKey(item.key)}
                      onCheckedChange={(nextChecked) => void toggleSetting(item, nextChecked)}
                    />
                  ) : (
                    <span className="max-w-32 truncate rounded-lg bg-white px-3 py-1.5 font-mono text-xs font-bold text-indigo-700 ring-1 ring-slate-200">
                      {item.value}
                    </span>
                  )}
                </div>
              </Card>
            );
          })}
        </div>
      )}

      <Modal
        open={open}
        onClose={closeModal}
        title={editingItem ? "Edit application setting" : "Create application setting"}
        description={
          editingItem
            ? "Update the setting identity and supporting description."
            : "Create a new policy control. It will start in the disabled state."
        }
      >
        <form className="space-y-4" onSubmit={form.handleSubmit(save)}>
          <div className="rounded-2xl border border-slate-200 bg-slate-50/70 p-4">
            <label className="mb-2 flex items-center gap-2 text-sm font-extrabold text-slate-800">
              <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-indigo-100 text-indigo-600">
                <Tag className="h-3.5 w-3.5" />
              </span>
              Setting key
            </label>
            <Input
              className="bg-white font-mono"
              placeholder="AllowLateSubmission"
              {...form.register("key")}
            />
            <p className="mt-2 text-xs leading-5 text-slate-500">
              Use a clear unique name. Example: AllowLateSubmission.
            </p>
            {form.formState.errors.key && (
              <p className="field-error">{form.formState.errors.key.message}</p>
            )}
          </div>

          <div className="rounded-2xl border border-slate-200 bg-slate-50/70 p-4">
            <label className="mb-2 flex items-center gap-2 text-sm font-extrabold text-slate-800">
              <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-sky-100 text-sky-600">
                <AlignLeft className="h-3.5 w-3.5" />
              </span>
              Description
            </label>
            <Textarea
              className="min-h-28 bg-white"
              placeholder="Explain how this setting affects the application."
              {...form.register("description")}
            />
            <div className="mt-2 flex items-center justify-between text-xs text-slate-500">
              <span>Keep it short and helpful for administrators.</span>
              <span>Max 300</span>
            </div>
            {form.formState.errors.description && (
              <p className="field-error">{form.formState.errors.description.message}</p>
            )}
          </div>

          <div className="flex items-center gap-3 rounded-xl border border-indigo-100 bg-indigo-50 px-4 py-3">
            <SlidersHorizontal className="h-4 w-4 shrink-0 text-indigo-600" />
            <p className="text-xs font-semibold leading-5 text-indigo-700">
              Value is controlled from the card toggle. New settings start as false.
            </p>
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="secondary" onClick={closeModal}>
              Cancel
            </Button>
            <Button type="submit" loading={form.formState.isSubmitting}>
              {editingItem ? "Save changes" : "Create setting"}
            </Button>
          </div>
        </form>
      </Modal>
    </RolePage>
  );
}
