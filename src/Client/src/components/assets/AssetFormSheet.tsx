import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import {
  Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription,
} from "@/components/ui/sheet";
import {
  Form, FormControl, FormField, FormItem, FormLabel, FormMessage,
} from "@/components/ui/form";
import {
  Select, SelectContent, SelectItem, SelectTrigger, SelectValue,
} from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { DatePicker } from "@/components/ui/date-picker";
import { FieldInfo } from "@/components/ui/field-info";
import { ArrowLeft, Loader2 } from "lucide-react";
import {
  type AssetDto, type AssetType, type DepreciationMethod,
  ASSET_TYPE_LABELS,
} from "@/types/assets";
import { AssetTypeSelector } from "./AssetTypeSelector";
import {
  TYPE_SPECIFIC_FIELDS, INSTITUTION_TYPES, DEPRECIATION_TYPES,
  type FieldDef,
} from "./type-specific-fields";

/** Depreciation method options with i18n label keys */
const DEPRECIATION_METHODS: { value: DepreciationMethod; labelKey: string }[] = [
  { value: "None", labelKey: "options.DepreciationMethod.None" },
  { value: "StraightLine", labelKey: "options.DepreciationMethod.StraightLine" },
  { value: "DecliningBalance", labelKey: "options.DepreciationMethod.DecliningBalance" },
  { value: "Macrs", labelKey: "options.DepreciationMethod.Macrs" },
];

interface AssetFormSheetProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  asset?: AssetDto | null;
  onSubmit: (values: Record<string, unknown>) => void;
  isPending?: boolean;
}

/**
 * Side-panel form for creating/editing assets.
 *
 * @description Two-step flow: step 1 picks asset type, step 2 shows
 * common + type-specific fields. Includes contextual help tooltips
 * and localized dropdown labels via react-i18next.
 */
export function AssetFormSheet({ open, onOpenChange, asset, onSubmit, isPending }: AssetFormSheetProps) {
  const { t } = useTranslation("assets");
  const isEdit = !!asset;
  const [selectedType, setSelectedType] = useState<AssetType | null>(null);
  const [step, setStep] = useState<1 | 2>(1);

  const form = useForm({ defaultValues: {} as Record<string, any> });

  // Reset state when sheet opens/closes
  useEffect(() => {
    if (open) {
      if (isEdit && asset) {
        setSelectedType(asset.type);
        setStep(2);
        // Pre-populate all known fields
        const defaults: Record<string, any> = {
          name: asset.name,
          currentValue: asset.currentValue,
          description: asset.description ?? "",
          purchasePrice: asset.purchasePrice ?? "",
          purchaseDate: asset.purchaseDate ?? "",
          marketValue: "",
          lastValuationDate: "",
          location: asset.location ?? "",
          accountNumber: asset.accountNumber ?? "",
          institutionName: asset.institutionName ?? "",
          depreciationMethod: "None",
          salvageValue: "",
          usefulLifeMonths: "",
          inServiceDate: "",
        };
        form.reset(defaults);
      } else {
        setSelectedType(null);
        setStep(1);
        form.reset({});
      }
    }
  }, [open, asset]);

  function handleTypeSelect(type: AssetType) {
    setSelectedType(type);
    setStep(2);
    form.reset({
      name: "",
      currentValue: "",
      description: "",
      purchasePrice: "",
      purchaseDate: "",
      marketValue: "",
      lastValuationDate: "",
      location: "",
      accountNumber: "",
      institutionName: "",
      depreciationMethod: "None",
      salvageValue: "",
      usefulLifeMonths: "",
      inServiceDate: "",
    });
  }

  function handleBack() {
    setStep(1);
    setSelectedType(null);
  }

  function handleFormSubmit(values: Record<string, any>) {
    onSubmit({ ...values, type: selectedType });
  }

  const showInstitution = selectedType && INSTITUTION_TYPES.includes(selectedType);
  const showDepreciation = selectedType && DEPRECIATION_TYPES.includes(selectedType);
  const typeFields = selectedType ? TYPE_SPECIFIC_FIELDS[selectedType] ?? [] : [];
  const depMethod = form.watch("depreciationMethod");
  const showDepFields = depMethod && depMethod !== "None";

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">
        <SheetHeader>
          <SheetTitle>
            {step === 1
              ? "Choose Asset Type"
              : isEdit
              ? "Edit Asset"
              : `Add ${selectedType ? ASSET_TYPE_LABELS[selectedType] : "Asset"}`}
          </SheetTitle>
          <SheetDescription>
            {step === 1
              ? "What kind of asset would you like to track?"
              : isEdit
              ? "Update the details of this asset."
              : "Fill in the details below."}
          </SheetDescription>
        </SheetHeader>

        {step === 1 && <AssetTypeSelector onSelect={handleTypeSelect} />}

        {step === 2 && selectedType && (
          <Form {...form}>
            <form onSubmit={form.handleSubmit(handleFormSubmit)} className="space-y-6 mt-4">
              {/* Back button */}
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={handleBack}
                className="gap-1 -ml-2 text-muted-foreground"
              >
                <ArrowLeft className="w-4 h-4" />
                Change type
              </Button>

              {/* Common fields */}
              <div className="space-y-4">
                <FormField
                  control={form.control}
                  name="name"
                  rules={{ required: "Name is required", maxLength: { value: 100, message: "Max 100 chars" } }}
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Asset Name *</FormLabel>
                      <FormControl><Input placeholder="e.g. Primary Residence" {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="currentValue"
                  rules={{ required: "Current value is required" }}
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel className="inline-flex items-center gap-1">
                        Current Value ($) *
                        <FieldInfo textKey="help.common.currentValue" />
                      </FormLabel>
                      <FormControl><Input type="number" min={0} step="0.01" placeholder="0" {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="description"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Description</FormLabel>
                      <FormControl><Textarea placeholder="Optional description" rows={3} {...field} /></FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="purchasePrice"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="inline-flex items-center gap-1">
                          Purchase Price ($)
                          <FieldInfo textKey="help.common.purchasePrice" />
                        </FormLabel>
                        <FormControl><Input type="number" min={0} step="0.01" placeholder="—" {...field} /></FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="purchaseDate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Purchase Date</FormLabel>
                        <FormControl>
                          <DatePicker
                            value={field.value}
                            onChange={field.onChange}
                            name={field.name}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <FormField
                    control={form.control}
                    name="marketValue"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel className="inline-flex items-center gap-1">
                          Market Value ($)
                          <FieldInfo textKey="help.common.marketValue" />
                        </FormLabel>
                        <FormControl><Input type="number" min={0} step="0.01" placeholder="—" {...field} /></FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={form.control}
                    name="lastValuationDate"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Last Valuation Date</FormLabel>
                        <FormControl>
                          <DatePicker
                            value={field.value}
                            onChange={field.onChange}
                            name={field.name}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </div>
              </div>

              {/* Type-specific fields */}
              {typeFields.length > 0 && (
                <>
                  <Separator />
                  <div className="space-y-4">
                    <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
                      {ASSET_TYPE_LABELS[selectedType]} Details
                    </h3>
                    <DynamicFields fields={typeFields} control={form.control} t={t} />
                  </div>
                </>
              )}

              {/* Institution & Account */}
              {showInstitution && (
                <>
                  <Separator />
                  <div className="space-y-4">
                    <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Institution & Account</h3>
                    <div className="grid grid-cols-2 gap-4">
                      <FormField
                        control={form.control}
                        name="institutionName"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Institution Name</FormLabel>
                            <FormControl><Input placeholder="e.g. Fidelity" {...field} /></FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                      <FormField
                        control={form.control}
                        name="accountNumber"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Account Number</FormLabel>
                            <FormControl><Input placeholder="e.g. ****1234" {...field} /></FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>
                  </div>
                </>
              )}

              {/* Depreciation */}
              {showDepreciation && (
                <>
                  <Separator />
                  <div className="space-y-4">
                    <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Depreciation</h3>
                    <FormField
                      control={form.control}
                      name="depreciationMethod"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel className="inline-flex items-center gap-1">
                            Method
                            <FieldInfo textKey="help.common.depreciationMethod" />
                          </FormLabel>
                          <Select onValueChange={field.onChange} value={field.value || "None"}>
                            <FormControl>
                              <SelectTrigger><SelectValue placeholder="None" /></SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              {DEPRECIATION_METHODS.map((m) => (
                                <SelectItem key={m.value} value={m.value}>{t(m.labelKey)}</SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                    {showDepFields && (
                      <div className="grid grid-cols-2 gap-4">
                        <FormField
                          control={form.control}
                          name="salvageValue"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel className="inline-flex items-center gap-1">
                                Salvage Value ($)
                                <FieldInfo textKey="help.common.salvageValue" />
                              </FormLabel>
                              <FormControl><Input type="number" min={0} step="0.01" placeholder="0" {...field} /></FormControl>
                              <FormMessage />
                            </FormItem>
                          )}
                        />
                        <FormField
                          control={form.control}
                          name="usefulLifeMonths"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel className="inline-flex items-center gap-1">
                                Useful Life (months)
                                <FieldInfo textKey="help.common.usefulLifeMonths" />
                              </FormLabel>
                              <FormControl><Input type="number" min={1} step="1" placeholder="60" {...field} /></FormControl>
                              <FormMessage />
                            </FormItem>
                          )}
                        />
                        <FormField
                          control={form.control}
                          name="inServiceDate"
                          render={({ field }) => (
                            <FormItem>
                              <FormLabel className="inline-flex items-center gap-1">
                                In-Service Date
                                <FieldInfo textKey="help.common.inServiceDate" />
                              </FormLabel>
                              <FormControl>
                                <DatePicker
                                  value={field.value}
                                  onChange={field.onChange}
                                  name={field.name}
                                />
                              </FormControl>
                              <FormMessage />
                            </FormItem>
                          )}
                        />
                      </div>
                    )}
                  </div>
                </>
              )}

              {/* Actions */}
              <div className="flex gap-3 pt-4">
                <Button type="button" variant="outline" className="flex-1" onClick={() => onOpenChange(false)}>
                  Cancel
                </Button>
                <Button type="submit" variant="gold" className="flex-1" disabled={isPending}>
                  {isPending && <Loader2 className="w-4 h-4 animate-spin" />}
                  {isEdit ? "Save Changes" : "Add Asset"}
                </Button>
              </div>
            </form>
          </Form>
        )}
      </SheetContent>
    </Sheet>
  );
}

/**
 * Renders an array of FieldDef dynamically with i18n support.
 *
 * @description Resolves option labels via `t(labelKey)` and renders
 * `FieldInfo` tooltips for fields that have a `helpKey`.
 */
function DynamicFields({ fields, control, t }: { fields: FieldDef[]; control: any; t: (key: string) => string }) {
  return (
    <div className="grid grid-cols-2 gap-4">
      {fields.map((f) => (
        <FormField
          key={f.name}
          control={control}
          name={f.name}
          rules={f.required ? { required: `${f.label} is required` } : undefined}
          render={({ field }) => (
            <FormItem className={f.type === "textarea" ? "col-span-2" : undefined}>
              <FormLabel className={f.helpKey ? "inline-flex items-center gap-1" : undefined}>
                {f.label}{f.required ? " *" : ""}
                {f.helpKey && <FieldInfo textKey={f.helpKey} />}
              </FormLabel>
              <FormControl>
                {f.type === "select" ? (
                  <Select onValueChange={field.onChange} value={field.value || ""}>
                    <SelectTrigger><SelectValue placeholder={`Select ${f.label.toLowerCase()}`} /></SelectTrigger>
                    <SelectContent>
                      {f.options?.map((o) => (
                        <SelectItem key={o.value} value={o.value}>{t(o.labelKey)}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : f.type === "textarea" ? (
                  <Textarea placeholder={f.placeholder} rows={3} {...field} />
                ) : (
                  <Input
                    type={f.type === "number" ? "number" : f.type === "date" ? "date" : "text"}
                    min={f.type === "number" ? 0 : undefined}
                    step={f.step}
                    placeholder={f.placeholder}
                    {...field}
                  />
                )}
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
      ))}
    </div>
  );
}
