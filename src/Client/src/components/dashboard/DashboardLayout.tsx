import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { Logo } from "@/components/Logo";
import { ThemeToggle } from "@/components/ThemeToggle";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/auth/useAuth";
import { useAuthProfile } from "@/hooks/use-auth-profile";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Home,
  LayoutDashboard,
  Wallet,
  TrendingUp,
  Shield,
  FileText,
  Users,
  Settings,
  LogOut,
  User,
  Bell,
  Search,
  Menu,
  X,
  ChevronLeft,
  Package,
  UserCog,
  ClipboardList,
  BarChart3,
  AlertCircle,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface NavItem {
  icon: typeof Home;
  label: string;
  path: string;
}

interface NavSection {
  title: string;
  dataTestId: string;
  items: NavItem[];
}

const clientNavSections: NavSection[] = [
  {
    title: "",
    dataTestId: "nav-main",
    items: [
      { icon: Home, label: "Home", path: "/" },
      { icon: LayoutDashboard, label: "Dashboard", path: "/dashboard" },
    ],
  },
  {
    title: "My Account",
    dataTestId: "my-account",
    items: [
      { icon: TrendingUp, label: "My Portfolio", path: "/portfolio" },
      { icon: Package, label: "Assets", path: "/assets" },
      { icon: Wallet, label: "Accounts", path: "/dashboard/accounts" },
      { icon: Shield, label: "Insurance", path: "/dashboard/insurance" },
      { icon: FileText, label: "Documents", path: "/dashboard/documents" },
      { icon: Users, label: "Beneficiaries", path: "/contacts" },
    ],
  },
];

const adminNavSections: NavSection[] = [
  {
    title: "",
    dataTestId: "nav-main",
    items: [
      { icon: Home, label: "Home", path: "/" },
    ],
  },
  {
    title: "Administration",
    dataTestId: "administration",
    items: [
      { icon: LayoutDashboard, label: "Dashboard", path: "/admin/dashboard" },
      { icon: UserCog, label: "User Management", path: "/admin/users" },
      { icon: ClipboardList, label: "Audit Logs", path: "/admin/audit" },
      { icon: BarChart3, label: "System Reports", path: "/admin/reports" },
    ],
  },
  {
    title: "My Account",
    dataTestId: "my-account",
    items: [
      { icon: TrendingUp, label: "My Portfolio", path: "/portfolio" },
      { icon: Package, label: "Assets", path: "/assets" },
    ],
  },
];

/**
 * Dashboard layout with sidebar navigation, top bar, and user menu.
 *
 * @description Uses MSAL auth for user info and sign out.
 * Integrates with useAuthProfile to display API-sourced profile data
 * (admin badge, profile completion status).
 * Responsive sidebar collapses on mobile with overlay.
 */
export function DashboardLayout({ children }: { children: React.ReactNode }) {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout, isAdmin } = useAuth();
  const { profile, isLoading: isProfileLoading } = useAuthProfile();

  // Use auth state (from Entra claims) for admin badge; profile API for completion status
  const showAdminBadge = isAdmin;
  const showProfileIncompleteBanner = !isProfileLoading && profile != null && profile.displayName === "";

  const displayUser = {
    name: user?.name ?? "User",
    email: user?.email ?? "",
    initials: user?.initials ?? "U",
  };

  const navSections = isAdmin ? adminNavSections : clientNavSections;

  return (
    <div className="min-h-screen bg-background flex">
      {/* Mobile overlay */}
      {mobileOpen && (
        <div
          className="fixed inset-0 z-40 bg-background/80 backdrop-blur-sm md:hidden"
          onClick={() => setMobileOpen(false)}
        />
      )}

      {/* Sidebar */}
      <aside
        data-testid="sidebar"
        className={cn(
          "fixed inset-y-0 left-0 z-50 flex flex-col bg-sidebar-background border-r border-sidebar-border transition-all duration-300",
          sidebarCollapsed ? "w-16" : "w-64",
          mobileOpen ? "translate-x-0" : "-translate-x-full md:translate-x-0"
        )}
      >
        {/* Sidebar header */}
        <div className={cn("flex items-center h-16 px-4 border-b border-sidebar-border", sidebarCollapsed && "justify-center")}>
          {sidebarCollapsed ? (
            <div className="w-8 h-8 rounded-lg bg-primary flex items-center justify-center">
              <span className="text-primary-foreground font-bold text-sm">R</span>
            </div>
          ) : (
            <Logo size="sm" />
          )}
        </div>

        {/* Nav items */}
        <nav className="flex-1 py-4 px-2 space-y-4 overflow-y-auto">
          {navSections.map((section) => (
            <div key={section.dataTestId} data-testid={section.dataTestId}>
              {section.title && (
                <p className={cn(
                  "text-xs font-semibold uppercase tracking-wider text-sidebar-foreground/50 mb-2",
                  sidebarCollapsed ? "text-center" : "px-3"
                )}>
                  {!sidebarCollapsed && section.title}
                </p>
              )}
              <div className="space-y-1">
                {section.items.map((item) => {
                  const isActive = location.pathname === item.path;
                  return (
                    <Link
                      key={item.path}
                      to={item.path}
                      onClick={() => setMobileOpen(false)}
                      className={cn(
                        "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors",
                        isActive
                          ? "bg-primary/10 text-primary"
                          : "text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground",
                        sidebarCollapsed && "justify-center px-2"
                      )}
                      title={sidebarCollapsed ? item.label : undefined}
                    >
                      <item.icon className="w-5 h-5 shrink-0" aria-hidden="true" />
                      {!sidebarCollapsed && <span>{item.label}</span>}
                    </Link>
                  );
                })}
              </div>
            </div>
          ))}
        </nav>

        {/* Sidebar footer */}
        <div className={cn("p-2 border-t border-sidebar-border space-y-1", sidebarCollapsed && "flex flex-col items-center")}>
          <Link
            to="/settings"
            className={cn(
              "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors",
              location.pathname.startsWith("/settings")
                ? "bg-primary/10 text-primary"
                : "text-sidebar-foreground hover:bg-sidebar-accent",
              sidebarCollapsed && "justify-center px-2"
            )}
          >
            <Settings className="w-5 h-5 shrink-0" aria-hidden="true" />
            {!sidebarCollapsed && <span>Settings</span>}
          </Link>
          <button
            onClick={() => logout()}
            className={cn(
              "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-destructive hover:bg-sidebar-accent transition-colors w-full",
              sidebarCollapsed && "justify-center px-2"
            )}
          >
            <LogOut className="w-5 h-5 shrink-0" aria-hidden="true" />
            {!sidebarCollapsed && <span>Log out</span>}
          </button>
        </div>

        {/* Collapse button - desktop only */}
        <button
          onClick={() => setSidebarCollapsed(!sidebarCollapsed)}
          aria-label={sidebarCollapsed ? "Expand sidebar" : "Collapse sidebar"}
          className="hidden md:flex absolute -right-3 top-20 w-6 h-6 rounded-full bg-card border border-border items-center justify-center text-muted-foreground hover:text-foreground transition-colors"
        >
          <ChevronLeft className={cn("w-3.5 h-3.5 transition-transform", sidebarCollapsed && "rotate-180")} aria-hidden="true" />
        </button>
      </aside>

      {/* Main content area */}
      <div className={cn("flex-1 flex flex-col transition-all duration-300", sidebarCollapsed ? "md:ml-16" : "md:ml-64")}>
        {/* Top bar */}
        <header className="sticky top-0 z-30 h-16 bg-card/90 backdrop-blur-xl border-b border-border/30 flex items-center px-4 md:px-6 gap-4">
          {/* Mobile menu button */}
          <button
            data-testid="mobile-menu-toggle"
            onClick={() => setMobileOpen(!mobileOpen)}
            className="md:hidden text-foreground p-1.5"
            aria-label={mobileOpen ? "Close menu" : "Open menu"}
          >
            {mobileOpen ? <X className="w-5 h-5" aria-hidden="true" /> : <Menu className="w-5 h-5" aria-hidden="true" />}
          </button>

          {/* Search */}
          <div className="flex-1 max-w-md">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" aria-hidden="true" />
              <input
                type="text"
                placeholder="Search accounts, transactions..."
                className="w-full h-9 pl-9 pr-4 rounded-lg bg-secondary border-none text-sm text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
          </div>

          {/* Right side */}
          <div className="flex items-center gap-2">
            <ThemeToggle />

            {/* Notifications */}
            <Button variant="ghost" size="icon" className="relative" aria-label="Notifications">
              <Bell className="w-5 h-5" aria-hidden="true" />
              <span className="absolute top-1.5 right-1.5 w-2 h-2 rounded-full bg-destructive" />
            </Button>

            {/* User menu */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button className="flex items-center gap-2 p-1.5 rounded-lg hover:bg-secondary transition-colors">
                  <Avatar className="w-8 h-8">
                    <AvatarImage />
                    <AvatarFallback className="bg-primary/20 text-primary text-xs font-semibold">
                      {displayUser.initials}
                    </AvatarFallback>
                  </Avatar>
                  {!sidebarCollapsed && (
                    <span className="hidden lg:block text-sm font-medium text-foreground">
                      {displayUser.name}
                    </span>
                  )}
                </button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-56">
                <DropdownMenuLabel>
                  <div className="flex flex-col">
                    <div className="flex items-center gap-2">
                      <span>{displayUser.name}</span>
                      {showAdminBadge && (
                        <Badge
                          data-testid="admin-badge"
                          variant="secondary"
                          className="text-xs px-1.5 py-0 bg-primary/10 text-primary"
                        >
                          <Shield className="w-3 h-3 mr-1" aria-hidden="true" />
                          Admin
                        </Badge>
                      )}
                    </div>
                    <span className="text-xs font-normal text-muted-foreground">{displayUser.email}</span>
                  </div>
                </DropdownMenuLabel>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => navigate("/settings/profile")}>
                  <User className="w-4 h-4 mr-2" aria-hidden="true" />
                  Profile
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => navigate("/settings")}>
                  <Settings className="w-4 h-4 mr-2" aria-hidden="true" />
                  Settings
                </DropdownMenuItem>
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => logout()} className="text-destructive">
                  <LogOut className="w-4 h-4 mr-2" aria-hidden="true" />
                  Log out
                </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 p-4 md:p-6 overflow-auto">
          {showProfileIncompleteBanner && (
            <Alert
              data-testid="profile-incomplete-banner"
              className="mb-4 border-primary/30 bg-primary/5"
            >
              <AlertCircle className="h-4 w-4 text-primary" />
              <AlertDescription className="flex items-center justify-between">
                <span>Complete your profile to unlock all features.</span>
                <Button
                  variant="goldOutline"
                  size="sm"
                  onClick={() => navigate("/settings/profile")}
                >
                  Complete Profile
                </Button>
              </AlertDescription>
            </Alert>
          )}
          {children}
        </main>
      </div>
    </div>
  );
}
