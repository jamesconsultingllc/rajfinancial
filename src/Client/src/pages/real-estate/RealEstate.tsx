import { useState, useMemo } from "react";
import { DashboardLayout } from "@/components/dashboard/DashboardLayout";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Helmet } from "react-helmet-async";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { MapPin } from "lucide-react";
import MarketOverview from "@/components/real-estate/MarketOverview";
import PropertyForm from "@/components/real-estate/PropertyForm";
import PropertyAnalysis from "@/components/real-estate/PropertyAnalysis";
import StrategyCards from "@/components/real-estate/StrategyCards";
import RefinanceCalc from "@/components/real-estate/RefinanceCalc";
import ScenarioModeler from "@/components/real-estate/ScenarioModeler";
import { getMarketData, getStateFromZip } from "@/lib/real-estate/marketData";
import { scoreStrategies } from "@/lib/real-estate/calculations";
import { currentRates } from "@/lib/real-estate/rateData";
import type { PropertyInputs, TabId } from "@/lib/real-estate/types";

const defaultInputs: PropertyInputs = {
  purchasePrice: 400000,
  downPaymentPct: 20,
  interestRate: currentRates.rate30yr,
  loanTermYears: 30,
  zipCode: "75201",
  state: "TX",
  monthlyRent: 2200,
  vacancyRate: 7,
  propertyMgmtPct: 10,
  maintenancePct: 1,
  capexPct: 1,
  hoaMonthly: 0,
  appreciationRate: 3.8,
  monthlyPersonalRent: 1800,
  renovationBudget: 40000,
  arvMultiplier: 1.3,
  numRentalUnits: 1,
  ownerUnitRent: 1200,
  currentLoanBalance: 320000,
  currentRate: currentRates.rate30yr,
  currentRemainingMonths: 360,
  refiClosingCostPct: 2,
};

export default function RealEstate() {
  const [activeTab, setActiveTab] = useState<TabId>("market");
  const [zipCode, setZipCode] = useState("75201");
  const [inputs, setInputs] = useState<PropertyInputs>(defaultInputs);

  const marketData = useMemo(() => getMarketData(zipCode), [zipCode]);

  const handleZipChange = (zip: string) => {
    setZipCode(zip);
    if (zip.length === 5) {
      const md = getMarketData(zip);
      const state = getStateFromZip(zip);
      setInputs((prev) => ({
        ...prev,
        zipCode: zip,
        state,
        appreciationRate: md.appreciationRate,
      }));
    }
  };

  const strategies = useMemo(
    () => scoreStrategies(inputs, marketData),
    [inputs, marketData]
  );

  return (
    <DashboardLayout>
      <Helmet>
        <title>Real Estate Analysis | RAJ Financial</title>
      </Helmet>

      <div className="space-y-6">
        {/* Header */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-foreground">
              Real Estate Analysis
            </h1>
            <p className="text-muted-foreground text-sm mt-1">
              Evaluate properties, compare strategies, and model scenarios
            </p>
          </div>

          {/* ZIP Code Input */}
          <div className="flex items-center gap-2">
            <MapPin className="w-4 h-4 text-primary" />
            <Label htmlFor="zip-input" className="text-sm font-medium sr-only">
              ZIP Code
            </Label>
            <Input
              id="zip-input"
              type="text"
              value={zipCode}
              onChange={(e) => handleZipChange(e.target.value)}
              placeholder="ZIP Code"
              maxLength={5}
              className="w-24 h-9 text-sm"
            />
            <span className="text-xs text-muted-foreground">
              {marketData.region}
            </span>
          </div>
        </div>

        {/* Tab Navigation */}
        <Tabs
          value={activeTab}
          onValueChange={(v) => setActiveTab(v as TabId)}
        >
          <TabsList className="grid w-full grid-cols-5">
            <TabsTrigger value="market">Market</TabsTrigger>
            <TabsTrigger value="analyzer">Analyzer</TabsTrigger>
            <TabsTrigger value="strategies">Strategies</TabsTrigger>
            <TabsTrigger value="refinance">Refinance</TabsTrigger>
            <TabsTrigger value="scenarios">Scenarios</TabsTrigger>
          </TabsList>

          <TabsContent value="market">
            <MarketOverview marketData={marketData} />
          </TabsContent>

          <TabsContent value="analyzer">
            <div className="space-y-6">
              <PropertyForm inputs={inputs} onChange={setInputs} />
              <PropertyAnalysis inputs={inputs} marketData={marketData} />
            </div>
          </TabsContent>

          <TabsContent value="strategies">
            <div className="space-y-6">
              <PropertyForm inputs={inputs} onChange={setInputs} />
              <StrategyCards strategies={strategies} />
            </div>
          </TabsContent>

          <TabsContent value="refinance">
            <div className="space-y-6">
              <PropertyForm inputs={inputs} onChange={setInputs} />
              <RefinanceCalc inputs={inputs} />
            </div>
          </TabsContent>

          <TabsContent value="scenarios">
            <div className="space-y-6">
              <PropertyForm inputs={inputs} onChange={setInputs} />
              <ScenarioModeler inputs={inputs} marketData={marketData} />
            </div>
          </TabsContent>
        </Tabs>
      </div>
    </DashboardLayout>
  );
}
