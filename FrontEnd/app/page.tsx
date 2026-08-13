"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { FullPageLoader } from "@/components/ui/loading";
import { useAuth } from "@/components/auth/auth-provider";

export default function HomePage() {
  const router = useRouter();
  const { session, ready } = useAuth();

  useEffect(() => {
    if (!ready) return;
    router.replace(session ? "/dashboard" : "/login");
  }, [ready, router, session]);

  return <FullPageLoader label="Opening AssignmentHub..." />;
}
