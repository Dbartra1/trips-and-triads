using Xunit;
using Xunit.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Simulation
{
    /// <summary>
    /// Parametric balance tests that measure how changing stat bands and
    /// the maximum stat value affects match outcomes.
    ///
    /// Two families of tests:
    ///
    ///   1. Band Tuning — keep max=10, vary Street/Pro total bands.
    ///      Answers: what bands give generated crews a 40–65% win rate?
    ///
    ///   2. Scale Comparison — fix band ratios, scale the max stat (10/20/30).
    ///      Answers: does a wider stat range change balance, or is it purely
    ///      proportional? Does variance between cards increase meaningfully?
    ///
    /// All tests use the same AI hand model: Vesna (scaled) + Verity (scaled)
    /// + 3 generated Streets, so comparisons are apples-to-apples.
    /// </summary>
    public class StatScaleTests
    {
        private readonly ITestOutputHelper _output;
        private const int Games   = 2000; // per configuration
        private const int Crews   = 100;  // distinct player crews
        private const int GamesPerCrew = Games / Crews;
        private const int Seed    = 42;

        public StatScaleTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a player hand using custom stat band parameters.
        /// Mirrors CrewGenerator.Generate + SelectBestFive but with
        /// injected band values so we can test different configurations.
        /// </summary>
        private static List<CardData> GenerateScaledHand(
            Random rng,
            int streetMin, int streetMax, int streetEdgeMin, int streetEdgeMax,
            int proMin,    int proMax,    int proEdgeMin,    int proEdgeMax,
            int heroA,     int heroSoftMin, int heroSoftMax,
            int heroMidMin, int heroMidMax)
        {
            var usedNames = new HashSet<string>();

            // Hero: one A edge, one soft, two mid
            var heroEdges = new int[4];
            heroEdges[0] = heroA;
            heroEdges[1] = rng.Next(heroSoftMin, heroSoftMax + 1);
            heroEdges[2] = rng.Next(heroMidMin,  heroMidMax  + 1);
            heroEdges[3] = rng.Next(heroMidMin,  heroMidMax  + 1);
            Shuffle(heroEdges, rng);

            var hero = new CardData
            {
                Name        = "Hero",
                Top         = heroEdges[0], Right  = heroEdges[1],
                Bottom      = heroEdges[2], Left   = heroEdges[3],
                Tier        = Tier.Hero,    Level  = heroA,
                Faction     = Faction.None,
            };

            // Pro
            int proTotal  = rng.Next(proMin, proMax + 1);
            var proEdges  = DistributeStats(proTotal, proEdgeMin, proEdgeMax, rng);
            var pro = new CardData
            {
                Name    = "Pro",
                Top     = proEdges[0], Right  = proEdges[1],
                Bottom  = proEdges[2], Left   = proEdges[3],
                Tier    = Tier.Pro,    Level  = 6,
                Faction = Faction.None,
            };

            // 3 Streets (SelectBestFive will pick the top 3)
            var streets = new List<CardData>();
            for (int i = 0; i < 5; i++)
            {
                int total = rng.Next(streetMin, streetMax + 1);
                var e     = DistributeStats(total, streetEdgeMin, streetEdgeMax, rng);
                streets.Add(new CardData
                {
                    Name    = $"Street{i}",
                    Top     = e[0], Right  = e[1],
                    Bottom  = e[2], Left   = e[3],
                    Tier    = Tier.Street, Level = 3,
                    Faction = Faction.None,
                });
            }

            // SelectBestFive: hero + top 4 non-hero by total
            var sorted = streets.Concat(new[] { pro })
                .OrderByDescending(c => c.Top + c.Right + c.Bottom + c.Left)
                .Take(4).ToList();
            sorted.Insert(0, hero);
            return sorted;
        }

        /// <summary>
        /// Builds a scaled AI hand: Vesna at scaledA/scaledA/scaledA/scaledA
        /// + Verity scaled proportionally + 3 scaled Streets.
        /// </summary>
        private static List<CardData> BuildScaledAIHand(
            Random rng, int scale,
            int streetMin, int streetMax, int streetEdgeMin, int streetEdgeMax)
        {
            var usedNames = new HashSet<string>();
            var hand      = new List<CardData>();

            // Vesna: all edges = scale (full A equivalent)
            hand.Add(new CardData
            {
                Name        = "Vesna",
                Top         = scale, Right  = scale,
                Bottom      = scale, Left   = scale,
                Tier        = Tier.Hero, Level = scale,
                AbilityType = AbilityType.Decay, Faction = Faction.HollowChoir,
            });

            // Verity: 7/9/7/9 proportionally scaled
            int vHigh = (int)System.Math.Round(scale * 0.9);
            int vMid  = (int)System.Math.Round(scale * 0.7);
            hand.Add(new CardData
            {
                Name    = "Verity",
                Top     = vMid,  Right  = vHigh,
                Bottom  = vMid,  Left   = vHigh,
                Tier    = Tier.TopTier, Level = (int)System.Math.Round(scale * 0.8),
                Faction = Faction.Effigy,
            });

            // 3 Streets
            for (int i = 0; i < 3; i++)
            {
                int total = rng.Next(streetMin, streetMax + 1);
                var e     = DistributeStats(total, streetEdgeMin, streetEdgeMax, rng);
                hand.Add(new CardData
                {
                    Name    = $"AIStreet{i}",
                    Top     = e[0], Right  = e[1],
                    Bottom  = e[2], Left   = e[3],
                    Tier    = Tier.Street, Level = 3,
                    Faction = Faction.None,
                });
            }
            return hand;
        }

        private double RunBalance(
            Func<Random, List<CardData>> playerFactory,
            Func<Random, List<CardData>> aiFactory,
            Random rng)
        {
            int wins = 0, total = 0;
            for (int c = 0; c < Crews; c++)
            {
                var playerHand = playerFactory(rng);
                for (int g = 0; g < GamesPerCrew; g++)
                {
                    TestLogger.Clear();
                    var result = GameSimulator.RunGame(
                        playerHand, aiFactory(rng),
                        GameSimulator.Strategy.Greedy,
                        GameSimulator.Strategy.Greedy,
                        new MatchConfig(), rng);
                    if (result.Winner == 1) wins++;
                    total++;
                }
            }
            return (double)wins / total;
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 1: BAND TUNING (max=10)
        // Systematically vary Street and Pro bands to find the sweet spot.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void BandTuning_WinRateAcrossStreetBands()
        {
            var configs = new[]
            {
                // (label, streetMin, streetMax, edgeMin, edgeMax)
                ("S: 10-14 (current)",  10, 14, 2, 5),
                ("S: 12-16",            12, 16, 2, 6),
                ("S: 14-18",            14, 18, 2, 7),
                ("S: 16-20",            16, 20, 3, 8),
                ("S: 18-22",            18, 22, 3, 9),
            };

            _output.WriteLine("=== Street Band Tuning (Pro fixed at 16-22, max=10) ===");
            _output.WriteLine($"{"Config",-22} {"WinRate",10} {"Assessment",-20}");
            _output.WriteLine(new string('-', 55));

            foreach (var (label, sMin, sMax, eMin, eMax) in configs)
            {
                var rng = new Random(Seed);
                double wr = RunBalance(
                    r => GenerateScaledHand(r,
                        sMin, sMax, eMin, eMax,        // street
                        16, 22, 2, 9,                  // pro (fixed)
                        10, 1, 3, 4, 8),               // hero (fixed)
                    r => BuildScaledAIHand(r, 10,
                        sMin, sMax, eMin, eMax),        // AI streets match player
                    rng);

                string assessment = wr < 0.30 ? "too weak" :
                                    wr < 0.40 ? "below target" :
                                    wr <= 0.65 ? "✓ target range" :
                                    wr <= 0.75 ? "slightly strong" : "too strong";

                _output.WriteLine($"{label,-22} {wr,10:P1} {assessment,-20}");
            }

            Assert.True(true);
        }

        [Fact]
        public void BandTuning_WinRateAcrossProBands()
        {
            var configs = new[]
            {
                ("P: 16-22 (current)", 16, 22, 2, 9),
                ("P: 18-24",           18, 24, 2, 9),
                ("P: 20-26",           20, 26, 3, 9),
                ("P: 22-28",           22, 28, 3, 9),
            };

            _output.WriteLine("=== Pro Band Tuning (Street fixed at 10-14, max=10) ===");
            _output.WriteLine($"{"Config",-20} {"WinRate",10} {"Assessment",-20}");
            _output.WriteLine(new string('-', 53));

            foreach (var (label, pMin, pMax, eMin, eMax) in configs)
            {
                var rng = new Random(Seed);
                double wr = RunBalance(
                    r => GenerateScaledHand(r,
                        10, 14, 2, 5,                  // street (fixed)
                        pMin, pMax, eMin, eMax,        // pro
                        10, 1, 3, 4, 8),               // hero (fixed)
                    r => BuildScaledAIHand(r, 10,
                        10, 14, 2, 5),
                    rng);

                string assessment = wr < 0.30 ? "too weak" :
                                    wr < 0.40 ? "below target" :
                                    wr <= 0.65 ? "✓ target range" :
                                    wr <= 0.75 ? "slightly strong" : "too strong";

                _output.WriteLine($"{label,-20} {wr,10:P1} {assessment,-20}");
            }

            Assert.True(true);
        }

        [Fact]
        public void BandTuning_CombinedStreetAndPro()
        {
            // Test Street + Pro raised together — more realistic than raising one at a time
            var configs = new[]
            {
                ("Current",        10, 14, 16, 22),
                ("Street+2",       12, 16, 16, 22),
                ("Pro+2",          10, 14, 18, 24),
                ("Both+2",         12, 16, 18, 24),
                ("Both+4",         14, 18, 20, 26),
                ("Both+6",         16, 20, 22, 28),
            };

            _output.WriteLine("=== Combined Street+Pro Band Tuning (max=10) ===");
            _output.WriteLine($"{"Config",-14} {"Street",8} {"Pro",8} {"WinRate",10} {"Assessment",-20}");
            _output.WriteLine(new string('-', 64));

            foreach (var (label, sMin, sMax, pMin, pMax) in configs)
            {
                var rng = new Random(Seed);
                double wr = RunBalance(
                    r => GenerateScaledHand(r,
                        sMin, sMax, 2, System.Math.Min(9, sMax / 2 + 1),
                        pMin, pMax, 2, 9,
                        10, 1, 3, 4, 8),
                    r => BuildScaledAIHand(r, 10,
                        sMin, sMax, 2, System.Math.Min(9, sMax / 2 + 1)),
                    rng);

                string assessment = wr < 0.30 ? "too weak" :
                                    wr < 0.40 ? "below target" :
                                    wr <= 0.65 ? "✓ target range" :
                                    wr <= 0.75 ? "slightly strong" : "too strong";

                _output.WriteLine($"{label,-14} {$"{sMin}-{sMax}",8} {$"{pMin}-{pMax}",8} {wr,10:P1} {assessment,-20}");
            }

            Assert.True(true);
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 2: STAT SCALE COMPARISON (1-10 vs 1-20 vs 1-30)
        // Keep relative proportions constant, scale the max stat.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void ScaleComparison_MirrorMatch_WinRateByScale()
        {
            // Mirror match (player vs player, same bands) across scales.
            // If capture is purely comparative, P1 win rate should stay ~50%
            // regardless of scale. Any deviation shows scale has real effect.
            var scales = new[] { 10, 20, 30, 50 };

            _output.WriteLine("=== Mirror Match Win Rate by Stat Scale ===");
            _output.WriteLine("(Identical decks, both players greedy)");
            _output.WriteLine($"{"Scale",8} {"P1 WinRate",12} {"Avg Captures",14} {"Notes",-30}");
            _output.WriteLine(new string('-', 68));

            foreach (int scale in scales)
            {
                int sMin = scale,      sMax = (int)(scale * 1.4);
                int eMin = scale / 5,  eMax = scale / 2;

                var rng      = new Random(Seed);
                int wins     = 0;
                double captures = 0;
                int total    = 0;

                for (int g = 0; g < Games; g++)
                {
                    TestLogger.Clear();
                    var hand = GenerateScaledHand(rng,
                        sMin, sMax, eMin, eMax,
                        sMin, sMax, eMin, eMax,
                        scale, eMin, eMin + 1, eMin + 2, eMax - 1);

                    var result = GameSimulator.RunGame(
                        hand, new List<CardData>(hand), // mirror — same deck
                        GameSimulator.Strategy.Greedy,
                        GameSimulator.Strategy.Greedy,
                        new MatchConfig(), rng);

                    if (result.Winner == 1) wins++;
                    captures += result.TotalCaptures;
                    total++;
                }

                double wr  = (double)wins / total;
                double cap = captures / total;
                string note = System.Math.Abs(wr - 0.50) < 0.05
                    ? "balanced (as expected)"
                    : wr > 0.55 ? "P1 advantage" : "P2 advantage";

                _output.WriteLine($"{$"1-{scale}",8} {wr,12:P1} {cap,14:F1} {note,-30}");
            }

            Assert.True(true);
        }

        [Fact]
        public void ScaleComparison_PlayerVsAI_WinRateByScale()
        {
            // The key test: does a wider stat range help players compete
            // against the AI, or does scaling everything proportionally
            // keep the same relative balance?
            var scales = new[] { 10, 20, 30, 50 };

            _output.WriteLine("=== Player vs AI Win Rate by Stat Scale ===");
            _output.WriteLine("(Stats proportionally scaled at each level)");
            _output.WriteLine($"{"Scale",8} {"P1 WinRate",12} {"Margin",10} {"Assessment",-25}");
            _output.WriteLine(new string('-', 58));

            foreach (int scale in scales)
            {
                int sMin  = scale;
                int sMax  = (int)(scale * 1.4);
                int eMin  = System.Math.Max(1, scale / 5);
                int eMax  = scale / 2;
                int pMin  = (int)(scale * 1.6);
                int pMax  = (int)(scale * 2.2);

                var rng = new Random(Seed);
                int wins = 0, total = 0;
                double totalMargin = 0;

                for (int c = 0; c < Crews; c++)
                {
                    var playerHand = GenerateScaledHand(rng,
                        sMin, sMax, eMin, eMax,
                        pMin, pMax, eMin, eMax,
                        scale, eMin, eMin + scale / 10, eMin + 2, eMax - 1);

                    for (int g = 0; g < GamesPerCrew; g++)
                    {
                        TestLogger.Clear();
                        var aiHand = BuildScaledAIHand(rng, scale,
                            sMin, sMax, eMin, eMax);

                        var result = GameSimulator.RunGame(
                            playerHand, aiHand,
                            GameSimulator.Strategy.Greedy,
                            GameSimulator.Strategy.Greedy,
                            new MatchConfig(), rng);

                        if (result.Winner == 1) wins++;
                        totalMargin += result.P1FinalScore - result.P2FinalScore;
                        total++;
                    }
                }

                double wr     = (double)wins / total;
                double margin = totalMargin / total;
                string assessment = wr < 0.30 ? "too weak" :
                                    wr < 0.40 ? "below target" :
                                    wr <= 0.65 ? "✓ target range" :
                                    wr <= 0.75 ? "slightly strong" : "too strong";

                _output.WriteLine($"{$"1-{scale}",8} {wr,12:P1} {margin,10:F2} {assessment,-25}");
            }

            _output.WriteLine("");
            _output.WriteLine("Key insight: if win rates are similar across scales,");
            _output.WriteLine("scale alone doesn't fix balance — band ratios matter more.");

            Assert.True(true);
        }

        [Fact]
        public void ScaleComparison_CaptureVariance_ByScale()
        {
            // Wider scales should produce more decisive captures and more
            // interesting board states. This measures average captures per game
            // and score margin spread across scales.
            var scales = new[] { 10, 20, 30, 50 };

            _output.WriteLine("=== Capture Variance by Stat Scale ===");
            _output.WriteLine($"{"Scale",8} {"AvgCaptures",13} {"AvgMargin",11} {"MarginStdDev",14}");
            _output.WriteLine(new string('-', 50));

            foreach (int scale in scales)
            {
                int sMin = scale,   sMax = (int)(scale * 1.4);
                int eMin = System.Math.Max(1, scale / 5);
                int eMax = scale / 2;

                var rng      = new Random(Seed);
                var margins  = new List<int>();
                double totalCaptures = 0;

                for (int g = 0; g < Games; g++)
                {
                    TestLogger.Clear();
                    var p1 = GenerateScaledHand(rng, sMin, sMax, eMin, eMax,
                        (int)(sMin*1.6), (int)(sMin*2.2), eMin, eMax,
                        scale, eMin, eMin+1, eMin+2, eMax-1);
                    var p2 = GenerateScaledHand(rng, sMin, sMax, eMin, eMax,
                        (int)(sMin*1.6), (int)(sMin*2.2), eMin, eMax,
                        scale, eMin, eMin+1, eMin+2, eMax-1);

                    var result = GameSimulator.RunGame(p1, p2,
                        GameSimulator.Strategy.Greedy,
                        GameSimulator.Strategy.Greedy,
                        new MatchConfig(), rng);

                    margins.Add(result.P1FinalScore - result.P2FinalScore);
                    totalCaptures += result.TotalCaptures;
                }

                double avgCap    = totalCaptures / Games;
                double avgMargin = margins.Average();
                double stdDev    = System.Math.Sqrt(margins.Select(m => System.Math.Pow(m - avgMargin, 2)).Average());

                _output.WriteLine($"{$"1-{scale}",8} {avgCap,13:F1} {avgMargin,11:F2} {stdDev,14:F2}");
            }

            _output.WriteLine("");
            _output.WriteLine("Higher stdDev = more decisive/varied outcomes.");
            _output.WriteLine("If stdDev increases with scale, wider range = more interesting games.");

            Assert.True(true);
        }

        // ── Stat helpers (mirrors Logic/CrewGenerator internals) ──────────────

        private static int[] DistributeStats(int total, int minEdge, int maxEdge, Random rng)
        {
            var edges     = new int[4];
            int remaining = total;
            for (int i = 0; i < 3; i++)
            {
                int lo   = System.Math.Max(minEdge, remaining - maxEdge * (3 - i));
                int hi   = System.Math.Min(maxEdge, remaining - minEdge * (3 - i));
                if (lo > hi) lo = hi;
                edges[i]  = rng.Next(lo, hi + 1);
                remaining -= edges[i];
            }
            edges[3] = System.Math.Clamp(remaining, minEdge, maxEdge);
            Shuffle(edges, rng);
            return edges;
        }

        private static void Shuffle<T>(T[] arr, Random rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }
    }
}