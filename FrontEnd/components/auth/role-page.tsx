import { AuthGuard } from "@/components/auth/auth-guard";
import type { Role } from "@/lib/types";

export function RolePage({ role, children }: { role: Role | Role[]; children: React.ReactNode }) {
  return <AuthGuard roles={Array.isArray(role) ? role : [role]}>{children}</AuthGuard>;
}
