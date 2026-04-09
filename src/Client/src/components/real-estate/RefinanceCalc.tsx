import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import MetricCard from './MetricCard';
import type { PropertyInputs } from '@/lib/real-estate/types';
import { calculateRefinance, formatCurrency } from '@/lib/real-estate/calculations';

const CHART_COLORS = {
  primary: 'hsl(48, 91%, 49%)',
  grid: 'hsl(40, 15%, 25%)',
  axis: 'hsl(30, 8%, 45%)',
};

interface Props {
  inputs: PropertyInputs;
}

export default function RefinanceCalc({ inputs }: Props) {
  const refi = calculateRefinance(inputs);
  const currentRate = inputs.currentRate || inputs.interestRate;

  const chartData = refi.scenarios.map(s => ({
    rate: `${s.rate.toFixed(2)}%`,
    savings: Math.round(s.savings),
    npv: Math.round(s.npvSavings),
    breakeven: s.breakevenMonths,
  }));

  if (refi.scenarios.length === 0) {
    return (
      <Card className="border-border/50">
        <CardContent className="p-8 text-center">
          <p className="text-lg font-semibold text-foreground">Current rate is already very low</p>
          <p className="text-sm text-muted-foreground mt-2">
            At {currentRate.toFixed(2)}%, refinancing is unlikely to benefit you. Rates would need to drop significantly for savings to materialize.
          </p>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard label="Current Rate" value={`${currentRate.toFixed(2)}%`} color="red" />
        <MetricCard label="Trigger Rate" value={`${refi.triggerRate.toFixed(2)}%`} sublabel="Rate where refi makes sense" color="green" />
        <MetricCard
          label="Best Monthly Savings"
          value={formatCurrency(refi.scenarios[refi.scenarios.length - 1].savings)}
          sublabel={`At ${refi.scenarios[refi.scenarios.length - 1].rate.toFixed(2)}%`}
          color="gold"
        />
        <MetricCard
          label="Best Breakeven"
          value={`${refi.scenarios[0].breakevenMonths} mo`}
          sublabel="Months to recoup closing costs"
          color="amber"
        />
      </div>

      <Card className="border-border/50">
        <CardHeader>
          <CardTitle className="text-lg">Monthly Savings by New Rate</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="h-72">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={chartData} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
                <XAxis dataKey="rate" stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} />
                <YAxis stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} tickFormatter={(v: number) => `$${v}`} />
                <Tooltip
                  contentStyle={{ backgroundColor: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: '8px', color: 'hsl(var(--popover-foreground))' }}
                  formatter={(value: string | number | (string | number)[], name: string) => {
                    if (name === 'savings') return [formatCurrency(Number(value)), 'Monthly Savings'];
                    if (name === 'npv') return [formatCurrency(Number(value)), 'NPV of Savings'];
                    return [value, name];
                  }}
                />
                <ReferenceLine y={0} stroke={CHART_COLORS.axis} />
                <Bar dataKey="savings" fill={CHART_COLORS.primary} radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </CardContent>
      </Card>

      <Card className="border-border/50 overflow-hidden">
        <CardHeader>
          <CardTitle className="text-lg">Refinance Scenario Table</CardTitle>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border text-muted-foreground">
                  <th className="text-left py-2 px-4">New Rate</th>
                  <th className="text-right py-2 px-4">New Payment</th>
                  <th className="text-right py-2 px-4">Monthly Savings</th>
                  <th className="text-right py-2 px-4">Breakeven</th>
                  <th className="text-right py-2 px-4">NPV Savings</th>
                  <th className="text-center py-2 px-4">Verdict</th>
                </tr>
              </thead>
              <tbody>
                {refi.scenarios.map((s) => (
                  <tr key={s.rate} className="border-b border-border/50 hover:bg-muted/50">
                    <td className="py-2 px-4 text-foreground font-medium">{s.rate.toFixed(2)}%</td>
                    <td className="py-2 px-4 text-right text-foreground/80">{formatCurrency(s.newPayment)}</td>
                    <td className={`py-2 px-4 text-right font-medium ${s.savings > 0 ? 'text-[hsl(var(--success))]' : 'text-destructive'}`}>
                      {formatCurrency(s.savings)}
                    </td>
                    <td className="py-2 px-4 text-right text-foreground/80">
                      {s.breakevenMonths < 999 ? `${s.breakevenMonths} mo` : '—'}
                    </td>
                    <td className={`py-2 px-4 text-right font-medium ${s.npvSavings > 0 ? 'text-[hsl(var(--success))]' : 'text-destructive'}`}>
                      {formatCurrency(s.npvSavings)}
                    </td>
                    <td className="py-2 px-4 text-center">
                      {s.npvSavings > 10000 ? (
                        <Badge variant="secondary" className="bg-[hsl(var(--success))]/20 text-[hsl(var(--success))] hover:bg-[hsl(var(--success))]/20">Strong</Badge>
                      ) : s.npvSavings > 0 ? (
                        <Badge variant="secondary" className="bg-amber-500/20 text-amber-600 hover:bg-amber-500/20">Maybe</Badge>
                      ) : (
                        <Badge variant="secondary" className="bg-destructive/20 text-destructive hover:bg-destructive/20">No</Badge>
                      )}
                    </td>
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
