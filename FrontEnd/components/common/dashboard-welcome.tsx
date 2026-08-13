import { Sparkles } from "lucide-react";
import { useAuth } from "@/components/auth/auth-provider";

export function DashboardWelcome({ message }: { message: string }) {
  const { session } = useAuth();
  return (
    <div className="relative mb-6 overflow-hidden rounded-3xl bg-gradient-to-r from-slate-950 via-indigo-950 to-indigo-800 p-6 text-white shadow-xl sm:p-8">
      <div className="absolute -right-12 -top-12 h-52 w-52 rounded-full bg-sky-400/20 blur-3xl" />
      <div className="absolute -bottom-16 left-1/3 h-40 w-40 rounded-full bg-indigo-400/20 blur-3xl" />
      <div className="relative">
        <div className="inline-flex items-center gap-2 rounded-full border border-white/10 bg-white/10 px-3 py-1.5 text-xs font-semibold text-indigo-100">
          <Sparkles className="h-3.5 w-3.5" /> Welcome to your workspace
        </div>
        <h1 className="mt-4 text-3xl font-black tracking-tight sm:text-4xl">Hello, {session?.user.fullName}</h1>
        <p className="mt-2 max-w-2xl text-sm leading-6 text-indigo-100 sm:text-base">{message}</p>
      </div>
    </div>
  );
}
