import { Inbox } from "lucide-react";

export function EmptyState({ title = "Nothing found", description = "There are no records to display yet." }: { title?: string; description?: string }) {
  return (
    <div className="flex min-h-56 flex-col items-center justify-center px-6 text-center">
      <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-slate-100 text-slate-400">
        <Inbox className="h-7 w-7" />
      </div>
      <h3 className="mt-4 font-bold text-slate-800">{title}</h3>
      <p className="mt-1 max-w-sm text-sm text-slate-500">{description}</p>
    </div>
  );
}
