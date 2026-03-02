// ============================================================================
// Admin Dashboard Step Definitions
// ============================================================================
// Steps for AdminDashboard.feature.
// NOTE: Admin routes (/admin/dashboard) are not yet implemented in React.
// These steps are scaffolded for when the admin pages are built.
// ============================================================================

import { Then } from "@cucumber/cucumber";
import { expect } from "@playwright/test";
import { CustomWorld } from "../support/world";

Then(
  "I should see the admin dashboard",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    await this.page.waitForTimeout(1000);

    const dashboard = this.page.locator("[data-testid='admin-dashboard']");
    const title = this.page.locator(
      "[data-testid='admin-dashboard-title'], h1:has-text('Administrator Dashboard')"
    );

    let found = false;
    try {
      await expect(dashboard).toBeVisible({ timeout: 15000 });
      found = true;
    } catch {
      try {
        await expect(title).toBeVisible({ timeout: 5000 });
        found = true;
      } catch {
        // neither found
      }
    }
    expect(found).toBe(true);
  }
);

Then(
  "I should not see an access denied message",
  async function (this: CustomWorld) {
    const content = await this.page.content();
    expect(content).not.toContain("Access Denied");
    expect(content.toLowerCase()).not.toContain("not authorized");
  }
);

Then(
  "I should see an access denied message",
  async function (this: CustomWorld) {
    const content = await this.page.content();
    expect(
      content.includes("Access Denied") ||
        content.toLowerCase().includes("not authorized")
    ).toBe(true);
  }
);

Then(
  "I should see activity items in the recent activity section",
  async function (this: CustomWorld) {
    const items = await this.page.locator("li").count();
    expect(items).toBeGreaterThanOrEqual(1);
  }
);

Then(
  "the statistics cards should be stacked vertically",
  async function (this: CustomWorld) {
    const cards = await this.page.locator(".card").all();
    if (cards.length >= 2) {
      const box1 = await cards[0].boundingBox();
      const box2 = await cards[1].boundingBox();
      expect(box1).not.toBeNull();
      expect(box2).not.toBeNull();
      expect(box2!.y).toBeGreaterThan(box1!.y);
    }
  }
);

Then(
  "the quick actions should be visible",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    const quickActions = this.page.locator(
      "[data-testid='quick-actions'], h5:has-text('Quick Actions')"
    );
    await quickActions.first().scrollIntoViewIfNeeded();
    await expect(quickActions.first()).toBeVisible({ timeout: 20000 });
  }
);

Then(
  "the statistics cards should adapt to tablet layout",
  async function (this: CustomWorld) {
    await this.page.waitForLoadState("networkidle");
    const cards = this.page.locator(".grid .card");
    await expect(cards.first()).toBeVisible();
    expect(await cards.count()).toBeGreaterThanOrEqual(1);
  }
);

Then(
  "all statistics should have accessible labels",
  async function (this: CustomWorld) {
    const cards = await this.page.locator(".card").all();
    for (const card of cards.slice(0, 4)) {
      const labels = await card
        .locator("h1, h2, h3, h4, h5, h6, label, [aria-label]")
        .count();
      expect(labels).toBeGreaterThanOrEqual(1);
    }
  }
);

Then(
  "all icons should be hidden from screen readers or have labels",
  async function (this: CustomWorld) {
    const icons = await this.page.locator("svg, i[class*='icon']").all();
    for (const icon of icons) {
      const ariaHidden = await icon.getAttribute("aria-hidden");
      const ariaLabel = await icon.getAttribute("aria-label");
      const title = await icon.getAttribute("title");

      expect(
        ariaHidden === "true" ||
          (ariaLabel?.trim().length ?? 0) > 0 ||
          (title?.trim().length ?? 0) > 0
      ).toBe(true);
    }
  }
);
