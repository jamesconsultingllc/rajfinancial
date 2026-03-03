import type { ContactAddressDto } from "@/types/contacts";

export type SubscriptionTier = "Free" | "Premium";
export type ThemePreference = "System" | "Light" | "Dark";

export interface UserProfileDto {
  id: string;
  userId: string;
  displayName: string;
  email: string;
  phone?: string;
  address?: ContactAddressDto;
  avatarUrl?: string;
  locale: string;
  timezone: string;
  currencyCode: string;
  tier: SubscriptionTier;
  subscriptionEndDate?: string;
  hasByokKey: boolean;
  emailDigestEnabled: boolean;
  alertSyncIssues: boolean;
  alertCoverageGaps: boolean;
  alertTierLimits: boolean;
  theme: ThemePreference;
  createdAt: string;
  deletionRequestedAt?: string;
}

export interface TierUsageDto {
  assetCount: number;
  assetLimit: number;
  contactCount: number;
  contactLimit: number;
  manualAccountCount: number;
  manualAccountLimit: number;
  storageUsedBytes: number;
  storageLimitBytes: number;
  aiCallsThisMonth: number;
  aiCallLimit: number;
}

export interface SubscriptionDetailsDto {
  tier: SubscriptionTier;
  subscriptionStartDate?: string;
  subscriptionEndDate?: string;
  isCancelling: boolean;
  usage: TierUsageDto;
}

export interface DataAccessGrantDto {
  id: string;
  granteeId: string;
  granteeName: string;
  granteeEmail: string;
  accessType: "Read" | "Limited" | "Full";
  dataCategories: string[];
  expiresAt?: string;
  isActive: boolean;
  createdAt: string;
}

export interface ShareInvitationRequest {
  granteeEmail: string;
  accessType: "Read" | "Limited" | "Full";
  dataCategories: string[];
  expiresAt?: string;
}

export interface DataExportStatusDto {
  exportId: string;
  status: "Queued" | "Processing" | "Completed" | "Failed" | "Expired";
  requestedAt: string;
  completedAt?: string;
  fileSizeBytes?: number;
  downloadUrl?: string;
  downloadExpiresAt?: string;
}

export interface DeletionStatusDto {
  requestedAt: string;
  scheduledDeletionAt: string;
  daysRemaining: number;
  reason?: string;
}

export interface AccountDeletionRequest {
  confirmationEmail: string;
  reason?: string;
}
