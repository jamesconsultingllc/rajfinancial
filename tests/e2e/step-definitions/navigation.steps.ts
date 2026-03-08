// ============================================================================
// Navigation Step Definitions
// ============================================================================
// Steps for Navigation.feature — tests role-based sidebar navigation.
// ============================================================================

import { Given, When, Then } from "@cucumber/cucumber";
import { expect } from "@playwright/test";
import { CustomWorld } from "../support/world";
import { config } from "../support/config";
import {
  handleEntraLogin,
  waitForEntraLoginPage,
} from "../support/helpers/entra-auth";
import { getBrowser, getStorageStatePath } from "../support/hooks";

const testUserEmails: Record<string, string> = {
  Client: "test-client@rajfinancialdev.onmicrosoft.com",
  Administrator: "test-admin@rajfinancialdev.onmicrosoft.com",
};

// ── Login steps ─────────────────────────────────────────────────────────────

Given(
  "I am logged in as a {string}",
  { timeout: 60_000 },
  async function (this: CustomWorld, role: string) {
    await loginAsRole.call(this, role);
  }
);

Given(
  "I am logged in as an {string}",
  { timeout: 60_000 },
  async function (this: CustomWorld, role: string) {
    await loginAsRole.call(this, role);
  }
);

async function loginAsRole(this: CustomWorld, role: string) {
  this.set("UserRole", role);

  const email = testUserEmails[role];
  if (!email) {
    throw new Error(
      `Unknown role: '${role}'. Valid: ${Object.keys(testUserEmails).join(", ")}`
    );
  }
  this.set("LoggedInEmail", email);

  // Try cached storage state first (pre-generated in BeforeAll).
  // This avoids hitting Entra on every scenario — mirrors C# PlaywrightHooks.
  const cachedState = getStorageStatePath(role);
  if (cachedState) {
    const fs = await import("fs");
    if (fs.existsSync(cachedState)) {
      const viewport = this.page.viewportSize() ?? {
        width: 1280,
        height: 720,
      };
      await this.page.close();
      await this.context.close();

      this.context = await getBrowser().newContext({
        storageState: cachedState,
        viewport,
        ignoreHTTPSErrors: true,
      });
      this.page = await this.context.newPage();

      await this.page.goto(config.baseUrl, { waitUntil: "networkidle" });
      await this.page.waitForTimeout(1000);

      // Verify we're actually authenticated
      const logoutVisible = await this.page
        .locator("text=/Log out/i")
        .first()
        .isVisible()
        .catch(() => false);
      const onDashboard = this.page.url().includes("/dashboard");

      if (logoutVisible || onDashboard) return;
      // Storage state stale — fall through to interactive login
      console.warn(`⚠️ Cached state for ${role} is stale — falling back to interactive login`);
    }
  }

  // Also try user-configured storage state path from .env
  const storageStatePath = config.testUsers[role]?.storageStatePath;
  if (storageStatePath) {
    const fs = await import("fs");
    if (fs.existsSync(storageStatePath)) {
      const viewport = this.page.viewportSize() ?? {
        width: 1280,
        height: 720,
      };
      await this.page.close();
      await this.context.close();

      this.context = await getBrowser().newContext({
        storageState: storageStatePath,
        viewport,
        ignoreHTTPSErrors: true,
      });
      this.page = await this.context.newPage();

      await this.page.goto(config.baseUrl, { waitUntil: "networkidle" });
      await this.page.waitForTimeout(1000);

      const logoutVisible = await this.page
        .locator("text=/Log out/i")
        .first()
        .isVisible()
        .catch(() => false);
      const onDashboard = this.page.url().includes("/dashboard");

      if (logoutVisible || onDashboard) return;
    }
  }

  // Interactive login — last resort
  const password =
    config.testUsers[role]?.password ??
    process.env[`TEST_${role.toUpperCase()}_PASSWORD`];

  if (!password) {
    throw new Error(
      `Password not configured for role '${role}'. ` +
        `Set TEST_${role.toUpperCase()}_PASSWORD in .env or environment.`
    );
  }

  await this.page.goto(config.baseUrl);
  await this.page.waitForTimeout(1000);

  // Click Sign In — try multiple selectors since mobile/desktop render
  // different buttons. Use CSS visibility to find the one actually shown.
  const loginSelectors = [
    "button:visible:has-text('Sign In')",
    "a:visible:has-text('Sign In')",
    "button:has-text('Sign In')",
    "a[href*='authentication/login']",
    "a[href*='authentication/register']",
  ];

  let clicked = false;
  for (const selector of loginSelectors) {
    try {
      const btn = this.page.locator(selector).first();
      if (await btn.isVisible()) {
        await btn.click();
        clicked = true;
        break;
      }
    } catch {
      // try next
    }
  }

  if (!clicked) {
    throw new Error(
      `Sign In button not found on ${this.page.url()}. ` +
        `Viewport: ${JSON.stringify(this.page.viewportSize())}`
    );
  }

  await waitForEntraLoginPage(this.page);
  await handleEntraLogin(this.page, email, password);
  await waitForAuthenticatedState(this.page);
}

Given("I am not logged in", async function (this: CustomWorld) {
  // Clear any existing auth state
  await this.page.goto(config.baseUrl);
  await this.page.evaluate(() => {
    localStorage.clear();
    sessionStorage.clear();
  });
  await this.page.reload({ waitUntil: "networkidle" });
  await this.page.waitForTimeout(1000);
});

// ── Navigation steps ────────────────────────────────────────────────────────

When(
  "I view the navigation menu",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(1000);

    // On mobile, open hamburger menu
    const viewport = this.page.viewportSize();
    if (viewport && viewport.width < 768) {
      const hamburger = this.page
        .locator(
          "[data-testid='mobile-menu-toggle'], [aria-label*='menu' i]"
        )
        .first();
      if (await hamburger.isVisible()) {
        await hamburger.click();
        await this.page.waitForTimeout(500);
      }
    }
  }
);

Then(
  "I should see the {string} link in admin section",
  async function (this: CustomWorld, linkText: string) {
    const adminSection = this.page
      .locator("nav")
      .filter({ hasText: "Administration" });
    const link = adminSection.locator(`text=${linkText}`);
    await expect(link).toBeVisible();
  }
);

When(
  "I press Tab multiple times",
  async function (this: CustomWorld) {
    for (let i = 0; i < 5; i++) {
      await this.page.keyboard.press("Tab");
      await this.page.waitForTimeout(100);
    }
  }
);

Then(
  "I should be able to navigate through menu items",
  async function (this: CustomWorld) {
    const tag = await this.page.evaluate(
      () => document.activeElement?.tagName
    );
    expect(tag).not.toBeNull();
    expect(tag!.toUpperCase()).not.toBe("BODY");
  }
);

Then(
  "I should see a hamburger menu button",
  async function (this: CustomWorld) {
    const hamburger = this.page
      .locator("[data-testid='mobile-menu-toggle']")
      .first();
    await expect(hamburger).toBeVisible();
  }
);

When(
  "I click the hamburger menu button",
  async function (this: CustomWorld) {
    const hamburger = this.page
      .locator("[data-testid='mobile-menu-toggle']")
      .first();
    await hamburger.click();
    await this.page.waitForTimeout(300);
  }
);

Then(
  "the navigation menu should be visible",
  async function (this: CustomWorld) {
    const sidebar = this.page.locator("[data-testid='sidebar']");
    await expect(sidebar.first()).toBeVisible();
  }
);

// ── Helpers ─────────────────────────────────────────────────────────────────

async function waitForAuthenticatedState(page: import("playwright").Page) {
  await page.waitForLoadState("networkidle");

  const maxAttempts = 30;
  for (let i = 0; i < maxAttempts; i++) {
    await page.waitForTimeout(500);

    const url = page.url();
    if (
      url.includes("/dashboard") ||
      url.includes("/admin") ||
      url.includes("/advisor")
    ) {
      break;
    }

    const logoutVisible = await page
      .getByText(/log out/i)
      .first()
      .isVisible()
      .catch(() => false);
    const sidebarVisible = await page
      .locator("[data-testid='sidebar'], nav[aria-label]")
      .first()
      .isVisible()
      .catch(() => false);

    if (logoutVisible || sidebarVisible) break;
  }

  await page.waitForLoadState("networkidle");
  await page.waitForTimeout(1000);
}
