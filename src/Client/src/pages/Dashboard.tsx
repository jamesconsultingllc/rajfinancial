import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Helmet } from "react-helmet-async";
import {
  TrendingUp,
  TrendingDown,
  DollarSign,
  CreditCard,
  PiggyBank,
  ArrowUpRight,
  ArrowDownRight,
} from "lucide-react";
import { cn } from "@/lib/utils";

const netWorthData = {
  total: 487250,
  change: 12450,
  changePercent: 2.62,
  isPositive: true,
};

const summaryCards = [
  { label: "Total Assets", value: "$623,400", change: "+3.1%", isPositive: true, icon: PiggyBank },
  { label: "Total Liabilities", value: "$136,150", change: "-1.2%", isPositive: true, icon: CreditCard },
  { label: "Monthly Income", value: "$12,800", change: "+5.4%", isPositive: true, icon: DollarSign },
  { label: "Monthly Expenses", value: "$8,340", change: "+2.1%", isPositive: false, icon: TrendingDown },
];

const accounts = [
  { name: "Checking Account", institution: "Chase Bank", balance: 15420, type: "Cash" },
  { name: "Savings Account", institution: "Marcus by Goldman", balance: 45000, type: "Cash" },
  { name: "Brokerage", institution: "Fidelity", balance: 312000, type: "Investment" },
  { name: "401(k)", institution: "Vanguard", balance: 198500, type: "Retirement" },
  { name: "Roth IRA", institution: "Charles Schwab", balance: 52480, type: "Retirement" },
];

const recentActivity = [
  { description: "Salary Deposit", amount: 6400, date: "Feb 15", type: "income" },
  { description: "Mortgage Payment", amount: -2150, date: "Feb 14", type: "expense" },
  { description: "Investment Dividend", amount: 340, date: "Feb 12", type: "income" },
  { description: "Auto Insurance", amount: -185, date: "Feb 10", type: "expense" },
  { description: "Freelance Payment", amount: 1200, date: "Feb 8", type: "income" },
];

function formatCurrency(value: number) {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD", maximumFractionDigits: 0 }).format(value);
}

export default function Dashboard() {
  return (
    <DashboardLayout>
      <Helmet>
        <title>Dashboard | RAJ Financial</title>
      </Helmet>

      <div className="space-y-6">
        {/* Welcome & Net Worth */}
        <div>
          <h1 className="text-2xl font-bold text-foreground">Welcome back, Rajesh</h1>
          <p className="text-muted-foreground text-sm mt-1">Here's your financial overview</p>
        </div>

        {/* Net Worth Hero */}
        <Card className="bg-card border-border/50">
          <CardContent className="p-6">
            <p className="text-sm text-muted-foreground mb-1">Net Worth</p>
            <div className="flex items-end gap-3">
              <h2 className="text-4xl font-bold text-foreground">{formatCurrency(netWorthData.total)}</h2>
              <div className={cn("flex items-center gap-1 text-sm font-medium pb-1", netWorthData.isPositive ? "text-[hsl(var(--success))]" : "text-destructive")}>
                {netWorthData.isPositive ? <ArrowUpRight className="w-4 h-4" /> : <ArrowDownRight className="w-4 h-4" />}
                {formatCurrency(netWorthData.change)} ({netWorthData.changePercent}%)
              </div>
            </div>
            <p className="text-xs text-muted-foreground mt-2">Updated today at 9:00 AM</p>
          </CardContent>
        </Card>

        {/* Summary Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {summaryCards.map((card) => (
            <Card key={card.label} className="bg-card border-border/50">
              <CardContent className="p-4">
                <div className="flex items-center justify-between mb-3">
                  <span className="text-sm text-muted-foreground">{card.label}</span>
                  <card.icon className="w-4 h-4 text-primary" />
                </div>
                <p className="text-xl font-bold text-foreground">{card.value}</p>
                <p className={cn("text-xs font-medium mt-1", card.isPositive ? "text-[hsl(var(--success))]" : "text-destructive")}>
                  {card.change} this month
                </p>
              </CardContent>
            </Card>
          ))}
        </div>

        {/* Accounts & Activity */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Accounts */}
          <Card className="lg:col-span-2 bg-card border-border/50">
            <CardHeader>
              <CardTitle className="text-lg">Accounts</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y divide-border/50">
                {accounts.map((account) => (
                  <div key={account.name} className="flex items-center justify-between px-6 py-3 hover:bg-secondary/50 transition-colors">
                    <div>
                      <p className="text-sm font-medium text-foreground">{account.name}</p>
                      <p className="text-xs text-muted-foreground">{account.institution}</p>
                    </div>
                    <div className="text-right">
                      <p className="text-sm font-semibold text-foreground">{formatCurrency(account.balance)}</p>
                      <p className="text-xs text-muted-foreground">{account.type}</p>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>

          {/* Recent Activity */}
          <Card className="bg-card border-border/50">
            <CardHeader>
              <CardTitle className="text-lg">Recent Activity</CardTitle>
            </CardHeader>
            <CardContent className="p-0">
              <div className="divide-y divide-border/50">
                {recentActivity.map((item, i) => (
                  <div key={i} className="flex items-center justify-between px-6 py-3">
                    <div>
                      <p className="text-sm font-medium text-foreground">{item.description}</p>
                      <p className="text-xs text-muted-foreground">{item.date}</p>
                    </div>
                    <p className={cn("text-sm font-semibold", item.amount > 0 ? "text-[hsl(var(--success))]" : "text-foreground")}>
                      {item.amount > 0 ? "+" : ""}{formatCurrency(Math.abs(item.amount))}
                    </p>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </DashboardLayout>
  );
}
