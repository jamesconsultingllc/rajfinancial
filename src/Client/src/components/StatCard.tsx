import { cn } from "@/lib/utils";

interface StatCardProps {
  label: string;
  value: string;
  trend?: string;
  trendUp?: boolean;
  className?: string;
  delay?: number;
}

export function StatCard({ label, value, trend, trendUp, className, delay = 0 }: StatCardProps) {
  return (
    <div
      className={cn(
        "relative p-6 rounded-xl bg-card/50 backdrop-blur-xl border border-border/30",
        "transition-all duration-300 hover:border-primary/40 hover:shadow-gold-sm",
        "opacity-0 animate-fade-in",
        className
      )}
      style={{ animationDelay: `${delay}ms` }}
    >
      <p className="text-sm text-muted-foreground mb-2">{label}</p>
      <p className="text-3xl font-bold text-gold-gradient">{value}</p>
      {trend && (
        <p className={cn(
          "text-sm mt-2 font-medium",
          trendUp ? "text-success" : "text-destructive"
        )}>
          {trendUp ? "↑" : "↓"} {trend}
        </p>
      )}
    </div>
  );
}
