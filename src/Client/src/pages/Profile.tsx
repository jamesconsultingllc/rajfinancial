import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Separator } from "@/components/ui/separator";
import { Helmet } from "react-helmet-async";
import { Camera, Mail, Phone, MapPin, Building, Calendar } from "lucide-react";

const mockUser = {
  name: "Rajesh Patel",
  email: "rajesh@example.com",
  phone: "+1 (555) 123-4567",
  address: "123 Financial Ave, New York, NY 10001",
  company: "Patel Enterprises",
  joinDate: "January 2024",
  avatar: "",
  initials: "RP",
};

export default function Profile() {
  return (
    <DashboardLayout>
      <Helmet>
        <title>Profile | RAJ Financial</title>
      </Helmet>

      <div className="max-w-3xl mx-auto space-y-6">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Profile</h1>
          <p className="text-muted-foreground text-sm mt-1">Manage your personal information</p>
        </div>

        {/* Avatar Section */}
        <Card className="bg-card border-border/50">
          <CardContent className="p-6">
            <div className="flex items-center gap-6">
              <div className="relative">
                <Avatar className="w-20 h-20">
                  <AvatarImage src={mockUser.avatar} />
                  <AvatarFallback className="bg-primary/20 text-primary text-xl font-semibold">
                    {mockUser.initials}
                  </AvatarFallback>
                </Avatar>
                <button className="absolute bottom-0 right-0 w-7 h-7 rounded-full bg-primary text-primary-foreground flex items-center justify-center hover:bg-primary/90 transition-colors">
                  <Camera className="w-3.5 h-3.5" />
                </button>
              </div>
              <div>
                <h2 className="text-lg font-semibold text-foreground">{mockUser.name}</h2>
                <p className="text-sm text-muted-foreground">{mockUser.email}</p>
                <div className="flex items-center gap-1 mt-1 text-xs text-muted-foreground">
                  <Calendar className="w-3 h-3" />
                  <span>Member since {mockUser.joinDate}</span>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>

        {/* Personal Information */}
        <Card className="bg-card border-border/50">
          <CardHeader>
            <CardTitle className="text-lg">Personal Information</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">First Name</Label>
                <Input id="firstName" defaultValue="Rajesh" />
              </div>
              <div className="space-y-2">
                <Label htmlFor="lastName">Last Name</Label>
                <Input id="lastName" defaultValue="Patel" />
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">
                <span className="flex items-center gap-1.5">
                  <Mail className="w-3.5 h-3.5" /> Email
                </span>
              </Label>
              <Input id="email" type="email" defaultValue={mockUser.email} />
            </div>

            <div className="space-y-2">
              <Label htmlFor="phone">
                <span className="flex items-center gap-1.5">
                  <Phone className="w-3.5 h-3.5" /> Phone
                </span>
              </Label>
              <Input id="phone" type="tel" defaultValue={mockUser.phone} />
            </div>

            <div className="space-y-2">
              <Label htmlFor="address">
                <span className="flex items-center gap-1.5">
                  <MapPin className="w-3.5 h-3.5" /> Address
                </span>
              </Label>
              <Input id="address" defaultValue={mockUser.address} />
            </div>

            <div className="space-y-2">
              <Label htmlFor="company">
                <span className="flex items-center gap-1.5">
                  <Building className="w-3.5 h-3.5" /> Company
                </span>
              </Label>
              <Input id="company" defaultValue={mockUser.company} />
            </div>

            <Separator />

            <div className="flex justify-end gap-3">
              <Button variant="outline">Cancel</Button>
              <Button variant="gold">Save Changes</Button>
            </div>
          </CardContent>
        </Card>

        {/* Security */}
        <Card className="bg-card border-border/50">
          <CardHeader>
            <CardTitle className="text-lg">Security</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-foreground">Password</p>
                <p className="text-xs text-muted-foreground">Last changed 30 days ago</p>
              </div>
              <Button variant="outline" size="sm">Change Password</Button>
            </div>
            <Separator />
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-foreground">Two-Factor Authentication</p>
                <p className="text-xs text-muted-foreground">Add an extra layer of security</p>
              </div>
              <Button variant="outline" size="sm">Enable</Button>
            </div>
            <Separator />
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-foreground">Connected Account</p>
                <p className="text-xs text-muted-foreground">Microsoft Entra ID</p>
              </div>
              <span className="text-xs font-medium text-[hsl(var(--success))]">Connected</span>
            </div>
          </CardContent>
        </Card>
      </div>
    </DashboardLayout>
  );
}
