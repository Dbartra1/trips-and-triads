using Xunit;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Capture
{
    /// <summary>
    /// Tests for the fundamental capture mechanic — edge comparison without any protocols.
    /// Every test here uses MatchConfig.BaseRules() so failures are unambiguous.
    /// </summary>
    public class BaseCaptureTests
    {
        private readonly CaptureResolver _resolver = new CaptureResolver(MatchConfig.BaseRules());

        // ── Simple win / loss / tie ────────────────────────────────────────────

        [Fact]
        public void PlacedCard_HigherTop_CapturesCardBelow()
        {
            // Place an enemy card at (0,0), then place attacker at (1,0).
            // Attacker's Top (8) > enemy's Bottom (5) → capture.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Enemy", t:3, r:3, b:5, l:3, owner:2), row:0, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:8, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = _resolver.Resolve(board, 1, 0);

            Assert.Single(captures);
            Assert.Equal((0, 0), captures[0]);
            Assert.Equal(1, board.GetCard(0, 0)!.OwnerId);
        }

        [Fact]
        public void PlacedCard_LowerTop_DoesNotCapture()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:3, r:3, b:9, l:3, owner:2), row:0, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:5, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = _resolver.Resolve(board, 1, 0);

            Assert.Empty(captures);
            Assert.Equal(2, board.GetCard(0, 0)!.OwnerId);
        }

        [Fact]
        public void EqualEdge_BaseRules_DoesNotCapture()
        {
            // Equal edges under base rules = no capture. Handshake changes this.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:3, r:3, b:6, l:3, owner:2), row:0, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:6, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = _resolver.Resolve(board, 1, 0);

            Assert.Empty(captures);
        }

        [Fact]
        public void CardDoesNotCaptureOwnTeam()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Friendly", t:3, r:3, b:1, l:3, owner:1), row:0, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:9, r:9, b:9, l:9, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = _resolver.Resolve(board, 1, 0);

            Assert.Empty(captures);
            Assert.Equal(1, board.GetCard(0, 0)!.OwnerId);
        }

        // ── Multi-direction captures ───────────────────────────────────────────

        [Fact]
        public void CardCanCaptureInMultipleDirections()
        {
            // Attacker at center (1,1), enemies above and to the right.
            // Attacker Top(9) > enemy Bottom(3) and Right(9) > enemy Left(2).
            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyTop",   t:5, r:5, b:3, l:5, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyRight", t:5, r:5, b:5, l:2, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:9, r:9, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = _resolver.Resolve(board, 1, 1);

            Assert.Equal(2, captures.Count);
            Assert.Equal(1, board.GetCard(0, 1)!.OwnerId);
            Assert.Equal(1, board.GetCard(1, 2)!.OwnerId);
        }

        [Fact]
        public void CenterCard_CanCaptureFourDirections()
        {
            // Center card with all 9s surrounded by weak enemies.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:1, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("S", t:1, r:1, b:1, l:1, owner:2), row:2, col:1)
                .Place(CardFactory.Street("W", t:1, r:1, b:1, l:1, owner:2), row:1, col:0)
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:1, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Center", t:9, r:9, b:9, l:9, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = _resolver.Resolve(board, 1, 1);

            Assert.Equal(4, captures.Count);
        }

        // ── Corner / edge geometry ─────────────────────────────────────────────

        [Fact]
        public void CornerCard_OnlyExposedEdgesAreChecked()
        {
            // A card at (0,0) can only be attacked/capture via Right and Bottom.
            // The board doesn't wrap — Top and Left are walls.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("East",  t:5, r:5, b:5, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("South", t:1, r:5, b:5, l:5, owner:2), row:1, col:0)
                .BuildBoard();

            var corner = CardFactory.Street("Corner", t:9, r:9, b:9, l:9, owner:1);
            board.PlaceCard(corner, 0, 0);

            var captures = _resolver.Resolve(board, 0, 0);

            // Should capture East (Right > Left) and South (Bottom > Top) only.
            Assert.Equal(2, captures.Count);
        }

        // ── Seraph Yune: stat shape correctness ───────────────────────────────

        [Fact]
        public void YuneBlindSpot_WeakBottomCanBeExploited()
        {
            // Yune's Bottom is 3. A card with Top >= 4 placed below her flips her.
            var board = new BoardBuilder()
                .Place(CardFactory.SeraphYune(owner: 1), row: 0, col: 1)
                .BuildBoard();

            var exploiter = CardFactory.Street("Exploiter", t:4, r:1, b:1, l:1, owner:2);
            board.PlaceCard(exploiter, 1, 1);

            var captures = _resolver.Resolve(board, 1, 1);

            Assert.Single(captures);
            Assert.Equal(2, board.GetCard(0, 1)!.OwnerId); // Yune flipped
        }

        [Fact]
        public void YuneFront_StrongTopCapturesWeakEnemy()
        {
            // Yune's Top is 10 (A). Enemy Bottom must be < 10 to be captured.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Enemy", t:5, r:5, b:9, l:5, owner:2), row:0, col:1)
                .BuildBoard();

            var yune = CardFactory.SeraphYune(owner: 1);
            board.PlaceCard(yune, 1, 1);

            var captures = _resolver.Resolve(board, 1, 1);

            // Yune's Top(10) > Enemy's Bottom(9) → capture
            Assert.Contains((0, 1), captures);
        }

        // ── Sister Grin: corner predator shape ────────────────────────────────

        [Fact]
        public void Grin_SoftRightCanBeExploited()
        {
            // Grin's Right is 2. An enemy to her right with Left >= 3 flips her.
            var board = new BoardBuilder()
                .Place(CardFactory.SisterGrin(owner: 1), row: 0, col: 0)
                .BuildBoard();

            var exploiter = CardFactory.Street("Flanker", t:1, r:1, b:1, l:3, owner:2);
            board.PlaceCard(exploiter, 0, 1);

            var captures = _resolver.Resolve(board, 0, 1);

            Assert.Single(captures);
            Assert.Equal(2, board.GetCard(0, 0)!.OwnerId);
        }

        // ── Score reflects capture ownership ──────────────────────────────────

        [Fact]
        public void AfterCapture_ScoreUpdatesCorrectly()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("E1", t:1,r:1,b:1,l:1, owner:2), row:0, col:0)
                .Place(CardFactory.Street("E2", t:1,r:1,b:1,l:1, owner:2), row:0, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:9,r:9,b:9,l:9, owner:1);
            board.PlaceCard(attacker, 0, 1);
            _resolver.Resolve(board, 0, 1);

            // P1 now owns the attacker + both captured cards
            Assert.Equal(3, board.GetScore(1));
            Assert.Equal(0, board.GetScore(2));
        }
    }
}
