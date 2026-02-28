import { cn } from "@/lib/utils";
import { LucideIcon } from "lucide-react";

interface FeatureCardProps {
  icon: LucideIcon;
  title: string;
  description: string;
  className?: string;
  delay?: number;
}

export function FeatureCard({ icon: Icon, title, description, className, delay = 0 }: FeatureCardProps) {
  return (
    <div
      className={cn(
        "group relative p-8 rounded-xl bg-card/50 backdrop-blur-xl border border-border/30",
        "transition-all duration-500 hover:border-primary/50 hover:shadow-gold-sm",
        "opacity-0 animate-fade-in",
        className
      )}
      style={{ animationDelay: `${delay}ms` }}
    >
      {/* Gold gradient border on hover */}
      <div className="absolute inset-0 rounded-xl bg-gold-gradient opacity-0 group-hover:opacity-10 transition-opacity duration-500" />
      
      {/* Icon container */}
      <div className="relative mb-6 inline-flex items-center justify-center w-14 h-14 rounded-xl bg-primary/10 border border-primary/20 group-hover:shadow-gold-sm transition-all duration-300">
        <Icon className="w-7 h-7 text-primary" />
      </div>
      
      {/* Content */}
      <h3 className="relative text-xl font-bold text-foreground mb-3 group-hover:text-gold-gradient transition-colors">
        {title}
      </h3>
      <p className="relative text-muted-foreground leading-relaxed">
        {description}
      </p>
    </div>
  );
}
