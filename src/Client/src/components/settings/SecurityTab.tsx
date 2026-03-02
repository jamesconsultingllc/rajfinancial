import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import { Shield, ExternalLink } from "lucide-react";

export function SecurityTab() {
  const { t } = useTranslation("settings");

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Shield className="w-5 h-5" /> {t("security.title")}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-foreground">{t("security.password")}</p>
              <p className="text-xs text-muted-foreground">{t("security.lastChanged", { days: 30 })}</p>
            </div>
            <Button variant="outline" size="sm">
              {t("security.changePassword")} <ExternalLink className="w-3 h-3 ml-1" />
            </Button>
          </div>

          <Separator />

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-foreground">{t("security.twoFactor")}</p>
              <p className="text-xs text-muted-foreground">{t("security.twoFactorDisabled")}</p>
            </div>
            <Button variant="outline" size="sm">
              {t("security.enableMfa")} <ExternalLink className="w-3 h-3 ml-1" />
            </Button>
          </div>

          <Separator />

          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-medium text-foreground">{t("security.connectedAccount")}</p>
              <p className="text-xs text-muted-foreground">{t("security.entraId")}</p>
            </div>
            <Badge variant="outline" className="text-[hsl(var(--success))] border-[hsl(var(--success)/0.3)]">
              {t("security.connected")}
            </Badge>
          </div>
        </CardContent>
      </Card>

      <p className="text-xs text-muted-foreground">
        Password and MFA are managed through your Microsoft Entra ID account. Clicking the buttons above will redirect you to the Entra portal.
      </p>
    </div>
  );
}
