/**
 * Contextual help tooltip for form fields.
 *
 * @description Renders a small HelpCircle icon that shows explanatory text
 * in a tooltip on hover/focus. Designed to sit inline next to a form label.
 *
 * Uses i18next to resolve the help text from translation keys.
 * Accessible: keyboard-focusable button with aria-label, 44x44 min touch target.
 *
 * @example
 * <FieldInfo textKey="help.Vehicle.vin" ns="assets" />
 */
import { useTranslation } from "react-i18next";
import { HelpCircle } from "lucide-react";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";

interface FieldInfoProps {
  /** Translation key for the help text (e.g., "help.Vehicle.vin") */
  textKey: string;
  /** i18next namespace (defaults to "assets") */
  ns?: string;
}

export function FieldInfo({ textKey, ns = "assets" }: FieldInfoProps) {
  const { t } = useTranslation(ns);
  const text = t(textKey);

  // Don't render if the key resolves to itself (missing translation)
  if (text === textKey) {
    return null;
  }

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          type="button"
          className="inline-flex items-center justify-center p-2 -m-2 min-w-[44px] min-h-[44px] rounded-full focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label="More info"
        >
          <HelpCircle
            className="h-3.5 w-3.5 text-muted-foreground"
            aria-hidden="true"
          />
        </button>
      </TooltipTrigger>
      <TooltipContent side="top" className="max-w-xs text-sm">
        {text}
      </TooltipContent>
    </Tooltip>
  );
}
