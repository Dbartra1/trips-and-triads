using Xunit;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies the exact numerical and behavioral output of every bond.
    /// Each bond is a named relationship with backstory — these tests confirm
    /// the mechanic matches the lore intent precisely.
    ///
    /// Bond roster (lore.md §9):
    ///   The Rivalry         — Yune ↔ Grin: RivalryActive on both
    ///   The Last Crew       — Riven ↔ Mara: +1 all sides each
    ///   Maker's Mark        — Yune ↔ Cassia Vane: Yune Bottom +2
    ///   The Inheritance     — Sumi ↔ The Heir: Heir compounds alongside Sumi
    ///   The Understudy      — Lethe ↔ her copied hero: both highest edges → 5
    ///   Contamination       — Vesna ↔ adjacent non-Choir enemies: lowest edge -1
    ///   The Listener        — Riven ↔ Vesna: Riven.BlockChoir = true
    /// </summary>
    public class BondTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // THE RIVALRY — Yune ↔ Grin: RivalryActive on both when both on board
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Rivalry_BothOnBoard_RivalryActiveTrueOnBoth()
        {
            var yune = CardFactory.SeraphYune(owner: 1);
            var grin = CardFactory.SisterGrin(owner: 2);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 0)
                .Place(grin, row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.True(yune.RivalryActive);
            Assert.True(grin.RivalryActive);
        }

        [Fact]
        public void Rivalry_YuneAlone_RivalryActiveFalse()
        {
            var yune = CardFactory.SeraphYune(owner: 1);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 0)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.False(yune.RivalryActive);
        }

        [Fact]
        public void Rivalry_GrinAlone_RivalryActiveFalse()
        {
            var grin = CardFactory.SisterGrin(owner: 1);

            var board = new BoardBuilder()
                .Place(grin, row: 0, col: 0)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.False(grin.RivalryActive);
        }

        [Fact]
        public void Rivalry_DoesNotGrantStatBonuses_OnlyFlag()
        {
            // The Rivalry only sets RivalryActive — no BondBonus numbers.
            var yune = CardFactory.SeraphYune(owner: 1);
            var grin = CardFactory.SisterGrin(owner: 2);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 0)
                .Place(grin, row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.Equal(0, yune.BondBonusTop);
            Assert.Equal(0, yune.BondBonusBottom);
            Assert.Equal(0, grin.BondBonusTop);
        }

        // ══════════════════════════════════════════════════════════════════════
        // THE LAST CREW — Riven ↔ Mara Kane: +1 all sides each
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void LastCrew_BothOnBoard_EachGetsPlusOneAllSides()
        {
            var riven = CardFactory.Riven(owner: 1);
            var mara  = CardFactory.MaraKane(owner: 1);

            var board = new BoardBuilder()
                .Place(riven, row: 0, col: 0)
                .Place(mara,  row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            // Riven: lore 3/9/9/2 → Stats(Top=3, Right=9, Bottom=2, Left=9)
            // After bond: effective +1 all sides
            Assert.Equal(1, riven.BondBonusTop);
            Assert.Equal(1, riven.BondBonusRight);
            Assert.Equal(1, riven.BondBonusBottom);
            Assert.Equal(1, riven.BondBonusLeft);

            // Mara: 6/6/6/6 → +1 all sides
            Assert.Equal(1, mara.BondBonusTop);
            Assert.Equal(1, mara.BondBonusRight);
            Assert.Equal(1, mara.BondBonusBottom);
            Assert.Equal(1, mara.BondBonusLeft);
        }

        [Fact]
        public void LastCrew_RivenAlone_NoBonuses()
        {
            var riven = CardFactory.Riven(owner: 1);

            var board = new BoardBuilder()
                .Place(riven, row: 0, col: 0)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.Equal(0, riven.BondBonusTop);
            Assert.Equal(0, riven.BondBonusLeft);
        }

        [Fact]
        public void LastCrew_MaraAlone_NoBonuses()
        {
            var mara = CardFactory.MaraKane(owner: 1);

            var board = new BoardBuilder()
                .Place(mara, row: 0, col: 0)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.Equal(0, mara.BondBonusTop);
        }

        [Fact]
        public void LastCrew_DifferentOwners_BondStillFires()
        {
            // The bond is a named relationship, not a faction buff.
            // It fires regardless of which player controls each card.
            var riven = CardFactory.Riven(owner: 1);
            var mara  = CardFactory.MaraKane(owner: 2);

            var board = new BoardBuilder()
                .Place(riven, row: 0, col: 0)
                .Place(mara,  row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.Equal(1, riven.BondBonusTop);
            Assert.Equal(1, mara.BondBonusTop);
        }

        [Fact]
        public void LastCrew_BondReset_NoBonusDoubleStack()
        {
            // Apply twice — bonus must still be +1 not +2.
            var riven = CardFactory.Riven(owner: 1);
            var mara  = CardFactory.MaraKane(owner: 1);

            var board = new BoardBuilder()
                .Place(riven, row: 0, col: 0)
                .Place(mara,  row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);
            BondResolver.Apply(board);

            Assert.Equal(1, riven.BondBonusTop); // reset + reapply = 1, not 2
        }

        // ══════════════════════════════════════════════════════════════════════
        // MAKER'S MARK — Yune ↔ Cassia Vane: Yune's Bottom gets +2
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void MakersMark_BothOnBoard_YuneBottomPlusTwo()
        {
            var yune = CardFactory.SeraphYune(owner: 1);
            var vane = CardFactory.CassiaVane(owner: 1);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 0)
                .Place(vane, row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            // Yune Bottom base = 3; +2 bond → effective 5
            Assert.Equal(2, yune.BondBonusBottom);
            Assert.Equal(5, yune.GetValue(Direction.Bottom));
        }

        [Fact]
        public void MakersMark_OnlyBuffsBottom_NotOtherEdges()
        {
            var yune = CardFactory.SeraphYune(owner: 1);
            var vane = CardFactory.CassiaVane(owner: 1);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 0)
                .Place(vane, row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.Equal(0, yune.BondBonusTop);
            Assert.Equal(0, yune.BondBonusRight);
            Assert.Equal(0, yune.BondBonusLeft);
            Assert.Equal(2, yune.BondBonusBottom); // only Bottom
        }

        [Fact]
        public void MakersMark_VaneAlone_NoBonusToYune()
        {
            // Vane on board without Yune — no bond fires.
            var vane = CardFactory.CassiaVane(owner: 1);

            var board = new BoardBuilder()
                .Place(vane, row: 0, col: 0)
                .BuildBoard();

            BondResolver.Apply(board);
            // Nothing to assert on Yune since she's not on the board.
            // Just confirm no exception and Vane has no bond bonuses herself.
            Assert.Equal(0, vane.BondBonusBottom);
        }

        // ══════════════════════════════════════════════════════════════════════
        // THE UNDERSTUDY — Lethe ↔ copied hero: both highest edges clamped to 5
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Understudy_LetheAdjacentToCopiedHero_BothHighestEdgesClamped()
        {
            // Lethe copies Yune (Top=10) and is placed adjacent to Yune.
            // Both highest edges must be clamped to 5.
            var yune  = CardFactory.SeraphYune(owner: 1);
            var lethe = CardFactory.Lethe(owner: 2);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 1)
                .BuildBoard();

            // Place Lethe and trigger her copy ability
            board.PlaceCard(lethe, 1, 1);
            lethe.Ability!.OnPlaced(board, lethe, 1, 1);
            // Lethe now has Yune's base stats: Top=10, Right=8, Bottom=3, Left=8

            BondResolver.Apply(board);

            // Yune's highest edge (Top=10) clamped to 5
            Assert.Equal(5, yune.GetBaseValue(Direction.Top));
            // Lethe's highest edge (Top=10, copied from Yune) clamped to 5
            Assert.Equal(5, lethe.GetBaseValue(Direction.Top));
        }

        [Fact]
        public void Understudy_LetheNotAdjacent_NoClamping()
        {
            var yune  = CardFactory.SeraphYune(owner: 1);
            var lethe = CardFactory.Lethe(owner: 2);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 0)
                .BuildBoard();

            // Place Lethe far from Yune, trigger copy
            board.PlaceCard(lethe, 2, 2);
            lethe.Ability!.OnPlaced(board, lethe, 2, 2);

            BondResolver.Apply(board);

            // Not adjacent — no clamping
            Assert.Equal(10, yune.GetBaseValue(Direction.Top));
        }

        [Fact]
        public void Understudy_LetheNotModified_NoBondFires()
        {
            // Lethe with no copy (stays 0/0/0/0) — IsModified=false, bond skipped.
            // IMPORTANT: use board.PlaceCard directly, not BoardBuilder.Place.
            // BoardBuilder auto-calls OnPlaced which would trigger the copy.
            var yune  = CardFactory.SeraphYune(owner: 1);
            var lethe = CardFactory.Lethe(owner: 2);

            var board = new BoardBuilder()
                .Place(yune, row: 0, col: 1)
                .BuildBoard();

            // Place Lethe without triggering OnPlaced — she stays 0/0/0/0, IsModified=false
            board.PlaceCard(lethe, 1, 1);

            BondResolver.Apply(board);

            // IsModified=false → ApplyLetheBonds returns early → Yune unchanged
            Assert.Equal(10, yune.GetBaseValue(Direction.Top));
        }

        // ══════════════════════════════════════════════════════════════════════
        // THE LISTENER — Riven ↔ Vesna: Riven.BlockChoir = true
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Listener_BothOnBoard_RivenBlockChoirTrue()
        {
            var riven = CardFactory.Riven(owner: 1);
            var vesna = CardFactory.Vesna(owner: 2);

            var board = new BoardBuilder()
                .Place(riven, row: 0, col: 0)
                .Place(vesna, row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.True(riven.BlockChoir);
        }

        [Fact]
        public void Listener_RivenAlone_BlockChoirFalse()
        {
            var riven = CardFactory.Riven(owner: 1);

            var board = new BoardBuilder()
                .Place(riven, row: 0, col: 0)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.False(riven.BlockChoir);
        }

        [Fact]
        public void Listener_VesnaAlone_NoBlockChoir()
        {
            // Vesna alone — nobody to set BlockChoir on.
            var vesna = CardFactory.Vesna(owner: 1);

            var board = new BoardBuilder()
                .Place(vesna, row: 0, col: 0)
                .BuildBoard();

            BondResolver.Apply(board); // should not throw
            Assert.Equal(0, board.GetScore(2)); // sanity
        }

        [Fact]
        public void Listener_DoesNotBlockNonChoirCaptures()
        {
            // BlockChoir=true only blocks HollowChoir faction cards.
            // Riven can still capture non-Choir enemies normally.
            var riven = CardFactory.Riven(owner: 1);
            var vesna = CardFactory.Vesna(owner: 2);
            var enemy = CardFactory.Street("NonChoir", t:1, r:1, b:1, l:1, owner:2);

            var board = new BoardBuilder()
                .Place(vesna, row: 2, col: 2)
                .Place(enemy, row: 0, col: 1)
                .BuildBoard();

            board.PlaceCard(riven, 1, 1);
            BondResolver.Apply(board);

            var resolver = new CaptureResolver(MatchConfig.BaseRules());
            var captures = resolver.Resolve(board, 1, 1);

            // Riven Top=3 > NonChoir Bottom=1 → capture (not blocked)
            Assert.Contains((0, 1), captures);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CONTAMINATION — Vesna ↔ adjacent non-Choir: lowest edge -1 (disabled)
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Contamination_DisabledByDefault_NoBonusApplied()
        {
            BondResolver.ContaminationEnabled = false;

            var vesna = CardFactory.Vesna(owner: 1);
            var enemy = CardFactory.Street("Target", t:3, r:5, b:6, l:4, owner:2);

            var board = new BoardBuilder()
                .Place(vesna, row: 1, col: 1)
                .Place(enemy, row: 0, col: 1)
                .BuildBoard();

            BondResolver.Apply(board);

            // Disabled — lowest edge (Top=3) must not be reduced
            Assert.Equal(3, enemy.GetBaseValue(Direction.Top));
            Assert.False(enemy.IsContaminated);
        }

        [Fact]
        public void Contamination_WhenEnabled_ReducesLowestEdgeByOne()
        {
            BondResolver.ContaminationEnabled = true;
            try
            {
                var vesna = CardFactory.Vesna(owner: 1);
                var enemy = CardFactory.Street("Target", t:3, r:5, b:6, l:4, owner:2);
                // Lowest edge: Top=3

                var board = new BoardBuilder()
                    .Place(vesna, row: 1, col: 1)
                    .Place(enemy, row: 0, col: 1)
                    .BuildBoard();

                BondResolver.Apply(board);

                Assert.Equal(2, enemy.GetBaseValue(Direction.Top)); // 3-1
                Assert.True(enemy.IsContaminated);
            }
            finally
            {
                BondResolver.ContaminationEnabled = false; // always restore
            }
        }

        [Fact]
        public void Contamination_WhenEnabled_HollowChoirImmune()
        {
            BondResolver.ContaminationEnabled = true;
            try
            {
                var vesna     = CardFactory.Vesna(owner: 1);
                var choirCard = CardFactory.Create("Choir")
                    .Stats(3, 5, 6, 4).Faction(Faction.HollowChoir).Tier(Tier.Street)
                    .Build(ownerId: 2);

                var board = new BoardBuilder()
                    .Place(vesna,     row: 1, col: 1)
                    .Place(choirCard, row: 0, col: 1)
                    .BuildBoard();

                BondResolver.Apply(board);

                // Choir card is immune to Contamination
                Assert.Equal(3, choirCard.GetBaseValue(Direction.Top));
                Assert.False(choirCard.IsContaminated);
            }
            finally
            {
                BondResolver.ContaminationEnabled = false;
            }
        }

        [Fact]
        public void Contamination_WhenEnabled_FriendlyAdjacentNotAffected()
        {
            BondResolver.ContaminationEnabled = true;
            try
            {
                var vesna    = CardFactory.Vesna(owner: 1);
                var friendly = CardFactory.Street("Friend", t:3, r:5, b:6, l:4, owner:1);

                var board = new BoardBuilder()
                    .Place(vesna,    row: 1, col: 1)
                    .Place(friendly, row: 0, col: 1)
                    .BuildBoard();

                BondResolver.Apply(board);

                // Friendly not contaminated
                Assert.Equal(3, friendly.GetBaseValue(Direction.Top));
                Assert.False(friendly.IsContaminated);
            }
            finally
            {
                BondResolver.ContaminationEnabled = false;
            }
        }

        [Fact]
        public void Contamination_AlreadyContaminated_NotAppliedAgain()
        {
            BondResolver.ContaminationEnabled = true;
            try
            {
                var vesna = CardFactory.Vesna(owner: 1);
                var enemy = CardFactory.Street("Target", t:3, r:5, b:6, l:4, owner:2);

                var board = new BoardBuilder()
                    .Place(vesna, row: 1, col: 1)
                    .Place(enemy, row: 0, col: 1)
                    .BuildBoard();

                BondResolver.Apply(board); // first apply: Top 3→2
                BondResolver.Apply(board); // second apply: IsContaminated guards against re-apply

                Assert.Equal(2, enemy.GetBaseValue(Direction.Top)); // still 2, not 1
            }
            finally
            {
                BondResolver.ContaminationEnabled = false;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // MULTIPLE BONDS — several bonds active simultaneously
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void MultipleBonds_RivalryAndLastCrew_BothFireIndependently()
        {
            // All four heroes on the board: Yune, Grin, Riven, Mara.
            // Rivalry fires on Yune+Grin. Last Crew fires on Riven+Mara.
            var yune  = CardFactory.SeraphYune(owner: 1);
            var grin  = CardFactory.SisterGrin(owner: 2);
            var riven = CardFactory.Riven(owner: 1);
            var mara  = CardFactory.MaraKane(owner: 1);

            var board = new BoardBuilder()
                .Place(yune,  row: 0, col: 0)
                .Place(grin,  row: 0, col: 2)
                .Place(riven, row: 2, col: 0)
                .Place(mara,  row: 2, col: 2)
                .BuildBoard();

            BondResolver.Apply(board);

            Assert.True(yune.RivalryActive);
            Assert.True(grin.RivalryActive);
            Assert.Equal(1, riven.BondBonusTop);
            Assert.Equal(1, mara.BondBonusTop);
        }
    }
}