interface Props extends React.InputHTMLAttributes<HTMLInputElement> {
  label: string;
  errors?: string[];
}

export function Field({ label, errors, id, ...input }: Props) {
  const fieldId = id ?? input.name;
  const invalid = (errors?.length ?? 0) > 0;

  return (
    <div>
      <label htmlFor={fieldId} className="font-mono text-xs uppercase tracking-widest text-muted">
        {label}
      </label>
      <input
        {...input}
        id={fieldId}
        aria-invalid={invalid}
        aria-describedby={invalid ? `${fieldId}-error` : undefined}
        className={`mt-2 w-full border bg-bg px-4 py-3 text-sm ${invalid ? "border-danger" : "border-line"}`}
      />
      {invalid && (
        <p id={`${fieldId}-error`} className="mt-2 text-xs text-danger">
          {errors!.join(" ")}
        </p>
      )}
    </div>
  );
}

export function FormError({ message, traceId }: { message: string; traceId?: string }) {
  return (
    <div className="border border-danger px-4 py-3">
      <p className="text-sm text-danger">{message}</p>
      {traceId && <p className="mt-1 font-mono text-[11px] text-muted">{traceId}</p>}
    </div>
  );
}
