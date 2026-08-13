import type { AuthSession } from "@/lib/types";

const key = "assignment_management_session";

export const authStorage = {
  get(): AuthSession | null {
    if (typeof window === "undefined") return null;
    const value = localStorage.getItem(key);
    if (!value) return null;
    try {
      return JSON.parse(value) as AuthSession;
    } catch {
      localStorage.removeItem(key);
      return null;
    }
  },
  set(session: AuthSession) {
    if (typeof window !== "undefined") localStorage.setItem(key, JSON.stringify(session));
  },
  clear() {
    if (typeof window !== "undefined") localStorage.removeItem(key);
  },
};
