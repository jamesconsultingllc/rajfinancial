// ============================================================================
// Custom Cucumber World — shared Playwright state per scenario
// ============================================================================

import { World, IWorldOptions } from "@cucumber/cucumber";
import { Page, BrowserContext } from "playwright";

export class CustomWorld extends World {
  page!: Page;
  context!: BrowserContext;

  /** Arbitrary scenario-scoped storage (replaces ScenarioContext from C#) */
  state: Record<string, unknown> = {};

  constructor(options: IWorldOptions) {
    super(options);
  }

  get<T>(key: string): T {
    const value = this.state[key];
    if (value === undefined) {
      throw new Error(`Key '${key}' not found in scenario state`);
    }
    return value as T;
  }

  set(key: string, value: unknown): void {
    this.state[key] = value;
  }
}
