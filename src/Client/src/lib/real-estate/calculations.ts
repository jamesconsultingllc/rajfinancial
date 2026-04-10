import type {
  PropertyInputs, PITIBreakdown, CashFlowResult, AmortizationRow,
  BreakevenResult, RefinanceResult, FlipResult, HouseHackResult,
  ScenarioResult, StrategyScore, MarketData
} from './types';

export function calculateMonthlyPayment(principal: number, annualRate: number, termYears: number): number {
  if (principal <= 0 || termYears <= 0) return 0;
  const r = annualRate / 100 / 12;
  const n = termYears * 12;
  if (r === 0) return principal / n;
  return principal * (r * Math.pow(1 + r, n)) / (Math.pow(1 + r, n) - 1);
}

export function calculatePITI(inputs: PropertyInputs, marketData: MarketData): PITIBreakdown {
  const loanAmount = inputs.purchasePrice * (1 - inputs.downPaymentPct / 100);
  const monthlyPI = calculateMonthlyPayment(loanAmount, inputs.interestRate, inputs.loanTermYears);

  const r = inputs.interestRate / 100 / 12;
  const n = inputs.loanTermYears * 12;
  const firstInterest = loanAmount * r;
  const firstPrincipal = n > 0 ? monthlyPI - firstInterest : 0;

  const monthlyTax = (inputs.purchasePrice * marketData.taxRate / 100) / 12;
  const monthlyInsurance = (inputs.purchasePrice * marketData.insuranceRate / 100) / 12;

  const pmi = inputs.downPaymentPct < 20
    ? (loanAmount * 0.005) / 12
    : 0;

  return {
    principal: firstPrincipal,
    interest: firstInterest,
    tax: monthlyTax,
    insurance: monthlyInsurance,
    pmi,
    hoa: inputs.hoaMonthly,
    total: monthlyPI + monthlyTax + monthlyInsurance + pmi + inputs.hoaMonthly,
  };
}

export function generateAmortizationSchedule(
  principal: number, annualRate: number, termYears: number,
  purchasePrice: number, downPaymentPct: number, appreciationRate: number
): AmortizationRow[] {
  const r = annualRate / 100 / 12;
  const n = termYears * 12;
  const payment = calculateMonthlyPayment(principal, annualRate, termYears);
  const rows: AmortizationRow[] = [];
  let balance = principal;

  for (let month = 1; month <= n; month++) {
    const interest = balance * r;
    const princ = payment - interest;
    balance -= princ;
    const homeValue = purchasePrice * Math.pow(1 + appreciationRate / 100 / 12, month);
    const loanBalance = Math.max(0, balance);
    const totalEquity = homeValue - loanBalance;

    rows.push({
      month,
      payment,
      principal: princ,
      interest,
      balance: loanBalance,
      totalEquity,
    });
  }
  return rows;
}

export function calculateCashFlow(inputs: PropertyInputs, piti: PITIBreakdown): CashFlowResult {
  const grossRent = inputs.monthlyRent;
  const vacancy = grossRent * inputs.vacancyRate / 100;
  const effectiveRent = grossRent - vacancy;
  const propertyMgmt = effectiveRent * inputs.propertyMgmtPct / 100;
  const maintenance = (inputs.purchasePrice * inputs.maintenancePct / 100) / 12;
  const capex = (inputs.purchasePrice * inputs.capexPct / 100) / 12;
  const totalExpenses = propertyMgmt + maintenance + capex;
  const netCashFlow = effectiveRent - totalExpenses - piti.total;
  const downPayment = inputs.purchasePrice * inputs.downPaymentPct / 100;
  const closingCosts = inputs.purchasePrice * 0.03;
  const totalCashInvested = downPayment + closingCosts;
  const annualCashFlow = netCashFlow * 12;

  const noi = (effectiveRent - totalExpenses) * 12;
  const capRate = inputs.purchasePrice > 0 ? (noi / inputs.purchasePrice) * 100 : 0;

  return {
    grossRent,
    vacancy,
    effectiveRent,
    propertyMgmt,
    maintenance,
    capex,
    totalExpenses,
    piti: piti.total,
    netCashFlow,
    cashOnCashReturn: totalCashInvested > 0 ? (annualCashFlow / totalCashInvested) * 100 : 0,
    capRate,
  };
}

export function calculateBreakeven(inputs: PropertyInputs, marketData: MarketData): BreakevenResult {
  const loanAmount = inputs.purchasePrice * (1 - inputs.downPaymentPct / 100);
  const monthlyPayment = calculateMonthlyPayment(loanAmount, inputs.interestRate, inputs.loanTermYears);
  const piti = calculatePITI(inputs, marketData);
  const cashFlow = calculateCashFlow(inputs, piti);
  const downPayment = inputs.purchasePrice * inputs.downPaymentPct / 100;
  const closingCosts = inputs.purchasePrice * 0.03;
  const sellingCostsPct = 0.06;

  const monthlyData: { month: number; totalCost: number; totalReturn: number }[] = [];
  const r = inputs.interestRate / 100 / 12;
  let balance = loanAmount;
  let totalCost = downPayment + closingCosts;
  let totalReturn = 0;
  const maxMonths = inputs.loanTermYears * 12;

  for (let month = 1; month <= maxMonths; month++) {
    if (balance <= 0) break;
    const interest = balance * r;
    const principal = monthlyPayment - interest;
    balance -= principal;

    const monthlyCost = cashFlow.netCashFlow < 0 ? Math.abs(cashFlow.netCashFlow) : 0;
    totalCost += monthlyCost;

    const homeValue = inputs.purchasePrice * Math.pow(1 + inputs.appreciationRate / 100 / 12, month);
    const netProceeds = homeValue * (1 - sellingCostsPct) - Math.max(0, balance);
    const cumulativeCashFlow = cashFlow.netCashFlow > 0 ? cashFlow.netCashFlow * month : 0;
    totalReturn = netProceeds + cumulativeCashFlow;

    if (month % 3 === 0 || month <= 12) {
      monthlyData.push({ month, totalCost, totalReturn });
    }

    if (totalReturn >= totalCost && month > 1) {
      return {
        months: month,
        years: Math.round(month / 12 * 10) / 10,
        totalCostAtBreakeven: totalCost,
        totalReturnAtBreakeven: totalReturn,
        monthlyData,
      };
    }
  }

  return {
    months: maxMonths,
    years: inputs.loanTermYears,
    totalCostAtBreakeven: totalCost,
    totalReturnAtBreakeven: totalReturn,
    monthlyData,
  };
}

export function calculateRefinance(inputs: PropertyInputs): RefinanceResult {
  const balance = inputs.currentLoanBalance || inputs.purchasePrice * (1 - inputs.downPaymentPct / 100);
  const currentPayment = calculateMonthlyPayment(balance, inputs.currentRate || inputs.interestRate, Math.ceil((inputs.currentRemainingMonths || 360) / 12));
  const closingCosts = balance * (inputs.refiClosingCostPct || 2) / 100;
  const remainingMonths = inputs.currentRemainingMonths || 360;

  const scenarios: RefinanceResult['scenarios'] = [];
  let triggerRate = inputs.currentRate || inputs.interestRate;
  const discountRate = 0.04 / 12;

  for (let rateDrop = 0.25; rateDrop <= 3.0; rateDrop += 0.25) {
    const newRate = (inputs.currentRate || inputs.interestRate) - rateDrop;
    if (newRate < 0.5) break;

    const newPayment = calculateMonthlyPayment(balance + closingCosts, newRate, 30);
    const monthlySavings = currentPayment - newPayment;

    const breakevenMonths = monthlySavings > 0 ? Math.ceil(closingCosts / monthlySavings) : 999;

    let npv = -closingCosts;
    for (let m = 1; m <= Math.min(remainingMonths, 360); m++) {
      npv += monthlySavings / Math.pow(1 + discountRate, m);
    }

    scenarios.push({
      rate: newRate,
      newPayment,
      savings: monthlySavings,
      breakevenMonths,
      npvSavings: npv,
    });

    if (npv > 0 && triggerRate === (inputs.currentRate || inputs.interestRate)) {
      triggerRate = newRate;
    }
  }

  const bestByNpv = scenarios.length > 0
    ? scenarios.reduce((best, s) => s.npvSavings > best.npvSavings ? s : best, scenarios[0])
    : null;

  return {
    triggerRate,
    monthlySavings: bestByNpv?.savings || 0,
    breakevenMonths: bestByNpv?.breakevenMonths || 0,
    lifetimeSavings: bestByNpv ? bestByNpv.savings * (inputs.currentRemainingMonths || 360) : 0,
    scenarios,
  };
}

export function calculateFlipROI(inputs: PropertyInputs): FlipResult {
  const holdingMonths = 6;
  const loanAmount = inputs.purchasePrice * (1 - inputs.downPaymentPct / 100);
  const monthlyCarrying = (loanAmount * (inputs.interestRate + 2) / 100 / 12) + 500;
  const carryingCosts = monthlyCarrying * holdingMonths;
  const arv = inputs.purchasePrice * (inputs.arvMultiplier || 1.3);
  const sellingCosts = arv * 0.08;
  const totalInvestment = inputs.purchasePrice + inputs.renovationBudget + carryingCosts;
  const netProfit = arv - totalInvestment - sellingCosts;
  const cashInvested = inputs.purchasePrice * inputs.downPaymentPct / 100 + inputs.renovationBudget;
  const roi = cashInvested > 0 ? (netProfit / cashInvested) * 100 : 0;
  const annualizedROI = roi * (12 / holdingMonths);

  return {
    totalInvestment,
    arv,
    sellingCosts,
    netProfit,
    roi,
    annualizedROI,
    carryingCosts,
  };
}

export function calculateHouseHack(inputs: PropertyInputs, piti: PITIBreakdown): HouseHackResult {
  const rentalIncome = inputs.numRentalUnits * inputs.ownerUnitRent * (1 - inputs.vacancyRate / 100);
  const netCostOfLiving = piti.total - rentalIncome;
  const savingsVsRenting = inputs.monthlyPersonalRent - netCostOfLiving;

  return {
    totalPITI: piti.total,
    rentalIncome,
    netCostOfLiving: Math.max(0, netCostOfLiving),
    savingsVsRenting,
    effectiveHousingCost: netCostOfLiving,
  };
}

export function runScenarios(inputs: PropertyInputs, marketData: MarketData): ScenarioResult[] {
  const basePiti = calculatePITI(inputs, marketData);
  const baseCashFlow = calculateCashFlow(inputs, basePiti);
  const loanAmount = inputs.purchasePrice * (1 - inputs.downPaymentPct / 100);
  const scenarios: ScenarioResult[] = [];

  for (const bump of [0.5, 1.0, 1.5, 2.0]) {
    const shockedRate = inputs.interestRate + bump;
    const shockedInputs = { ...inputs, interestRate: shockedRate };
    const shockedPiti = calculatePITI(shockedInputs, marketData);
    const shockedCashFlow = calculateCashFlow(shockedInputs, shockedPiti);
    const schedule = generateAmortizationSchedule(
      loanAmount, shockedRate, inputs.loanTermYears,
      inputs.purchasePrice, inputs.downPaymentPct, inputs.appreciationRate
    );
    const equityAt5 = schedule[59]?.totalEquity || 0;

    scenarios.push({
      label: `Rate +${bump}% (${shockedRate.toFixed(2)}%)`,
      monthlyPayment: shockedPiti.total,
      monthlyCashFlow: shockedCashFlow.netCashFlow,
      paymentChange: shockedPiti.total - basePiti.total,
      cashFlowChange: shockedCashFlow.netCashFlow - baseCashFlow.netCashFlow,
      equityAt5Years: equityAt5,
    });
  }

  for (const decline of [5, 10, 15, 20]) {
    const declinedPrice = inputs.purchasePrice * (1 - decline / 100);
    const declinedInputs = { ...inputs, purchasePrice: declinedPrice };
    const declinedPiti = calculatePITI(declinedInputs, marketData);
    const declinedCashFlow = calculateCashFlow(declinedInputs, declinedPiti);
    const declinedLoan = declinedPrice * (1 - inputs.downPaymentPct / 100);
    const schedule = generateAmortizationSchedule(
      declinedLoan, inputs.interestRate, inputs.loanTermYears,
      declinedPrice, inputs.downPaymentPct, inputs.appreciationRate
    );
    const equityAt5 = schedule[59]?.totalEquity || 0;

    scenarios.push({
      label: `Price -${decline}% ($${(declinedPrice / 1000).toFixed(0)}K)`,
      monthlyPayment: declinedPiti.total,
      monthlyCashFlow: declinedCashFlow.netCashFlow,
      paymentChange: declinedPiti.total - basePiti.total,
      cashFlowChange: declinedCashFlow.netCashFlow - baseCashFlow.netCashFlow,
      equityAt5Years: equityAt5,
    });
  }

  return scenarios;
}

export function scoreStrategies(inputs: PropertyInputs, marketData: MarketData): StrategyScore[] {
  const piti = calculatePITI(inputs, marketData);
  const cashFlow = calculateCashFlow(inputs, piti);
  const breakeven = calculateBreakeven(inputs, marketData);
  const refi = calculateRefinance(inputs);
  const flip = calculateFlipROI(inputs);
  const houseHack = calculateHouseHack(inputs, piti);
  const scores: StrategyScore[] = [];

  let buyScore = 50;
  if (cashFlow.netCashFlow > 0) buyScore += 20;
  if (cashFlow.capRate > 5) buyScore += 10;
  if (breakeven.years < 5) buyScore += 10;
  if (inputs.interestRate < 6.5) buyScore += 10;
  if (cashFlow.netCashFlow < -500) buyScore -= 20;
  if (breakeven.years > 10) buyScore -= 10;
  scores.push({
    strategy: 'buy', label: 'Buy Now',
    score: Math.max(0, Math.min(100, buyScore)),
    verdict: buyScore >= 70 ? 'strong' : buyScore >= 45 ? 'moderate' : 'weak',
    reasons: [
      cashFlow.netCashFlow > 0 ? `Positive cash flow: $${cashFlow.netCashFlow.toFixed(0)}/mo` : `Negative cash flow: $${cashFlow.netCashFlow.toFixed(0)}/mo`,
      `Breakeven in ${breakeven.years} years`,
      `Cap rate: ${cashFlow.capRate.toFixed(1)}%`,
    ],
    keyMetric: 'Monthly Cash Flow',
    keyMetricValue: `$${cashFlow.netCashFlow.toFixed(0)}`,
  });

  let waitScore = 50;
  if (inputs.interestRate > 7) waitScore += 20;
  if (cashFlow.netCashFlow < -300) waitScore += 15;
  if (inputs.appreciationRate > 4) waitScore -= 20;
  if (inputs.interestRate < 6) waitScore -= 15;
  if (cashFlow.capRate > 6) waitScore -= 10;
  const monthlyRentCost = inputs.monthlyPersonalRent || 1500;
  scores.push({
    strategy: 'wait', label: 'Wait',
    score: Math.max(0, Math.min(100, waitScore)),
    verdict: waitScore >= 70 ? 'strong' : waitScore >= 45 ? 'moderate' : 'weak',
    reasons: [
      inputs.interestRate > 7 ? 'Rates are elevated — may come down' : 'Rates are moderate',
      `Renting costs $${monthlyRentCost.toFixed(0)}/mo while waiting`,
      inputs.appreciationRate > 3 ? 'Prices rising — waiting costs equity' : 'Price growth is moderate',
    ],
    keyMetric: 'Cost of Waiting (1yr)',
    keyMetricValue: `$${(monthlyRentCost * 12).toLocaleString()}`,
  });

  let refiScore = 30;
  const currentRate = inputs.currentRate || inputs.interestRate;
  if (refi.triggerRate < currentRate - 0.5) refiScore += 30;
  if (refi.scenarios.some(s => s.npvSavings > 10000)) refiScore += 20;
  if (refi.scenarios.some(s => s.breakevenMonths < 24)) refiScore += 10;
  if (currentRate < 5) refiScore -= 20;
  scores.push({
    strategy: 'refinance', label: 'Refinance',
    score: Math.max(0, Math.min(100, refiScore)),
    verdict: refiScore >= 70 ? 'strong' : refiScore >= 45 ? 'moderate' : 'weak',
    reasons: [
      `Trigger rate: ${refi.triggerRate.toFixed(2)}%`,
      refi.scenarios.length > 0 ? `Best savings: $${refi.scenarios[refi.scenarios.length - 1]?.savings.toFixed(0)}/mo` : 'No beneficial refi scenarios',
      `Current rate: ${currentRate.toFixed(2)}%`,
    ],
    keyMetric: 'Trigger Rate',
    keyMetricValue: `${refi.triggerRate.toFixed(2)}%`,
  });

  let hackScore = 50;
  if (houseHack.savingsVsRenting > 500) hackScore += 25;
  if (houseHack.netCostOfLiving < 500) hackScore += 15;
  if (houseHack.rentalIncome > piti.total * 0.5) hackScore += 10;
  if (inputs.numRentalUnits === 0) hackScore -= 30;
  scores.push({
    strategy: 'houseHack', label: 'House-Hack',
    score: Math.max(0, Math.min(100, hackScore)),
    verdict: hackScore >= 70 ? 'strong' : hackScore >= 45 ? 'moderate' : 'weak',
    reasons: [
      `Net housing cost: $${houseHack.netCostOfLiving.toFixed(0)}/mo`,
      `Saves $${houseHack.savingsVsRenting.toFixed(0)}/mo vs renting`,
      `${inputs.numRentalUnits} rental unit(s) generating $${houseHack.rentalIncome.toFixed(0)}/mo`,
    ],
    keyMetric: 'Net Housing Cost',
    keyMetricValue: `$${houseHack.netCostOfLiving.toFixed(0)}/mo`,
  });

  let flipScore = 40;
  if (flip.roi > 20) flipScore += 25;
  if (flip.annualizedROI > 30) flipScore += 15;
  if (flip.netProfit > 30000) flipScore += 10;
  if (flip.roi < 10) flipScore -= 20;
  if (flip.netProfit < 0) flipScore -= 30;
  scores.push({
    strategy: 'flip', label: 'Flip',
    score: Math.max(0, Math.min(100, flipScore)),
    verdict: flipScore >= 70 ? 'strong' : flipScore >= 45 ? 'moderate' : 'weak',
    reasons: [
      `Projected profit: $${flip.netProfit.toLocaleString(undefined, { maximumFractionDigits: 0 })}`,
      `ROI: ${flip.roi.toFixed(1)}%`,
      `ARV: $${flip.arv.toLocaleString(undefined, { maximumFractionDigits: 0 })}`,
    ],
    keyMetric: 'Net Profit',
    keyMetricValue: `$${flip.netProfit.toLocaleString(undefined, { maximumFractionDigits: 0 })}`,
  });

  let holdScore = 50;
  if (cashFlow.netCashFlow > 0) holdScore += 15;
  if (inputs.appreciationRate > 3) holdScore += 15;
  if (breakeven.years < 7) holdScore += 10;
  if (cashFlow.cashOnCashReturn > 8) holdScore += 10;
  if (cashFlow.netCashFlow < -500) holdScore -= 15;
  scores.push({
    strategy: 'hold', label: 'Hold Long-Term',
    score: Math.max(0, Math.min(100, holdScore)),
    verdict: holdScore >= 70 ? 'strong' : holdScore >= 45 ? 'moderate' : 'weak',
    reasons: [
      `Cash-on-cash: ${cashFlow.cashOnCashReturn.toFixed(1)}%`,
      `${inputs.appreciationRate}% annual appreciation`,
      `Breakeven in ${breakeven.years} years`,
    ],
    keyMetric: 'Cash-on-Cash',
    keyMetricValue: `${cashFlow.cashOnCashReturn.toFixed(1)}%`,
  });

  return scores.sort((a, b) => b.score - a.score);
}

export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value);
}

export function formatPct(value: number): string {
  return `${value.toFixed(2)}%`;
}
