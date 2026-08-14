function LoadingMark() {
  return (
    <div className="flex h-12 items-end justify-center gap-1.5" aria-hidden="true">
      <span className="loader-bar h-5 w-2.5 bg-indigo-400" />
      <span className="loader-bar h-9 w-2.5 bg-indigo-600" />
      <span className="loader-bar h-7 w-2.5 bg-sky-500" />
      <span className="loader-bar h-11 w-2.5 bg-slate-800" />
    </div>
  );
}

function LoadingContent({ label }: { label: string }) {
  return (
    <div className="w-full max-w-56 text-center" role="status" aria-live="polite">
      <LoadingMark />
      <p className="mt-4 text-sm font-bold tracking-wide text-slate-700">{label}</p>
      <div className="loader-track mt-3 h-1.5 w-full overflow-hidden bg-slate-200" aria-hidden="true">
        <span className="loader-progress block h-full w-2/5 bg-gradient-to-r from-indigo-600 to-sky-500" />
      </div>
      <span className="sr-only">Please wait</span>
    </div>
  );
}

export function FullPageLoader({ label = "Loading..." }: { label?: string }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-6">
      <LoadingContent label={label} />
    </div>
  );
}

export function PageLoader({ label = "Preparing your page..." }: { label?: string }) {
  return (
    <div className="flex min-h-[calc(100vh-9rem)] items-center justify-center px-6">
      <LoadingContent label={label} />
    </div>
  );
}

export function InlineLoader({ label = "Loading..." }: { label?: string }) {
  return (
    <div className="flex min-h-48 items-center justify-center px-6">
      <LoadingContent label={label} />
    </div>
  );
}
