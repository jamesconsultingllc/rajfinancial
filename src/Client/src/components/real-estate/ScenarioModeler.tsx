import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine, Cell } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import type { PropertyInputs, MarketData } from '@/lib/real-estate/types';
import { runScenarios, calculatePITI, calculateCashFlow, formatCurrency } from '@/lib/real-estate/calculations';

const CHART_COLORS = {
  primary: 'hsl(48, 91%, 49%)',
  positive: 'hsl(160, 60%, 45%)',
  negative: 'hsl(0, 84%, 50%)',
  grid: 'hsl(40, 15%, 25%)',
  axis: 'hsl(30, 8%, 45%)',
};

interface Props {
  inputs: PropertyInputs;
  marketData: MarketData;
}

export default function ScenarioModeler({ inputs, marketData }: Props) {
  const scenarios = runScenarios(inputs, marketData);
  const basePiti = calculatePITI(inputs, marketData);
  const baseCashFlow = calculateCashFlow(inputs, basePiti);

  const rateScenarios = scenarios.filter(s => s.label.startsWith('Rate'));
  const priceScenarios = scenarios.filter(s => s.label.startsWith('Price'));

  const rateChartData = rateScenarios.map(s => ({
    label: s.label.replace('Rate ', ''),
    paymentChange: Math.round(s.paymentChange),
    cashFlowChange: Math.round(s.cashFlowChange),
  }));

  const priceChartData = priceScenarios.map(s => ({
    label: s.label.replace('Price ', ''),
    paymentChange: Math.round(s.paymentChange),
    equityAt5Years: Math.round(s.equityAt5Years),
  }));

  return (
    <div className="space-y-6">
      <Card className="border-border/50">
        <CardContent className="p-5">
          <h2 className="text-sm font-semibold text-muted-foreground uppercase tracking-wider mb-2">Baseline</h2>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div>
              <p className="text-muted-foreground">Monthly PITI</p>
              <p className="text-lg font-bold text-foreground">{formatCurrency(basePiti.total)}</p>
            </div>
            <div>
              <p className="text-muted-foreground">Net Cash Flow</p>
              <p className={cn("text-lg font-bold", baseCashFlow.netCashFlow >= 0 ? "text-[hsl(var(--success))]" : "text-destructive")}>
                {formatCurrency(baseCashFlow.netCashFlow)}
              </p>
            </div>
            <div>
              <p className="text-muted-foreground">Rate</p>
              <p className="text-lg font-bold text-foreground">{inputs.interestRate}%</p>
            </div>
            <div>
              <p className="text-muted-foreground">Price</p>
              <p className="text-lg font-bold text-foreground">{formatCurrency(inputs.purchasePrice)}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="border-border/50">
        <CardHeader>
          <CardTitle className="text-lg">Rate Shock Scenarios</CardTitle>
          <p className="text-xs text-muted-foreground">Impact on monthly payment if rates were higher at purchase</p>
        </CardHeader>
        <CardContent>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={rateChartData} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
                <XAxis dataKey="label" stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} />
                <YAxis stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} tickFormatter={(v: number) => `$${v}`} />
                <Tooltip
                  contentStyle={{ backgroundColor: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: '8px', color: 'hsl(var(--popover-foreground))' }}
                  formatter={(value: string | number | (string | number)[], name: string) => [
                    `${Number(value) >= 0 ? '+' : ''}${formatCurrency(Number(value))}/mo`,
                    name === 'paymentChange' ? 'Payment Impact' : 'Cash Flow Impact'
                  ]}
                />
                <ReferenceLine y={0} stroke={CHART_COLORS.axis} />
                <Bar dataKey="paymentChange" name="paymentChange" radius={[4, 4, 0, 0]}>
                  {rateChartData.map((entry, index) => (
                    <Cell key={index} fill={entry.paymentChange > 0 ? CHART_COLORS.negative : CHART_COLORS.positive} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>

      <Card className="border-border/50">
        <CardHeader>
          <CardTitle className="text-lg">Price Decline Scenarios</CardTitle>
          <p className="text-xs text-muted-foreground">Impact if you bought at a lower price (or values dropped after purchase)</p>
        </CardHeader>
        <CardContent>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={priceChartData} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
                <XAxis dataKey="label" stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} />
                <YAxis stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} tickFormatter={(v: number) => `$${(v / 1000).toFixed(0)}K`} />
                <Tooltip
                  contentStyle={{ backgroundColor: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: '8px', color: 'hsl(var(--popover-foreground))' }}
                  formatter={(value: string | number | (string | number)[], name: string) => [
                    formatCurrency(Number(value)),
                    name === 'equityAt5Years' ? 'Equity at 5 Years' : 'Payment Change'
                  ]}
                />
                <Bar dataKey="equityAt5Years" name="equityAt5Years" fill={CHART_COLORS.primary} radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>

      <Card className="border-border/50 overflow-hidden">
        <CardHeader>
          <CardTitle className="text-lg">Full Stress Test Matrix</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-muted-foreground">
                  <th className="text-left py-2 px-4">Scenario</th>
                  <th className="text-right py-2 px-4">Monthly Payment</th>
                  <th className="text-right py-2 px-4">Payment Change</th>
                  <th className="text-right py-2 px-4">Cash Flow</th>
                  <th className="text-right py-2 px-4">CF Change</th>
                  <th className="text-right py-2 px-4">Equity @ 5yr</th>
                </tr>
              </thead>
              <tbody>
                {scenarios.map((s) => (
                  <tr key={s.label} className="border-b border-border/50 hover:bg-muted/50">
                    <td className="py-2 px-4 text-foreground font-medium">{s.label}</td>
                    <td className="py-2 px-4 text-right text-foreground/80">{formatCurrency(s.monthlyPayment)}</td>
                    <td className={cn("py-2 px-4 text-right", s.paymentChange > 0 ? "text-destructive" : "text-[hsl(var(--success))]")}>
                      {s.paymentChange > 0 ? '+' : ''}{formatCurrency(s.paymentChange)}
                    </td>
                    <td className={cn("py-2 px-4 text-right", s.monthlyCashFlow >= 0 ? "text-[hsl(var(--success))]" : "text-destructive")}>
                      {formatCurrency(s.monthlyCashFlow)}
                    </td>
                    <td className={cn("py-2 px-4 text-right", s.cashFlowChange > 0 ? "text-[hsl(var(--success))]" : "text-destructive")}>
                      {s.cashFlowChange > 0 ? '+' : ''}{formatCurrency(s.cashFlowChange)}
                    </td>
                    <td className="py-2 px-4 text-right text-foreground/80">{formatCurrency(s.equityAt5Years)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
