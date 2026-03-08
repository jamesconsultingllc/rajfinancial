import { UserPlus, Link2, Eye } from "lucide-react";

const steps = [
  {
    icon: UserPlus,
    number: "1",
    title: "Create Your Account",
    description: "Sign up for free with your email or use your existing Microsoft, Google, or Apple account. No credit card required.",
  },
  {
    icon: Link2,
    number: "2",
    title: "Connect Your Accounts",
    description: "Securely link your bank accounts, investment accounts, and credit cards. We use bank-level encryption to keep your data safe.",
  },
  {
    icon: Eye,
    number: "3",
    title: "See Your Full Picture",
    description: "Get instant insights into your net worth, spending patterns, and opportunities to improve your financial health.",
  },
];

export function HowItWorksSection() {
  return (
    <section id="how-it-works" className="section-dark relative py-24">
      <div className="container relative mx-auto px-6">
        {/* Section Header */}
        <div className="max-w-3xl mx-auto text-center mb-16">
          <p className="text-sm font-semibold text-primary uppercase tracking-wider mb-4 opacity-0 animate-fade-in">
            Getting Started
          </p>
          <h2 className="text-3xl sm:text-4xl md:text-5xl font-extrabold mb-6 opacity-0 animate-fade-in" style={{ animationDelay: "100ms" }}>
            Get Started in 3 Simple Steps
          </h2>
          <p className="text-lg section-muted opacity-0 animate-fade-in" style={{ animationDelay: "200ms" }}>
            Start your journey to financial clarity today. It only takes a few minutes.
          </p>
        </div>

        {/* Steps */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 max-w-4xl mx-auto">
          {steps.map((step, index) => (
            <div
              key={step.number}
              className="relative opacity-0 animate-fade-in"
              style={{ animationDelay: `${300 + index * 100}ms` }}
            >
              {/* Step Card */}
              <div className="relative p-8 rounded-xl section-card bg-primary/5 border border-primary/20 text-center">
                {/* Number Badge */}
                <div className="mx-auto mb-6 w-12 h-12 rounded-full bg-primary/20 border border-primary/30 flex items-center justify-center">
                  <span className="text-lg font-bold text-primary">{step.number}</span>
                </div>
                
                {/* Content */}
                <h3 className="text-lg font-bold mb-3">{step.title}</h3>
                <p className="text-sm section-muted leading-relaxed">{step.description}</p>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
