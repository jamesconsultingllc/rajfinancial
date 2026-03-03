import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Bell } from "lucide-react";
import { toast } from "sonner";
import { mockProfile } from "@/data/mock-settings";

export function NotificationsTab() {
  const { t } = useTranslation("settings");
  const [digest, setDigest] = useState(mockProfile.emailDigestEnabled);
  const [syncAlerts, setSyncAlerts] = useState(mockProfile.alertSyncIssues);
  const [coverageAlerts, setCoverageAlerts] = useState(mockProfile.alertCoverageGaps);
  const [tierAlerts, setTierAlerts] = useState(mockProfile.alertTierLimits);

  const handleSave = () => {
    toast.success(t("notifications.saved"));
  };

  const notifications = [
    {
      id: "digest",
      label: t("notifications.digest"),
      description: t("notifications.digestDescription"),
      checked: digest,
      onChange: setDigest,
    },
    {
      id: "sync",
      label: t("notifications.syncAlerts"),
      description: t("notifications.syncAlertsDescription"),
      checked: syncAlerts,
      onChange: setSyncAlerts,
    },
    {
      id: "coverage",
      label: t("notifications.coverageAlerts"),
      description: t("notifications.coverageAlertsDescription"),
      checked: coverageAlerts,
      onChange: setCoverageAlerts,
    },
    {
      id: "tier",
      label: t("notifications.tierAlerts"),
      description: t("notifications.tierAlertsDescription"),
      checked: tierAlerts,
      onChange: setTierAlerts,
    },
  ];

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Bell className="w-5 h-5" /> {t("notifications.title")}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          {notifications.map((n) => (
            <div key={n.id} className="flex items-center justify-between gap-4">
              <div className="space-y-0.5">
                <Label htmlFor={n.id} className="text-sm font-medium cursor-pointer">{n.label}</Label>
                <p className="text-xs text-muted-foreground">{n.description}</p>
              </div>
              <Switch id={n.id} checked={n.checked} onCheckedChange={n.onChange} />
            </div>
          ))}

          <div className="flex justify-end pt-2">
            <Button variant="gold" onClick={handleSave}>{t("notifications.savePreferences")}</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
