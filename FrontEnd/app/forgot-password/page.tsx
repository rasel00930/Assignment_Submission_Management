"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, CheckCircle2, KeyRound, LockKeyhole, Mail, ShieldCheck } from "lucide-react";
import { z } from "zod";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { authService } from "@/lib/services";
import { errorMessage } from "@/lib/utils";

const emailSchema = z.object({
  email: z.string().email("Enter a valid email address"),
});

const resetSchema = z.object({
  email: z.string().email("Enter a valid email address"),
  verificationCode: z.string().regex(/^\d{6}$/, "Enter the 6-digit verification code"),
  newPassword: z.string().min(8, "Password must be at least 8 characters").max(100),
  confirmPassword: z.string(),
}).refine((values) => values.newPassword === values.confirmPassword, {
  message: "Passwords do not match",
  path: ["confirmPassword"],
});

type EmailForm = z.infer<typeof emailSchema>;
type ResetForm = z.infer<typeof resetSchema>;
type Step = "email" | "code" | "success";

export default function ForgotPasswordPage() {
  const [step, setStep] = useState<Step>("email");
  const emailForm = useForm<EmailForm>({
    resolver: zodResolver(emailSchema),
    defaultValues: { email: "" },
  });
  const resetForm = useForm<ResetForm>({
    resolver: zodResolver(resetSchema),
    defaultValues: {
      email: "",
      verificationCode: "",
      newPassword: "",
      confirmPassword: "",
    },
  });

  useEffect(() => {
    const email = new URLSearchParams(window.location.search).get("email");
    if (!email) return;
    emailForm.setValue("email", email);
    resetForm.setValue("email", email);
  }, [emailForm, resetForm]);

  const requestCode = async (values: EmailForm) => {
    try {
      await authService.forgotPassword(values.email);
      resetForm.setValue("email", values.email);
      setStep("code");
      toast.success("If the account exists, a verification code was sent");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const resetPassword = async (values: ResetForm) => {
    try {
      await authService.resetPassword({
        email: values.email,
        verificationCode: values.verificationCode,
        newPassword: values.newPassword,
      });
      setStep("success");
      toast.success("Password reset successfully");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  const resendCode = async () => {
    const email = resetForm.getValues("email");
    try {
      await authService.forgotPassword(email);
      toast.success("If resend is available, a verification code has been sent.");
    } catch (error) {
      toast.error(errorMessage(error));
    }
  };

  return (
    <main className="flex min-h-screen items-center justify-center p-5 sm:p-10">
      <div className="w-full max-w-md animate-slide-up">
        <Link href="/login" className="mb-5 inline-flex items-center gap-2 text-sm font-bold text-slate-600 hover:text-indigo-600">
          <ArrowLeft className="h-4 w-4" /> Back to sign in
        </Link>

        <div className="surface p-6 sm:p-8">
          {step === "success" ? (
            <div className="text-center">
              <div className="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-600">
                <CheckCircle2 className="h-7 w-7" />
              </div>
              <h1 className="mt-5 text-2xl font-black text-slate-900">Password reset complete</h1>
              <p className="mt-2 text-sm leading-6 text-slate-500">Your new password is ready. You can now sign in to your account.</p>
              <Link href="/login" className="mt-6 block">
                <Button type="button" size="lg" className="w-full">Go to sign in</Button>
              </Link>
            </div>
          ) : (
            <>
              <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-indigo-100 text-indigo-600">
                {step === "email" ? <Mail className="h-6 w-6" /> : <ShieldCheck className="h-6 w-6" />}
              </div>
              <h1 className="mt-5 text-3xl font-black tracking-tight text-slate-900">
                {step === "email" ? "Forgot your password?" : "Verify your email"}
              </h1>
              <p className="mt-2 text-sm leading-6 text-slate-500">
                {step === "email"
                  ? "Enter your account email and we will send a 6-digit verification code."
                  : "Enter the code from your email and choose a new password."}
              </p>

              {step === "email" ? (
                <form className="mt-7 space-y-5" onSubmit={emailForm.handleSubmit(requestCode)}>
                  <div>
                    <label className="field-label">Email address</label>
                    <div className="relative">
                      <Mail className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400" />
                      <Input className="pl-10" type="email" autoComplete="email" placeholder="you@example.com" {...emailForm.register("email")} />
                    </div>
                    {emailForm.formState.errors.email && <p className="field-error">{emailForm.formState.errors.email.message}</p>}
                  </div>
                  <Button type="submit" size="lg" className="w-full" loading={emailForm.formState.isSubmitting}>
                    Send verification code
                  </Button>
                </form>
              ) : (
                <form className="mt-7 space-y-4" onSubmit={resetForm.handleSubmit(resetPassword)}>
                  <div>
                    <label className="field-label">Email address</label>
                    <Input type="email" autoComplete="email" {...resetForm.register("email")} />
                    {resetForm.formState.errors.email && <p className="field-error">{resetForm.formState.errors.email.message}</p>}
                  </div>
                  <div>
                    <label className="field-label">6-digit verification code</label>
                    <div className="relative">
                      <KeyRound className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400" />
                      <Input className="pl-10 tracking-[0.35em]" inputMode="numeric" autoComplete="one-time-code" maxLength={6} placeholder="000000" {...resetForm.register("verificationCode")} />
                    </div>
                    {resetForm.formState.errors.verificationCode && <p className="field-error">{resetForm.formState.errors.verificationCode.message}</p>}
                  </div>
                  <div>
                    <label className="field-label">New password</label>
                    <div className="relative">
                      <LockKeyhole className="absolute left-3.5 top-3.5 h-4 w-4 text-slate-400" />
                      <Input className="pl-10" type="password" autoComplete="new-password" {...resetForm.register("newPassword")} />
                    </div>
                    {resetForm.formState.errors.newPassword && <p className="field-error">{resetForm.formState.errors.newPassword.message}</p>}
                  </div>
                  <div>
                    <label className="field-label">Confirm new password</label>
                    <Input type="password" autoComplete="new-password" {...resetForm.register("confirmPassword")} />
                    {resetForm.formState.errors.confirmPassword && <p className="field-error">{resetForm.formState.errors.confirmPassword.message}</p>}
                  </div>
                  <Button type="submit" size="lg" className="w-full" loading={resetForm.formState.isSubmitting}>
                    Reset password
                  </Button>
                  <div className="flex items-center justify-between text-xs">
                    <button type="button" onClick={() => setStep("email")} className="font-bold text-slate-500 hover:text-slate-800">
                      Change email
                    </button>
                    <button type="button" onClick={resendCode} className="font-bold text-indigo-600 hover:text-indigo-700">
                      Resend code
                    </button>
                  </div>
                </form>
              )}
            </>
          )}
        </div>
      </div>
    </main>
  );
}
