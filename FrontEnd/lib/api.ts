import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { authStorage } from "@/lib/auth-storage";
import type { ApiResponse, TokenResponse } from "@/lib/types";

export const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7081";

export const api = axios.create({
  baseURL: API_BASE_URL,
  headers: { "Content-Type": "application/json" },
});

api.interceptors.request.use((config) => {
  const token = authStorage.get()?.accessToken;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

let refreshing: Promise<TokenResponse> | null = null;

async function refreshAccessToken(): Promise<TokenResponse> {
  const current = authStorage.get();
  if (!current?.refreshToken) throw new Error("No refresh token available");

  const response = await axios.post<ApiResponse<TokenResponse>>(
    `${API_BASE_URL}/api/auth/refresh`,
    { refreshToken: current.refreshToken },
    { headers: { "Content-Type": "application/json" } },
  );
  authStorage.set(response.data.data);
  window.dispatchEvent(new Event("auth-session-changed"));
  return response.data.data;
}

api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const request = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;
    const isAuthRequest = [
      "/api/auth/login",
      "/api/auth/refresh",
      "/api/auth/forgot-password",
      "/api/auth/reset-password",
    ].some((path) => request?.url?.includes(path));

    if (error.response?.status === 401 && request && !request._retry && !isAuthRequest) {
      request._retry = true;
      try {
        refreshing ??= refreshAccessToken().finally(() => {
          refreshing = null;
        });
        const session = await refreshing;
        request.headers.Authorization = `Bearer ${session.accessToken}`;
        return api(request);
      } catch {
        authStorage.clear();
        if (typeof window !== "undefined") window.location.href = "/login";
      }
    }
    return Promise.reject(error);
  },
);
