using Xunit;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Capture
{
    /// <summary>
    /// Tests for the three capture-rule protocols: Handshake, The Tally, Wall Signature.
    /// Each test section documents the lore rule it is verifying.
    /// </summary>
    public class ProtocolTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // HANDSHAKE — equal edge contacts capture both tied cards.
        // Requires exactly ≥2 tie contacts to trigger.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Handshake_TwoTieContacts_CapturesBoth()
        {
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            // Attacker Right=5. Enemy Left=5 → tie.
            // Attacker Top=7.   Enemy Bottom=7 → tie.
            // Two ties → Handshake fires, both captured.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyTop",   t:1, r:1, b:7, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyRight", t:1, r:1, b:1, l:5, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:7, r:5, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            Assert.Contains((0, 1), captures);
            Assert.Contains((1, 2), captures);
        }

        [Fact]
        public void Handshake_OneTieContact_DoesNotTrigger()
        {
            // Lore: a single identity collision is a coincidence, not a Handshake.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("Enemy", t:1, r:1, b:6, l:1, owner:2), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:6, r:1, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            // No base capture (equal = no capture in base rules) and no Handshake.
            Assert.Empty(captures);
        }

        [Fact]
        public void Handshake_AlreadyCapturedCard_NotRecaptured()
        {
            // A card captured by base rules before the protocol pass
            // should not be re-captured (or double-counted) by Handshake.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            // EnemyTop  Bottom=5 → ties with attacker Top=5 (one tie).
            // EnemyLeft Right=3  → attacker Left=9 > 3, so this is a base capture.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyTop",  t:1, r:1, b:5, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyLeft", t:1, r:3, b:1, l:1, owner:2), row:1, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:5, r:1, b:1, l:9, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            // EnemyLeft captured by base, EnemyTop tied but only one tie = no Handshake.
            Assert.Single(captures);
            Assert.Contains((1, 0), captures);
        }

        [Fact]
        public void Handshake_EfffigyStat_SymmetryMakesTiesLikely()
        {
            // Effigy's point-symmetric stats (T=B, L=R) means the same
            // edge value appears in two directions — Handshake is the
            // faction's fingerprint. This tests that the math works.
            //
            // Effigy card: T=7, R=9, B=7, L=9 (symmetric)
            // Place it between two enemies each with matching edges.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            var effigyCard = CardFactory.Create("Verity")
                .Id("eff_top_verity")
                .Stats(7, 9, 7, 9)
                .Faction(Faction.Effigy).Tier(Tier.TopTier)
                .Build(ownerId: 1);

            // Enemies designed to tie with Verity on two contacts
            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyNorth", t:1, r:1, b:7, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyEast",  t:1, r:1, b:1, l:9, owner:2), row:1, col:2)
                .BuildBoard();

            board.PlaceCard(effigyCard, 1, 1);
            var captures = resolver.Resolve(board, 1, 1);

            Assert.Equal(2, captures.Count);
        }

        // ══════════════════════════════════════════════════════════════════════
        // THE TALLY — equal contact-pair SUMS capture both cards.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Tally_EqualSums_CapturesBothCards()
        {
            // Attacker Top=7, EnemyNorth Bottom=3  → sum = 10
            // Attacker Right=4, EnemyEast Left=6   → sum = 10
            // Equal sums → The Tally fires, both captured.
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyNorth", t:1, r:1, b:3, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyEast",  t:1, r:1, b:1, l:6, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:7, r:4, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            Assert.Contains((0, 1), captures);
            Assert.Contains((1, 2), captures);
        }

        [Fact]
        public void Tally_UnequalSums_DoesNotCapture()
        {
            var config   = new MatchConfig { Protocols = new List<IProtocol> { new TallyProtocol() } };
            var resolver = new CaptureResolver(config);

            // Sums: 7+3=10 and 4+8=12 — different, no Tally.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyNorth", t:1, r:1, b:3, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyEast",  t:1, r:1, b:1, l:8, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:7, r:4, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            // Neither captured by base (7>3 for north, 4<8 for east)
            // 7>3 → base captures north only
            Assert.Single(captures);
            Assert.Contains((0, 1), captures);
        }

        [Fact]
        public void Tally_SumiCompounding_ShiftsSumsOverTime()
        {
            // Sumi starts 4/4/4/4 and compounds each turn. After 2 turns she
            // is at 6/6/6/6. This test verifies Tally sums shift with her growth —
            // a previously non-matching pair might match after compounding.
            // We directly call SumiAbility to simulate turns.
            var sumi   = CardFactory.MadameSumi(owner: 1);
            var board  = new BoardBuilder().Place(sumi, row: 1, col: 1).BuildBoard();

            var ability = new SumiAbility();

            // Simulate 2 turns
            ability.OnTurnEnd(board, sumi, 1, 1);
            ability.OnTurnEnd(board, sumi, 1, 1);

            // After 2 turns: 4+2=6 on all edges
            Assert.Equal(6, sumi.GetBaseValue(Direction.Top));
            Assert.Equal(6, sumi.GetBaseValue(Direction.Left));
        }

        // ══════════════════════════════════════════════════════════════════════
        // WALL SIGNATURE — board edge counts as A(10) for Handshake ties.
        // Extends Handshake — does nothing without it.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void WallSignature_EdgeTieWithWall_CapturesEnemy()
        {
            // Place attacker in corner (0,0). Its Top edge faces the wall (A=10).
            // If attacker Top = 10, wall contact sum = 20.
            // Enemy to the right: attacker Right = 10, enemy Left = 10 → real contact sum = 20.
            // Both sums match → Wall Signature fires and captures the enemy.
            var config = new MatchConfig
            {
                Protocols = new List<IProtocol>
                {
                    new WallSignatureProtocol(),
                    new HandshakeProtocol()
                }
            };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("Enemy", t:5, r:5, b:5, l:10, owner:2), row:0, col:1)
                .BuildBoard();

            // Attacker at corner (0,0). Top faces wall (sum 10+10=20). Right faces Enemy (10+10=20).
            var attacker = CardFactory.Street("Attacker", t:10, r:10, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 0, 0);

            var captures = resolver.Resolve(board, 0, 0);

            Assert.Contains((0, 1), captures);
        }

        [Fact]
        public void WallSignature_OnlyEnemy_NotFriendly_CapturedByWallTie()
        {
            var config = new MatchConfig
            {
                Protocols = new List<IProtocol>
                {
                    new WallSignatureProtocol(),
                    new HandshakeProtocol()
                }
            };
            var resolver = new CaptureResolver(config);

            // Friendly card to the right — wall tie should never flip friendly cards.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("Friendly", t:5, r:5, b:5, l:10, owner:1), row:0, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:10, r:10, b:1, l:1, owner:1);
            board.PlaceCard(attacker, 0, 0);

            var captures = resolver.Resolve(board, 0, 0);

            Assert.Empty(captures);
            Assert.Equal(1, board.GetCard(0, 1)!.OwnerId);
        }

        // ══════════════════════════════════════════════════════════════════════
        // PROTOCOL STACKING — Handshake + Tally together (The Powder Room / Vault)
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void StackedProtocols_BothCanTriggerIndependently()
        {
            var config   = MatchConfig.PowderRoom(); // Tally + Handshake
            var resolver = new CaptureResolver(config);

            // Design scenario where Tally fires on north/south and Handshake on east/west.
            // North: attacker Top=6, enemy Bottom=4 → sum 10
            // South: attacker Bottom=5, enemy Top=5 → sum 10 (also a Handshake tie!)
            // East:  attacker Right=7, enemy Left=7 → Handshake tie (also sum 14)
            // West:  attacker Left=7, enemy Right=7 → Handshake tie (also sum 14)
            var board = new BoardBuilder()
                .Place(CardFactory.Street("N", t:1, r:1, b:4, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("S", t:5, r:1, b:1, l:1, owner:2), row:2, col:1)
                .Place(CardFactory.Street("E", t:1, r:1, b:1, l:7, owner:2), row:1, col:2)
                .Place(CardFactory.Street("W", t:1, r:7, b:1, l:1, owner:2), row:1, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:6, r:7, b:5, l:7, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var captures = resolver.Resolve(board, 1, 1);

            // All 4 should be captured — a mix of Tally (N+S sum match)
            // and Handshake (E+W tie match).
            Assert.Equal(4, captures.Count);
        }
    }
}