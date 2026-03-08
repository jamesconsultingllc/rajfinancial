/**
 * Contacts & Beneficiaries list page.
 *
 * @description Displays all contacts (Individual, Trust, Organization)
 * with summary cards, type filter tabs, desktop table, mobile cards,
 * and full CRUD via ContactFormSheet. Follows the same layout pattern
 * as the Assets page.
 *
 * "Beneficiary" is a role a contact plays when linked to an asset —
 * it is not a separate entity type. Every contact can be linked to
 * one or more assets with a specific role, designation, and allocation.
 */
import { useState } from "react";
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
  DropdownMenu, DropdownMenuContent, DropdownMenuItem,
  DropdownMenuTrigger, DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription,
} from "@/components/ui/dialog";
import {
  Plus, MoreHorizontal, ChevronRight, Users, User, ShieldCheck,
  Building2, Pencil, Trash2, Link2, UserPlus,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  useContacts, useCreateContact, useUpdateContact, useDeleteContact,
  useContactLinks,
} from "@/services/contact-service";
import type {
  ContactDto, ContactType, AssetContactLinkDto,
} from "@/types/contacts";
import {
  CONTACT_TYPE_LABELS, RELATIONSHIP_LABELS, TRUST_CATEGORY_LABELS,
  ORGANIZATION_TYPE_LABELS, ASSET_CONTACT_ROLE_LABELS,
  DESIGNATION_LABELS,
} from "@/types/contacts";
import { ContactFormSheet } from "@/components/contacts/ContactFormSheet";
import { toast } from "sonner";

/* ------------------------------------------------------------------ */
/*  Filter tabs                                                        */
/* ------------------------------------------------------------------ */

const FILTER_TABS: { label: string; value: ContactType | "All" }[] = [
  { label: "All", value: "All" },
  { label: "Individuals", value: "Individual" },
  { label: "Trusts", value: "Trust" },
  { label: "Organizations", value: "Organization" },
];

/* ------------------------------------------------------------------ */
/*  Contact type icon                                                  */
/* ------------------------------------------------------------------ */

/** Maps contact type to an icon with a tinted background. */
function ContactIcon({ type }: { type: ContactType }) {
  const config: Record<ContactType, { icon: typeof User; bg: string; fg: string }> = {
    Individual: { icon: User, bg: "bg-blue-500/10", fg: "text-blue-500" },
    Trust: { icon: ShieldCheck, bg: "bg-amber-500/10", fg: "text-amber-500" },
    Organization: { icon: Building2, bg: "bg-emerald-500/10", fg: "text-emerald-500" },
  };
  const { icon: Icon, bg, fg } = config[type];
  return (
    <div className={cn("w-9 h-9 rounded-lg flex items-center justify-center shrink-0", bg)}>
      <Icon className={cn("w-5 h-5", fg)} />
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Summary cards                                                      */
/* ------------------------------------------------------------------ */

/** Displays four summary cards — total contacts + count per type. */
function SummaryCards({ contacts }: { contacts: ContactDto[] }) {
  const individuals = contacts.filter((c) => c.contactType === "Individual").length;
  const trusts = contacts.filter((c) => c.contactType === "Trust").length;
  const orgs = contacts.filter((c) => c.contactType === "Organization").length;

  const cards = [
    { label: "Total Contacts", value: contacts.length, icon: Users },
    { label: "Individuals", value: individuals, icon: User },
    { label: "Trusts", value: trusts, icon: ShieldCheck },
    { label: "Organizations", value: orgs, icon: Building2 },
  ];

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
      {cards.map((c) => (
        <Card key={c.label} className="bg-card border-border/50">
          <CardContent className="p-4 flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-primary/10 flex items-center justify-center shrink-0">
              <c.icon className="w-5 h-5 text-primary" />
            </div>
            <div>
              <p className="text-xs text-muted-foreground">{c.label}</p>
              <p className="text-lg font-bold text-foreground">{c.value}</p>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Subtitle helper — shows relationship / category / org type         */
/* ------------------------------------------------------------------ */

/** Returns a human-friendly subtitle based on contact type. */
function contactSubtitle(contact: ContactDto): string {
  switch (contact.contactType) {
    case "Individual":
      return contact.relationship ? RELATIONSHIP_LABELS[contact.relationship] : "Individual";
    case "Trust":
      return contact.category ? TRUST_CATEGORY_LABELS[contact.category] : "Trust";
    case "Organization":
      return contact.organizationType
        ? ORGANIZATION_TYPE_LABELS[contact.organizationType]
        : "Organization";
  }
}

/* ------------------------------------------------------------------ */
/*  Linked assets badge                                                */
/* ------------------------------------------------------------------ */

/** Shows a green badge with link count or a muted "None" badge. */
function LinkedAssetsBadge({ count }: { count: number }) {
  if (count > 0) {
    return (
      <Badge variant="secondary" className="bg-emerald-500/10 text-emerald-600 border-0 text-xs">
        <Link2 className="w-3 h-3 mr-1" />
        {count} linked
      </Badge>
    );
  }
  return (
    <Badge variant="secondary" className="bg-secondary text-muted-foreground border-0 text-xs">
      None
    </Badge>
  );
}

/* ------------------------------------------------------------------ */
/*  Actions menu                                                       */
/* ------------------------------------------------------------------ */

/** Per-row dropdown with Edit, View Links, Delete. */
function ActionsMenu({
  contact,
  onEdit,
  onViewLinks,
  onDelete,
}: {
  contact: ContactDto;
  onEdit: (c: ContactDto) => void;
  onViewLinks: (c: ContactDto) => void;
  onDelete: (c: ContactDto) => void;
}) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="icon" className="h-8 w-8" aria-label="Contact actions">
          <MoreHorizontal className="w-4 h-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-44">
        <DropdownMenuItem onClick={() => onEdit(contact)}>
          <Pencil className="w-4 h-4 mr-2" /> Edit
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => onViewLinks(contact)}>
          <Link2 className="w-4 h-4 mr-2" /> View Links
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={() => onDelete(contact)}
          className="text-destructive focus:text-destructive"
        >
          <Trash2 className="w-4 h-4 mr-2" /> Delete
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/* ------------------------------------------------------------------ */
/*  Desktop table                                                      */
/* ------------------------------------------------------------------ */

/** Full table visible on lg+ screens. */
function DesktopTable({
  contacts,
  onEdit,
  onViewLinks,
  onDelete,
}: {
  contacts: ContactDto[];
  onEdit: (c: ContactDto) => void;
  onViewLinks: (c: ContactDto) => void;
  onDelete: (c: ContactDto) => void;
}) {
  return (
    <div className="hidden lg:block">
      <Card className="bg-card border-border/50 overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent border-border/50">
              <TableHead className="text-muted-foreground">Name</TableHead>
              <TableHead className="text-muted-foreground">Type</TableHead>
              <TableHead className="text-muted-foreground">Email</TableHead>
              <TableHead className="text-muted-foreground">Phone</TableHead>
              <TableHead className="text-muted-foreground">Linked Assets</TableHead>
              <TableHead className="w-12" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {contacts.map((contact) => (
              <TableRow
                key={contact.id}
                className="border-border/50 cursor-pointer hover:bg-muted/50"
                onClick={() => onEdit(contact)}
              >
                <TableCell>
                  <div className="flex items-center gap-3">
                    <ContactIcon type={contact.contactType} />
                    <div>
                      <p className="font-medium text-foreground">{contact.displayName}</p>
                      <p className="text-xs text-muted-foreground">{contactSubtitle(contact)}</p>
                    </div>
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant="outline" className="text-xs">
                    {CONTACT_TYPE_LABELS[contact.contactType]}
                  </Badge>
                </TableCell>
                <TableCell className="text-muted-foreground text-sm">
                  {contact.email ?? "—"}
                </TableCell>
                <TableCell className="text-muted-foreground text-sm">
                  {contact.phone ?? "—"}
                </TableCell>
                <TableCell>
                  <LinkedAssetsBadge count={contact.assetLinkCount} />
                </TableCell>
                <TableCell onClick={(e) => e.stopPropagation()}>
                  <ActionsMenu
                    contact={contact}
                    onEdit={onEdit}
                    onViewLinks={onViewLinks}
                    onDelete={onDelete}
                  />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Card>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Mobile cards                                                       */
/* ------------------------------------------------------------------ */

/** Card layout for small screens. */
function MobileCards({
  contacts,
  onEdit,
}: {
  contacts: ContactDto[];
  onEdit: (c: ContactDto) => void;
}) {
  return (
    <div className="lg:hidden space-y-3">
      {contacts.map((contact) => (
        <Card
          key={contact.id}
          className="bg-card border-border/50 cursor-pointer hover:border-primary/30 transition-colors"
          onClick={() => onEdit(contact)}
        >
          <CardContent className="p-4">
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-3 min-w-0">
                <ContactIcon type={contact.contactType} />
                <div className="min-w-0">
                  <p className="font-medium text-foreground truncate">{contact.displayName}</p>
                  <p className="text-xs text-muted-foreground">{contactSubtitle(contact)}</p>
                </div>
              </div>
              <LinkedAssetsBadge count={contact.assetLinkCount} />
            </div>
            <div className="flex items-center justify-between mt-3">
              <p className="text-sm text-muted-foreground truncate">
                {contact.email ?? contact.phone ?? "No contact info"}
              </p>
              <ChevronRight className="w-5 h-5 text-muted-foreground shrink-0" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Loading skeleton                                                   */
/* ------------------------------------------------------------------ */

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

/* ------------------------------------------------------------------ */
/*  Empty state                                                        */
/* ------------------------------------------------------------------ */

function EmptyState() {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-center">
      <div className="w-16 h-16 rounded-2xl bg-primary/10 flex items-center justify-center mb-4">
        <UserPlus className="w-8 h-8 text-primary" />
      </div>
      <h3 className="text-lg font-semibold text-foreground mb-1">No contacts yet</h3>
      <p className="text-sm text-muted-foreground mb-6 max-w-sm">
        Add individuals, trusts, or organizations that will be linked to your assets
        as beneficiaries, co-owners, trustees, and more.
      </p>
      <Button variant="gold">
        <Plus className="w-4 h-4" /> Add Your First Contact
      </Button>
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Linked assets dialog                                               */
/* ------------------------------------------------------------------ */

/**
 * Dialog showing all asset-contact links for a contact.
 *
 * @description Displays asset name, role, designation, and allocation
 * for every link. Read-only — link CRUD will live on the Asset detail
 * page in a future iteration.
 */
function LinkedAssetsDialog({
  contact,
  open,
  onOpenChange,
}: {
  contact: ContactDto | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const { data: links, isLoading } = useContactLinks(contact?.id ?? "");

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Linked Assets</DialogTitle>
          <DialogDescription>
            Assets linked to <span className="font-medium">{contact?.displayName}</span>
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="space-y-3 py-4">
            {[1, 2].map((i) => (
              <Skeleton key={i} className="h-16 w-full rounded-lg" />
            ))}
          </div>
        ) : !links || links.length === 0 ? (
          <p className="text-sm text-muted-foreground py-6 text-center">
            No assets linked to this contact.
          </p>
        ) : (
          <div className="space-y-3 py-2 max-h-80 overflow-y-auto">
            {links.map((link) => (
              <LinkCard key={link.id} link={link} />
            ))}
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}

/** Single asset link card inside the dialog. */
function LinkCard({ link }: { link: AssetContactLinkDto }) {
  return (
    <Card className="bg-muted/50 border-border/50">
      <CardContent className="p-3 space-y-1.5">
        <div className="flex items-center justify-between">
          <p className="font-medium text-sm text-foreground">{link.assetName}</p>
          <Badge variant="outline" className="text-xs">
            {ASSET_CONTACT_ROLE_LABELS[link.role]}
          </Badge>
        </div>
        <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
          {link.designation && (
            <span>Designation: {DESIGNATION_LABELS[link.designation]}</span>
          )}
          {link.allocationPercent != null && (
            <span>Allocation: {link.allocationPercent}%</span>
          )}
          {link.perStirpes && <span className="text-amber-600">Per Stirpes</span>}
        </div>
      </CardContent>
    </Card>
  );
}

/* ------------------------------------------------------------------ */
/*  Main page component                                                */
/* ------------------------------------------------------------------ */

/**
 * Contacts & Beneficiaries page.
 *
 * @description Main list page with filter tabs, summary cards,
 * responsive table/cards, CRUD sheet, and linked-assets dialog.
 * Accessible via /contacts with RequireClient policy.
 */
export default function Contacts() {
  const [activeFilter, setActiveFilter] = useState<ContactType | "All">("All");
  const [sheetOpen, setSheetOpen] = useState(false);
  const [editingContact, setEditingContact] = useState<ContactDto | null>(null);
  const [linksDialogContact, setLinksDialogContact] = useState<ContactDto | null>(null);
  const [linksDialogOpen, setLinksDialogOpen] = useState(false);

  const queryParams = activeFilter === "All" ? undefined : { type: activeFilter };
  const { data: contacts, isLoading } = useContacts(queryParams);
  const createContact = useCreateContact();
  const updateContact = useUpdateContact();
  const deleteContact = useDeleteContact();

  const filteredContacts = contacts ?? [];

  /** Opens the form sheet in "add" mode. */
  function handleOpenAdd() {
    setEditingContact(null);
    setSheetOpen(true);
  }

  /** Opens the form sheet in "edit" mode for a specific contact. */
  function handleOpenEdit(contact: ContactDto) {
    setEditingContact(contact);
    setSheetOpen(true);
  }

  /** Opens the linked-assets dialog for a contact. */
  function handleViewLinks(contact: ContactDto) {
    setLinksDialogContact(contact);
    setLinksDialogOpen(true);
  }

  /**
   * Deletes a contact. Catches CONTACT_HAS_LINKS to show a
   * user-friendly error toast instead of a raw error.
   */
  function handleDelete(contact: ContactDto) {
    if (contact.assetLinkCount > 0) {
      toast.error("Cannot delete contact", {
        description: `${contact.displayName} is linked to ${contact.assetLinkCount} asset(s). Remove all links before deleting.`,
      });
      return;
    }

    deleteContact.mutate(contact.id, {
      onSuccess: () => toast.success(`${contact.displayName} deleted`),
      onError: (err) => {
        if (err.message === "CONTACT_HAS_LINKS") {
          toast.error("Cannot delete contact", {
            description: "This contact is linked to one or more assets. Remove all links first.",
          });
        } else {
          toast.error("Failed to delete contact");
        }
      },
    });
  }

  /** Handles form submission for both create and update. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  function handleFormSubmit(values: any) {
    if (editingContact) {
      updateContact.mutate(
        { id: editingContact.id, data: values },
        {
          onSuccess: () => {
            toast.success("Contact updated successfully");
            setSheetOpen(false);
          },
          onError: () => toast.error("Failed to update contact"),
        }
      );
    } else {
      createContact.mutate(values, {
        onSuccess: () => {
          toast.success("Contact added successfully");
          setSheetOpen(false);
        },
        onError: () => toast.error("Failed to add contact"),
      });
    }
  }

  return (
    <DashboardLayout>
      <Helmet>
        <title>Contacts & Beneficiaries | RAJ Financial</title>
      </Helmet>

      <div className="space-y-6">
        {/* Summary cards — only when data is loaded and non-empty */}
        {!isLoading && filteredContacts.length > 0 && (
          <SummaryCards contacts={contacts ?? []} />
        )}

        {/* Header with title and add button */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <h1 className="text-2xl font-bold text-foreground">Contacts & Beneficiaries</h1>
          <Button variant="gold" className="sm:w-auto w-full" onClick={handleOpenAdd}>
            <Plus className="w-4 h-4" /> Add Contact
          </Button>
        </div>

        {/* Filter tabs */}
        <div className="flex flex-wrap gap-2">
          {FILTER_TABS.map((tab) => (
            <button
              key={tab.value}
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

        {/* Content area */}
        {isLoading ? (
          <LoadingState />
        ) : filteredContacts.length === 0 ? (
          <EmptyState />
        ) : (
          <>
            <DesktopTable
              contacts={filteredContacts}
              onEdit={handleOpenEdit}
              onViewLinks={handleViewLinks}
              onDelete={handleDelete}
            />
            <MobileCards
              contacts={filteredContacts}
              onEdit={handleOpenEdit}
            />
          </>
        )}
      </div>

      {/* Form sheet — add / edit contact */}
      <ContactFormSheet
        open={sheetOpen}
        onOpenChange={setSheetOpen}
        contact={editingContact}
        onSubmit={handleFormSubmit}
        isPending={createContact.isPending || updateContact.isPending}
      />

      {/* Linked assets dialog */}
      <LinkedAssetsDialog
        contact={linksDialogContact}
        open={linksDialogOpen}
        onOpenChange={setLinksDialogOpen}
      />
    </DashboardLayout>
  );
}
