import { Button } from "./ui/button";
import { ArrowRight } from "lucide-react";

export function CTASection() {
  return (
    <section className="section-dark relative py-24">
      <div className="container relative mx-auto px-6">
        <div className="max-w-2xl mx-auto text-center">
          <h2 className="text-3xl sm:text-4xl md:text-5xl font-extrabold mb-6 opacity-0 animate-fade-in">
            Ready to Take Control?
          </h2>
          
          <p className="text-lg section-muted max-w-xl mx-auto mb-10 opacity-0 animate-fade-in" style={{ animationDelay: "100ms" }}>
            Join users who have transformed their financial lives with RAJ Financial. Start your journey today - it is free.
          </p>

          {/* CTA */}
          <div className="opacity-0 animate-fade-in" style={{ animationDelay: "200ms" }}>
            <Button variant="gold" size="xl">
              Create Free Account
              <ArrowRight className="w-5 h-5 ml-2" />
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
}
