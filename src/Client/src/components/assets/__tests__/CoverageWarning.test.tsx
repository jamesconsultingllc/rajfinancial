/**
 * Tests for the CoverageWarning component.
 *
 * @description Verifies rendering, i18n, icons, and resolve callback
 * for each warning type.
 */
import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { I18nextProvider } from "react-i18next";
import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import { CoverageWarning, type CoverageWarningType } from "../CoverageWarning";

/* ------------------------------------------------------------------ */
/*  i18n setup                                                         */
/* ------------------------------------------------------------------ */

const translations = {
  "coverageWarning.NoBeneficiaries.title": "No Beneficiaries Assigned",
  "coverageWarning.NoBeneficiaries.description":
    "This asset has no beneficiaries. Without designated beneficiaries, the asset may be subject to probate.",
  "coverageWarning.IncompleteAllocation.title": "Incomplete Allocation",
  "coverageWarning.IncompleteAllocation.description":
    "Beneficiary allocations do not total 100%. Adjust allocations to ensure full coverage.",
  "coverageWarning.NoContingentBeneficiaries.title": "No Contingent Beneficiaries",
  "coverageWarning.NoContingentBeneficiaries.description":
    "This asset has no contingent beneficiaries. Consider adding backup beneficiaries in case primary beneficiaries are unable to inherit.",
  "coverageWarning.resolve": "Resolve",
};

function createTestI18n() {
  const testI18n = i18n.createInstance();
  testI18n.use(initReactI18next).init({
    lng: "en",
    resources: { en: { assets: translations } },
    defaultNS: "assets",
    interpolation: { escapeValue: false },
  });
  return testI18n;
}

function renderWarning(
  props: {
    warningType: CoverageWarningType;
    message?: string;
    assetId?: string;
    onResolve?: () => void;
  },
) {
  return render(
    <I18nextProvider i18n={createTestI18n()}>
      <CoverageWarning {...props} />
    </I18nextProvider>,
  );
}

/* ------------------------------------------------------------------ */
/*  Rendering per warning type                                         */
/* ------------------------------------------------------------------ */

describe("CoverageWarning — NoBeneficiaries", () => {
  it("renders title and default description", () => {
    renderWarning({ warningType: "NoBeneficiaries" });

    expect(screen.getByText("No Beneficiaries Assigned")).toBeInTheDocument();
    expect(screen.getByText(/subject to probate/i)).toBeInTheDocument();
  });

  it("renders the alert with role='alert'", () => {
    renderWarning({ warningType: "NoBeneficiaries" });

    expect(screen.getByRole("alert")).toBeInTheDocument();
  });
});

describe("CoverageWarning — IncompleteAllocation", () => {
  it("renders title and default description", () => {
    renderWarning({ warningType: "IncompleteAllocation" });

    expect(screen.getByText("Incomplete Allocation")).toBeInTheDocument();
    expect(screen.getByText(/do not total 100%/i)).toBeInTheDocument();
  });
});

describe("CoverageWarning — NoContingentBeneficiaries", () => {
  it("renders title and default description", () => {
    renderWarning({ warningType: "NoContingentBeneficiaries" });

    expect(screen.getByText("No Contingent Beneficiaries")).toBeInTheDocument();
    expect(screen.getByText(/backup beneficiaries/i)).toBeInTheDocument();
  });
});

/* ------------------------------------------------------------------ */
/*  Custom message                                                     */
/* ------------------------------------------------------------------ */

describe("CoverageWarning — custom message", () => {
  it("uses custom message over default description", () => {
    renderWarning({
      warningType: "NoBeneficiaries",
      message: "Custom warning text",
    });

    expect(screen.getByText("Custom warning text")).toBeInTheDocument();
    expect(screen.queryByText(/subject to probate/i)).not.toBeInTheDocument();
  });
});

/* ------------------------------------------------------------------ */
/*  Resolve button                                                     */
/* ------------------------------------------------------------------ */

describe("CoverageWarning — resolve action", () => {
  it("renders Resolve button when onResolve is provided", () => {
    renderWarning({
      warningType: "NoBeneficiaries",
      onResolve: vi.fn(),
    });

    expect(screen.getByRole("button", { name: "Resolve" })).toBeInTheDocument();
  });

  it("does not render Resolve button when onResolve is omitted", () => {
    renderWarning({ warningType: "NoBeneficiaries" });

    expect(screen.queryByRole("button", { name: "Resolve" })).not.toBeInTheDocument();
  });

  it("calls onResolve when Resolve button is clicked", async () => {
    const user = userEvent.setup();
    const onResolve = vi.fn();
    renderWarning({ warningType: "IncompleteAllocation", onResolve });

    await user.click(screen.getByRole("button", { name: "Resolve" }));

    expect(onResolve).toHaveBeenCalledOnce();
  });
});
