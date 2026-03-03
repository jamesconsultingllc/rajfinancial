import { useNavigate, useParams } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Helmet } from "react-helmet-async";
import { cn } from "@/lib/utils";
import { ScrollArea, ScrollBar } from "@/components/ui/scroll-area";
import {
  User, CreditCard, Key, Share2, Bell, Palette, Download, Trash2,
} from "lucide-react";

import { ProfileTab } from "@/components/settings/ProfileTab";
import { SubscriptionTab } from "@/components/settings/SubscriptionTab";
import { AIKeysTab } from "@/components/settings/AIKeysTab";
import { SharingTab } from "@/components/settings/SharingTab";
import { NotificationsTab } from "@/components/settings/NotificationsTab";
import { AppearanceTab } from "@/components/settings/AppearanceTab";
import { ExportTab } from "@/components/settings/ExportTab";
import { DeleteAccountTab } from "@/components/settings/DeleteAccountTab";

const TAB_IDS = ["profile", "subscription", "ai-keys", "sharing", "notifications", "appearance", "export", "delete"] as const;
type TabId = typeof TAB_IDS[number];

interface TabDef {
  id: TabId;
  label: string;
  icon: typeof User;
  destructive?: boolean;
}

const tabs: TabDef[] = [
  { id: "profile", label: "Profile", icon: User },
  { id: "subscription", label: "Subscription", icon: CreditCard },
  { id: "ai-keys", label: "AI Keys", icon: Key },
  { id: "sharing", label: "Sharing", icon: Share2 },
  { id: "notifications", label: "Notifications", icon: Bell },
  { id: "appearance", label: "Appearance", icon: Palette },
  { id: "export", label: "Export", icon: Download },
  { id: "delete", label: "Delete Account", icon: Trash2, destructive: true },
];

const tabComponents: Record<TabId, React.FC> = {
  profile: ProfileTab,
  subscription: SubscriptionTab,
  "ai-keys": AIKeysTab,
  sharing: SharingTab,
  notifications: NotificationsTab,
  appearance: AppearanceTab,
  export: ExportTab,
  delete: DeleteAccountTab,
};

export default function Settings() {
  const { tab } = useParams<{ tab?: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation("settings");
  const activeTab: TabId = (tabs.find(t => t.id === tab)?.id ?? "profile");

  const ActiveComponent = tabComponents[activeTab] ?? tabComponents.profile;

  const handleTabChange = (id: string) => {
    navigate(`/settings/${id}`, { replace: true });
  };

  return (
    <DashboardLayout>
      <Helmet>
        <title>{t("title")} | RAJ Financial</title>
      </Helmet>

      <div className="max-w-5xl mx-auto space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-foreground">{t("title")}</h1>
          <p className="text-muted-foreground text-sm mt-1">{t("subtitle")}</p>
        </div>

        <div className="flex flex-col md:flex-row gap-6">
          {/* Desktop sidebar nav */}
          <nav className="hidden md:flex flex-col w-56 shrink-0 space-y-1" aria-label={t("title")}>
            {tabs.map((tabDef) => {
              const Icon = tabDef.icon;
              const isActive = activeTab === tabDef.id;
              return (
                <button
                  key={tabDef.id}
                  onClick={() => handleTabChange(tabDef.id)}
                  aria-current={isActive ? "page" : undefined}
                  className={cn(
                    "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors text-left w-full",
                    isActive
                      ? "bg-primary/10 text-primary"
                      : tabDef.destructive
                        ? "text-destructive hover:bg-destructive/10"
                        : "text-muted-foreground hover:bg-secondary hover:text-foreground"
                  )}
                >
                  <Icon className="w-4 h-4 shrink-0" />
                  {t(`tabs.${tabDef.id}`, tabDef.label)}
                </button>
              );
            })}
          </nav>

          {/* Mobile horizontal tabs */}
          <div className="md:hidden">
            <ScrollArea className="w-full">
              <div className="flex gap-1 pb-2">
                {tabs.map((tabDef) => {
                  const Icon = tabDef.icon;
                  const isActive = activeTab === tabDef.id;
                  return (
                    <button
                      key={tabDef.id}
                      onClick={() => handleTabChange(tabDef.id)}
                      aria-current={isActive ? "page" : undefined}
                      className={cn(
                        "flex items-center gap-1.5 px-3 py-2 rounded-lg text-xs font-medium whitespace-nowrap transition-colors shrink-0",
                        isActive
                          ? "bg-primary/10 text-primary"
                          : tabDef.destructive
                            ? "text-destructive"
                            : "text-muted-foreground hover:bg-secondary"
                      )}
                    >
                      <Icon className="w-3.5 h-3.5" />
                      {t(`tabs.${tabDef.id}`, tabDef.label)}
                    </button>
                  );
                })}
              </div>
              <ScrollBar orientation="horizontal" />
            </ScrollArea>
          </div>

          {/* Content */}
          <div className="flex-1 min-w-0">
            <ActiveComponent />
          </div>
        </div>
      </div>
    </DashboardLayout>
  );
}
