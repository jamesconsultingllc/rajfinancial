export interface PropertyInputs {
  purchasePrice: number;
  downPaymentPct: number;
  interestRate: number;
  loanTermYears: number;
  zipCode: string;
  state: string;
  monthlyRent: number;
  vacancyRate: number;
  propertyMgmtPct: number;
  maintenancePct: number;
  capexPct: number;
  hoaMonthly: number;
  appreciationRate: number;
  monthlyPersonalRent: number;
  renovationBudget: number;
  arvMultiplier: number;
  numRentalUnits: number;
  ownerUnitRent: number;
  currentLoanBalance: number;
  currentRate: number;
  currentRemainingMonths: number;
  refiClosingCostPct: number;
}

export interface PITIBreakdown {
  principal: number;
  interest: number;
  tax: number;
  insurance: number;
  pmi: number;
  hoa: number;
  total: number;
}

export interface CashFlowResult {
  grossRent: number;
  vacancy: number;
  effectiveRent: number;
  propertyMgmt: number;
  maintenance: number;
  capex: number;
  totalExpenses: number;
  piti: number;
  netCashFlow: number;
  cashOnCashReturn: number;
  capRate: number;
}

export interface AmortizationRow {
  month: number;
  payment: number;
  principal: number;
  interest: number;
  balance: number;
  totalEquity: number;
}

export interface BreakevenResult {
  months: number;
  years: number;
  totalCostAtBreakeven: number;
  totalReturnAtBreakeven: number;
  monthlyData: { month: number; totalCost: number; totalReturn: number }[];
}

export interface RefinanceResult {
  triggerRate: number;
  monthlySavings: number;
  breakevenMonths: number;
  lifetimeSavings: number;
  scenarios: {
    rate: number;
    newPayment: number;
    savings: number;
    breakevenMonths: number;
    npvSavings: number;
  }[];
}

export interface FlipResult {
  totalInvestment: number;
  arv: number;
  sellingCosts: number;
  netProfit: number;
  roi: number;
  annualizedROI: number;
  carryingCosts: number;
}

export interface HouseHackResult {
  totalPITI: number;
  rentalIncome: number;
  netCostOfLiving: number;
  savingsVsRenting: number;
  effectiveHousingCost: number;
}

export interface ScenarioResult {
  label: string;
  monthlyPayment: number;
  monthlyCashFlow: number;
  paymentChange: number;
  cashFlowChange: number;
  equityAt5Years: number;
}

export interface StrategyScore {
  strategy: 'buy' | 'wait' | 'refinance' | 'houseHack' | 'flip' | 'hold';
  label: string;
  score: number;
  verdict: 'strong' | 'moderate' | 'weak';
  reasons: string[];
  keyMetric: string;
  keyMetricValue: string;
}

export interface MarketData {
  taxRate: number;
  insuranceRate: number;
  medianPrice: number;
  rentToPrice: number;
  renovationCostPerSqft: { light: number; medium: number; heavy: number };
  appreciationRate: number;
  region: string;
}

export interface RateDataPoint {
  date: string;
  rate30yr: number;
  rate15yr: number;
}

export type TabId = 'market' | 'analyzer' | 'strategies' | 'refinance' | 'scenarios';
