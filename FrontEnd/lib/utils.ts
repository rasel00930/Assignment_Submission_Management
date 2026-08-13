import { clsx, type ClassValue } from "clsx";
import { format, formatDistanceToNow, isPast } from "date-fns";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export function formatDate(value?: string | null) {
  if (!value) return "—";
  return format(new Date(value), "dd MMM yyyy, hh:mm a");
}

export function relativeDate(value?: string | null) {
  if (!value) return "—";
  return formatDistanceToNow(new Date(value), { addSuffix: true });
}

export function deadlineState(value: string) {
  const date = new Date(value);
  return {
    expired: isPast(date),
    label: formatDistanceToNow(date, { addSuffix: true }),
  };
}

export function getInitials(name: string) {
  return name
    .split(" ")
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join("");
}

export function toLocalInputDateTime(utc: string) {
  const date = new Date(utc);
  const offset = date.getTimezoneOffset();
  return new Date(date.getTime() - offset * 60_000).toISOString().slice(0, 16);
}

export function errorMessage(error: unknown): string {
  if (typeof error === "object" && error && "response" in error) {
    const response = (error as { response?: { data?: { message?: string; errors?: Record<string, string[]> } } }).response;
    if (response?.data?.message) return response.data.message;
    const errors = response?.data?.errors;
    if (errors) return Object.values(errors).flat().join(" ");
  }
  if (error instanceof Error) return error.message;
  return "Something went wrong. Please try again.";
}
