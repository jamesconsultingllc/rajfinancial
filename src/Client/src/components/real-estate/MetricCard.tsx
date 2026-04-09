import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";

interface MetricCardProps {
  label: string;
  value: string;
  sublabel?: string;
  trend?: 'up' | 'down' | 'flat';
  color?: 'gold' | 'green' | 'red' | 'amber' | 'purple';
}

const colorMap = {
  gold: 'border-primary/30 bg-primary/5',
  green: 'border-[hsl(var(--success))]/30 bg-[hsl(var(--success))]/5',
  red: 'border-destructive/30 bg-destructive/5',
  amber: 'border-amber-500/30 bg-amber-500/5',
  purple: 'border-purple-500/30 bg-purple-500/5',
};

const trendIcons = { up: '↑', down: '↓', flat: '→' };

export default function MetricCard({ label, value, sublabel, trend, color = 'gold' }: MetricCardProps) {
  return (
    <Card className={cn("border", colorMap[color])}>
      <CardContent className="p-4">
        <p className="text-xs text-muted-foreground uppercase tracking-wider mb-1">{label}</p>
        <p className="text-2xl font-bold text-foreground flex items-center gap-2">
          {value}
          {trend && (
            <span className={cn("text-sm", {
              'text-destructive': trend === 'up',
              'text-[hsl(var(--success))]': trend === 'down',
              'text-muted-foreground': trend === 'flat',
            })}>
              {trendIcons[trend]}
            </span>
          )}
        </p>
        {sublabel && <p className="text-xs text-muted-foreground mt-1">{sublabel}</p>}
      </CardContent>
    </Card>
  );
}
