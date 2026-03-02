/**
 * Contact form sheet — slide-over for creating/editing contacts.
 *
 * @description Two-step flow (matches AssetFormSheet pattern):
 *  Step 1 → Choose contact type (Individual / Trust / Organization)
 *  Step 2 → Fill type-specific fields
 *
 * Uses react-hook-form for form state and validation.
 * On edit, jumps directly to step 2 with pre-populated data.
 */
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from "@/components/ui/sheet";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { SensitiveInput } from "@/components/ui/sensitive-input";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { Switch } from "@/components/ui/switch";
import { ArrowLeft, Loader2 } from "lucide-react";
import {
  type ContactDto,
  type ContactType,
  type CreateContactRequest,
  CONTACT_TYPE_LABELS,
  RELATIONSHIP_LABELS,
  TRUST_CATEGORY_LABELS,
  TRUST_PURPOSE_LABELS,
  ORGANIZATION_TYPE_LABELS,
  type RelationshipType,
  type TrustCategory,
  type TrustPurpose,
  type OrganizationType,
} from "@/types/contacts";
import { ContactTypeSelector } from "./ContactTypeSelector";

/* ------------------------------------------------------------------ */
/*  Props                                                              */
/* ------------------------------------------------------------------ */

interface ContactFormSheetProps {
  /** Whether the sheet is open. */
  open: boolean;
  /** Callback to toggle open state. */
  onOpenChange: (open: boolean) => void;
  /** Contact to edit — null/undefined for create. */
  contact?: ContactDto | null;
  /** Called with form values on submission. */
  onSubmit: (values: CreateContactRequest) => void;
  /** Loading indicator while mutation runs. */
  isPending?: boolean;
}

/* ------------------------------------------------------------------ */
/*  Default form values                                                */
/* ------------------------------------------------------------------ */

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function getDefaults(contact?: ContactDto | null): Record<string, any> {
  if (!contact) {
    return {
      // Individual
      firstName: "",
      lastName: "",
      dateOfBirth: "",
      ssn: "",
      relationship: "",
      // Trust
      trustName: "",
      ein: "",
      category: "",
      purpose: "",
      specificType: "",
      trustDate: "",
      stateOfFormation: "",
      isGrantorTrust: false,
      hasCrummeyProvisions: false,
      isGstExempt: false,
      // Organization
      organizationName: "",
      organizationType: "",
      is501C3: false,
      // Shared
      email: "",
      phone: "",
      street1: "",
      street2: "",
      city: "",
      state: "",
      postalCode: "",
      country: "",
      notes: "",
    };
  }

  return {
    firstName: contact.firstName ?? "",
    lastName: contact.lastName ?? "",
    dateOfBirth: contact.dateOfBirth ?? "",
    ssn: "",
    relationship: contact.relationship ?? "",
    trustName: contact.trustName ?? "",
    ein: "",
    category: contact.category ?? "",
    purpose: contact.purpose ?? "",
    specificType: contact.specificType ?? "",
    trustDate: contact.trustDate ?? "",
    stateOfFormation: contact.stateOfFormation ?? "",
    isGrantorTrust: contact.isGrantorTrust ?? false,
    hasCrummeyProvisions: contact.hasCrummeyProvisions ?? false,
    isGstExempt: contact.isGstExempt ?? false,
    organizationName: contact.organizationName ?? "",
    organizationType: contact.organizationType ?? "",
    is501C3: contact.is501C3 ?? false,
    email: contact.email ?? "",
    phone: contact.phone ?? "",
    street1: contact.address?.street1 ?? "",
    street2: contact.address?.street2 ?? "",
    city: contact.address?.city ?? "",
    state: contact.address?.state ?? "",
    postalCode: contact.address?.postalCode ?? "",
    country: contact.address?.country ?? "",
    notes: contact.notes ?? "",
  };
}

/* ------------------------------------------------------------------ */
/*  Component                                                          */
/* ------------------------------------------------------------------ */

/**
 * Slide-over sheet for creating or editing a Contact.
 *
 * @example
 * <ContactFormSheet
 *   open={isOpen}
 *   onOpenChange={setIsOpen}
 *   contact={editingContact}
 *   onSubmit={handleSave}
 *   isPending={mutation.isPending}
 * />
 */
export function ContactFormSheet({
  open,
  onOpenChange,
  contact,
  onSubmit,
  isPending,
}: ContactFormSheetProps) {
  const isEdit = !!contact;
  const [selectedType, setSelectedType] = useState<ContactType | null>(null);
  const [step, setStep] = useState<1 | 2>(1);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const form = useForm({ defaultValues: {} as Record<string, any> });

  // Reset when sheet opens / closes
  useEffect(() => {
    if (open) {
      if (isEdit && contact) {
        setSelectedType(contact.contactType);
        setStep(2);
        form.reset(getDefaults(contact));
      } else {
        setSelectedType(null);
        setStep(1);
        form.reset(getDefaults());
      }
    }
  }, [open, contact, isEdit, form]);

  /** Step 1 → Step 2 transition. */
  function handleTypeSelect(type: ContactType) {
    setSelectedType(type);
    setStep(2);
    form.reset(getDefaults());
  }

  /** Back to type selection. */
  function handleBack() {
    setStep(1);
    setSelectedType(null);
  }

  /** Build a CreateContactRequest from flat form values. */
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  function handleFormSubmit(values: Record<string, any>) {
    if (!selectedType) return;

    const address =
      values.street1 || values.city || values.state
        ? {
            street1: values.street1,
            street2: values.street2 || undefined,
            city: values.city,
            state: values.state,
            postalCode: values.postalCode,
            country: values.country || "US",
          }
        : undefined;

    const base = {
      email: values.email || undefined,
      phone: values.phone || undefined,
      address,
      notes: values.notes || undefined,
    };

    let payload: CreateContactRequest;

    switch (selectedType) {
      case "Individual":
        payload = {
          ...base,
          contactType: "Individual",
          firstName: values.firstName,
          lastName: values.lastName,
          dateOfBirth: values.dateOfBirth || undefined,
          ssn: values.ssn || undefined,
          relationship: (values.relationship as RelationshipType) || undefined,
        };
        break;
      case "Trust":
        payload = {
          ...base,
          contactType: "Trust",
          trustName: values.trustName,
          ein: values.ein || undefined,
          category: values.category as TrustCategory,
          purpose: values.purpose as TrustPurpose,
          specificType: values.specificType || undefined,
          trustDate: values.trustDate || undefined,
          stateOfFormation: values.stateOfFormation || undefined,
          isGrantorTrust: values.isGrantorTrust,
          hasCrummeyProvisions: values.hasCrummeyProvisions,
          isGstExempt: values.isGstExempt,
        };
        break;
      case "Organization":
        payload = {
          ...base,
          contactType: "Organization",
          organizationName: values.organizationName,
          ein: values.ein || undefined,
          organizationType: values.organizationType as OrganizationType,
          is501C3: values.is501C3,
        };
        break;
    }

    onSubmit(payload);
  }

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent
        side="right"
        className="w-full sm:max-w-lg overflow-y-auto"
      >
        <SheetHeader>
          <SheetTitle>
            {step === 1
              ? "Choose Contact Type"
              : isEdit
                ? "Edit Contact"
                : `Add ${selectedType ? CONTACT_TYPE_LABELS[selectedType] : "Contact"}`}
          </SheetTitle>
          <SheetDescription>
            {step === 1
              ? "What kind of contact would you like to add?"
              : isEdit
                ? "Update the details for this contact."
                : "Fill in the details below."}
          </SheetDescription>
        </SheetHeader>

        {/* ---- Step 1: Type selector ---- */}
        {step === 1 && <ContactTypeSelector onSelect={handleTypeSelect} />}

        {/* ---- Step 2: Form ---- */}
        {step === 2 && selectedType && (
          <Form {...form}>
            <form
              onSubmit={form.handleSubmit(handleFormSubmit)}
              className="space-y-6 mt-4"
            >
              {/* Back button (create only) */}
              {!isEdit && (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={handleBack}
                  className="gap-1 -ml-2 text-muted-foreground"
                >
                  <ArrowLeft className="w-4 h-4" aria-hidden="true" />
                  Change type
                </Button>
              )}

              {/* ============================================= */}
              {/*  Type-specific fields                          */}
              {/* ============================================= */}
              {selectedType === "Individual" && (
                <IndividualFields control={form.control} />
              )}
              {selectedType === "Trust" && (
                <TrustFields control={form.control} />
              )}
              {selectedType === "Organization" && (
                <OrganizationFields control={form.control} />
              )}

              {/* ============================================= */}
              {/*  Shared fields                                 */}
              {/* ============================================= */}
              <Separator />
              <SharedFields control={form.control} />

              {/* Actions */}
              <div className="flex gap-3 pt-4">
                <Button
                  type="button"
                  variant="outline"
                  className="flex-1"
                  onClick={() => onOpenChange(false)}
                >
                  Cancel
                </Button>
                <Button
                  type="submit"
                  variant="gold"
                  className="flex-1"
                  disabled={isPending}
                >
                  {isPending && (
                    <Loader2
                      className="w-4 h-4 animate-spin"
                      aria-hidden="true"
                    />
                  )}
                  {isEdit ? "Save Changes" : "Add Contact"}
                </Button>
              </div>
            </form>
          </Form>
        )}
      </SheetContent>
    </Sheet>
  );
}

/* ------------------------------------------------------------------ */
/*  Sub-components: type-specific fields                               */
/* ------------------------------------------------------------------ */

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function IndividualFields({ control }: { control: any }) {
  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
        Individual Details
      </h3>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          control={control}
          name="firstName"
          rules={{
            required: "First name is required",
            maxLength: { value: 100, message: "Max 100 characters" },
          }}
          render={({ field }) => (
            <FormItem>
              <FormLabel>First Name *</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Priya" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="lastName"
          rules={{
            required: "Last name is required",
            maxLength: { value: 100, message: "Max 100 characters" },
          }}
          render={({ field }) => (
            <FormItem>
              <FormLabel>Last Name *</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Patel" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>
      <FormField
        control={control}
        name="relationship"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Relationship</FormLabel>
            <Select onValueChange={field.onChange} value={field.value || ""}>
              <FormControl>
                <SelectTrigger>
                  <SelectValue placeholder="Select relationship..." />
                </SelectTrigger>
              </FormControl>
              <SelectContent>
                {Object.entries(RELATIONSHIP_LABELS).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <FormMessage />
          </FormItem>
        )}
      />
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          control={control}
          name="dateOfBirth"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Date of Birth</FormLabel>
              <FormControl>
                <Input type="date" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="ssn"
          rules={{
            pattern: {
              value: /^\d{0,9}$/,
              message: "SSN must be 9 digits",
            },
          }}
          render={({ field }) => (
            <FormItem>
              <FormLabel>SSN</FormLabel>
              <FormControl>
                <SensitiveInput
                  format="ssn"
                  value={field.value}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  ref={field.ref}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>
    </div>
  );
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function TrustFields({ control }: { control: any }) {
  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
        Trust Details
      </h3>
      <FormField
        control={control}
        name="trustName"
        rules={{
          required: "Trust name is required",
          maxLength: { value: 200, message: "Max 200 characters" },
        }}
        render={({ field }) => (
          <FormItem>
            <FormLabel>Trust Name *</FormLabel>
            <FormControl>
              <Input
                placeholder="e.g. Patel Family Revocable Trust"
                {...field}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          control={control}
          name="category"
          rules={{ required: "Category is required" }}
          render={({ field }) => (
            <FormItem>
              <FormLabel>Category *</FormLabel>
              <Select onValueChange={field.onChange} value={field.value || ""}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select category..." />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {Object.entries(TRUST_CATEGORY_LABELS).map(
                    ([value, label]) => (
                      <SelectItem key={value} value={value}>
                        {label}
                      </SelectItem>
                    )
                  )}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="purpose"
          rules={{ required: "Purpose is required" }}
          render={({ field }) => (
            <FormItem>
              <FormLabel>Purpose *</FormLabel>
              <Select onValueChange={field.onChange} value={field.value || ""}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select purpose..." />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {Object.entries(TRUST_PURPOSE_LABELS).map(
                    ([value, label]) => (
                      <SelectItem key={value} value={value}>
                        {label}
                      </SelectItem>
                    )
                  )}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>
      <FormField
        control={control}
        name="specificType"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Specific Type</FormLabel>
            <FormControl>
              <Input placeholder="e.g. GRAT, QPRT, ILIT" {...field} />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <FormField
          control={control}
          name="trustDate"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Trust Date</FormLabel>
              <FormControl>
                <Input type="date" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="stateOfFormation"
          render={({ field }) => (
            <FormItem>
              <FormLabel>State of Formation</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Texas" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      </div>
      <FormField
        control={control}
        name="ein"
        rules={{
          pattern: {
            value: /^\d{0,9}$/,
            message: "EIN must be 9 digits",
          },
        }}
        render={({ field }) => (
          <FormItem>
            <FormLabel>EIN</FormLabel>
            <FormControl>
              <SensitiveInput
                format="ein"
                value={field.value}
                onChange={field.onChange}
                onBlur={field.onBlur}
                ref={field.ref}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <div className="space-y-3">
        <FormField
          control={control}
          name="isGrantorTrust"
          render={({ field }) => (
            <FormItem className="flex items-center justify-between rounded-lg border p-3">
              <FormLabel className="text-sm font-normal">
                Grantor Trust
              </FormLabel>
              <FormControl>
                <Switch
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="hasCrummeyProvisions"
          render={({ field }) => (
            <FormItem className="flex items-center justify-between rounded-lg border p-3">
              <FormLabel className="text-sm font-normal">
                Crummey Provisions
              </FormLabel>
              <FormControl>
                <Switch
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="isGstExempt"
          render={({ field }) => (
            <FormItem className="flex items-center justify-between rounded-lg border p-3">
              <FormLabel className="text-sm font-normal">
                GST Exempt
              </FormLabel>
              <FormControl>
                <Switch
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
            </FormItem>
          )}
        />
      </div>
    </div>
  );
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function OrganizationFields({ control }: { control: any }) {
  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
        Organization Details
      </h3>
      <FormField
        control={control}
        name="organizationName"
        rules={{
          required: "Organization name is required",
          maxLength: { value: 200, message: "Max 200 characters" },
        }}
        render={({ field }) => (
          <FormItem>
            <FormLabel>Organization Name *</FormLabel>
            <FormControl>
              <Input
                placeholder="e.g. Austin Community Foundation"
                {...field}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <FormField
        control={control}
        name="organizationType"
        rules={{ required: "Organization type is required" }}
        render={({ field }) => (
          <FormItem>
            <FormLabel>Type *</FormLabel>
            <Select onValueChange={field.onChange} value={field.value || ""}>
              <FormControl>
                <SelectTrigger>
                  <SelectValue placeholder="Select type..." />
                </SelectTrigger>
              </FormControl>
              <SelectContent>
                {Object.entries(ORGANIZATION_TYPE_LABELS).map(
                  ([value, label]) => (
                    <SelectItem key={value} value={value}>
                      {label}
                    </SelectItem>
                  )
                )}
              </SelectContent>
            </Select>
            <FormMessage />
          </FormItem>
        )}
      />
      <FormField
        control={control}
        name="ein"
        rules={{
          pattern: {
            value: /^\d{0,9}$/,
            message: "EIN must be 9 digits",
          },
        }}
        render={({ field }) => (
          <FormItem>
            <FormLabel>EIN</FormLabel>
            <FormControl>
              <SensitiveInput
                format="ein"
                value={field.value}
                onChange={field.onChange}
                onBlur={field.onBlur}
                ref={field.ref}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
      <FormField
        control={control}
        name="is501C3"
        render={({ field }) => (
          <FormItem className="flex items-center justify-between rounded-lg border p-3">
            <FormLabel className="text-sm font-normal">
              501(c)(3) Tax-Exempt
            </FormLabel>
            <FormControl>
              <Switch
                checked={field.value}
                onCheckedChange={field.onChange}
              />
            </FormControl>
          </FormItem>
        )}
      />
    </div>
  );
}

/* ------------------------------------------------------------------ */
/*  Shared fields: contact info + address + notes                      */
/* ------------------------------------------------------------------ */

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function SharedFields({ control }: { control: any }) {
  return (
    <>
      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          Contact Information
        </h3>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <FormField
            control={control}
            name="email"
            rules={{
              pattern: {
                value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                message: "Invalid email address",
              },
            }}
            render={({ field }) => (
              <FormItem>
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input
                    type="email"
                    placeholder="user@example.com"
                    {...field}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={control}
            name="phone"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Phone</FormLabel>
                <FormControl>
                  <Input type="tel" placeholder="(512) 555-1234" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>
      </div>

      <Separator />

      <div className="space-y-4">
        <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          Address
        </h3>
        <FormField
          control={control}
          name="street1"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Street Line 1</FormLabel>
              <FormControl>
                <Input placeholder="123 Main Street" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={control}
          name="street2"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Street Line 2</FormLabel>
              <FormControl>
                <Input placeholder="Suite 200" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <FormField
            control={control}
            name="city"
            render={({ field }) => (
              <FormItem className="col-span-2 sm:col-span-1">
                <FormLabel>City</FormLabel>
                <FormControl>
                  <Input placeholder="Austin" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={control}
            name="state"
            render={({ field }) => (
              <FormItem>
                <FormLabel>State</FormLabel>
                <FormControl>
                  <Input placeholder="TX" maxLength={2} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={control}
            name="postalCode"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Zip</FormLabel>
                <FormControl>
                  <Input placeholder="78701" maxLength={10} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={control}
            name="country"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Country</FormLabel>
                <FormControl>
                  <Input placeholder="US" maxLength={2} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>
      </div>

      <Separator />

      <FormField
        control={control}
        name="notes"
        render={({ field }) => (
          <FormItem>
            <FormLabel>Notes</FormLabel>
            <FormControl>
              <Textarea
                placeholder="Additional notes..."
                rows={3}
                {...field}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />
    </>
  );
}
