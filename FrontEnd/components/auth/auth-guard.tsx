"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { FullPageLoader } from "@/components/ui/loading";
import { useAuth } from "@/components/auth/auth-provider";
import type { Role } from "@/lib/types";

export function AuthGuard({ children, roles }: { children: React.ReactNode; roles?: Role[] }) {
  const { session, ready } = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!ready) return;
    if (!session) {
      router.replace(`/login?returnUrl=${encodeURIComponent(pathname)}`);
      return;
    }
    if (roles?.length && !session.user.roles.some((role) => roles.includes(role))) {
      router.replace("/dashboard");
    }
  }, [pathname, ready, roles, router, session]);

  if (!ready || !session) return <FullPageLoader label="Checking your session..." />;
  if (roles?.length && !session.user.roles.some((role) => roles.includes(role))) {
    return <FullPageLoader label="Redirecting..." />;
  }
  return children;
}
