using Xunit;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies the exact mathematical boundary conditions of capture:
    ///   - Capture is STRICT greater-than (attacker > defender), never >=
    ///   - At-threshold cases (delta of 1 in each direction)
    ///   - A (10) as maximum edge value
    ///   - Capture uses effective GetValue(), not GetBaseValue()
    /// </summary>
    public class CaptureBoundaryTests
    {
        private readonly CaptureResolver _resolver = new CaptureResolver(MatchConfig.BaseRules());

        // ── Strict greater-than ───────────────────────────────────────────────

        [Theory]
        [InlineData(5, 4, true)]   // attacker wins
        [InlineData(5, 5, false)]  // equal — no capture in base rules
        [InlineData(5, 6, false)]  // attacker loses
        [InlineData(1, 0, true)]   // minimum attacker beats zero
        [InlineData(0, 0, false)]  // zero vs zero — no capture
        [InlineData(10, 9, true)]  // A beats 9
        [InlineData(10, 10, false)]// A vs A — no capture
        public void Capture_StrictGreaterThan(int attackerTop, int defenderBottom, bool expectCapture)
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:1, r:1, b:defenderBottom, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:attackerTop, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = _resolver.Resolve(board, 1, 1);

            if (expectCapture)
                Assert.Contains((0, 1), captures);
            else
                Assert.DoesNotContain((0, 1), captures);
        }

        // ── Threshold: exactly one apart ──────────────────────────────────────

        [Fact]
        public void Capture_AttackerOneHigher_Captures()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:1, r:1, b:6, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:7, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            Assert.Contains((0, 1), _resolver.Resolve(board, 1, 1));
        }

        [Fact]
        public void Capture_AttackerOneLower_DoesNotCapture()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:1, r:1, b:8, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:7, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            Assert.DoesNotContain((0, 1), _resolver.Resolve(board, 1, 1));
        }

        // ── A (10) is the absolute maximum ────────────────────────────────────

        [Fact]
        public void Capture_A_BeatsAllValuesBelow10()
        {
            for (int defend = 0; defend <= 9; defend++)
            {
                var board = new BoardBuilder()
                    .Place(CardFactory.Street("D", t:1, r:1, b:defend, l:1, owner:2), row:0, col:1)
                    .BuildBoard();

                var attacker = CardFactory.Street("A", t:10, r:1, b:1, l:1, owner:1);
                board.PlaceCard(attacker, 1, 1);

                var captures = _resolver.Resolve(board, 1, 1);
                Assert.True(captures.Count > 0,
                    $"A(10) should capture defender with Bottom={defend}");
            }
        }

        [Fact]
        public void Capture_A_DoesNotCapture_A()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:1, r:1, b:10, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:10, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            Assert.Empty(_resolver.Resolve(board, 1, 1));
        }

        // ── Effective value is used for capture, not base value ───────────────

        [Fact]
        public void Capture_UsesEffectiveValue_DomainBonusTipsCaptureOver()
        {
            // Attacker base Top=5, but domain bonus +1 makes effective Top=6.
            // Defender Bottom=5. Base would tie (no capture), effective wins.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:1, r:1, b:5, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:5, r:1, b:1, l:1, owner:1);
            attacker.DomainBonusTop = 1; // effective Top = 6
            board.PlaceCard(attacker, 1, 1);

            var captures = _resolver.Resolve(board, 1, 1);
            Assert.Contains((0, 1), captures);
        }

        [Fact]
        public void Capture_UsesEffectiveValue_DomainBonusOnDefenderPreventsCapture()
        {
            // Attacker Top=6 would normally beat Defender Bottom=5.
            // But defender has DomainBonus +2 → effective Bottom=7. No capture.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:1, r:1, b:5, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var defender = board.GetCard(0, 1)!;
            defender.DomainBonusBottom = 2; // effective Bottom = 7

            var attacker = CardFactory.Street("Attacker", t:6, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            Assert.DoesNotContain((0, 1), _resolver.Resolve(board, 1, 1));
        }

        // ── Correct edges compared (direction mapping) ────────────────────────

        [Fact]
        public void Capture_Top_ComparedAgainstDefenderBottom()
        {
            // Attacker placed below defender. Attacker Top vs Defender Bottom.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:9, r:9, b:2, l:9, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:3, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            // Attacker Top=3 > Defender Bottom=2 → capture
            Assert.Contains((0, 1), _resolver.Resolve(board, 1, 1));
        }

        [Fact]
        public void Capture_Right_ComparedAgainstDefenderLeft()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:9, r:9, b:9, l:2, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:1, r:3, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            // Attacker Right=3 > Defender Left=2 → capture
            Assert.Contains((1, 2), _resolver.Resolve(board, 1, 1));
        }

        [Fact]
        public void Capture_Bottom_ComparedAgainstDefenderTop()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:2, r:9, b:9, l:9, owner:2), row:2, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:1, r:1, b:3, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            // Attacker Bottom=3 > Defender Top=2 → capture
            Assert.Contains((2, 1), _resolver.Resolve(board, 1, 1));
        }

        [Fact]
        public void Capture_Left_ComparedAgainstDefenderRight()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:9, r:2, b:9, l:9, owner:2), row:1, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:1, r:1, b:1, l:3, owner:1);
            board.PlaceCard(attacker, 1, 1);

            // Attacker Left=3 > Defender Right=2 → capture
            Assert.Contains((1, 0), _resolver.Resolve(board, 1, 1));
        }

        // ── Wrong-edge comparison never causes accidental capture ──────────────

        [Fact]
        public void Capture_DoesNotUseSameEdgeForBothCards()
        {
            // Attacker Top=9, Defender Top=1.
            // If the system wrongly compared Top vs Top it would capture.
            // Correct comparison is Attacker Top vs Defender Bottom=9. No capture.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Defender", t:1, r:1, b:9, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:9, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            // Attacker Top=9, Defender Bottom=9 → equal, no capture
            Assert.Empty(_resolver.Resolve(board, 1, 1));
        }
    }
}
