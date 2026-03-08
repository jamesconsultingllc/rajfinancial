import { type AssetType, ASSET_TYPE_LABELS, ASSET_TYPE_ICONS } from "@/types/assets";
import { cn } from "@/lib/utils";

const ALL_ASSET_TYPES: AssetType[] = [
  "RealEstate", "Vehicle", "Investment", "Retirement",
  "BankAccount", "Insurance", "Business", "PersonalProperty",
  "Collectible", "Cryptocurrency", "IntellectualProperty", "Other",
];

interface AssetTypeSelectorProps {
  onSelect: (type: AssetType) => void;
}

export function AssetTypeSelector({ onSelect }: AssetTypeSelectorProps) {
  return (
    <div className="space-y-4 mt-6">
      <p className="text-sm text-muted-foreground">Select the type of asset you'd like to add.</p>
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
        {ALL_ASSET_TYPES.map((type) => {
          const Icon = ASSET_TYPE_ICONS[type];
          const label = ASSET_TYPE_LABELS[type];
          return (
            <button
              key={type}
              type="button"
              onClick={() => onSelect(type)}
              className={cn(
                "flex flex-col items-center gap-2 p-4 rounded-xl border border-border/50",
                "bg-card hover:border-primary/50 hover:bg-primary/5 transition-all",
                "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              )}
            >
              <div className="w-10 h-10 rounded-lg bg-primary/10 flex items-center justify-center">
                <Icon className="w-5 h-5 text-primary" />
              </div>
              <span className="text-sm font-medium text-foreground text-center leading-tight">{label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}
