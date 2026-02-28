/**
 * Contact type selector — step 1 of the create contact flow.
 *
 * @description Renders three large cards for Individual, Trust,
 * and Organization. Mirrors the AssetTypeSelector pattern.
 */
import { User, ShieldCheck, Building2 } from "lucide-react";
import { type ContactType, CONTACT_TYPE_LABELS } from "@/types/contacts";
import { cn } from "@/lib/utils";

const TYPES: { type: ContactType; icon: typeof User; description: string }[] = [
  {
    type: "Individual",
    icon: User,
    description: "A person — spouse, child, parent, advisor, etc.",
  },
  {
    type: "Trust",
    icon: ShieldCheck,
    description: "Revocable, irrevocable, or testamentary trust.",
  },
  {
    type: "Organization",
    icon: Building2,
    description: "Charity, business, non-profit, or institution.",
  },
];

interface ContactTypeSelectorProps {
  /** Called when the user picks a contact type. */
  onSelect: (type: ContactType) => void;
}

/**
 * Renders three selectable cards for the three contact subtypes.
 */
export function ContactTypeSelector({ onSelect }: ContactTypeSelectorProps) {
  return (
    <div className="space-y-4 mt-6">
      <p className="text-sm text-muted-foreground">
        What kind of contact would you like to add?
      </p>
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        {TYPES.map(({ type, icon: Icon, description }) => (
          <button
            key={type}
            type="button"
            onClick={() => onSelect(type)}
            className={cn(
              "flex flex-col items-center gap-2 p-5 rounded-xl border border-border/50",
              "bg-card hover:border-primary/50 hover:bg-primary/5 transition-all",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            )}
          >
            <div className="w-12 h-12 rounded-lg bg-primary/10 flex items-center justify-center">
              <Icon className="w-6 h-6 text-primary" />
            </div>
            <span className="text-sm font-semibold text-foreground">
              {CONTACT_TYPE_LABELS[type]}
            </span>
            <span className="text-xs text-muted-foreground text-center leading-snug">
              {description}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
