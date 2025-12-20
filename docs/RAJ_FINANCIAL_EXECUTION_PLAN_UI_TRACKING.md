# RAJ Financial Software - Execution Plan (UI Tracking)

This document contains the UI implementation tracking tables extracted from [RAJ_FINANCIAL_EXECUTION_PLAN.md](RAJ_FINANCIAL_EXECUTION_PLAN.md).

---

## Part 1: UI Development Tracking

### 1.1 Design System & Theme

| Task | Status | Priority | Notes |
|------|--------|----------|-------|
| CSS Design Tokens (raj-theme.css) | ⬜ Not Started | P0 | Gold palette, typography, spacing |
| Animation keyframes | ⬜ Not Started | P1 | fadeIn, slideUp, celebrate, shimmer |
| Gradient utilities | ⬜ Not Started | P1 | gradient-gold, gradient-mesh |
| Glass effect styles | ⬜ Not Started | P1 | Backdrop blur, borders |
| Button styles (btn-gold) | ⬜ Not Started | P0 | Primary, outline variants |
| Focus states (accessibility) | ⬜ Not Started | P0 | Gold focus rings |
| Skeleton loading styles | ⬜ Not Started | P1 | Shimmer animation |

### 1.2 Layout Components

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| MainLayout.razor | Components/Layout/ | ⬜ Not Started | P0 |
| DesktopSidebar.razor | Components/Layout/ | ⬜ Not Started | P0 |
| MobileBottomNav.razor | Components/Layout/ | ⬜ Not Started | P0 |
| MobileHeader.razor | Components/Layout/ | ⬜ Not Started | P0 |

### 1.3 Common/Shared Components

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| GlassCard.razor | Components/Common/ | ⬜ Not Started | P0 |
| EmptyState.razor | Components/Common/ | ⬜ Not Started | P0 |
| AnimatedNumber.razor | Components/Common/ | ⬜ Not Started | P1 |
| CelebrationModal.razor | Components/Common/ | ⬜ Not Started | P2 |
| TrendBadge.razor | Components/Common/ | ⬜ Not Started | P1 |
| DynamicIcon.razor | Components/Common/ | ⬜ Not Started | P0 |
| SkeletonLoader.razor | Components/Common/ | ⬜ Not Started | P1 |

### 1.4 Dashboard Components

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| Dashboard.razor (Page) | Pages/ | ⬜ Not Started | P0 |
| NetWorthHero.razor | Components/Dashboard/ | ⬜ Not Started | P0 |
| QuickStatCard.razor | Components/Dashboard/ | ⬜ Not Started | P0 |
| InsightsPanel.razor | Components/Dashboard/ | ⬜ Not Started | P1 |
| InsightCard.razor | Components/Dashboard/ | ⬜ Not Started | P1 |
| HealthScoreCard.razor | Components/Dashboard/ | ⬜ Not Started | P1 |
| RecentActivityCard.razor | Components/Dashboard/ | ⬜ Not Started | P2 |
| QuickActionsCard.razor | Components/Dashboard/ | ⬜ Not Started | P2 |
| NetWorthChartCard.razor | Components/Dashboard/ | ⬜ Not Started | P1 |
| AssetAllocationCard.razor | Components/Dashboard/ | ⬜ Not Started | P2 |

### 1.5 Account Components

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| Accounts.razor (Page) | Pages/ | ⬜ Not Started | P0 |
| PlaidLinkModal.razor | Components/Accounts/ | ⬜ Not Started | P0 |
| AccountCard.razor | Components/Accounts/ | ⬜ Not Started | P0 |
| AccountStatusBadge.razor | Components/Accounts/ | ⬜ Not Started | P1 |
| AccountSummaryCards.razor | Components/Accounts/ | ⬜ Not Started | P1 |

### 1.6 Asset Components

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| Assets.razor (Page) | Pages/ | ⬜ Not Started | P0 |
| AssetForm.razor | Components/Assets/ | ⬜ Not Started | P0 |
| AssetCard.razor | Components/Assets/ | ⬜ Not Started | P0 |
| AssetTypeIcon.razor | Components/Assets/ | ⬜ Not Started | P1 |
| AssetFilterTabs.razor | Components/Assets/ | ⬜ Not Started | P1 |
| AssetGrid.razor | Components/Assets/ | ⬜ Not Started | P1 |
| AssetSummary.razor | Components/Assets/ | ⬜ Not Started | P2 |

### 1.7 Beneficiary Components

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| Beneficiaries.razor (Page) | Pages/ | ⬜ Not Started | P0 |
| BeneficiaryCard.razor | Components/Beneficiaries/ | ⬜ Not Started | P0 |
| BeneficiaryForm.razor | Components/Beneficiaries/ | ⬜ Not Started | P0 |
| AssignmentDialog.razor | Components/Beneficiaries/ | ⬜ Not Started | P0 |
| CoverageWarning.razor | Components/Beneficiaries/ | ⬜ Not Started | P1 |
| BeneficiaryViewToggle.razor | Components/Beneficiaries/ | ⬜ Not Started | P2 |

### 1.8 Debt Payoff Tool

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| DebtPayoff.razor (Page) | Pages/Tools/ | ⬜ Not Started | P1 |
| StrategyCard.razor | Components/DebtPayoff/ | ⬜ Not Started | P1 |
| DebtListItem.razor | Components/DebtPayoff/ | ⬜ Not Started | P1 |
| DebtForm.razor | Components/DebtPayoff/ | ⬜ Not Started | P1 |
| PayoffScheduleTable.razor | Components/DebtPayoff/ | ⬜ Not Started | P2 |
| PayoffChart.razor | Components/DebtPayoff/ | ⬜ Not Started | P1 |
| StrategyComparison.razor | Components/DebtPayoff/ | ⬜ Not Started | P1 |

### 1.9 Insurance Calculator Tool

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| InsuranceCalculator.razor (Page) | Pages/Tools/ | ⬜ Not Started | P1 |
| CoverageGauge.razor | Components/Insurance/ | ⬜ Not Started | P1 |
| BreakdownItem.razor | Components/Insurance/ | ⬜ Not Started | P1 |
| InsuranceInputForm.razor | Components/Insurance/ | ⬜ Not Started | P1 |
| CoverageResultCard.razor | Components/Insurance/ | ⬜ Not Started | P1 |
| ConsiderationsList.razor | Components/Insurance/ | ⬜ Not Started | P2 |

### 1.10 Estate Planning Tool

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| EstateChecklist.razor (Page) | Pages/Tools/ | ⬜ Not Started | P2 |
| ChecklistItem.razor | Components/Estate/ | ⬜ Not Started | P2 |
| ProgressIndicator.razor | Components/Estate/ | ⬜ Not Started | P2 |

### 1.11 Settings & Profile

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| Settings.razor (Page) | Pages/ | ⬜ Not Started | P2 |
| ProfileSection.razor | Components/Settings/ | ⬜ Not Started | P2 |
| SecuritySection.razor | Components/Settings/ | ⬜ Not Started | P2 |
| NotificationPrefs.razor | Components/Settings/ | ⬜ Not Started | P3 |

### 1.12 Authentication Pages

| Component | File | Status | Priority |
|-----------|------|--------|----------|
| AuthRedirect.razor | Pages/Auth/ | ⬜ Not Started | P0 |
| AccessDenied.razor | Pages/Auth/ | ⬜ Not Started | P0 |
| LoggingIn.razor | Pages/Auth/ | ⬜ Not Started | P1 |

> **Note**: Login, registration, and password reset are handled by Entra External ID hosted pages with custom branding. These pages are redirect handlers and error states.
