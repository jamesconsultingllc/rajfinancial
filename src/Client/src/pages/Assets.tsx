import { useState, useEffect } from "react";
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
  Tooltip, TooltipContent, TooltipTrigger,
} from "@/components/ui/tooltip";
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
  Pencil, DollarSign, Archive, Users, Trash2, CheckCircle2,
  LayoutGrid, List,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useAssets, useCreateAsset, useUpdateAsset, useDeleteAsset } from "@/services/asset-service";
import { computeAssetsSummary } from "@/data/mock-assets";
import {
  type AssetDto, type AssetType, ASSET_TYPE_LABELS, ASSET_TYPE_ICONS, formatCurrency,
} from "@/types/assets";
import { AssetFormSheet } from "@/components/assets/AssetFormSheet";
import { toast } from "sonner";

const FILTER_TABS: { label: string; value: AssetType | "All" }[] = [
  { label: "All", value: "All" },
  { label: "Real Estate", value: "RealEstate" },
  { label: "Vehicles", value: "Vehicle" },
  { label: "Investments", value: "Investment" },
  { label: "Retirement", value: "Retirement" },
  { label: "Bank Accounts", value: "BankAccount" },
  { label: "Business", value: "Business" },
  { label: "Other", value: "Other" },
];

function SummaryCards({ assets }: { assets: AssetDto[] }) {
  const summary = computeAssetsSummary(assets);
  const cards = [
    { label: "Total Assets Value", value: formatCurrency(summary.totalValue), accent: true },
    { label: "Number of Assets", value: String(summary.count) },
    { label: "Top Category", value: summary.topCategory },
    {
      label: "Needs Attention",
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

function ActionsMenu({ asset, onEdit, onDelete }: { asset: AssetDto; onEdit: (asset: AssetDto) => void; onDelete: (asset: AssetDto) => void }) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-8 w-8">
          <MoreHorizontal className="w-4 h-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem onClick={() => onEdit(asset)}>
          <Pencil className="w-4 h-4 mr-2" /> Edit
        </DropdownMenuItem>
        <DropdownMenuItem>
          <DollarSign className="w-4 h-4 mr-2" /> Update Value
        </DropdownMenuItem>
        <Tooltip>
          <TooltipTrigger asChild>
            <DropdownMenuItem disabled className="opacity-50">
              <Archive className="w-4 h-4 mr-2" /> Mark as Disposed
            </DropdownMenuItem>
          </TooltipTrigger>
          <TooltipContent side="left">Coming soon</TooltipContent>
        </Tooltip>
        <DropdownMenuItem>
          <Users className="w-4 h-4 mr-2" /> Manage Beneficiaries
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          className="text-destructive focus:text-destructive"
          onClick={() => onDelete(asset)}
        >
          <Trash2 className="w-4 h-4 mr-2" /> Delete
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function BeneficiaryStatus({ has }: { has: boolean }) {
  if (has) {
    return (
      <span className="inline-flex items-center gap-1 text-sm text-[hsl(var(--success))]">
        <CheckCircle2 className="w-4 h-4" /> Assigned
      </span>
    );
  }
  return <Badge variant="secondary" className="bg-primary/10 text-primary border-0">None</Badge>;
}

function AssetTable({ assets, onEdit, onDelete }: { assets: AssetDto[]; onEdit: (asset: AssetDto) => void; onDelete: (asset: AssetDto) => void }) {
  return (
    <div>
      <Card className="bg-card border-border/50">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead className="text-right">Current Value</TableHead>
              <TableHead className="text-right">Purchase Price</TableHead>
              <TableHead>Beneficiaries</TableHead>
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
                  <ActionsMenu asset={asset} onEdit={onEdit} onDelete={onDelete} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>
    </div>
  );
}

function CardGrid({ assets, onEdit, onDelete }: { assets: AssetDto[]; onEdit: (asset: AssetDto) => void; onDelete: (asset: AssetDto) => void }) {
  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      {assets.map((asset) => (
        <Card key={asset.id} className="bg-card border-border/50 cursor-pointer hover:border-primary/30 transition-colors" onClick={() => onEdit(asset)}>
          <CardContent className="p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-3 min-w-0">
                <AssetIcon type={asset.type} />
                <div className="min-w-0">
                  <p className="font-medium text-foreground truncate">{asset.name}</p>
                  <p className="text-xs text-muted-foreground">{ASSET_TYPE_LABELS[asset.type]}</p>
                </div>
              </div>
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 text-muted-foreground hover:text-destructive shrink-0"
                aria-label={`Delete ${asset.name}`}
                onClick={(e) => {
                  e.stopPropagation();
                  onDelete(asset);
                }}
              >
                <Trash2 className="w-4 h-4" />
              </Button>
            </div>
            <div className="flex items-center justify-between mt-3">
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
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <div className="w-16 h-16 rounded-2xl bg-primary/10 flex items-center justify-center mb-4">
        <Package className="w-8 h-8 text-primary" />
      </div>
      <h3 className="text-lg font-semibold text-foreground mb-1">No assets yet</h3>
      <p className="text-sm text-muted-foreground mb-6 max-w-sm">
        Start building your financial picture by adding your first asset — property, accounts, investments, and more.
      </p>
      <Button variant="gold">
        <Plus className="w-4 h-4" /> Add Your First Asset
      </Button>
    </div>
  );
}

export default function Assets() {
  const [activeFilter, setActiveFilter] = useState<AssetType | "All">("All");
  const [viewMode, setViewMode] = useState<"card" | "table">(() => {
    const stored = localStorage.getItem("assets-view-mode");
    return stored === "card" || stored === "table" ? stored : "card";
  });
  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingAsset, setEditingAsset] = useState<AssetDto | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [assetToDelete, setAssetToDelete] = useState<AssetDto | null>(null);

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
        toast.success(`"${assetToDelete.name}" has been deleted`);
        setDeleteDialogOpen(false);
        setAssetToDelete(null);
      },
      onError: () => {
        toast.error("Failed to delete asset. Please try again.");
      },
    });
  }

  function handleFormSubmit(values: any) {
    if (editingAsset) {
      updateAsset.mutate(
        { id: editingAsset.id, data: values },
        {
          onSuccess: () => {
            toast.success("Asset updated successfully");
            setSheetOpen(false);
          },
          onError: () => toast.error("Failed to update asset"),
        }
      );
    } else {
      createAsset.mutate(values, {
        onSuccess: () => {
          toast.success("Asset added successfully");
          setSheetOpen(false);
        },
        onError: () => toast.error("Failed to add asset"),
      });
    }
  }

  return (
    <DashboardLayout>
      <Helmet>
        <title>Assets | RAJ Financial</title>
      </Helmet>

      <div className="space-y-6">
        {!isLoading && filteredAssets.length > 0 && (
          <SummaryCards assets={assets ?? []} />
        )}

        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <h1 className="text-2xl font-bold text-foreground">Assets</h1>
          <div className="flex items-center gap-3">
            <div className="flex border border-border rounded-lg overflow-hidden" role="group" aria-label="View mode">
              <button
                onClick={() => setViewMode("card")}
                className={cn(
                  "p-2 transition-colors",
                  viewMode === "card"
                    ? "bg-primary text-primary-foreground"
                    : "bg-secondary text-muted-foreground hover:text-foreground"
                )}
                aria-label="Card view"
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
                aria-label="Table view"
                aria-pressed={viewMode === "table"}
              >
                <List className="w-4 h-4" />
              </button>
            </div>
            <Button variant="gold" className="sm:w-auto w-full" onClick={handleOpenAdd}>
              <Plus className="w-4 h-4" /> Add Asset
            </Button>
          </div>
        </div>

        <div className="flex flex-wrap gap-2" role="tablist" aria-label="Filter by asset type">
          {FILTER_TABS.map((tab) => (
            <button
              key={tab.value}
              role="tab"
              aria-selected={activeFilter === tab.value}
              onClick={() => setActiveFilter(tab.value)}
              className={cn(
                "px-3 py-1.5 rounded-full text-sm font-medium transition-colors",
                activeFilter === tab.value
                  ? "bg-primary text-primary-foreground"
                  : "bg-secondary text-muted-foreground hover:text-foreground"
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        {isLoading ? (
          <LoadingState />
        ) : filteredAssets.length === 0 ? (
          <EmptyState />
        ) : (
          viewMode === "table" ? (
            <AssetTable assets={filteredAssets} onEdit={handleOpenEdit} onDelete={handleDeleteClick} />
          ) : (
            <CardGrid assets={filteredAssets} onEdit={handleOpenEdit} onDelete={handleDeleteClick} />
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
            <AlertDialogTitle>Delete Asset</AlertDialogTitle>
            <AlertDialogDescription>
              Are you sure you want to delete <strong>{assetToDelete?.name}</strong>?
              This action cannot be undone. All associated beneficiary assignments will also be removed.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction asChild>
              <Button
                onClick={(e) => {
                  e.preventDefault();
                  handleConfirmDelete();
                }}
                className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                disabled={deleteAsset.isPending}
              >
                {deleteAsset.isPending ? "Deleting..." : "Delete"}
              </Button>
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </DashboardLayout>
  );
}
