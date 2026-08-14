"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Eye, EyeOff, GraduationCap, LockKeyhole, UserRound } from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { FullPageLoader } from "@/components/ui/loading";
import { useAuth } from "@/components/auth/auth-provider";
import { errorMessage } from "@/lib/utils";

const schema = z.object({
  userName: z.string().min(1, "Username is required"),
  password: z.string().min(1, "Password is required"),
});
type FormValues = z.infer<typeof schema>;

export default function LoginPage() {
  const { login, session, ready } = useAuth();
  const router = useRouter();
  const [showPassword, setShowPassword] = useState(false);
  const [redirecting, setRedirecting] = useState(false);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { userName: "", password: "" },
  });

  useEffect(() => {
    if (ready && session && !redirecting) {
      setRedirecting(true);
      const role = session.user.roles[0];
      router.replace(role ? `/${role.toLowerCase()}` : "/dashboard");
    }
  }, [ready, redirecting, router, session]);

  const submit = async (values: FormValues) => {
    try {
      const result = await login(values.userName, values.password);
      setRedirecting(true);
      toast.success(`Welcome back, ${result.user.fullName}`);
      const returnUrl = typeof window !== "undefined" ? new URLSearchParams(window.location.search).get("returnUrl") : null;
      const safeReturnUrl = returnUrl?.startsWith("/") && !returnUrl.startsWith("//") ? returnUrl : null;
      const role = result.user.roles[0];
      router.replace(safeReturnUrl || (role ? `/${role.toLowerCase()}` : "/dashboard"));
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  if (redirecting || (ready && session)) {
    return <FullPageLoader label="Login successful. Opening your dashboard..." />;
  }

  return (
    <main className="grid min-h-screen lg:grid-cols-[1.05fr_.95fr]">
      <section className="relative hidden overflow-hidden bg-slate-950 lg:flex lg:flex-col lg:justify-between lg:p-12">
        <div className="absolute -left-20 top-20 h-72 w-72 rounded-full bg-indigo-600/30 blur-3xl" />
        <div className="absolute -bottom-24 right-0 h-96 w-96 rounded-full bg-sky-500/20 blur-3xl" />
        <div className="relative z-10 flex items-center gap-3 text-white">
          <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-indigo-500 to-sky-500">
            <GraduationCap className="h-7 w-7" />
          </div>
          <div>
            <p className="text-xl font-extrabold">AssignmentHub</p>
            <p className="text-sm text-slate-400">Academic workflow, simplified.</p>
          </div>
        </div>
        <div className="relative z-10 max-w-xl">
          <span className="inline-flex rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm font-semibold text-indigo-200">School & College Management</span>
          <h1 className="mt-6 text-5xl font-black leading-tight tracking-tight text-white">Create, submit and review assignments with confidence.</h1>
          <p className="mt-5 max-w-lg text-lg leading-8 text-slate-300">A secure role-based workspace for administrators, teachers and students.</p>
          <div className="mt-10 grid grid-cols-3 gap-3">
            {["Role-based access", "Fast submissions", "Clear feedback"].map((item) => (
              <div key={item} className="rounded-2xl border border-white/10 bg-white/5 p-4 text-sm font-semibold text-slate-200 backdrop-blur">{item}</div>
            ))}
          </div>
        </div>
        <p className="relative z-10 text-sm text-slate-500">Built with Next.js, React, TypeScript and ASP.NET Core.</p>
      </section>

      <section className="flex items-center justify-center p-5 sm:p-10">
        <div className="w-full max-w-md animate-slide-up">
          <div className="mb-8 flex items-center gap-3 lg:hidden">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-indigo-600 text-white"><GraduationCap className="h-6 w-6" /></div>
            <div><p className="font-extrabold">AssignmentHub</p><p className="text-xs text-slate-500">Academic workflow, simplified.</p></div>
          </div>
          <div className="surface p-6 sm:p-8">
            <div>
              <p className="text-sm font-bold uppercase tracking-widest text-indigo-600">Secure access</p>
              <h2 className="mt-2 text-3xl font-black tracking-tight text-slate-900">Welcome back</h2>
              <p className="mt-2 text-sm leading-6 text-slate-500">Sign in with the account created by your administrator.</p>
            </div>

            <form className="mt-8 space-y-5" onSubmit={handleSubmit(submit)}>
              <div>
                <label className="field-label">Username</label>
                <div className="relative">
                  <UserRound className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400" />
                  <Input className="pl-10" placeholder="Enter your username" autoComplete="username" {...register("userName")} />
                </div>
                {errors.userName && <p className="field-error">{errors.userName.message}</p>}
              </div>
              <div>
                <div className="mb-1.5 flex items-center justify-between">
                  <label className="block text-sm font-semibold text-slate-700">Password</label>
                  <Link href="/forgot-password" className="text-xs font-bold text-indigo-600 hover:text-indigo-700">
                    Forgot password?
                  </Link>
                </div>
                <div className="relative">
                  <LockKeyhole className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400" />
                  <Input className="pl-10 pr-11" type={showPassword ? "text" : "password"} placeholder="Enter your password" autoComplete="current-password" {...register("password")} />
                  <button type="button" onClick={() => setShowPassword((value) => !value)} className="absolute right-3 top-3 rounded p-1 text-slate-400 hover:text-slate-700">
                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </button>
                </div>
                {errors.password && <p className="field-error">{errors.password.message}</p>}
              </div>
              <Button type="submit" size="lg" className="w-full" loading={isSubmitting}>Sign in</Button>
            </form>

            <div className="mt-6 rounded-xl bg-slate-50 p-4 text-xs leading-5 text-slate-500">
              <span className="font-bold text-slate-700">Initial admin:</span> username <code className="font-semibold">admin</code>, password <code className="font-semibold">Admin@123</code>. Change it after first login.
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}
