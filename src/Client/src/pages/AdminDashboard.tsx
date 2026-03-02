import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Helmet } from "react-helmet-async";
import {
  Users,
  Settings,
  FileText,
  BarChart3,
  Activity,
  Shield,
  UserPlus,
  AlertTriangle,
} from "lucide-react";
import { useNavigate } from "react-router-dom";

const statsCards = [
  { label: "Total Users", value: "1,247", icon: Users },
  { label: "Active Sessions", value: "89", icon: Activity },
  { label: "Pending Approvals", value: "12", icon: AlertTriangle },
  { label: "System Health", value: "99.9%", icon: Shield },
];

const recentActivity = [
  { description: "New user registered: john.doe@example.com", time: "2 minutes ago" },
  { description: "System backup completed successfully", time: "15 minutes ago" },
  { description: "User role updated: advisor@rajfinancial.com → Administrator", time: "1 hour ago" },
  { description: "Security scan completed — no issues found", time: "3 hours ago" },
  { description: "Monthly report generated", time: "5 hours ago" },
];

export default function AdminDashboard() {
  const navigate = useNavigate();

  return (
    <DashboardLayout>
      <Helmet>
        <title>Administrator Dashboard | RAJ Financial</title>
      </Helmet>

      <div data-testid="admin-dashboard" className="space-y-6">
        <div>
          <h1 data-testid="admin-dashboard-title" className="text-2xl font-bold text-foreground">
            Administrator Dashboard
          </h1>
          <p className="text-muted-foreground text-sm mt-1">System overview and management tools</p>
        </div>

        {/* Statistics Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {statsCards.map((card) => (
            <Card key={card.label} className="card bg-card border-border/50">
              <CardContent className="p-4">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-sm text-muted-foreground">{card.label}</span>
                  <card.icon className="w-4 h-4 text-primary" aria-hidden="true" />
                </div>
                <h3 className="text-xl font-bold text-foreground">{card.value}</h3>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Quick Actions & Recent Activity */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* Quick Actions */}
          <Card data-testid="quick-actions" className="bg-card border-border/50">
            <CardHeader>
              <CardTitle className="text-lg">Quick Actions</CardTitle>
            </CardHeader>
            <CardContent className="grid grid-cols-2 gap-3">
              <Button variant="outline" className="h-auto py-3 flex flex-col gap-1" onClick={() => navigate("/admin/users")}>
                <Users className="w-5 h-5" aria-hidden="true" />
                <span>Manage Users</span>
              </Button>
              <Button variant="outline" className="h-auto py-3 flex flex-col gap-1" onClick={() => navigate("/admin/settings")}>
                <Settings className="w-5 h-5" aria-hidden="true" />
                <span>System Settings</span>
              </Button>
              <Button variant="outline" className="h-auto py-3 flex flex-col gap-1" onClick={() => navigate("/admin/audit")}>
                <FileText className="w-5 h-5" aria-hidden="true" />
                <span>Audit Logs</span>
              </Button>
              <Button variant="outline" className="h-auto py-3 flex flex-col gap-1" onClick={() => navigate("/admin/reports")}>
                <BarChart3 className="w-5 h-5" aria-hidden="true" />
                <span>System Reports</span>
              </Button>
            </CardContent>
          </Card>

          {/* Recent Activity */}
          <Card data-testid="recent-activity" className="bg-card border-border/50">
            <CardHeader>
              <CardTitle className="text-lg">Recent Activity</CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-3">
                {recentActivity.map((item, idx) => (
                  <li key={idx} className="flex items-start gap-3 text-sm">
                    <Activity className="w-4 h-4 text-primary mt-0.5 shrink-0" aria-hidden="true" />
                    <div>
                      <p className="text-foreground">{item.description}</p>
                      <p className="text-muted-foreground text-xs">{item.time}</p>
                    </div>
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>
        </div>
      </div>
    </DashboardLayout>
  );
}
