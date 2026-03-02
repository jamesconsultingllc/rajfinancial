import type { AssetDto, AssetType } from "@/types/assets";
import { ASSET_TYPE_LABELS } from "@/types/assets";

export const mockAssets: AssetDto[] = [
  {
    id: "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    name: "Primary Residence",
    type: "RealEstate",
    currentValue: 425000,
    purchasePrice: 380000,
    purchaseDate: "2019-06-15T00:00:00Z",
    description: "Single-family home in Austin, TX",
    location: "Austin, TX",
    isDepreciable: false,
    isDisposed: false,
    hasBeneficiaries: true,
    createdAt: "2024-01-10T08:00:00Z",
    updatedAt: "2024-12-01T10:30:00Z",
  },
  {
    id: "b2c3d4e5-f6a7-8901-bcde-f12345678901",
    name: "2022 Tesla Model 3",
    type: "Vehicle",
    currentValue: 35000,
    purchasePrice: 47000,
    purchaseDate: "2022-03-20T00:00:00Z",
    description: "Long Range AWD, Pearl White",
    isDepreciable: true,
    isDisposed: false,
    hasBeneficiaries: false,
    createdAt: "2024-01-10T08:05:00Z",
    updatedAt: "2024-11-15T14:20:00Z",
  },
  {
    id: "c3d4e5f6-a7b8-9012-cdef-123456789012",
    name: "Fidelity 401(k)",
    type: "Retirement",
    currentValue: 180000,
    institutionName: "Fidelity Investments",
    accountNumber: "****4821",
    isDepreciable: false,
    isDisposed: false,
    hasBeneficiaries: true,
    createdAt: "2024-01-10T08:10:00Z",
    updatedAt: "2025-01-05T09:00:00Z",
  },
  {
    id: "d4e5f6a7-b8c9-0123-defa-234567890123",
    name: "High-Yield Savings",
    type: "BankAccount",
    currentValue: 25000,
    institutionName: "Marcus by Goldman Sachs",
    accountNumber: "****7392",
    isDepreciable: false,
    isDisposed: false,
    hasBeneficiaries: false,
    createdAt: "2024-02-14T11:00:00Z",
  },
  {
    id: "e5f6a7b8-c9d0-1234-efab-345678901234",
    name: "Bitcoin Holdings",
    type: "Cryptocurrency",
    currentValue: 12500,
    purchasePrice: 8000,
    purchaseDate: "2023-01-08T00:00:00Z",
    description: "0.15 BTC on Coinbase",
    institutionName: "Coinbase",
    isDepreciable: false,
    isDisposed: false,
    hasBeneficiaries: false,
    createdAt: "2024-03-01T16:45:00Z",
    updatedAt: "2025-02-10T08:00:00Z",
  },
  {
    id: "f6a7b8c9-d0e1-2345-fabc-456789012345",
    name: "Vintage Watch Collection",
    type: "Collectible",
    currentValue: 8000,
    purchasePrice: 5500,
    description: "Omega Speedmaster & Seiko Presage",
    isDepreciable: false,
    isDisposed: false,
    hasBeneficiaries: false,
    createdAt: "2024-05-20T13:30:00Z",
  },
];

export function computeAssetsSummary(assets: AssetDto[]) {
  const totalValue = assets.reduce((sum, a) => sum + a.currentValue, 0);
  const needsAttention = assets.filter((a) => !a.hasBeneficiaries).length;

  // Top category by total value
  const byType: Partial<Record<AssetType, number>> = {};
  for (const a of assets) {
    byType[a.type] = (byType[a.type] || 0) + a.currentValue;
  }
  const topType = Object.entries(byType).sort((a, b) => b[1] - a[1])[0];
  const topCategory = topType ? ASSET_TYPE_LABELS[topType[0] as AssetType] : "—";

  return { totalValue, count: assets.length, topCategory, needsAttention };
}
