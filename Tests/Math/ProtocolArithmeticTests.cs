using Xunit;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies the exact arithmetic of each protocol:
    ///
    ///   Handshake: fires when attacker_edge == defender_edge on ≥2 contacts
    ///   The Tally:  fires when (attacker + defender) sums match on ≥2 contacts
    ///   Wall Signature: board edge counts as 10 for sum matching
    ///
    /// Tests cover edge cases: off-by-one on equality, sum overflow,
    /// contacts already in alreadyCaptured excluded from protocol pool,
    /// and Wall Signature's interaction with Handshake.
    /// </summary>
    public class ProtocolArithmeticTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // HANDSHAKE — equality is exact, off-by-one does not trigger
        // ══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(5, 5, 5, 5, true)]  // both ties → Handshake fires
        [InlineData(5, 5, 5, 4, false)] // one tie, one not → does not fire
        [InlineData(5, 6, 5, 4, false)] // no ties → does not fire
        [InlineData(5, 5, 5, 6, false)] // tie on north, but south edge higher → south not a tie
        public void Handshake_TwoTieRequirement(
            int attackTop, int northBottom,
            int attackRight, int eastLeft,
            bool expectFire)
        {
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:northBottom, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:eastLeft, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:attackTop, r:attackRight, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            if (expectFire)
            {
                Assert.Contains((0, 1), captures);
                Assert.Contains((1, 2), captures);
            }
            else
            {
                // With no Handshake, neither tie should produce an extra capture
                // (equal edges don't base-capture either)
                Assert.DoesNotContain((0, 1), captures);
            }
        }

        [Fact]
        public void Handshake_ExactEquality_OffByOneDoesNotTrigger()
        {
            // Attacker Top=5, North Bottom=6: 5<6 — miss, no base capture
            // Attacker Right=5, East Left=6:  5<6 — miss, no base capture
            // Neither edge is equal, neither wins → nothing captured at all.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:6, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:6, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:5, r:5, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);
            Assert.Empty(captures);
        }

        [Fact]
        public void Handshake_AlreadyCapturedCard_ExcludedFromTieCount()
        {
            // North is base-captured before Handshake runs.
            // Only one tie contact left (South) → Handshake must not fire.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            // North: Bottom=3 → attacker Top=5 wins by base (5>3); North goes to alreadyCaptured
            // South: Top=5 → attacker Bottom=5 ties → would be second contact IF north were available
            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:3, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("S", t:5, r:1, b:1, l:1, owner:2), row:2, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:5, r:1, b:5, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            // North captured by base. South tied but only 1 Handshake contact → no Handshake.
            Assert.Contains((0, 1), captures);  // base capture
            Assert.DoesNotContain((2, 1), captures); // no Handshake
        }

        // ══════════════════════════════════════════════════════════════════════
        // THE TALLY — sum equality, off-by-one, base-capture exclusion
        // ══════════════════════════════════════════════════════════════════════

        [Theory]
        [InlineData(4, 6, 3, 7, true)]  // 4+6=10, 3+7=10 → match
        [InlineData(4, 6, 3, 8, false)] // 4+6=10, 3+8=11 → no match
        [InlineData(1, 9, 2, 8, true)]  // 1+9=10, 2+8=10 → match
        [InlineData(2,  8, 3, 7, true)]  // 2+8=10, 3+7=10; attacker loses both (no base capture)
        public void Tally_SumEquality(
            int attackTop, int northBottom,
            int attackRight, int eastLeft,
            bool expectFire)
        {
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } };
            var resolver = new CaptureResolver(config);

            // Ensure no base capture (attacker edges below defenders)
            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:northBottom, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:eastLeft, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:attackTop, r:attackRight, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            if (expectFire)
            {
                Assert.Contains((0, 1), captures);
                Assert.Contains((1, 2), captures);
            }
            else
            {
                Assert.Empty(captures);
            }
        }

        [Fact]
        public void Tally_OffByOne_DoesNotTrigger()
        {
            // Sums: 4+6=10 and 3+8=11 — one apart. Must not fire.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:6, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:8, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:4, r:3, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            Assert.Empty(resolver.Resolve(board, 1, 1));
        }

        [Fact]
        public void Tally_ThreeContactsSameSum_AllThreeCaptured()
        {
            // Three contacts all summing to 10 → all three Tally-captured.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:6, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:6, owner:2), row:1, col:2)
                .Place(CardFactory.Street("S", t:6, r:1, b:1, l:1, owner:2), row:2, col:1)
                .BuildBoard();

            // Attacker: Top=4 (sum 10 vs N), Right=4 (sum 10 vs E), Bottom=4 (sum 10 vs S)
            var attacker = CardFactory.Street("Att", t:4, r:4, b:4, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            Assert.Contains((0, 1), captures);
            Assert.Contains((1, 2), captures);
            Assert.Contains((2, 1), captures);
        }

        [Fact]
        public void Tally_BaseCapture_ExcludesCardFromTallyPool()
        {
            // North is base-captured (attacker Top > North Bottom).
            // The sum for North is still calculated but North is in alreadyCaptured,
            // so only East can participate in Tally. One contact → Tally doesn't fire.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:3, l:1, owner:2), row:0, col:1) // 7+3=10
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:6, owner:2), row:1, col:2) // 4+6=10
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:7, r:4, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            Assert.Contains((0, 1), captures);     // base capture
            Assert.DoesNotContain((1, 2), captures); // only 1 Tally contact → no fire
        }

        // ══════════════════════════════════════════════════════════════════════
        // WALL SIGNATURE — board edge counts as 10 for sum matching
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void WallSignature_BoardEdgeCountsAsTen()
        {
            // Card at corner (0,0). Top and Left face board edges (value=10 each).
            // Attacker Top=8: wall contact sum = 8+10=18
            // Enemy to right: Left=10, Attacker Right=8: contact sum = 8+10=18
            // Two sums of 18 match → Wall Signature fires.
            var config = new MatchConfig
            {
                Protocols = new List<IProtocol> { new WallSignatureProtocol(), new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("Enemy", t:1, r:1, b:1, l:10, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:8, r:8, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 0, 0);

            var captures = resolver.Resolve(board, 0, 0);
            Assert.Contains((0, 1), captures);
        }

        [Fact]
        public void WallSignature_WallValueIsExactlyTen_NineDoesNotMatch()
        {
            // Attacker edge=9: wall contact sum = 9+10=19.
            // Enemy Left=10: real contact sum = 9+10=19. These match.
            // But: Enemy Left=9: real contact sum = 9+9=18 ≠ 19. No fire.
            var config = new MatchConfig
            {
                Protocols = new List<IProtocol> { new WallSignatureProtocol(), new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("Enemy", t:1, r:1, b:1, l:9, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:9, r:9, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 0, 0);

            // Wall sum: 9+10=19. Real contact sum: 9+9=18. No match → no fire.
            var captures = resolver.Resolve(board, 0, 0);
            Assert.DoesNotContain((0, 1), captures);
        }

        [Fact]
        public void WallSignature_MultipleWallContacts_EachCountSeparately()
        {
            // Card at (0,0): two wall contacts (Top and Left) and one real contact.
            // Top wall sum = 10+10=20 (attacker edge 10 + wall 10)
            // Left wall sum = 10+10=20
            // Enemy right: Left=10, attacker Right=10 → real sum = 10+10=20
            // All three sums = 20 → fires.
            var config = new MatchConfig
            {
                Protocols = new List<IProtocol> { new WallSignatureProtocol(), new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("Enemy", t:1, r:1, b:1, l:10, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Att", t:10, r:10, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 0, 0);

            var captures = resolver.Resolve(board, 0, 0);
            Assert.Contains((0, 1), captures);
        }
    }
}