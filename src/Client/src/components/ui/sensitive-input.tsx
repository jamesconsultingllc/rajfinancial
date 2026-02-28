/**
 * Sensitive input with auto-dash formatting and show/hide toggle.
 *
 * @description Provides a masked input field for sensitive identifiers
 * like SSN (XXX-XX-XXXX) and EIN (XX-XXXXXXX). Features:
 *  - Auto-inserts dashes at the correct positions as user types
 *  - Strips non-numeric characters
 *  - Eye/EyeOff toggle to reveal or mask the value
 *  - When masked, shows bullet characters with dashes for position reference
 *  - Integrates with react-hook-form via standard onChange/onBlur/value props
 *
 * @example
 * <SensitiveInput
 *   format="ssn"
 *   placeholder="XXX-XX-XXXX"
 *   value={field.value}
 *   onChange={field.onChange}
 *   onBlur={field.onBlur}
 * />
 */
import { useState, useCallback, type ChangeEvent, forwardRef } from "react";
import { cn } from "@/lib/utils";
import { Eye, EyeOff } from "lucide-react";

/** Supported formatting patterns. */
export type SensitiveFormat = "ssn" | "ein";

interface SensitiveInputProps
  extends Omit<
    React.InputHTMLAttributes<HTMLInputElement>,
    "onChange" | "value" | "type"
  > {
  /** The formatting pattern to apply. */
  format: SensitiveFormat;
  /** Current value (stored as raw digits, no dashes). */
  value?: string;
  /** Called with the raw-digits value (no dashes) on each keystroke. */
  onChange?: (value: string) => void;
  /** Called on blur. */
  onBlur?: () => void;
}

/* Dash-insertion positions for each format. */
const FORMAT_CONFIG: Record<
  SensitiveFormat,
  { dashPositions: number[]; maxDigits: number; placeholder: string }
> = {
  ssn: { dashPositions: [3, 5], maxDigits: 9, placeholder: "XXX-XX-XXXX" },
  ein: { dashPositions: [2], maxDigits: 9, placeholder: "XX-XXXXXXX" },
};

/**
 * Inserts dashes into a raw-digit string at the configured positions.
 *
 * @param raw - String of digits only (e.g. "123456789")
 * @param format - The format type determining dash placement
 * @returns Formatted string (e.g. "123-45-6789" for SSN)
 */
function formatWithDashes(raw: string, format: SensitiveFormat): string {
  const { dashPositions } = FORMAT_CONFIG[format];
  let result = "";
  let digitIndex = 0;

  for (let i = 0; i < raw.length; i++) {
    if (dashPositions.includes(digitIndex)) {
      result += "-";
    }
    result += raw[i];
    digitIndex++;
  }

  return result;
}

/**
 * Strips all non-digit characters from a string.
 *
 * @param value - Any string
 * @returns Digits only
 */
function stripNonDigits(value: string): string {
  return value.replace(/\D/g, "");
}

/**
 * SensitiveInput — auto-formatting masked input for SSN/EIN values.
 *
 * @param props - Component props including format, value, onChange
 * @returns A styled input element with show/hide toggle
 */
const SensitiveInput = forwardRef<HTMLInputElement, SensitiveInputProps>(
  ({ format, value = "", onChange, onBlur, className, placeholder, ...rest }, ref) => {
    const [visible, setVisible] = useState(false);
    const config = FORMAT_CONFIG[format];

    const rawDigits = stripNonDigits(value);
    const formatted = formatWithDashes(rawDigits, format);

    const handleChange = useCallback(
      (e: ChangeEvent<HTMLInputElement>) => {
        const inputVal = e.target.value;
        // Strip everything except digits
        const digits = stripNonDigits(inputVal).slice(0, config.maxDigits);
        onChange?.(digits);
      },
      [onChange, config.maxDigits]
    );

    const toggleVisibility = useCallback(() => {
      setVisible((prev) => !prev);
    }, []);

    return (
      <div className="relative">
        <input
          ref={ref}
          className={cn(
            "flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm shadow-sm transition-colors",
            "file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground",
            "placeholder:text-muted-foreground",
            "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
            "disabled:cursor-not-allowed disabled:opacity-50",
            "pr-10", // space for the toggle button
            className
          )}
          type={visible ? "text" : "password"}
          inputMode="numeric"
          autoComplete="off"
          placeholder={placeholder || config.placeholder}
          value={formatted}
          onChange={handleChange}
          onBlur={onBlur}
          aria-label={format === "ssn" ? "Social Security Number" : "Employer Identification Number"}
          {...rest}
        />
        <button
          type="button"
          className={cn(
            "absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground",
            "hover:text-foreground transition-colors",
            "focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring focus-visible:rounded-sm"
          )}
          onClick={toggleVisibility}
          tabIndex={-1}
          aria-label={visible ? "Hide value" : "Show value"}
        >
          {visible ? (
            <EyeOff className="h-4 w-4" aria-hidden="true" />
          ) : (
            <Eye className="h-4 w-4" aria-hidden="true" />
          )}
        </button>
      </div>
    );
  }
);

SensitiveInput.displayName = "SensitiveInput";

export { SensitiveInput };
