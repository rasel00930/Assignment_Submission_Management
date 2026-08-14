"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  CheckCircle2,
  Lightbulb,
  Loader2,
  Plus,
  Settings2,
  ShieldCheck,
  SlidersHorizontal,
} from "lucide-react";
import { toast } from "sonner";
import { RolePage } from "@/components/auth/role-page";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { InlineLoader } from "@/components/ui/loading";
import { PageHeader } from "@/components/ui/page-header";
import { Switch } from "@/components/ui/switch";
import { adminService } from "@/lib/services";
import type { SettingCatalogResponse } from "@/lib/types";
import { errorMessage } from "@/lib/utils";

export default function SettingsPage() {
  const [catalog, setCatalog] = useState<SettingCatalogResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [workingKeys, setWorkingKeys] = useState<Set<string>>(new Set());

  const load = useCallback(async () => {
    setLoading(true);
    try {
      setCatalog(await adminService.getSettingCatalog());
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const configured = useMemo(() => catalog.filter((item) => item.isConfigured), [catalog]);
  const suggestions = useMemo(() => catalog.filter((item) => !item.isConfigured), [catalog]);

  const setWorking = (key: string, working: boolean) => {
    setWorkingKeys((current) => {
      const next = new Set(current);
      if (working) next.add(key);
      else next.delete(key);
      return next;
    });
  };

  const savePolicy = async (item: SettingCatalogResponse, enabled: boolean, creating = false) => {
    setWorking(item.key, true);
    try {
      await adminService.upsertSetting({
        key: item.key,
        value: enabled ? "true" : "false",
        description: item.description,
      });
      setCatalog((current) =>
        current.map((policy) =>
          policy.key === item.key
            ? { ...policy, isConfigured: true, isEnabled: enabled }
            : policy,
        ),
      );
      toast.success(creating ? `${item.title} added` : `${item.title} turned ${enabled ? "on" : "off"}`);
    } catch (error) {
      toast.error(errorMessage(error));
    } finally {
      setWorking(item.key, false);
    }
  };

  return (
    <RolePage role="Admin">
      <PageHeader
        title="Application settings"
        description="Choose which assignment options teachers can use. Teachers still decide which options to allow on each assignment."
      />

      <div className="mb-5 flex flex-col gap-3 rounded-2xl border border-indigo-100 bg-gradient-to-r from-indigo-50 via-white to-sky-50 p-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-white text-indigo-600 shadow-sm ring-1 ring-indigo-100">
            <SlidersHorizontal className="h-5 w-5" />
          </div>
          <div>
            <p className="text-sm font-extrabold text-slate-900">Assignment policy controls</p>
            <p className="text-xs text-slate-500">Turn on a policy to make its option available when teachers create or edit assignments.</p>
          </div>
        </div>
        <div className="flex gap-2 text-xs font-bold">
          <span className="rounded-full bg-white px-3 py-1.5 text-slate-600 shadow-sm ring-1 ring-slate-200">{configured.length} added</span>
          <span className="rounded-full bg-amber-100 px-3 py-1.5 text-amber-700">{suggestions.length} more available</span>
        </div>
      </div>

      {loading ? (
        <InlineLoader />
      ) : catalog.length === 0 ? (
        <Card><EmptyState title="No policy options found" description="There are no assignment policies available to manage right now." /></Card>
      ) : (
        <>
          <div className="grid gap-5 lg:grid-cols-2">
            {configured.map((item) => {
              const working = workingKeys.has(item.key);
              return (
                <Card key={item.key} className="overflow-hidden border-slate-200/80">
                  <div className={item.isEnabled ? "h-1.5 bg-gradient-to-r from-indigo-500 to-sky-500" : "h-1.5 bg-slate-200"} />
                  <div className="p-5">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex gap-3">
                        <div className={item.isEnabled ? "flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-indigo-100 text-indigo-600" : "flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-slate-100 text-slate-500"}>
                          <Settings2 className="h-5 w-5" />
                        </div>
                        <div><h2 className="font-black text-slate-900">{item.title}</h2><p className="mt-1 font-mono text-[11px] font-semibold text-slate-400">{item.key}</p></div>
                      </div>
                      <div className="flex items-center gap-2">
                        {working && <Loader2 className="h-4 w-4 animate-spin text-indigo-600" />}
                        <Switch checked={item.isEnabled} disabled={working} aria-label={`Toggle ${item.title}`} onCheckedChange={(enabled) => void savePolicy(item, enabled)} />
                      </div>
                    </div>
                    <p className="mt-4 text-sm leading-6 text-slate-600">{item.description}</p>
                    <div className="mt-4 rounded-xl border border-indigo-100 bg-indigo-50/70 p-3">
                      <p className="flex items-center gap-2 text-xs font-extrabold uppercase tracking-wide text-indigo-700"><ShieldCheck className="h-4 w-4" /> What happens when this is on</p>
                      <p className="mt-2 text-sm leading-6 text-indigo-900">{item.alignment}</p>
                    </div>
                    <div className="mt-4 flex justify-end text-xs font-bold">
                      <span className={item.isEnabled ? "inline-flex items-center gap-1.5 text-emerald-700" : "text-slate-500"}>{item.isEnabled && <CheckCircle2 className="h-4 w-4" />}{item.isEnabled ? "On" : "Off"}</span>
                    </div>
                  </div>
                </Card>
              );
            })}
          </div>

          {suggestions.length > 0 && (
            <Card className="mt-6 overflow-hidden border-amber-200 bg-amber-50/40">
              <div className="border-b border-amber-200 bg-amber-50 px-5 py-4">
                <div className="flex items-start gap-3">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white text-amber-600 shadow-sm"><Lightbulb className="h-5 w-5" /></div>
                  <div><h2 className="font-black text-amber-950">More policy options</h2><p className="mt-1 text-sm leading-6 text-amber-800">Add any option you want to manage. After adding it, turn it on to make the matching option available to teachers.</p></div>
                </div>
              </div>
              <div className="grid gap-3 p-5 md:grid-cols-2 xl:grid-cols-3">
                {suggestions.map((item) => {
                  const working = workingKeys.has(item.key);
                  return (
                    <div key={item.key} className="flex flex-col rounded-2xl border border-amber-100 bg-white p-4 shadow-sm">
                      <p className="font-mono text-[10px] font-bold uppercase tracking-wide text-amber-700">{item.key}</p>
                      <h3 className="mt-3 text-sm font-extrabold text-slate-900">{item.title}</h3>
                      <p className="mt-2 flex-1 text-xs leading-5 text-slate-600">{item.description}</p>
                      <Button className="mt-4 w-full" variant="secondary" loading={working} onClick={() => void savePolicy(item, item.defaultValue, true)}><Plus className="h-4 w-4" /> Add policy</Button>
                    </div>
                  );
                })}
              </div>
            </Card>
          )}
        </>
      )}
    </RolePage>
  );
}
