import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { CreditCard, AlertTriangle } from "lucide-react";
import { toast } from "sonner";
import { mockSubscription } from "@/data/mock-settings";

function formatBytes(bytes: number): string {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + " " + sizes[i];
}

function formatLimit(val: number): string {
  return val === -1 ? "∞" : val.toString();
}

function usageColor(used: number, limit: number): string {
  if (limit === -1) return "bg-[hsl(var(--success))]";
  const pct = (used / limit) * 100;
  if (pct > 90) return "bg-destructive";
  if (pct > 75) return "bg-[hsl(40,90%,50%)]";
  return "bg-[hsl(var(--success))]";
}

function usagePct(used: number, limit: number): number {
  if (limit === -1) return Math.min((used / 100) * 5, 100); // show some progress for unlimited
  return Math.min((used / limit) * 100, 100);
}

export function SubscriptionTab() {
  const { t } = useTranslation("settings");
  const [sub] = useState(mockSubscription);
  const [cancelling, setCancelling] = useState(sub.isCancelling);

  const usageMeters = [
    { label: "Assets", used: sub.usage.assetCount, limit: sub.usage.assetLimit },
    { label: "Contacts", used: sub.usage.contactCount, limit: sub.usage.contactLimit },
    { label: "Accounts", used: sub.usage.manualAccountCount, limit: sub.usage.manualAccountLimit },
    { label: "Storage", used: sub.usage.storageUsedBytes, limit: sub.usage.storageLimitBytes, isBytes: true },
    { label: "AI Calls", used: sub.usage.aiCallsThisMonth, limit: sub.usage.aiCallLimit },
  ];

  return (
    <div className="space-y-6">
      {cancelling && (
        <Alert variant="destructive">
          <AlertTriangle className="w-4 h-4" />
          <AlertDescription className="flex items-center justify-between">
            <span>{t("subscription.cancelWarning", { date: sub.subscriptionEndDate })}</span>
            <Button
              variant="outline"
              size="sm"
              onClick={() => { setCancelling(false); toast.success(t("subscription.restored")); }}
            >
              {t("subscription.keepSubscription")}
            </Button>
          </AlertDescription>
        </Alert>
      )}

      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <CreditCard className="w-5 h-5" /> {t("subscription.title")}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <span className="text-sm font-medium text-foreground">{t("subscription.currentPlan")}</span>
              <Badge variant={sub.tier === "Premium" ? "default" : "secondary"}>
                {sub.tier}
              </Badge>
            </div>
            {sub.tier === "Free" ? (
              <Button variant="gold" size="sm">{t("subscription.upgrade")}</Button>
            ) : !cancelling ? (
              <Button
                variant="outline"
                size="sm"
                onClick={() => { setCancelling(true); toast.success(t("subscription.cancelled")); }}
              >
                {t("subscription.cancel")}
              </Button>
            ) : null}
          </div>

          {sub.tier === "Premium" && sub.subscriptionStartDate && (
            <p className="text-xs text-muted-foreground">
              Billing period: {sub.subscriptionStartDate} — {sub.subscriptionEndDate}
            </p>
          )}
        </CardContent>
      </Card>

      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg">{t("subscription.usage")}</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {usageMeters.map((m) => (
              <div key={m.label} className="space-y-2 p-3 rounded-lg bg-secondary/50">
                <div className="flex justify-between text-sm">
                  <span className="font-medium text-foreground">{m.label}</span>
                  <span className="text-muted-foreground">
                    {m.isBytes ? formatBytes(m.used) : m.used} / {m.isBytes ? formatBytes(m.limit) : formatLimit(m.limit)}
                  </span>
                </div>
                <div
                  role="progressbar"
                  aria-label={m.label}
                  aria-valuenow={m.used}
                  aria-valuemax={m.limit === -1 ? undefined : m.limit}
                  className="relative h-2 rounded-full bg-muted overflow-hidden"
                >
                  <div
                    className={`absolute inset-y-0 left-0 rounded-full transition-all ${usageColor(m.used, m.limit)}`}
                    style={{ width: `${usagePct(m.used, m.limit)}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
