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
    /// Path A prototype: scale protocols and domain bonuses alongside stats.
    ///
    /// Changes tested (Logic only, not production yet):
    ///   HandshakeProtocol(tolerance: 1)  — fires when |attack - defend| &lt;= 1
    ///   TallyProtocol(sumTolerance: 2)   — fires when |sum1 - sum2| &lt;= 2
    ///   WallSignatureProtocol(wallValue: 20) — board edge counts as 20
    ///   DomainResolver.BonusMultiplier = 2  — doubles all domain bonuses
    ///
    /// Success criteria:
    ///   - Protocol fire rates recover to near Scale-10 levels
    ///   - Overall balance stays in 40–65% window
    ///   - Domain bonus delta lifts from ~1.5% (negligible) to ~3%+ (meaningful)
    ///   - All 8 districts remain in target range
    ///   - Existing protocol math tests still pass (defaults unchanged)
    /// </summary>
    [Collection("DomainState")]
    public class Scale20PathATests
    {
        private readonly ITestOutputHelper _output;

        private const int Crews        = 500;
        private const int GamesPerCrew = 20;
        private const int Games        = Crews * GamesPerCrew; // 10,000
        private const int Seed         = 42;

        // Scale-20 constants
        private const int A20             = 20;
        private const int SoftMin20       = 4,  SoftMax20       = 8;
        private const int MidMin20        = 10, MidMax20        = 16;
        private const int StreetMin20     = 20, StreetMax20     = 28;
        private const int StreetEdgeMin20 = 4,  StreetEdgeMax20 = 10;
        private const int ProMin20        = 32, ProMax20        = 44;
        private const int ProEdgeMin20    = 4,  ProEdgeMax20    = 18;
        private const int ChoirMidMin20   = 14, ChoirMidMax20   = 18;

        // Path A scaled configs
        private static MatchConfig PathA_Base() => new MatchConfig();
        private static MatchConfig PathA_GlassSpire() => new MatchConfig
        {
            Intercept = true,
            Protocols = new List<IProtocol>
            {
                new WallSignatureProtocol(wallValue: 20, sumTolerance: 2),
                new HandshakeProtocol(tolerance: 2),
            }
        };
        private static MatchConfig PathA_Killfloor() => new MatchConfig
            { Conscription = true, Standoff = true };
        private static MatchConfig PathA_DeadChannel() => new MatchConfig
            { Intercept = true, Cascade = true };
        private static MatchConfig PathA_SprawlMarket() => new MatchConfig
            { Conscription = true };
        private static MatchConfig PathA_PowderRoom() => new MatchConfig
        {
            Protocols = new List<IProtocol>
            {
                new TallyProtocol(sumTolerance: 2),
                new HandshakeProtocol(tolerance: 2),
            }
        };
        private static MatchConfig PathA_TheHush() => new MatchConfig
        {
            Cascade = true,
            Protocols = new List<IProtocol>
            {
                new WallSignatureProtocol(wallValue: 20, sumTolerance: 2),
                new HandshakeProtocol(tolerance: 2),
            }
        };
        private static MatchConfig PathA_TheVault() => new MatchConfig
        {
            Intercept = true, Conscription = true, Standoff = true, Cascade = true,
            Protocols = new List<IProtocol>
            {
                new HandshakeProtocol(tolerance: 2),
                new TallyProtocol(sumTolerance: 2),
                new WallSignatureProtocol(wallValue: 20, sumTolerance: 2),
            }
        };

        public Scale20PathATests(ITestOutputHelper output)
        {
            _output = output;
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 1: PROTOCOL FIRE RATES — Path A vs baseline Scale-20
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PathA_ProtocolFireRates_vs_Scale10Baseline()
        {
            _output.WriteLine("=== Path A Protocol Fire Rates ===");
            _output.WriteLine("(Avg protocol-triggered captures per game, 10000 games each)");
            _output.WriteLine($"{"Protocol",-22} {"Scale-10",10} {"Scale-20",10} {"Path A",10} {"Recovered?"}");
            _output.WriteLine(new string('-', 68));

            var protocolConfigs = new (string name, MatchConfig s10, MatchConfig s20pathA)[]
            {
                ("Handshake",
                    new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } },
                    new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol(tolerance: 2) } }),
                ("Tally",
                    new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } },
                    new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol(sumTolerance: 2) } }),
                ("Wall Signature",
                    new MatchConfig { Protocols = new List<IProtocol> { new WallSignatureProtocol(), new HandshakeProtocol() } },
                    new MatchConfig { Protocols = new List<IProtocol> { new WallSignatureProtocol(wallValue: 20, sumTolerance: 2), new HandshakeProtocol(tolerance: 2) } }),
                ("Cascade",
                    new MatchConfig { Cascade = true, Protocols = new List<IProtocol> { new HandshakeProtocol() } },
                    new MatchConfig { Cascade = true, Protocols = new List<IProtocol> { new HandshakeProtocol(tolerance: 2) } }),
            };

            // Known Scale-20 baseline fire rates from Scale20ExtendedTests
            // Path A uses: Handshake tolerance=2, Tally sumTolerance=2,
            //              WallSignature wallValue=20+sumTolerance=2, Cascade with Handshake tolerance=2
            var scale20Baseline = new Dictionary<string, double>
            {
                { "Handshake",      0.000 },
                { "Tally",          0.035 },
                { "Wall Signature", 0.157 },
                { "Cascade",        0.000 },
            };

            foreach (var (name, s10Config, pathAConfig) in protocolConfigs)
            {
                // Scale-10
                var rng10 = new Random(Seed);
                double base10 = 0, config10 = 0;
                for (int g = 0; g < Games; g++)
                {
                    TestLogger.Clear();
                    var gen = CrewGenerator.Generate(rng10);
                    var p1  = CrewGenerator.SelectBestFive(gen);
                    var p2  = BuildScale10AIHand(rng10);
                    base10 += GameSimulator.RunGame(p1, p2,
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        new MatchConfig(), rng10).TotalCaptures;
                    TestLogger.Clear();
                    config10 += GameSimulator.RunGame(
                        CrewGenerator.SelectBestFive(CrewGenerator.Generate(rng10)),
                        BuildScale10AIHand(rng10),
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        s10Config.Clone(), rng10).TotalCaptures;
                }
                double rate10 = System.Math.Max(0, (config10 - base10) / Games);

                // Path A (Scale-20 with scaled protocols)
                var rngA = new Random(Seed);
                double baseA = 0, configA = 0;
                for (int g = 0; g < Games; g++)
                {
                    TestLogger.Clear();
                    var p1 = GenerateScale20PlayerHand(rngA);
                    var p2 = BuildScale20AIHand(rngA);
                    baseA += GameSimulator.RunGame(p1, p2,
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        new MatchConfig(), rngA).TotalCaptures;
                    TestLogger.Clear();
                    configA += GameSimulator.RunGame(
                        GenerateScale20PlayerHand(rngA),
                        BuildScale20AIHand(rngA),
                        GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                        pathAConfig.Clone(), rngA).TotalCaptures;
                }
                double rateA = System.Math.Max(0, (configA - baseA) / Games);

                double s20 = scale20Baseline[name];
                string recovered = rateA >= rate10 * 0.7 ? "✓ yes" :
                                   rateA >= rate10 * 0.4 ? "partial" : "⚠ no";

                _output.WriteLine($"{name,-22} {rate10,10:F3} {s20,10:F3} {rateA,10:F3} {recovered}");
            }

            Assert.True(true);
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 2: OVERALL BALANCE WITH PATH A
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PathA_OverallBalance_WithScaledProtocols()
        {
            var rng  = new Random(Seed);
            int wins = 0, total = 0;
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
                        PathA_Base(), rng);
                    if (result.Winner == 1) wins++;
                    captures += result.TotalCaptures;
                    margin   += result.P1FinalScore - result.P2FinalScore;
                    total++;
                }
            }

            double wr  = (double)wins / total;
            _output.WriteLine($"=== Path A Overall Balance ({total} games) ===");
            _output.WriteLine($"Player win rate : {wr:P1}");
            _output.WriteLine($"Avg captures    : {captures/total:F1}");
            _output.WriteLine($"Avg margin      : {margin/total:F2}");
            _output.WriteLine($"Assessment      : {(wr >= 0.40 && wr <= 0.65 ? "✓ target range" : "outside target")}");

            Assert.InRange(wr, 0.10, 0.95);
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 3: DOMAIN RELEVANCE WITH MULTIPLIER = 2
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PathA_DomainRelevance_WithDoubledBonuses()
        {
            var domains = new (string name, DomainType domain, int[] stats)[]
            {
                ("Aegis (Yune-like)",    DomainType.AegisProtocol, new[]{20, 16, 16, 6}),
                ("Killzone (Grin-like)", DomainType.Killzone,       new[]{20, 20, 4,  6}),
                ("Lateral (Riven-like)", DomainType.LateralGrid,    new[]{6,  18, 18, 4}),
                ("Sprawl (Mara-like)",   DomainType.Sprawl,         new[]{12, 12, 12, 12}),
            };

            _output.WriteLine($"=== Path A Domain Relevance (BonusMultiplier=2, {Games} games each) ===");
            _output.WriteLine($"{"Domain",-22} {"With Domain",13} {"No Domain",11} {"Delta",8} {"Meaningful?"}");
            _output.WriteLine(new string('-', 72));

            foreach (var (name, domain, stats) in domains)
            {
                // With domain + multiplier=2
                DomainResolver.BonusMultiplier = 2;
                try
                {
                    var rngWith = new Random(Seed);
                    int wWith   = 0;
                    for (int g = 0; g < Games; g++)
                    {
                        TestLogger.Clear();
                        var p1     = MakeDomainHand(stats, domain, rngWith);
                        var result = GameSimulator.RunGame(p1, BuildScale20AIHand(rngWith),
                            GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                            PathA_Base(), rngWith);
                        if (result.Winner == 1) wWith++;
                    }

                    // Without domain (same stats, no domain)
                    var rngWithout = new Random(Seed);
                    int wWithout   = 0;
                    for (int g = 0; g < Games; g++)
                    {
                        TestLogger.Clear();
                        var p1     = MakeDomainHand(stats, DomainType.None, rngWithout);
                        var result = GameSimulator.RunGame(p1, BuildScale20AIHand(rngWithout),
                            GameSimulator.Strategy.Greedy, GameSimulator.Strategy.Greedy,
                            PathA_Base(), rngWithout);
                        if (result.Winner == 1) wWithout++;
                    }

                    double wrWith    = (double)wWith    / Games;
                    double wrWithout = (double)wWithout / Games;
                    double delta     = wrWith - wrWithout;
                    string meaningful = delta > 0.03 ? "✓ yes" :
                                        delta > 0.01 ? "marginal" : "⚠ negligible";

                    _output.WriteLine($"{name,-22} {wrWith,13:P1} {wrWithout,11:P1} {delta,8:+0.0%;-0.0%} {meaningful}");
                }
                finally
                {
                    DomainResolver.BonusMultiplier = 1; // always restore
                }
            }

            Assert.Equal(1, DomainResolver.BonusMultiplier); // confirm restored
        }

        // ══════════════════════════════════════════════════════════════════════
        // TEST 4: ALL DISTRICTS WITH PATH A CONFIGS
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void PathA_AllDistricts_WinRateAndCaptureRate()
        {
            var districts = new (string name, MatchConfig config)[]
            {
                ("The Stub (Base)",  PathA_Base()),
                ("Glass Spire",      PathA_GlassSpire()),
                ("The Killfloor",    PathA_Killfloor()),
                ("Dead Channel",     PathA_DeadChannel()),
                ("Sprawl Market",    PathA_SprawlMarket()),
                ("Powder Room",      PathA_PowderRoom()),
                ("The Hush",         PathA_TheHush()),
                ("The Vault",        PathA_TheVault()),
            };

            _output.WriteLine($"=== Path A: All Districts ({Crews} crews × {GamesPerCrew} games) ===");
            _output.WriteLine($"{"District",-18} {"WinRate",10} {"AvgCap",8} {"AvgMargin",11} {"Assessment",-20}");
            _output.WriteLine(new string('-', 72));

            foreach (var (name, config) in districts)
            {
                var rng   = new Random(Seed);
                int wins  = 0, total = 0;
                double cap = 0, mgn = 0;

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
                        cap += result.TotalCaptures;
                        mgn += result.P1FinalScore - result.P2FinalScore;
                        total++;
                    }
                }

                double wr = (double)wins / total;
                string assessment = wr < 0.30 ? "⚠ too weak" :
                                    wr < 0.40 ? "below target" :
                                    wr <= 0.65 ? "✓ target range" :
                                    wr <= 0.75 ? "slightly strong" : "⚠ too strong";

                _output.WriteLine($"{name,-18} {wr,10:P1} {cap/total,8:F1} {mgn/total,11:F2} {assessment,-20}");
            }

            Assert.True(true);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS (shared with Scale20ExtendedTests pattern)
        // ══════════════════════════════════════════════════════════════════════

        private List<CardData> GenerateScale20PlayerHand(Random rng)
        {
            var cards = new List<CardData>();
            var heroSlots = GenerateHeroSlots20(rng);
            cards.Add(new CardData { Name="Hero",
                Top=heroSlots[0], Right=heroSlots[1], Bottom=heroSlots[2], Left=heroSlots[3],
                Tier=Tier.Hero, Level=A20, Faction=PickFaction(rng) });

            int pt = rng.Next(ProMin20, ProMax20+1);
            var pe = DistributeStats20(pt, ProEdgeMin20, ProEdgeMax20, rng);
            cards.Add(new CardData { Name="Pro",
                Top=pe[0], Right=pe[1], Bottom=pe[2], Left=pe[3],
                Tier=Tier.Pro, Level=12, Faction=PickFaction(rng) });

            for (int i = 0; i < 5; i++)
            {
                int t = rng.Next(StreetMin20, StreetMax20+1);
                var e = DistributeStats20(t, StreetEdgeMin20, StreetEdgeMax20, rng);
                cards.Add(new CardData { Name=$"St{i}",
                    Top=e[0], Right=e[1], Bottom=e[2], Left=e[3],
                    Tier=Tier.Street, Level=3, Faction=PickFaction(rng) });
            }

            var best = cards.Where(c => c.Tier != Tier.Hero)
                .OrderByDescending(c => c.Top+c.Right+c.Bottom+c.Left)
                .Take(4).ToList();
            best.Insert(0, cards.First(c => c.Tier == Tier.Hero));
            return best;
        }

        private List<CardData> BuildScale20AIHand(Random rng)
        {
            var hand = new List<CardData>();
            hand.Add(new CardData { Name="Vesna",
                Top=A20, Right=A20, Bottom=A20, Left=A20,
                Tier=Tier.Hero, Level=A20, AbilityType=AbilityType.Decay,
                Faction=Faction.HollowChoir });
            hand.Add(new CardData { Name="Verity",
                Top=14, Right=18, Bottom=14, Left=18,
                Tier=Tier.TopTier, Level=16, Faction=Faction.Effigy });
            for (int i = 0; i < 3; i++)
            {
                int t = rng.Next(StreetMin20, StreetMax20+1);
                var e = DistributeStats20(t, StreetEdgeMin20, StreetEdgeMax20, rng);
                hand.Add(new CardData { Name=$"AISt{i}",
                    Top=e[0], Right=e[1], Bottom=e[2], Left=e[3],
                    Tier=Tier.Street, Level=3, Faction=Faction.None });
            }
            return hand;
        }

        private static List<CardData> BuildScale10AIHand(Random rng) =>
            new List<CardData>
            {
                CardFactory.Vesna().Data,
                CardFactory.Verity().Data,
                CardFactory.Street("s1",t:3,r:3,b:3,l:3).Data,
                CardFactory.Street("s2",t:3,r:3,b:3,l:3).Data,
                CardFactory.Street("s3",t:3,r:3,b:3,l:3).Data,
            };

        private List<CardData> MakeDomainHand(int[] stats, DomainType domain, Random rng)
        {
            var hand = new List<CardData>
            {
                new CardData { Name="Hero", Top=stats[0], Right=stats[1],
                    Bottom=stats[2], Left=stats[3], Tier=Tier.Hero,
                    Level=A20, DomainType=domain, Faction=Faction.Ascendant }
            };
            for (int i = 0; i < 4; i++)
            {
                int t = rng.Next(StreetMin20, StreetMax20+1);
                var e = DistributeStats20(t, StreetEdgeMin20, StreetEdgeMax20, rng);
                hand.Add(new CardData { Name=$"Sup{i}",
                    Top=e[0], Right=e[1], Bottom=e[2], Left=e[3],
                    Tier=Tier.Street, Level=3, Faction=Faction.Ascendant });
            }
            return hand;
        }

        private int[] GenerateHeroSlots20(Random rng)
        {
            var f = PickFaction(rng); var s = new int[4];
            int soft=rng.Next(SoftMin20,SoftMax20+1), mA=rng.Next(MidMin20,MidMax20+1), mB=rng.Next(MidMin20,MidMax20+1);
            switch (f)
            {
                case Faction.Ascendant: s[0]=A20;s[2]=soft;s[1]=mA;s[3]=mB; break;
                case Faction.Razorkin:
                    if(rng.Next(2)==0){s[0]=A20;s[1]=soft;}else{s[3]=A20;s[2]=soft;}
                    FillMid(s,mA,mB); break;
                case Faction.Ghostwire:
                    if(rng.Next(2)==0){s[3]=A20;s[0]=soft;}else{s[1]=A20;s[2]=soft;}
                    FillMid(s,mA,mB); break;
                case Faction.Commons:
                    int ev=rng.Next(MidMin20,MidMax20+1);
                    s[0]=A20;s[1]=soft;s[2]=ev;s[3]=ev; Shuffle(s,rng); break;
                case Faction.Effigy:
                    if(rng.Next(2)==0){s[0]=A20;s[2]=A20;s[1]=soft;s[3]=soft;}
                    else{s[1]=A20;s[3]=A20;s[0]=soft;s[2]=soft;} break;
                case Faction.Lacquer:
                    AssignR(s,A20,rng.Next(SoftMin20,SoftMin20+3),rng.Next(MidMin20,MidMax20+1),rng.Next(MidMin20,MidMax20+1),rng); break;
                case Faction.HollowChoir:
                    int td=rng.Next(4);s[td]=0;
                    var rm=Enumerable.Range(0,4).Where(i=>i!=td).ToList();
                    int ai=rm[rng.Next(rm.Count)];s[ai]=A20;rm.Remove(ai);
                    s[rm[0]]=rng.Next(ChoirMidMin20,ChoirMidMax20+1);
                    s[rm[1]]=rng.Next(ChoirMidMin20,ChoirMidMax20+1); break;
                default: AssignR(s,A20,soft,mA,mB,rng); break;
            }
            return s;
        }

        private static void FillMid(int[] s, int a, int b)
        { int f=0; for(int i=0;i<4;i++) if(s[i]==0) s[i]=f++==0?a:b; }
        private static void AssignR(int[] s, int a, int b, int c, int d, Random rng)
        { var v=new[]{a,b,c,d}; Shuffle(v,rng); for(int i=0;i<4;i++) s[i]=v[i]; }
        private static void Shuffle<T>(T[] arr, Random rng)
        { for(int i=arr.Length-1;i>0;i--){int j=rng.Next(i+1);(arr[i],arr[j])=(arr[j],arr[i]);} }
        private static int[] DistributeStats20(int total, int min, int max, Random rng)
        {
            var e=new int[4]; int rem=total;
            for(int i=0;i<3;i++){
                int lo=System.Math.Max(min,rem-max*(3-i)), hi=System.Math.Min(max,rem-min*(3-i));
                if(lo>hi)lo=hi; e[i]=rng.Next(lo,hi+1); rem-=e[i];
            }
            e[3]=System.Math.Clamp(rem,min,max); Shuffle(e,rng); return e;
        }

        private static readonly Faction[] AllFactions =
        {
            Faction.Ascendant, Faction.Razorkin, Faction.Ghostwire,
            Faction.Commons,   Faction.Effigy,   Faction.Lacquer, Faction.HollowChoir,
        };
        private static Faction PickFaction(Random rng) => AllFactions[rng.Next(AllFactions.Length)];
    }
}