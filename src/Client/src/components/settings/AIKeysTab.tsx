import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Key, Info } from "lucide-react";
import { toast } from "sonner";
import { mockProfile } from "@/data/mock-settings";

export function AIKeysTab() {
  const { t } = useTranslation("settings");
  const [hasKey, setHasKey] = useState(mockProfile.hasByokKey);
  const [keyValue, setKeyValue] = useState("");

  const handleSave = () => {
    if (!keyValue.trim()) return;
    setHasKey(true);
    setKeyValue("");
    toast.success(t("aiKeys.saved"));
  };

  const handleRemove = () => {
    setHasKey(false);
    toast.success(t("aiKeys.removed"));
  };

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Key className="w-5 h-5" /> {t("aiKeys.title")}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-3">
            <span className="text-sm font-medium text-foreground">{t("aiKeys.status")}</span>
            <Badge variant={hasKey ? "default" : "secondary"}>
              {hasKey ? t("aiKeys.configured") : t("aiKeys.notConfigured")}
            </Badge>
          </div>

          <div className="space-y-2">
            <Label htmlFor="apiKey">{hasKey ? t("aiKeys.updateKey") : t("aiKeys.setKey")}</Label>
            <Input
              id="apiKey"
              type="password"
              placeholder="sk-ant-..."
              value={keyValue}
              onChange={(e) => setKeyValue(e.target.value)}
            />
          </div>

          <div className="flex gap-3">
            <Button variant="gold" onClick={handleSave} disabled={!keyValue.trim()}>
              {t("aiKeys.saveKey")}
            </Button>
            {hasKey && (
              <Button variant="outline" className="text-destructive border-destructive/30 hover:bg-destructive/10" onClick={handleRemove}>
                {t("aiKeys.removeKey")}
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      <Alert>
        <Info className="w-4 h-4" />
        <AlertDescription>
          {t("aiKeys.info")}
        </AlertDescription>
      </Alert>
    </div>
  );
}
