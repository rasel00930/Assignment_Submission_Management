"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { InlineLoader } from "@/components/ui/loading";
import { useAuth } from "@/components/auth/auth-provider";

export default function DashboardRedirectPage() {
  const { userRole } = useAuth();
  const router = useRouter();
  useEffect(() => {
    if (userRole) router.replace(`/${userRole.toLowerCase()}`);
  }, [router, userRole]);
  return <InlineLoader label="Preparing your dashboard..." />;


  
}
