import { describe, it, expect } from "vitest";
import { parseRoles } from "../useAuth";
import { satisfiesPolicy } from "../ProtectedRoute";
import type { AccountInfo } from "@azure/msal-browser";

/**
 * Creates a mock MSAL AccountInfo with the given roles in idTokenClaims.
 */
function mockAccount(roles?: string | string[]): AccountInfo {
  return {
    homeAccountId: "test-home-id",
    environment: "login.microsoftonline.com",
    tenantId: "test-tenant",
    username: "test@example.com",
    localAccountId: "test-local-id",
    idTokenClaims: roles !== undefined ? { roles } : {},
  } as AccountInfo;
}

describe("parseRoles", () => {
  it("returns empty array when account is null", () => {
    expect(parseRoles(null)).toEqual([]);
  });

  it("returns empty array when no roles claim exists", () => {
    const account = mockAccount();
    expect(parseRoles(account)).toEqual([]);
  });

  it("parses a single string role", () => {
    const account = mockAccount("Administrator");
    expect(parseRoles(account)).toEqual(["Administrator"]);
  });

  it("parses an array of roles", () => {
    const account = mockAccount(["Administrator", "Client"]);
    expect(parseRoles(account)).toEqual(["Administrator", "Client"]);
  });

  it("filters out empty strings in array", () => {
    const account = mockAccount(["Administrator", "", "  "]);
    expect(parseRoles(account)).toEqual(["Administrator"]);
  });

  it("trims whitespace from single string role", () => {
    const account = mockAccount("  Client  ");
    expect(parseRoles(account)).toEqual(["Client"]);
  });
});

describe("satisfiesPolicy", () => {
  it("RequireAuthenticated allows any authenticated user", () => {
    expect(satisfiesPolicy([], "RequireAuthenticated")).toBe(true);
    expect(satisfiesPolicy(["Client"], "RequireAuthenticated")).toBe(true);
    expect(satisfiesPolicy(["Administrator"], "RequireAuthenticated")).toBe(
      true
    );
  });

  it("RequireClient allows users with no role (implicit client)", () => {
    expect(satisfiesPolicy([], "RequireClient")).toBe(true);
  });

  it("RequireClient allows explicit Client role", () => {
    expect(satisfiesPolicy(["Client"], "RequireClient")).toBe(true);
  });

  it("RequireClient allows Administrator (admin can access client pages)", () => {
    expect(satisfiesPolicy(["Administrator"], "RequireClient")).toBe(true);
  });

  it("RequireClient allows AdminClient", () => {
    expect(satisfiesPolicy(["AdminClient"], "RequireClient")).toBe(true);
  });

  it("RequireAdministrator denies users with no role", () => {
    expect(satisfiesPolicy([], "RequireAdministrator")).toBe(false);
  });

  it("RequireAdministrator denies Client role", () => {
    expect(satisfiesPolicy(["Client"], "RequireAdministrator")).toBe(false);
  });

  it("RequireAdministrator allows Administrator role", () => {
    expect(satisfiesPolicy(["Administrator"], "RequireAdministrator")).toBe(
      true
    );
  });

  it("RequireAdministrator allows AdminAdvisor role", () => {
    expect(satisfiesPolicy(["AdminAdvisor"], "RequireAdministrator")).toBe(
      true
    );
  });

  it("RequireAdministrator allows AdminClient role", () => {
    expect(satisfiesPolicy(["AdminClient"], "RequireAdministrator")).toBe(
      true
    );
  });
});
