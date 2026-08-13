"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { authStorage } from "@/lib/auth-storage";
import { authService } from "@/lib/services";
import type { AuthSession, Role } from "@/lib/types";

interface AuthContextValue {
  session: AuthSession | null;
  ready: boolean;
  userRole?: Role;
  login: (userName: string, password: string) => Promise<AuthSession>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [session, setSession] = useState<AuthSession | null>(null);
  const [ready, setReady] = useState(false);

  const sync = useCallback(() => setSession(authStorage.get()), []);

  useEffect(() => {
    sync();
    setReady(true);
    window.addEventListener("auth-session-changed", sync);
    return () => window.removeEventListener("auth-session-changed", sync);
  }, [sync]);

  const login = useCallback(async (userName: string, password: string) => {
    const result = await authService.login({ userName, password });
    authStorage.set(result);
    setSession(result);
    return result;
  }, []);

  const logout = useCallback(async () => {
    const current = authStorage.get();
    try {
      if (current?.refreshToken) await authService.logout(current.refreshToken);
    } finally {
      authStorage.clear();
      setSession(null);
      router.replace("/login");
    }
  }, [router]);

  const refreshUser = useCallback(async () => {
    const current = authStorage.get();
    if (!current) return;
    const user = await authService.me();
    const updated = { ...current, user };
    authStorage.set(updated);
    setSession(updated);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      ready,
      userRole: session?.user.roles[0],
      login,
      logout,
      refreshUser,
    }),
    [login, logout, ready, refreshUser, session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth must be used inside AuthProvider");
  return value;
}
