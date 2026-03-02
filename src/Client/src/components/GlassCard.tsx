import { cn } from "@/lib/utils";
import { ReactNode } from "react";

interface GlassCardProps {
  children: ReactNode;
  className?: string;
  hover?: boolean;
  glow?: boolean;
}

export function GlassCard({ children, className, hover = true, glow = false }: GlassCardProps) {
  return (
    <div
      className={cn(
        "rounded-xl bg-card/70 backdrop-blur-xl border border-border/30",
        hover && "transition-all duration-300 hover:border-primary/40 hover:shadow-gold-sm",
        glow && "shadow-gold-sm",
        className
      )}
    >
      {children}
    </div>
  );
}
