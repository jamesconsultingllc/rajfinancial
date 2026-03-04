/**
 * Dialog for managing beneficiary/contact assignments on an asset.
 *
 * Shows existing assignments with allocation totals and validation,
 * and provides a form to add new assignments from the contacts list.
 */
import { useState } from "react";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Badge } from "@/components/ui/badge";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import {
  Table, TableHeader, TableHead, TableBody, TableRow, TableCell,
} from "@/components/ui/table";
import { Separator } from "@/components/ui/separator";
import { AlertTriangle, Plus, Trash2, Pencil, Users } from "lucide-react";
import { toast } from "sonner";
import { cn } from "@/lib/utils";
import { useContacts } from "@/services/contact-service";
import {
  useAssetLinks, useCreateAssetLink, useUpdateAssetLink, useDeleteAssetLink,
} from "@/services/beneficiary-service";
import type { AssetDto } from "@/types/assets";
import type { AssetContactRole, DesignationType } from "@/types/contacts";
import { ASSET_CONTACT_ROLE_LABELS, DESIGNATION_LABELS } from "@/types/contacts";

interface BeneficiaryAssignmentDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  asset: AssetDto;
}

const ROLES: AssetContactRole[] = ["Beneficiary", "CoOwner", "Trustee", "Custodian", "PowerOfAttorney", "Other"];
const DESIGNATIONS: DesignationType[] = ["Primary", "Contingent"];

export function BeneficiaryAssignmentDialog({
  open, onOpenChange, asset,
}: BeneficiaryAssignmentDialogProps) {
  const { data: contacts = [] } = useContacts();
  const { data: links = [], isLoading } = useAssetLinks(asset.id);
  const createLink = useCreateAssetLink();
  const updateLink = useUpdateAssetLink();
  const deleteLink = useDeleteAssetLink();

  // Add form state
  const [showAddForm, setShowAddForm] = useState(false);
  const [selectedContactId, setSelectedContactId] = useState("");
  const [role, setRole] = useState<AssetContactRole>("Beneficiary");
  const [designation, setDesignation] = useState<DesignationType>("Primary");
  const [allocation, setAllocation] = useState("");
  const [perStirpes, setPerStirpes] = useState(false);

  // Edit state
  const [editingLinkId, setEditingLinkId] = useState<string | null>(null);
  const [editRole, setEditRole] = useState<AssetContactRole>("Beneficiary");
  const [editDesignation, setEditDesignation] = useState<DesignationType>("Primary");
  const [editAllocation, setEditAllocation] = useState("");
  const [editPerStirpes, setEditPerStirpes] = useState(false);

  // Compute allocation totals
  const primaryTotal = links
    .filter((l) => l.role === "Beneficiary" && l.designation === "Primary")
    .reduce((sum, l) => sum + (l.allocationPercent ?? 0), 0);
  const contingentTotal = links
    .filter((l) => l.role === "Beneficiary" && l.designation === "Contingent")
    .reduce((sum, l) => sum + (l.allocationPercent ?? 0), 0);

  // Contacts not already assigned
  const assignedContactIds = new Set(links.map((l) => l.contactId));
  const availableContacts = contacts.filter((c) => !assignedContactIds.has(c.id));

  function resetAddForm() {
    setSelectedContactId("");
    setRole("Beneficiary");
    setDesignation("Primary");
    setAllocation("");
    setPerStirpes(false);
    setShowAddForm(false);
  }

  function handleAdd() {
    const contact = contacts.find((c) => c.id === selectedContactId);
    if (!contact) return;

    createLink.mutate(
      {
        assetId: asset.id,
        assetName: asset.name,
        contactId: contact.id,
        contactDisplayName: contact.displayName,
        data: {
          contactId: contact.id,
          role,
          designation: role === "Beneficiary" ? designation : undefined,
          allocationPercent: role === "Beneficiary" ? Number(allocation) || 0 : undefined,
          perStirpes: role === "Beneficiary" ? perStirpes : false,
        },
      },
      {
        onSuccess: () => {
          toast.success(`${contact.displayName} assigned`);
          resetAddForm();
        },
        onError: () => toast.error("Failed to add assignment"),
      }
    );
  }

  function handleStartEdit(link: typeof links[0]) {
    setEditingLinkId(link.id);
    setEditRole(link.role);
    setEditDesignation(link.designation ?? "Primary");
    setEditAllocation(String(link.allocationPercent ?? ""));
    setEditPerStirpes(link.perStirpes);
  }

  function handleSaveEdit(linkId: string) {
    updateLink.mutate(
      {
        linkId,
        assetId: asset.id,
        data: {
          role: editRole,
          designation: editRole === "Beneficiary" ? editDesignation : undefined,
          allocationPercent: editRole === "Beneficiary" ? Number(editAllocation) || 0 : undefined,
          perStirpes: editRole === "Beneficiary" ? editPerStirpes : false,
        },
      },
      {
        onSuccess: () => {
          toast.success("Assignment updated");
          setEditingLinkId(null);
        },
        onError: () => toast.error("Failed to update"),
      }
    );
  }

  function handleDelete(linkId: string, name: string) {
    deleteLink.mutate(
      { linkId, assetId: asset.id },
      {
        onSuccess: () => toast.success(`${name} removed`),
        onError: () => toast.error("Failed to remove"),
      }
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Users className="w-5 h-5" /> Manage Beneficiaries
          </DialogTitle>
          <DialogDescription>
            Assign contacts to <strong>{asset.name}</strong> with roles and allocation percentages.
          </DialogDescription>
        </DialogHeader>

        {/* Allocation summary */}
        <div className="flex flex-wrap gap-3">
          <AllocationBadge label="Primary" total={primaryTotal} />
          <AllocationBadge label="Contingent" total={contingentTotal} />
        </div>

        <Separator />

        {/* Existing assignments */}
        {isLoading ? (
          <p className="text-sm text-muted-foreground py-4 text-center">Loading…</p>
        ) : links.length === 0 ? (
          <p className="text-sm text-muted-foreground py-4 text-center">
            No beneficiaries assigned yet.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Contact</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Designation</TableHead>
                  <TableHead className="text-right">Allocation</TableHead>
                  <TableHead className="text-center">Per Stirpes</TableHead>
                  <TableHead className="w-20" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {links.map((link) =>
                  editingLinkId === link.id ? (
                    <TableRow key={link.id}>
                      <TableCell className="font-medium">{link.contactDisplayName}</TableCell>
                      <TableCell>
                        <Select value={editRole} onValueChange={(v) => setEditRole(v as AssetContactRole)}>
                          <SelectTrigger className="h-8 w-28"><SelectValue /></SelectTrigger>
                          <SelectContent>
                            {ROLES.map((r) => (
                              <SelectItem key={r} value={r}>{ASSET_CONTACT_ROLE_LABELS[r]}</SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                      </TableCell>
                      <TableCell>
                        {editRole === "Beneficiary" && (
                          <Select value={editDesignation} onValueChange={(v) => setEditDesignation(v as DesignationType)}>
                            <SelectTrigger className="h-8 w-28"><SelectValue /></SelectTrigger>
                            <SelectContent>
                              {DESIGNATIONS.map((d) => (
                                <SelectItem key={d} value={d}>{DESIGNATION_LABELS[d]}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      </TableCell>
                      <TableCell>
                        {editRole === "Beneficiary" && (
                          <Input
                            type="number" min={0} max={100}
                            value={editAllocation}
                            onChange={(e) => setEditAllocation(e.target.value)}
                            className="h-8 w-20 text-right"
                          />
                        )}
                      </TableCell>
                      <TableCell className="text-center">
                        {editRole === "Beneficiary" && (
                          <Checkbox checked={editPerStirpes} onCheckedChange={(v) => setEditPerStirpes(!!v)} />
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button variant="ghost" size="sm" onClick={() => handleSaveEdit(link.id)}
                            disabled={updateLink.isPending}>Save</Button>
                          <Button variant="ghost" size="sm" onClick={() => setEditingLinkId(null)}>Cancel</Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ) : (
                    <TableRow key={link.id}>
                      <TableCell className="font-medium">{link.contactDisplayName}</TableCell>
                      <TableCell>{ASSET_CONTACT_ROLE_LABELS[link.role]}</TableCell>
                      <TableCell>
                        {link.designation ? (
                          <Badge variant={link.designation === "Primary" ? "default" : "secondary"}>
                            {DESIGNATION_LABELS[link.designation]}
                          </Badge>
                        ) : "—"}
                      </TableCell>
                      <TableCell className="text-right">
                        {link.allocationPercent != null ? `${link.allocationPercent}%` : "—"}
                      </TableCell>
                      <TableCell className="text-center">
                        {link.perStirpes ? "Yes" : "—"}
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-1">
                          <Button variant="ghost" size="icon" className="h-7 w-7"
                            onClick={() => handleStartEdit(link)}>
                            <Pencil className="w-3.5 h-3.5" />
                          </Button>
                          <Button variant="ghost" size="icon"
                            className="h-7 w-7 text-destructive hover:text-destructive"
                            onClick={() => handleDelete(link.id, link.contactDisplayName)}>
                            <Trash2 className="w-3.5 h-3.5" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                )}
              </TableBody>
            </Table>
          </div>
        )}

        <Separator />

        {/* Add new assignment */}
        {showAddForm ? (
          <div className="space-y-4 p-4 rounded-lg bg-secondary/30 border border-border/50">
            <h4 className="text-sm font-semibold text-foreground">Add Assignment</h4>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-1.5">
                <Label>Contact</Label>
                <Select value={selectedContactId} onValueChange={setSelectedContactId}>
                  <SelectTrigger><SelectValue placeholder="Select contact…" /></SelectTrigger>
                  <SelectContent>
                    {availableContacts.length === 0 ? (
                      <SelectItem value="__none" disabled>No available contacts</SelectItem>
                    ) : (
                      availableContacts.map((c) => (
                        <SelectItem key={c.id} value={c.id}>{c.displayName}</SelectItem>
                      ))
                    )}
                  </SelectContent>
                </Select>
              </div>
              <div className="space-y-1.5">
                <Label>Role</Label>
                <Select value={role} onValueChange={(v) => setRole(v as AssetContactRole)}>
                  <SelectTrigger><SelectValue /></SelectTrigger>
                  <SelectContent>
                    {ROLES.map((r) => (
                      <SelectItem key={r} value={r}>{ASSET_CONTACT_ROLE_LABELS[r]}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              {role === "Beneficiary" && (
                <>
                  <div className="space-y-1.5">
                    <Label>Designation</Label>
                    <Select value={designation} onValueChange={(v) => setDesignation(v as DesignationType)}>
                      <SelectTrigger><SelectValue /></SelectTrigger>
                      <SelectContent>
                        {DESIGNATIONS.map((d) => (
                          <SelectItem key={d} value={d}>{DESIGNATION_LABELS[d]}</SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="space-y-1.5">
                    <Label>Allocation %</Label>
                    <Input
                      type="number" min={0} max={100}
                      placeholder="e.g. 50"
                      value={allocation}
                      onChange={(e) => setAllocation(e.target.value)}
                    />
                  </div>
                  <div className="flex items-center gap-2 sm:col-span-2">
                    <Checkbox
                      id="per-stirpes"
                      checked={perStirpes}
                      onCheckedChange={(v) => setPerStirpes(!!v)}
                    />
                    <Label htmlFor="per-stirpes" className="text-sm cursor-pointer">
                      Per Stirpes — share passes to descendants if beneficiary predeceases
                    </Label>
                  </div>
                </>
              )}
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="outline" size="sm" onClick={resetAddForm}>Cancel</Button>
              <Button variant="gold" size="sm" onClick={handleAdd}
                disabled={!selectedContactId || createLink.isPending}>
                {createLink.isPending ? "Adding…" : "Add"}
              </Button>
            </div>
          </div>
        ) : (
          <Button variant="outline" className="w-full" onClick={() => setShowAddForm(true)}>
            <Plus className="w-4 h-4" /> Add Assignment
          </Button>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Close</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function AllocationBadge({ label, total }: { label: string; total: number }) {
  const isValid = total === 100;
  const isZero = total === 0;
  return (
    <div className={cn(
      "flex items-center gap-2 px-3 py-1.5 rounded-lg border text-sm",
      isValid
        ? "border-[hsl(var(--success))]/30 bg-[hsl(var(--success))]/10 text-[hsl(var(--success))]"
        : isZero
          ? "border-border bg-secondary text-muted-foreground"
          : "border-destructive/30 bg-destructive/10 text-destructive"
    )}>
      {!isValid && !isZero && <AlertTriangle className="w-3.5 h-3.5" />}
      <span className="font-medium">{label}:</span>
      <span>{total}%</span>
      {isValid && <span className="text-xs">✓</span>}
    </div>
  );
}
