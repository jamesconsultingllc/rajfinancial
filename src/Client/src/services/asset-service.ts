import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import type { AssetDto, AssetDetailDto, CreateAssetRequest, UpdateAssetRequest } from "@/types/assets";
import { mockAssets } from "@/data/mock-assets";

// Simulate network delay
const delay = (ms = 400) => new Promise((r) => setTimeout(r, ms));

// --- Raw service functions (swap to fetch when API is ready) ---

export async function getAssets(params?: { type?: string; includeDisposed?: boolean }): Promise<AssetDto[]> {
  await delay();
  let result = [...mockAssets];
  if (params?.type) result = result.filter((a) => a.type === params.type);
  if (!params?.includeDisposed) result = result.filter((a) => !a.isDisposed);
  return result;
}

export async function getAsset(id: string): Promise<AssetDetailDto> {
  await delay();
  const asset = mockAssets.find((a) => a.id === id);
  if (!asset) throw new Error("ASSET_NOT_FOUND");
  return { ...asset, beneficiaries: [] } as AssetDetailDto;
}

export async function createAsset(data: CreateAssetRequest): Promise<AssetDto> {
  await delay(600);
  const newAsset: AssetDto = {
    ...data,
    id: crypto.randomUUID(),
    isDepreciable: !!data.depreciationMethod && data.depreciationMethod !== "None",
    isDisposed: false,
    hasBeneficiaries: false,
    createdAt: new Date().toISOString(),
  };
  mockAssets.push(newAsset);
  return newAsset;
}

export async function updateAsset(id: string, data: UpdateAssetRequest): Promise<AssetDto> {
  await delay(600);
  const idx = mockAssets.findIndex((a) => a.id === id);
  if (idx === -1) throw new Error("ASSET_NOT_FOUND");
  const updated = { ...mockAssets[idx], ...data, updatedAt: new Date().toISOString() };
  mockAssets[idx] = updated;
  return updated;
}

export async function deleteAsset(id: string): Promise<void> {
  await delay(400);
  const idx = mockAssets.findIndex((a) => a.id === id);
  if (idx === -1) throw new Error("ASSET_NOT_FOUND");
  mockAssets.splice(idx, 1);
}

// --- TanStack Query hooks ---

export function useAssets(params?: { type?: string }) {
  return useQuery({
    queryKey: ["assets", params],
    queryFn: () => getAssets(params),
  });
}

export function useAsset(id: string) {
  return useQuery({
    queryKey: ["assets", id],
    queryFn: () => getAsset(id),
    enabled: !!id,
  });
}

export function useCreateAsset() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: createAsset,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assets"] }),
  });
}

export function useUpdateAsset() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateAssetRequest }) => updateAsset(id, data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assets"] }),
  });
}

export function useDeleteAsset() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteAsset,
    onSuccess: () => qc.invalidateQueries({ queryKey: ["assets"] }),
  });
}
