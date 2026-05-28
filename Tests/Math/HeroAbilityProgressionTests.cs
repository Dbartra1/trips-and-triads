using Xunit;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies exact per-turn arithmetic for Vesna (decay -1/turn) and
    /// Sumi (compound +1/turn). Tests track values over multiple turns,
    /// confirm floor/ceiling clamping, and verify base-value vs effective-value
    /// distinction is maintained throughout ability progression.
    /// </summary>
    public class HeroAbilityProgressionTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // VESNA — decays -1 all edges per owner turn-end, floors at 0
        // ══════════════════════════════════════════════════════════════════════

        private static (BoardState board, CardInstance vesna) PlaceVesna(int owner = 1)
        {
            var v = CardFactory.Vesna(owner);
            var board = new BoardBuilder().Place(v, row: 1, col: 1).BuildBoard();
            return (board, v);
        }

        [Fact]
        public void Vesna_StartsAt_AAAA()
        {
            var (_, vesna) = PlaceVesna();
            Assert.Equal(10, vesna.GetBaseValue(Direction.Top));
            Assert.Equal(10, vesna.GetBaseValue(Direction.Right));
            Assert.Equal(10, vesna.GetBaseValue(Direction.Bottom));
            Assert.Equal(10, vesna.GetBaseValue(Direction.Left));
        }

        [Theory]
        [InlineData(1, 9)]
        [InlineData(2, 8)]
        [InlineData(3, 7)]
        [InlineData(4, 6)]
        [InlineData(5, 5)]
        [InlineData(9, 1)]
        [InlineData(10, 0)]
        public void Vesna_DecaysExactlyMinusOnePerTurn(int turns, int expectedValue)
        {
            var (board, vesna) = PlaceVesna();
            var ability = new VesnaAbility();

            for (int t = 0; t < turns; t++)
                ability.OnTurnEnd(board, vesna, 1, 1);

            Assert.Equal(expectedValue, vesna.GetBaseValue(Direction.Top));
            Assert.Equal(expectedValue, vesna.GetBaseValue(Direction.Right));
            Assert.Equal(expectedValue, vesna.GetBaseValue(Direction.Bottom));
            Assert.Equal(expectedValue, vesna.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void Vesna_DecayFloorsAtZero_NeverNegative()
        {
            var (board, vesna) = PlaceVesna();
            var ability = new VesnaAbility();

            // Apply 15 turns of decay — 5 beyond the floor
            for (int t = 0; t < 15; t++)
                ability.OnTurnEnd(board, vesna, 1, 1);

            Assert.Equal(0, vesna.GetBaseValue(Direction.Top));
            Assert.Equal(0, vesna.GetBaseValue(Direction.Right));
            Assert.Equal(0, vesna.GetBaseValue(Direction.Bottom));
            Assert.Equal(0, vesna.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void Vesna_Decay_AllFourEdgesDecaySymmetrically()
        {
            var (board, vesna) = PlaceVesna();
            var ability = new VesnaAbility();

            ability.OnTurnEnd(board, vesna, 1, 1);
            ability.OnTurnEnd(board, vesna, 1, 1);
            ability.OnTurnEnd(board, vesna, 1, 1);

            // All four must be exactly 7 after 3 turns
            int expected = 7;
            Assert.Equal(expected, vesna.GetBaseValue(Direction.Top));
            Assert.Equal(expected, vesna.GetBaseValue(Direction.Right));
            Assert.Equal(expected, vesna.GetBaseValue(Direction.Bottom));
            Assert.Equal(expected, vesna.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void Vesna_Decay_ModifiesOverrideNotDataStat()
        {
            var (board, vesna) = PlaceVesna();
            var ability = new VesnaAbility();

            ability.OnTurnEnd(board, vesna, 1, 1);

            // Data stat must stay at 10 — ability only sets Override
            Assert.Equal(10, vesna.Data.Top);
            Assert.Equal(9,  vesna.GetBaseValue(Direction.Top)); // override = 9
        }

        [Fact]
        public void Vesna_DomainBonus_DoesNotInteractWithDecayProgression()
        {
            // Domain bonus is a transient additive on top of base.
            // Decay only modifies the override; domain bonus is reset each turn.
            // After decay + domain: GetBaseValue decayed, GetValue has domain on top.
            var (board, vesna) = PlaceVesna();
            var ability = new VesnaAbility();

            ability.OnTurnEnd(board, vesna, 1, 1); // base → 9
            vesna.DomainBonusTop = 2;              // effective → 11

            Assert.Equal(9,  vesna.GetBaseValue(Direction.Top)); // decay only
            Assert.Equal(11, vesna.GetValue(Direction.Top));     // +domain
        }

        // ══════════════════════════════════════════════════════════════════════
        // SUMI — compounds +1 all edges per owner turn-end, no ceiling cap
        // ══════════════════════════════════════════════════════════════════════

        private static (BoardState board, CardInstance sumi) PlaceSumi(int owner = 1)
        {
            var s = CardFactory.MadameSumi(owner);
            var board = new BoardBuilder().Place(s, row: 1, col: 1).BuildBoard();
            return (board, s);
        }

        [Fact]
        public void Sumi_StartsAt_4444()
        {
            var (_, sumi) = PlaceSumi();
            Assert.Equal(4, sumi.GetBaseValue(Direction.Top));
            Assert.Equal(4, sumi.GetBaseValue(Direction.Right));
            Assert.Equal(4, sumi.GetBaseValue(Direction.Bottom));
            Assert.Equal(4, sumi.GetBaseValue(Direction.Left));
        }

        [Theory]
        [InlineData(1, 5)]
        [InlineData(2, 6)]
        [InlineData(3, 7)]
        [InlineData(4, 8)]
        [InlineData(5, 9)]
        [InlineData(6, 10)]
        [InlineData(7, 11)] // Sumi can exceed A — no ceiling
        public void Sumi_CompoundsExactlyPlusOnePerTurn(int turns, int expectedValue)
        {
            var (board, sumi) = PlaceSumi();
            var ability = new SumiAbility();

            for (int t = 0; t < turns; t++)
                ability.OnTurnEnd(board, sumi, 1, 1);

            Assert.Equal(expectedValue, sumi.GetBaseValue(Direction.Top));
            Assert.Equal(expectedValue, sumi.GetBaseValue(Direction.Right));
            Assert.Equal(expectedValue, sumi.GetBaseValue(Direction.Bottom));
            Assert.Equal(expectedValue, sumi.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void Sumi_Compound_AllFourEdgesGrowSymmetrically()
        {
            var (board, sumi) = PlaceSumi();
            var ability = new SumiAbility();

            ability.OnTurnEnd(board, sumi, 1, 1);
            ability.OnTurnEnd(board, sumi, 1, 1);

            int expected = 6; // 4 + 2 turns
            Assert.Equal(expected, sumi.GetBaseValue(Direction.Top));
            Assert.Equal(expected, sumi.GetBaseValue(Direction.Right));
            Assert.Equal(expected, sumi.GetBaseValue(Direction.Bottom));
            Assert.Equal(expected, sumi.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void Sumi_Compound_ModifiesOverrideNotDataStat()
        {
            var (board, sumi) = PlaceSumi();
            var ability = new SumiAbility();

            ability.OnTurnEnd(board, sumi, 1, 1);

            Assert.Equal(4, sumi.Data.Top);              // data unchanged
            Assert.Equal(5, sumi.GetBaseValue(Direction.Top)); // override = 5
        }

        [Fact]
        public void Sumi_Ledger_AdjacentFriendlyAlsoCompounds()
        {
            // A friendly card adjacent to Sumi should also receive +1 per turn
            // via The Ledger effect in SumiAbility.
            var sumi    = CardFactory.MadameSumi(owner: 1);
            var support = CardFactory.Street("Support", t:5, r:5, b:5, l:5, owner:1);

            var board = new BoardBuilder()
                .Place(sumi,    row: 1, col: 1)
                .Place(support, row: 0, col: 1) // adjacent above
                .BuildBoard();

            var ability = new SumiAbility();
            ability.OnTurnEnd(board, sumi, 1, 1);

            // Support card must also compound +1 via Ledger
            Assert.Equal(6, support.GetBaseValue(Direction.Top));
            Assert.Equal(6, support.GetBaseValue(Direction.Right));
            Assert.Equal(6, support.GetBaseValue(Direction.Bottom));
            Assert.Equal(6, support.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void Sumi_Ledger_EnemyAdjacentDoesNotCompound()
        {
            // Only friendly adjacent cards get the Ledger buff.
            var sumi  = CardFactory.MadameSumi(owner: 1);
            var enemy = CardFactory.Street("Enemy", t:5, r:5, b:5, l:5, owner:2);

            var board = new BoardBuilder()
                .Place(sumi,  row: 1, col: 1)
                .Place(enemy, row: 0, col: 1)
                .BuildBoard();

            var ability = new SumiAbility();
            ability.OnTurnEnd(board, sumi, 1, 1);

            // Enemy unchanged
            Assert.Equal(5, enemy.GetBaseValue(Direction.Top));
        }

        [Fact]
        public void Sumi_Inheritance_HeirAlsoCompounds_WhenBothOnBoard()
        {
            var sumi = CardFactory.MadameSumi(owner: 1);
            var heir = CardFactory.TheHeir(owner: 1);

            // Place in non-adjacent positions to isolate Inheritance from Ledger
            var board = new BoardBuilder()
                .Place(sumi, row: 0, col: 0)
                .Place(heir, row: 2, col: 2)
                .BuildBoard();

            var ability = new SumiAbility();
            ability.OnTurnEnd(board, sumi, 0, 0);

            // Heir: lore 7/7/5/7 → Stats(Top=7, Right=5, Bottom=7, Left=7)
            // After Inheritance: each edge +1
            Assert.Equal(8, heir.GetBaseValue(Direction.Top));
            Assert.Equal(6, heir.GetBaseValue(Direction.Right));
            Assert.Equal(8, heir.GetBaseValue(Direction.Bottom));
            Assert.Equal(8, heir.GetBaseValue(Direction.Left));
        }

        // ══════════════════════════════════════════════════════════════════════
        // LETHE — copies highest-total card on board at placement
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Lethe_OnPlacement_CopiesHighestTotalCard()
        {
            var highCard = CardFactory.Street("High", t:8, r:9, b:7, l:8, owner:2);
            var lowCard  = CardFactory.Street("Low",  t:2, r:3, b:2, l:3, owner:1);
            var lethe    = CardFactory.Lethe(owner: 1);

            var board = new BoardBuilder()
                .Place(highCard, row: 0, col: 0)
                .Place(lowCard,  row: 0, col: 2)
                .BuildBoard();

            board.PlaceCard(lethe, 1, 1);
            lethe.Ability!.OnPlaced(board, lethe, 1, 1);

            // High card total = 8+9+7+8 = 32; Low = 2+3+2+3 = 10
            // Lethe should copy High
            Assert.Equal(8, lethe.GetBaseValue(Direction.Top));
            Assert.Equal(9, lethe.GetBaseValue(Direction.Right));
            Assert.Equal(7, lethe.GetBaseValue(Direction.Bottom));
            Assert.Equal(8, lethe.GetBaseValue(Direction.Left));
        }

        [Fact]
        public void Lethe_CopiesBaseValues_NotBonusInflatedValues()
        {
            // A card with domain bonus looks stronger via GetValue, but Lethe
            // must copy GetBaseValue to satisfy lore: "numbers only, not Domains".
            var target = CardFactory.Street("Target", t:6, r:6, b:6, l:6, owner:2);
            target.DomainBonusTop = 3; // GetValue(Top)=9, GetBaseValue(Top)=6
            var lethe = CardFactory.Lethe(owner: 1);

            var board = new BoardBuilder()
                .Place(target, row: 0, col: 1)
                .BuildBoard();

            board.PlaceCard(lethe, 1, 1);
            lethe.Ability!.OnPlaced(board, lethe, 1, 1);

            // Must copy 6 (base), not 9 (effective)
            Assert.Equal(6, lethe.GetBaseValue(Direction.Top));
        }

        [Fact]
        public void Lethe_EmptyBoard_StaysZeroZeroZeroZero()
        {
            var lethe = CardFactory.Lethe(owner: 1);
            var board = new BoardBuilder().BuildBoard();

            board.PlaceCard(lethe, 1, 1);
            lethe.Ability!.OnPlaced(board, lethe, 1, 1);

            Assert.Equal(0, lethe.GetBaseValue(Direction.Top));
            Assert.Equal(0, lethe.GetBaseValue(Direction.Right));
            Assert.Equal(0, lethe.GetBaseValue(Direction.Bottom));
            Assert.Equal(0, lethe.GetBaseValue(Direction.Left));
        }
    }
}
