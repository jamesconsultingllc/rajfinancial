import type { UserProfileDto, SubscriptionDetailsDto, DataAccessGrantDto } from "@/types/settings";

export const mockProfile: UserProfileDto = {
  id: "usr-001",
  userId: "auth-001",
  displayName: "Rajesh Patel",
  email: "rajesh@example.com",
  phone: "+1 (555) 123-4567",
  address: {
    street1: "123 Financial Ave",
    street2: "Suite 200",
    city: "New York",
    state: "NY",
    postalCode: "10001",
    country: "US",
  },
  avatarUrl: "",
  locale: "en-US",
  timezone: "America/New_York",
  currencyCode: "USD",
  tier: "Premium",
  subscriptionEndDate: "2026-12-31",
  hasByokKey: true,
  emailDigestEnabled: true,
  alertSyncIssues: true,
  alertCoverageGaps: true,
  alertTierLimits: true,
  theme: "Dark",
  createdAt: "2024-01-15T00:00:00Z",
  deletionRequestedAt: undefined,
};

export const mockSubscription: SubscriptionDetailsDto = {
  tier: "Premium",
  subscriptionStartDate: "2025-01-01",
  subscriptionEndDate: "2026-12-31",
  isCancelling: false,
  usage: {
    assetCount: 7,
    assetLimit: -1,
    contactCount: 12,
    contactLimit: -1,
    manualAccountCount: 4,
    manualAccountLimit: -1,
    storageUsedBytes: 268435456, // 256MB
    storageLimitBytes: 5368709120, // 5GB
    aiCallsThisMonth: 42,
    aiCallLimit: 500,
  },
};

export const mockGrants: DataAccessGrantDto[] = [
  {
    id: "grant-001",
    granteeId: "adv-001",
    granteeName: "Sarah Chen",
    granteeEmail: "sarah.chen@advisors.com",
    accessType: "Read",
    dataCategories: ["Assets", "Accounts"],
    expiresAt: "2026-06-30",
    isActive: true,
    createdAt: "2025-03-01",
  },
  {
    id: "grant-002",
    granteeId: "adv-002",
    granteeName: "Michael Rivera",
    granteeEmail: "m.rivera@wealthplan.com",
    accessType: "Full",
    dataCategories: ["Assets", "Contacts", "Accounts", "Transactions", "Documents"],
    isActive: true,
    createdAt: "2025-06-15",
  },
];
