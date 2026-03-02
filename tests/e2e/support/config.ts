// ============================================================================
// E2E Test Configuration
// ============================================================================
// Loads settings from environment variables and .env file.
// ============================================================================

import * as dotenv from "dotenv";
import * as path from "path";

dotenv.config({ path: path.resolve(__dirname, "../.env") });

export interface TestUserConfig {
  email: string;
  password?: string;
  storageStatePath?: string;
}

export const config = {
  baseUrl: process.env.BASE_URL || "https://localhost:8080",

  browser: (process.env.BROWSER || "chromium") as
    | "chromium"
    | "firefox"
    | "webkit",

  headed: process.env.HEADED === "true",

  /** Test users keyed by role */
  testUsers: {
    Client: {
      email: "test-client@rajfinancialdev.onmicrosoft.com",
      password: process.env.TEST_CLIENT_PASSWORD,
      storageStatePath: process.env.TEST_CLIENT_STORAGE_STATE,
    },
    Administrator: {
      email: "test-admin@rajfinancialdev.onmicrosoft.com",
      password: process.env.TEST_ADMINISTRATOR_PASSWORD,
      storageStatePath: process.env.TEST_ADMINISTRATOR_STORAGE_STATE,
    },
  } as Record<string, TestUserConfig>,

  imap: {
    host: process.env.IMAP_HOST,
    port: parseInt(process.env.IMAP_PORT || "993", 10),
    username: process.env.IMAP_USERNAME,
    password: process.env.IMAP_PASSWORD,
  },
};
