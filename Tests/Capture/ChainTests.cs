using Xunit;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Capture
{
    public class ChainTests
    {
        // ══════════════════════════════════════════════════════════════════════
        // THE BREACH — base captures chain when placed card is adjacent to
        // a friendly Vesna (Decay ability card).
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Breach_CaptureChains_WhenAdjacentToVesna()
        {
            // Vesna at (1,0) — friendly. Placer placed at (1,1), adjacent to Vesna.
            // Placer Top(9) captures EnemyA at (0,1) (Bottom=5). Breach fires.
            // EnemyA (now P1) has Right=9 which beats EnemyB Left=2 at (0,2).
            var vesna = CardFactory.Vesna(owner: 1);
            var board = new BoardBuilder()
                .Place(vesna, row: 1, col: 0)
                .Place(CardFactory.Street("EnemyA", t:1, r:9, b:5, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyB", t:1, r:1, b:1, l:2, owner:2), row:0, col:2)
                .BuildBoard();

            var placer = CardFactory.Street("Placer", t:9, r:1, b:1, l:1, owner:1);
            board.PlaceCard(placer, 1, 1);

            var config   = MatchConfig.BaseRules();
            var resolver = new CaptureResolver(config);
            var captures = resolver.Resolve(board, 1, 1);

            Assert.Contains((0, 1), captures); // EnemyA — direct capture
            Assert.Contains((0, 2), captures); // EnemyB — chain via EnemyA's Right=9
        }

        [Fact]
        public void Breach_NoChain_WhenNotAdjacentToVesna()
        {
            // Vesna at (2,2) — NOT adjacent to placer at (1,1). No Breach.
            var vesna = CardFactory.Vesna(owner: 1);
            var board = new BoardBuilder()
                .Place(vesna, row: 2, col: 2)
                .Place(CardFactory.Street("EnemyA", t:1, r:9, b:5, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyB", t:1, r:1, b:1, l:2, owner:2), row:0, col:2)
                .BuildBoard();

            var placer = CardFactory.Street("Placer", t:9, r:1, b:1, l:1, owner:1);
            board.PlaceCard(placer, 1, 1);

            var config   = MatchConfig.BaseRules();
            var resolver = new CaptureResolver(config);
            var captures = resolver.Resolve(board, 1, 1);

            Assert.Single(captures);
            Assert.Contains((0, 1), captures);
            Assert.DoesNotContain((0, 2), captures); // no chain
        }

        [Fact]
        public void Breach_DoesNotInfiniteLoop_WithMutualCapture()
        {
            // Two enemy cards each strong toward each other.
            // The visited set must prevent infinite loop when Breach fires.
            var vesna = CardFactory.Vesna(owner: 1);
            var board = new BoardBuilder()
                .Place(vesna, row: 0, col: 0)
                .Place(CardFactory.Street("EnemyA", t:1, r:9, b:1, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("EnemyB", t:1, r:1, b:1, l:9, owner:2), row:0, col:2)
                .BuildBoard();

            var placer = CardFactory.Street("Placer", t:1, r:1, b:1, l:1, owner:1);
            board.PlaceCard(placer, 1, 0);

            var config   = MatchConfig.BaseRules();
            var resolver = new CaptureResolver(config);

            var exception = Record.Exception(() => resolver.Resolve(board, 1, 0));
            Assert.Null(exception);
        }

        // ══════════════════════════════════════════════════════════════════════
        // CASCADE — protocol captures chain under base capture rules.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Overflow_ProtocolCaptureChains()
        {
            // Overflow active. Attacker at (1,0) ties with EnemyA (right) and
            // TieHelper (bottom) — two ties → Handshake fires, both captured.
            // EnemyA (now P1) has Right=9, captures EnemyB Left=2 at (1,2).
            var config = new MatchConfig
            {
                Overflow   = true,
                Protocols = new List<IProtocol> { new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyA",    t:5, r:9, b:5, l:5, owner:2), row:1, col:1)
                .Place(CardFactory.Street("TieHelper", t:5, r:1, b:1, l:1, owner:2), row:2, col:0)
                .Place(CardFactory.Street("EnemyB",    t:1, r:1, b:1, l:2, owner:2), row:1, col:2)
                .BuildBoard();

            // Attacker Right=5 ties EnemyA Left=5; Attacker Bottom=5 ties TieHelper Top=5.
            var attacker = CardFactory.Street("Attacker", t:1, r:5, b:5, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = resolver.Resolve(board, 1, 0);

            Assert.Contains((1, 1), captures); // EnemyA via Handshake
            Assert.Contains((2, 0), captures); // TieHelper via Handshake
            Assert.Contains((1, 2), captures); // EnemyB via Overflow chain from EnemyA
        }

        [Fact]
        public void Overflow_Inactive_ProtocolCaptureDoesNotChain()
        {
            // Same layout but Overflow=false — EnemyB must NOT be captured.
            var config = new MatchConfig
            {
                Overflow   = false,
                Protocols = new List<IProtocol> { new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            var board = new BoardBuilder()
                .Place(CardFactory.Street("EnemyA",    t:5, r:9, b:5, l:5, owner:2), row:1, col:1)
                .Place(CardFactory.Street("TieHelper", t:5, r:1, b:1, l:1, owner:2), row:2, col:0)
                .Place(CardFactory.Street("EnemyB",    t:1, r:1, b:1, l:2, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:1, r:5, b:5, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);

            var captures = resolver.Resolve(board, 1, 0);

            Assert.Contains((1, 1), captures);   // EnemyA via Handshake
            Assert.Contains((2, 0), captures);   // TieHelper via Handshake
            Assert.DoesNotContain((1, 2), captures); // EnemyB — no Overflow
        }

        [Fact]
        public void Overflow_ChainDepth_MultipleLinks()
        {
            // Three-link chain: Handshake gets Card1 → Card1 Bottom=9 takes Card2
            // → Card2 Bottom=9 takes Card3.
            var config = new MatchConfig
            {
                Overflow   = true,
                Protocols = new List<IProtocol> { new HandshakeProtocol() }
            };
            var resolver = new CaptureResolver(config);

            // Attacker at (0,1): Right=6 ties Card1 Left=6, Left=6 ties TieHelper Right=6.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("TieHelper", t:1, r:6, b:1, l:1, owner:2), row:0, col:0)
                .Place(CardFactory.Street("Card1",     t:1, r:1, b:9, l:6, owner:2), row:0, col:2)
                .Place(CardFactory.Street("Card2",     t:3, r:1, b:9, l:1, owner:2), row:1, col:2)
                .Place(CardFactory.Street("Card3",     t:2, r:1, b:1, l:1, owner:2), row:2, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("Attacker", t:1, r:6, b:1, l:6, owner:1);
            board.PlaceCard(attacker, 0, 1);

            var captures = resolver.Resolve(board, 0, 1);

            Assert.Contains((0, 0), captures); // TieHelper via Handshake
            Assert.Contains((0, 2), captures); // Card1 via Handshake
            Assert.Contains((1, 2), captures); // Card2 via Overflow from Card1 (Bottom=9 > Top=3)
            Assert.Contains((2, 2), captures); // Card3 via Overflow from Card2 (Bottom=9 > Top=2)
        }

        // ══════════════════════════════════════════════════════════════════════
        // THE LISTENER — Riven refuses to capture Hollow Choir cards.
        // ══════════════════════════════════════════════════════════════════════

        [Fact]
        public void Listener_RivenRefusesToCaptureChoir_WhenBondActive()
        {
            var riven = CardFactory.Riven(owner: 1);
            var vesna = CardFactory.Vesna(owner: 2);

            var choirCard = CardFactory.Create("Choir Card")
                .Stats(1, 1, 1, 1)
                .Faction(Faction.HollowChoir).Tier(Tier.Street)
                .Build(ownerId: 2);

            var board = new BoardBuilder()
                .Place(vesna,    row: 2, col: 2)
                .Place(choirCard, row: 0, col: 1)
                .BuildBoard();

            BondResolver.Apply(board);
            board.PlaceCard(riven, 1, 1);
            BondResolver.Apply(board);
            DomainResolver.Apply(board);
            BondResolver.Apply(board);

            var resolver = new CaptureResolver(MatchConfig.BaseRules());
            var captures = resolver.Resolve(board, 1, 1);

            Assert.DoesNotContain((0, 1), captures);
            Assert.Equal(2, board.GetCard(0, 1)!.OwnerId);
        }

        [Fact]
        public void Listener_RivenCapturesNonChoir_Normally()
        {
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

            // Riven Top=3 > enemy Bottom=1
            Assert.Contains((0, 1), captures);
        }
    }
}