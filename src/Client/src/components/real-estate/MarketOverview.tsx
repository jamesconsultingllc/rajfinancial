import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, ReferenceLine } from 'recharts';
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import MetricCard from './MetricCard';
import { rateHistory, currentRates, getRateTrend } from '@/lib/real-estate/rateData';
import type { MarketData } from '@/lib/real-estate/types';

const CHART_COLORS = {
  primary: 'hsl(48, 91%, 49%)',
  secondary: 'hsl(280, 60%, 55%)',
  grid: 'hsl(40, 15%, 25%)',
  axis: 'hsl(30, 8%, 45%)',
  threshold: 'hsl(0, 84%, 50%)',
};

interface Props {
  marketData: MarketData;
}

export default function MarketOverview({ marketData }: Props) {
  const trend = getRateTrend();

  const chartData = rateHistory.map(d => ({
    date: d.date,
    label: new Date(d.date + 'T00:00:00').toLocaleDateString('en-US', { month: 'short', year: '2-digit' }),
    '30yr': d.rate30yr,
    '15yr': d.rate15yr,
  }));

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard
          label="30-Year Fixed"
          value={`${currentRates.rate30yr}%`}
          sublabel={`As of ${currentRates.asOf}`}
          trend={trend.direction}
          color="gold"
        />
        <MetricCard
          label="15-Year Fixed"
          value={`${currentRates.rate15yr}%`}
          sublabel={`As of ${currentRates.asOf}`}
          color="purple"
        />
        <MetricCard
          label="Rate Trend"
          value={trend.direction === 'up' ? 'Rising' : trend.direction === 'down' ? 'Falling' : 'Stable'}
          sublabel={`${trend.change.toFixed(2)}% over ${trend.period}`}
          trend={trend.direction}
          color={trend.direction === 'up' ? 'red' : trend.direction === 'down' ? 'green' : 'amber'}
        />
        <MetricCard
          label="Region"
          value={marketData.region.split(' ')[0]}
          sublabel={`Tax: ${marketData.taxRate}% | Ins: ${marketData.insuranceRate}%`}
          color="amber"
        />
      </div>

      <Card className="border-border/50">
        <CardHeader>
          <CardTitle className="text-lg">Freddie Mac PMMS — Mortgage Rate History</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="h-80">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={chartData} margin={{ top: 5, right: 20, bottom: 5, left: 10 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={CHART_COLORS.grid} />
                <XAxis dataKey="label" stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} interval={8} />
                <YAxis stroke={CHART_COLORS.axis} tick={{ fontSize: 11 }} domain={['auto', 'auto']} tickFormatter={(v: number) => `${v}%`} />
                <Tooltip
                  contentStyle={{ backgroundColor: 'hsl(var(--popover))', border: '1px solid hsl(var(--border))', borderRadius: '8px', color: 'hsl(var(--popover-foreground))' }}
                  labelStyle={{ color: 'hsl(var(--muted-foreground))' }}
                  formatter={(value: string | number | (string | number)[]) => [`${Number(value).toFixed(2)}%`]}
                />
                <ReferenceLine y={6} stroke={CHART_COLORS.threshold} strokeDasharray="5 5" label={{ value: '6% threshold', fill: CHART_COLORS.threshold, fontSize: 11, position: 'right' }} />
                <Line type="monotone" dataKey="30yr" stroke={CHART_COLORS.primary} strokeWidth={2} dot={false} />
                <Line type="monotone" dataKey="15yr" stroke={CHART_COLORS.secondary} strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>
          <div className="flex gap-6 mt-4 text-sm">
            <div className="flex items-center gap-2">
              <div className="w-3 h-0.5 rounded" style={{ backgroundColor: CHART_COLORS.primary }} />
              <span className="text-muted-foreground">30-Year Fixed</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-3 h-0.5 rounded" style={{ backgroundColor: CHART_COLORS.secondary }} />
              <span className="text-muted-foreground">15-Year Fixed</span>
            </div>
            <div className="flex items-center gap-2">
              <div className="w-3 h-0.5 rounded border-dashed" style={{ backgroundColor: CHART_COLORS.threshold }} />
              <span className="text-muted-foreground">6% Threshold</span>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card className="border-border/50">
        <CardHeader>
          <CardTitle className="text-lg">Spring 2026 Market Context</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid md:grid-cols-2 gap-4 text-sm text-foreground">
            <div className="space-y-2">
              <p><span className="text-muted-foreground">Rate environment:</span> Rates briefly dipped below 6% in Sept 2025 before bouncing back to 6.46%. The "lock in before it drops more" window has closed for now.</p>
              <p><span className="text-muted-foreground">Buyer impact:</span> At 6.46%, a $400K loan costs ~$2,514/mo in P&I — $134/mo more than at 5.89% (Sept 2025 low).</p>
            </div>
            <div className="space-y-2">
              <p><span className="text-muted-foreground">Strategy implication:</span> Buy-and-refi-later is viable if you can stomach the current rate. House-hacking offsets the rate pain. Flips need tighter margins.</p>
              <p><span className="text-muted-foreground">Regional data:</span> Tax rate {marketData.taxRate}%, insurance {marketData.insuranceRate}%, est. appreciation {marketData.appreciationRate}%/yr.</p>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
