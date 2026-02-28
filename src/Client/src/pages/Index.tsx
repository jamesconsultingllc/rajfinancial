import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { Navbar } from "@/components/Navbar";
import { HeroSection } from "@/components/HeroSection";
import { FeaturesSection } from "@/components/FeaturesSection";
import { HowItWorksSection } from "@/components/HowItWorksSection";
import { SecuritySection } from "@/components/SecuritySection";
import { CTASection } from "@/components/CTASection";
import { Footer } from "@/components/Footer";
import { Helmet } from "react-helmet-async";
import { useAuth } from "@/auth/useAuth";

/**
 * Landing page / home page for RAJ Financial.
 *
 * @description
 * - Authenticated users are redirected to their role-appropriate dashboard
 * - Unauthenticated visitors see the marketing landing page
 *
 * Role redirect logic matches the Blazor Index.razor behavior:
 * - Administrator/AdminAdvisor/AdminClient → /admin/dashboard
 * - Advisor → /advisor/clients
 * - Default (Client, Viewer, or any authenticated user) → /dashboard
 */
const Index = () => {
  const { isAuthenticated, isLoading, user, hasRole } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (isLoading || !isAuthenticated || !user) return;

    // Redirect based on role — matches Blazor's Index.razor logic
    if (
      hasRole("Administrator") ||
      hasRole("AdminAdvisor") ||
      hasRole("AdminClient")
    ) {
      navigate("/admin/dashboard", { replace: true });
    } else if (hasRole("Advisor")) {
      navigate("/advisor/clients", { replace: true });
    } else {
      // Default for Client, Viewer, or any authenticated user
      navigate("/dashboard", { replace: true });
    }
  }, [isAuthenticated, isLoading, user, hasRole, navigate]);

  // Show loading spinner while checking auth or redirecting
  if (isLoading || isAuthenticated) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="text-center">
          <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-muted-foreground text-sm">Loading...</p>
        </div>
      </div>
    );
  }

  return (
    <>
      <Helmet>
        <title>RAJ Financial | Premium Personal Finance & Net Worth Tracker</title>
        <meta
          name="description"
          content="Take control of your financial future with RAJ Financial. Track net worth, manage assets & debts, calculate insurance needs, and build wealth with premium financial planning tools."
        />
        <meta name="keywords" content="net worth tracker, personal finance, asset management, debt payoff, insurance calculator, financial planning" />
        <link rel="canonical" href="https://rajfinancial.com" />
      </Helmet>

      <div className="min-h-screen bg-background">
        <Navbar />
        <main>
          <HeroSection />
          <FeaturesSection />
          <HowItWorksSection />
          <SecuritySection />
          <CTASection />
        </main>
        <Footer />
      </div>
    </>
  );
};

export default Index;
