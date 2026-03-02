// ============================================================================
// Home Page Step Definitions
// ============================================================================
// Steps for HomePage.feature — tests the public landing page.
// Selectors target React components with data-testid attributes.
// ============================================================================

import { Given, Then } from "@cucumber/cucumber";
import { expect } from "@playwright/test";
import { CustomWorld } from "../support/world";
import { config } from "../support/config";

Given("I am on the home page", async function (this: CustomWorld) {
  await this.page.goto(config.baseUrl, { waitUntil: "networkidle" });
  // Wait for React to render
  await this.page.waitForTimeout(1000);
});

Then(
  "I should see the brand name {string}",
  async function (this: CustomWorld, brandName: string) {
    // Check logo alt text — brand name appears on the <img> element, not as visible text
    await expect(
      this.page.getByAltText(brandName).first()
    ).toBeVisible({ timeout: 5000 });
  }
);

Then(
  "I should see the tagline {string}",
  async function (this: CustomWorld, tagline: string) {
    // Tagline text spans multiple elements inside the hero <h1>.
    // innerText respects CSS layout (block spans → newlines) whereas
    // textContent concatenates without any separator.
    const hero = this.page.locator("[data-testid='hero-section']");
    await expect(hero).toBeVisible({ timeout: 5000 });
    const heroText = await hero.innerText();
    const normalised = heroText.replace(/\s+/g, " ");
    if (!normalised.includes(tagline)) {
      throw new Error(
        `Expected hero section to contain tagline "${tagline}" but got: "${normalised.substring(0, 300)}…"`
      );
    }
  }
);

Then("I should see the hero section", async function (this: CustomWorld) {
  const hero = this.page.locator("[data-testid='hero-section']");
  await expect(hero).toBeVisible({ timeout: 5000 });
});

Then(
  "I should see at least {int} feature cards",
  async function (this: CustomWorld, count: number) {
    const cards = this.page.locator("[data-testid='feature-card']");
    const actual = await cards.count();
    expect(actual).toBeGreaterThanOrEqual(count);
  }
);

Then(
  "I should see features describing the platform benefits",
  async function (this: CustomWorld) {
    const cards = await this.page
      .locator("[data-testid='feature-card']")
      .count();
    expect(cards).toBeGreaterThanOrEqual(1);
  }
);

Then("I should see the CTA section", async function (this: CustomWorld) {
  const cta = this.page.locator("[data-testid='cta-section']");
  await expect(cta).toBeVisible({ timeout: 5000 });
});

Then(
  "the CTA section should encourage users to sign up",
  async function (this: CustomWorld) {
    const ctaText = await this.page
      .locator("[data-testid='cta-section']")
      .textContent();
    expect(ctaText).not.toBeNull();
    expect(ctaText!.length).toBeGreaterThan(10);
  }
);

Then(
  "the hero section should be visible",
  async function (this: CustomWorld) {
    const hero = this.page.locator("[data-testid='hero-section']");
    await expect(hero).toBeVisible({ timeout: 5000 });
  }
);

Then(
  "the navigation should be accessible",
  async function (this: CustomWorld) {
    const hamburger = await this.page
      .locator(
        "[aria-label*='menu' i], [aria-label*='Menu'], [data-testid='mobile-menu-toggle']"
      )
      .count();
    const nav = await this.page.locator("nav").count();
    expect(hamburger >= 1 || nav >= 1).toBe(true);
  }
);

Then(
  "the feature cards should be visible",
  async function (this: CustomWorld) {
    const cards = await this.page
      .locator("[data-testid='feature-card']")
      .count();
    expect(cards).toBeGreaterThanOrEqual(1);
  }
);
