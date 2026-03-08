import { useState, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { Helmet } from "react-helmet-async";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table, TableHeader, TableHead, TableBody, TableRow, TableCell,
} from "@/components/ui/table";
import {
  DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger, DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Plus, MoreHorizontal, ChevronRight, AlertTriangle, Package,
  Pencil, DollarSign, Users, Trash2, CheckCircle2,
  LayoutGrid, List,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAssets, useCreateAsset, useUpdateAsset, useDeleteAsset } from "@/services/asset-service";
import { computeAssetsSummary } from "@/data/mock-assets";
import {
  type AssetDto, type AssetType, ASSET_TYPE_LABELS, ASSET_TYPE_ICONS, formatCurrency,
} from "@/types/assets";
import { AssetFormSheet } from "@/components/assets/AssetFormSheet";
import { BeneficiaryAssignmentDialog } from "@/components/assets/BeneficiaryAssignmentDialog";
import { toast } from "sonner";

const FILTER_VALUES: (AssetType | "All")[] = [
  "All", "RealEstate", "Vehicle", "Investment", "Retirement", "BankAccount", "Business", "Other",
];

function SummaryCards({ assets }: { assets: AssetDto[] }) {
  const { t } = useTranslation("assets");
  const summary = computeAssetsSummary(assets);
  const cards = [
    { label: t("summary.totalValue"), value: formatCurrency(summary.totalValue), accent: true },
    { label: t("summary.count"), value: String(summary.count) },
    { label: t("summary.topCategory"), value: summary.topCategory },
    {
      label: t("summary.needsAttention"),
      value: String(summary.needsAttention),
      warning: summary.needsAttention > 0,
    },
  ];

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      {cards.map((c) => (
        <Card key={c.label} className="bg-card border-border/50">
          <CardContent className="p-4">
            <p className="text-sm text-muted-foreground">{c.label}</p>
            <p
              className={cn(
                "text-2xl font-bold mt-1",
                c.accent && "text-primary",
                c.warning && "text-destructive"
              )}
            >
              {c.value}
            </p>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function AssetIcon({ type }: { type: AssetType }) {
  const Icon = ASSET_TYPE_ICONS[type];
  return (
    <div className="w-9 h-9 rounded-lg bg-primary/10 flex items-center justify-center shrink-0">
      <Icon className="w-4.5 h-4.5 text-primary" />
    </div>
  );
}

function ActionsMenu({ asset, onEdit, onDelete, onManageBeneficiaries }: {
  asset: AssetDto;
  onEdit: (asset: AssetDto) => void;
  onDelete: (asset: AssetDto) => void;
  onManageBeneficiaries: (asset: AssetDto) => void;
}) {
  const { t } = useTranslation("assets");
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-8 w-8" aria-label={t("actions.moreActions")}>
          <MoreHorizontal className="w-4 h-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => onEdit(asset)}>
          <Pencil className="w-4 h-4 mr-2" /> {t("actions.edit")}
        </DropdownMenuItem>
        <DropdownMenuItem>
          <DollarSign className="w-4 h-4 mr-2" /> {t("actions.updateValue")}
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => onManageBeneficiaries(asset)}>
          <Users className="w-4 h-4 mr-2" /> {t("actions.manageBeneficiaries")}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="text-destructive focus:text-destructive"
          onClick={() => onDelete(asset)}
        >
          <Trash2 className="w-4 h-4 mr-2" /> {t("actions.delete")}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function BeneficiaryStatus({ has }: { has: boolean }) {
  const { t } = useTranslation("assets");
  if (has) {
    return (
      <span className="inline-flex items-center gap-1 text-sm text-[hsl(var(--success))]">
        <CheckCircle2 className="w-4 h-4" /> {t("beneficiaryStatus.assigned")}
      </span>
    );
  }
  return <Badge variant="secondary" className="bg-primary/10 text-primary border-0">{t("beneficiaryStatus.none")}</Badge>;
}

function AssetTable({ assets, onEdit, onDelete, onManageBeneficiaries }: {
  assets: AssetDto[];
  onEdit: (asset: AssetDto) => void;
  onDelete: (asset: AssetDto) => void;
  onManageBeneficiaries: (asset: AssetDto) => void;
}) {
  const { t } = useTranslation("assets");
  return (
    <div>
      <Card className="bg-card border-border/50">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t("table.name")}</TableHead>
              <TableHead className="text-right">{t("table.currentValue")}</TableHead>
              <TableHead className="text-right">{t("table.purchasePrice")}</TableHead>
              <TableHead>{t("table.beneficiaries")}</TableHead>
              <TableHead className="w-12" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {assets.map((asset) => (
              <TableRow key={asset.id}>
                <TableCell>
                  <div className="flex items-center gap-3">
                    <AssetIcon type={asset.type} />
                    <div>
                      <p className="font-medium text-foreground">{asset.name}</p>
                      <p className="text-xs text-muted-foreground">{ASSET_TYPE_LABELS[asset.type]}</p>
                    </div>
                  </div>
                </TableCell>
                <TableCell className="text-right font-semibold text-foreground">
                  {formatCurrency(asset.currentValue)}
                </TableCell>
                <TableCell className="text-right text-muted-foreground">
                  {asset.purchasePrice ? formatCurrency(asset.purchasePrice) : "—"}
                </TableCell>
                <TableCell>
                  <BeneficiaryStatus has={asset.hasBeneficiaries} />
                </TableCell>
                <TableCell>
                  <ActionsMenu asset={asset} onEdit={onEdit} onDelete={onDelete} onManageBeneficiaries={onManageBeneficiaries} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>
    </div>
  );
}

function CardGrid({ assets, onEdit, onDelete, onManageBeneficiaries }: {
  assets: AssetDto[];
  onEdit: (asset: AssetDto) => void;
  onDelete: (asset: AssetDto) => void;
  onManageBeneficiaries: (asset: AssetDto) => void;
}) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      {assets.map((asset) => (
        <Card key={asset.id} className="bg-card border-border/50 hover:border-primary/30 transition-colors">
          <CardContent className="p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-3 min-w-0 cursor-pointer" role="button" tabIndex={0} onClick={() => onEdit(asset)} onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onEdit(asset); } }}>
                <AssetIcon type={asset.type} />
                <div className="min-w-0">
                  <p className="font-medium text-foreground truncate">{asset.name}</p>
                  <p className="text-xs text-muted-foreground">{ASSET_TYPE_LABELS[asset.type]}</p>
                </div>
              </div>
              <div onClick={(e) => e.stopPropagation()}>
                <ActionsMenu asset={asset} onEdit={onEdit} onDelete={onDelete} onManageBeneficiaries={onManageBeneficiaries} />
              </div>
            </div>
            <div className="flex items-center justify-between mt-3 cursor-pointer" role="button" tabIndex={0} onClick={() => onEdit(asset)} onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onEdit(asset); } }}>
              <p className="text-lg font-bold text-foreground">{formatCurrency(asset.currentValue)}</p>
              <ChevronRight className="w-5 h-5 text-muted-foreground" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}


function LoadingState() {
  return (
    <div className="space-y-3">
      {[1, 2, 3].map((i) => (
        <Card key={i} className="bg-card border-border/50">
          <CardContent className="p-4">
            <div className="flex items-center gap-3">
              <Skeleton className="w-9 h-9 rounded-lg" />
              <div className="flex-1 space-y-2">
                <Skeleton className="h-4 w-40" />
                <Skeleton className="h-3 w-24" />
              </div>
              <Skeleton className="h-6 w-20" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function EmptyState() {
  const { t } = useTranslation("assets");
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <div className="w-16 h-16 rounded-2xl bg-primary/10 flex items-center justify-center mb-4">
        <Package className="w-8 h-8 text-primary" />
      </div>
      <h3 className="text-lg font-semibold text-foreground mb-1">{t("empty.title")}</h3>
      <p className="text-sm text-muted-foreground mb-6 max-w-sm">
        {t("empty.description")}
      </p>
      <Button variant="gold">
        <Plus className="w-4 h-4" /> {t("empty.addFirst")}
      </Button>
    </div>
  );
}

export default function Assets() {
  const { t } = useTranslation("assets");
  const [activeFilter, setActiveFilter] = useState<AssetType | "All">("All");
  const [viewMode, setViewMode] = useState<"card" | "table">(() => {
    const stored = localStorage.getItem("assets-view-mode");
    return stored === "card" || stored === "table" ? stored : "card";
  });
  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingAsset, setEditingAsset] = useState<AssetDto | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [assetToDelete, setAssetToDelete] = useState<AssetDto | null>(null);
  const [beneficiaryAsset, setBeneficiaryAsset] = useState<AssetDto | null>(null);

  useEffect(() => {
    localStorage.setItem("assets-view-mode", viewMode);
  }, [viewMode]);

  const queryParams = activeFilter === "All" ? undefined : { type: activeFilter };
  const { data: assets, isLoading } = useAssets(queryParams);
  const createAsset = useCreateAsset();
  const updateAsset = useUpdateAsset();
  const deleteAsset = useDeleteAsset();

  const filteredAssets = assets ?? [];

  function handleOpenAdd() {
    setEditingAsset(null);
    setSheetOpen(true);
  }

  function handleOpenEdit(asset: AssetDto) {
    setEditingAsset(asset);
    setSheetOpen(true);
  }

  function handleDeleteClick(asset: AssetDto) {
    setAssetToDelete(asset);
    setDeleteDialogOpen(true);
  }

  function handleConfirmDelete() {
    if (!assetToDelete) return;

    deleteAsset.mutate(assetToDelete.id, {
      onSuccess: () => {
        toast.success(t("toast.deleted", { name: assetToDelete.name }));
        setDeleteDialogOpen(false);
        setAssetToDelete(null);
      },
      onError: () => {
        toast.error(t("toast.deleteFailed"));
      },
    });
  }

  function handleFormSubmit(values: any) {
    if (editingAsset) {
      updateAsset.mutate(
        { id: editingAsset.id, data: values },
        {
          onSuccess: () => {
            toast.success(t("toast.updated"));
            setSheetOpen(false);
          },
          onError: () => toast.error(t("toast.updateFailed")),
        }
      );
    } else {
      createAsset.mutate(values, {
        onSuccess: () => {
          toast.success(t("toast.added"));
          setSheetOpen(false);
        },
        onError: () => toast.error(t("toast.addFailed")),
      });
    }
  }

  return (
    <DashboardLayout>
      <Helmet>
        <title>{t("page.title")}</title>
      </Helmet>

      <div className="space-y-6">
        {!isLoading && filteredAssets.length > 0 && (
          <SummaryCards assets={assets ?? []} />
        )}

        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <h1 className="text-2xl font-bold text-foreground">{t("page.heading")}</h1>
          <div className="flex items-center gap-3">
            <div className="flex border border-border rounded-lg overflow-hidden" role="group" aria-label={t("page.viewModeLabel")}>
              <button
                onClick={() => setViewMode("card")}
                className={cn(
                  "p-2 transition-colors",
                  viewMode === "card"
                    ? "bg-primary text-primary-foreground"
                    : "bg-secondary text-muted-foreground hover:text-foreground"
                )}
                aria-label={t("page.cardView")}
                aria-pressed={viewMode === "card"}
              >
                <LayoutGrid className="w-4 h-4" />
              </button>
              <button
                onClick={() => setViewMode("table")}
                className={cn(
                  "p-2 transition-colors",
                  viewMode === "table"
                    ? "bg-primary text-primary-foreground"
                    : "bg-secondary text-muted-foreground hover:text-foreground"
                )}
                aria-label={t("page.tableView")}
                aria-pressed={viewMode === "table"}
              >
                <List className="w-4 h-4" />
              </button>
            </div>
            <Button variant="gold" className="sm:w-auto w-full" onClick={handleOpenAdd}>
              <Plus className="w-4 h-4" /> {t("page.addAsset")}
            </Button>
          </div>
        </div>

        <div className="flex flex-wrap gap-2" role="radiogroup" aria-label={t("page.filterLabel")}>
          {FILTER_VALUES.map((value) => (
            <button
              key={value}
              role="radio"
              aria-checked={activeFilter === value}
              onClick={() => setActiveFilter(value)}
              className={cn(
                "px-3 py-1.5 rounded-full text-sm font-medium transition-colors",
                activeFilter === value
                  ? "bg-primary text-primary-foreground"
                  : "bg-secondary text-muted-foreground hover:text-foreground"
              )}
            >
              {t(`filters.${value}`)}
            </button>
          ))}
        </div>

        {isLoading ? (
          <LoadingState />
        ) : filteredAssets.length === 0 ? (
          <EmptyState />
        ) : (
          viewMode === "table" ? (
            <AssetTable assets={filteredAssets} onEdit={handleOpenEdit} onDelete={handleDeleteClick} onManageBeneficiaries={setBeneficiaryAsset} />
          ) : (
            <CardGrid assets={filteredAssets} onEdit={handleOpenEdit} onDelete={handleDeleteClick} onManageBeneficiaries={setBeneficiaryAsset} />
          )
        )}
      </div>

      <AssetFormSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        asset={editingAsset}
        onSubmit={handleFormSubmit}
        isPending={createAsset.isPending || updateAsset.isPending}
      />

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{t("deleteDialog.title")}</AlertDialogTitle>
            <AlertDialogDescription>
              {t("deleteDialog.description", { name: assetToDelete?.name })}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>{t("deleteDialog.cancel")}</AlertDialogCancel>
            <AlertDialogAction asChild>
              <Button
                onClick={(e) => {
                  e.preventDefault();
                  handleConfirmDelete();
                }}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                disabled={deleteAsset.isPending}
              >
                {deleteAsset.isPending ? t("deleteDialog.deleting") : t("deleteDialog.confirm")}
              </Button>
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {beneficiaryAsset && (
        <BeneficiaryAssignmentDialog
          open={!!beneficiaryAsset}
          onOpenChange={(open) => { if (!open) setBeneficiaryAsset(null); }}
          asset={beneficiaryAsset}
        />
      )}
    </DashboardLayout>
  );
}
