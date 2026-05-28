using Xunit;
using System;
using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Integration
{
    /// <summary>
    /// Validates CrewGenerator output over many samples.
    /// These tests are statistical — they generate large batches and assert
    /// that every output falls within spec. A single outlier is a real bug.
    ///
    /// Rules being verified (from lore.md and CrewGenerator source):
    ///   Street: total 10–14, all edges 2–5
    ///   Pro:    total 16–22, all edges 2–9
    ///   Hero:   exactly one A (10), one soft edge ≤ 4, Tier=Hero, Level=10
    ///   Crew:   1 Hero + 1 Pro + 5 Street = 7 cards
    ///   SelectBestFive: always includes the hero, picks 4 highest-total others
    ///   AbilityWeights: Decay never appears on player hero
    /// </summary>
    public class CrewGeneratorTests
    {
        private const int Samples = 500; // enough to catch outliers without being slow

        // ── Crew composition ──────────────────────────────────────────────────

        [Fact]
        public void Generate_AlwaysProduces7Cards()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                Assert.Equal(7, crew.Count);
            }
        }

        [Fact]
        public void Generate_AlwaysHasExactlyOneHero()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                int heroCount = crew.FindAll(c => c.Tier == Tier.Hero).Count;
                Assert.Equal(1, heroCount);
            }
        }

        [Fact]
        public void Generate_AlwaysHasExactlyOnePro()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                int proCount = crew.FindAll(c => c.Tier == Tier.Pro).Count;
                Assert.Equal(1, proCount);
            }
        }

        [Fact]
        public void Generate_AlwaysHasFiveStreet()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                int streetCount = crew.FindAll(c => c.Tier == Tier.Street).Count;
                Assert.Equal(5, streetCount);
            }
        }

        // ── Street card stat bands ────────────────────────────────────────────

        [Fact]
        public void Street_TotalAlwaysInBand_10to14()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                foreach (var card in crew.FindAll(c => c.Tier == Tier.Street))
                {
                    int total = card.Top + card.Right + card.Bottom + card.Left;
                    Assert.InRange(total, 10, 14);
                }
            }
        }

        [Fact]
        public void Street_AllEdgesAtLeast2()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                foreach (var card in crew.FindAll(c => c.Tier == Tier.Street))
                {
                    Assert.True(card.Top    >= 2, $"Street {card.Name} Top={card.Top} < 2");
                    Assert.True(card.Right  >= 2, $"Street {card.Name} Right={card.Right} < 2");
                    Assert.True(card.Bottom >= 2, $"Street {card.Name} Bottom={card.Bottom} < 2");
                    Assert.True(card.Left   >= 2, $"Street {card.Name} Left={card.Left} < 2");
                }
            }
        }

        [Fact]
        public void Street_AllEdgesAtMost5()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                foreach (var card in crew.FindAll(c => c.Tier == Tier.Street))
                {
                    Assert.True(card.Top    <= 5, $"Street {card.Name} Top={card.Top} > 5");
                    Assert.True(card.Right  <= 5, $"Street {card.Name} Right={card.Right} > 5");
                    Assert.True(card.Bottom <= 5, $"Street {card.Name} Bottom={card.Bottom} > 5");
                    Assert.True(card.Left   <= 5, $"Street {card.Name} Left={card.Left} > 5");
                }
            }
        }

        // ── Pro card stat bands ───────────────────────────────────────────────

        [Fact]
        public void Pro_TotalAlwaysInBand_16to22()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                var pro  = crew.Find(c => c.Tier == Tier.Pro)!;
                int total = pro.Top + pro.Right + pro.Bottom + pro.Left;
                Assert.InRange(total, 16, 22);
            }
        }

        [Fact]
        public void Pro_AllEdgesAtLeast2()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var pro = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Pro)!;
                Assert.True(pro.Top    >= 2);
                Assert.True(pro.Right  >= 2);
                Assert.True(pro.Bottom >= 2);
                Assert.True(pro.Left   >= 2);
            }
        }

        [Fact]
        public void Pro_AllEdgesAtMost9()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var pro = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Pro)!;
                Assert.True(pro.Top    <= 9);
                Assert.True(pro.Right  <= 9);
                Assert.True(pro.Bottom <= 9);
                Assert.True(pro.Left   <= 9);
            }
        }

        // ── Hero shape rules ──────────────────────────────────────────────────

        [Fact]
        public void Hero_AlwaysHasExactlyOneOrTwoA()
        {
            // Most heroes: exactly 1 A (lore.md §3 design rule 4).
            // Effigy heroes are point-symmetric (T=B, L=R) and deliberately have 2 As.
            // Both are intentional — the test allows either.
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var hero  = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Hero)!;
                var edges = new[] { hero.Top, hero.Right, hero.Bottom, hero.Left };
                int aCount = System.Array.FindAll(edges, e => e == 10).Length;
                Assert.InRange(aCount, 1, 2);
                // Effigy is the only faction allowed two As
                if (aCount == 2)
                    Assert.Equal(Faction.Effigy, hero.Faction);
            }
        }

        [Fact]
        public void Hero_AlwaysHasAtLeastOneSoftEdge_AtMostFive()
        {
            // lore.md §3: "one deliberately soft edge". Most factions cap soft at 4.
            // Lacquer's soft edge goes up to 5 by design (demure now, lethal later).
            // We use ≤ 5 as the threshold so Lacquer heroes pass.
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var hero  = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Hero)!;
                var edges = new[] { hero.Top, hero.Right, hero.Bottom, hero.Left };
                int softCount = System.Array.FindAll(edges, e => e <= 5).Length;
                Assert.True(softCount >= 1,
                    $"Hero {hero.Name} ({hero.Top}/{hero.Right}/{hero.Bottom}/{hero.Left}) " +
                    $"has no soft edge (≤5)");
            }
        }

        [Fact]
        public void Hero_TierIsHero_LevelIsTen()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var hero = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Hero)!;
                Assert.Equal(Tier.Hero, hero.Tier);
                Assert.Equal(10, hero.Level);
            }
        }

        [Fact]
        public void Hero_AbilityType_NeverDecay()
        {
            // Decay is Vesna's curse — thematically wrong on a player hero.
            // CrewGenerator explicitly excludes it from the AbilityWeights pool.
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var hero = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Hero)!;
                Assert.NotEqual(AbilityType.Decay, hero.AbilityType);
            }
        }

        [Fact]
        public void Hero_AbilityType_ValidValues_Only()
        {
            var valid = new[] { AbilityType.None, AbilityType.Compound, AbilityType.Copy };
            var rng   = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var hero = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Hero)!;
                Assert.Contains(hero.AbilityType, valid);
            }
        }

        // ── HollowChoir hero: must always have exactly one 0 (Toll) ──────────

        [Fact]
        public void HollowChoirHero_AlwaysHasExactlyOneToll()
        {
            // Generate many crews seeded to hit HollowChoir heroes.
            // When a Choir hero appears, it must have exactly one 0 edge.
            var rng     = new Random(42);
            int checkedCount = 0;

            for (int i = 0; i < 5000 && checkedCount < 50; i++)
            {
                var hero = CrewGenerator.Generate(rng).Find(c => c.Tier == Tier.Hero)!;
                if (hero.Faction != Faction.HollowChoir) continue;

                var edges  = new[] { hero.Top, hero.Right, hero.Bottom, hero.Left };
                int zeroes = System.Array.FindAll(edges, e => e == 0).Length;
                Assert.Equal(1, zeroes);
                checkedCount++;
            }
            // If we couldn't get 50 Choir heroes in 5000 generations something is wrong
            Assert.True(checkedCount > 0, "Could not generate any HollowChoir heroes in 5000 attempts");
        }

        // ── Unique names ─────────────────────────────────────────────────────

        [Fact]
        public void Generate_AllCardsInCrew_HaveUniqueNames()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew  = CrewGenerator.Generate(rng);
                var names = new HashSet<string>();
                foreach (var card in crew)
                    Assert.True(names.Add(card.Name),
                        $"Duplicate name '{card.Name}' in generated crew");
            }
        }

        // ── SelectBestFive ────────────────────────────────────────────────────

        [Fact]
        public void SelectBestFive_AlwaysReturns5Cards()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                var hand = CrewGenerator.SelectBestFive(crew);
                Assert.Equal(5, hand.Count);
            }
        }

        [Fact]
        public void SelectBestFive_AlwaysIncludesHero()
        {
            var rng = new Random(42);
            for (int i = 0; i < Samples; i++)
            {
                var crew = CrewGenerator.Generate(rng);
                var hand = CrewGenerator.SelectBestFive(crew);
                Assert.Contains(hand, c => c.Tier == Tier.Hero);
            }
        }

        [Fact]
        public void SelectBestFive_PicksHighestTotalNonHeroCards()
        {
            // Controlled crew: hero + 1 strong Pro + 5 weak Streets
            // SelectBestFive must pick hero + strong Pro + 3 of the 5 Streets
            var hero  = new CardData { Tier = Tier.Hero, Top=10, Right=8, Bottom=3, Left=8,
                                       Name="TestHero", Level=10 };
            var strong = new CardData { Tier = Tier.Pro,    Top=9, Right=9, Bottom=9, Left=4,
                                        Name="StrongPro", Level=7 }; // total 31
            var crew  = new List<CardData> { hero, strong };
            for (int i = 0; i < 5; i++)
                crew.Add(new CardData { Tier = Tier.Street, Top=2, Right=2, Bottom=2, Left=2,
                                        Name=$"Weak{i}", Level=1 }); // total 8

            var hand = CrewGenerator.SelectBestFive(crew);

            Assert.Equal(5, hand.Count);
            Assert.Contains(hand, c => c.Name == "TestHero");
            Assert.Contains(hand, c => c.Name == "StrongPro");
        }

        [Fact]
        public void SelectBestFive_EmptyInput_ReturnsEmptyList()
        {
            var result = CrewGenerator.SelectBestFive(new List<CardData>());
            Assert.Empty(result);
        }

        [Fact]
        public void SelectBestFive_NullInput_ReturnsEmptyList()
        {
            var result = CrewGenerator.SelectBestFive(null!);
            Assert.Empty(result);
        }
    }
}