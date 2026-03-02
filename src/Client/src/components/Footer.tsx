import { Logo } from "./Logo";

export function Footer() {
  return (
    <footer className="section-dark relative py-8 border-t border-primary/20">
      <div className="container mx-auto px-6">
        <div className="flex flex-col sm:flex-row items-center justify-between gap-4">
          <Logo size="sm" />
          <p className="text-sm section-muted">
            © {new Date().getFullYear()} RAJ Financial Software. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}
