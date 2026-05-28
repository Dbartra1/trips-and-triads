using Xunit;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies StepUpPromoter — the succession system that promotes a Top-Tier
    /// merc to hero when the crew loses their leader (systems.md §7.5).
    ///
    /// Promotion rules:
    ///   1. Highest-total non-hero card is selected from the deck.
    ///   2. Highest edge → A (10).
    ///   3. Lowest edge → capped to 3 if it was above 3; left alone if ≤ 3.
    ///   4. Middle edges are untouched.
    ///   5. Tier → Hero. Level → 10.
    ///   6. DomainType → faction-appropriate default.
    ///   7. AbilityType → None (promoted heroes start without active abilities).
    ///   8. Hero cards in the deck are skipped as candidates.
    ///   9. Returns null if no eligible card exists.
    /// </summary>
    public class StepUpPromoterTests
    {
        // ── Candidate selection ───────────────────────────────────────────────

        [Fact]
        public void Promote_SelectsHighestTotalCard()
        {
            var weak   = CardFactory.Street("Weak",   t:3, r:3, b:3, l:3).Data;
            var strong = CardFactory.Street("Strong", t:7, r:7, b:7, l:7).Data;
            var mid    = CardFactory.Street("Mid",    t:5, r:5, b:5, l:5).Data;

            var deck = new List<CardData> { weak, mid, strong };
            var promoted = StepUpPromoter.Promote(deck);

            Assert.NotNull(promoted);
            Assert.Equal("Strong", promoted.Name);
        }

        [Fact]
        public void Promote_SkipsHeroCards()
        {
            // Hero in the deck should never be selected as a candidate.
            var hero   = CardFactory.SeraphYune().Data;
            var nonHero = CardFactory.Street("Candidate", t:6, r:6, b:6, l:6).Data;

            var deck = new List<CardData> { hero, nonHero };
            var promoted = StepUpPromoter.Promote(deck);

            Assert.NotNull(promoted);
            Assert.Equal("Candidate", promoted.Name);
        }

        [Fact]
        public void Promote_AllHeroes_ReturnsNull()
        {
            var deck = new List<CardData>
            {
                CardFactory.SeraphYune().Data,
                CardFactory.SisterGrin().Data,
            };

            var promoted = StepUpPromoter.Promote(deck);

            Assert.Null(promoted);
        }

        [Fact]
        public void Promote_EmptyDeck_ReturnsNull()
        {
            var promoted = StepUpPromoter.Promote(new List<CardData>());
            Assert.Null(promoted);
        }

        // ── Highest edge becomes A (10) ───────────────────────────────────────

        [Fact]
        public void Promote_HighestEdge_BecomesA()
        {
            // Top is clearly highest
            var card = CardFactory.Street("Card", t:8, r:5, b:4, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(10, card.Top);
        }

        [Fact]
        public void Promote_HighestEdge_Right()
        {
            var card = CardFactory.Street("Card", t:5, r:9, b:4, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(10, card.Right);
        }

        [Fact]
        public void Promote_HighestEdge_Bottom()
        {
            var card = CardFactory.Street("Card", t:5, r:4, b:8, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(10, card.Bottom);
        }

        [Fact]
        public void Promote_HighestEdge_Left()
        {
            var card = CardFactory.Street("Card", t:5, r:4, b:3, l:7).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(10, card.Left);
        }

        // ── Lowest edge capped to 3 ───────────────────────────────────────────

        [Fact]
        public void Promote_LowestEdgeAboveThree_CappedToThree()
        {
            // Lowest is Left=4 — above 3, should be capped
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:4).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(3, card.Left);
        }

        [Fact]
        public void Promote_LowestEdgeAlreadyThree_Unchanged()
        {
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(3, card.Left);
        }

        [Fact]
        public void Promote_LowestEdgeBelowThree_LeftAlone()
        {
            // Lowest is Left=1 — below 3, must not be raised
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:1).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(1, card.Left);
        }

        [Fact]
        public void Promote_LowestEdgeTwo_LeftAlone()
        {
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:2).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(2, card.Left);
        }

        // ── Middle edges untouched ────────────────────────────────────────────

        [Fact]
        public void Promote_MiddleEdges_Unchanged()
        {
            // Top=8 (highest → A), Left=2 (lowest → unchanged ≤3)
            // Middle: Right=6, Bottom=5 — must not change
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:2).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(10, card.Top);    // promoted
            Assert.Equal(6,  card.Right);  // middle — unchanged
            Assert.Equal(5,  card.Bottom); // middle — unchanged
            Assert.Equal(2,  card.Left);   // lowest — unchanged (≤3)
        }

        // ── Tier, Level, DomainType, AbilityType ─────────────────────────────

        [Fact]
        public void Promote_TierBecomesHero()
        {
            var card = CardFactory.Street("Card", t:7, r:5, b:4, l:3).Data;
            Assert.Equal(Tier.Street, card.Tier);

            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(Tier.Hero, card.Tier);
        }

        [Fact]
        public void Promote_LevelBecomesToen()
        {
            var card = CardFactory.Street("Card", t:7, r:5, b:4, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(10, card.Level);
        }

        [Fact]
        public void Promote_AbilityTypeBecomesNone()
        {
            var card = CardFactory.Street("Card", t:7, r:5, b:4, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(AbilityType.None, card.AbilityType);
        }

        [Theory]
        [InlineData(Faction.Ascendant,  DomainType.AegisProtocol)]
        [InlineData(Faction.Razorkin,   DomainType.Killzone)]
        [InlineData(Faction.Ghostwire,  DomainType.LateralGrid)]
        [InlineData(Faction.Commons,    DomainType.Sprawl)]
        [InlineData(Faction.Effigy,     DomainType.AegisProtocol)] // fallback
        [InlineData(Faction.Lacquer,    DomainType.AegisProtocol)] // fallback
        [InlineData(Faction.HollowChoir,DomainType.AegisProtocol)] // fallback
        [InlineData(Faction.None,       DomainType.AegisProtocol)] // fallback
        public void Promote_DomainType_MatchesFaction(Faction faction, DomainType expectedDomain)
        {
            var card = CardFactory.Create("Card")
                .Stats(7, 5, 4, 3)
                .Faction(faction).Tier(Tier.Street)
                .Data();

            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(expectedDomain, card.DomainType);
        }

        // ── PreviewPromotion — read-only, no mutation ─────────────────────────

        [Fact]
        public void PreviewPromotion_DoesNotMutateCard()
        {
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:4).Data;
            var before = (card.Top, card.Right, card.Bottom, card.Left,
                          card.Tier, card.DomainType);

            StepUpPromoter.PreviewPromotion(card);

            Assert.Equal(before.Top,        card.Top);
            Assert.Equal(before.Right,      card.Right);
            Assert.Equal(before.Bottom,     card.Bottom);
            Assert.Equal(before.Left,       card.Left);
            Assert.Equal(before.Tier,       card.Tier);
            Assert.Equal(before.DomainType, card.DomainType);
        }

        [Fact]
        public void PreviewPromotion_ReturnsCorrectProjectedStats()
        {
            // Top=8 (highest → 10), Left=4 (lowest, >3 → 3), Right/Bottom unchanged
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:4).Data;
            var (top, right, bottom, left) = StepUpPromoter.PreviewPromotion(card);

            Assert.Equal(10, top);
            Assert.Equal(6,  right);
            Assert.Equal(5,  bottom);
            Assert.Equal(3,  left);
        }

        // ── PromoteSpecific ────────────────────────────────────────────────────

        [Fact]
        public void PromoteSpecific_PromotesChosenCard_NotHighestTotal()
        {
            var weak   = CardFactory.Street("Weak",   t:3, r:3, b:3, l:3).Data;
            var strong = CardFactory.Street("Strong", t:8, r:8, b:8, l:8).Data;

            // Explicitly promote the weak card
            StepUpPromoter.PromoteSpecific(weak);

            Assert.Equal(Tier.Hero, weak.Tier);
            Assert.Equal(Tier.Street, strong.Tier); // strong unaffected
        }

        [Fact]
        public void PromoteSpecific_HeroCard_ReturnsNull()
        {
            var hero   = CardFactory.SeraphYune().Data;
            var result = StepUpPromoter.PromoteSpecific(hero);

            Assert.Null(result);
            Assert.Equal(Tier.Hero, hero.Tier); // unchanged
        }

        // ── Geometry is lore: stat shape after promotion ──────────────────────

        [Fact]
        public void Promote_LateralCard_HighestSideBecomesA_LowestSoftened()
        {
            // A Ghostwire-style lateral card: strong sides, weak vertical
            // Simulates a real promotion candidate from that faction
            var card = CardFactory.Create("Ghost Merc")
                .Stats(3, 8, 2, 7) // Top=3, Right=8, Bottom=2, Left=7
                .Faction(Faction.Ghostwire).Tier(Tier.TopTier)
                .Data();

            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(10, card.Right);   // highest: 8 → A
            Assert.Equal(7,  card.Left);    // middle: unchanged
            Assert.Equal(3,  card.Top);     // middle: unchanged
            Assert.Equal(2,  card.Bottom);  // lowest: 2 ≤ 3, left alone
            Assert.Equal(DomainType.LateralGrid, card.DomainType);
        }

        [Fact]
        public void Promote_EvenCard_HighestEdgeGetsA_LowestCapped()
        {
            // Mara-style even card: all edges equal
            // When all edges tie, first found (Top, index 0) becomes A
            var card = CardFactory.Create("Commons Merc")
                .Stats(6, 6, 6, 6)
                .Faction(Faction.Commons).Tier(Tier.TopTier)
                .Data();

            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(Tier.Hero, card.Tier);
            Assert.Equal(DomainType.Sprawl, card.DomainType);
            // One edge should be 10, one should be ≤3
            int a    = 0;
            int soft = 0;
            foreach (var v in new[] { card.Top, card.Right, card.Bottom, card.Left })
            {
                if (v == 10) a++;
                if (v <= 3)  soft++;
            }
            Assert.Equal(1, a);    // exactly one A
            Assert.True(soft >= 1); // at least one soft edge
        }
    }
}