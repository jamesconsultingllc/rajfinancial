# RAJ Financial Software - Complete UI & Integration Guide

## Brand Identity

```
╔══════════════════════════════════════════════════════════════╗
║                                                              ║
║     ██████╗  █████╗      ██╗    ███████╗██╗███╗   ██╗       ║
║     ██╔══██╗██╔══██╗     ██║    ██╔════╝██║████╗  ██║       ║
║     ██████╔╝███████║     ██║    █████╗  ██║██╔██╗ ██║       ║
║     ██╔══██╗██╔══██║██   ██║    ██╔══╝  ██║██║╚██╗██║       ║
║     ██║  ██║██║  ██║╚█████╔╝    ██║     ██║██║ ╚████║       ║
║     ╚═╝  ╚═╝╚═╝  ╚═╝ ╚════╝     ╚═╝     ╚═╝╚═╝  ╚═══╝       ║
║                                                              ║
║                    Your Financial Future                     ║
║                                                              ║
╚══════════════════════════════════════════════════════════════╝
```

---

## Part 1: Design System - RAJ Financial Theme

### CSS Design Tokens

```css
/* RAJFinancial.Client/wwwroot/css/raj-theme.css */

@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');

:root {
  /* ═══════════════════════════════════════════════════════════
     RAJ FINANCIAL BRAND COLORS
     Premium gold palette conveying wealth, trust, and excellence
     ═══════════════════════════════════════════════════════════ */
  
  /* Primary Gold Palette */
  --gold-50: #fffbcc;      /* Lemon Chiffon - lightest backgrounds */
  --gold-100: #fff7b3;     /* Light cream */
  --gold-200: #f5e99a;     /* Soft gold */
  --gold-300: #eed688;     /* Flax - secondary elements */
  --gold-400: #e8c94d;     /* Bright gold */
  --gold-500: #ebbb10;     /* Spanish Yellow - PRIMARY */
  --gold-600: #d4a80e;     /* Rich gold */
  --gold-700: #c3922e;     /* UC Gold - accent/depth */
  --gold-800: #a67c26;     /* Deep gold */
  --gold-900: #8a661f;     /* Darkest gold */
  
  /* Neutral Palette - Warm grays to complement gold */
  --neutral-50: #fafaf9;
  --neutral-100: #f5f5f4;
  --neutral-200: #e7e5e4;
  --neutral-300: #d6d3d1;
  --neutral-400: #a8a29e;
  --neutral-500: #78716c;
  --neutral-600: #57534e;
  --neutral-700: #44403c;
  --neutral-800: #292524;
  --neutral-900: #1c1917;
  
  /* Semantic Colors */
  --success-50: #f0fdf4;
  --success-100: #dcfce7;
  --success-500: #22c55e;
  --success-600: #16a34a;
  --success-700: #15803d;
  
  --warning-50: #fffbeb;
  --warning-100: #fef3c7;
  --warning-500: #f59e0b;
  --warning-600: #d97706;
  
  --error-50: #fef2f2;
  --error-100: #fee2e2;
  --error-500: #ef4444;
  --error-600: #dc2626;
  
  --info-50: #eff6ff;
  --info-100: #dbeafe;
  --info-500: #3b82f6;
  --info-600: #2563eb;
  
  /* Typography */
  --font-display: 'Nexa', 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  --font-body: 'Inter', -apple-system, BlinkMacSystemFont, sans-serif;
  --font-mono: 'JetBrains Mono', 'Fira Code', monospace;
  
  /* Font Sizes */
  --text-xs: 0.75rem;
  --text-sm: 0.875rem;
  --text-base: 1rem;
  --text-lg: 1.125rem;
  --text-xl: 1.25rem;
  --text-2xl: 1.5rem;
  --text-3xl: 1.875rem;
  --text-4xl: 2.25rem;
  --text-5xl: 3rem;
  --text-6xl: 3.75rem;
  
  /* Spacing Scale */
  --space-1: 0.25rem;
  --space-2: 0.5rem;
  --space-3: 0.75rem;
  --space-4: 1rem;
  --space-5: 1.25rem;
  --space-6: 1.5rem;
  --space-8: 2rem;
  --space-10: 2.5rem;
  --space-12: 3rem;
  --space-16: 4rem;
  --space-20: 5rem;
  
  /* Shadows - Warm-tinted for gold theme */
  --shadow-sm: 0 1px 2px 0 rgb(166 124 38 / 0.05);
  --shadow-md: 0 4px 6px -1px rgb(166 124 38 / 0.1), 0 2px 4px -2px rgb(166 124 38 / 0.1);
  --shadow-lg: 0 10px 15px -3px rgb(166 124 38 / 0.1), 0 4px 6px -4px rgb(166 124 38 / 0.1);
  --shadow-xl: 0 20px 25px -5px rgb(166 124 38 / 0.15), 0 8px 10px -6px rgb(166 124 38 / 0.1);
  --shadow-gold: 0 4px 14px 0 rgb(235 187 16 / 0.25);
  --shadow-gold-lg: 0 10px 40px 0 rgb(235 187 16 / 0.3);
  
  /* Border Radius */
  --radius-sm: 0.375rem;
  --radius-md: 0.5rem;
  --radius-lg: 0.75rem;
  --radius-xl: 1rem;
  --radius-2xl: 1.5rem;
  --radius-full: 9999px;
  
  /* Transitions */
  --transition-fast: 150ms cubic-bezier(0.4, 0, 0.2, 1);
  --transition-base: 200ms cubic-bezier(0.4, 0, 0.2, 1);
  --transition-slow: 300ms cubic-bezier(0.4, 0, 0.2, 1);
  --transition-bounce: 500ms cubic-bezier(0.68, -0.55, 0.265, 1.55);
}

/* ═══════════════════════════════════════════════════════════
   ANIMATIONS
   ═══════════════════════════════════════════════════════════ */

@keyframes fadeIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

@keyframes slideUp {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes slideDown {
  from { opacity: 0; transform: translateY(-20px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes slideInRight {
  from { opacity: 0; transform: translateX(30px); }
  to { opacity: 1; transform: translateX(0); }
}

@keyframes slideInLeft {
  from { opacity: 0; transform: translateX(-30px); }
  to { opacity: 1; transform: translateX(0); }
}

@keyframes scaleIn {
  from { opacity: 0; transform: scale(0.95); }
  to { opacity: 1; transform: scale(1); }
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

@keyframes shimmer {
  0% { background-position: -200% 0; }
  100% { background-position: 200% 0; }
}

@keyframes goldShine {
  0% { background-position: -100% 0; }
  100% { background-position: 200% 0; }
}

@keyframes celebrate {
  0% { transform: scale(1); }
  25% { transform: scale(1.1); }
  50% { transform: scale(1); }
  75% { transform: scale(1.05); }
  100% { transform: scale(1); }
}

@keyframes countUp {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes progressFill {
  from { width: 0; }
}

@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-10px); }
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

/* Animation Classes */
.animate-fade-in { animation: fadeIn var(--transition-base) ease-out; }
.animate-slide-up { animation: slideUp var(--transition-slow) ease-out; }
.animate-slide-down { animation: slideDown var(--transition-slow) ease-out; }
.animate-slide-in-right { animation: slideInRight var(--transition-slow) ease-out; }
.animate-slide-in-left { animation: slideInLeft var(--transition-slow) ease-out; }
.animate-scale-in { animation: scaleIn var(--transition-base) ease-out; }
.animate-pulse { animation: pulse 2s ease-in-out infinite; }
.animate-celebrate { animation: celebrate 0.6s ease-in-out; }
.animate-float { animation: float 3s ease-in-out infinite; }
.animate-spin { animation: spin 1s linear infinite; }

/* Staggered Animations */
.stagger-1 { animation-delay: 50ms; }
.stagger-2 { animation-delay: 100ms; }
.stagger-3 { animation-delay: 150ms; }
.stagger-4 { animation-delay: 200ms; }
.stagger-5 { animation-delay: 250ms; }

/* ═══════════════════════════════════════════════════════════
   GRADIENT UTILITIES
   ═══════════════════════════════════════════════════════════ */

/* Primary gold gradient - used for hero sections and CTAs */
.gradient-gold {
  background: linear-gradient(135deg, var(--gold-500) 0%, var(--gold-700) 100%);
}

/* Premium gold gradient with shine effect */
.gradient-gold-shine {
  background: linear-gradient(
    120deg,
    var(--gold-600) 0%,
    var(--gold-400) 25%,
    var(--gold-500) 50%,
    var(--gold-400) 75%,
    var(--gold-600) 100%
  );
  background-size: 200% 100%;
  animation: goldShine 3s ease-in-out infinite;
}

/* Subtle gold for cards */
.gradient-gold-subtle {
  background: linear-gradient(180deg, var(--gold-50) 0%, white 100%);
}

/* Dark premium gradient for headers */
.gradient-dark-gold {
  background: linear-gradient(135deg, var(--neutral-900) 0%, var(--neutral-800) 50%, var(--gold-900) 100%);
}

/* Mesh gradient background */
.gradient-mesh {
  background: 
    radial-gradient(at 40% 20%, var(--gold-100) 0px, transparent 50%),
    radial-gradient(at 80% 0%, var(--gold-50) 0px, transparent 50%),
    radial-gradient(at 0% 50%, var(--gold-100) 0px, transparent 50%),
    radial-gradient(at 80% 50%, var(--gold-50) 0px, transparent 50%),
    radial-gradient(at 0% 100%, var(--gold-200) 0px, transparent 50%),
    white;
}

/* ═══════════════════════════════════════════════════════════
   COMPONENT UTILITIES
   ═══════════════════════════════════════════════════════════ */

/* Glass effect card */
.glass {
  background: rgba(255, 255, 255, 0.85);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border: 1px solid rgba(235, 187, 16, 0.1);
}

/* Premium card with gold accent */
.card-premium {
  background: white;
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-lg);
  border: 1px solid var(--gold-100);
  transition: all var(--transition-base);
}

.card-premium:hover {
  box-shadow: var(--shadow-xl);
  border-color: var(--gold-300);
  transform: translateY(-2px);
}

/* Gold accent border */
.border-gold-accent {
  border-left: 4px solid var(--gold-500);
}

/* Skeleton loading with gold tint */
.skeleton {
  background: linear-gradient(
    90deg,
    var(--neutral-200) 25%,
    var(--gold-50) 50%,
    var(--neutral-200) 75%
  );
  background-size: 200% 100%;
  animation: shimmer 1.5s ease-in-out infinite;
  border-radius: var(--radius-md);
}

/* Button styles */
.btn-gold {
  background: var(--gold-500);
  color: var(--neutral-900);
  font-weight: 600;
  padding: var(--space-3) var(--space-6);
  border-radius: var(--radius-lg);
  transition: all var(--transition-base);
  box-shadow: var(--shadow-gold);
}

.btn-gold:hover {
  background: var(--gold-600);
  box-shadow: var(--shadow-gold-lg);
  transform: translateY(-1px);
}

.btn-gold:active {
  transform: translateY(0);
}

.btn-gold-outline {
  background: transparent;
  color: var(--gold-700);
  border: 2px solid var(--gold-500);
  font-weight: 600;
  padding: var(--space-3) var(--space-6);
  border-radius: var(--radius-lg);
  transition: all var(--transition-base);
}

.btn-gold-outline:hover {
  background: var(--gold-50);
  border-color: var(--gold-600);
}

/* Focus states - accessible gold */
.focus-gold:focus {
  outline: none;
  box-shadow: 0 0 0 3px rgba(235, 187, 16, 0.4);
}

/* Text utilities */
.text-gold { color: var(--gold-500); }
.text-gold-dark { color: var(--gold-700); }
.text-gold-light { color: var(--gold-300); }

/* Background utilities */
.bg-gold-50 { background-color: var(--gold-50); }
.bg-gold-100 { background-color: var(--gold-100); }
.bg-gold-500 { background-color: var(--gold-500); }
```

---

## Part 2: Debt Payoff Analyzer - Premium Interactive Tool

This is the flagship analysis tool - a beautiful, interactive debt payoff calculator that helps users visualize their path to debt freedom.

### Debt Payoff Page Component

```razor
@* RAJFinancial.Client/Pages/Tools/DebtPayoff.razor *@
@page "/tools/debt-payoff"
@attribute [Authorize]
@inject IStringLocalizer<DebtPayoff> L
@inject IState<DebtPayoffState> State
@inject IDispatcher Dispatcher
@inject NavigationManager Nav

<PageTitle>@L["Title"] - RAJ Financial</PageTitle>

<div class="min-h-screen bg-neutral-50">
    @* Background Pattern *@
    <div class="fixed inset-0 gradient-mesh opacity-60 pointer-events-none"></div>
    
    <div class="relative">
        @* Hero Header *@
        <header class="relative overflow-hidden">
            <div class="absolute inset-0 gradient-dark-gold"></div>
            
            @* Decorative gold circles *@
            <div class="absolute inset-0 overflow-hidden" aria-hidden="true">
                <div class="absolute -top-20 -right-20 w-64 h-64 rounded-full bg-gold-500/10"></div>
                <div class="absolute top-40 -left-10 w-40 h-40 rounded-full bg-gold-500/5"></div>
            </div>
            
            <div class="relative px-4 py-8 lg:px-8 lg:py-12">
                <div class="max-w-7xl mx-auto">
                    @* Breadcrumb *@
                    <nav class="flex items-center gap-2 text-sm text-gold-300 mb-4" aria-label="Breadcrumb">
                        <a href="/dashboard" class="hover:text-gold-100 transition-colors">Dashboard</a>
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                        </svg>
                        <a href="/tools" class="hover:text-gold-100 transition-colors">Tools</a>
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                        </svg>
                        <span class="text-white">@L["Title"]</span>
                    </nav>
                    
                    <div class="flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
                        <div class="animate-fade-in">
                            <div class="flex items-center gap-3 mb-2">
                                <div class="w-12 h-12 rounded-xl bg-gold-500/20 backdrop-blur 
                                            flex items-center justify-center">
                                    <svg class="w-6 h-6 text-gold-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                              d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
                                    </svg>
                                </div>
                                <h1 class="text-2xl lg:text-3xl font-bold text-white">
                                    @L["Title"]
                                </h1>
                            </div>
                            <p class="text-gold-200 max-w-xl">
                                @L["Subtitle"]
                            </p>
                        </div>
                        
                        @* Quick Summary Cards *@
                        @if (!State.Value.IsLoading && State.Value.Debts.Any())
                        {
                            <div class="flex flex-wrap gap-3 animate-slide-in-right">
                                <div class="glass rounded-xl px-4 py-3">
                                    <p class="text-xs text-gold-300 uppercase tracking-wide">Total Debt</p>
                                    <p class="text-xl font-bold text-white">
                                        @State.Value.TotalDebt.ToString("C0")
                                    </p>
                                </div>
                                <div class="glass rounded-xl px-4 py-3">
                                    <p class="text-xs text-gold-300 uppercase tracking-wide">Avg. Interest</p>
                                    <p class="text-xl font-bold text-white">
                                        @State.Value.AverageInterestRate.ToString("P1")
                                    </p>
                                </div>
                            </div>
                        }
                    </div>
                </div>
            </div>
        </header>
        
        @* Main Content *@
        <main class="relative px-4 py-8 lg:px-8">
            <div class="max-w-7xl mx-auto">
                
                @if (State.Value.IsLoading)
                {
                    <DebtPayoffSkeleton />
                }
                else if (!State.Value.Debts.Any())
                {
                    <EmptyDebtState OnAddDebt="OpenAddDebtDialog" />
                }
                else
                {
                    <div class="grid lg:grid-cols-3 gap-6">
                        @* Left Column - Debt List & Controls *@
                        <div class="lg:col-span-1 space-y-6">
                            @* Extra Payment Input *@
                            <div class="card-premium p-5 animate-slide-up">
                                <h3 class="font-semibold text-neutral-900 mb-3 flex items-center gap-2">
                                    <svg class="w-5 h-5 text-gold-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                              d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                                    </svg>
                                    @L["ExtraPayment.Title"]
                                </h3>
                                <p class="text-sm text-neutral-500 mb-4">
                                    @L["ExtraPayment.Description"]
                                </p>
                                
                                <div class="relative">
                                    <span class="absolute left-4 top-1/2 -translate-y-1/2 text-neutral-400">$</span>
                                    <SfNumericTextBox @bind-Value="ExtraPayment"
                                                     Min="0"
                                                     Max="10000"
                                                     Step="50"
                                                     Format="N0"
                                                     CssClass="pl-8 w-full"
                                                     Placeholder="0" />
                                </div>
                                
                                @* Quick preset buttons *@
                                <div class="flex flex-wrap gap-2 mt-3">
                                    @foreach (var preset in new[] { 100, 250, 500, 1000 })
                                    {
                                        <button @onclick="() => ExtraPayment = preset"
                                                class="px-3 py-1.5 text-sm rounded-lg transition-all
                                                       @(ExtraPayment == preset 
                                                           ? "bg-gold-500 text-neutral-900 font-medium" 
                                                           : "bg-neutral-100 text-neutral-600 hover:bg-gold-100")">
                                            $@preset
                                        </button>
                                    }
                                </div>
                            </div>
                            
                            @* Debt List *@
                            <div class="card-premium p-5 animate-slide-up stagger-1">
                                <div class="flex items-center justify-between mb-4">
                                    <h3 class="font-semibold text-neutral-900">@L["DebtList.Title"]</h3>
                                    <button @onclick="OpenAddDebtDialog"
                                            class="p-2 rounded-lg text-gold-600 hover:bg-gold-50 transition-colors">
                                        <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                                        </svg>
                                    </button>
                                </div>
                                
                                <div class="space-y-3">
                                    @foreach (var debt in State.Value.Debts.OrderByDescending(d => d.InterestRate))
                                    {
                                        <DebtListItem 
                                            Debt="@debt"
                                            IsSelected="@(SelectedDebtId == debt.Id)"
                                            OnSelect="() => SelectDebt(debt.Id)"
                                            OnEdit="() => EditDebt(debt)"
                                            OnDelete="() => ConfirmDeleteDebt(debt)" />
                                    }
                                </div>
                            </div>
                        </div>
                        
                        @* Right Column - Strategy Comparison *@
                        <div class="lg:col-span-2 space-y-6">
                            @* Strategy Cards *@
                            <div class="grid sm:grid-cols-2 gap-4">
                                @foreach (var (strategy, index) in State.Value.Strategies.Select((s, i) => (s, i)))
                                {
                                    <StrategyCard 
                                        Strategy="@strategy"
                                        IsRecommended="@(index == 0)"
                                        IsSelected="@(SelectedStrategy == strategy.Name)"
                                        OnSelect="() => SelectStrategy(strategy.Name)"
                                        AnimationDelay="@(index * 100)" />
                                }
                            </div>
                            
                            @* Payoff Timeline Chart *@
                            <div class="card-premium p-6 animate-slide-up stagger-3">
                                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
                                    <div>
                                        <h3 class="font-semibold text-neutral-900">@L["Timeline.Title"]</h3>
                                        <p class="text-sm text-neutral-500">
                                            @L["Timeline.Subtitle", SelectedStrategy]
                                        </p>
                                    </div>
                                    
                                    <div class="flex items-center gap-4 text-sm">
                                        <div class="flex items-center gap-2">
                                            <div class="w-3 h-3 rounded-full bg-gold-500"></div>
                                            <span class="text-neutral-600">Balance</span>
                                        </div>
                                        <div class="flex items-center gap-2">
                                            <div class="w-3 h-3 rounded-full bg-success-500"></div>
                                            <span class="text-neutral-600">Principal Paid</span>
                                        </div>
                                    </div>
                                </div>
                                
                                <div class="h-72 lg:h-80">
                                    <SfChart @ref="PayoffChart" Theme="Syncfusion.Blazor.Theme.Tailwind">
                                        <ChartArea>
                                            <ChartAreaBorder Width="0" />
                                        </ChartArea>
                                        
                                        <ChartPrimaryXAxis ValueType="Syncfusion.Blazor.Charts.ValueType.DateTime"
                                                          LabelFormat="MMM yy"
                                                          IntervalType="IntervalType.Months"
                                                          EdgeLabelPlacement="EdgeLabelPlacement.Shift">
                                            <ChartAxisMajorGridLines Width="0" />
                                            <ChartAxisLineStyle Width="1" Color="#E5E7EB" />
                                        </ChartPrimaryXAxis>
                                        
                                        <ChartPrimaryYAxis LabelFormat="c0"
                                                          Minimum="0"
                                                          RangePadding="ChartRangePadding.Additional">
                                            <ChartAxisMajorGridLines Width="1" Color="#F3F4F6" DashArray="4,4" />
                                            <ChartAxisLineStyle Width="0" />
                                        </ChartPrimaryYAxis>
                                        
                                        <ChartTooltipSettings Enable="true" 
                                                             Shared="true"
                                                             Format="${point.y:C0}" />
                                        
                                        <ChartSeriesCollection>
                                            @* Remaining Balance Area *@
                                            <ChartSeries DataSource="@GetSelectedStrategyData()"
                                                        XName="Date"
                                                        YName="Balance"
                                                        Name="Remaining Balance"
                                                        Type="ChartSeriesType.SplineArea"
                                                        Fill="url(#balanceGradient)"
                                                        Opacity="0.6">
                                                <ChartSeriesBorder Width="3" Color="#ebbb10" />
                                                <ChartMarker Visible="false" />
                                            </ChartSeries>
                                            
                                            @* Principal Paid *@
                                            <ChartSeries DataSource="@GetSelectedStrategyData()"
                                                        XName="Date"
                                                        YName="PrincipalPaid"
                                                        Name="Principal Paid"
                                                        Type="ChartSeriesType.Spline"
                                                        Width="2">
                                                <ChartSeriesBorder Width="2" Color="#22c55e" />
                                                <ChartMarker Visible="false" />
                                            </ChartSeries>
                                        </ChartSeriesCollection>
                                        
                                        @* Annotations for milestones *@
                                        <ChartAnnotations>
                                            @if (GetSelectedStrategy()?.FirstDebtPaidDate != null)
                                            {
                                                <ChartAnnotation X="@GetSelectedStrategy()?.FirstDebtPaidDate"
                                                                Y="@GetSelectedStrategy()?.FirstDebtPaidBalance"
                                                                CoordinateUnits="Units.Point">
                                                    <ContentTemplate>
                                                        <div class="bg-success-500 text-white text-xs px-2 py-1 rounded-full shadow-lg">
                                                            🎉 First debt paid!
                                                        </div>
                                                    </ContentTemplate>
                                                </ChartAnnotation>
                                            }
                                        </ChartAnnotations>
                                        
                                        @* Gradient Definitions *@
                                        <defs>
                                            <linearGradient id="balanceGradient" x1="0" y1="0" x2="0" y2="1">
                                                <stop offset="0%" stop-color="#ebbb10" stop-opacity="0.4" />
                                                <stop offset="100%" stop-color="#ebbb10" stop-opacity="0.05" />
                                            </linearGradient>
                                        </defs>
                                    </SfChart>
                                </div>
                            </div>
                            
                            @* Payoff Schedule Table *@
                            <div class="card-premium overflow-hidden animate-slide-up stagger-4">
                                <div class="p-5 border-b border-neutral-100">
                                    <h3 class="font-semibold text-neutral-900">@L["Schedule.Title"]</h3>
                                    <p class="text-sm text-neutral-500">
                                        @L["Schedule.Subtitle"]
                                    </p>
                                </div>
                                
                                <PayoffScheduleTable 
                                    Debts="@State.Value.Debts"
                                    Strategy="@GetSelectedStrategy()"
                                    OnDebtPaidCelebration="ShowDebtPaidCelebration" />
                            </div>
                            
                            @* Comparison Summary *@
                            <div class="card-premium p-6 border-l-4 border-gold-500 animate-slide-up stagger-5">
                                <div class="flex items-start gap-4">
                                    <div class="w-12 h-12 rounded-xl bg-gold-100 flex items-center justify-center flex-shrink-0">
                                        <svg class="w-6 h-6 text-gold-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                                  d="M9.663 17h4.673M12 3v1m6.364 1.636l-.707.707M21 12h-1M4 12H3m3.343-5.657l-.707-.707m2.828 9.9a5 5 0 117.072 0l-.548.547A3.374 3.374 0 0014 18.469V19a2 2 0 11-4 0v-.531c0-.895-.356-1.754-.988-2.386l-.548-.547z" />
                                        </svg>
                                    </div>
                                    <div class="flex-1">
                                        <h4 class="font-semibold text-neutral-900 mb-2">
                                            @L["Summary.Title"]
                                        </h4>
                                        <p class="text-neutral-600 text-sm leading-relaxed">
                                            @GetAnalysisSummary()
                                        </p>
                                        
                                        @if (GetSelectedStrategy()?.SavingsVsMinimum > 0)
                                        {
                                            <div class="mt-4 p-4 rounded-xl bg-success-50 border border-success-200">
                                                <div class="flex items-center gap-3">
                                                    <div class="w-10 h-10 rounded-full bg-success-100 flex items-center justify-center">
                                                        <svg class="w-5 h-5 text-success-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                                                  d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                                                        </svg>
                                                    </div>
                                                    <div>
                                                        <p class="text-sm font-medium text-success-800">
                                                            Potential Interest Savings
                                                        </p>
                                                        <p class="text-2xl font-bold text-success-700">
                                                            @GetSelectedStrategy()?.SavingsVsMinimum.ToString("C0")
                                                        </p>
                                                    </div>
                                                </div>
                                            </div>
                                        }
                                    </div>
                                </div>
                                
                                @* Disclaimer *@
                                <div class="mt-4 pt-4 border-t border-neutral-100">
                                    <p class="text-xs text-neutral-400">
                                        @L["Disclaimer"]
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                }
            </div>
        </main>
    </div>
</div>

@* Add/Edit Debt Drawer *@
<SfSidebar @bind-IsOpen="IsDebtDrawerOpen"
           Type="SidebarType.Over"
           Position="SidebarPosition.Right"
           Width="400px"
           EnableGestures="true"
           CloseOnDocumentClick="true">
    <ChildContent>
        <DebtForm 
            Debt="@EditingDebt"
            OnSave="HandleDebtSave"
            OnCancel="() => IsDebtDrawerOpen = false" />
    </ChildContent>
</SfSidebar>

@* Delete Confirmation Dialog *@
<SfDialog @bind-Visible="IsDeleteDialogOpen"
          Width="400px"
          IsModal="true"
          ShowCloseIcon="true">
    <DialogTemplates>
        <Header>
            <span class="font-semibold">Delete Debt</span>
        </Header>
        <Content>
            <p class="text-neutral-600">
                Are you sure you want to remove <strong>@DeletingDebt?.Name</strong> from your debt list?
                This action cannot be undone.
            </p>
        </Content>
    </DialogTemplates>
    <DialogButtons>
        <DialogButton Content="Cancel" 
                     CssClass="e-flat" 
                     OnClick="() => IsDeleteDialogOpen = false" />
        <DialogButton Content="Delete" 
                     CssClass="e-danger" 
                     IsPrimary="true"
                     OnClick="ExecuteDeleteDebt" />
    </DialogButtons>
</SfDialog>

@* Celebration Modal *@
<CelebrationModal @ref="CelebrationModal" />

@code {
    private SfChart? PayoffChart;
    private CelebrationModal? CelebrationModal;
    
    private decimal ExtraPayment { get; set; } = 0;
    private string SelectedStrategy { get; set; } = "Avalanche";
    private Guid? SelectedDebtId { get; set; }
    
    private bool IsDebtDrawerOpen { get; set; }
    private bool IsDeleteDialogOpen { get; set; }
    private DebtDto? EditingDebt { get; set; }
    private DebtDto? DeletingDebt { get; set; }
    
    protected override void OnInitialized()
    {
        Dispatcher.Dispatch(new LoadDebtsAction());
    }
    
    protected override void OnParametersSet()
    {
        // Recalculate strategies when extra payment changes
        if (State.Value.Debts.Any())
        {
            Dispatcher.Dispatch(new CalculateStrategiesAction(ExtraPayment));
        }
    }
    
    private PayoffStrategyDto? GetSelectedStrategy() =>
        State.Value.Strategies.FirstOrDefault(s => s.Name == SelectedStrategy);
    
    private List<PayoffDataPoint> GetSelectedStrategyData() =>
        GetSelectedStrategy()?.MonthlyProjection ?? new();
    
    private void SelectStrategy(string name)
    {
        SelectedStrategy = name;
        PayoffChart?.Refresh();
    }
    
    private void SelectDebt(Guid id)
    {
        SelectedDebtId = SelectedDebtId == id ? null : id;
    }
    
    private void OpenAddDebtDialog()
    {
        EditingDebt = null;
        IsDebtDrawerOpen = true;
    }
    
    private void EditDebt(DebtDto debt)
    {
        EditingDebt = debt;
        IsDebtDrawerOpen = true;
    }
    
    private void ConfirmDeleteDebt(DebtDto debt)
    {
        DeletingDebt = debt;
        IsDeleteDialogOpen = true;
    }
    
    private async Task ExecuteDeleteDebt()
    {
        if (DeletingDebt != null)
        {
            Dispatcher.Dispatch(new DeleteDebtAction(DeletingDebt.Id));
        }
        IsDeleteDialogOpen = false;
        DeletingDebt = null;
    }
    
    private async Task HandleDebtSave(DebtDto debt)
    {
        if (EditingDebt == null)
        {
            Dispatcher.Dispatch(new AddDebtAction(debt));
        }
        else
        {
            Dispatcher.Dispatch(new UpdateDebtAction(debt));
        }
        IsDebtDrawerOpen = false;
        EditingDebt = null;
    }
    
    private string GetAnalysisSummary()
    {
        var strategy = GetSelectedStrategy();
        if (strategy == null) return "";
        
        var debtFreeDate = DateTime.Now.AddMonths(strategy.MonthsToPayoff);
        
        return string.Format(L["Summary.Text"],
            strategy.Name,
            debtFreeDate.ToString("MMMM yyyy"),
            strategy.MonthsToPayoff,
            strategy.TotalInterestPaid.ToString("C0"));
    }
    
    private async Task ShowDebtPaidCelebration(DebtDto debt)
    {
        await CelebrationModal!.ShowAsync(
            $"🎉 {debt.Name} Paid Off!",
            $"One down, {State.Value.Debts.Count - 1} to go!");
    }
}
```

### Strategy Card Component

```razor
@* RAJFinancial.Client/Components/DebtPayoff/StrategyCard.razor *@

<div class="animate-slide-up cursor-pointer group"
     style="animation-delay: @(AnimationDelay)ms;"
     @onclick="OnSelect"
     role="button"
     tabindex="0"
     aria-pressed="@IsSelected">
    
    <div class="relative p-5 rounded-2xl border-2 transition-all duration-300
                @(IsSelected 
                    ? "bg-gold-50 border-gold-500 shadow-gold" 
                    : "bg-white border-neutral-200 hover:border-gold-300 hover:shadow-lg")">
        
        @* Recommended Badge *@
        @if (IsRecommended)
        {
            <div class="absolute -top-3 left-4">
                <span class="inline-flex items-center gap-1 px-3 py-1 rounded-full text-xs font-semibold
                             bg-gold-500 text-neutral-900 shadow-gold">
                    <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M10.868 2.884c-.321-.772-1.415-.772-1.736 0l-1.83 4.401-4.753.381c-.833.067-1.171 1.107-.536 1.651l3.62 3.102-1.106 4.637c-.194.813.691 1.456 1.405 1.02L10 15.591l4.069 2.485c.713.436 1.598-.207 1.404-1.02l-1.106-4.637 3.62-3.102c.635-.544.297-1.584-.536-1.65l-4.752-.382-1.831-4.401z" clip-rule="evenodd" />
                    </svg>
                    Best for Savings
                </span>
            </div>
        }
        
        @* Strategy Header *@
        <div class="flex items-start justify-between mb-4 mt-2">
            <div>
                <h3 class="font-bold text-lg text-neutral-900">@Strategy.Name</h3>
                <p class="text-sm text-neutral-500 mt-0.5">@Strategy.Description</p>
            </div>
            
            @* Selection Indicator *@
            <div class="w-6 h-6 rounded-full border-2 flex items-center justify-center transition-all
                        @(IsSelected 
                            ? "border-gold-500 bg-gold-500" 
                            : "border-neutral-300 group-hover:border-gold-400")">
                @if (IsSelected)
                {
                    <svg class="w-4 h-4 text-white" fill="currentColor" viewBox="0 0 20 20">
                        <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
                    </svg>
                }
            </div>
        </div>
        
        @* Key Metrics *@
        <div class="grid grid-cols-2 gap-4">
            <div>
                <p class="text-xs text-neutral-500 uppercase tracking-wide">Debt Free In</p>
                <p class="text-2xl font-bold text-neutral-900">
                    @Strategy.MonthsToPayoff
                    <span class="text-sm font-normal text-neutral-500">months</span>
                </p>
            </div>
            <div>
                <p class="text-xs text-neutral-500 uppercase tracking-wide">Total Interest</p>
                <p class="text-2xl font-bold @(Strategy.TotalInterestPaid < GetLowestInterest() + 100 ? "text-success-600" : "text-neutral-900")">
                    @Strategy.TotalInterestPaid.ToString("C0")
                </p>
            </div>
        </div>
        
        @* Savings Highlight *@
        @if (Strategy.SavingsVsMinimum > 0)
        {
            <div class="mt-4 pt-4 border-t border-neutral-100">
                <div class="flex items-center gap-2">
                    <svg class="w-5 h-5 text-success-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                              d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
                    </svg>
                    <span class="text-sm text-success-700 font-medium">
                        Save @Strategy.SavingsVsMinimum.ToString("C0") vs. minimum payments
                    </span>
                </div>
            </div>
        }
        
        @* Payoff Order Preview *@
        <div class="mt-4">
            <p class="text-xs text-neutral-500 mb-2">Payoff order:</p>
            <div class="flex flex-wrap gap-1.5">
                @foreach (var (debt, index) in Strategy.PayoffOrder.Take(3).Select((d, i) => (d, i)))
                {
                    <span class="inline-flex items-center gap-1 px-2 py-1 rounded-lg text-xs
                                 @(index == 0 ? "bg-gold-100 text-gold-800" : "bg-neutral-100 text-neutral-600")">
                        <span class="font-medium">@(index + 1).</span>
                        @debt.Name
                    </span>
                }
                @if (Strategy.PayoffOrder.Count > 3)
                {
                    <span class="px-2 py-1 rounded-lg text-xs bg-neutral-100 text-neutral-400">
                        +@(Strategy.PayoffOrder.Count - 3) more
                    </span>
                }
            </div>
        </div>
    </div>
</div>

@code {
    [Parameter] public PayoffStrategyDto Strategy { get; set; } = default!;
    [Parameter] public bool IsRecommended { get; set; }
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public EventCallback OnSelect { get; set; }
    [Parameter] public int AnimationDelay { get; set; }
    
    [CascadingParameter] public IEnumerable<PayoffStrategyDto>? AllStrategies { get; set; }
    
    private decimal GetLowestInterest() =>
        AllStrategies?.Min(s => s.TotalInterestPaid) ?? Strategy.TotalInterestPaid;
}
```

### Debt List Item Component

```razor
@* RAJFinancial.Client/Components/DebtPayoff/DebtListItem.razor *@

<div class="group relative">
    <div class="flex items-center gap-3 p-3 rounded-xl transition-all duration-200
                @(IsSelected 
                    ? "bg-gold-50 border border-gold-200" 
                    : "hover:bg-neutral-50 border border-transparent")"
         @onclick="OnSelect"
         role="button"
         tabindex="0">
        
        @* Debt Type Icon *@
        <div class="w-10 h-10 rounded-lg @GetDebtTypeBackground() 
                    flex items-center justify-center flex-shrink-0">
            <DebtTypeIcon Type="@Debt.DebtType" Class="w-5 h-5" />
        </div>
        
        @* Debt Info *@
        <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
                <h4 class="font-medium text-neutral-900 truncate">@Debt.Name</h4>
                @if (Debt.InterestRate > 0.15m)
                {
                    <span class="px-1.5 py-0.5 rounded text-xs font-medium bg-error-100 text-error-700">
                        High APR
                    </span>
                }
            </div>
            <div class="flex items-center gap-3 mt-0.5 text-sm text-neutral-500">
                <span>@Debt.InterestRate.ToString("P1") APR</span>
                <span>•</span>
                <span>@Debt.MinimumPayment.ToString("C0")/mo</span>
            </div>
        </div>
        
        @* Balance *@
        <div class="text-right">
            <p class="font-semibold text-neutral-900">@Debt.Balance.ToString("C0")</p>
            <p class="text-xs text-neutral-400">balance</p>
        </div>
        
        @* Actions Menu *@
        <div class="opacity-0 group-hover:opacity-100 transition-opacity">
            <SfDropDownButton CssClass="e-flat e-small" 
                             IconCss="e-icons e-more-vertical-1"
                             aria-label="Debt actions">
                <DropDownMenuItems>
                    <DropDownMenuItem Text="Edit" IconCss="e-icons e-edit" 
                                     @onclick="OnEdit" @onclick:stopPropagation="true" />
                    <DropDownMenuItem Text="Delete" IconCss="e-icons e-trash" 
                                     CssClass="e-danger"
                                     @onclick="OnDelete" @onclick:stopPropagation="true" />
                </DropDownMenuItems>
            </SfDropDownButton>
        </div>
    </div>
    
    @* Progress Bar *@
    @if (IsSelected && Debt.OriginalBalance > 0)
    {
        <div class="mt-2 px-3">
            <div class="flex items-center justify-between text-xs text-neutral-500 mb-1">
                <span>Progress</span>
                <span>@GetPayoffProgress().ToString("P0") paid</span>
            </div>
            <div class="h-2 rounded-full bg-neutral-200 overflow-hidden">
                <div class="h-full rounded-full bg-gradient-to-r from-gold-400 to-gold-600 
                            transition-all duration-500"
                     style="width: @(GetPayoffProgress() * 100)%"></div>
            </div>
        </div>
    }
</div>

@code {
    [Parameter] public DebtDto Debt { get; set; } = default!;
    [Parameter] public bool IsSelected { get; set; }
    [Parameter] public EventCallback OnSelect { get; set; }
    [Parameter] public EventCallback OnEdit { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }
    
    private string GetDebtTypeBackground() => Debt.DebtType switch
    {
        DebtType.CreditCard => "bg-purple-100 text-purple-600",
        DebtType.StudentLoan => "bg-blue-100 text-blue-600",
        DebtType.AutoLoan => "bg-cyan-100 text-cyan-600",
        DebtType.Mortgage => "bg-green-100 text-green-600",
        DebtType.PersonalLoan => "bg-orange-100 text-orange-600",
        DebtType.MedicalDebt => "bg-red-100 text-red-600",
        _ => "bg-neutral-100 text-neutral-600"
    };
    
    private decimal GetPayoffProgress()
    {
        if (Debt.OriginalBalance <= 0) return 0;
        return Math.Max(0, Math.Min(1, 1 - (Debt.Balance / Debt.OriginalBalance)));
    }
}
```

---

## Part 3: Insurance Coverage Calculator - Visual Analysis Tool

### Insurance Calculator Page

```razor
@* RAJFinancial.Client/Pages/Tools/InsuranceCalculator.razor *@
@page "/tools/insurance"
@attribute [Authorize]
@inject IStringLocalizer<InsuranceCalculator> L
@inject IState<InsuranceState> State
@inject IDispatcher Dispatcher

<PageTitle>@L["Title"] - RAJ Financial</PageTitle>

<div class="min-h-screen bg-neutral-50">
    <div class="fixed inset-0 gradient-mesh opacity-60 pointer-events-none"></div>
    
    <div class="relative">
        @* Header *@
        <header class="relative overflow-hidden">
            <div class="absolute inset-0 gradient-dark-gold"></div>
            <div class="absolute inset-0 overflow-hidden" aria-hidden="true">
                <div class="absolute -top-20 -right-20 w-64 h-64 rounded-full bg-gold-500/10"></div>
            </div>
            
            <div class="relative px-4 py-8 lg:px-8 lg:py-12">
                <div class="max-w-7xl mx-auto">
                    <nav class="flex items-center gap-2 text-sm text-gold-300 mb-4">
                        <a href="/dashboard" class="hover:text-gold-100">Dashboard</a>
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                        </svg>
                        <a href="/tools" class="hover:text-gold-100">Tools</a>
                        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                        </svg>
                        <span class="text-white">@L["Title"]</span>
                    </nav>
                    
                    <div class="flex items-center gap-3 mb-2">
                        <div class="w-12 h-12 rounded-xl bg-gold-500/20 backdrop-blur flex items-center justify-center">
                            <svg class="w-6 h-6 text-gold-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                      d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                            </svg>
                        </div>
                        <h1 class="text-2xl lg:text-3xl font-bold text-white">@L["Title"]</h1>
                    </div>
                    <p class="text-gold-200 max-w-xl">@L["Subtitle"]</p>
                </div>
            </div>
        </header>
        
        @* Main Content *@
        <main class="relative px-4 py-8 lg:px-8">
            <div class="max-w-7xl mx-auto">
                <div class="grid lg:grid-cols-5 gap-6">
                    
                    @* Left Column - Inputs (2 cols) *@
                    <div class="lg:col-span-2 space-y-6">
                        @* Income & Family *@
                        <div class="card-premium p-6 animate-slide-up">
                            <h3 class="font-semibold text-neutral-900 mb-4 flex items-center gap-2">
                                <svg class="w-5 h-5 text-gold-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                          d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
                                </svg>
                                Income & Family
                            </h3>
                            
                            <div class="space-y-4">
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1.5">
                                        Annual Household Income
                                    </label>
                                    <SfNumericTextBox @bind-Value="@AnnualIncome"
                                                     Format="C0"
                                                     Min="0"
                                                     Step="5000"
                                                     CssClass="w-full" />
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1.5">
                                        Years of Income to Replace
                                    </label>
                                    <div class="flex items-center gap-4">
                                        <SfSlider @bind-Value="@IncomeYears"
                                                  Min="1" Max="20" Step="1"
                                                  Type="SliderType.MinRange"
                                                  CssClass="flex-1">
                                            <SliderTooltipData IsVisible="true" Placement="TooltipPlacement.Before" />
                                        </SfSlider>
                                        <span class="text-lg font-semibold text-neutral-900 w-12 text-right">
                                            @IncomeYears yr
                                        </span>
                                    </div>
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1.5">
                                        Number of Dependents
                                    </label>
                                    <div class="flex gap-2">
                                        @for (int i = 0; i <= 5; i++)
                                        {
                                            var count = i;
                                            <button @onclick="() => Dependents = count"
                                                    class="flex-1 py-2 rounded-lg transition-all
                                                           @(Dependents == count 
                                                               ? "bg-gold-500 text-neutral-900 font-semibold shadow-gold" 
                                                               : "bg-neutral-100 text-neutral-600 hover:bg-gold-100")">
                                                @(i == 5 ? "5+" : i.ToString())
                                            </button>
                                        }
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        @* Debts & Expenses *@
                        <div class="card-premium p-6 animate-slide-up stagger-1">
                            <h3 class="font-semibold text-neutral-900 mb-4 flex items-center gap-2">
                                <svg class="w-5 h-5 text-gold-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                          d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
                                </svg>
                                Debts & Future Expenses
                            </h3>
                            
                            <div class="space-y-4">
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1.5">
                                        Mortgage Balance
                                    </label>
                                    <SfNumericTextBox @bind-Value="@MortgageBalance"
                                                     Format="C0"
                                                     Min="0"
                                                     Step="10000"
                                                     CssClass="w-full" />
                                </div>
                                
                                <div>
                                    <label class="block text-sm font-medium text-neutral-700 mb-1.5">
                                        Other Debts
                                    </label>
                                    <SfNumericTextBox @bind-Value="@OtherDebts"
                                                     Format="C0"
                                                     Min="0"
                                                     Step="1000"
                                                     CssClass="w-full" />
                                </div>
                                
                                <div class="flex items-center justify-between p-3 rounded-lg bg-neutral-50">
                                    <div>
                                        <p class="font-medium text-neutral-900">Include Education Funding</p>
                                        <p class="text-sm text-neutral-500">$50,000 per dependent</p>
                                    </div>
                                    <SfSwitch @bind-Checked="@IncludeEducation" />
                                </div>
                            </div>
                        </div>
                        
                        @* Current Coverage *@
                        <div class="card-premium p-6 animate-slide-up stagger-2">
                            <h3 class="font-semibold text-neutral-900 mb-4 flex items-center gap-2">
                                <svg class="w-5 h-5 text-gold-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                          d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
                                </svg>
                                Current Life Insurance
                            </h3>
                            
                            <div>
                                <label class="block text-sm font-medium text-neutral-700 mb-1.5">
                                    Existing Coverage Amount
                                </label>
                                <SfNumericTextBox @bind-Value="@CurrentCoverage"
                                                 Format="C0"
                                                 Min="0"
                                                 Step="50000"
                                                 CssClass="w-full" />
                                <p class="text-xs text-neutral-500 mt-1.5">
                                    Include employer-provided, term, and whole life policies
                                </p>
                            </div>
                        </div>
                    </div>
                    
                    @* Right Column - Results (3 cols) *@
                    <div class="lg:col-span-3 space-y-6">
                        @* Coverage Gauge *@
                        <div class="card-premium p-6 animate-slide-up stagger-1">
                            <div class="flex flex-col lg:flex-row lg:items-center gap-6">
                                @* Gauge *@
                                <div class="flex-shrink-0">
                                    <CoverageGauge 
                                        CalculatedNeed="@CalculatedNeed"
                                        CurrentCoverage="@CurrentCoverage"
                                        CoverageRatio="@CoverageRatio" />
                                </div>
                                
                                @* Summary Stats *@
                                <div class="flex-1 space-y-4">
                                    <div>
                                        <p class="text-sm text-neutral-500">Calculated Coverage Need</p>
                                        <p class="text-3xl font-bold text-neutral-900">
                                            @CalculatedNeed.ToString("C0")
                                        </p>
                                    </div>
                                    
                                    <div class="grid grid-cols-2 gap-4">
                                        <div>
                                            <p class="text-sm text-neutral-500">Current Coverage</p>
                                            <p class="text-xl font-semibold text-neutral-700">
                                                @CurrentCoverage.ToString("C0")
                                            </p>
                                        </div>
                                        <div>
                                            <p class="text-sm text-neutral-500">Coverage Gap</p>
                                            <p class="text-xl font-semibold @(CoverageGap > 0 ? "text-error-600" : "text-success-600")">
                                                @(CoverageGap > 0 ? CoverageGap.ToString("C0") : "None")
                                            </p>
                                        </div>
                                    </div>
                                    
                                    @* Status Message *@
                                    <div class="p-4 rounded-xl @GetStatusBackgroundClass()">
                                        <div class="flex items-center gap-3">
                                            @if (CoverageRatio >= 1)
                                            {
                                                <div class="w-10 h-10 rounded-full bg-success-200 flex items-center justify-center">
                                                    <svg class="w-5 h-5 text-success-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                                                    </svg>
                                                </div>
                                                <div>
                                                    <p class="font-medium text-success-800">Coverage Looks Good</p>
                                                    <p class="text-sm text-success-700">
                                                        Your current coverage meets the calculated need.
                                                    </p>
                                                </div>
                                            }
                                            else if (CoverageRatio >= 0.7m)
                                            {
                                                <div class="w-10 h-10 rounded-full bg-warning-200 flex items-center justify-center">
                                                    <svg class="w-5 h-5 text-warning-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                                                    </svg>
                                                </div>
                                                <div>
                                                    <p class="font-medium text-warning-800">Partial Coverage</p>
                                                    <p class="text-sm text-warning-700">
                                                        Consider reviewing your coverage to close the @CoverageGap.ToString("C0") gap.
                                                    </p>
                                                </div>
                                            }
                                            else
                                            {
                                                <div class="w-10 h-10 rounded-full bg-error-200 flex items-center justify-center">
                                                    <svg class="w-5 h-5 text-error-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                                                    </svg>
                                                </div>
                                                <div>
                                                    <p class="font-medium text-error-800">Coverage Gap Detected</p>
                                                    <p class="text-sm text-error-700">
                                                        A @CoverageGap.ToString("C0") gap exists between your coverage and calculated need.
                                                    </p>
                                                </div>
                                            }
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        
                        @* Breakdown Chart *@
                        <div class="card-premium p-6 animate-slide-up stagger-2">
                            <h3 class="font-semibold text-neutral-900 mb-6">Coverage Need Breakdown</h3>
                            
                            <div class="grid lg:grid-cols-2 gap-6">
                                @* Donut Chart *@
                                <div class="h-64">
                                    <SfAccumulationChart EnableSmartLabels="true" Theme="Syncfusion.Blazor.Theme.Tailwind">
                                        <AccumulationChartSeriesCollection>
                                            <AccumulationChartSeries DataSource="@GetBreakdownData()"
                                                                    XName="Category"
                                                                    YName="Amount"
                                                                    InnerRadius="60%"
                                                                    Radius="100%"
                                                                    Palettes="@(new[] { "#ebbb10", "#c3922e", "#22c55e", "#3b82f6", "#8b5cf6" })">
                                                <AccumulationDataLabelSettings Visible="true" 
                                                                               Position="AccumulationLabelPosition.Outside"
                                                                               Name="Category">
                                                    <AccumulationChartConnector Length="20px" />
                                                </AccumulationDataLabelSettings>
                                            </AccumulationChartSeries>
                                        </AccumulationChartSeriesCollection>
                                        
                                        <AccumulationChartLegendSettings Visible="false" />
                                        
                                        <AccumulationChartTooltipSettings Enable="true" Format="${point.x}: ${point.y:C0}" />
                                        
                                        @* Center Label *@
                                        <AccumulationChartAnnotations>
                                            <AccumulationChartAnnotation Region="Regions.Chart" X="50%" Y="50%">
                                                <ContentTemplate>
                                                    <div class="text-center">
                                                        <p class="text-xs text-neutral-500">Total Need</p>
                                                        <p class="text-lg font-bold text-neutral-900">@CalculatedNeed.ToString("C0")</p>
                                                    </div>
                                                </ContentTemplate>
                                            </AccumulationChartAnnotation>
                                        </AccumulationChartAnnotations>
                                    </SfAccumulationChart>
                                </div>
                                
                                @* Breakdown List *@
                                <div class="space-y-3">
                                    @foreach (var (item, index) in GetBreakdownData().Select((i, idx) => (i, idx)))
                                    {
                                        <BreakdownItem 
                                            Category="@item.Category"
                                            Amount="@item.Amount"
                                            Percentage="@(item.Amount / CalculatedNeed)"
                                            Description="@item.Description"
                                            ColorIndex="@index" />
                                    }
                                </div>
                            </div>
                        </div>
                        
                        @* Considerations *@
                        <div class="card-premium p-6 border-l-4 border-gold-500 animate-slide-up stagger-3">
                            <div class="flex items-start gap-4">
                                <div class="w-10 h-10 rounded-xl bg-gold-100 flex items-center justify-center flex-shrink-0">
                                    <svg class="w-5 h-5 text-gold-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                                              d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                                    </svg>
                                </div>
                                <div>
                                    <h4 class="font-semibold text-neutral-900 mb-2">Things to Consider</h4>
                                    <ul class="space-y-2 text-sm text-neutral-600">
                                        <li class="flex items-start gap-2">
                                            <svg class="w-4 h-4 text-gold-500 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                                            </svg>
                                            <span>Social Security survivor benefits may reduce your need</span>
                                        </li>
                                        <li class="flex items-start gap-2">
                                            <svg class="w-4 h-4 text-gold-500 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                                            </svg>
                                            <span>Spouse's income and earning potential affect calculations</span>
                                        </li>
                                        <li class="flex items-start gap-2">
                                            <svg class="w-4 h-4 text-gold-500 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                                            </svg>
                                            <span>Consider future expenses like healthcare and long-term care</span>
                                        </li>
                                        <li class="flex items-start gap-2">
                                            <svg class="w-4 h-4 text-gold-500 mt-0.5 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
                                                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                                            </svg>
                                            <span>Employer-provided coverage typically ends when employment ends</span>
                                        </li>
                                    </ul>
                                </div>
                            </div>
                            
                            <div class="mt-4 pt-4 border-t border-neutral-100">
                                <p class="text-xs text-neutral-400">
                                    @L["Disclaimer"]
                                </p>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </main>
    </div>
</div>

@code {
    // Input values
    private decimal AnnualIncome { get; set; } = 75000;
    private int IncomeYears { get; set; } = 10;
    private int Dependents { get; set; } = 2;
    private decimal MortgageBalance { get; set; } = 250000;
    private decimal OtherDebts { get; set; } = 25000;
    private bool IncludeEducation { get; set; } = true;
    private decimal CurrentCoverage { get; set; } = 250000;
    
    // Calculated values
    private decimal IncomeReplacement => AnnualIncome * IncomeYears;
    private decimal DebtPayoff => MortgageBalance + OtherDebts;
    private decimal EducationFunding => IncludeEducation ? Dependents * 50000 : 0;
    private decimal FinalExpenses => 25000; // Funeral, medical, legal
    private decimal EmergencyFund => AnnualIncome * 0.5m; // 6 months
    
    private decimal CalculatedNeed => IncomeReplacement + DebtPayoff + EducationFunding + FinalExpenses + EmergencyFund;
    private decimal CoverageGap => Math.Max(0, CalculatedNeed - CurrentCoverage);
    private decimal CoverageRatio => CalculatedNeed > 0 ? CurrentCoverage / CalculatedNeed : 1;
    
    private string GetStatusBackgroundClass() => CoverageRatio switch
    {
        >= 1 => "bg-success-50 border border-success-200",
        >= 0.7m => "bg-warning-50 border border-warning-200",
        _ => "bg-error-50 border border-error-200"
    };
    
    private List<BreakdownDataPoint> GetBreakdownData() => new()
    {
        new("Income Replacement", IncomeReplacement, $"{IncomeYears} years × {AnnualIncome:C0}"),
        new("Debt Payoff", DebtPayoff, "Mortgage + other debts"),
        new("Education", EducationFunding, $"{Dependents} dependents × $50,000"),
        new("Final Expenses", FinalExpenses, "Funeral, medical, legal"),
        new("Emergency Fund", EmergencyFund, "6 months income")
    };
    
    private record BreakdownDataPoint(string Category, decimal Amount, string Description);
}
```

### Coverage Gauge Component

```razor
@* RAJFinancial.Client/Components/Insurance/CoverageGauge.razor *@

<div class="relative w-48 h-48">
    <svg class="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
        @* Background track *@
        <circle cx="50" cy="50" r="42" 
                fill="none" 
                stroke="#E5E7EB" 
                stroke-width="12"
                stroke-linecap="round" />
        
        @* Progress arc *@
        <circle cx="50" cy="50" r="42" 
                fill="none" 
                stroke="url(#coverageGradient)" 
                stroke-width="12"
                stroke-linecap="round"
                stroke-dasharray="@GetCircumference()"
                stroke-dashoffset="@GetDashOffset()"
                class="transition-all duration-1000 ease-out" />
        
        @* Gradient definitions *@
        <defs>
            <linearGradient id="coverageGradient" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stop-color="@GetGradientStart()" />
                <stop offset="100%" stop-color="@GetGradientEnd()" />
            </linearGradient>
        </defs>
    </svg>
    
    @* Center content *@
    <div class="absolute inset-0 flex flex-col items-center justify-center">
        <span class="text-4xl font-bold @GetRatioColor()">
            @((CoverageRatio * 100).ToString("N0"))%
        </span>
        <span class="text-sm text-neutral-500">Covered</span>
    </div>
    
    @* Status icon *@
    <div class="absolute -bottom-2 left-1/2 -translate-x-1/2">
        <div class="w-10 h-10 rounded-full @GetStatusBgClass() 
                    flex items-center justify-center shadow-lg">
            @if (CoverageRatio >= 1)
            {
                <svg class="w-5 h-5 text-success-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
            }
            else if (CoverageRatio >= 0.7m)
            {
                <svg class="w-5 h-5 text-warning-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01" />
                </svg>
            }
            else
            {
                <svg class="w-5 h-5 text-error-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
            }
        </div>
    </div>
</div>

@code {
    [Parameter] public decimal CalculatedNeed { get; set; }
    [Parameter] public decimal CurrentCoverage { get; set; }
    [Parameter] public decimal CoverageRatio { get; set; }
    
    private const double Circumference = 2 * Math.PI * 42;
    
    private double GetCircumference() => Circumference;
    
    private double GetDashOffset()
    {
        var ratio = Math.Min(1, (double)CoverageRatio);
        return Circumference - (Circumference * ratio);
    }
    
    private string GetGradientStart() => CoverageRatio switch
    {
        >= 1 => "#22c55e",
        >= 0.7m => "#eab308",
        _ => "#ef4444"
    };
    
    private string GetGradientEnd() => CoverageRatio switch
    {
        >= 1 => "#16a34a",
        >= 0.7m => "#ca8a04",
        _ => "#dc2626"
    };
    
    private string GetRatioColor() => CoverageRatio switch
    {
        >= 1 => "text-success-600",
        >= 0.7m => "text-warning-600",
        _ => "text-error-600"
    };
    
    private string GetStatusBgClass() => CoverageRatio switch
    {
        >= 1 => "bg-success-100",
        >= 0.7m => "bg-warning-100",
        _ => "bg-error-100"
    };
}
```

---

## Part 4: Shared UI Components

### Glass Card Component

```razor
@* RAJFinancial.Client/Components/Common/GlassCard.razor *@

<div class="glass rounded-2xl shadow-lg @CssClass">
    <div class="@GetPaddingClass()">
        @ChildContent
    </div>
</div>

@code {
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public string Padding { get; set; } = "md";
    [Parameter] public string? CssClass { get; set; }
    
    private string GetPaddingClass() => Padding switch
    {
        "none" => "",
        "sm" => "p-4",
        "md" => "p-6",
        "lg" => "p-8",
        _ => "p-6"
    };
}
```

### Empty State Component

```razor
@* RAJFinancial.Client/Components/Common/EmptyState.razor *@

<div class="text-center py-12 px-6">
    <div class="w-20 h-20 mx-auto mb-6 rounded-2xl @GetIconBgClass() 
                flex items-center justify-center animate-float">
        <DynamicIcon Name="@Icon" Class="w-10 h-10 @GetIconClass()" />
    </div>
    
    <h3 class="text-xl font-semibold text-neutral-900 mb-2">@Title</h3>
    <p class="text-neutral-500 max-w-sm mx-auto mb-6">@Description</p>
    
    @if (OnAction.HasDelegate)
    {
        <button @onclick="OnAction"
                class="btn-gold inline-flex items-center gap-2">
            @if (!string.IsNullOrEmpty(ActionIcon))
            {
                <DynamicIcon Name="@ActionIcon" Class="w-5 h-5" />
            }
            @ActionText
        </button>
    }
</div>

@code {
    [Parameter] public string Icon { get; set; } = "inbox";
    [Parameter] public string Title { get; set; } = "No items found";
    [Parameter] public string Description { get; set; } = "";
    [Parameter] public string ActionText { get; set; } = "";
    [Parameter] public string? ActionIcon { get; set; }
    [Parameter] public EventCallback OnAction { get; set; }
    [Parameter] public string Variant { get; set; } = "default";
    
    private string GetIconBgClass() => Variant switch
    {
        "success" => "bg-success-100",
        "warning" => "bg-warning-100",
        "error" => "bg-error-100",
        _ => "bg-gold-100"
    };
    
    private string GetIconClass() => Variant switch
    {
        "success" => "text-success-600",
        "warning" => "text-warning-600",
        "error" => "text-error-600",
        _ => "text-gold-600"
    };
}
```

### Celebration Modal Component

```razor
@* RAJFinancial.Client/Components/Common/CelebrationModal.razor *@

<div class="@(IsVisible ? "fixed inset-0 z-50 flex items-center justify-center" : "hidden")">
    @* Backdrop *@
    <div class="absolute inset-0 bg-neutral-900/70 backdrop-blur-sm animate-fade-in"></div>
    
    @* Confetti Animation *@
    @if (IsVisible)
    {
        <div class="absolute inset-0 pointer-events-none overflow-hidden">
            @for (int i = 0; i < 50; i++)
            {
                <div class="confetti-piece"
                     style="left: @(Random.Shared.Next(0, 100))%; 
                            animation-delay: @(Random.Shared.Next(0, 300))ms;
                            background: @GetRandomConfettiColor();"></div>
            }
        </div>
    }
    
    @* Modal *@
    <div class="relative bg-white rounded-3xl shadow-2xl p-8 max-w-sm mx-4 animate-celebrate">
        <div class="text-center">
            @* Trophy/Star Animation *@
            <div class="relative w-24 h-24 mx-auto mb-6">
                <div class="absolute inset-0 rounded-full bg-gold-100 animate-ping opacity-50"></div>
                <div class="relative w-24 h-24 rounded-full gradient-gold 
                            flex items-center justify-center shadow-gold-lg">
                    <span class="text-4xl">🎉</span>
                </div>
            </div>
            
            <h2 class="text-2xl font-bold text-neutral-900 mb-2">@Title</h2>
            <p class="text-neutral-600 mb-6">@Message</p>
            
            <button @onclick="Hide"
                    class="btn-gold w-full">
                Awesome!
            </button>
        </div>
    </div>
</div>

<style>
    .confetti-piece {
        position: absolute;
        width: 10px;
        height: 10px;
        top: -10px;
        animation: confetti-fall 3s ease-out forwards;
    }
    
    @@keyframes confetti-fall {
        0% {
            transform: translateY(0) rotate(0deg);
            opacity: 1;
        }
        100% {
            transform: translateY(100vh) rotate(720deg);
            opacity: 0;
        }
    }
</style>

@code {
    private bool IsVisible { get; set; }
    private string Title { get; set; } = "";
    private string Message { get; set; } = "";
    
    public async Task ShowAsync(string title, string message)
    {
        Title = title;
        Message = message;
        IsVisible = true;
        StateHasChanged();
        
        // Auto-hide after 4 seconds
        await Task.Delay(4000);
        Hide();
    }
    
    private void Hide()
    {
        IsVisible = false;
        StateHasChanged();
    }
    
    private static string GetRandomConfettiColor()
    {
        var colors = new[] { "#ebbb10", "#c3922e", "#22c55e", "#3b82f6", "#8b5cf6", "#ec4899" };
        return colors[Random.Shared.Next(colors.Length)];
    }
}
```

---

## Part 5: Mobile Navigation

### Mobile Bottom Navigation

```razor
@* RAJFinancial.Client/Components/Layout/MobileBottomNav.razor *@

<nav class="lg:hidden fixed bottom-0 inset-x-0 bg-white border-t border-neutral-200 
            safe-area-bottom z-40"
     aria-label="Main navigation">
    <div class="flex items-center justify-around h-16">
        @foreach (var item in NavItems)
        {
            <NavLink href="@item.Href"
                     class="flex flex-col items-center justify-center w-full h-full 
                            transition-colors group"
                     ActiveClass="text-gold-600"
                     Match="NavLinkMatch.Prefix">
                <div class="relative">
                    <DynamicIcon Name="@item.Icon" 
                                Class="w-6 h-6 transition-transform group-hover:scale-110" />
                    @if (item.BadgeCount > 0)
                    {
                        <span class="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-error-500 
                                     text-white text-xs flex items-center justify-center">
                            @item.BadgeCount
                        </span>
                    }
                </div>
                <span class="text-xs mt-1 font-medium">@item.Label</span>
            </NavLink>
        }
    </div>
</nav>

@code {
    private readonly NavItem[] NavItems = new[]
    {
        new NavItem("Dashboard", "/dashboard", "home"),
        new NavItem("Accounts", "/accounts", "credit-card"),
        new NavItem("Assets", "/assets", "briefcase"),
        new NavItem("Beneficiaries", "/beneficiaries", "users"),
        new NavItem("Tools", "/tools", "calculator")
    };
    
    private record NavItem(string Label, string Href, string Icon, int BadgeCount = 0);
}
```

### Desktop Sidebar Navigation

```razor
@* RAJFinancial.Client/Components/Layout/DesktopSidebar.razor *@

<aside class="hidden lg:flex lg:flex-col lg:w-64 lg:fixed lg:inset-y-0 
              bg-neutral-900 text-white">
    @* Logo *@
    <div class="flex items-center gap-3 px-6 py-5 border-b border-neutral-800">
        <img src="/images/logo_only.svg" alt="" class="w-10 h-10" />
        <div>
            <span class="font-bold text-lg text-gold-400">RAJ</span>
            <span class="font-bold text-lg text-white">Financial</span>
        </div>
    </div>
    
    @* Navigation *@
    <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto" aria-label="Main navigation">
        @foreach (var section in NavSections)
        {
            @if (!string.IsNullOrEmpty(section.Title))
            {
                <p class="px-3 py-2 text-xs font-semibold text-neutral-500 uppercase tracking-wider">
                    @section.Title
                </p>
            }
            
            @foreach (var item in section.Items)
            {
                <NavLink href="@item.Href"
                         class="flex items-center gap-3 px-3 py-2.5 rounded-xl 
                                transition-all group"
                         ActiveClass="bg-gold-500/10 text-gold-400"
                         Match="NavLinkMatch.Prefix">
                    <DynamicIcon Name="@item.Icon" 
                                Class="w-5 h-5 transition-colors" />
                    <span class="font-medium">@item.Label</span>
                    @if (item.BadgeCount > 0)
                    {
                        <span class="ml-auto px-2 py-0.5 rounded-full text-xs font-medium
                                     bg-gold-500 text-neutral-900">
                            @item.BadgeCount
                        </span>
                    }
                </NavLink>
            }
        }
    </nav>
    
    @* User Profile *@
    <div class="p-4 border-t border-neutral-800">
        <div class="flex items-center gap-3 p-2 rounded-xl hover:bg-neutral-800 
                    transition-colors cursor-pointer">
            <div class="w-10 h-10 rounded-full bg-gold-500/20 flex items-center justify-center">
                <span class="text-gold-400 font-semibold">@GetInitials()</span>
            </div>
            <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-white truncate">@UserName</p>
                <p class="text-xs text-neutral-500 truncate">@UserEmail</p>
            </div>
            <svg class="w-5 h-5 text-neutral-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                      d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" 
                      d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
        </div>
    </div>
</aside>

@code {
    [CascadingParameter] public UserState? User { get; set; }
    
    private string UserName => User?.FullName ?? "User";
    private string UserEmail => User?.Email ?? "";
    
    private string GetInitials()
    {
        if (string.IsNullOrEmpty(UserName)) return "U";
        var parts = UserName.Split(' ');
        return parts.Length > 1 
            ? $"{parts[0][0]}{parts[^1][0]}" 
            : UserName[..1].ToUpper();
    }
    
    private readonly NavSection[] NavSections = new[]
    {
        new NavSection("", new[]
        {
            new NavItem("Dashboard", "/dashboard", "home"),
        }),
        new NavSection("Financial", new[]
        {
            new NavItem("Linked Accounts", "/accounts", "link"),
            new NavItem("Manual Assets", "/assets", "briefcase"),
            new NavItem("Beneficiaries", "/beneficiaries", "users"),
        }),
        new NavSection("Planning Tools", new[]
        {
            new NavItem("Debt Payoff", "/tools/debt-payoff", "trending-down"),
            new NavItem("Insurance Calculator", "/tools/insurance", "shield"),
            new NavItem("Estate Checklist", "/tools/estate", "clipboard-list"),
        }),
        new NavSection("Account", new[]
        {
            new NavItem("Settings", "/settings", "settings"),
            new NavItem("Help & Support", "/help", "help-circle"),
        })
    };
    
    private record NavSection(string Title, NavItem[] Items);
    private record NavItem(string Label, string Href, string Icon, int BadgeCount = 0);
}
```

---

## Summary

This document provides a complete UI implementation for RAJ Financial Software with:

1. **RAJ Financial Brand Theme** - Gold color palette (#ebbb10, #c3922e, #eed688, #fffbcc), Nexa font, premium shadows

2. **Debt Payoff Analyzer** - Interactive strategy comparison (Avalanche vs Snowball), timeline visualization, payoff schedule, celebration animations

3. **Insurance Coverage Calculator** - Visual gauge, breakdown chart, real-time calculations, consideration highlights

4. **Shared Components** - Glass cards, empty states, celebration modal, animated numbers

5. **Navigation** - Mobile bottom nav, desktop sidebar with gold accents

The design emphasizes:
- **Premium feel** with gold gradients and sophisticated shadows
- **Delight** through micro-animations and celebrations
- **Trust** through professional typography and clear data visualization
- **Accessibility** with WCAG-compliant contrast and keyboard navigation
- **Mobile-first** responsive layouts

All components follow the development guidelines for security, localization, and observability.
