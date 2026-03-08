/**
 * Tests for the beneficiary assignment service.
 *
 * @description Covers CRUD functions and TanStack Query hooks for
 * managing asset-contact links (beneficiary assignments).
 */
import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { PropsWithChildren } from "react";
import React from "react";

/**
 * Because the service module mutates a module-level `assetLinksStore`,
 * we re-import a fresh copy for each test via `vi.importActual` after
 * resetting the module registry.
 */

/* Seed IDs used across tests */
const SEED_ASSET_ID = "a1b2c3d4-e5f6-7890-abcd-ef1234567890";
const UNKNOWN_ASSET_ID = "unknown-asset-id";

/* ------------------------------------------------------------------ */
/*  Helpers                                                            */
/* ------------------------------------------------------------------ */

function createQueryWrapper() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  function Wrapper({ children }: PropsWithChildren) {
    return React.createElement(QueryClientProvider, { client: qc }, children);
  }

  return { wrapper: Wrapper, queryClient: qc };
}

/* ------------------------------------------------------------------ */
/*  Raw service function tests                                         */
/* ------------------------------------------------------------------ */

describe("beneficiary-service — raw functions", () => {
  // We need a fresh module for each test to reset the in-memory store
  let getAssetLinks: typeof import("../beneficiary-service").getAssetLinks;
  let createAssetLink: typeof import("../beneficiary-service").createAssetLink;
  let updateAssetLink: typeof import("../beneficiary-service").updateAssetLink;
  let deleteAssetLink: typeof import("../beneficiary-service").deleteAssetLink;

  beforeEach(async () => {
    vi.resetModules();
    const mod = await import("../beneficiary-service");
    getAssetLinks = mod.getAssetLinks;
    createAssetLink = mod.createAssetLink;
    updateAssetLink = mod.updateAssetLink;
    deleteAssetLink = mod.deleteAssetLink;
  });

  /* ------ getAssetLinks ------ */

  describe("getAssetLinks", () => {
    it("returns seed data for the known asset ID", async () => {
      const links = await getAssetLinks(SEED_ASSET_ID);

      expect(links).toHaveLength(4);
      expect(links[0]).toMatchObject({
        id: "lnk-0001",
        contactDisplayName: "Priya Patel",
        role: "Beneficiary",
        designation: "Primary",
        allocationPercent: 50,
      });
    });

    it("returns an empty array for an unknown asset ID", async () => {
      const links = await getAssetLinks(UNKNOWN_ASSET_ID);
      expect(links).toEqual([]);
    });

    it("returns a defensive copy that does not mutate the store", async () => {
      const first = await getAssetLinks(SEED_ASSET_ID);
      first.pop(); // mutate the returned array
      const second = await getAssetLinks(SEED_ASSET_ID);
      expect(second).toHaveLength(4);
    });
  });

  /* ------ createAssetLink ------ */

  describe("createAssetLink", () => {
    it("creates a new link and appends to the store", async () => {
      const link = await createAssetLink(
        SEED_ASSET_ID,
        "Primary Residence",
        "c-new",
        "New Contact",
        {
          contactId: "c-new",
          role: "Trustee",
          perStirpes: false,
        }
      );

      expect(link).toMatchObject({
        assetId: SEED_ASSET_ID,
        contactId: "c-new",
        contactDisplayName: "New Contact",
        role: "Trustee",
        perStirpes: false,
      });
      expect(link.id).toMatch(/^lnk-/);

      // Verify it's in the store
      const links = await getAssetLinks(SEED_ASSET_ID);
      expect(links).toHaveLength(5);
    });

    it("creates store entry for a brand-new asset ID", async () => {
      const newAsset = "new-asset-id";
      await createAssetLink(newAsset, "New Asset", "c-new", "Contact", {
        contactId: "c-new",
        role: "Beneficiary",
        designation: "Primary",
        allocationPercent: 100,
      });

      const links = await getAssetLinks(newAsset);
      expect(links).toHaveLength(1);
      expect(links[0].assetName).toBe("New Asset");
    });

    it("preserves optional fields (designation, allocation, notes)", async () => {
      const link = await createAssetLink(
        SEED_ASSET_ID,
        "Primary Residence",
        "c-opt",
        "Optional Fields",
        {
          contactId: "c-opt",
          role: "Beneficiary",
          designation: "Contingent",
          allocationPercent: 75,
          perStirpes: true,
          notes: "Test note",
        }
      );

      expect(link.designation).toBe("Contingent");
      expect(link.allocationPercent).toBe(75);
      expect(link.perStirpes).toBe(true);
      expect(link.notes).toBe("Test note");
    });
  });

  /* ------ updateAssetLink ------ */

  describe("updateAssetLink", () => {
    it("updates an existing link's fields", async () => {
      const updated = await updateAssetLink("lnk-0001", SEED_ASSET_ID, {
        role: "CoOwner",
        allocationPercent: 60,
      });

      expect(updated.role).toBe("CoOwner");
      expect(updated.allocationPercent).toBe(60);
      // Unchanged fields preserved
      expect(updated.contactDisplayName).toBe("Priya Patel");
    });

    it("throws LINK_NOT_FOUND for non-existent link", async () => {
      await expect(
        updateAssetLink("lnk-9999", SEED_ASSET_ID, { role: "Trustee" })
      ).rejects.toThrow("LINK_NOT_FOUND");
    });

    it("throws LINK_NOT_FOUND for valid link on wrong asset", async () => {
      await expect(
        updateAssetLink("lnk-0001", UNKNOWN_ASSET_ID, { role: "Trustee" })
      ).rejects.toThrow("LINK_NOT_FOUND");
    });
  });

  /* ------ deleteAssetLink ------ */

  describe("deleteAssetLink", () => {
    it("removes a link from the store", async () => {
      await deleteAssetLink("lnk-0001", SEED_ASSET_ID);
      const links = await getAssetLinks(SEED_ASSET_ID);
      expect(links).toHaveLength(3);
      expect(links.find((l) => l.id === "lnk-0001")).toBeUndefined();
    });

    it("throws LINK_NOT_FOUND for non-existent link", async () => {
      await expect(
        deleteAssetLink("lnk-9999", SEED_ASSET_ID)
      ).rejects.toThrow("LINK_NOT_FOUND");
    });

    it("throws LINK_NOT_FOUND for valid link on wrong asset", async () => {
      await expect(
        deleteAssetLink("lnk-0001", UNKNOWN_ASSET_ID)
      ).rejects.toThrow("LINK_NOT_FOUND");
    });
  });
});

/* ------------------------------------------------------------------ */
/*  TanStack Query hook tests                                          */
/* ------------------------------------------------------------------ */

describe("beneficiary-service — TanStack Query hooks", () => {
  let useAssetLinks: typeof import("../beneficiary-service").useAssetLinks;
  let useCreateAssetLink: typeof import("../beneficiary-service").useCreateAssetLink;
  let useUpdateAssetLink: typeof import("../beneficiary-service").useUpdateAssetLink;
  let useDeleteAssetLink: typeof import("../beneficiary-service").useDeleteAssetLink;

  beforeEach(async () => {
    vi.resetModules();
    const mod = await import("../beneficiary-service");
    useAssetLinks = mod.useAssetLinks;
    useCreateAssetLink = mod.useCreateAssetLink;
    useUpdateAssetLink = mod.useUpdateAssetLink;
    useDeleteAssetLink = mod.useDeleteAssetLink;
  });

  describe("useAssetLinks", () => {
    it("fetches links for a given asset", async () => {
      const { wrapper } = createQueryWrapper();
      const { result } = renderHook(() => useAssetLinks(SEED_ASSET_ID), { wrapper });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));
      expect(result.current.data).toHaveLength(4);
    });

    it("is disabled when assetId is empty", () => {
      const { wrapper } = createQueryWrapper();
      const { result } = renderHook(() => useAssetLinks(""), { wrapper });

      // Should not be loading — query is disabled
      expect(result.current.fetchStatus).toBe("idle");
    });
  });

  describe("useCreateAssetLink", () => {
    it("creates a link and invalidates the query cache", async () => {
      const { wrapper, queryClient } = createQueryWrapper();
      const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

      const { result } = renderHook(() => useCreateAssetLink(), { wrapper });

      result.current.mutate({
        assetId: SEED_ASSET_ID,
        assetName: "Primary Residence",
        contactId: "c-hook",
        contactDisplayName: "Hook Contact",
        data: { contactId: "c-hook", role: "Custodian" },
      });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));
      expect(result.current.data).toMatchObject({
        contactId: "c-hook",
        role: "Custodian",
      });
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ["asset-links", SEED_ASSET_ID],
      });
    });
  });

  describe("useUpdateAssetLink", () => {
    it("updates a link and invalidates the query cache", async () => {
      const { wrapper, queryClient } = createQueryWrapper();
      const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

      const { result } = renderHook(() => useUpdateAssetLink(), { wrapper });

      result.current.mutate({
        linkId: "lnk-0001",
        assetId: SEED_ASSET_ID,
        data: { allocationPercent: 80 },
      });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));
      expect(result.current.data?.allocationPercent).toBe(80);
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ["asset-links", SEED_ASSET_ID],
      });
    });
  });

  describe("useDeleteAssetLink", () => {
    it("deletes a link and invalidates the query cache", async () => {
      const { wrapper, queryClient } = createQueryWrapper();
      const invalidateSpy = vi.spyOn(queryClient, "invalidateQueries");

      const { result } = renderHook(() => useDeleteAssetLink(), { wrapper });

      result.current.mutate({
        linkId: "lnk-0001",
        assetId: SEED_ASSET_ID,
      });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));
      expect(invalidateSpy).toHaveBeenCalledWith({
        queryKey: ["asset-links", SEED_ASSET_ID],
      });
    });
  });
});
