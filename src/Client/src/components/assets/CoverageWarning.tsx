/**
 * Beneficiary coverage warning component.
 *
 * @description Displays a contextual warning when an asset has coverage
 * issues — no beneficiaries, incomplete allocation totals, or missing
 * contingent beneficiaries. Includes a "Resolve" action to open the
 * beneficiary assignment dialog.
 */
import { useTranslation } from "react-i18next";
import { AlertTriangle, UserX, PieChart, ShieldAlert } from "lucide-react";
import { Alert, AlertTitle, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";

/* ------------------------------------------------------------------ */
/*  Types                                                              */
/* ------------------------------------------------------------------ */

export type CoverageWarningType =
  | "NoBeneficiaries"
  | "IncompleteAllocation"
  | "NoContingentBeneficiaries";

export interface CoverageWarningProps {
  warningType: CoverageWarningType;
  message?: string;
  assetId?: string;
  onResolve?: () => void;
}

/* ------------------------------------------------------------------ */
/*  Icon map                                                           */
/* ------------------------------------------------------------------ */

const iconMap: Record<CoverageWarningType, typeof AlertTriangle> = {
  NoBeneficiaries: UserX,
  IncompleteAllocation: PieChart,
  NoContingentBeneficiaries: ShieldAlert,
};

/* ------------------------------------------------------------------ */
/*  Component                                                          */
/* ------------------------------------------------------------------ */

export function CoverageWarning({
  warningType,
  message,
  onResolve,
}: CoverageWarningProps) {
  const { t } = useTranslation("assets");

  const Icon = iconMap[warningType];
  const title = t(`coverageWarning.${warningType}.title`);
  const description = message ?? t(`coverageWarning.${warningType}.description`);

  return (
    <Alert variant="warning" className="flex items-start gap-3">
      <Icon className="h-5 w-5 mt-0.5 shrink-0" />
      <div className="flex-1 min-w-0">
        <AlertTitle>{title}</AlertTitle>
        <AlertDescription className="mt-1">{description}</AlertDescription>
      </div>
      {onResolve && (
        <Button
          variant="outline"
          size="sm"
          className="shrink-0 border-amber-500/50 text-amber-900 hover:bg-amber-100 dark:border-amber-500/30 dark:text-amber-200 dark:hover:bg-amber-900/30"
          onClick={onResolve}
        >
          {t("coverageWarning.resolve")}
        </Button>
      )}
    </Alert>
  );
}
