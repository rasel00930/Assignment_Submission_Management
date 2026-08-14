"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  BookOpenCheck,
  Building2,
  ChevronDown,
  ClipboardCheck,
  FileText,
  GraduationCap,
  LayoutDashboard,
  LogOut,
  Menu,
  School,
  Settings,
  ShieldCheck,
  UserCog,
  Users,
  X,
} from "lucide-react";
import { useAuth } from "@/components/auth/auth-provider";
import { cn, getInitials } from "@/lib/utils";
import type { Role } from "@/lib/types";

interface NavItem {
  label: string;
  href: string;
  icon: React.ComponentType<{ className?: string }>;
}

const menus: Record<Role, NavItem[]> = {
  Admin: [
    { label: "Overview", href: "/admin", icon: LayoutDashboard },
    { label: "Users", href: "/admin/users", icon: Users },
    { label: "Classes & Courses", href: "/admin/classes", icon: School },
    { label: "Subjects", href: "/admin/subjects", icon: BookOpenCheck },
    { label: "Teacher Mapping", href: "/admin/teacher-assignments", icon: UserCog },
    { label: "Assignments", href: "/admin/assignments", icon: FileText },
    { label: "Submissions", href: "/admin/submissions", icon: ClipboardCheck },
    { label: "Institution", href: "/admin/institution", icon: Building2 },
    { label: "Settings", href: "/admin/settings", icon: Settings },
  ],
  Teacher: [
    { label: "Overview", href: "/teacher", icon: LayoutDashboard },
    { label: "Assignments", href: "/teacher/assignments", icon: FileText },
    { label: "Submissions", href: "/teacher/submissions", icon: ClipboardCheck },
  ],
  Student: [
    { label: "Overview", href: "/student", icon: LayoutDashboard },
    { label: "Assignments", href: "/student/assignments", icon: FileText },
    { label: "My Submissions", href: "/student/submissions", icon: ClipboardCheck },
  ],
};

export function DashboardShell({ children }: { children: React.ReactNode }) {
  const { session, userRole, logout } = useAuth();
  const pathname = usePathname();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);

  if (!session || !userRole) return null;
  const navItems = menus[userRole];

  const sidebar = (
    <div className="flex h-full flex-col bg-slate-950 text-white">
      <div className="flex h-20 items-center gap-3 border-b border-white/10 px-5">
        <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-gradient-to-br from-indigo-500 to-sky-500 shadow-lg shadow-indigo-950/40">
          <GraduationCap className="h-6 w-6" />
        </div>
        <div>
          <p className="font-extrabold tracking-tight">AssignmentHub</p>
          <p className="text-xs text-slate-400">Learn · Submit · Review</p>
        </div>
      </div>

      <div className="px-4 py-5">
        <div className="rounded-2xl border border-white/10 bg-white/5 p-3">
          <p className="truncate text-sm font-semibold">{session.user.institutionName}</p>
          <div className="mt-2 inline-flex items-center gap-1.5 rounded-lg bg-indigo-500/20 px-2 py-1 text-xs font-semibold text-indigo-200">
            <ShieldCheck className="h-3.5 w-3.5" /> {userRole}
          </div>
        </div>
      </div>

      <nav className="flex-1 space-y-1 overflow-y-auto px-3 pb-5">
        {navItems.map((item) => {
          const active = pathname === item.href || (item.href !== `/${userRole.toLowerCase()}` && pathname.startsWith(`${item.href}/`));
          const Icon = item.icon;
          return (
            <Link
              key={item.href}
              href={item.href}
              onClick={() => setMobileOpen(false)}
              className={cn(
                "flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold transition",
                active ? "bg-indigo-600 text-white shadow-lg shadow-indigo-950/30" : "text-slate-300 hover:bg-white/10 hover:text-white",
              )}
            >
              <Icon className="h-5 w-5" />
              {item.label}
            </Link>
          );
        })}
      </nav>

      <div className="border-t border-white/10 p-3">
        <button onClick={() => void logout()} className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold text-slate-300 transition hover:bg-rose-500/15 hover:text-rose-300">
          <LogOut className="h-5 w-5" /> Sign out
        </button>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen">
      <aside className="fixed inset-y-0 left-0 z-40 hidden w-72 lg:block">{sidebar}</aside>

      {mobileOpen && (
        <div className="fixed inset-0 z-50 lg:hidden">
          <div className="absolute inset-0 bg-slate-950/60 backdrop-blur-sm" onClick={() => setMobileOpen(false)} />
          <aside className="relative h-full w-72 animate-fade-in shadow-2xl">{sidebar}</aside>
          <button className="absolute right-4 top-4 rounded-xl bg-white/10 p-2 text-white" onClick={() => setMobileOpen(false)}>
            <X className="h-5 w-5" />
          </button>
        </div>
      )}

      <div className="lg:pl-72">
        <header className="sticky top-0 z-30 flex h-20 items-center justify-between border-b border-slate-200/80 bg-white/85 px-4 backdrop-blur-xl sm:px-6 lg:px-8">
          <div className="flex items-center gap-3">
            <button className="rounded-xl border border-slate-200 p-2.5 text-slate-600 lg:hidden" onClick={() => setMobileOpen(true)}>
              <Menu className="h-5 w-5" />
            </button>
            <div className="hidden sm:block">
              <p className="text-xs font-semibold uppercase tracking-widest text-indigo-600">Academic workspace</p>
              <p className="mt-0.5 text-sm font-medium text-slate-500">Manage learning activities in one place</p>
            </div>
          </div>

          <div className="relative">
            <button onClick={() => setProfileOpen((value) => !value)} className="flex items-center gap-3 rounded-2xl border border-slate-200 bg-white p-2 pr-3 shadow-sm transition hover:border-indigo-200 hover:shadow-md">
              <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-gradient-to-br from-indigo-500 to-sky-500 text-sm font-bold text-white">
                {getInitials(session.user.fullName)}
              </div>
              <div className="hidden text-left sm:block">
                <p className="max-w-40 truncate text-sm font-bold text-slate-800">{session.user.fullName}</p>
                <p className="text-xs text-slate-500">{session.user.userName}</p>
              </div>
              <ChevronDown className="h-4 w-4 text-slate-400" />
            </button>

            {profileOpen && (
              <div className="absolute right-0 mt-2 w-56 rounded-2xl border border-slate-200 bg-white p-2 shadow-xl animate-slide-up">
                <Link href="/profile" onClick={() => setProfileOpen(false)} className="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50">
                  <UserCog className="h-4 w-4" /> Profile & password
                </Link>
                <button onClick={() => void logout()} className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-semibold text-rose-600 hover:bg-rose-50">
                  <LogOut className="h-4 w-4" /> Sign out
                </button>
              </div>
            )}
          </div>
        </header>

        <main className="min-h-[calc(100vh-5rem)] p-3 sm:p-5 lg:p-7">
          <div className="page-frame">
            <div aria-hidden="true" className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-indigo-500 via-sky-500 to-emerald-400" />
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}
