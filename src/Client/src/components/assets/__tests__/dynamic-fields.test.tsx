import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { TooltipProvider } from "@/components/ui/tooltip";
import { I18nextProvider } from "react-i18next";
import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import { AssetFormSheet } from "../AssetFormSheet";

/**
 * Creates a test i18n instance with asset translations.
 */
function createTestI18n() {
  const testI18n = i18n.createInstance();
  testI18n.use(initReactI18next).init({
    lng: "en",
    resources: {
      en: {
        assets: {
          "help.Vehicle.vin": "17-character Vehicle Identification Number",
          "help.Investment.costBasis": "Total amount originally paid for shares",
          "help.common.currentValue": "What you believe this asset is worth today.",
          "help.common.depreciationMethod": "How the asset's value decreases over time.",
          "options.PropertyType.SingleFamily": "Single Family Home",
          "options.PropertyType.Condo": "Condominium",
          "options.PropertyType.Townhouse": "Townhouse",
          "options.PropertyType.MultiFamily": "Multi-Family (Duplex, Triplex)",
          "options.PropertyType.Land": "Undeveloped Land",
          "options.PropertyType.Commercial": "Commercial",
          "options.PropertyType.Other": "Other",
          "options.DepreciationMethod.None": "None",
          "options.DepreciationMethod.StraightLine": "Straight Line",
          "options.DepreciationMethod.DecliningBalance": "Declining Balance",
          "options.DepreciationMethod.Macrs": "MACRS (Modified Accelerated Cost Recovery)",
          "options.InvestmentType.Stocks": "Stocks",
          "options.InvestmentType.Bonds": "Bonds",
          "options.InvestmentType.MutualFunds": "Mutual Funds",
          "options.InvestmentType.ETF": "ETF",
          "options.InvestmentType.Options": "Options",
          "options.InvestmentType.Other": "Other",
          "options.BankAccountType.Checking": "Checking",
          "options.BankAccountType.Savings": "Savings",
          "options.BankAccountType.MoneyMarket": "Money Market",
          "options.BankAccountType.CD": "Certificate of Deposit (CD)",
          "options.BankAccountType.HYSA": "High-Yield Savings (HYSA)",
          "options.BankAccountType.Other": "Other",
        },
      },
    },
    defaultNS: "assets",
    interpolation: { escapeValue: false },
  });
  return testI18n;
}

/** Wrapper with providers needed for rendering AssetFormSheet */
function renderAssetForm(props: Partial<React.ComponentProps<typeof AssetFormSheet>> = {}) {
  const testI18n = createTestI18n();
  return render(
    <I18nextProvider i18n={testI18n}>
      <TooltipProvider>
        <AssetFormSheet
          open={true}
          onOpenChange={() => {}}
          onSubmit={() => {}}
          {...props}
        />
      </TooltipProvider>
    </I18nextProvider>,
  );
}

describe("AssetFormSheet — type selection renders expected fields", () => {
  it("renders asset type selector on step 1", () => {
    renderAssetForm();

    expect(screen.getByText("Choose Asset Type")).toBeInTheDocument();
  });
});

describe("AssetFormSheet — common fields have help tooltips", () => {
  it("renders help icon next to Current Value field", async () => {
    renderAssetForm({
      asset: {
        id: "test-1",
        name: "Test Asset",
        type: "Vehicle",
        currentValue: 10000,
        isActive: true,
      } as any,
    });

    // The form should be in step 2 (edit mode)
    const helpButtons = screen.getAllByRole("button", { name: "More info" });
    expect(helpButtons.length).toBeGreaterThan(0);
  });
});

describe("AssetFormSheet — VIN field shows help icon", () => {
  it("renders help icon for VIN field in Vehicle form", () => {
    renderAssetForm({
      asset: {
        id: "test-1",
        name: "My Car",
        type: "Vehicle",
        currentValue: 25000,
        isActive: true,
      } as any,
    });

    // VIN label should be present
    expect(screen.getByText(/VIN/)).toBeInTheDocument();

    // At least one help icon should be present (for VIN and currentValue)
    const helpButtons = screen.getAllByRole("button", { name: "More info" });
    expect(helpButtons.length).toBeGreaterThanOrEqual(2);
  });
});
