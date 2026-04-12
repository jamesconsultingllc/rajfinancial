// ============================================================================
// Entra External ID Authentication Helper
// ============================================================================
// Handles Microsoft Entra login flow for E2E tests.
// Works with both single-page and two-step Entra CIAM login forms.
// ============================================================================

import { Page } from "playwright";

/**
 * Performs interactive login on the Microsoft Entra External ID login page.
 * Handles email entry, password entry, "Stay signed in?" prompt, and
 * redirect back to the application.
 */
export async function handleEntraLogin(
  page: Page,
  email: string,
  password: string
): Promise<void> {
  await page.waitForLoadState("domcontentloaded").catch(() => {});
  await page.waitForTimeout(1000);

  // Handle "Pick an account" page when cached accounts exist
  const accountPickerHandled = await tryHandleAccountPicker(page, email);

  if (!accountPickerHandled) {
    // No account picker — proceed with email/password entry
    // Step 1: Email
    const emailSelectors = [
      "input[name='username']", // Entra External ID (CIAM)
      "input[data-testid='iusernameInput']",
      "input[type='email']",
      "input[name='loginfmt']", // Entra ID (B2B/workforce)
      "input[name='email']",
    ];

    const emailInput = await findVisibleLocator(page, emailSelectors, 5000);
    if (emailInput) {
      await emailInput.fill(email);

      // Check if password field is already visible (same-page flow)
      const pwdVisible = await page
        .locator(
          "input[data-testid='ipasswordInput'], input[type='password']"
        )
        .first()
        .isVisible()
        .catch(() => false);

      if (!pwdVisible) {
        // Two-step flow: click Next
        const next = page
          .locator(
            "input[type='submit'], button[type='submit'], button[name='idSIButton9']"
          )
          .first();
        await next.click();
        await page.waitForLoadState("domcontentloaded").catch(() => {});
        await page.waitForTimeout(1000);
      }
    }
  }

  // Step 2: Password
  const passwordSelectors = [
    "input[name='password']",
    "input[data-testid='ipasswordInput']",
    "input[type='password']",
    "input[name='passwd']",
  ];

  const passwordInput = await findVisibleLocator(page, passwordSelectors, 15000);
  if (!passwordInput) {
    throw new Error(`Password field not found. URL: ${page.url()}`);
  }
  await passwordInput.fill(password);

  // Step 3: Submit
  const submit = page
    .locator(
      "input[type='submit'], button[type='submit'], button:has-text('Sign in'), button[name='idSIButton9']"
    )
    .first();
  await submit.click();

  // Step 4: "Stay signed in?" prompt
  try {
    const noBtn = page
      .locator(
        "#idBtn_Back, #declineButton, button:has-text('No'), button:has-text(\"Don't show\")"
      )
      .first();
    await noBtn.waitFor({ state: "visible", timeout: 8000 });
    if (await noBtn.isVisible()) await noBtn.click();
  } catch {
    // Prompt not shown — continue
  }

  // Step 5: Wait for redirect back to app
  try {
    await page.waitForURL(
      (url) =>
        !url.toString().includes("ciamlogin.com") &&
        !url.toString().includes("login.microsoftonline.com") &&
        !url.toString().includes("b2clogin.com"),
      { timeout: 30000 }
    );
  } catch {
    console.warn(
      `⚠️ Login redirect did not complete within 30s. URL: ${page.url()}`
    );
  }
}

/**
 * Waits for the Entra External ID login page to appear after a redirect.
 */
export async function waitForEntraLoginPage(page: Page): Promise<void> {
  await page.waitForURL(
    (url) =>
      url.toString().includes("ciamlogin.com") ||
      url.toString().includes("login.microsoftonline.com") ||
      url.toString().includes("b2clogin.com"),
    { timeout: 30000 }
  );
}

/**
 * Handles the "Pick an account" page shown when Entra has cached accounts.
 * Tries to click the matching account tile directly, or falls back to
 * "Use another account" to get to the email/password form.
 *
 * @returns true if the account picker was found and handled
 */
async function tryHandleAccountPicker(
  page: Page,
  email: string
): Promise<boolean> {
  // Check if we're on the account picker page
  const pageContent = await page.content();
  const isAccountPicker =
    pageContent.includes("Pick an account") ||
    pageContent.includes("otherTile");

  if (!isAccountPicker) return false;

  console.log("ℹ Account picker detected — looking for matching account tile");

  // Try to click the account tile that matches the target email.
  // Entra renders tiles as clickable divs containing the email text.
  const tileSelectors = [
    `div[data-test-id] :text-is("${email}")`,
    `small:text-is("${email}")`,
    `div:has-text("${email}")`,
  ];

  for (const selector of tileSelectors) {
    try {
      const tile = page.locator(selector).first();
      const visible = await tile.isVisible().catch(() => false);
      if (visible) {
        console.log(`✓ Found account tile for ${email} — clicking`);
        await tile.click();
        await page.waitForLoadState("domcontentloaded").catch(() => {});
        await page.waitForTimeout(1000);
        return true;
      }
    } catch {
      // try next
    }
  }

  console.log("ℹ Account tile not found — trying 'Use another account'");

  // Account not in picker — click "Use another account" to get to email form
  const useAnotherSelectors = [
    "[data-test-id='otherTile']",
    "#otherTile",
    "div:has-text('Use another account')",
    "button:has-text('Use another account')",
    "a:has-text('Use another account')",
    "[aria-label*='Use another account']",
    "#otherTileText",
  ];

  for (const selector of useAnotherSelectors) {
    try {
      const el = page.locator(selector).first();
      await el.waitFor({ state: "visible", timeout: 2000 });
      console.log(`✓ Clicked 'Use another account' using selector: ${selector}`);
      await el.click();
      await page.waitForLoadState("domcontentloaded").catch(() => {});
      await page.waitForTimeout(1000);
      return false; // still need email/password entry
    } catch {
      // try next
    }
  }

  console.log("ℹ 'Use another account' not found — proceeding with current page");
  return false;
}

async function findVisibleLocator(
  page: Page,
  selectors: string[],
  timeoutMs: number
) {
  for (const selector of selectors) {
    try {
      const locator = page.locator(selector).first();
      await locator.waitFor({ state: "visible", timeout: timeoutMs });
      return locator;
    } catch {
      // try next
    }
  }
  return null;
}
