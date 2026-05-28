using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Simulation
{
    /// <summary>
    /// Monte Carlo win-rate simulations. These tests produce balance data rather
    /// than pass/fail assertions (they assert that results are in a sane range,
    /// not that a specific outcome occurs).
    ///
    /// Run with: dotnet test --verbosity normal
    /// The ITestOutputHelper writes the full summary to test output.
    ///
    /// As new cards and mechanics are added, extend the deck factory methods
    /// at the bottom of this file. The simulation infrastructure handles the rest.
    /// </summary>
    public class WinRateTests
    {
        private readonly ITestOutputHelper _output;
        private const int GAMES = 1000;

        public WinRateTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ── Helper: print and return ───────────────────────────────────────────
        private GameSimulator.BatchResult Run(
            System.Func<List<CardData>> p1,
            System.Func<List<CardData>> p2,
            MatchConfig? config = null,
            string? label = null)
        {
            var result = GameSimulator.RunBatch(p1, p2, GAMES,
                p1Strategy: GameSimulator.Strategy.Greedy,
                p2Strategy: GameSimulator.Strategy.Greedy,
                config: config);

            if (label != null) _output.WriteLine($"\n--- {label} ---");
            _output.WriteLine(result.Summary());
            return result;
        }

        // ══════════════════════════════════════════════════════════════════════
        // BASELINE — random vs random establishes the 50/50 control
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Baseline_RandomVsRandom_ShowsFirstMoverAdvantage()
        {
            var result = GameSimulator.RunBatch(
                BalancedDeck, BalancedDeck, GAMES,
                p1Strategy: GameSimulator.Strategy.Random,
                p2Strategy: GameSimulator.Strategy.Random);

            _output.WriteLine("--- Random vs Random (baseline) ---");
            _output.WriteLine(result.Summary());

            // P1 places the 9th (last) card in every game — a structural last-move
            // advantage. ~70% P1 win rate on random play is expected and correct.
            // This test documents the bias; it is not a balance problem.
            Assert.True(result.P1WinRate > 0.55,
                $"Expected P1 structural advantage >55% but got {result.P1WinRate:P1}");
        }

        // ══════════════════════════════════════════════════════════════════════
        // BALANCED DECK vs BALANCED DECK — greedy strategy
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void BalancedDeck_GreedyVsGreedy_IsRoughlyFair()
        {
            var result = Run(BalancedDeck, BalancedDeck, label: "Balanced vs Balanced (greedy)");
            Assert.InRange(result.P1WinRate, 0.35, 0.65);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HERO MATCHUPS — win rates for each hero deck vs a balanced opponent
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void YuneDeck_WinRate_vs_BalancedDeck()
        {
            var result = Run(YuneDeck, BalancedDeck, label: "Yune (Aegis) vs Balanced");
            _output.WriteLine($"Yune win rate: {result.P1WinRate:P1}");
            // Sanity check — hero deck should outperform pure junk but not be degenerate
            Assert.InRange(result.P1WinRate, 0.30, 0.85);
        }

        [Fact]
        public void GrinDeck_WinRate_vs_BalancedDeck()
        {
            var result = Run(GrinDeck, BalancedDeck, label: "Grin (Killzone) vs Balanced");
            _output.WriteLine($"Grin win rate: {result.P1WinRate:P1}");
            Assert.InRange(result.P1WinRate, 0.30, 0.85);
        }

        [Fact]
        public void RivenDeck_WinRate_vs_BalancedDeck()
        {
            var result = Run(RivenDeck, BalancedDeck, label: "Riven (Lateral Grid) vs Balanced");
            _output.WriteLine($"Riven win rate: {result.P1WinRate:P1}");
            Assert.InRange(result.P1WinRate, 0.30, 0.85);
        }

        [Fact]
        public void MaraDeck_WinRate_vs_BalancedDeck()
        {
            var result = Run(MaraDeck, BalancedDeck, label: "Mara (Sprawl) vs Balanced");
            _output.WriteLine($"Mara win rate: {result.P1WinRate:P1}");
            Assert.InRange(result.P1WinRate, 0.30, 0.85);
        }

        [Fact]
        public void SumiDeck_WinRate_vs_BalancedDeck()
        {
            var result = Run(SumiDeck, BalancedDeck, label: "Sumi (Compound) vs Balanced");
            _output.WriteLine($"Sumi win rate: {result.P1WinRate:P1}");
            Assert.InRange(result.P1WinRate, 0.30, 0.85);
        }

        [Fact]
        public void VesnaDeck_WinRate_vs_BalancedDeck()
        {
            var result = Run(VesnaDeck, BalancedDeck, label: "Vesna (Decay) vs Balanced");
            _output.WriteLine($"Vesna win rate: {result.P1WinRate:P1}");
            Assert.InRange(result.P1WinRate, 0.30, 0.85);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HERO VS HERO MATCHUPS — cross-faction face-offs
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void AllHeroMatchups_WinRateSummary()
        {
            var heroes = new (string name, System.Func<List<CardData>> deck)[]
            {
                ("Yune",  YuneDeck),
                ("Grin",  GrinDeck),
                ("Riven", RivenDeck),
                ("Mara",  MaraDeck),
                ("Sumi",  SumiDeck),
                ("Vesna", VesnaDeck),
            };

            _output.WriteLine("\n=== Hero vs Hero Win Rate Matrix ===");
            _output.WriteLine($"{"P1 \\ P2",-8}" + string.Concat(System.Array.ConvertAll(heroes, h => $"{h.name,8}")));

            foreach (var (p1name, p1factory) in heroes)
            {
                string row = $"{p1name,-8}";
                foreach (var (p2name, p2factory) in heroes)
                {
                    if (p1name == p2name)
                    {
                        row += $"{"--",8}";
                        continue;
                    }
                    var r = GameSimulator.RunBatch(p1factory, p2factory, GAMES,
                        p1Strategy: GameSimulator.Strategy.Greedy,
                        p2Strategy: GameSimulator.Strategy.Greedy);
                    row += $"{r.P1WinRate,8:P0}";
                }
                _output.WriteLine(row);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // DISTRICT PROTOCOLS — how much do protocols shift win rates?
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void DistrictProtocols_WinRateShiftSummary()
        {
            var districts = new (string name, MatchConfig config)[]
            {
                ("Base Rules",    MatchConfig.BaseRules()),
                ("Glass Spire",   MatchConfig.GlassSpire()),
                ("Dead Channel",  MatchConfig.DeadChannel()),
                ("Powder Room",   MatchConfig.PowderRoom()),
                ("The Hush",      MatchConfig.TheHush()),
            };

            _output.WriteLine("\n=== Protocol Impact on Balanced Deck Mirror Match ===");
            foreach (var (name, config) in districts)
            {
                var r = GameSimulator.RunBatch(BalancedDeck, BalancedDeck, GAMES,
                    config: config);
                _output.WriteLine($"{name,-16}: P1={r.P1WinRate:P1}  P2={r.P2WinRate:P1}  " +
                                  $"Draws={r.DrawRate:P1}  AvgCaptures={r.AvgCaptures:F1}");
            }

            // No assertions — this is a data collection test. Results go to output.
            Assert.True(true);
        }

        // ══════════════════════════════════════════════════════════════════════
        // DECK FACTORY METHODS
        // Add to these as more named cards are implemented.
        // Each returns List<CardData> (five cards — one hand).
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>Five even-stat Street/Pro cards. The fairness baseline.</summary>
        private static List<CardData> BalancedDeck() => new()
        {
            CardFactory.Street("B1", t:5, r:5, b:5, l:5).Data(),
            CardFactory.Street("B2", t:6, r:4, b:5, l:5).Data(),
            CardFactory.Street("B3", t:4, r:6, b:5, l:5).Data(),
            CardFactory.Street("B4", t:5, r:5, b:6, l:4).Data(),
            CardFactory.Street("B5", t:5, r:5, b:4, l:6).Data(),
        };

        private static List<CardData> YuneDeck() => new()
        {
            CardFactory.SeraphYune().Data,
            CardFactory.CassiaVane().Data,
            CardFactory.Street("A1", t:6, r:7, b:5, l:5).Data(),
            CardFactory.Street("A2", t:5, r:6, b:6, l:5).Data(),
            CardFactory.Street("A3", t:7, r:5, b:4, l:6).Data(),
        };

        private static List<CardData> GrinDeck() => new()
        {
            CardFactory.SisterGrin().Data,
            CardFactory.Street("G1", t:8, r:7, b:2, l:4).Data(),
            CardFactory.Street("G2", t:9, r:7, b:2, l:3).Data(),
            CardFactory.Street("G3", t:7, r:8, b:3, l:3).Data(),
            CardFactory.Street("G4", t:8, r:8, b:2, l:2).Data(),
        };

        private static List<CardData> RivenDeck() => new()
        {
            CardFactory.Riven().Data,
            CardFactory.Street("R1", t:4, r:8, b:3, l:7).Data(),
            CardFactory.Street("R2", t:3, r:9, b:3, l:7).Data(),
            CardFactory.Street("R3", t:4, r:7, b:4, l:8).Data(),
            CardFactory.Street("R4", t:3, r:8, b:4, l:7).Data(),
        };

        private static List<CardData> MaraDeck() => new()
        {
            CardFactory.MaraKane().Data,
            CardFactory.Street("M1", t:5, r:6, b:6, l:5).Data(),
            CardFactory.Street("M2", t:6, r:5, b:5, l:6).Data(),
            CardFactory.Street("M3", t:5, r:6, b:5, l:6).Data(),
            CardFactory.Street("M4", t:6, r:6, b:5, l:5).Data(),
        };

        private static List<CardData> SumiDeck() => new()
        {
            CardFactory.MadameSumi().Data,
            CardFactory.TheHeir().Data,
            CardFactory.Street("S1", t:6, r:5, b:7, l:4).Data(),
            CardFactory.Street("S2", t:5, r:6, b:6, l:5).Data(),
            CardFactory.Street("S3", t:7, r:5, b:5, l:5).Data(),
        };

        private static List<CardData> VesnaDeck() => new()
        {
            CardFactory.Vesna().Data,
            CardFactory.Street("V1", t:8, r:8, b:1, l:8).Data(),
            CardFactory.Street("V2", t:9, r:8, b:0, l:8).Data(),
            CardFactory.Street("V3", t:8, r:9, b:1, l:7).Data(),
            CardFactory.Street("V4", t:7, r:8, b:1, l:9).Data(),
        };
    }

    // Extension to expose CardInstance.Data as CardData for deck building
    internal static class CardInstanceDeckExtensions
    {
        public static CardData Data(this CardInstance ci) => ci.Data;
    }
}