import { 
  LineChart, 
  Wallet, 
  CreditCard, 
  Shield, 
  Users, 
  Calculator 
} from "lucide-react";
import { LucideIcon } from "lucide-react";

interface FeatureCardProps {
  icon: LucideIcon;
  title: string;
  description: string;
  features: string[];
  delay?: number;
}

function FeatureCard({ icon: Icon, title, description, features, delay = 0 }: FeatureCardProps) {
  return (
    <div
      className="group relative p-8 rounded-xl bg-card border border-border/50 transition-all duration-500 hover:border-primary/50 hover:shadow-gold-sm opacity-0 animate-fade-in"
      style={{ animationDelay: `${delay}ms` }}
    >
      {/* Icon container */}
      <div className="mb-6 inline-flex items-center justify-center w-12 h-12 rounded-xl bg-primary/10 border border-primary/20">
        <Icon className="w-6 h-6 text-primary" />
      </div>
      
      {/* Content */}
      <h3 className="text-lg font-bold text-foreground mb-2">{title}</h3>
      <p className="text-muted-foreground text-sm leading-relaxed mb-4">{description}</p>
      
      {/* Feature list */}
      <ul className="space-y-2">
        {features.map((feature, idx) => (
          <li key={idx} className="flex items-center gap-2 text-sm text-muted-foreground">
            <span className="w-1.5 h-1.5 rounded-full bg-primary" />
            {feature}
          </li>
        ))}
      </ul>
    </div>
  );
}

const featuresData = [
  {
    icon: LineChart,
    title: "Net Worth Tracking",
    description: "Get a complete picture of your assets and liabilities. Track your net worth over time with beautiful charts and detailed breakdowns.",
    features: ["Real-time portfolio valuation", "Historical trends and charts", "Asset allocation analysis"],
  },
  {
    icon: Wallet,
    title: "Bank Account Linking",
    description: "Securely connect your bank accounts, credit cards, and investment accounts to automatically sync balances and transactions.",
    features: ["Plaid-powered secure connections", "Automatic balance updates", "Transaction categorization"],
  },
  {
    icon: Calculator,
    title: "Smart Analytics",
    description: "AI-powered insights help you understand your spending patterns and identify opportunities to grow your wealth faster.",
    features: ["Personalized recommendations", "Spending analysis", "Financial health score"],
  },
  {
    icon: CreditCard,
    title: "Debt Payoff Planner",
    description: "Compare debt payoff strategies like Avalanche and Snowball methods. See exactly when you will be debt-free and how much you will save.",
    features: ["Multiple payoff strategies", "Interest savings calculator", "Payment schedule generator"],
  },
  {
    icon: Shield,
    title: "Insurance Calculator",
    description: "Calculate your life insurance needs based on your income, dependents, debts, and future goals. Never be under or over-insured again.",
    features: ["Personalized coverage recommendations", "Gap analysis", "Detailed cost breakdown"],
  },
  {
    icon: Users,
    title: "Beneficiary Management",
    description: "Keep your beneficiary designations organized and up-to-date. Ensure your assets go where you want them to.",
    features: ["Centralized beneficiary tracking", "Asset assignment", "Document storage"],
  },
];

export function FeaturesSection() {
  return (
    <section id="features" className="section-light relative py-24 overflow-hidden">
      <div className="container relative mx-auto px-6">
        {/* Section Header */}
        <div className="max-w-3xl mx-auto text-center mb-16">
          <p className="text-sm font-semibold text-primary uppercase tracking-wider mb-4 opacity-0 animate-fade-in">
            Platform Features
          </p>
          <h2 className="text-3xl sm:text-4xl md:text-5xl font-extrabold mb-6 text-foreground opacity-0 animate-fade-in" style={{ animationDelay: "100ms" }}>
            Everything You Need to Manage Your
            <span className="text-primary"> Wealth</span>
          </h2>
          <p className="text-lg text-muted-foreground opacity-0 animate-fade-in" style={{ animationDelay: "200ms" }}>
            From tracking your net worth to planning for the future, RAJ Financial provides the tools and insights you need to make smarter financial decisions.
          </p>
        </div>

        {/* Features Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {featuresData.map((feature, index) => (
            <FeatureCard
              key={feature.title}
              icon={feature.icon}
              title={feature.title}
              description={feature.description}
              features={feature.features}
              delay={300 + index * 100}
            />
          ))}
        </div>
      </div>
    </section>
  );
}
