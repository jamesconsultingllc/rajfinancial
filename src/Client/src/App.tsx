import { Toaster } from "@/components/ui/toaster";
import { Toaster as Sonner } from "@/components/ui/sonner";
import { TooltipProvider } from "@/components/ui/tooltip";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { HelmetProvider } from "react-helmet-async";
import { I18nextProvider } from "react-i18next";
import i18n from "@/lib/i18n";
import { ThemeProvider } from "@/hooks/use-theme";
import { AuthProvider } from "@/auth/AuthProvider";
import { ProtectedRoute } from "@/auth/ProtectedRoute";
import Index from "./pages/Index";
import Dashboard from "./pages/Dashboard";
import Profile from "./pages/Profile";
import Assets from "./pages/Assets";
import Contacts from "./pages/Contacts";
import NotFound from "./pages/NotFound";

const queryClient = new QueryClient();

/**
 * Root application component.
 *
 * @description Wraps the app with provider hierarchy:
 * 1. HelmetProvider — SEO/head management
 * 2. I18nextProvider — Internationalization context
 * 3. AuthProvider — MSAL authentication context
 * 4. ThemeProvider — Custom dark/light theme context
 * 5. QueryClientProvider — TanStack React Query for data fetching
 * 6. TooltipProvider — Radix tooltip context
 * 7. BrowserRouter — Client-side routing
 *
 * Public routes (landing page) are accessible without auth.
 * Dashboard routes require authentication via ProtectedRoute.
 */
const App = () => (
  <HelmetProvider>
    <I18nextProvider i18n={i18n}>
    <AuthProvider>
      <ThemeProvider>
        <QueryClientProvider client={queryClient}>
          <TooltipProvider>
            <Toaster />
            <Sonner />
            <BrowserRouter>
              <Routes>
                {/* Public routes */}
                <Route path="/" element={<Index />} />

                {/* Protected client routes */}
                <Route
                  path="/dashboard"
                  element={
                    <ProtectedRoute policy="RequireClient">
                      <Dashboard />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/assets"
                  element={
                    <ProtectedRoute policy="RequireClient">
                      <Assets />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/contacts"
                  element={
                    <ProtectedRoute policy="RequireClient">
                      <Contacts />
                    </ProtectedRoute>
                  }
                />
                <Route
                  path="/profile"
                  element={
                    <ProtectedRoute policy="RequireAuthenticated">
                      <Profile />
                    </ProtectedRoute>
                  }
                />

                {/* Catch-all */}
                <Route path="*" element={<NotFound />} />
              </Routes>
            </BrowserRouter>
          </TooltipProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </AuthProvider>
    </I18nextProvider>
  </HelmetProvider>
);

export default App;
