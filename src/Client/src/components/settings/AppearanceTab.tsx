import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Palette, Monitor, Sun, Moon } from "lucide-react";
import { useTheme } from "@/hooks/use-theme";
import { toast } from "sonner";

const themes = [
  {
    value: "system",
    label: "System",
    description: "Match your operating system preference",
    icon: Monitor,
  },
  {
    value: "light",
    label: "Light",
    description: "Light background with dark text",
    icon: Sun,
  },
  {
    value: "dark",
    label: "Dark",
    description: "Dark background with light text",
    icon: Moon,
  },
];

export function AppearanceTab() {
  const { t } = useTranslation("settings");
  const { theme, setTheme } = useTheme();

  const handleChange = (value: string) => {
    setTheme(value as "dark" | "light" | "system");
    toast.success(t("appearance.changed", { theme: value }));
  };

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardHeader>
          <CardTitle className="text-lg flex items-center gap-2">
            <Palette className="w-5 h-5" /> {t("appearance.title")}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <RadioGroup value={theme} onValueChange={handleChange} className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            {themes.map((t) => {
              const Icon = t.icon;
              return (
                <label
                  key={t.value}
                  className={`relative flex flex-col items-center gap-3 p-6 rounded-lg border-2 cursor-pointer transition-all ${
                    theme === t.value
                      ? "border-primary bg-primary/5"
                      : "border-border/50 hover:border-border"
                  }`}
                >
                  <RadioGroupItem value={t.value} className="sr-only" />
                  <Icon className={`w-8 h-8 ${theme === t.value ? "text-primary" : "text-muted-foreground"}`} />
                  <span className="text-sm font-medium text-foreground">{t.label}</span>
                  <span className="text-xs text-muted-foreground text-center">{t.description}</span>
                </label>
              );
            })}
          </RadioGroup>
        </CardContent>
      </Card>
    </div>
  );
}
