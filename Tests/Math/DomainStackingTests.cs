using Xunit;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies the exact numerical output of every Domain:
    ///   - AegisProtocol: +1 all sides to adjacent friendlies
    ///   - Killzone: +2 to the two LOWEST edges of adjacent friendlies
    ///   - LateralGrid: +2 Left and Right only
    ///   - Sprawl: +1 to adjacent Commons cards; Mara +1/adjacent friendly
    ///
    /// Also tests that two Domain heroes stack correctly and that domains
    ///   - apply only to friendlies
    ///   - require adjacency
    ///   - reset cleanly between DomainResolver.Apply calls
    /// </summary>
    public class DomainStackingTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // AEGIS PROTOCOL (Yune) — +1 all sides to adjacent friendlies
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Aegis_AdjacentFriendly_GetsExactlyPlusOneAllSides()
        {
            var yune    = CardFactory.SeraphYune(owner: 1);
            var support = CardFactory.Street("Support", t:4, r:5, b:3, l:6, owner:1);

            var board = new BoardBuilder()
                .Place(yune,    row: 1, col: 1)
                .Place(support, row: 0, col: 1) // adjacent above
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(5, support.GetValue(Direction.Top));    // 4+1
            Assert.Equal(6, support.GetValue(Direction.Right));  // 5+1
            Assert.Equal(4, support.GetValue(Direction.Bottom)); // 3+1
            Assert.Equal(7, support.GetValue(Direction.Left));   // 6+1
        }

        [Fact]
        public void Aegis_AllFourAdjacentFriendlies_EachGetPlusOne()
        {
            var yune = CardFactory.SeraphYune(owner: 1);
            var n    = CardFactory.Street("N", t:3, r:3, b:3, l:3, owner:1);
            var s    = CardFactory.Street("S", t:3, r:3, b:3, l:3, owner:1);
            var e    = CardFactory.Street("E", t:3, r:3, b:3, l:3, owner:1);
            var w    = CardFactory.Street("W", t:3, r:3, b:3, l:3, owner:1);

            var board = new BoardBuilder()
                .Place(yune, row:1, col:1)
                .Place(n,    row:0, col:1)
                .Place(s,    row:2, col:1)
                .Place(e,    row:1, col:2)
                .Place(w,    row:1, col:0)
                .BuildBoard();

            DomainResolver.Apply(board);

            foreach (var card in new[] { n, s, e, w })
            {
                Assert.Equal(4, card.GetValue(Direction.Top));
                Assert.Equal(4, card.GetValue(Direction.Right));
                Assert.Equal(4, card.GetValue(Direction.Bottom));
                Assert.Equal(4, card.GetValue(Direction.Left));
            }
        }

        [Fact]
        public void Aegis_DoesNotBuffEnemy()
        {
            var yune  = CardFactory.SeraphYune(owner: 1);
            var enemy = CardFactory.Street("Enemy", t:4, r:4, b:4, l:4, owner:2);

            var board = new BoardBuilder()
                .Place(yune,  row: 1, col: 1)
                .Place(enemy, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(4, enemy.GetValue(Direction.Top)); // unchanged
        }

        [Fact]
        public void Aegis_DoesNotBuffNonAdjacentFriendly()
        {
            var yune   = CardFactory.SeraphYune(owner: 1);
            var remote = CardFactory.Street("Remote", t:4, r:4, b:4, l:4, owner:1);

            var board = new BoardBuilder()
                .Place(yune,   row: 0, col: 0)
                .Place(remote, row: 2, col: 2) // diagonal — not adjacent
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(4, remote.GetValue(Direction.Top)); // unchanged
        }

        // ══════════════════════════════════════════════════════════════════════
        // KILLZONE (Grin) — +2 to the two LOWEST edges of adjacent friendlies
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Killzone_BuffsExactlyTwoLowestEdges_PlusTwoEach()
        {
            var grin    = CardFactory.SisterGrin(owner: 1);
            // Card with clearly distinct edge values so lowest two are unambiguous
            var support = CardFactory.Street("Support", t:8, r:6, b:3, l:1, owner:1);
            // Lowest: Left=1, Bottom=3. Those two get +2.

            var board = new BoardBuilder()
                .Place(grin,    row: 1, col: 1)
                .Place(support, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(8, support.GetValue(Direction.Top));    // highest — unchanged
            Assert.Equal(6, support.GetValue(Direction.Right));  // 3rd — unchanged
            Assert.Equal(5, support.GetValue(Direction.Bottom)); // 2nd lowest: 3+2
            Assert.Equal(3, support.GetValue(Direction.Left));   // lowest: 1+2
        }

        [Fact]
        public void Killzone_WithTiedLowest_BothTiedEdgesGetBuff()
        {
            // Two edges tied at the lowest value — both should receive +2.
            var grin    = CardFactory.SisterGrin(owner: 1);
            var support = CardFactory.Street("Support", t:8, r:8, b:2, l:2, owner:1);

            var board = new BoardBuilder()
                .Place(grin,    row: 1, col: 1)
                .Place(support, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(8, support.GetValue(Direction.Top));    // unchanged
            Assert.Equal(8, support.GetValue(Direction.Right));  // unchanged
            Assert.Equal(4, support.GetValue(Direction.Bottom)); // 2+2
            Assert.Equal(4, support.GetValue(Direction.Left));   // 2+2
        }

        // ══════════════════════════════════════════════════════════════════════
        // LATERAL GRID (Riven) — +2 Left and Right only
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void LateralGrid_BuffsOnlyLeftAndRight_PlusTwoEach()
        {
            var riven   = CardFactory.Riven(owner: 1);
            var support = CardFactory.Street("Support", t:5, r:4, b:5, l:3, owner:1);

            var board = new BoardBuilder()
                .Place(riven,   row: 1, col: 1)
                .Place(support, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(5, support.GetValue(Direction.Top));    // unchanged
            Assert.Equal(6, support.GetValue(Direction.Right));  // 4+2
            Assert.Equal(5, support.GetValue(Direction.Bottom)); // unchanged
            Assert.Equal(5, support.GetValue(Direction.Left));   // 3+2
        }

        [Fact]
        public void LateralGrid_TopAndBottom_StrictlyUnchanged()
        {
            var riven   = CardFactory.Riven(owner: 1);
            var support = CardFactory.Street("Support", t:7, r:3, b:7, l:3, owner:1);

            var board = new BoardBuilder()
                .Place(riven,   row: 1, col: 1)
                .Place(support, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(7, support.GetValue(Direction.Top));
            Assert.Equal(7, support.GetValue(Direction.Bottom));
        }

        // ══════════════════════════════════════════════════════════════════════
        // SPRAWL (Mara) — +1 to adjacent Commons; Mara +1/adjacent friendly
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Sprawl_AdjacentCommonsFriendly_GetsPlusOneAllSides()
        {
            var mara    = CardFactory.MaraKane(owner: 1);
            var commons = CardFactory.Create("Commons Card")
                .Stats(4, 4, 4, 4).Faction(Faction.Commons).Tier(Tier.Street)
                .Build(ownerId: 1);

            var board = new BoardBuilder()
                .Place(mara,    row: 1, col: 1)
                .Place(commons, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(5, commons.GetValue(Direction.Top));
            Assert.Equal(5, commons.GetValue(Direction.Right));
            Assert.Equal(5, commons.GetValue(Direction.Bottom));
            Assert.Equal(5, commons.GetValue(Direction.Left));
        }

        [Fact]
        public void Sprawl_NonCommonsFriendly_DoesNotGetBuff()
        {
            var mara       = CardFactory.MaraKane(owner: 1);
            var nonCommons = CardFactory.Street("NonCommons", t:4, r:4, b:4, l:4, owner:1);
            // Street cards have Faction.None — not Commons

            var board = new BoardBuilder()
                .Place(mara,       row: 1, col: 1)
                .Place(nonCommons, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            Assert.Equal(4, nonCommons.GetValue(Direction.Top)); // unchanged
        }

        [Theory]
        [InlineData(1, 7)] // 1 adjacent friendly → Mara 6+1=7
        [InlineData(2, 8)] // 2 adjacent friendlies → Mara 6+2=8
        [InlineData(3, 9)] // 3 adjacent friendlies → Mara 6+3=9
        [InlineData(4, 10)]// 4 adjacent friendlies → Mara 6+4=10
        public void Sprawl_Mara_GrowsWithAdjacentFriendlyCount(int friendlyCount, int expectedMaraValue)
        {
            var mara = CardFactory.MaraKane(owner: 1);
            var builder = new BoardBuilder().Place(mara, row: 1, col: 1);

            var positions = new[] { (0,1), (1,2), (2,1), (1,0) };
            for (int i = 0; i < friendlyCount; i++)
            {
                var f = CardFactory.Street($"F{i}", t:3, r:3, b:3, l:3, owner:1);
                builder.Place(f, positions[i].Item1, positions[i].Item2);
            }

            var board = builder.BuildBoard();
            DomainResolver.Apply(board);

            Assert.Equal(expectedMaraValue, mara.GetValue(Direction.Top));
            Assert.Equal(expectedMaraValue, mara.GetValue(Direction.Right));
            Assert.Equal(expectedMaraValue, mara.GetValue(Direction.Bottom));
            Assert.Equal(expectedMaraValue, mara.GetValue(Direction.Left));
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOMAIN RESET — bonuses clear between Apply calls
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void DomainResolver_Apply_ResetsBeforeReapplying()
        {
            // Apply twice — bonus should be +1, not +2.
            var yune    = CardFactory.SeraphYune(owner: 1);
            var support = CardFactory.Street("Support", t:5, r:5, b:5, l:5, owner:1);

            var board = new BoardBuilder()
                .Place(yune,    row: 1, col: 1)
                .Place(support, row: 0, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);
            DomainResolver.Apply(board); // second apply must reset first

            Assert.Equal(6, support.GetValue(Direction.Top)); // 5+1, not 5+2
        }

        // ══════════════════════════════════════════════════════════════════════
        // DOMAIN STACKING — two heroes on the same board
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void TwoHeroes_BothDomains_StackOnSharedAdjacentCard()
        {
            // Yune at (0,1) and Riven at (2,1) — both adjacent to center (1,1).
            // Friendly card at (1,1) should receive Aegis (+1 all) AND LateralGrid (+2 L/R).
            var yune   = CardFactory.SeraphYune(owner: 1);
            var riven  = CardFactory.Riven(owner: 1);
            var shared = CardFactory.Street("Shared", t:4, r:4, b:4, l:4, owner:1);

            var board = new BoardBuilder()
                .Place(yune,   row: 0, col: 1)
                .Place(riven,  row: 2, col: 1)
                .Place(shared, row: 1, col: 1)
                .BuildBoard();

            DomainResolver.Apply(board);

            // Aegis +1 all + LateralGrid +2 L/R
            Assert.Equal(5, shared.GetValue(Direction.Top));    // 4+1 (Aegis only)
            Assert.Equal(7, shared.GetValue(Direction.Right));  // 4+1+2 (Aegis+Lateral)
            Assert.Equal(5, shared.GetValue(Direction.Bottom)); // 4+1 (Aegis only)
            Assert.Equal(7, shared.GetValue(Direction.Left));   // 4+1+2 (Aegis+Lateral)
        }
    }
}
