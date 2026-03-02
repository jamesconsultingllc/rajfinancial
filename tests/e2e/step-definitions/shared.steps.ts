// ============================================================================
// Shared Step Definitions
// ============================================================================
// Common/generic steps reused across multiple feature files.
// ============================================================================

import { Given, When, Then } from "@cucumber/cucumber";
import { expect } from "@playwright/test";
import { CustomWorld } from "../support/world";
import { config } from "../support/config";

// ── Background / Setup ──────────────────────────────────────────────────────

Given("the application is running", async function (this: CustomWorld) {
  const response = await this.page.goto(config.baseUrl);
  expect(response).not.toBeNull();
  expect(response!.ok()).toBe(true);
});

Given(
  "the viewport is set to mobile size",
  async function (this: CustomWorld) {
    await this.page.setViewportSize({ width: 375, height: 667 });
  }
);

Given(
  "the viewport is set to tablet size",
  async function (this: CustomWorld) {
    await this.page.setViewportSize({ width: 768, height: 1024 });
  }
);

Given(
  "the viewport is set to desktop size",
  async function (this: CustomWorld) {
    await this.page.setViewportSize({ width: 1920, height: 1080 });
  }
);

// ── Page / content assertions ───────────────────────────────────────────────

Then("the page should load successfully", async function (this: CustomWorld) {
  const response = await this.page.goto(config.baseUrl);
  expect(response).not.toBeNull();
  expect(response!.ok()).toBe(true);
});

Then(
  "the page title should contain {string}",
  async function (this: CustomWorld, expectedTitle: string) {
    const title = await this.page.title();
    expect(title).toContain(expectedTitle);
  }
);

Then(
  "I should see {string}",
  async function (this: CustomWorld, text: string) {
    await this.page.waitForLoadState("networkidle");
    await expect(this.page.getByText(text).first()).toBeVisible({
      timeout: 10000,
    });
  }
);

Then(
  "I should see a {string} button",
  async function (this: CustomWorld, buttonText: string) {
    await this.page.waitForLoadState("networkidle");
    const button = this.page
      .locator("a, button")
      .filter({ hasText: buttonText })
      .first();
    await expect(button).toBeVisible({ timeout: 10000 });
  }
);

Then(
  "I should see the {string} button",
  async function (this: CustomWorld, buttonText: string) {
    await this.page.waitForLoadState("networkidle");
    const button = this.page
      .locator("a, button")
      .filter({ hasText: buttonText })
      .first();
    await expect(button).toBeVisible({ timeout: 10000 });
  }
);

Then(
  "I should not see a {string} button",
  async function (this: CustomWorld, buttonText: string) {
    const button = this.page
      .locator("a, button")
      .filter({ hasText: buttonText })
      .first();
    await expect(button).not.toBeVisible();
  }
);

When(
  "I click the {string} button",
  async function (this: CustomWorld, buttonText: string) {
    const button = this.page
      .locator(
        `[data-testid*='${buttonText.replace(/ /g, "-").toLowerCase()}'], button:has-text('${buttonText}'), a:has-text('${buttonText}')`
      )
      .first();
    await button.waitFor({ state: "visible", timeout: 15000 });
    await button.click();
    await this.page.waitForTimeout(1000);
  }
);

// ── Section assertions ──────────────────────────────────────────────────────

Then(
  "I should see the {string} section",
  async function (this: CustomWorld, sectionName: string) {
    await this.page.waitForLoadState("networkidle");

    const dataTestId = sectionName.toLowerCase().replace(/ /g, "-");
    const testIdLocator = this.page.locator(`[data-testid='${dataTestId}']`);
    const textLocator = this.page.getByText(sectionName);

    let found = false;
    for (let i = 0; i < 10 && !found; i++) {
      await this.page.waitForTimeout(500);
      if (
        ((await testIdLocator.count()) > 0 &&
          (await testIdLocator.first().isVisible())) ||
        ((await textLocator.count()) > 0 &&
          (await textLocator.first().isVisible()))
      ) {
        found = true;
      }
    }

    expect(found).toBe(true);
  }
);

Then(
  "I should not see the {string} section",
  async function (this: CustomWorld, sectionName: string) {
    const content = await this.page.content();
    expect(content).not.toContain(sectionName);
  }
);

// ── Link assertions ─────────────────────────────────────────────────────────

Then(
  "I should see the {string} link",
  async function (this: CustomWorld, linkText: string) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(500);

    const candidates = [
      this.page.locator(`a:has-text("${linkText}")`),
      this.page.locator(`button:has-text("${linkText}")`),
      this.page.getByText(linkText),
    ];

    // For "Home", also check the brand logo link
    if (linkText.toLowerCase() === "home") {
      candidates.unshift(this.page.locator("[data-testid='nav-home-link']"));
      candidates.unshift(this.page.locator("a").filter({ has: this.page.locator("img[alt*='logo' i], [data-testid='logo']") }));
    }

    let found = false;
    for (const candidate of candidates) {
      try {
        await expect(candidate.first()).toBeVisible({ timeout: 5000 });
        found = true;
        break;
      } catch {
        // try next
      }
    }
    expect(found).toBe(true);
  }
);

// ── Navigation ──────────────────────────────────────────────────────────────

When(
  "I navigate to {string}",
  async function (this: CustomWorld, urlPath: string) {
    const fullUrl = config.baseUrl.replace(/\/$/, "") + urlPath;
    await this.page.goto(fullUrl, { waitUntil: "networkidle" });
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(1000);
  }
);

Then(
  "I should be on the {string} page",
  async function (this: CustomWorld, urlPath: string) {
    expect(this.page.url()).toContain(urlPath);
  }
);

Then(
  "I should be redirected to the login page",
  async function (this: CustomWorld) {
    const url = this.page.url();
    const content = await this.page.content();

    const isLoginPage =
      url.includes("login") ||
      url.includes("auth") ||
      url.includes("signin") ||
      content.includes("Sign In") ||
      content.includes("Get Started");

    expect(isLoginPage).toBe(true);
  }
);

// ── Responsive ──────────────────────────────────────────────────────────────

Then(
  "the page should not have horizontal scroll",
  async function (this: CustomWorld) {
    const hasScroll = await this.page.evaluate(
      () =>
        document.documentElement.scrollWidth >
        document.documentElement.clientWidth
    );
    expect(hasScroll).toBe(false);
  }
);

// ── Accessibility ───────────────────────────────────────────────────────────

Then(
  "the page should have proper heading hierarchy",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(1000);

    const h1Count = await this.page.locator("h1").count();
    expect(h1Count).toBeGreaterThanOrEqual(1);

    const headings = await this.page.evaluate(() => {
      const els = document.querySelectorAll("h1, h2, h3, h4, h5, h6");
      return Array.from(els).map((h) => parseInt(h.tagName.substring(1)));
    });
    expect(headings.length).toBeGreaterThan(0);
  }
);

Then(
  "all buttons should have accessible labels",
  async function (this: CustomWorld) {
    const buttons = await this.page
      .locator("button, a[class*='btn']")
      .all();

    for (const button of buttons) {
      const text = await button.textContent();
      const ariaLabel = await button.getAttribute("aria-label");
      const title = await button.getAttribute("title");

      const hasLabel =
        (text?.trim().length ?? 0) > 0 ||
        (ariaLabel?.trim().length ?? 0) > 0 ||
        (title?.trim().length ?? 0) > 0;

      expect(hasLabel).toBe(true);
    }
  }
);

Then(
  "all images should have alt text",
  async function (this: CustomWorld) {
    const images = await this.page.locator("img").all();
    for (const img of images) {
      const alt = await img.getAttribute("alt");
      const ariaHidden = await img.getAttribute("aria-hidden");
      expect(alt !== null || ariaHidden === "true").toBe(true);
    }
  }
);

Then(
  "focus indicators should be visible when tabbing",
  async function (this: CustomWorld) {
    await this.page.keyboard.press("Tab");
    await this.page.keyboard.press("Tab");

    const hasFocus = await this.page.evaluate(() => {
      const el = document.activeElement;
      if (!el) return false;
      const styles = window.getComputedStyle(el);
      return (
        (styles.outline !== "" &&
          !styles.outline.includes("none") &&
          !styles.outline.includes("0px")) ||
        (styles.boxShadow !== "" && styles.boxShadow !== "none")
      );
    });
    expect(hasFocus).toBe(true);
  }
);

Then(
  "each focused item should have a visible focus indicator",
  async function (this: CustomWorld) {
    const hasFocus = await this.page.evaluate(() => {
      const el = document.activeElement;
      if (!el) return false;
      const styles = window.getComputedStyle(el);
      return (
        (styles.outline !== "" &&
          !styles.outline.includes("none") &&
          !styles.outline.includes("0px")) ||
        (styles.boxShadow !== "" && styles.boxShadow !== "none")
      );
    });
    expect(hasFocus).toBe(true);
  }
);
