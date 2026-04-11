import { Component, type ReactNode } from "react";
import { ApiError } from "@/types/api";
import { msalInstance } from "@/auth/AuthProvider";
import { apiRequest, loginRequest } from "@/auth/authConfig";
import { InteractionRequiredAuthError } from "@azure/msal-browser";
import { WifiOff, ShieldX, AlertTriangle, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";

interface ApiErrorBoundaryProps {
  /** Child components to render */
  children: ReactNode;
  /** Optional fallback component to render on error */
  fallback?: ReactNode;
}

interface ApiErrorBoundaryState {
  error: ApiError | Error | null;
  isRetrying: boolean;
}

/**
 * Error boundary that catches API errors and displays appropriate UI.
 *
 * @description Handles specific error scenarios:
 * - 401 Unauthorized: Attempts silent token refresh, falls back to interactive login
 * - 403 Forbidden: Shows Access Denied UI without redirect loop
 * - Network errors (status 0): Shows offline/retry banner
 * - Other errors: Shows generic error with retry option
 *
 * @example
 * ```tsx
 * <ApiErrorBoundary>
 *   <Dashboard />
 * </ApiErrorBoundary>
 * ```
 */
export class ApiErrorBoundary extends Component<
  ApiErrorBoundaryProps,
  ApiErrorBoundaryState
> {
  constructor(props: ApiErrorBoundaryProps) {
    super(props);
    this.state = { error: null, isRetrying: false };
  }

  static getDerivedStateFromError(error: Error): Partial<ApiErrorBoundaryState> {
    return { error };
  }

  componentDidCatch(error: Error): void {
    // Log for debugging
    console.error("[ApiErrorBoundary] Caught error:", error);

    // Handle 401 automatically
    if (error instanceof ApiError && error.status === 401) {
      this.handleUnauthorized();
    }
  }

  /**
   * Handles 401 Unauthorized errors.
   * Attempts silent token refresh, falls back to interactive login.
   */
  private async handleUnauthorized(): Promise<void> {
    const account = msalInstance.getActiveAccount();
    if (!account) {
      // No account — redirect to login
      await msalInstance.loginRedirect(loginRequest);
      return;
    }

    try {
      // Try silent token refresh
      await msalInstance.acquireTokenSilent({
        scopes: apiRequest.scopes,
        account,
      });
      // Success — clear error and re-render
      this.setState({ error: null });
    } catch (refreshError) {
      if (refreshError instanceof InteractionRequiredAuthError) {
        // Silent refresh failed — need interactive login
        await msalInstance.acquireTokenRedirect({
          scopes: apiRequest.scopes,
          account,
        });
      }
      // Other errors will show the error UI
    }
  }

  /**
   * Handles retry button click.
   * Clears error state to re-render children.
   */
  private handleRetry = (): void => {
    this.setState({ error: null, isRetrying: true });
    // Brief delay to show loading state
    setTimeout(() => {
      this.setState({ isRetrying: false });
    }, 100);
  };

  /**
   * Handles sign out for 403 errors.
   */
  private handleSignOut = async (): Promise<void> => {
    await msalInstance.logoutRedirect();
  };

  render(): ReactNode {
    const { error, isRetrying } = this.state;
    const { children, fallback } = this.props;

    if (isRetrying) {
      return children;
    }

    if (!error) {
      return children;
    }

    // Custom fallback provided
    if (fallback) {
      return fallback;
    }

    // Handle ApiError with specific status codes
    if (error instanceof ApiError) {
      switch (error.status) {
        case 401:
          // 401 is handled automatically in componentDidCatch
          // Show loading while redirect happens
          return <AuthenticatingFallback />;

        case 403:
          return (
            <ForbiddenError
              onGoBack={() => window.history.back()}
              onSignOut={this.handleSignOut}
            />
          );

        case 0:
          // Network error / API unreachable
          return (
            <NetworkError
              onRetry={this.handleRetry}
              message={error.message}
            />
          );

        default:
          return (
            <GenericError
              code={error.code}
              message={error.message}
              status={error.status}
              onRetry={this.handleRetry}
            />
          );
      }
    }

    // Generic Error
    return (
      <GenericError
        code="UNKNOWN_ERROR"
        message={error.message}
        onRetry={this.handleRetry}
      />
    );
  }
}

/**
 * Loading fallback shown during 401 handling.
 */
function AuthenticatingFallback() {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="text-center">
        <div className="w-8 h-8 border-2 border-primary border-t-transparent rounded-full animate-spin mx-auto mb-4" />
        <p className="text-muted-foreground text-sm">
          Refreshing authentication...
        </p>
      </div>
    </div>
  );
}

/**
 * 403 Forbidden error UI.
 */
function ForbiddenError({
  onGoBack,
  onSignOut,
}: {
  onGoBack: () => void;
  onSignOut: () => void;
}) {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="text-center max-w-md mx-auto p-6">
        <ShieldX className="w-16 h-16 text-destructive mx-auto mb-4" />
        <h2 className="text-xl font-bold text-foreground mb-2">
          Access Denied
        </h2>
        <p className="text-muted-foreground mb-6">
          You don't have permission to access this resource. If you believe this
          is an error, contact your administrator.
        </p>
        <div className="flex gap-3 justify-center">
          <Button variant="outline" onClick={onGoBack}>
            Go Back
          </Button>
          <Button variant="destructive" onClick={onSignOut}>
            Sign Out
          </Button>
        </div>
      </div>
    </div>
  );
}

/**
 * Network error / offline UI with retry option.
 */
function NetworkError({
  onRetry,
  message,
}: {
  onRetry: () => void;
  message: string;
}) {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="text-center max-w-md mx-auto p-6">
        <WifiOff className="w-16 h-16 text-warning mx-auto mb-4" />
        <h2 className="text-xl font-bold text-foreground mb-2">
          Connection Problem
        </h2>
        <p className="text-muted-foreground mb-6">
          {message || "Unable to reach the server. Please check your internet connection."}
        </p>
        <Button onClick={onRetry}>
          <RefreshCw className="w-4 h-4 mr-2" />
          Try Again
        </Button>
      </div>
    </div>
  );
}

/**
 * Generic error UI with retry option.
 */
function GenericError({
  code,
  message,
  status,
  onRetry,
}: {
  code: string;
  message: string;
  status?: number;
  onRetry: () => void;
}) {
  return (
    <div className="flex items-center justify-center min-h-[400px]">
      <div className="text-center max-w-md mx-auto p-6">
        <AlertTriangle className="w-16 h-16 text-destructive mx-auto mb-4" />
        <h2 className="text-xl font-bold text-foreground mb-2">
          Something Went Wrong
        </h2>
        <p className="text-muted-foreground mb-2">{message}</p>
        {status && (
          <p className="text-xs text-muted-foreground mb-6">
            Error {status}: {code}
          </p>
        )}
        <Button onClick={onRetry}>
          <RefreshCw className="w-4 h-4 mr-2" />
          Try Again
        </Button>
      </div>
    </div>
  );
}

export default ApiErrorBoundary;
