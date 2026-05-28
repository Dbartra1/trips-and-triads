using Xunit;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Capture
{
    /// <summary>
    /// Tests for chain capture mechanics: The Breach (Vesna's Domain) and Cascade.
    /// Chains are the most complex part of the capture system — easy to get wrong,
    /// easy to infinite-loop, important to test at depth.
    /// </summary>
    public class ChainTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // THE BREACH — base captures chain when placed card is adjacent to
        // a friendly Vesna (Decay ability card).
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Breach_CaptureChains_WhenAdjacentToVesna()
        {
            // Vesna at (1,0). P1 places a strong card at (1,1).
            // That card captures EnemyA at (0,1).
            // EnemyA, now P1, re-attacks EnemyB at (0,2) — chain capture.
            var vesna = CardFactory.Vesna(owner: 1);
            var board = new BoardBuilder()
                .Place(vesna, row: 1, col: 0)
                .Place(CardFactory.Street("EnemyA", t:1, r:1, b:5, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyB", t:1, r:1, b:1, l:2, owner:2), row:0, col:2)
                .BuildBoard();

            // Placer: Top=9 captures EnemyA (Bottom=5). Right=9 > EnemyB Left=2 after chain.
            var placer = CardFactory.Street("Placer", t:9, r:9, b:1, l:1, owner:1);
            board.PlaceCard(placer, 1, 1);

            var config   = MatchConfig.BaseRules();
            var resolver = new CaptureResolver(config);
            var captures = resolver.Resolve(board, 1, 1);

            // Direct capture: EnemyA (0,1)
            Assert.Contains((0, 1), captures);
            // Chain capture: EnemyB (0,2) via EnemyA's new Right edge after flip
            Assert.Contains((0, 2), captures);
        }

        [Fact]
        public void Breach_NoChain_WhenNotAdjacentToVesna()
        {
            // Same layout as above but Vesna is far away — no chain.
            var vesna = CardFactory.Vesna(owner: 1);
            var board = new BoardBuilder()
                .Place(vesna, row: 2, col: 2) // not adjacent to placer at (1,1)
                .Place(CardFactory.Street("EnemyA", t:1, r:1, b:5, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyB", t:1, r:1, b:1, l:2, owner:2), row:0, col:2)
                .BuildBoard();

            var placer = CardFactory.Street("Placer", t:9, r:9, b:1, l:1, owner:1);
            board.PlaceCard(placer, 1, 1);

            var config   = MatchConfig.BaseRules();
            var resolver = new CaptureResolver(config);
            var captures = resolver.Resolve(board, 1, 1);

            // Direct capture only
            Assert.Single(captures);
            Assert.Contains((0, 1), captures);
            // EnemyB not captured — no Breach, no chain
            Assert.DoesNotContain((0, 2), captures);
        }

        [Fact]
        public void Breach_DoesNotInfiniteLoop_WithMutualCapture()
        {
            // Two enemy cards arranged so EnemyA can capture EnemyB after flip
            // and EnemyB could in theory re-flip EnemyA. The visited set must
            // prevent this loop.
            var vesna = CardFactory.Vesna(owner: 1);
            var board = new BoardBuilder()
                .Place(vesna, row: 0, col: 0)
                // EnemyA and EnemyB both strong in the direction facing each other
                .Place(CardFactory.Street("EnemyA", t:1, r:9, b:1, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyB", t:1, r:1, b:1, l:9, owner:2), row:0, col:2)
                .BuildBoard();

            var placer = CardFactory.Street("Placer", t:1, r:1, b:1, l:1, owner:1);
            board.PlaceCard(placer, 1, 0);

            var config   = MatchConfig.BaseRules();
            var resolver = new CaptureResolver(config);

            // This must terminate — exception or timeout would be a fail
            var exception = Record.Exception(() => resolver.Resolve(board, 1, 0));
            Assert.Null(exception);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CASCADE — protocol captures chain under base capture rules.
        // Active in Dead Channel and The Hush.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Cascade_ProtocolCaptureChains()
        {
            // Cascade is active. Handshake triggers on EnemyA. EnemyA (now P1)
            // then captures EnemyB via base rules.
            var config = new MatchConfig
            {
                Cascade   = true,
                Protocols = new List<IProtocol> { new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            // EnemyA: positioned to be caught by Handshake (tie on two contacts),
            //         and has a strong Right that can take EnemyB.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyA", t:5, r:9, b:5, l:1, owner:2), row:1, col:1)
                .Place(CardFactory.Street("EnemyB", t:1, r:1, b:1, l:2, owner:2), row:1, col:2)
                .Place(CardFactory.Street("TieHelper", t:1, r:1, b:5, l:1, owner:2), row:2, col:1)
                .BuildBoard();

            // Attacker: ties with EnemyA (Top=5=EnemyA Top) and TieHelper (Bottom=5=TieHelper Top)
            var attacker = CardFactory.Street("Attacker", t:5, r:1, b:5, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = resolver.Resolve(board, 1, 0);

            // Handshake captures EnemyA + TieHelper (two tie contacts).
            // Cascade: EnemyA (now P1) uses Right=9 to capture EnemyB (Left=2).
            Assert.Contains((1, 1), captures); // EnemyA via Handshake
            Assert.Contains((2, 1), captures); // TieHelper via Handshake
            Assert.Contains((1, 2), captures); // EnemyB via Cascade chain
        }

        [Fact]
        public void Cascade_Inactive_ProtocolCaptureDoesNotChain()
        {
            // Same scenario but Cascade disabled — EnemyB should not be captured.
            var config = new MatchConfig
            {
                Cascade   = false,
                Protocols = new List<IProtocol> { new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyA",    t:5, r:9, b:5, l:1, owner:2), row:1, col:1)
                .Place(CardFactory.Street("EnemyB",    t:1, r:1, b:1, l:2, owner:2), row:1, col:2)
                .Place(CardFactory.Street("TieHelper", t:1, r:1, b:5, l:1, owner:2), row:2, col:1)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:5, r:1, b:5, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = resolver.Resolve(board, 1, 0);

            Assert.Contains((1, 1), captures);
            Assert.Contains((2, 1), captures);
            Assert.DoesNotContain((1, 2), captures); // no chain
        }

        [Fact]
        public void Cascade_ChainDepth_MultipleLinks()
        {
            // Tests a three-link chain: Handshake gets Card1 → Card1 takes Card2 → Card2 takes Card3.
            var config = new MatchConfig
            {
                Cascade   = true,
                Protocols = new List<IProtocol> { new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            // Layout (row 0): [TieHelper(0,0)] [Attacker(0,1)] [Card1(0,2)]
            // Layout (row 1): [Card2(0,2 neighbour) = (1,2)]
            // Card1 at (0,2): Left=6 ties → gets Handshake-captured → Right=9 captures Card2
            // Card2 at (1,2): Top=9, captures Card3 at (2,2)
            var board = new BoardBuilder()
                .Place(CardFactory.Street("TieHelper", t:1, r:6, b:1, l:1, owner:2), row:0, col:0)
                .Place(CardFactory.Street("Card1", t:1, r:9, b:9, l:6, owner:2), row:0, col:2)
                .Place(CardFactory.Street("Card2", t:3, r:9, b:1, l:1, owner:2), row:1, col:2)
                .Place(CardFactory.Street("Card3", t:2, r:1, b:1, l:1, owner:2), row:2, col:2)
                .BuildBoard();

            // Attacker at (0,1): Right=6 ties with Card1 Left=6.
            //                    Left=6 ties with TieHelper Right=6.
            var attacker = CardFactory.Street("Attacker", t:1, r:6, b:1, l:6, owner:1);
            board.PlaceCard(attacker, 0, 1);

            var captures = resolver.Resolve(board, 0, 1);

            Assert.Contains((0, 0), captures); // TieHelper via Handshake
            Assert.Contains((0, 2), captures); // Card1 via Handshake
            Assert.Contains((1, 2), captures); // Card2 via Cascade from Card1
            Assert.Contains((2, 2), captures); // Card3 via Cascade from Card2
        }

        // ══════════════════════════════════════════════════════════════════════
        // THE LISTENER — Riven refuses to capture Hollow Choir cards.
        // Bond must be active (both Riven and Vesna on board).
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Listener_RivenRefusesToCaptureChoir_WhenBondActive()
        {
            // Riven and Vesna both on board → The Listener activates.
            // Riven attempts to capture a Choir card she would normally win.
            var riven = CardFactory.Riven(owner: 1);
            var vesna = CardFactory.Vesna(owner: 2); // enemy Vesna, just needs to exist on board

            var choirCard = CardFactory.Create("Choir Card")
                .Stats(1, 1, 1, 1)
                .Faction(Faction.HollowChoir).Tier(Tier.Street)
                .Build(owner: 2);

            var board = new BoardBuilder()
                .Place(vesna,    row: 2, col: 2)
                .Place(choirCard, row: 0, col: 1)
                .BuildBoard();

            // Apply bonds to activate Listener
            BondResolver.Apply(board);

            board.PlaceCard(riven, 1, 1);
            BondResolver.Apply(board); // re-apply with Riven now on board
            DomainResolver.Apply(board);
            BondResolver.Apply(board);

            // Riven's Right=9 > choirCard Left=1, but Listener blocks
            var resolver = new CaptureResolver(MatchConfig.BaseRules());
            var captures = resolver.Resolve(board, 1, 1);

            Assert.DoesNotContain((0, 1), captures);
            Assert.Equal(2, board.GetCard(0, 1)!.OwnerId); // Choir card still enemy
        }

        [Fact]
        public void Listener_RivenCapturesNonChoir_Normally()
        {
            // Listener is active but the card being captured is not Choir — normal capture.
            var riven = CardFactory.Riven(owner: 1);
            var vesna = CardFactory.Vesna(owner: 2);

            var nonChoirEnemy = CardFactory.Street("NotChoir", t:1, r:1, b:1, l:1, owner:2);

            var board = new BoardBuilder()
                .Place(vesna,         row: 2, col: 2)
                .Place(nonChoirEnemy, row: 0, col: 1)
                .BuildBoard();

            board.PlaceCard(riven, 1, 1);
            BondResolver.Apply(board);
            DomainResolver.Apply(board);
            BondResolver.Apply(board);

            var resolver = new CaptureResolver(MatchConfig.BaseRules());
            var captures = resolver.Resolve(board, 1, 1);

            // Riven's Top=3 vs enemy Bottom=1 — Riven wins
            Assert.Contains((0, 1), captures);
        }
    }
}
