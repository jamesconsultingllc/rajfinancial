import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Separator } from "@/components/ui/separator";
import { Camera, Lock, Calendar } from "lucide-react";
import { toast } from "sonner";
import { mockProfile } from "@/data/mock-settings";

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

const COUNTRIES = [
  { value: "US", label: "United States" },
  { value: "CA", label: "Canada" },
  { value: "MX", label: "Mexico" },
  { value: "GB", label: "United Kingdom" },
  { value: "FR", label: "France" },
  { value: "BR", label: "Brazil" },
  { value: "JP", label: "Japan" },
];

export function ProfileTab() {
  const { t } = useTranslation("settings");
  const [profile] = useState(mockProfile);

  const handleSave = () => {
    toast.success(t("profile.saved"));
  };

  const memberSince = new Date(profile.createdAt).toLocaleDateString("en-US", {
    month: "long",
    year: "numeric",
  });

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardContent className="p-6">
          <div className="flex items-center gap-6">
            <div className="relative">
              <Avatar className="w-20 h-20">
                <AvatarImage src={profile.avatarUrl} />
                <AvatarFallback className="bg-primary/20 text-primary text-xl font-semibold">
                  {profile.displayName.split(" ").map(n => n[0]).join("")}
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
              <h2 className="text-lg font-semibold text-foreground">{profile.displayName}</h2>
              <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
                <Lock className="w-3.5 h-3.5" />
                <span>{profile.email}</span>
              </div>
              <div className="flex items-center gap-1 mt-1 text-xs text-muted-foreground">
                <Calendar className="w-3 h-3" />
                <span>{t("profile.memberSince", { date: memberSince })}</span>
              </div>
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
            <Input id="displayName" defaultValue={profile.displayName} maxLength={200} />
          </div>

          <div className="space-y-2">
            <Label htmlFor="phone">{t("profile.phone")}</Label>
            <Input id="phone" type="tel" defaultValue={profile.phone} maxLength={30} />
          </div>

          <Separator />
          <h3 className="text-sm font-semibold text-foreground">{t("profile.address")}</h3>

          <div className="space-y-2">
            <Label htmlFor="street1">{t("profile.street1")}</Label>
            <Input id="street1" defaultValue={profile.address?.street1} maxLength={200} />
          </div>
          <div className="space-y-2">
            <Label htmlFor="street2">{t("profile.street2")}</Label>
            <Input id="street2" defaultValue={profile.address?.street2} maxLength={200} />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="city">{t("profile.city")}</Label>
              <Input id="city" defaultValue={profile.address?.city} maxLength={100} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="state">{t("profile.state")}</Label>
              <Input id="state" defaultValue={profile.address?.state} maxLength={100} />
            </div>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="postalCode">{t("profile.postalCode")}</Label>
              <Input id="postalCode" defaultValue={profile.address?.postalCode} maxLength={20} />
            </div>
            <div className="space-y-2">
              <Label htmlFor="country">{t("profile.country")}</Label>
              <Select defaultValue={profile.address?.country || "US"}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {COUNTRIES.map(c => (
                    <SelectItem key={c.value} value={c.value}>{c.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <Separator />
          <h3 className="text-sm font-semibold text-foreground">{t("profile.preferences")}</h3>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label>{t("profile.locale")}</Label>
              <Select defaultValue={profile.locale}>
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
              <Select defaultValue={profile.timezone}>
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
              <Select defaultValue={profile.currencyCode}>
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
            <Button variant="gold" onClick={handleSave}>{t("profile.saveChanges")}</Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
