import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { AlertTriangle, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { mockProfile } from "@/data/mock-settings";

export function DeleteAccountTab() {
  const { t } = useTranslation("settings");
  const [deletionRequested, setDeletionRequested] = useState(!!mockProfile.deletionRequestedAt);
  const [confirmEmail, setConfirmEmail] = useState("");
  const [reason, setReason] = useState("");

  const deletionDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toLocaleDateString("en-US", {
    month: "long",
    day: "numeric",
    year: "numeric",
  });

  if (deletionRequested) {
    return (
      <Alert variant="destructive">
        <AlertTriangle className="w-4 h-4" />
        <AlertDescription className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <span>{t("delete.scheduledBanner", { date: deletionDate, days: 30 })}</span>
          <Button
            variant="outline"
            size="sm"
            onClick={() => { setDeletionRequested(false); toast.success(t("delete.deletionCancelled")); }}
          >
            {t("delete.cancelDeletion")}
          </Button>
        </AlertDescription>
      </Alert>
    );
  }

  const emailMatches = confirmEmail.toLowerCase() === mockProfile.email.toLowerCase();

  return (
    <div className="space-y-6">
      <Card className="bg-card border-destructive/30">
        <CardContent className="p-6 space-y-4">
          <div className="flex items-start gap-3">
            <AlertTriangle className="w-6 h-6 text-destructive shrink-0 mt-0.5" />
            <div className="space-y-3">
              <h3 className="text-lg font-semibold text-destructive">{t("delete.title")}</h3>
              <p className="text-sm text-muted-foreground">
                {t("delete.warning")}
              </p>
              <ul className="text-sm text-muted-foreground list-disc list-inside space-y-1">
                <li>{t("delete.items.profile")}</li>
                <li>{t("delete.items.assets")}</li>
                <li>{t("delete.items.contacts")}</li>
                <li>{t("delete.items.accounts")}</li>
                <li>{t("delete.items.documents")}</li>
                <li>{t("delete.items.keys")}</li>
              </ul>
            </div>
          </div>

          <AlertDialog>
            <AlertDialogTrigger asChild>
              <Button variant="destructive">
                <Trash2 className="w-4 h-4 mr-1" /> {t("delete.deleteButton")}
              </Button>
            </AlertDialogTrigger>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>{t("delete.confirmTitle")}</AlertDialogTitle>
                <AlertDialogDescription>
                  {t("delete.confirmDescription")}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <div className="space-y-4 py-2">
                <div className="space-y-2">
                  <Label htmlFor="confirmEmail">{t("delete.confirmEmail")}</Label>
                  <Input
                    id="confirmEmail"
                    placeholder={mockProfile.email}
                    value={confirmEmail}
                    onChange={(e) => setConfirmEmail(e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="deleteReason">{t("delete.reasonLabel")}</Label>
                  <Textarea
                    id="deleteReason"
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                    maxLength={500}
                    placeholder={t("delete.reasonPlaceholder")}
                    rows={3}
                  />
                </div>
              </div>
              <AlertDialogFooter>
                <AlertDialogCancel>{t("delete.cancelButton")}</AlertDialogCancel>
                <AlertDialogAction
                  disabled={!emailMatches}
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                  onClick={() => { setDeletionRequested(true); toast.success(t("delete.requested")); }}
                >
                  {t("delete.confirmDeleteButton")}
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </CardContent>
      </Card>
    </div>
  );
}
