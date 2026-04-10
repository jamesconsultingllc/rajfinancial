import { TrendingUp, Clock, RefreshCw, Home, Hammer, Shield } from 'lucide-react';
import { Card, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import type { StrategyScore } from '@/lib/real-estate/types';

interface Props {
  strategies: StrategyScore[];
}

const icons: Record<string, React.ReactNode> = {
  buy: <TrendingUp size={20} />,
  wait: <Clock size={20} />,
  refinance: <RefreshCw size={20} />,
  houseHack: <Home size={20} />,
  flip: <Hammer size={20} />,
  hold: <Shield size={20} />,
};

const verdictStyles = {
  strong: {
    card: 'border-primary/40 bg-primary/5',
    text: 'text-primary',
    bar: 'bg-primary',
    badge: 'bg-primary/20 text-primary hover:bg-primary/20',
  },
  moderate: {
    card: 'border-amber-500/40 bg-amber-500/5',
    text: 'text-amber-500',
    bar: 'bg-amber-500',
    badge: 'bg-amber-500/20 text-amber-600 hover:bg-amber-500/20',
  },
  weak: {
    card: 'border-destructive/40 bg-destructive/5',
    text: 'text-destructive',
    bar: 'bg-destructive',
    badge: 'bg-destructive/20 text-destructive hover:bg-destructive/20',
  },
};

export default function StrategyCards({ strategies }: Props) {
  const top = strategies[0];

  return (
    <div className="space-y-6">
      {top && (
        <Card className={cn("border", verdictStyles[top.verdict].card)}>
          <CardContent className="p-5">
            <div className="flex items-center gap-3">
              <div className={verdictStyles[top.verdict].text}>{icons[top.strategy]}</div>
              <div>
                <p className="text-xs text-muted-foreground uppercase tracking-wider">Top Recommendation</p>
                <p className={cn("text-xl font-bold", verdictStyles[top.verdict].text)}>
                  {top.label} — Score: {top.score}/100
                </p>
                <p className="text-sm text-foreground/80 mt-1">
                  {top.keyMetric}: <span className="font-semibold text-foreground">{top.keyMetricValue}</span>
                </p>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-4">
        {strategies.map((s) => {
          const styles = verdictStyles[s.verdict];
          return (
            <Card key={s.strategy} className={cn("border transition-transform hover:scale-[1.02]", styles.card)}>
              <CardContent className="p-5">
                <div className="flex items-center justify-between mb-3">
                  <div className="flex items-center gap-2">
                    <span className={styles.text}>{icons[s.strategy]}</span>
                    <h3 className="text-sm font-bold text-foreground">{s.label}</h3>
                  </div>
                  <Badge variant="secondary" className={styles.badge}>
                    {s.score}/100
                  </Badge>
                </div>

                <div className="w-full bg-muted rounded-full h-1.5 mb-3">
                  <div
                    className={cn("h-1.5 rounded-full transition-all", styles.bar)}
                    style={{ width: `${s.score}%` }}
                  />
                </div>

                <div className="bg-muted/50 rounded-lg px-3 py-2 mb-3">
                  <p className="text-xs text-muted-foreground">{s.keyMetric}</p>
                  <p className="text-lg font-bold text-foreground">{s.keyMetricValue}</p>
                </div>

                <ul className="space-y-1">
                  {s.reasons.map((r, i) => (
                    <li key={i} className="text-xs text-muted-foreground flex items-start gap-1.5">
                      <span className={cn("mt-0.5", styles.text)}>&#8226;</span>
                      {r}
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </div>
  );
}
