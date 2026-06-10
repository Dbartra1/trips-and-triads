using System;
using System.Collections.Generic;
using System.Linq;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Tests.Helpers
{
    /// <summary>
    /// Simulates a complete campaign "run" from start to finish, including
    /// Phase 11 systems: Payroll, Debt, Obligation, and Collector escalation.
    /// 
    /// This allows us to tune game balance, test edge cases, and verify that
    /// the debt trap is insidious but escapable, and that Mutual Aid provides
    /// a viable alternative without being overpowered.
    /// </summary>
    public static class CampaignSimulator
    {
        public enum DeficitStrategy
        {
            TakeDebt,          // Always take Lacquer debt (high risk, high reward later)
            TakeMutualAid,     // Always take Della Obligation (safe, but locks you into free labor)
            ReleaseCheapest,   // Release the cheapest card to balance the books
            Hybrid             // Mutual Aid if available, else Debt, else Release
        }

        public enum RunEndReason
        {
            Active,            // Still running (should not be final state)
            BrokenUp,          // Lost to the Terminal Collector
            Prestige,          // Reached max turns / victory condition
            MaxTurns           // Hit the simulation turn limit without dying or winning
        }

        public class RunResult
        {
            public int TotalTurns { get; set; }
            public RunEndReason EndReason { get; set; }
            
            public int MaxDebtReached { get; set; }
            public int MaxObligationReached { get; set; }
            public int TotalCollectorsFought { get; set; }
            public int CollectorsWon { get; set; }
            public bool MiracleWinAgainstVespera { get; set; } // <1% chance to beat the terminal collector
            
            public int TimesUsedDebt { get; set; }
            public int TimesUsedMutualAid { get; set; }
            public int TimesReleasedCards { get; set; }
            
            public CredTier FinalCredTier { get; set; }
            public int FinalRosterSize { get; set; }
            
            // Track progression: Turn number when each Cred Tier was first reached
            public Dictionary<CredTier, int> TurnsToReachCred { get; set; } = new();
            
            public string Summary()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Turns: {TotalTurns,3} | End: {EndReason,-10} | " +
                              $"MaxDebt: {MaxDebtReached,4} | MaxObl: {MaxObligationReached,4} | " +
                              $"Collectors: {TotalCollectorsFought} ({CollectorsWon}W) | " +
                              $"Debt/Mutual/Release: {TimesUsedDebt}/{TimesUsedMutualAid}/{TimesReleasedCards} | " +
                              $"Final Cred: {FinalCredTier}, Roster: {FinalRosterSize}");
                if (MiracleWinAgainstVespera)
                    sb.AppendLine("  *** MIRACLE WIN AGAINST VESPERA, THE IRON LEDGER! DEBT WIPED! ***");
                
                sb.AppendLine("  Progression (Turns to reach):");
                foreach (var kvp in TurnsToReachCred.OrderBy(x => (int)x.Key))
                {
                    sb.AppendLine($"    {kvp.Key}: Turn {kvp.Value}");
                }
                return sb.ToString();
            }
        }

        public class BatchResult
        {
            public int TotalRuns { get; set; }
            public int BrokenUpCount { get; set; }
            public int PrestigeCount { get; set; }
            public int MaxTurnsCount { get; set; }
            public int MiracleWinCount { get; set; } // Track the <1% easter egg
            
            public double AvgTurns { get; set; }
            public double AvgMaxDebt { get; set; }
            public double AvgCollectorsFought { get; set; }
            public double CollectorWinRate { get; set; }
            
            public double AvgTimesUsedDebt { get; set; }
            public double AvgTimesUsedMutualAid { get; set; }
            public double AvgTimesReleasedCards { get; set; }
            
            // Average turns to reach each Cred Tier (only for runs that reached it)
            public Dictionary<CredTier, double> AvgTurnsToReachCred { get; set; } = new();

            public string Summary()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== Campaign Simulation Results ({TotalRuns} runs) ===");
                sb.AppendLine($"End Reasons:");
                sb.AppendLine($"  Broken Up (Collector) : {BrokenUpCount,4} ({(double)BrokenUpCount/TotalRuns:P1})");
                sb.AppendLine($"  Prestige / Victory    : {PrestigeCount,4} ({(double)PrestigeCount/TotalRuns:P1})");
                sb.AppendLine($"  Max Turns Reached     : {MaxTurnsCount,4} ({(double)MaxTurnsCount/TotalRuns:P1})");
                if (MiracleWinCount > 0)
                    sb.AppendLine($"  *** MIRACLE WINS vs VESPERA : {MiracleWinCount,4} ({(double)MiracleWinCount/TotalRuns:P3}) ***");
                sb.AppendLine($"Metrics:");
                sb.AppendLine($"  Avg Turns             : {AvgTurns:F1}");
                sb.AppendLine($"  Avg Max Debt          : {AvgMaxDebt:F1}");
                sb.AppendLine($"  Avg Collectors Fought : {AvgCollectorsFought:F1}");
                sb.AppendLine($"  Collector Win Rate    : {CollectorWinRate:P1}");
                sb.AppendLine($"  Avg Debt Taken        : {AvgTimesUsedDebt:F1}");
                sb.AppendLine($"  Avg Mutual Aid Used   : {AvgTimesUsedMutualAid:F1}");
                sb.AppendLine($"  Avg Cards Released    : {AvgTimesReleasedCards:F1}");
                
                sb.AppendLine("  Avg Progression (Turns to reach):");
                foreach (var kvp in AvgTurnsToReachCred.OrderBy(x => (int)x.Key))
                {
                    sb.AppendLine($"    {kvp.Key}: Turn {kvp.Value:F1}");
                }
                
                return sb.ToString();
            }
        }

        public static RunResult RunSingleCampaign(
            List<CardData> initialRoster,
            CredTier startingCred = CredTier.Nameless,
            int startingScrip = 15, // Increased to 15 to give room for early recruitment/losses
            DeficitStrategy deficitStrategy = DeficitStrategy.Hybrid,
            GameSimulator.Strategy matchStrategy = GameSimulator.Strategy.Greedy,
            int maxTurns = 50,
            Random? rng = null)
        {
            rng ??= new Random();
            
            var result = new RunResult
            {
                FinalCredTier = startingCred,
                FinalRosterSize = initialRoster.Count
            };
            
            // Record starting cred tier as reached at turn 0
            result.TurnsToReachCred[startingCred] = 0;

            int scrip = startingScrip;
            int debt = 0;
            int obligation = 0;
            int collectorHeat = 0;
            int deferrals = 0;
            var roster = new List<CardData>(initialRoster);
            int turn = 0;

            // Baseline expected income for Collector Heat calculation (e.g., standard contract)
            // Nameless=10, Known=15, Named=25, Notorious=40, Legend=50
            int GetExpectedIncome(CredTier tier) => tier switch
            {
                CredTier.Nameless => 10,
                CredTier.Known => 15,
                CredTier.Named => 25,
                CredTier.Notorious => 40,
                CredTier.Legend => 50,
                _ => 10
            };

            while (turn < maxTurns && result.EndReason == RunEndReason.Active)
            {
                turn++;

                // 1. Overworld Turn: Payroll
                int upkeep = roster.Sum(c => CredEffects.UpkeepCost(c.Tier, false)); // Simplified: no Hollowing in basic sim yet
                scrip -= upkeep;

                // 2. Handle Deficit
                if (scrip < 0)
                {
                    int deficit = -scrip;
                    bool handled = false;

                    if (deficitStrategy == DeficitStrategy.TakeMutualAid || 
                       (deficitStrategy == DeficitStrategy.Hybrid && rng.NextDouble() < 0.7)) // 70% chance to prefer Mutual Aid if Hybrid
                    {
                        obligation += deficit;
                        scrip = 0;
                        result.TimesUsedMutualAid++;
                        handled = true;
                    }
                    else if (deficitStrategy == DeficitStrategy.ReleaseCheapest || 
                            (deficitStrategy == DeficitStrategy.Hybrid && roster.Count > 5))
                    {
                        // Release cheapest card(s) until deficit is covered or roster is too small
                        while (scrip < 0 && roster.Count > 3)
                        {
                            var cheapest = roster.OrderBy(c => CredEffects.UpkeepCost(c.Tier)).First();
                            roster.Remove(cheapest);
                            scrip += CredEffects.UpkeepCost(cheapest.Tier); // "Refund" the upkeep
                            result.TimesReleasedCards++;
                        }
                        if (scrip < 0)
                        {
                            // Still in deficit, fall back to debt
                            debt += -scrip;
                            scrip = 0;
                            result.TimesUsedDebt++;
                        }
                        handled = true;
                    }

                    if (!handled || deficitStrategy == DeficitStrategy.TakeDebt)
                    {
                        debt += deficit;
                        scrip = 0;
                        result.TimesUsedDebt++;
                    }
                }

                // 3. Apply Debt Interest
                if (debt > 0)
                {
                    float interestRate = CredEffects.DebtInterestRate(result.FinalCredTier);
                    int interest = (int)System.Math.Ceiling(debt * interestRate);
                    debt += interest;
                }

                // 4. Apply Obligation Reduction (if working off Della's aid)
                // Simulate doing a Della contract to pay it off
                if (obligation > 0)
                {
                    int dellaPayout = 10; // Base Della payout
                    if (obligation >= dellaPayout)
                    {
                        obligation -= dellaPayout;
                    }
                    else
                    {
                        obligation = 0;
                    }
                }

                // 5. Collector Heat Calculation
                int expectedIncome = GetExpectedIncome(result.FinalCredTier);
                if (debt > 0)
                {
                    if (debt < expectedIncome) collectorHeat += 1;
                    else if (debt <= expectedIncome * 3) collectorHeat += 2;
                    else collectorHeat += 3;
                }

                // Track maxes
                result.MaxDebtReached = global::System.Math.Max(result.MaxDebtReached, debt);
                result.MaxObligationReached = global::System.Math.Max(result.MaxObligationReached, obligation);

                // 6. Collector Duel Check
                if (collectorHeat >= 6)
                {
                    collectorHeat = 0;
                    result.TotalCollectorsFought++;

                    bool isVespera = deferrals >= 3; // Terminal Collector: Vespera, the Iron Ledger
                    
                    double winChance;
                    if (isVespera)
                    {
                        // Vespera's crew has all 20s. It's nearly impossible.
                        // <1% chance of a "miracle win" through a perfect, lucky hand.
                        winChance = 0.005; // 0.5% chance
                    }
                    else
                    {
                        // Normal Collector: Base win chance 70%. -10% per deferral. +5% per roster card above 5.
                        winChance = 0.70 - (deferrals * 0.10) + ((roster.Count - 5) * 0.05);
                        winChance = global::System.Math.Max(0.10, global::System.Math.Min(0.90, winChance)); // Clamp between 10% and 90%
                    }

                    if (rng.NextDouble() < winChance)
                    {
                        // Player wins
                        result.CollectorsWon++;
                        deferrals++;
                        
                        if (isVespera)
                        {
                            // MIRACLE WIN: Debt is wiped clean, Obligation is forgiven, and you gain a legendary card
                            result.MiracleWinAgainstVespera = true;
                            debt = 0;
                            obligation = 0;
                            deferrals = 0; // Reset the ladder since debt is gone
                            // (In a real game, we'd add Vespera's card to the roster here)
                        }
                    }
                    else
                    {
                        // Player loses
                        if (isVespera)
                        {
                            // Lost to Vespera: Campaign over, broken up.
                            result.EndReason = RunEndReason.BrokenUp;
                            break;
                        }
                        else
                        {
                            // Lost to a normal collector: Barely survived, but deferral still counts
                            // (In a real game, this might mean taking a penalty, but for sim we just increment deferrals)
                            result.CollectorsWon++; // Count as a "survival" for metric purposes
                            deferrals++;
                        }
                    }
                }

                // 7. Simulate Match (Income Generation)
                // Obligation Debuff: If working off Della's aid, you cannot gain Cred, and payouts are reduced by 10%
                bool isObligated = obligation > 0;
                double credGainChance = isObligated ? 0.0 : 0.30; // Cannot level up while in Obligation
                float payoutMultiplier = isObligated ? 0.90f : 1.0f; // 10% reduction in income

                // Use a simplified win probability based on roster strength vs a baseline enemy
                // 60% base win rate, modified by roster size
                double matchWinChance = 0.60 + ((roster.Count - 5) * 0.02);
                matchWinChance = global::System.Math.Max(0.40, global::System.Math.Min(0.80, matchWinChance));

                if (rng.NextDouble() < matchWinChance)
                {
                    // Win: Gain scrip
                    int basePayout = 20;
                    float multiplier = CredEffects.IncomeMultiplier(result.FinalCredTier) * payoutMultiplier;
                    int payout = (int)(basePayout * multiplier);
                    scrip += payout;

                    // Occasional Cred gain (blocked if obligated)
                    if (rng.NextDouble() < credGainChance)
                    {
                        CredTier nextTier = (CredTier)global::System.Math.Min((int)CredTier.Legend, (int)result.FinalCredTier + 1);
                        if (nextTier > result.FinalCredTier)
                        {
                            result.FinalCredTier = nextTier;
                            // Record the turn this tier was reached if not already recorded
                            if (!result.TurnsToReachCred.ContainsKey(nextTier))
                            {
                                result.TurnsToReachCred[nextTier] = turn;
                            }
                        }
                    }
                }
                else
                {
                    // Loss: Small scrip penalty or card loss (simplified)
                    scrip = global::System.Math.Max(0, scrip - 5);
                    if (rng.NextDouble() < 0.1 && roster.Count > 3)
                    {
                        roster.RemoveAt(rng.Next(roster.Count));
                    }
                }

                result.FinalRosterSize = roster.Count;
            }

            if (result.EndReason == RunEndReason.Active)
            {
                result.EndReason = turn >= maxTurns ? RunEndReason.MaxTurns : RunEndReason.Prestige;
            }

            result.TotalTurns = turn;
            return result;
        }

        public static BatchResult RunBatch(
            int runs = 1000,
            DeficitStrategy deficitStrategy = DeficitStrategy.Hybrid,
            int seed = 42)
        {
            var rng = new Random(seed);
            var result = new BatchResult { TotalRuns = runs };

            double totalTurns = 0;
            double totalMaxDebt = 0;
            double totalCollectors = 0;
            double totalCollectorWins = 0;
            double totalDebtUses = 0;
            double totalMutualUses = 0;
            double totalReleases = 0;
            
            // For averaging progression
            var turnsToReachCredSums = new Dictionary<CredTier, double>();
            var turnsToReachCredCounts = new Dictionary<CredTier, int>();

            // Generate a baseline "balanced" starting roster: 1 Pro, 4 Street
            var initialRoster = new List<CardData>
            {
                new CardData { Tier = Tier.Pro, Name = "Pro 1" },
                new CardData { Tier = Tier.Street, Name = "Street 1" },
                new CardData { Tier = Tier.Street, Name = "Street 2" },
                new CardData { Tier = Tier.Street, Name = "Street 3" },
                new CardData { Tier = Tier.Street, Name = "Street 4" }
            };

            for (int i = 0; i < runs; i++)
            {
                var runResult = RunSingleCampaign(
                    initialRoster: initialRoster,
                    startingCred: CredTier.Nameless,
                    startingScrip: 15, // Updated default
                    deficitStrategy: deficitStrategy,
                    maxTurns: 50,
                    rng: rng);

                if (runResult.EndReason == RunEndReason.BrokenUp) result.BrokenUpCount++;
                else if (runResult.EndReason == RunEndReason.Prestige) result.PrestigeCount++;
                else if (runResult.EndReason == RunEndReason.MaxTurns) result.MaxTurnsCount++;
                
                if (runResult.MiracleWinAgainstVespera) result.MiracleWinCount++;

                totalTurns += runResult.TotalTurns;
                totalMaxDebt += runResult.MaxDebtReached;
                totalCollectors += runResult.TotalCollectorsFought;
                totalCollectorWins += runResult.CollectorsWon;
                totalDebtUses += runResult.TimesUsedDebt;
                totalMutualUses += runResult.TimesUsedMutualAid;
                totalReleases += runResult.TimesReleasedCards;
                
                // Aggregate progression data
                foreach (var kvp in runResult.TurnsToReachCred)
                {
                    if (!turnsToReachCredSums.ContainsKey(kvp.Key))
                    {
                        turnsToReachCredSums[kvp.Key] = 0;
                        turnsToReachCredCounts[kvp.Key] = 0;
                    }
                    turnsToReachCredSums[kvp.Key] += kvp.Value;
                    turnsToReachCredCounts[kvp.Key]++;
                }
            }

            result.AvgTurns = totalTurns / runs;
            result.AvgMaxDebt = totalMaxDebt / runs;
            result.AvgCollectorsFought = totalCollectors / runs;
            result.CollectorWinRate = totalCollectors > 0 ? totalCollectorWins / totalCollectors : 0;
            result.AvgTimesUsedDebt = totalDebtUses / runs;
            result.AvgTimesUsedMutualAid = totalMutualUses / runs;
            result.AvgTimesReleasedCards = totalReleases / runs;
            
            // Calculate averages for progression
            foreach (var kvp in turnsToReachCredSums)
            {
                result.AvgTurnsToReachCred[kvp.Key] = kvp.Value / turnsToReachCredCounts[kvp.Key];
            }

            return result;
        }
    }
}
