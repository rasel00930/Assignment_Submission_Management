import type { ButtonHTMLAttributes } from "react";
import { cn } from "@/lib/utils";

interface SwitchProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, "onChange"> {
  checked: boolean;
  onCheckedChange?: (checked: boolean) => void;
}

export function Switch({
  checked,
  onCheckedChange,
  className,
  disabled,
  ...props
}: SwitchProps) {
  return (
    <button
      type="button"
      role="switch"
      aria-checked={checked}
      disabled={disabled}
      onClick={() => onCheckedChange?.(!checked)}
      className={cn(
        "relative inline-flex h-7 w-12 shrink-0 items-center rounded-full border-2 border-transparent transition-all duration-200",
        "focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-indigo-100",
        "disabled:cursor-not-allowed disabled:opacity-50",
        checked ? "bg-indigo-600 shadow-sm shadow-indigo-200" : "bg-slate-300",
        className,
      )}
      {...props}
    >
      <span
        aria-hidden="true"
        className={cn(
          "pointer-events-none block h-5 w-5 rounded-full bg-white shadow-md transition-transform duration-200",
          checked ? "translate-x-5" : "translate-x-0.5",
        )}
      />
    </button>
  );
}
