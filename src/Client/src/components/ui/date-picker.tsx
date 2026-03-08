import * as React from "react";
import { format, parse, parseISO, isValid } from "date-fns";
import { Calendar as CalendarIcon } from "lucide-react";

import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Calendar } from "@/components/ui/calendar";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Input } from "@/components/ui/input";

export interface DatePickerProps {
  /** Value can be Date, ISO string (yyyy-MM-dd), or undefined */
  value?: Date | string;
  /** Called with ISO string (yyyy-MM-dd) for form compatibility, or undefined */
  onChange?: (date: string | undefined) => void;
  placeholder?: string;
  disabled?: boolean;
  className?: string;
  id?: string;
  name?: string;
  "aria-describedby"?: string;
  "aria-invalid"?: boolean;
}

function toDate(value: Date | string | undefined): Date | undefined {
  if (!value) return undefined;
  if (value instanceof Date) return value;
  // Try ISO format (yyyy-MM-dd)
  const parsed = parseISO(value);
  return isValid(parsed) ? parsed : undefined;
}

export function DatePicker({
  value,
  onChange,
  placeholder = "MM/DD/YYYY",
  disabled,
  className,
  id,
  name,
  "aria-describedby": ariaDescribedBy,
  "aria-invalid": ariaInvalid,
}: DatePickerProps) {
  const [open, setOpen] = React.useState(false);
  const dateValue = toDate(value);
  const [inputValue, setInputValue] = React.useState(dateValue ? format(dateValue, "MM/dd/yyyy") : "");

  // Sync input value when external value changes
  React.useEffect(() => {
    const d = toDate(value);
    if (d) {
      setInputValue(format(d, "MM/dd/yyyy"));
    } else {
      setInputValue("");
    }
  }, [value]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const val = e.target.value;
    setInputValue(val);

    // Try to parse the date (user types MM/dd/yyyy)
    const parsed = parse(val, "MM/dd/yyyy", new Date());
    if (isValid(parsed) && val.length === 10) {
      onChange?.(format(parsed, "yyyy-MM-dd"));
    }
  };

  const handleInputBlur = () => {
    // On blur, validate and format
    if (inputValue) {
      const parsed = parse(inputValue, "MM/dd/yyyy", new Date());
      if (isValid(parsed)) {
        setInputValue(format(parsed, "MM/dd/yyyy"));
        onChange?.(format(parsed, "yyyy-MM-dd"));
      } else {
        // Reset to last valid value or empty
        const d = toDate(value);
        setInputValue(d ? format(d, "MM/dd/yyyy") : "");
      }
    } else {
      onChange?.(undefined);
    }
  };

  const handleCalendarSelect = (date: Date | undefined) => {
    if (date) {
      onChange?.(format(date, "yyyy-MM-dd"));
      setInputValue(format(date, "MM/dd/yyyy"));
    } else {
      onChange?.(undefined);
      setInputValue("");
    }
    setOpen(false);
  };

  return (
    <div className={cn("relative flex", className)}>
      <Input
        id={id}
        name={name}
        type="text"
        placeholder={placeholder}
        value={inputValue}
        onChange={handleInputChange}
        onBlur={handleInputBlur}
        disabled={disabled}
        aria-describedby={ariaDescribedBy}
        aria-invalid={ariaInvalid}
        className="pr-10"
      />
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            disabled={disabled}
            className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
            aria-label="Open calendar"
          >
            <CalendarIcon className="h-4 w-4 text-muted-foreground" />
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="end">
          <Calendar
            mode="single"
            selected={dateValue}
            onSelect={handleCalendarSelect}
            initialFocus
          />
        </PopoverContent>
      </Popover>
    </div>
  );
}
