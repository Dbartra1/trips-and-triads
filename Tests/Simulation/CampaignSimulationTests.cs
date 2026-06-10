using System;
using Xunit;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Simulation
{
    public class CampaignSimulationTests
    {
        [Fact]
        public void RunBatch_HybridStrategy_ShouldShowBalancedMetrics()
        {
            // Arrange & Act
            var result = CampaignSimulator.RunBatch(
                runs: 1000,
                deficitStrategy: CampaignSimulator.DeficitStrategy.Hybrid,
                seed: 42);

            // Assert & Output
            Console.WriteLine("\n" + result.Summary());

            // Basic sanity checks
            Assert.Equal(1000, result.TotalRuns);
            Assert.True(result.AvgTurns > 10); // Should survive at least a few turns
            Assert.True(result.BrokenUpCount > 0); // Debt should kill some crews
            Assert.True(result.PrestigeCount + result.MaxTurnsCount > 0); // Some should survive
        }

        [Fact]
        public void RunBatch_TakeDebtStrategy_ShouldHaveHigherCollectorDeaths()
        {
            var hybridResult = CampaignSimulator.RunBatch(
                runs: 500,
                deficitStrategy: CampaignSimulator.DeficitStrategy.Hybrid,
                seed: 42);

            var debtResult = CampaignSimulator.RunBatch(
                runs: 500,
                deficitStrategy: CampaignSimulator.DeficitStrategy.TakeDebt,
                seed: 42);

            Console.WriteLine("\n--- Hybrid Strategy ---");
            Console.WriteLine(hybridResult.Summary());
            Console.WriteLine("\n--- Take Debt Strategy ---");
            Console.WriteLine(debtResult.Summary());

            // Taking pure debt should lead to more Collector deaths than Hybrid
            // (because Hybrid uses Mutual Aid or releases cards to mitigate)
            Assert.True(debtResult.BrokenUpCount > hybridResult.BrokenUpCount, 
                "TakeDebt strategy should result in more Broken Up runs than Hybrid");
            
            Assert.True(debtResult.AvgMaxDebt > hybridResult.AvgMaxDebt,
                "TakeDebt strategy should accumulate more max debt");
        }

        [Fact]
        public void RunBatch_TakeMutualAidStrategy_ShouldAvoidDebtButLimitGrowth()
        {
            var result = CampaignSimulator.RunBatch(
                runs: 500,
                deficitStrategy: CampaignSimulator.DeficitStrategy.TakeMutualAid,
                seed: 42);

            Console.WriteLine("\n--- Take Mutual Aid Strategy ---");
            Console.WriteLine(result.Summary());

            // Mutual Aid should result in very low debt, but also potentially lower survival 
            // if they can't generate enough surplus to grow, or high Obligation usage.
            Assert.True(result.AvgMaxDebt < 20, "Mutual Aid should keep debt very low");
            Assert.True(result.AvgTimesUsedMutualAid > 0, "Should use Mutual Aid when in deficit");
        }

        [Fact]
        public void RunBatch_ReleaseCheapestStrategy_ShouldShrinkRoster()
        {
            var result = CampaignSimulator.RunBatch(
                runs: 500,
                deficitStrategy: CampaignSimulator.DeficitStrategy.ReleaseCheapest,
                seed: 42);

            Console.WriteLine("\n--- Release Cheapest Strategy ---");
            Console.WriteLine(result.Summary());

            // This strategy should actively shrink the roster to survive
            Assert.True(result.AvgTimesReleasedCards > 0, "Should release cards to avoid debt");
        }
    }
}
