import { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import { Camera, Lock, Calendar } from "lucide-react";
import { toast } from "sonner";
import { useAuth } from "@/auth/useAuth";
import { useAuthProfile } from "@/hooks/use-auth-profile";
import { useUpdateProfile } from "@/hooks/use-update-profile";

const LOCALES = [
  { value: "en-US", label: "English (US)" },
  { value: "es-MX", label: "Español (MX)" },
  { value: "es-ES", label: "Español (ES)" },
  { value: "fr-FR", label: "Français (FR)" },
  { value: "pt-BR", label: "Português (BR)" },
];

const TIMEZONES = [
  "America/New_York", "America/Chicago", "America/Denver", "America/Los_Angeles",
  "America/Anchorage", "Pacific/Honolulu", "Europe/London", "Europe/Paris", "Asia/Tokyo",
];

const CURRENCIES = ["USD", "EUR", "GBP", "MXN", "BRL", "CAD"];

export function ProfileTab() {
  const { t } = useTranslation("settings");
  const { user } = useAuth();
  const { profile: apiProfile, isLoading } = useAuthProfile();
  const { mutate: updateProfile, isPending } = useUpdateProfile();

  // Controlled form state
  const [editDisplayName, setEditDisplayName] = useState("");
  const [locale, setLocale] = useState("en-US");
  const [timezone, setTimezone] = useState("America/New_York");
  const [currency, setCurrency] = useState("USD");

  // Sync form state when API profile loads
  useEffect(() => {
    if (apiProfile) {
      setEditDisplayName(apiProfile.displayName ?? "");
      // TODO: Read preferences from API when PreferencesJson is exposed in the response
    }
  }, [apiProfile]);

  const handleSave = () => {
    updateProfile(
      { displayName: editDisplayName, locale, timezone, currency },
      {
        onSuccess: () => toast.success(t("profile.saved")),
        onError: () => toast.error(t("profile.saveError", { defaultValue: "Failed to save profile" })),
      },
    );
  };

  // Use API profile data when available, fall back to useAuth
  const displayName = apiProfile?.displayName ?? user?.name ?? "User";
  const email = user?.email ?? "";
  const initials = user?.initials ?? displayName.split(" ").map(n => n[0]).join("").slice(0, 2);

  // Format member since date from profile createdAt
  const memberSince = apiProfile?.createdAt
    ? new Date(apiProfile.createdAt).toLocaleDateString("en-US", {
        month: "long",
        year: "numeric",
      })
    : null;

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Card className="bg-card border-border/50">
          <CardContent className="p-6">
            <div className="flex items-center gap-6">
              <Skeleton className="w-20 h-20 rounded-full" />
              <div className="space-y-2">
                <Skeleton className="h-6 w-40" />
                <Skeleton className="h-4 w-48" />
              </div>
            </div>
          </CardContent>
        </Card>
        <Card className="bg-card border-border/50">
          <CardHeader>
            <Skeleton className="h-6 w-32" />
          </CardHeader>
          <CardContent className="space-y-4">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardContent className="p-6">
          <div className="flex items-center gap-6">
            <div className="relative">
              <Avatar className="w-20 h-20">
                <AvatarImage />
                <AvatarFallback className="bg-primary/20 text-primary text-xl font-semibold">
                  {initials}
                </AvatarFallback>
              </Avatar>
              <button
                aria-label={t("profile.uploadAvatar")}
                className="absolute -bottom-1 -right-1 w-9 h-9 rounded-full bg-primary text-primary-foreground flex items-center justify-center hover:bg-primary/90 transition-colors"
              >
                <Camera className="w-4 h-4" aria-hidden="true" />
              </button>
            </div>
            <div>
              <h2 className="text-lg font-semibold text-foreground">{displayName}</h2>
              <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                <Lock className="w-3.5 h-3.5" />
                <span>{email}</span>
              </div>
              {memberSince && (
                <div className="flex items-center gap-1 mt-1 text-xs text-muted-foreground">
                  <Calendar className="w-3 h-3" />
                  <span>{t("profile.memberSince", { date: memberSince })}</span>
                </div>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg">{t("profile.personalInfo")}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="displayName">{t("profile.displayName")}</Label>
            <Input id="displayName" value={editDisplayName} onChange={e => setEditDisplayName(e.target.value)} maxLength={200} />
          </div>

          <Separator />
          <h3 className="text-sm font-semibold text-foreground">{t("profile.preferences")}</h3>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label>{t("profile.locale")}</Label>
              <Select value={locale} onValueChange={setLocale}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {LOCALES.map(l => (
                    <SelectItem key={l.value} value={l.value}>{l.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t("profile.timezone")}</Label>
              <Select value={timezone} onValueChange={setTimezone}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {TIMEZONES.map(tz => (
                    <SelectItem key={tz} value={tz}>{tz.replace(/_/g, " ")}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>{t("profile.currency")}</Label>
              <Select value={currency} onValueChange={setCurrency}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {CURRENCIES.map(c => (
                    <SelectItem key={c} value={c}>{c}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <Separator />
          <div className="flex justify-end">
            <Button variant="gold" onClick={handleSave} disabled={isPending}>
              {isPending ? t("profile.saving", { defaultValue: "Saving..." }) : t("profile.saveChanges")}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
