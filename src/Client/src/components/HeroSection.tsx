import { Button } from "./ui/button";
import { ArrowRight, TrendingUp, Shield, PieChart } from "lucide-react";
import logoIcon from "@/assets/logo-icon.png";

export function HeroSection() {
  return (
    <section className="section-dark relative min-h-screen flex items-center justify-center pt-20 overflow-hidden">
      {/* Background Effects */}
      <div className="absolute inset-0 hero-glow" />
      <div className="absolute inset-0 grid-pattern opacity-50" />
      
      {/* Floating Logo Animation */}
      <div className="absolute top-1/4 right-1/4 w-48 h-48 opacity-10 animate-float hidden lg:block">
        <img src={logoIcon} alt="" className="w-full h-full object-contain" />
      </div>

      <div className="container relative mx-auto px-6 py-20">
        <div className="max-w-4xl mx-auto text-center">
          {/* Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full bg-primary/10 border border-primary/30 mb-8 opacity-0 animate-fade-in">
            <div className="w-2 h-2 rounded-full bg-primary animate-pulse" />
            <span className="text-sm font-medium text-primary">Premium Financial Planning</span>
          </div>

          {/* Main Heading */}
          <h1 className="text-4xl sm:text-5xl md:text-6xl lg:text-7xl font-extrabold leading-tight mb-6 opacity-0 animate-fade-in" style={{ animationDelay: "100ms" }}>
            Take Control of Your
            <span className="block text-primary mt-2">Financial Future</span>
          </h1>

          {/* Subheading */}
          <p className="text-lg sm:text-xl section-muted max-w-2xl mx-auto mb-10 opacity-0 animate-fade-in" style={{ animationDelay: "200ms" }}>
            Track your net worth, manage assets & debts, and plan for complete financial security. 
            Built for professionals and families who demand excellence.
          </p>

          {/* CTA Buttons */}
          <div className="flex flex-col sm:flex-row items-center justify-center gap-4 mb-16 opacity-0 animate-fade-in" style={{ animationDelay: "300ms" }}>
            <Button variant="gold" size="xl">
              Get Started Free
              <ArrowRight className="w-5 h-5 ml-2" />
            </Button>
            <Button variant="goldOutline" size="xl">
              Explore Features
            </Button>
          </div>

          {/* Trust Indicators */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-8 max-w-3xl mx-auto opacity-0 animate-fade-in" style={{ animationDelay: "400ms" }}>
            <div className="flex items-center justify-center gap-3 section-muted">
              <Shield className="w-5 h-5 text-primary" />
              <span className="text-sm font-medium">Bank-Level Security</span>
            </div>
            <div className="flex items-center justify-center gap-3 section-muted">
              <PieChart className="w-5 h-5 text-primary" />
              <span className="text-sm font-medium">Free to Start</span>
            </div>
            <div className="flex items-center justify-center gap-3 section-muted">
              <TrendingUp className="w-5 h-5 text-primary" />
              <span className="text-sm font-medium">Setup in Minutes</span>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}
