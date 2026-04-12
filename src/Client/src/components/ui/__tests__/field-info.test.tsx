import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { FieldInfo } from "../field-info";
import { TooltipProvider } from "@/components/ui/tooltip";
import { I18nextProvider } from "react-i18next";
import i18n from "i18next";
import { initReactI18next } from "react-i18next";

/** Create a test i18n instance with known translations */
function createTestI18n(translations: Record<string, string> = {}) {
  const testI18n = i18n.createInstance();
  testI18n.use(initReactI18next).init({
    lng: "en",
    resources: {
      en: {
        assets: translations,
      },
    },
    defaultNS: "assets",
    interpolation: { escapeValue: false },
  });
  return testI18n;
}

function renderFieldInfo(textKey: string, translations: Record<string, string>) {
  const testI18n = createTestI18n(translations);
  return render(
    <I18nextProvider i18n={testI18n}>
      <TooltipProvider>
        <FieldInfo textKey={textKey} />
      </TooltipProvider>
    </I18nextProvider>,
  );
}

describe("FieldInfo", () => {
  it("renders an info button when translation key exists", () => {
    renderFieldInfo("help.Vehicle.vin", {
      "help.Vehicle.vin": "17-character Vehicle ID Number",
    });

    const button = screen.getByRole("button", { name: "More info" });
    expect(button).toBeInTheDocument();
  });

  it("does not render when translation key is missing", () => {
    renderFieldInfo("help.nonexistent.key", {});

    expect(screen.queryByRole("button", { name: "More info" })).not.toBeInTheDocument();
  });

  it("button is keyboard-focusable", async () => {
    renderFieldInfo("help.Vehicle.vin", {
      "help.Vehicle.vin": "VIN help text",
    });

    const button = screen.getByRole("button", { name: "More info" });
    await userEvent.tab();
    expect(button).toHaveFocus();
  });

  it("has minimum 44x44 touch target via CSS classes", () => {
    renderFieldInfo("help.Vehicle.vin", {
      "help.Vehicle.vin": "VIN help text",
    });

    const button = screen.getByRole("button", { name: "More info" });
    expect(button.className).toContain("min-w-[44px]");
    expect(button.className).toContain("min-h-[44px]");
  });

  it("has aria-hidden on the icon", () => {
    renderFieldInfo("help.Vehicle.vin", {
      "help.Vehicle.vin": "VIN help text",
    });

    const button = screen.getByRole("button", { name: "More info" });
    const svg = button.querySelector("svg");
    expect(svg).toHaveAttribute("aria-hidden", "true");
  });
});
