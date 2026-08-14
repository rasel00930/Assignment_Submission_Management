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
    const response = (error as { response?: { status?: number; data?: unknown } }).response;
    const payload = response?.data;

    if (typeof payload === "string" && payload.trim()) return payload.trim();

    if (isRecord(payload)) {
      const validationErrors = flattenValidationErrors(payload.errors ?? payload.Errors);
      const message = firstNonEmptyString(
        payload.message,
        payload.Message,
        payload.detail,
        payload.Detail,
        payload.title,
        payload.Title,
      );

      if (validationErrors.length > 0) {
        const genericMessage = !message || /^(validation failed|one or more validation errors occurred)\.?$/i.test(message);
        return genericMessage ? validationErrors.join(" ") : `${message} ${validationErrors.join(" ")}`;
      }

      if (message) return message;
    }

    const statusMessages: Record<number, string> = {
      400: "The supplied information is invalid. Please check it and try again.",
      401: "Your username or password is incorrect.",
      403: "You do not have permission to perform this action.",
      404: "The requested information could not be found.",
      409: "This information already exists or conflicts with an existing record.",
      422: "Some supplied values could not be processed.",
      429: "Too many requests. Please wait a moment and try again.",
      500: "The server encountered an unexpected error. Please try again.",
      502: "The server is temporarily unavailable. Please try again.",
      503: "The service is temporarily unavailable. Please try again.",
    };
    if (response?.status && statusMessages[response.status]) return statusMessages[response.status];
  }

  if (error instanceof Error) {
    if (/network error|failed to fetch|load failed/i.test(error.message)) {
      return "Cannot connect to the server. Make sure the backend API is running.";
    }
    if (!/^request failed with status code \d+$/i.test(error.message)) return error.message;
  }
  return "Something went wrong. Please try again.";
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function firstNonEmptyString(...values: unknown[]): string | undefined {
  return values.find((value): value is string => typeof value === "string" && value.trim().length > 0)?.trim();
}

function flattenValidationErrors(value: unknown): string[] {
  if (!value) return [];
  if (typeof value === "string") return value.trim() ? [value.trim()] : [];
  if (Array.isArray(value)) return value.flatMap(flattenValidationErrors);
  if (isRecord(value)) return Object.values(value).flatMap(flattenValidationErrors);
  return [];
}
