import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, type BadgeProps } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter } from "@/components/ui/dialog";
import { DatePicker } from "@/components/ui/date-picker";
import { Share2, Lock, UserPlus } from "lucide-react";
import { toast } from "sonner";
import { mockProfile, mockGrants } from "@/data/mock-settings";
import type { DataAccessGrantDto } from "@/types/settings";

const DATA_CATEGORIES = ["Assets", "Contacts", "Accounts", "Transactions", "Documents"];

const accessBadgeVariant = (type: string): BadgeProps["variant"] => {
  if (type === "Read") return "secondary";
  if (type === "Full") return "default";
  return "outline";
};

export function SharingTab() {
  const { t } = useTranslation("settings");
  const [grants, setGrants] = useState<DataAccessGrantDto[]>(mockGrants);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteAccess, setInviteAccess] = useState<string>("Read");
  const [inviteCategories, setInviteCategories] = useState<string[]>([]);
  const [inviteExpiry, setInviteExpiry] = useState<string | undefined>();

  const isPremium = mockProfile.tier === "Premium";

  if (!isPremium) {
    return (
      <Card className="bg-card border-border/50">
        <CardContent className="p-8 text-center space-y-4">
          <Lock className="w-12 h-12 mx-auto text-muted-foreground" />
          <h3 className="text-lg font-semibold text-foreground">{t("sharing.premiumTitle")}</h3>
          <p className="text-sm text-muted-foreground max-w-md mx-auto">
            {t("sharing.premiumDescription")}
          </p>
          <Button variant="gold">{t("subscription.upgrade")}</Button>
        </CardContent>
      </Card>
    );
  }

  const handleInvite = () => {
    if (!inviteEmail.trim() || inviteCategories.length === 0) return;
    const newGrant: DataAccessGrantDto = {
      id: `grant-${Date.now()}`,
      granteeId: `adv-${Date.now()}`,
      granteeName: inviteEmail.split("@")[0],
      granteeEmail: inviteEmail,
      accessType: inviteAccess as "Read" | "Limited" | "Full",
      dataCategories: inviteCategories,
      expiresAt: inviteExpiry,
      isActive: true,
      createdAt: new Date().toISOString().split("T")[0],
    };
    setGrants([...grants, newGrant]);
    setDialogOpen(false);
    setInviteEmail("");
    setInviteCategories([]);
    setInviteExpiry(undefined);
    toast.success(t("sharing.inviteSent"));
  };

  const handleRevoke = (id: string) => {
    setGrants(grants.filter(g => g.id !== id));
    toast.success(t("sharing.revoked"));
  };

  const toggleCategory = (cat: string) => {
    setInviteCategories(prev =>
      prev.includes(cat) ? prev.filter(c => c !== cat) : [...prev, cat]
    );
  };

  return (
    <div className="space-y-6">
      <Card className="bg-card border-border/50">
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-lg flex items-center gap-2">
            <Share2 className="w-5 h-5" /> {t("sharing.title")}
          </CardTitle>
          <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
            <DialogTrigger asChild>
              <Button variant="gold" size="sm">
                <UserPlus className="w-4 h-4 mr-1" /> {t("sharing.inviteAdvisor")}
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>{t("sharing.inviteAdvisor")}</DialogTitle>
              </DialogHeader>
              <div className="space-y-4 py-2">
                <div className="space-y-2">
                  <Label htmlFor="invite-email">{t("sharing.email")}</Label>
                  <Input id="invite-email" type="email" placeholder="advisor@example.com" value={inviteEmail} onChange={e => setInviteEmail(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="invite-access">{t("sharing.accessType")}</Label>
                  <Select value={inviteAccess} onValueChange={setInviteAccess}>
                    <SelectTrigger id="invite-access"><SelectValue /></SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Read">Read</SelectItem>
                      <SelectItem value="Limited">Limited</SelectItem>
                      <SelectItem value="Full">Full</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="space-y-2">
                  <Label>{t("sharing.dataCategories")}</Label>
                  <div className="flex flex-wrap gap-3">
                    {DATA_CATEGORIES.map(cat => (
                      <label key={cat} className="flex items-center gap-2 text-sm">
                        <Checkbox
                          checked={inviteCategories.includes(cat)}
                          onCheckedChange={() => toggleCategory(cat)}
                        />
                        {cat}
                      </label>
                    ))}
                  </div>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="invite-expiry">{t("sharing.expiryDate")}</Label>
                  <DatePicker
                    value={inviteExpiry}
                    onChange={(val) => setInviteExpiry(val)}
                  />
                </div>
              </div>
              <DialogFooter>
                <Button variant="outline" onClick={() => setDialogOpen(false)}>{t("sharing.cancel")}</Button>
                <Button variant="gold" onClick={handleInvite} disabled={!inviteEmail.trim() || inviteCategories.length === 0}>
                  {t("sharing.sendInvitation")}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </CardHeader>
        <CardContent className="space-y-4">
          {grants.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-6">
              {t("sharing.emptyState")}
            </p>
          ) : (
            grants.map(grant => (
              <div key={grant.id} className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 rounded-lg bg-secondary/50">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-foreground">{grant.granteeName}</span>
                    <Badge variant={accessBadgeVariant(grant.accessType)}>{grant.accessType}</Badge>
                  </div>
                  <p className="text-xs text-muted-foreground">{grant.granteeEmail}</p>
                  <div className="flex flex-wrap gap-1 mt-1">
                    {grant.dataCategories.map(cat => (
                      <span key={cat} className="px-2 py-0.5 text-xs rounded-full bg-muted text-muted-foreground">{cat}</span>
                    ))}
                  </div>
                  <p className="text-xs text-muted-foreground">
                    {grant.expiresAt ? t("sharing.expires", { date: grant.expiresAt }) : t("sharing.noExpiry")}
                  </p>
                </div>
                <Button
                  variant="outline"
                  size="sm"
                  className="text-destructive border-destructive/30 hover:bg-destructive/10"
                  onClick={() => handleRevoke(grant.id)}
                >
                  {t("sharing.revokeAccess")}
                </Button>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
