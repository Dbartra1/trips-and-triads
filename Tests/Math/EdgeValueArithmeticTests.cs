using Xunit;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies that GetValue() correctly assembles the four components that
    /// contribute to an edge's effective value:
    ///   GetValue(dir) = (Override ?? Data.stat) + DomainBonus + BondBonus
    ///
    /// These tests nail each component in isolation, then together, ensuring
    /// no component is dropped, double-counted, or applied to the wrong edge.
    /// </summary>
    public class EdgeValueArithmeticTests
    {
        // ── Component 1: base stat ────────────────────────────────────────────

        [Fact]
        public void GetValue_NoOverrides_ReturnsDataStat()
        {
            var card = CardFactory.Street("Base", t:3, r:5, b:7, l:2, owner:1);

            Assert.Equal(3, card.GetValue(Direction.Top));
            Assert.Equal(5, card.GetValue(Direction.Right));
            Assert.Equal(7, card.GetValue(Direction.Bottom));
            Assert.Equal(2, card.GetValue(Direction.Left));
        }

        [Fact]
        public void GetValue_MaxStat_A_Equals_Ten()
        {
            var card = CardFactory.SeraphYune(owner: 1);
            // Yune: Top=10(A), Right=8, Bottom=3, Left=8
            Assert.Equal(10, card.GetValue(Direction.Top));
        }

        // ── Component 2: override ─────────────────────────────────────────────

        [Fact]
        public void GetValue_WithOverride_UsesOverrideNotData()
        {
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.TopOverride = 9;

            Assert.Equal(9, card.GetValue(Direction.Top));
            // Other directions unaffected
            Assert.Equal(5, card.GetValue(Direction.Right));
            Assert.Equal(5, card.GetValue(Direction.Bottom));
            Assert.Equal(5, card.GetValue(Direction.Left));
        }

        [Fact]
        public void GetValue_OverrideZero_ReturnsZero_NotDataStat()
        {
            // Override of 0 is valid (Vesna fully decayed, Lethe on empty board).
            var card = CardFactory.Street("Card", t:8, r:8, b:8, l:8, owner:1);
            card.TopOverride = 0;

            Assert.Equal(0, card.GetValue(Direction.Top));
        }

        [Fact]
        public void GetValue_OverrideNull_FallsBackToDataStat()
        {
            var card = CardFactory.Street("Card", t:6, r:6, b:6, l:6, owner:1);
            // Explicitly confirm null override falls back
            card.TopOverride = null;

            Assert.Equal(6, card.GetValue(Direction.Top));
        }

        // ── Component 3: domain bonus ─────────────────────────────────────────

        [Fact]
        public void GetValue_DomainBonus_AddsToEffectiveValue()
        {
            var card = CardFactory.Street("Card", t:4, r:4, b:4, l:4, owner:1);
            card.DomainBonusTop    = 2;
            card.DomainBonusRight  = 1;
            card.DomainBonusBottom = 0;
            card.DomainBonusLeft   = 3;

            Assert.Equal(6, card.GetValue(Direction.Top));    // 4+2
            Assert.Equal(5, card.GetValue(Direction.Right));  // 4+1
            Assert.Equal(4, card.GetValue(Direction.Bottom)); // 4+0
            Assert.Equal(7, card.GetValue(Direction.Left));   // 4+3
        }

        [Fact]
        public void GetValue_DomainBonus_DoesNotAffectGetBaseValue()
        {
            // GetBaseValue must return override??data only — no bonuses.
            // This is what Lethe copies and what decay/compound modify.
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.DomainBonusTop = 3;

            Assert.Equal(8, card.GetValue(Direction.Top));     // with bonus
            Assert.Equal(5, card.GetBaseValue(Direction.Top)); // without bonus
        }

        // ── Component 4: bond bonus ───────────────────────────────────────────

        [Fact]
        public void GetValue_BondBonus_AddsToEffectiveValue()
        {
            var card = CardFactory.Street("Card", t:3, r:3, b:3, l:3, owner:1);
            card.BondBonusTop   = 1;
            card.BondBonusLeft  = 2;

            Assert.Equal(4, card.GetValue(Direction.Top));    // 3+1
            Assert.Equal(3, card.GetValue(Direction.Right));  // 3+0
            Assert.Equal(3, card.GetValue(Direction.Bottom)); // 3+0
            Assert.Equal(5, card.GetValue(Direction.Left));   // 3+2
        }

        // ── All components together ───────────────────────────────────────────

        [Fact]
        public void GetValue_AllThreeComponents_SumCorrectly()
        {
            // Base=4, Override=6 (override wins), DomainBonus=2, BondBonus=1
            // Expected: 6 + 2 + 1 = 9
            var card = CardFactory.Street("Card", t:4, r:4, b:4, l:4, owner:1);
            card.TopOverride   = 6;
            card.DomainBonusTop = 2;
            card.BondBonusTop   = 1;

            Assert.Equal(9, card.GetValue(Direction.Top));
            // Base stat (4) is irrelevant when override is set
            Assert.Equal(6, card.GetBaseValue(Direction.Top));
        }

        [Fact]
        public void GetValue_DomainAndBond_BothStack_NeitherDropped()
        {
            // Confirms neither bonus component shadows the other.
            var card = CardFactory.Street("Card", t:3, r:3, b:3, l:3, owner:1);
            card.DomainBonusTop = 2;
            card.BondBonusTop   = 3;

            Assert.Equal(8, card.GetValue(Direction.Top)); // 3+2+3
        }

        // ── Edge isolation: bonuses apply to correct edge only ────────────────

        [Fact]
        public void DomainBonus_AppliesOnlyToAssignedEdge_NotOthers()
        {
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.DomainBonusRight = 4;

            Assert.Equal(5, card.GetValue(Direction.Top));
            Assert.Equal(9, card.GetValue(Direction.Right));   // only Right affected
            Assert.Equal(5, card.GetValue(Direction.Bottom));
            Assert.Equal(5, card.GetValue(Direction.Left));
        }

        // ── Reset methods clear correct fields ────────────────────────────────

        [Fact]
        public void ResetDomainBonuses_ClearsAllDomainBonuses()
        {
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.DomainBonusTop    = 3;
            card.DomainBonusRight  = 3;
            card.DomainBonusBottom = 3;
            card.DomainBonusLeft   = 3;

            card.ResetDomainBonuses();

            Assert.Equal(5, card.GetValue(Direction.Top));
            Assert.Equal(5, card.GetValue(Direction.Right));
            Assert.Equal(5, card.GetValue(Direction.Bottom));
            Assert.Equal(5, card.GetValue(Direction.Left));
        }

        [Fact]
        public void ResetDomainBonuses_DoesNotClearBondBonuses()
        {
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.DomainBonusTop = 2;
            card.BondBonusTop   = 3;

            card.ResetDomainBonuses();

            // Bond bonus survives reset; domain does not
            Assert.Equal(8, card.GetValue(Direction.Top)); // 5+0+3
        }

        [Fact]
        public void ResetBondBonuses_ClearsAllBondBonuses()
        {
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.BondBonusTop   = 2;
            card.BondBonusRight = 2;

            card.ResetBondBonuses();

            Assert.Equal(5, card.GetValue(Direction.Top));
            Assert.Equal(5, card.GetValue(Direction.Right));
        }

        // ── AdjustAllEdges: modifies overrides, not data ──────────────────────

        [Fact]
        public void AdjustAllEdges_PositiveDelta_IncrementsAllEdges()
        {
            var card = CardFactory.Street("Card", t:4, r:3, b:2, l:5, owner:1);
            card.AdjustAllEdges(+2);

            Assert.Equal(6, card.GetBaseValue(Direction.Top));
            Assert.Equal(5, card.GetBaseValue(Direction.Right));
            Assert.Equal(4, card.GetBaseValue(Direction.Bottom));
            Assert.Equal(7, card.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void AdjustAllEdges_NegativeDelta_DecrementsAllEdges()
        {
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.AdjustAllEdges(-2);

            Assert.Equal(3, card.GetBaseValue(Direction.Top));
            Assert.Equal(3, card.GetBaseValue(Direction.Right));
            Assert.Equal(3, card.GetBaseValue(Direction.Bottom));
            Assert.Equal(3, card.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void AdjustAllEdges_NeverGoesNegative_ClampsAtZero()
        {
            var card = CardFactory.Street("Card", t:2, r:2, b:2, l:2, owner:1);
            card.AdjustAllEdges(-5); // would be -3 without clamping

            Assert.Equal(0, card.GetBaseValue(Direction.Top));
            Assert.Equal(0, card.GetBaseValue(Direction.Right));
            Assert.Equal(0, card.GetBaseValue(Direction.Bottom));
            Assert.Equal(0, card.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void AdjustAllEdges_DoesNotModifyDataStat_OnlyOverrides()
        {
            var card = CardFactory.Street("Card", t:5, r:5, b:5, l:5, owner:1);
            card.AdjustAllEdges(+3);

            // Data.Top unchanged
            Assert.Equal(5, card.Data.Top);
            // But GetBaseValue reflects the override
            Assert.Equal(8, card.GetBaseValue(Direction.Top));
        }
    }
}
