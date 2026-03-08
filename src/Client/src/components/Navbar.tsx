import { Link } from "react-router-dom";
import { Logo } from "./Logo";
import { Button } from "./ui/button";
import { Menu, X } from "lucide-react";
import { useState } from "react";
import { ThemeToggle } from "./ThemeToggle";
import { useAuth } from "@/auth/useAuth";

/**
 * Landing page navigation bar.
 *
 * @description Fixed-position navbar with glass morphism effect.
 * Shows Sign In / Get Started buttons for unauthenticated users.
 * Responsive with mobile hamburger menu.
 */
export function Navbar() {
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const { login } = useAuth();

  return (
    <nav className="fixed top-0 left-0 right-0 z-50 bg-card/90 backdrop-blur-xl border-b border-border/30">
      <div className="container mx-auto px-6">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link to="/" data-testid="nav-home-link" aria-label="Home">
            <Logo size="sm" />
          </Link>

          {/* Desktop CTA */}
          <div className="hidden md:flex items-center gap-2">
            <ThemeToggle />
            <Button variant="goldOutline" size="sm" onClick={() => login()}>
              Sign In
            </Button>
            <Button variant="gold" size="sm" onClick={() => login()}>
              Get Started
            </Button>
          </div>

          {/* Mobile Menu Toggle */}
          <div className="md:hidden flex items-center gap-2">
            <ThemeToggle />
            <Button variant="goldOutline" size="sm" onClick={() => login()}>
              Sign In
            </Button>
            <button
              className="text-foreground p-2"
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              aria-label={mobileMenuOpen ? "Close menu" : "Open menu"}
            >
              {mobileMenuOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
            </button>
          </div>
        </div>

        {/* Mobile Menu */}
        {mobileMenuOpen && (
          <div className="md:hidden py-6 border-t border-border/30">
            <div className="flex flex-col gap-3">
              <Button variant="goldOutline" className="w-full" onClick={() => login()}>
                Sign In
              </Button>
              <Button variant="gold" className="w-full" onClick={() => login()}>
                Get Started
              </Button>
            </div>
          </div>
        )}
      </div>
    </nav>
  );
}
