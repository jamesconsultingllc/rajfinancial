import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@/components/ui/collapsible";
import { ChevronDown } from "lucide-react";
import type { PropertyInputs } from "@/lib/real-estate/types";

interface Props {
  inputs: PropertyInputs;
  onChange: (inputs: PropertyInputs) => void;
}

function Field({ label, value, onChange, prefix, suffix, step, min, max }: {
  label: string; value: number; onChange: (v: number) => void;
  prefix?: string; suffix?: string; step?: number; min?: number; max?: number;
}) {
  return (
    <div>
      <Label className="text-xs text-muted-foreground mb-1">{label}</Label>
      <div className="flex items-center">
        {prefix && <span className="text-muted-foreground text-sm mr-1">{prefix}</span>}
        <Input
          type="number"
          value={value || ''}
          onChange={(e) => onChange(parseFloat(e.target.value) || 0)}
          step={step || 1}
          min={min}
          max={max}
          className="h-9 text-sm"
        />
        {suffix && <span className="text-muted-foreground text-sm ml-1">{suffix}</span>}
      </div>
    </div>
  );
}

export default function PropertyForm({ inputs, onChange }: Props) {
  const update = (key: keyof PropertyInputs, value: number) => {
    onChange({ ...inputs, [key]: value });
  };

  return (
    <Card className="border-border/50">
      <CardHeader className="pb-4">
        <CardTitle className="text-lg">Property Details</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
          <Field label="Purchase Price" value={inputs.purchasePrice} onChange={(v) => update('purchasePrice', v)} prefix="$" step={5000} min={0} />
          <Field label="Down Payment" value={inputs.downPaymentPct} onChange={(v) => update('downPaymentPct', v)} suffix="%" step={0.5} min={0} max={100} />
          <Field label="Interest Rate" value={inputs.interestRate} onChange={(v) => update('interestRate', v)} suffix="%" step={0.125} min={0} max={25} />
          <Field label="Loan Term" value={inputs.loanTermYears} onChange={(v) => update('loanTermYears', v)} suffix="yr" min={1} max={40} />
          <Field label="Monthly Rent (if rental)" value={inputs.monthlyRent} onChange={(v) => update('monthlyRent', v)} prefix="$" step={50} min={0} />
          <Field label="HOA Monthly" value={inputs.hoaMonthly} onChange={(v) => update('hoaMonthly', v)} prefix="$" step={25} min={0} />
        </div>

        <Collapsible>
          <CollapsibleTrigger className="flex items-center gap-1 text-sm text-primary hover:text-primary/80 transition-colors">
            <ChevronDown className="w-4 h-4" />
            Advanced Settings
          </CollapsibleTrigger>
          <CollapsibleContent>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mt-3 pt-3 border-t border-border/50">
              <Field label="Vacancy Rate" value={inputs.vacancyRate} onChange={(v) => update('vacancyRate', v)} suffix="%" step={1} min={0} max={100} />
              <Field label="Property Mgmt" value={inputs.propertyMgmtPct} onChange={(v) => update('propertyMgmtPct', v)} suffix="%" step={1} min={0} max={50} />
              <Field label="Maintenance" value={inputs.maintenancePct} onChange={(v) => update('maintenancePct', v)} suffix="%" step={0.25} min={0} max={50} />
              <Field label="CapEx Reserve" value={inputs.capexPct} onChange={(v) => update('capexPct', v)} suffix="%" step={0.25} min={0} max={50} />
              <Field label="Appreciation Rate" value={inputs.appreciationRate} onChange={(v) => update('appreciationRate', v)} suffix="%" step={0.5} />
              <Field label="Your Rent (if renting now)" value={inputs.monthlyPersonalRent} onChange={(v) => update('monthlyPersonalRent', v)} prefix="$" step={50} min={0} />
            </div>
          </CollapsibleContent>
        </Collapsible>

        <Collapsible>
          <CollapsibleTrigger className="flex items-center gap-1 text-sm text-primary hover:text-primary/80 transition-colors">
            <ChevronDown className="w-4 h-4" />
            Flip / House-Hack Settings
          </CollapsibleTrigger>
          <CollapsibleContent>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mt-3 pt-3 border-t border-border/50">
              <Field label="Renovation Budget" value={inputs.renovationBudget} onChange={(v) => update('renovationBudget', v)} prefix="$" step={5000} min={0} />
              <Field label="ARV Multiplier" value={inputs.arvMultiplier} onChange={(v) => update('arvMultiplier', v)} suffix="x" step={0.05} />
              <Field label="Rental Units (house-hack)" value={inputs.numRentalUnits} onChange={(v) => update('numRentalUnits', v)} step={1} min={0} max={100} />
              <Field label="Rent per Unit" value={inputs.ownerUnitRent} onChange={(v) => update('ownerUnitRent', v)} prefix="$" step={50} min={0} />
            </div>
          </CollapsibleContent>
        </Collapsible>

        <Collapsible>
          <CollapsibleTrigger className="flex items-center gap-1 text-sm text-primary hover:text-primary/80 transition-colors">
            <ChevronDown className="w-4 h-4" />
            Refinance Settings
          </CollapsibleTrigger>
          <CollapsibleContent>
            <div className="grid grid-cols-2 md:grid-cols-3 gap-3 mt-3 pt-3 border-t border-border/50">
              <Field label="Current Loan Balance" value={inputs.currentLoanBalance} onChange={(v) => update('currentLoanBalance', v)} prefix="$" step={5000} min={0} />
              <Field label="Current Rate" value={inputs.currentRate} onChange={(v) => update('currentRate', v)} suffix="%" step={0.125} min={0} max={25} />
              <Field label="Months Remaining" value={inputs.currentRemainingMonths} onChange={(v) => update('currentRemainingMonths', v)} step={12} min={1} />
              <Field label="Refi Closing Cost" value={inputs.refiClosingCostPct} onChange={(v) => update('refiClosingCostPct', v)} suffix="%" step={0.25} min={0} />
            </div>
          </CollapsibleContent>
        </Collapsible>
      </CardContent>
    </Card>
  );
}
