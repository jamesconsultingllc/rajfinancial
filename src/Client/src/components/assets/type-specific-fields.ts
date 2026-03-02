/**
 * Type-specific field definitions for asset forms.
 *
 * @description Defines the dynamic form fields rendered for each asset type.
 * Option labels use i18n translation keys (`labelKey`) resolved at render time
 * via `t()`. Fields may include a `helpKey` referencing contextual help text
 * in the `assets` i18n namespace.
 */
import { type AssetType } from "@/types/assets";

export interface FieldDef {
  /** Form field name (maps to form state key) */
  name: string;
  /** Display label for the field */
  label: string;
  /** Input type */
  type: "text" | "number" | "date" | "select" | "textarea";
  /** Whether the field is required */
  required?: boolean;
  /** Placeholder text */
  placeholder?: string;
  /** Select options with translation keys for labels */
  options?: { value: string; labelKey: string }[];
  /** Step attribute for number inputs */
  step?: string;
  /** Suffix text (e.g., units) */
  suffix?: string;
  /** i18n key for contextual help tooltip (resolved in `assets` namespace) */
  helpKey?: string;
}

/**
 * Creates select options with i18n label keys.
 *
 * @param enumName - The enum/group name used as prefix in translation keys
 * @param values - The raw option values
 * @returns Array of options with `value` and `labelKey` for i18n resolution
 *
 * @example
 * opts("PropertyType", "SingleFamily", "Condo")
 * // => [{ value: "SingleFamily", labelKey: "options.PropertyType.SingleFamily" }, ...]
 */
function opts(enumName: string, ...values: string[]) {
  return values.map((v) => ({ value: v, labelKey: `options.${enumName}.${v}` }));
}

const vehicleFields: FieldDef[] = [
  { name: "make", label: "Make", type: "text", required: true, placeholder: "e.g. Toyota" },
  { name: "model", label: "Model", type: "text", required: true, placeholder: "e.g. Camry" },
  { name: "year", label: "Year", type: "number", required: true, placeholder: "2024", step: "1" },
  { name: "vin", label: "VIN", type: "text", placeholder: "Vehicle ID Number", helpKey: "help.Vehicle.vin" },
  { name: "mileage", label: "Mileage", type: "number", placeholder: "0", step: "1" },
  { name: "color", label: "Color", type: "text", placeholder: "e.g. Silver" },
  { name: "licensePlate", label: "License Plate", type: "text", placeholder: "e.g. ABC-1234" },
];

const realEstateFields: FieldDef[] = [
  { name: "propertyType", label: "Property Type", type: "select", required: true, options: opts("PropertyType", "SingleFamily", "Condo", "Townhouse", "MultiFamily", "Land", "Commercial", "Other"), helpKey: "help.RealEstate.propertyType" },
  { name: "address", label: "Address", type: "text", required: true, placeholder: "Street address" },
  { name: "city", label: "City", type: "text", required: true, placeholder: "City" },
  { name: "state", label: "State", type: "text", required: true, placeholder: "State" },
  { name: "zip", label: "ZIP", type: "text", required: true, placeholder: "ZIP code" },
  { name: "sqFeet", label: "Sq Feet", type: "number", placeholder: "0", step: "1" },
  { name: "yearBuilt", label: "Year Built", type: "number", placeholder: "2000", step: "1" },
  { name: "bedrooms", label: "Bedrooms", type: "number", placeholder: "0", step: "1" },
  { name: "bathrooms", label: "Bathrooms", type: "number", placeholder: "0", step: "0.5" },
  { name: "lotSize", label: "Lot Size (acres)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.RealEstate.lotSize" },
];

const investmentFields: FieldDef[] = [
  { name: "investmentType", label: "Investment Type", type: "select", required: true, options: opts("InvestmentType", "Stocks", "Bonds", "MutualFunds", "ETF", "Options", "Other") },
  { name: "ticker", label: "Ticker", type: "text", placeholder: "e.g. AAPL", helpKey: "help.Investment.ticker" },
  { name: "shares", label: "Shares", type: "number", placeholder: "0", step: "0.0001", helpKey: "help.Investment.shares" },
  { name: "costBasis", label: "Cost Basis ($)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.Investment.costBasis" },
  { name: "brokerage", label: "Brokerage", type: "text", placeholder: "e.g. Fidelity" },
];

const retirementFields: FieldDef[] = [
  { name: "accountType", label: "Account Type", type: "select", required: true, options: opts("RetirementAccountType", "401k", "IRA", "RothIRA", "SEP_IRA", "Pension", "403b", "Other"), helpKey: "help.Retirement.accountType" },
  { name: "employerMatch", label: "Employer Match (%)", type: "number", placeholder: "0", step: "0.1", helpKey: "help.Retirement.employerMatch" },
  { name: "vestedPercent", label: "Vested (%)", type: "number", placeholder: "0", step: "0.1", helpKey: "help.Retirement.vestedPercent" },
  { name: "annualContribution", label: "Annual Contribution ($)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.Retirement.annualContribution" },
];

const bankAccountFields: FieldDef[] = [
  { name: "accountType", label: "Account Type", type: "select", required: true, options: opts("BankAccountType", "Checking", "Savings", "MoneyMarket", "CD", "HYSA", "Other"), helpKey: "help.BankAccount.accountType" },
  { name: "routingNumber", label: "Routing Number", type: "text", placeholder: "Routing number", helpKey: "help.BankAccount.routingNumber" },
  { name: "interestRate", label: "Interest Rate (%)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.BankAccount.interestRate" },
  { name: "maturityDate", label: "Maturity Date", type: "date", helpKey: "help.BankAccount.maturityDate" },
];

const insuranceFields: FieldDef[] = [
  { name: "policyType", label: "Policy Type", type: "select", required: true, options: opts("InsurancePolicyType", "WholeLife", "UniversalLife", "TermLife", "Annuity", "Other"), helpKey: "help.Insurance.policyType" },
  { name: "policyNumber", label: "Policy Number", type: "text", required: true, placeholder: "Policy #" },
  { name: "cashValue", label: "Cash Value ($)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.Insurance.cashValue" },
  { name: "deathBenefit", label: "Death Benefit ($)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.Insurance.deathBenefit" },
  { name: "premiumAmount", label: "Premium Amount ($)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.Insurance.premiumAmount" },
  { name: "premiumFrequency", label: "Premium Frequency", type: "select", options: opts("PremiumFrequency", "Monthly", "Quarterly", "SemiAnnually", "Annually") },
];

const businessFields: FieldDef[] = [
  { name: "entityType", label: "Entity Type", type: "select", required: true, options: opts("BusinessEntityType", "SoleProprietorship", "Partnership", "LLC", "Corporation", "SCorp", "Other"), helpKey: "help.Business.entityType" },
  { name: "ownershipPercent", label: "Ownership (%)", type: "number", required: true, placeholder: "100", step: "0.01", helpKey: "help.Business.ownershipPercent" },
  { name: "ein", label: "EIN", type: "text", placeholder: "XX-XXXXXXX", helpKey: "help.Business.ein" },
  { name: "industry", label: "Industry", type: "text", placeholder: "e.g. Technology" },
  { name: "annualRevenue", label: "Annual Revenue ($)", type: "number", placeholder: "0", step: "0.01" },
];

const cryptoFields: FieldDef[] = [
  { name: "coinSymbol", label: "Coin Symbol", type: "text", required: true, placeholder: "e.g. BTC", helpKey: "help.Cryptocurrency.coinSymbol" },
  { name: "quantity", label: "Quantity", type: "number", required: true, placeholder: "0", step: "0.00000001" },
  { name: "exchangeWallet", label: "Exchange / Wallet", type: "text", placeholder: "e.g. Coinbase" },
  { name: "stakingApy", label: "Staking APY (%)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.Cryptocurrency.stakingApy" },
  { name: "walletAddress", label: "Wallet Address", type: "text", placeholder: "Address" },
];

const collectibleFields: FieldDef[] = [
  { name: "category", label: "Category", type: "text", required: true, placeholder: "e.g. Fine Art" },
  { name: "condition", label: "Condition", type: "select", options: opts("ItemCondition", "Mint", "Excellent", "Good", "Fair", "Poor"), helpKey: "help.Collectible.condition" },
  { name: "provenance", label: "Provenance", type: "text", placeholder: "Origin / history", helpKey: "help.Collectible.provenance" },
  { name: "appraiserName", label: "Appraiser Name", type: "text", placeholder: "Appraiser" },
  { name: "lastAppraisalDate", label: "Last Appraisal Date", type: "date" },
];

const ipFields: FieldDef[] = [
  { name: "ipType", label: "IP Type", type: "select", required: true, options: opts("IpType", "Patent", "Trademark", "Copyright", "TradeSecret", "Other"), helpKey: "help.IntellectualProperty.ipType" },
  { name: "registrationNumber", label: "Registration Number", type: "text", placeholder: "Reg #", helpKey: "help.IntellectualProperty.registrationNumber" },
  { name: "filingDate", label: "Filing Date", type: "date" },
  { name: "expirationDate", label: "Expiration Date", type: "date" },
  { name: "licensee", label: "Licensee", type: "text", placeholder: "Licensed to" },
  { name: "royaltyRate", label: "Royalty Rate (%)", type: "number", placeholder: "0", step: "0.01", helpKey: "help.IntellectualProperty.royaltyRate" },
];

export const TYPE_SPECIFIC_FIELDS: Partial<Record<AssetType, FieldDef[]>> = {
  Vehicle: vehicleFields,
  RealEstate: realEstateFields,
  Investment: investmentFields,
  Retirement: retirementFields,
  BankAccount: bankAccountFields,
  Insurance: insuranceFields,
  Business: businessFields,
  Cryptocurrency: cryptoFields,
  Collectible: collectibleFields,
  PersonalProperty: collectibleFields,
  IntellectualProperty: ipFields,
};

export const INSTITUTION_TYPES: AssetType[] = [
  "Investment", "Retirement", "BankAccount", "Insurance", "Cryptocurrency",
];

export const DEPRECIATION_TYPES: AssetType[] = [
  "Vehicle", "RealEstate", "Business", "PersonalProperty", "Collectible",
];
