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
    /// Promotion rules (Scale-20):
    ///   1. Highest-total non-hero card is selected from the deck.
    ///   2. Highest edge → A (20).
    ///   3. Lowest remaining edge → capped to 6 if it was above 6; left alone if ≤ 6.
    ///   4. Middle edges are untouched.
    ///   5. Tier → Hero. Level → 20 (Scale-20 hero tier value; see StepUpPromoter doc NOTE).
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

        // ── Highest edge becomes A (20, Scale-20) ───────────────────────────────

        [Fact]
        public void Promote_HighestEdge_BecomesA()
        {
            // Top is clearly highest
            var card = CardFactory.Street("Card", t:8, r:5, b:4, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(20, card.Top);
        }

        [Fact]
        public void Promote_HighestEdge_Right()
        {
            var card = CardFactory.Street("Card", t:5, r:9, b:4, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(20, card.Right);
        }

        [Fact]
        public void Promote_HighestEdge_Bottom()
        {
            var card = CardFactory.Street("Card", t:5, r:4, b:8, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(20, card.Bottom);
        }

        [Fact]
        public void Promote_HighestEdge_Left()
        {
            var card = CardFactory.Street("Card", t:5, r:4, b:3, l:7).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(20, card.Left);
        }

        // ── Lowest remaining edge capped to 6 (Scale-20) ────────────────────────

        [Fact]
        public void Promote_LowestEdgeAboveSix_CappedToSix()
        {
            // Top=9 is highest → A. Among the rest (Right=7, Bottom=8, Left=7),
            // Right is the first-found lowest at 7 — above 6, should be capped.
            var card = CardFactory.Street("Card", t:9, r:7, b:8, l:7).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(6, card.Right);
        }

        [Fact]
        public void Promote_LowestEdgeAlreadySix_Unchanged()
        {
            // Top=9 is highest → A. Among the rest (Right=8, Bottom=7, Left=6),
            // Left is the lowest at 6 — already at the cap, left unchanged.
            var card = CardFactory.Street("Card", t:9, r:8, b:7, l:6).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(6, card.Left);
        }

        [Fact]
        public void Promote_LowestEdgeBelowSix_LeftAlone()
        {
            // Lowest is Left=1 — well below the Scale-20 cap of 6, must not be raised
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
            // Top=8 (highest → A=20), Left=2 (lowest → unchanged, ≤6)
            // Middle: Right=6, Bottom=5 — must not change
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:2).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(20, card.Top);    // promoted
            Assert.Equal(6,  card.Right);  // middle — unchanged
            Assert.Equal(5,  card.Bottom); // middle — unchanged
            Assert.Equal(2,  card.Left);   // lowest — unchanged (≤6)
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
        public void Promote_LevelBecomesHeroTierValue()
        {
            // StepUpPromoter sets Level=20 (Scale-20 hero tier value).
            // Note: Level semantics are inconsistent across the codebase —
            // cards.json heroes use Level=10, simulation helpers use Level=20.
            // The assertion here pins the StepUpPromoter's own behaviour.
            // See StepUpPromoter class doc NOTE for the full picture.
            var card = CardFactory.Street("Card", t:7, r:5, b:4, l:3).Data;
            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(20, card.Level);
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
            // Top=8 (highest → 20), Left=4 (lowest, ≤6 → unchanged), Right/Bottom unchanged
            var card = CardFactory.Street("Card", t:8, r:6, b:5, l:4).Data;
            var (top, right, bottom, left) = StepUpPromoter.PreviewPromotion(card);

            Assert.Equal(20, top);
            Assert.Equal(6,  right);
            Assert.Equal(5,  bottom);
            Assert.Equal(4,  left);
        }

        [Fact]
        public void PreviewPromotion_CapsLowestAboveSix()
        {
            // Top=9 (highest → 20). Among the rest (Right=7, Bottom=8, Left=7),
            // Right is the first-found lowest at 7 — above 6, capped to 6.
            var card = CardFactory.Street("Card", t:9, r:7, b:8, l:7).Data;
            var (top, right, bottom, left) = StepUpPromoter.PreviewPromotion(card);

            Assert.Equal(20, top);
            Assert.Equal(6,  right);
            Assert.Equal(8,  bottom);
            Assert.Equal(7,  left);
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

            Assert.Equal(20, card.Right);   // highest: 8 → A (Scale-20)
            Assert.Equal(7,  card.Left);    // middle: unchanged
            Assert.Equal(3,  card.Top);     // middle: unchanged
            Assert.Equal(2,  card.Bottom);  // lowest: 2 ≤ 6, left alone
            Assert.Equal(DomainType.LateralGrid, card.DomainType);
        }

        [Fact]
        public void Promote_EvenCard_HighestEdgeGetsA_LowestCapped()
        {
            // Mara Kane's actual Scale-20 stat line (lore.md §7: 6/6/6/6 ×2).
            // When all edges tie, first found (Top, index 0) becomes A; the
            // next-lowest found among the rest (12 > 6) is capped to 6.
            var card = CardFactory.Create("Commons Merc")
                .Stats(12, 12, 12, 12)
                .Faction(Faction.Commons).Tier(Tier.TopTier)
                .Data();

            StepUpPromoter.Promote(new List<CardData> { card });

            Assert.Equal(Tier.Hero, card.Tier);
            Assert.Equal(DomainType.Sprawl, card.DomainType);
            // One edge should be 20 (A), one should be capped to ≤6
            int a    = 0;
            int soft = 0;
            foreach (var v in new[] { card.Top, card.Right, card.Bottom, card.Left })
            {
                if (v == 20) a++;
                if (v <= 6)  soft++;
            }
            Assert.Equal(1, a);    // exactly one A
            Assert.True(soft >= 1); // at least one soft edge, capped to 6
        }
    }
}