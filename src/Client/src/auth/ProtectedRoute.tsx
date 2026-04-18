import { type ReactNode, useEffect, useRef } from "react";
import {
  AuthenticatedTemplate,
  UnauthenticatedTemplate,
  useMsal,
} from "@azure/msal-react";
import { InteractionStatus } from "@azure/msal-browser";
import { useTranslation } from "react-i18next";
import { useAuth } from "./useAuth";
import { loginRequest } from "./authConfig";
import { REDIRECT_COUNT_KEY } from "./AuthProvider";
import { Shield, ShieldAlert } from "lucide-react";
import { Button } from "@/components/ui/button";

/** Maximum number of redirect attempts before showing manual sign-in */
const MAX_REDIRECTS = 2;

/**
 * Authorization policies matching the Blazor backend policies.
 *
 * @description
 * - RequireAuthenticated: Any authenticated user
 * - RequireClient: Client role or implicit client (no role)
 * - RequireAdministrator: Administrator, AdminAdvisor, or AdminClient roles
 */
export type AuthPolicy =
  | "RequireAuthenticated"
  | "RequireClient"
  | "RequireAdministrator";

interface ProtectedRouteProps {
  /** The authorization policy to enforce */
  policy?: AuthPolicy;
  /** Child components to render when authorized */
  children: ReactNode;
}

/**
 * Checks whether the user's roles satisfy the given policy.
 *
 * @param roles - Array of user roles from the ID token
 * @param policy - The authorization policy to check
 * @returns Whether the user is authorized
 */
export function satisfiesPolicy(roles: string[], policy: AuthPolicy): boolean {
  switch (policy) {
    case "RequireAuthenticated":
      return true;

    case "RequireClient":
      // Implicit client: no roles assigned = client
      return (
        roles.length === 0 ||
        roles.includes("Client") ||
        roles.includes("Administrator") ||
        roles.includes("AdminClient")
      );

    case "RequireAdministrator":
      return (
        roles.includes("Administrator") ||
        roles.includes("AdminAdvisor") ||
        roles.includes("AdminClient")
      );

    default:
      return false;
  }
}

/**
 * Route guard component that enforces authentication and authorization.
 *
 * @description
 * - Unauthenticated users are redirected to MSAL login
 * - Authenticated users with insufficient roles see an Access Denied page
 * - Authorized users see the child content
 *
 * @param policy - The authorization policy to enforce (default: RequireAuthenticated)
 * @param children - Content to render when authorized
 *
 * @example
 * ```tsx
 * <Route
 *   path="/dashboard"
 *   element={
 *     <ProtectedRoute policy="RequireClient">
 *       <Dashboard />
 *     </ProtectedRoute>
 *   }
 * />
 * ```
 */
export function ProtectedRoute({
  policy = "RequireAuthenticated",
  children,
}: ProtectedRouteProps) {
  const { inProgress } = useMsal();
  const { user, isAuthenticated } = useAuth();
  const { t } = useTranslation();

  // Show loading while MSAL is processing
  if (inProgress !== InteractionStatus.None) {
    return (
      <div className="flex items-center justify-center min-h-screen bg-background">
        <div className="text-center">
          <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
          <p className="text-muted-foreground text-sm">{t("auth.authenticating")}</p>
        </div>
      </div>
    );
  }

  return (
    <>
      <UnauthenticatedTemplate>
        <RedirectToLogin />
      </UnauthenticatedTemplate>

      <AuthenticatedTemplate>
        {isAuthenticated &&
        user &&
        satisfiesPolicy(user.roles, policy) ? (
          children
        ) : (
          <AccessDenied />
        )}
      </AuthenticatedTemplate>
    </>
  );
}

/**
 * Triggers MSAL redirect login when rendered.
 * Includes a loop breaker that shows a manual sign-in card after MAX_REDIRECTS attempts.
 */
function RedirectToLogin() {
  const { instance, inProgress } = useMsal();
  const { t } = useTranslation();
  const hasTriggeredRedirect = useRef(false);

  const redirectCount = parseInt(
    sessionStorage.getItem(REDIRECT_COUNT_KEY) ?? "0",
    10
  );

  // Move side effects to useEffect to avoid issues with re-renders
  useEffect(() => {
    // Loop breaker: after MAX_REDIRECTS attempts, don't redirect
    if (redirectCount >= MAX_REDIRECTS) {
      return;
    }

    // Only trigger redirect when no interaction is in progress and we haven't already triggered
    if (inProgress === InteractionStatus.None && !hasTriggeredRedirect.current) {
      hasTriggeredRedirect.current = true;
      sessionStorage.setItem(REDIRECT_COUNT_KEY, String(redirectCount + 1));
      instance.loginRedirect(loginRequest);
    }
  }, [inProgress, redirectCount, instance]);

  // Loop breaker: after MAX_REDIRECTS attempts, show manual sign-in
  if (redirectCount >= MAX_REDIRECTS) {
    return <SessionExpiredCard />;
  }

  return (
    <div className="flex items-center justify-center min-h-screen bg-background">
      <div className="text-center">
        <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <p className="text-muted-foreground text-sm">
          {t("auth.redirecting")}
        </p>
      </div>
    </div>
  );
}

/**
 * Session expired card shown when redirect loop is detected.
 * Allows user to manually initiate sign-in after resetting the counter.
 */
function SessionExpiredCard() {
  const { instance } = useMsal();
  const { t } = useTranslation();

  const handleSignIn = () => {
    sessionStorage.setItem(REDIRECT_COUNT_KEY, "0");
    instance.loginRedirect(loginRequest);
  };

  return (
    <div className="flex items-center justify-center min-h-screen bg-background">
      <div className="text-center max-w-md mx-auto p-6">
        <ShieldAlert className="w-16 h-16 text-warning mx-auto mb-4" />
        <h1 className="text-2xl font-bold text-foreground mb-2">
          {t("auth.sessionExpired")}
        </h1>
        <p className="text-muted-foreground mb-6">
          {t("auth.sessionExpiredDescription")}
        </p>
        <Button onClick={handleSignIn}>{t("auth.signIn")}</Button>
      </div>
    </div>
  );
}

/**
 * Access denied page for authenticated but unauthorized users.
 */
function AccessDenied() {
  const { logout } = useAuth();
  const { t } = useTranslation();

  return (
    <div className="flex items-center justify-center min-h-screen bg-background">
      <div className="text-center max-w-md mx-auto p-6">
        <Shield className="w-16 h-16 text-destructive mx-auto mb-4" />
        <h1 className="text-2xl font-bold text-foreground mb-2">
          {t("auth.accessDenied")}
        </h1>
        <p className="text-muted-foreground mb-6">
          {t("auth.accessDeniedDescription")}
        </p>
        <div className="flex gap-3 justify-center">
          <Button variant="outline" onClick={() => window.history.back()}>
            {t("auth.goBack")}
          </Button>
          <Button variant="destructive" onClick={() => logout()}>
            {t("auth.signOut")}
          </Button>
        </div>
      </div>
    </div>
  );
}
