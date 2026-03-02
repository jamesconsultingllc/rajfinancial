import { Lock, Eye, ShieldOff, Trash2 } from "lucide-react";

const securityFeatures = [
  {
    icon: Lock,
    title: "256-bit Encryption",
    description: "All data is encrypted in transit and at rest using industry-standard AES-256 encryption.",
  },
  {
    icon: Eye,
    title: "Read-Only Access",
    description: "We only have read access to your accounts. We can never move money or make transactions.",
  },
  {
    icon: ShieldOff,
    title: "No Data Selling",
    description: "We never sell your personal or financial data to third parties. Your data is yours.",
  },
  {
    icon: Trash2,
    title: "Delete Anytime",
    description: "You can disconnect accounts and delete your data completely at any time with one click.",
  },
];

export function SecuritySection() {
  return (
    <section className="section-light relative py-24">
      <div className="container relative mx-auto px-6">
        {/* Section Header */}
        <div className="max-w-3xl mx-auto text-center mb-16">
          <p className="text-sm font-semibold text-primary uppercase tracking-wider mb-4 opacity-0 animate-fade-in">
            Security First
          </p>
          <h2 className="text-3xl sm:text-4xl md:text-5xl font-extrabold mb-6 text-foreground opacity-0 animate-fade-in" style={{ animationDelay: "100ms" }}>
            Your Data is Safe With Us
          </h2>
          <p className="text-lg text-muted-foreground opacity-0 animate-fade-in" style={{ animationDelay: "200ms" }}>
            We take security seriously. Your financial data is protected with enterprise-grade security.
          </p>
        </div>

        {/* Security Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 max-w-5xl mx-auto">
          {securityFeatures.map((feature, index) => (
            <div
              key={feature.title}
              className="relative p-6 rounded-xl bg-card border border-border/50 text-center opacity-0 animate-fade-in"
              style={{ animationDelay: `${300 + index * 100}ms` }}
            >
              {/* Icon */}
              <div className="mx-auto mb-4 w-12 h-12 rounded-full bg-primary/10 border border-primary/20 flex items-center justify-center">
                <feature.icon className="w-5 h-5 text-primary" />
              </div>
              
              {/* Content */}
              <h3 className="text-sm font-bold text-foreground mb-2">{feature.title}</h3>
              <p className="text-xs text-muted-foreground leading-relaxed">{feature.description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
