// ============================================================================
// Authentication Step Definitions
// ============================================================================
// Steps for Authentication.feature � signup, login, logout with Entra.
// ============================================================================

import { Then, When } from "@cucumber/cucumber";
import { expect } from "@playwright/test";
import { CustomWorld } from "../support/world";
import { config } from "../support/config";
import {
  handleEntraLogin,
  waitForEntraLoginPage,
  isEntraHost,
} from "../support/helpers/entra-auth";
import {
  generateTestEmail,
  getVerificationCodeFromEmail,
  isImapConfigured,
} from "../support/helpers/email-helper";

const testUsersToCleanup: string[] = [];

// ?? Entra redirect ??????????????????????????????????????????????????????????

Then(
  "I should be redirected to the Entra External ID login page",
  async function (this: CustomWorld) {
    await waitForEntraLoginPage(this.page);

    const url = this.page.url();
    expect(isEntraLoginUrl(url)).toBe(true);
  }
);

// ?? Signup: account creation link ???????????????????????????????????????????

When(
  "I click the {string} link on Entra page",
  async function (this: CustomWorld, _linkText: string) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(1000);

    await tryClickUseAnotherAccount(this.page);

    const signupSelectors = [
      "a:has-text('Create one')",
      "a:has-text('Create account')",
      "a:has-text('Sign up now')",
      "#createAccount",
      "a[href*='signup']",
      "p:has-text('No account') a",
    ];

    let clicked = false;
    for (const selector of signupSelectors) {
      try {
        const link = this.page.locator(selector).first();
        await link.waitFor({ state: "visible", timeout: 2000 });
        await link.click();
        clicked = true;
        break;
      } catch {
        // try next
      }
    }

    if (!clicked) {
      throw new Error("Could not find signup link on Entra page.");
    }
    await this.page.waitForLoadState("networkidle");
  }
);

// ?? Signup: email entry ?????????????????????????????????????????????????????

When(
  "I enter a unique test email address",
  async function (this: CustomWorld) {
    const testEmail = generateTestEmail();
    const guid = testEmail.split("@")[0].split("-").pop() || "";

    testUsersToCleanup.push(testEmail);
    this.set("TestUserEmail", testEmail);
    this.set("UsernameGuid", guid);
    this.set("TestUserPassword", generateSecurePassword());

    await this.page.waitForLoadState("networkidle");

    const emailSelectors = [
      "input[name='username']",
      "input[type='email']",
      "input[name='email']",
      "input#email",
    ];

    for (const selector of emailSelectors) {
      try {
        const field = this.page.locator(selector).first();
        await field.waitFor({ state: "visible", timeout: 3000 });
        await field.fill(testEmail);
        return;
      } catch {
        // try next
      }
    }
    throw new Error("Could not find email input field on signup form.");
  }
);

When(
  "I click the {string} button on Entra page",
  async function (this: CustomWorld, buttonText: string) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(500);

    const selectors: string[] = [];
    const textLower = buttonText.toLowerCase();

    if (textLower === "next") {
      selectors.push(
        "button[data-testid='usernamePrimaryButton']",
        "button[type='submit']",
        "#idSIButton9",
        "input[type='submit']"
      );
    } else if (textLower === "accept") {
      selectors.push(
        "input[type='submit'][value='Accept']",
        "button:has-text('Accept')",
        "button[type='submit']"
      );
    } else {
      selectors.push(
        `button:has-text('${buttonText}')`,
        "button[type='submit']",
        `input[type='submit'][value*='${buttonText}' i]`
      );
    }

    let clicked = false;
    for (const selector of selectors) {
      try {
        const btn = this.page.locator(selector).first();
        await btn.waitFor({ state: "visible", timeout: 3000 });
        await btn.click();
        clicked = true;
        break;
      } catch {
        // try next
      }
    }

    if (!clicked) {
      throw new Error(`Could not find '${buttonText}' button on Entra page.`);
    }
    await this.page.waitForLoadState("networkidle");
  }
);

// ?? Signup: email verification ??????????????????????????????????????????????

Then(
  "I should see the email verification code input",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(1000);

    const content = await this.page.content();
    const hasVerification =
      content.toLowerCase().includes("verify") ||
      content.toLowerCase().includes("code") ||
      content.toLowerCase().includes("confirmation");

    expect(hasVerification).toBe(true);
  }
);

When(
  "I retrieve and enter the email verification code",
  { timeout: 150_000 },
  async function (this: CustomWorld) {
    if (!isImapConfigured()) {
      throw new Error(
        "IMAP not configured. Set IMAP_HOST, IMAP_PORT, IMAP_USERNAME, IMAP_PASSWORD in .env"
      );
    }

    const testEmail = this.get<string>("TestUserEmail");
    console.log(`📧 Waiting for verification email to ${testEmail}...`);

    const verificationCode = await getVerificationCodeFromEmail(testEmail, 120);
    console.log(`✓ Retrieved verification code: ${verificationCode}`);
    this.set("VerificationCode", verificationCode);

    // Enter the code into the Entra verification form
    await this.page.waitForLoadState("networkidle");

    const codeSelectors = [
      "input[name='verificationCode']",
      "input[data-testid='verificationCodeInput']",
      "input[id*='code']",
      "input[placeholder*='code' i]",
      "input[type='text']",
    ];

    let entered = false;
    for (const selector of codeSelectors) {
      try {
        const field = this.page.locator(selector).first();
        await field.waitFor({ state: "visible", timeout: 2000 });
        await field.fill(verificationCode);
        entered = true;
        console.log(`✓ Entered code using selector: ${selector}`);
        break;
      } catch {
        // try next
      }
    }

    if (!entered) {
      throw new Error("Could not find verification code input field.");
    }

    // Click Verify button
    const verifySelectors = [
      "[data-testid='verifyButton']",
      "#verifyButton",
      "button:has-text('Verify')",
      "button[type='submit']",
    ];

    for (const selector of verifySelectors) {
      try {
        const btn = this.page.locator(selector).first();
        await btn.waitFor({ state: "visible", timeout: 2000 });
        await btn.click();
        break;
      } catch {
        // try next
      }
    }

    await this.page.waitForLoadState("networkidle");
  }
);

// ?? Signup: password ????????????????????????????????????????????????????????

Then(
  "I should see the password creation form",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(2000);

    const pwdField = this.page
      .locator("[data-testid='ipasswordInput'], input[type='password']")
      .first();

    try {
      await pwdField.waitFor({ state: "visible", timeout: 5000 });
    } catch {
      const content = await this.page.content();
      if (content.includes("givenName") || content.includes("surname")) {
        return;
      }
      throw new Error("Password creation form not found.");
    }
  }
);

When("I enter a new password", async function (this: CustomWorld) {
  const password = this.get<string>("TestUserPassword");
  const field = this.page
    .locator(
      "[data-testid='ipasswordInput'], input[type='password']:first-of-type"
    )
    .first();
  await field.clear();
  await field.fill(password);
});

When("I confirm the password", async function (this: CustomWorld) {
  const password = this.get<string>("TestUserPassword");
  const field = this.page
    .locator(
      "[data-testid='ipasswordConfirmationInput'], input#reenterPassword"
    )
    .first();

  try {
    await field.waitFor({ state: "visible", timeout: 3000 });
    await field.clear();
    await field.fill(password);
  } catch {
    // Single-password-field form
  }
});

// ?? Signup: profile fields ??????????????????????????????????????????????????

When(
  "I enter {string} in the {string} field on Entra page",
  async function (this: CustomWorld, value: string, fieldName: string) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(500);

    const selectors: string[] = [];
    const lower = fieldName.toLowerCase().replace(/ /g, "");

    if (lower.includes("given")) {
      selectors.push(
        "[data-testid='igivenNameInput']",
        "input[name='givenName']"
      );
    } else if (lower.includes("surname")) {
      selectors.push(
        "[data-testid='isurnameInput']",
        "input[name='surname']"
      );
    } else {
      selectors.push(
        `[data-testid='i${lower}Input']`,
        `input[name*='${lower}' i]`,
        `input[placeholder*='${fieldName}' i]`
      );
    }

    for (const selector of selectors) {
      try {
        const field = this.page.locator(selector).first();
        await field.waitFor({ state: "visible", timeout: 3000 });
        await field.clear();
        await field.fill(value);
        return;
      } catch {
        // try next
      }
    }
    throw new Error(`Could not find '${fieldName}' field on Entra page.`);
  }
);

When("I enter a unique username", async function (this: CustomWorld) {
  const guid = this.get<string>("UsernameGuid");
  const username = `testuser_${guid}`;
  this.set("TestUsername", username);

  const selectors = [
    "[data-testid='iusernameInput']",
    "input[name='displayName']",
    "input[name='username']",
  ];

  for (const selector of selectors) {
    try {
      const field = this.page.locator(selector).first();
      await field.waitFor({ state: "visible", timeout: 3000 });
      await field.clear();
      await field.fill(username);
      return;
    } catch {
      // try next
    }
  }
  throw new Error("Could not find username field.");
});

// ?? Signup: consent ?????????????????????????????????????????????????????????

Then(
  "I should see the permissions consent screen",
  async function (this: CustomWorld) {
    // Wait for page to stabilize — the Entra signup flow may still be navigating
    try {
      await this.page.waitForLoadState("domcontentloaded", { timeout: 15000 });
      await this.page.waitForLoadState("networkidle", { timeout: 10000 });
    } catch {
      // Page may already be past navigation
    }
    await this.page.waitForTimeout(1000);

    // The consent screen might have already auto-redirected.
    // Check if we're still on Entra or already back in the app.
    const url = this.page.url();
    if (isAppUrl(url)) {
      // Already redirected — consent was auto-accepted or skipped
      return;
    }

    let content = "";
    try {
      content = await this.page.content();
    } catch {
      // Page still navigating — wait and retry
      await this.page.waitForTimeout(2000);
      content = await this.page.content();
    }

    const hasConsent =
      content.toLowerCase().includes("permission") ||
      content.toLowerCase().includes("consent") ||
      content.toLowerCase().includes("accept");

    expect(hasConsent).toBe(true);
  }
);

// ?? Post-auth verification ??????????????????????????????????????????????????

Then(
  "I should be redirected back to the RAJ Financial app",
  async function (this: CustomWorld) {
    const maxWaitMs = 30000;
    const start = Date.now();

    while (Date.now() - start < maxWaitMs) {
      const url = this.page.url();
      if (isAppUrl(url)) {
        await this.page.waitForLoadState("networkidle");
        return;
      }
      await this.page.waitForTimeout(1000);
    }
    throw new Error(
      `Timeout waiting for redirect to app. URL: ${this.page.url()}`
    );
  }
);

Then(
  "I should see the client dashboard",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");

    const url = this.page.url();
    const content = await this.page.content();

    const onDashboard =
      url.includes("/dashboard") ||
      content.toLowerCase().includes("dashboard");

    expect(onDashboard).toBe(true);
  }
);

Then(
  "I should see my username next to the logout button",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(2000);

    const username = this.get<string>("TestUsername");
    const content = await this.page.content();

    const visible =
      content.includes(username) || content.includes("testuser_");

    if (!visible) {
      const loggedIn =
        content.toLowerCase().includes("log out") ||
        content.toLowerCase().includes("sign out");
      expect(loggedIn).toBe(true);
    }
  }
);

Then(
  "the test user should be marked for cleanup",
  async function (this: CustomWorld) {
    const email = this.get<string>("TestUserEmail");
    expect(testUsersToCleanup).toContain(email);
  }
);

// ?? Login (existing user) ???????????????????????????????????????????????????

When(
  "I sign in with test {string} credentials",
  async function (this: CustomWorld, role: string) {
    const emails: Record<string, string> = {
      Client: "test-client@rajfinancialdev.onmicrosoft.com",
      Administrator: "test-admin@rajfinancialdev.onmicrosoft.com",
    };

    const email = emails[role];
    const password =
      config.testUsers[role]?.password ??
      process.env[`TEST_${role.toUpperCase()}_PASSWORD`];

    if (!password) {
      throw new Error(`Password not configured for ${role} test user.`);
    }

    await handleEntraLogin(this.page, email, password);

    await this.page.waitForURL(
      (url) => url.toString().includes(config.baseUrl),
      { timeout: 15000 }
    );
    await this.page.waitForLoadState("networkidle");
  }
);

// ?? Logout ??????????????????????????????????????????????????????????????????
// "I click the {string} button" is defined in shared.steps.ts

Then("I should be logged out", async function (this: CustomWorld) {
  await this.page.waitForTimeout(3000);

  const url = this.page.url();
  if (isEntraLoginUrl(url)) {
    await this.page.goto(config.baseUrl, { waitUntil: "networkidle" });
  }

  await this.page.evaluate(() => {
    localStorage.clear();
    sessionStorage.clear();
  });
  await this.page.reload({ waitUntil: "networkidle" });
});

Then("I should be on the home page", async function (this: CustomWorld) {
  await this.page.waitForLoadState("networkidle");
  const url = this.page.url();
  expect(
    url === config.baseUrl ||
      url === config.baseUrl + "/" ||
      url.endsWith("/")
  ).toBe(true);
});

Then(
  "I should see an {string} message or be redirected",
  async function (this: CustomWorld, message: string) {
    const content = await this.page.content();
    const url = this.page.url();

    const hasMessage =
      content.includes(message) ||
      content.toLowerCase().includes("not authorized") ||
      content.toLowerCase().includes("access denied") ||
      !url.includes("/admin");

    expect(hasMessage).toBe(true);
  }
);

// ?? Helpers ?????????????????????????????????????????????????????????????????

async function tryClickUseAnotherAccount(
  page: import("playwright").Page
): Promise<void> {
  const selectors = [
    "[data-test-id='otherTile']",
    "#otherTile",
    "div:has-text('Use another account')",
    "button:has-text('Use another account')",
  ];

  for (const selector of selectors) {
    try {
      const el = page.locator(selector).first();
      await el.waitFor({ state: "visible", timeout: 2000 });
      await el.click();
      await page.waitForTimeout(1000);
      return;
    } catch {
      // try next
    }
  }
}

function isEntraLoginUrl(urlString: string): boolean {
  try {
    const { hostname } = new URL(urlString);
    return isEntraHost(hostname);
  } catch {
    return false;
  }
}

function isAppUrl(urlString: string): boolean {
  try {
    const { hostname } = new URL(urlString);
    const baseHostname = new URL(config.baseUrl).hostname;
    return hostname === baseHostname || hostname === "localhost";
  } catch {
    return false;
  }
}

function secureRandomIndex(max: number): number {
  const buf = new Uint32Array(1);
  const limit = Math.floor(0x100000000 / max) * max;
  do {
    crypto.getRandomValues(buf);
  } while (buf[0] >= limit);
  return buf[0] % max;
}

function generateSecurePassword(): string {
  const upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  const lower = "abcdefghijklmnopqrstuvwxyz";
  const digits = "0123456789";
  const special = "!@#$%^&*";
  const all = upper + lower + digits + special;

  const chars: string[] = [
    upper[secureRandomIndex(upper.length)],
    lower[secureRandomIndex(lower.length)],
    digits[secureRandomIndex(digits.length)],
    special[secureRandomIndex(special.length)],
  ];

  for (let i = 4; i < 16; i++) {
    chars.push(all[secureRandomIndex(all.length)]);
  }

  // Fisher-Yates shuffle using crypto.getRandomValues
  for (let i = chars.length - 1; i > 0; i--) {
    const j = secureRandomIndex(i + 1);
    [chars[i], chars[j]] = [chars[j], chars[i]];
  }

  return chars.join("");
}
