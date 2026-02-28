import logoHorizontal from "@/assets/logo-horizontal.svg";
import { cn } from "@/lib/utils";

interface LogoProps {
  className?: string;
  size?: "sm" | "md" | "lg";
}

export function Logo({ className, size = "md" }: LogoProps) {
  const sizes = {
    sm: "h-8",
    md: "h-10",
    lg: "h-14",
  };

  return (
    <img
      src={logoHorizontal}
      alt="RAJ Financial"
      className={cn(sizes[size], "w-auto", className)}
    />
  );
}
