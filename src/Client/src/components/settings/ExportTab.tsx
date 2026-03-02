import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Download, Loader2, Check, AlertTriangle } from "lucide-react";
import type { DataExportStatusDto } from "@/types/settings";

export function ExportTab() {
  const { t } = useTranslation("settings");
  const [exportStatus, setExportStatus] = useState<DataExportStatusDto | null>(null);

  const handleRequestExport = () => {
    setExportStatus({
      exportId: "exp-001",
      status: "Queued",
      requestedAt: new Date().toISOString(),
    });

    // Simulate processing
    setTimeout(() => {
      setExportStatus(prev => prev ? { ...prev, status: "Processing" } : null);
    }, 2000);

    setTimeout(() => {
      setExportStatus({
        exportId: "exp-001",
        status: "Completed",
        requestedAt: new Date().toISOString(),
        completedAt: new Date().toISOString(),
        fileSizeBytes: 4523000,
        downloadUrl: "#",
        downloadExpiresAt: new Date(Date.now() + 3600000).toISOString(),
      });
    }, 4000);
  };

  const formatBytes = (bytes: number) => {
    const mb = bytes / (1024 * 1024);
    return `${mb.toFixed(1)} MB`;
  };

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Download className="w-5 h-5" /> {t("export.title")}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            {t("export.description")}
          </p>

          {!exportStatus && (
            <Button variant="gold" onClick={handleRequestExport}>
              {t("export.requestExport")}
            </Button>
          )}

          {exportStatus && (
            <div className="p-4 rounded-lg bg-secondary/50 space-y-2">
              {(exportStatus.status === "Queued" || exportStatus.status === "Processing") && (
                <div className="flex items-center gap-2 text-sm">
                  <Loader2 className="w-4 h-4 animate-spin text-primary" />
                  <span className="text-foreground">
                    {exportStatus.status === "Queued" ? t("export.queued") : t("export.processing")}
                  </span>
                </div>
              )}
              {exportStatus.status === "Completed" && (
                <div className="space-y-3">
                  <div className="flex items-center gap-2 text-sm text-[hsl(var(--success))]">
                    <Check className="w-4 h-4" />
                    <span>{t("export.completed", { size: formatBytes(exportStatus.fileSizeBytes!) })}</span>
                  </div>
                  <div className="flex items-center gap-3">
                    <Button variant="gold" size="sm">{t("export.download")}</Button>
                    <Button variant="outline" size="sm" onClick={() => setExportStatus(null)}>
                      {t("export.dismiss")}
                    </Button>
                  </div>
                  <p className="text-xs text-muted-foreground">{t("export.linkExpiry")}</p>
                </div>
              )}
              {exportStatus.status === "Failed" && (
                <div className="flex items-center gap-2">
                  <AlertTriangle className="w-4 h-4 text-destructive" />
                  <span className="text-sm text-destructive">{t("export.failed")}</span>
                  <Button variant="outline" size="sm" onClick={handleRequestExport}>{t("export.retry")}</Button>
                </div>
              )}
            </div>
          )}

          <p className="text-xs text-muted-foreground">
            {t("export.linkExpiryNote")}
          </p>

          <div className="text-xs text-muted-foreground">
            <p className="font-medium mb-1">{t("export.includes")}</p>
            <p>{t("export.includesList")}</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
