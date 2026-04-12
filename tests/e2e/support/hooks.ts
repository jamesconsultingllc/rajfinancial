// ============================================================================
// Playwright Browser Hooks
// ============================================================================
// Manages browser lifecycle, context/page per scenario, and screenshots
// on failure. Mirrors the C# PlaywrightHooks but in TypeScript.
// ============================================================================

import {
  BeforeAll,
  AfterAll,
  Before,
  After,
  setWorldConstructor,
  setDefaultTimeout,
  Status,
} from "@cucumber/cucumber";
import { chromium, firefox, webkit, Browser, LaunchOptions } from "playwright";
import { CustomWorld } from "./world";
import { config } from "./config";
import {
  handleEntraLogin,
  waitForEntraLoginPage,
} from "./helpers/entra-auth";
import * as fs from "fs";
import * as path from "path";

setWorldConstructor(CustomWorld);

// Default step timeout — must exceed any Playwright assertion timeouts
setDefaultTimeout(30_000);

let browser: Browser;

/** Cached storage state paths keyed by role, generated in BeforeAll. */
const storageStates: Record<string, string> = {};

/** Launches the browser configured via BROWSER env var. */
async function launchConfiguredBrowser(
  options: LaunchOptions
): Promise<Browser> {
  switch (config.browser) {
    case "firefox":
      return firefox.launch(options);
    case "webkit":
      return webkit.launch(options);
    default:
      return chromium.launch(options);
  }
}

export function getBrowser(): Browser {
  return browser;
}

export function getStorageStatePath(role: string): string | undefined {
  return storageStates[role];
}

BeforeAll({ timeout: 120_000 }, async function () {
  const launchOptions: LaunchOptions = {
    headless: !config.headed,
  };

  browser = await launchConfiguredBrowser(launchOptions);

  console.log(
    `🚀 Browser: ${config.browser}, Headless: ${!config.headed}, Base URL: ${config.baseUrl}`
  );

  // Pre-authenticate each configured role and save storage state.
  // Mirrors C# PlaywrightHooks.BeforeTestRun — avoids hitting Entra
  // on every scenario.
  const stateDir = path.resolve(__dirname, "../.auth");
  fs.mkdirSync(stateDir, { recursive: true });

  for (const [role, userConfig] of Object.entries(config.testUsers)) {
    if (!userConfig.password) continue;

    const storagePath = path.join(stateDir, `${role.toLowerCase()}.json`);

    try {
      console.log(`🔐 Pre-authenticating ${role}...`);
      const context = await browser.newContext({
        viewport: { width: 1280, height: 720 },
        ignoreHTTPSErrors: true,
      });
      const page = await context.newPage();

      await page.goto(config.baseUrl);
      await page.waitForTimeout(1000);

      // Click Sign In
      const btn = page.locator("button:has-text('Sign In')").first();
      if (await btn.isVisible()) {
        await btn.click();
      }

      await waitForEntraLoginPage(page);
      await handleEntraLogin(page, userConfig.email, userConfig.password);

      // Wait for authenticated state
      await page.waitForLoadState("domcontentloaded").catch(() => {});
      const maxAttempts = 30;
      for (let i = 0; i < maxAttempts; i++) {
        await page.waitForTimeout(500);
        const url = page.url();
        if (url.includes("/dashboard") || url.includes("/admin")) break;
        const logoutVisible = await page
          .locator("text=/Log out/i")
          .first()
          .isVisible()
          .catch(() => false);
        if (logoutVisible) break;
      }

      // Save storage state (MSAL tokens are in localStorage)
      await context.storageState({ path: storagePath });
      storageStates[role] = storagePath;
      console.log(`✅ ${role} storage state saved → ${storagePath}`);

      await context.close();
    } catch (err) {
      console.warn(
        `⚠️ Pre-auth failed for ${role}: ${err instanceof Error ? err.message : err}`
      );
      // Tests will fall back to interactive login
    }
  }
});

// Skip @signup scenarios on WebKit — Entra CIAM's JS AJAX calls fail due to
// SSL incompatibilities in WebKit's headless engine, causing the signup flow
// to error after email verification (AADSTS50021).
Before({ tags: "@signup" }, async function (this: CustomWorld, scenario) {
  if (config.browser === "webkit") {
    console.log(
      `⏭️  Skipping "${scenario.pickle.name}" — Entra CIAM signup flow is incompatible with WebKit`
    );
    return "skipped";
  }
});

Before({ timeout: 30_000 }, async function (this: CustomWorld) {
  try {
    this.context = await browser.newContext({
      viewport: { width: 1280, height: 720 },
      ignoreHTTPSErrors: true,
    });
    this.page = await this.context.newPage();
  } catch (error) {
    // Browser may have crashed — relaunch
    console.warn("⚠️ Browser crashed — re-launching...");
    const launchOptions: LaunchOptions = { headless: !config.headed };
    browser = await launchConfiguredBrowser(launchOptions);
    this.context = await browser.newContext({
      viewport: { width: 1280, height: 720 },
      ignoreHTTPSErrors: true,
    });
    this.page = await this.context.newPage();
    console.log("✅ Browser re-launched successfully.");
  }
});

After(async function (this: CustomWorld, { result, pickle }) {
  if (result?.status === Status.FAILED && this.page) {
    try {
      const screenshotDir = path.resolve(__dirname, "../reports/screenshots");
      fs.mkdirSync(screenshotDir, { recursive: true });

      const safeName = pickle.name.replace(/[^a-zA-Z0-9]/g, "_");
      const timestamp = new Date().toISOString().replace(/[:.]/g, "-");
      const filePath = path.join(
        screenshotDir,
        `${safeName}_${timestamp}.png`
      );

      await this.page.screenshot({ path: filePath, fullPage: true });
      console.log(`📸 Failure screenshot: ${filePath}`);
    } catch {
      console.warn("⚠️ Could not capture failure screenshot.");
    }
  }

  try {
    await this.page?.close();
  } catch {
    /* already closed */
  }
  try {
    await this.context?.close();
  } catch {
    /* already closed */
  }
});

AfterAll(async function () {
  try {
    await browser?.close();
  } catch {
    /* already dead */
  }
});
