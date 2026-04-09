import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, AreaChart, Area, XAxis, YAxis, CartesianGrid } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import MetricCard from './MetricCard';
import { cn } from "@/lib/utils";
import type { PropertyInputs, MarketData } from '@/lib/real-estate/types';
import { calculatePITI, calculateCashFlow, generateAmortizationSchedule, formatCurrency } from '@/lib/real-estate/calculations';

const CHART_COLORS = {
  grid: 'hsl(40, 15%, 25%)',
  axis: 'hsl(30, 8%, 45%)',
};

interface Props {
  inputs: PropertyInputs;
  marketData: MarketData;
}

const COLORS = ['hsl(48, 91%, 49%)', 'hsl(280, 60%, 55%)', 'hsl(40, 90%, 55%)', 'hsl(160, 60%, 45%)', 'hsl(0, 84%, 50%)', 'hsl(30, 8%, 45%)'];

export default function PropertyAnalysis({ inputs, marketData }: Props) {
  const piti = calculatePITI(inputs, marketData);
  const cashFlow = calculateCashFlow(inputs, piti);
  const loanAmount = inputs.purchasePrice * (1 - inputs.downPaymentPct / 100);

  const amort = generateAmortizationSchedule(
    loanAmount, inputs.interestRate, inputs.loanTermYears,
    inputs.purchasePrice, inputs.downPaymentPct, inputs.appreciationRate
  );

  const pitiData = [
    { name: 'Principal', value: piti.principal },
    { name: 'Interest', value: piti.interest },
    { name: 'Tax', value: piti.tax },
    { name: 'Insurance', value: piti.insurance },
    ...(piti.pmi > 0 ? [{ name: 'PMI', value: piti.pmi }] : []),
    ...(piti.hoa > 0 ? [{ name: 'HOA', value: piti.hoa }] : []),
  ];

  const yearlyAmort = amort.filter((_, i) => (i + 1) % 12 === 0).map(row => ({
    year: Math.ceil(row.month / 12),
    equity: row.totalEquity,
    balance: row.balance,
  }));

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard label="Total PITI" value={formatCurrency(piti.total)} sublabel="Monthly payment" color="gold" />
        <MetricCard
          label="Net Cash Flow"
          value={formatCurrency(cashFlow.netCashFlow)}
          sublabel="Monthly (rental)"
          color={cashFlow.netCashFlow >= 0 ? 'green' : 'red'}
        />
        <MetricCard label="Cap Rate" value={`${cashFlow.capRate.toFixed(1)}%`} sublabel="Annual NOI / Price" color="purple" />
        <MetricCard label="Cash-on-Cash" value={`${cashFlow.cashOnCashReturn.toFixed(1)}%`} sublabel="Annual return" color="amber" />
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        <Card className="border-border/50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Monthly Payment Breakdown</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="h-56">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie data={pitiData} cx="50%" cy="50%" innerRadius={55} outerRadius={85} dataKey="value" paddingAngle={2}>
                    {pitiData.map((_, index) => (
                      <Cell key={index} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <Tooltip
                    contentStyle={{ backgroundColor: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: '8px', color: 'hsl(var(--popover-foreground))' }}
                    formatter={(value: string | number | (string | number)[]) => [formatCurrency(Number(value)), '']}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
            <div className="flex flex-wrap gap-3 mt-2 justify-center">
              {pitiData.map((item, i) => (
                <div key={item.name} className="flex items-center gap-1.5 text-xs">
                  <div className="w-2.5 h-2.5 rounded-full" style={{ backgroundColor: COLORS[i] }} />
                  <span className="text-muted-foreground">{item.name}: {formatCurrency(item.value)}</span>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card className="border-border/50">
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Cash Flow Breakdown (Monthly)</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-2 text-sm">
              <Row label="Gross Rent" value={cashFlow.grossRent} positive />
              <Row label="Vacancy" value={-cashFlow.vacancy} />
              <div className="border-t border-border/50 pt-1">
                <Row label="Effective Rent" value={cashFlow.effectiveRent} positive bold />
              </div>
              <Row label="Property Mgmt" value={-cashFlow.propertyMgmt} />
              <Row label="Maintenance" value={-cashFlow.maintenance} />
              <Row label="CapEx Reserve" value={-cashFlow.capex} />
              <Row label="PITI + HOA" value={-cashFlow.piti} />
              <div className="border-t border-border pt-2 mt-2">
                <Row label="Net Cash Flow" value={cashFlow.netCashFlow} bold positive={cashFlow.netCashFlow >= 0} />
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      <Card className="border-border/50">
        <CardHeader className="pb-2">
          <CardTitle className="text-sm">Equity Growth Over Time (with {inputs.appreciationRate}% appreciation)</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={yearlyAmort} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
                <XAxis dataKey="year" stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} label={{ value: 'Year', position: 'insideBottom', offset: -2, style: { fill: CHART_COLORS.axis, fontSize: 11 } }} />
                <YAxis stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} tickFormatter={(v: number) => `$${(v / 1000).toFixed(0)}K`} />
                <Tooltip
                  contentStyle={{ backgroundColor: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: '8px', color: 'hsl(var(--popover-foreground))' }}
                  formatter={(value: string | number | (string | number)[], name: string) => [formatCurrency(Number(value)), name === 'equity' ? 'Total Equity' : 'Loan Balance']}
                />
                <Area type="monotone" dataKey="equity" stroke="hsl(160, 60%, 45%)" fill="hsl(160, 60%, 45%)" fillOpacity={0.15} strokeWidth={2} />
                <Area type="monotone" dataKey="balance" stroke="hsl(0, 84%, 50%)" fill="hsl(0, 84%, 50%)" fillOpacity={0.1} strokeWidth={2} />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function Row({ label, value, positive, bold }: { label: string; value: number; positive?: boolean; bold?: boolean }) {
  return (
    <div className={cn("flex justify-between", bold ? "font-semibold text-foreground" : "text-foreground/80")}>
      <span>{label}</span>
      <span className={cn({
        'text-[hsl(var(--success))]': value >= 0 && positive,
        'text-destructive': value < 0,
      })}>
        {value >= 0 ? '+' : ''}{formatCurrency(value)}
      </span>
    </div>
  );
}
