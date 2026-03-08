/**
 * Mock beneficiary assignment service for asset-contact links.
 *
 * @description Manages assignments of contacts to assets with roles,
 * designations, and allocation percentages. Uses in-memory mock data.
 */
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type {
  AssetContactLinkDto,
  CreateAssetContactLinkRequest,
  UpdateAssetContactLinkRequest,
} from "@/types/contacts";

const delay = (ms = 400) => new Promise((r) => setTimeout(r, ms));

/** In-memory store keyed by assetId → links[] */
const assetLinksStore: Record<string, AssetContactLinkDto[]> = {
  "a1b2c3d4-e5f6-7890-abcd-ef1234567890": [
    {
      id: "lnk-0001",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444401",
      contactDisplayName: "Priya Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 50,
      perStirpes: false,
    },
    {
      id: "lnk-0004",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444402",
      contactDisplayName: "Arjun Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 25,
      perStirpes: true,
    },
    {
      id: "lnk-0005",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444403",
      contactDisplayName: "Anaya Patel",
      role: "Beneficiary",
      designation: "Primary",
      allocationPercent: 25,
      perStirpes: true,
    },
    {
      id: "lnk-0006",
      assetId: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      assetName: "Primary Residence",
      contactId: "c001-1111-2222-3333-444444444410",
      contactDisplayName: "Patel Family Revocable Trust",
      role: "Beneficiary",
      designation: "Contingent",
      allocationPercent: 100,
      perStirpes: false,
    },
  ],
};

export async function getAssetLinks(assetId: string): Promise<AssetContactLinkDto[]> {
  await delay();
  return [...(assetLinksStore[assetId] ?? [])];
}

export async function createAssetLink(
  assetId: string,
  assetName: string,
  contactId: string,
  contactDisplayName: string,
  data: CreateAssetContactLinkRequest
): Promise<AssetContactLinkDto> {
  await delay(500);
  const link: AssetContactLinkDto = {
    id: `lnk-${Date.now()}`,
    assetId,
    assetName,
    contactId,
    contactDisplayName,
    role: data.role,
    designation: data.designation,
    allocationPercent: data.allocationPercent,
    perStirpes: data.perStirpes ?? false,
    notes: data.notes,
  };
  if (!assetLinksStore[assetId]) assetLinksStore[assetId] = [];
  assetLinksStore[assetId].push(link);
  return link;
}

export async function updateAssetLink(
  linkId: string,
  assetId: string,
  data: UpdateAssetContactLinkRequest
): Promise<AssetContactLinkDto> {
  await delay(500);
  const links = assetLinksStore[assetId] ?? [];
  const idx = links.findIndex((l) => l.id === linkId);
  if (idx === -1) throw new Error("LINK_NOT_FOUND");
  links[idx] = {
    ...links[idx],
    ...data,
    designation: data.designation ?? null,
    allocationPercent: data.allocationPercent ?? null,
  };
  return links[idx];
}

export async function deleteAssetLink(linkId: string, assetId: string): Promise<void> {
  await delay(300);
  const links = assetLinksStore[assetId] ?? [];
  const idx = links.findIndex((l) => l.id === linkId);
  if (idx === -1) throw new Error("LINK_NOT_FOUND");
  links.splice(idx, 1);
}

/* ------------------------------------------------------------------ */
/*  TanStack Query hooks                                               */
/* ------------------------------------------------------------------ */

export function useAssetLinks(assetId: string) {
  return useQuery({
    queryKey: ["asset-links", assetId],
    queryFn: () => getAssetLinks(assetId),
    enabled: !!assetId,
  });
}

export function useCreateAssetLink() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (vars: {
      assetId: string;
      assetName: string;
      contactId: string;
      contactDisplayName: string;
      data: CreateAssetContactLinkRequest;
    }) => createAssetLink(vars.assetId, vars.assetName, vars.contactId, vars.contactDisplayName, vars.data),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ["asset-links", vars.assetId] });
      qc.invalidateQueries({ queryKey: ["assets"] });
    },
  });
}

export function useUpdateAssetLink() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (vars: { linkId: string; assetId: string; data: UpdateAssetContactLinkRequest }) =>
      updateAssetLink(vars.linkId, vars.assetId, vars.data),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ["asset-links", vars.assetId] });
      qc.invalidateQueries({ queryKey: ["assets"] });
    },
  });
}

export function useDeleteAssetLink() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (vars: { linkId: string; assetId: string }) => deleteAssetLink(vars.linkId, vars.assetId),
    onSuccess: (_d, vars) => {
      qc.invalidateQueries({ queryKey: ["asset-links", vars.assetId] });
      qc.invalidateQueries({ queryKey: ["assets"] });
    },
  });
}
