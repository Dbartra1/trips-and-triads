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
    /// Extended Scale-20 validation: districts, protocol fire rates, domain relevance.
    ///
    /// These tests answer three questions before Scale-20 goes to production:
    ///
    ///   1. DISTRICTS: does Scale-20 stay balanced across all 8 districts?
    ///      Are any districts broken by doubled stats?
    ///
    ///   2. PROTOCOL FIRE RATE: do Handshake/Tally still trigger at meaningful
    ///      rates at Scale-20? Wider spread could make ties/equal-sums so rare
    ///      the protocols become vestigial.
    ///
    ///   3. DOMAIN RELEVANCE: is +1 Aegis / +2 Killzone / +2 Lateral meaningful
    ///      on 20-point edges? Do domain heroes still outperform no-domain heroes?
    ///
    /// All tests use increased game counts for wider data bands.
    /// </summary>
    public class Scale20ExtendedTests
    {
        private readonly ITestOutputHelper _output;

        // Increased from 200x20=4000 to 500x20=10000 games per configuration
        private const int Crews        = 500;
        private const int GamesPerCrew = 20;
        private const int Seed         = 42;

        // Scale-20 constants (mirrored from Scale20PrototypeTests)
        private const int A20             = 20;
        private const int SoftMin20       = 4;
        private const int SoftMax20       = 8;
        private const int MidMin20        = 10;
        private const int MidMax20        = 16;
        private const int StreetMin20     = 20;
        private const int StreetMax20     = 28;
        private const int StreetEdgeMin20 = 4;
        private const int StreetEdgeMax20 = 10;
        private const int ProMin20        = 32;
        private const int ProMax20        = 44;
        private const int ProEdgeMin20    = 4;
        private const int ProEdgeMax20    = 18;
        private const int ChoirMidMin20   = 14;
        private const int ChoirMidMax20   = 18;

        public Scale20ExtendedTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 1: ALL DISTRICTS
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Scale20_AllDistricts_WinRateAndCaptureRate()
        {
            var districts = new (string name, MatchConfig config)[]
            {
                ("The Stub (Base)",  MatchConfig.BaseRules()),
                ("Glass Spire",      MatchConfig.GlassSpire()),
                ("The Killfloor",    MatchConfig.Killfloor()),
                ("Dead Channel",     MatchConfig.DeadChannel()),
                ("Sprawl Market",    MatchConfig.SprawlMarket()),
                ("Powder Room",      MatchConfig.PowderRoom()),
                ("The Hush",         MatchConfig.TheHush()),
                ("The Vault",        MatchConfig.TheVault()),
            };

            _output.WriteLine($"=== Scale-20 vs AI Across All Districts ({Crews} crews × {GamesPerCrew} games each) ===");
            _output.WriteLine($"{"District",-18} {"WinRate",10} {"AvgCap",8} {"AvgMargin",11} {"Assessment",-20}");
            _output.WriteLine(new string('-', 72));

            foreach (var (name, config) in districts)
            {
                var rng   = new Random(Seed);
                int wins  = 0, total = 0;
                double captures = 0, margin = 0;

                for (int c = 0; c < Crews; c++)
                {
                    var playerHand = GenerateScale20PlayerHand(rng);
                    for (int g = 0; g < GamesPerCrew; g++)
                    {
                        TestLogger.Clear();
                        var result = GameSimulator.RunGame(
                            playerHand, BuildScale20AIHand(rng),
                            GameSimulator.Strategy.Greedy,
                            GameSimulator.Strategy.Greedy,
                            config.Clone(), rng);

                        if (result.Winner == 1) wins++;
                        captures += result.TotalCaptures;
                        margin   += result.P1FinalScore - result.P2FinalScore;
                        total++;
                    }
                }

                double wr  = (double)wins / total;
                double cap = captures / total;
                double avg = margin / total;
                string assessment = wr < 0.30 ? "⚠ too weak" :
                                    wr < 0.40 ? "below target" :
                                    wr <= 0.65 ? "✓ target range" :
                                    wr <= 0.75 ? "slightly strong" : "⚠ too strong";

                _output.WriteLine($"{name,-18} {wr,10:P1} {cap,8:F1} {avg,11:F2} {assessment,-20}");
            }

            Assert.True(true); // data collection
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 2: PROTOCOL FIRE RATES
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Scale20_vs_Scale10_ProtocolFireRates()
        {
            // Measures how often each protocol fires per game at each scale.
            // Uses instrumented resolvers to count triggers.
            // "Fire rate" = avg protocol captures per game.
            // A fire rate near 0 means the protocol is vestigial at that scale.

            _output.WriteLine("=== Protocol Fire Rates: Scale-10 vs Scale-20 ===");
            _output.WriteLine("(Avg protocol-triggered captures per game, 10000 games each)");
            _output.WriteLine($"{"Protocol",-18} {"Scale-10",12} {"Scale-20",12} {"Change",-12}");
            _output.WriteLine(new string('-', 58));

            int games = 10000;

            // Helper: run games with a protocol config and count captures from that protocol
            double MeasureProtocol(
                Func<List<CardData>> p1Factory,
                Func<List<CardData>> p2Factory,
                MatchConfig config,
                int seed)
            {
                var rng  = new Random(seed);
                double totalProtocolCaptures = 0;

                // We approximate protocol captures by comparing captures in
                // protocol config vs base config — delta is protocol contribution
                var rngBase = new Random(seed);
                double baseCaptures = 0, configCaptures = 0;

                for (int g = 0; g < games; g++)
                {
                    TestLogger.Clear();
                    var r1 = GameSimulator.RunGame(p1Factory(), p2Factory(),
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        new MatchConfig(), rng);
                    baseCaptures += r1.TotalCaptures;

                    TestLogger.Clear();
                    var r2 = GameSimulator.RunGame(p1Factory(), p2Factory(),
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        config.Clone(), rngBase);
                    configCaptures += r2.TotalCaptures;
                }

                return System.Math.Max(0, (configCaptures - baseCaptures) / games);
            }

            var protocols = new (string name, MatchConfig config)[]
            {
                ("Handshake",      new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } }),
                ("Tally",          new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } }),
                ("Wall Signature", new MatchConfig { Protocols = new List<IProtocol> { new WallSignatureProtocol(), new HandshakeProtocol() } }),
                ("Cascade",        new MatchConfig { Cascade = true, Protocols = new List<IProtocol> { new HandshakeProtocol() } }),
            };

            foreach (var (name, config) in protocols)
            {
                // Scale-10 factories
                Func<List<CardData>> s10p1 = () =>
                {
                    var rng = new Random(Guid.NewGuid().GetHashCode());
                    var gen = CrewGenerator.Generate(rng);
                    return CrewGenerator.SelectBestFive(gen);
                };
                Func<List<CardData>> s10p2 = () =>
                {
                    var rng = new Random(Guid.NewGuid().GetHashCode());
                    return new List<CardData>
                    {
                        CardFactory.Vesna().Data,
                        CardFactory.Verity().Data,
                        CardFactory.Street("s1",t:3,r:3,b:3,l:3).Data,
                        CardFactory.Street("s2",t:3,r:3,b:3,l:3).Data,
                        CardFactory.Street("s3",t:3,r:3,b:3,l:3).Data,
                    };
                };

                // Scale-20 factories
                Func<List<CardData>> s20p1 = () =>
                {
                    var rng = new Random(Guid.NewGuid().GetHashCode());
                    return GenerateScale20PlayerHand(rng);
                };
                Func<List<CardData>> s20p2 = () =>
                {
                    var rng = new Random(Guid.NewGuid().GetHashCode());
                    return BuildScale20AIHand(rng);
                };

                double rate10 = MeasureProtocol(s10p1, s10p2, config, Seed);
                double rate20 = MeasureProtocol(s20p1, s20p2, config, Seed + 1);
                double change = rate20 - rate10;
                string trend  = System.Math.Abs(change) < 0.05 ? "≈ same" :
                                change > 0 ? $"+{change:F2} (more)" : $"{change:F2} (less)";

                _output.WriteLine($"{name,-18} {rate10,12:F3} {rate20,12:F3} {trend,-12}");
            }

            _output.WriteLine("");
            _output.WriteLine("Note: near-zero rates indicate the protocol rarely fires");
            _output.WriteLine("and may need stat adjustments at Scale-20.");

            Assert.True(true);
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 3: DOMAIN RELEVANCE
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Scale20_DomainRelevance_DomainVsNoDomain()
        {
            // Compare win rates of:
            //   A) Scale-20 deck with a domain hero (Aegis/Killzone/Lateral/Sprawl)
            //   B) Scale-20 deck with a no-domain hero (same stats, domain stripped)
            // If (A) ≈ (B), the +1/+2 bonuses are noise at Scale-20.
            // If (A) > (B) consistently, domains are still meaningful.

            var domains = new (string name, DomainType domain, int[] stats)[]
            {
                ("Aegis (Yune-like)",    DomainType.AegisProtocol, new[]{20, 16, 16, 6}),
                ("Killzone (Grin-like)", DomainType.Killzone,       new[]{20, 20, 4, 6}),
                ("Lateral (Riven-like)", DomainType.LateralGrid,    new[]{6, 18, 18, 4}),
                ("Sprawl (Mara-like)",   DomainType.Sprawl,         new[]{12, 12, 12, 12}),
            };

            int games = 10000;

            _output.WriteLine($"=== Scale-20 Domain Relevance ({games} games each) ===");
            _output.WriteLine($"{"Domain",-22} {"With Domain",13} {"No Domain",11} {"Delta",8} {"Meaningful?"}");
            _output.WriteLine(new string('-', 70));

            foreach (var (name, domain, stats) in domains)
            {
                // With domain
                var rngWith = new Random(Seed);
                int winsWith = 0;
                for (int g = 0; g < games; g++)
                {
                    TestLogger.Clear();
                    var p1 = MakeDomainHand(stats[0],stats[1],stats[2],stats[3], domain, rngWith);
                    var p2 = BuildScale20AIHand(rngWith);
                    var result = GameSimulator.RunGame(p1, p2,
                        GameSimulator.Strategy.Greedy,
                        GameSimulator.Strategy.Greedy,
                        MatchConfig.BaseRules(), rngWith);
                    if (result.Winner == 1) winsWith++;
                }

                // Without domain (same stats, domain removed)
                var rngWithout = new Random(Seed);
                int winsWithout = 0;
                for (int g = 0; g < games; g++)
                {
                    TestLogger.Clear();
                    var p1 = MakeDomainHand(stats[0],stats[1],stats[2],stats[3], DomainType.None, rngWithout);
                    var p2 = BuildScale20AIHand(rngWithout);
                    var result = GameSimulator.RunGame(p1, p2,
                        GameSimulator.Strategy.Greedy,
                        GameSimulator.Strategy.Greedy,
                        MatchConfig.BaseRules(), rngWithout);
                    if (result.Winner == 1) winsWithout++;
                }

                double wrWith    = (double)winsWith    / games;
                double wrWithout = (double)winsWithout / games;
                double delta     = wrWith - wrWithout;
                string meaningful = delta > 0.03 ? "✓ yes" :
                                    delta > 0.01 ? "marginal" : "⚠ negligible";

                _output.WriteLine($"{name,-22} {wrWith,13:P1} {wrWithout,11:P1} {delta,8:+0.0%;-0.0%} {meaningful}");
            }

            _output.WriteLine("");
            _output.WriteLine("Note: if delta < 1%, domain bonuses may need scaling too.");
            _output.WriteLine("Consider doubling domain bonuses (+2 Aegis, +4 Killzone/Lateral)");
            _output.WriteLine("if domains prove negligible at Scale-20.");

            Assert.True(true);
        }

        [Fact]
        public void Scale20_DomainRelevance_AcrossDistricts()
        {
            // Does domain advantage vary by district?
            // Some districts (Cascade, The Hush) amplify captures, which
            // could make domain bonuses more or less meaningful.

            int games = 5000;

            var districts = new (string name, MatchConfig config)[]
            {
                ("Base Rules",   MatchConfig.BaseRules()),
                ("Glass Spire",  MatchConfig.GlassSpire()),
                ("Powder Room",  MatchConfig.PowderRoom()),
                ("The Hush",     MatchConfig.TheHush()),
                ("The Vault",    MatchConfig.TheVault()),
            };

            // Use Aegis as the representative domain (most common on generated heroes)
            var aegisStats = new[]{20, 16, 16, 6};

            _output.WriteLine($"=== Scale-20 Aegis Domain: With vs Without by District ({games} games each) ===");
            _output.WriteLine($"{"District",-16} {"With Domain",13} {"No Domain",11} {"Delta",8}");
            _output.WriteLine(new string('-', 52));

            foreach (var (name, config) in districts)
            {
                var rngWith    = new Random(Seed);
                var rngWithout = new Random(Seed);
                int wWith = 0, wWithout = 0;

                for (int g = 0; g < games; g++)
                {
                    TestLogger.Clear();
                    var p1With = MakeDomainHand(aegisStats[0],aegisStats[1],
                        aegisStats[2],aegisStats[3], DomainType.AegisProtocol, rngWith);
                    var r1 = GameSimulator.RunGame(p1With, BuildScale20AIHand(rngWith),
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        config.Clone(), rngWith);
                    if (r1.Winner == 1) wWith++;

                    TestLogger.Clear();
                    var p1No = MakeDomainHand(aegisStats[0],aegisStats[1],
                        aegisStats[2],aegisStats[3], DomainType.None, rngWithout);
                    var r2 = GameSimulator.RunGame(p1No, BuildScale20AIHand(rngWithout),
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        config.Clone(), rngWithout);
                    if (r2.Winner == 1) wWithout++;
                }

                double wrWith    = (double)wWith    / games;
                double wrWithout = (double)wWithout / games;
                double delta     = wrWith - wrWithout;

                _output.WriteLine($"{name,-16} {wrWith,13:P1} {wrWithout,11:P1} {delta,8:+0.0%;-0.0%}");
            }

            Assert.True(true);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private List<CardData> MakeDomainHand(
            int t, int r, int b, int l,
            DomainType domain, Random rng)
        {
            var hero = new CardData
            {
                Name       = "Hero",
                Top        = t, Right  = r, Bottom = b, Left = l,
                Tier       = Tier.Hero, Level = A20,
                DomainType = domain, Faction = Faction.Ascendant,
            };

            var hand = new List<CardData> { hero };

            // Fill with 4 average Scale-20 Street cards
            for (int i = 0; i < 4; i++)
            {
                int total = rng.Next(StreetMin20, StreetMax20 + 1);
                var e     = DistributeStats20(total, StreetEdgeMin20, StreetEdgeMax20, rng);
                hand.Add(new CardData
                {
                    Name    = $"Support{i}",
                    Top     = e[0], Right  = e[1], Bottom = e[2], Left = e[3],
                    Tier    = Tier.Street, Level = 3, Faction = Faction.Ascendant,
                });
            }
            return hand;
        }

        private List<CardData> GenerateScale20PlayerHand(Random rng)
        {
            var cards = new List<CardData>();

            var heroSlots   = GenerateHeroSlots20(rng);
            var heroFaction = PickFaction(rng);
            cards.Add(new CardData
            {
                Name       = "Hero",
                Top        = heroSlots[0], Right  = heroSlots[1],
                Bottom     = heroSlots[2], Left   = heroSlots[3],
                Tier       = Tier.Hero, Level = A20, Faction = heroFaction,
            });

            int proTotal = rng.Next(ProMin20, ProMax20 + 1);
            var proEdges = DistributeStats20(proTotal, ProEdgeMin20, ProEdgeMax20, rng);
            cards.Add(new CardData
            {
                Name    = "Pro",
                Top     = proEdges[0], Right  = proEdges[1],
                Bottom  = proEdges[2], Left   = proEdges[3],
                Tier    = Tier.Pro, Level = 12, Faction = PickFaction(rng),
            });

            for (int i = 0; i < 5; i++)
            {
                int total = rng.Next(StreetMin20, StreetMax20 + 1);
                var e     = DistributeStats20(total, StreetEdgeMin20, StreetEdgeMax20, rng);
                cards.Add(new CardData
                {
                    Name    = $"Street{i}",
                    Top     = e[0], Right  = e[1], Bottom = e[2], Left = e[3],
                    Tier    = Tier.Street, Level = 3, Faction = PickFaction(rng),
                });
            }

            var best = cards.Where(c => c.Tier != Tier.Hero)
                .OrderByDescending(c => c.Top + c.Right + c.Bottom + c.Left)
                .Take(4).ToList();
            best.Insert(0, cards.First(c => c.Tier == Tier.Hero));
            return best;
        }

        private List<CardData> BuildScale20AIHand(Random rng)
        {
            var hand = new List<CardData>();
            hand.Add(new CardData
            {
                Name        = "Vesna",
                Top         = A20, Right  = A20, Bottom = A20, Left = A20,
                Tier        = Tier.Hero, Level = A20,
                AbilityType = AbilityType.Decay, Faction = Faction.HollowChoir,
            });
            hand.Add(new CardData
            {
                Name    = "Verity",
                Top     = 14, Right  = 18, Bottom = 14, Left = 18,
                Tier    = Tier.TopTier, Level = 16, Faction = Faction.Effigy,
            });
            for (int i = 0; i < 3; i++)
            {
                int total = rng.Next(StreetMin20, StreetMax20 + 1);
                var e     = DistributeStats20(total, StreetEdgeMin20, StreetEdgeMax20, rng);
                hand.Add(new CardData
                {
                    Name    = $"AIStreet{i}",
                    Top     = e[0], Right  = e[1], Bottom = e[2], Left = e[3],
                    Tier    = Tier.Street, Level = 3, Faction = Faction.None,
                });
            }
            return hand;
        }

        private int[] GenerateHeroSlots20(Random rng)
        {
            var faction = PickFaction(rng);
            var slots   = new int[4];
            int soft    = rng.Next(SoftMin20, SoftMax20 + 1);
            int midA    = rng.Next(MidMin20,  MidMax20  + 1);
            int midB    = rng.Next(MidMin20,  MidMax20  + 1);

            switch (faction)
            {
                case Faction.Ascendant:
                    slots[0] = A20; slots[2] = soft;
                    slots[1] = midA; slots[3] = midB; break;
                case Faction.Razorkin:
                    if (rng.Next(2) == 0) { slots[0] = A20; slots[1] = soft; }
                    else                  { slots[3] = A20; slots[2] = soft; }
                    FillRemainingMid20(slots, midA, midB); break;
                case Faction.Ghostwire:
                    if (rng.Next(2) == 0) { slots[3] = A20; slots[0] = soft; }
                    else                  { slots[1] = A20; slots[2] = soft; }
                    FillRemainingMid20(slots, midA, midB); break;
                case Faction.Commons:
                    int ev = rng.Next(MidMin20, MidMax20 + 1);
                    slots[0] = A20; slots[1] = soft; slots[2] = ev; slots[3] = ev;
                    Shuffle(slots, rng); break;
                case Faction.Effigy:
                    if (rng.Next(2) == 0)
                    { slots[0] = A20; slots[2] = A20; slots[1] = soft; slots[3] = soft; }
                    else
                    { slots[1] = A20; slots[3] = A20; slots[0] = soft; slots[2] = soft; }
                    break;
                case Faction.Lacquer:
                    int ls = rng.Next(SoftMin20, SoftMin20 + 3);
                    int la = rng.Next(MidMin20, MidMax20 + 1);
                    int lb = rng.Next(MidMin20, MidMax20 + 1);
                    AssignRandom20(slots, A20, ls, la, lb, rng); break;
                case Faction.HollowChoir:
                    int td = rng.Next(4); slots[td] = 0;
                    var rem = Enumerable.Range(0,4).Where(i => i != td).ToList();
                    int ai  = rem[rng.Next(rem.Count)]; slots[ai] = A20; rem.Remove(ai);
                    slots[rem[0]] = rng.Next(ChoirMidMin20, ChoirMidMax20 + 1);
                    slots[rem[1]] = rng.Next(ChoirMidMin20, ChoirMidMax20 + 1); break;
                default:
                    AssignRandom20(slots, A20, soft, midA, midB, rng); break;
            }
            return slots;
        }

        private static void FillRemainingMid20(int[] slots, int midA, int midB)
        {
            int filled = 0;
            for (int i = 0; i < 4; i++)
                if (slots[i] == 0) slots[i] = filled++ == 0 ? midA : midB;
        }

        private static void AssignRandom20(int[] slots, int a, int b, int c, int d, Random rng)
        {
            var vals = new[] {a,b,c,d};
            Shuffle(vals, rng);
            for (int i = 0; i < 4; i++) slots[i] = vals[i];
        }

        private static int[] DistributeStats20(int total, int minEdge, int maxEdge, Random rng)
        {
            var edges = new int[4]; int remaining = total;
            for (int i = 0; i < 3; i++)
            {
                int lo = System.Math.Max(minEdge, remaining - maxEdge * (3-i));
                int hi = System.Math.Min(maxEdge, remaining - minEdge * (3-i));
                if (lo > hi) lo = hi;
                edges[i]   = rng.Next(lo, hi + 1);
                remaining -= edges[i];
            }
            edges[3] = System.Math.Clamp(remaining, minEdge, maxEdge);
            Shuffle(edges, rng); return edges;
        }

        private static void Shuffle<T>(T[] arr, Random rng)
        {
            for (int i = arr.Length-1; i > 0; i--)
            { int j = rng.Next(i+1); (arr[i], arr[j]) = (arr[j], arr[i]); }
        }

        private static readonly Faction[] AllFactions =
        {
            Faction.Ascendant, Faction.Razorkin, Faction.Ghostwire,
            Faction.Commons,   Faction.Effigy,   Faction.Lacquer, Faction.HollowChoir,
        };
        private static Faction PickFaction(Random rng) => AllFactions[rng.Next(AllFactions.Length)];
    }

}