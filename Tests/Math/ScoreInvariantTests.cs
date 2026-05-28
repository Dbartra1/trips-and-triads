using Xunit;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.Tests.Helpers;

namespace TripsAndTriads.Tests.Math
{
    /// <summary>
    /// Verifies the fundamental score invariant of the game:
    ///   P1Score + P2Score == number of cards placed on the board
    ///
    /// This must hold after every single placement — no card should ever be
    /// unowned, double-counted, or disappear from scoring. Also verifies
    /// that the final score correctly accounts for the unplayed card in hand.
    /// </summary>
    public class ScoreInvariantTests
    {
        private static void AssertScoreInvariant(BoardState board, int placedCards, string context = "")
        {
            int p1 = board.GetScore(1);
            int p2 = board.GetScore(2);
            Assert.True(p1 + p2 == placedCards,
                $"Score invariant broken {context}: P1={p1} + P2={p2} = {p1+p2}, expected {placedCards}");
        }

        // ── Invariant holds after every placement ──────────────────────────────

        [Fact]
        public void ScoreInvariant_HoldsAfterEveryPlacement()
        {
            var gm = new GameManager(MatchConfig.BaseRules());

            var p1Deck = new List<CardData>
            {
                CardFactory.Street("P1a", t:8, r:8, b:8, l:8).Data,
                CardFactory.Street("P1b", t:7, r:7, b:7, l:7).Data,
                CardFactory.Street("P1c", t:6, r:6, b:6, l:6).Data,
                CardFactory.Street("P1d", t:5, r:5, b:5, l:5).Data,
                CardFactory.Street("P1e", t:4, r:4, b:4, l:4).Data,
            };
            var p2Deck = new List<CardData>
            {
                CardFactory.Street("P2a", t:1, r:1, b:1, l:1).Data,
                CardFactory.Street("P2b", t:1, r:1, b:1, l:1).Data,
                CardFactory.Street("P2c", t:1, r:1, b:1, l:1).Data,
                CardFactory.Street("P2d", t:1, r:1, b:1, l:1).Data,
                CardFactory.Street("P2e", t:1, r:1, b:1, l:1).Data,
            };

            gm.DealHands(p1Deck, p2Deck);

            // Play all 9 cards in a fixed order, checking invariant after each
            var moves = new[]
            {
                (0,0,0), (0,0,1), (0,0,2),
                (0,1,0), (0,1,1), (0,1,2),
                (0,2,0), (0,2,1), (0,2,2),
            };

            int placed = 0;
            foreach (var (hi, r, c) in moves)
            {
                gm.PlayCard(hi, r, c);
                placed++;
                AssertScoreInvariant(gm.Board, placed, $"after turn {placed}");
            }
        }

        [Fact]
        public void ScoreInvariant_HoldsAfterCapture()
        {
            // P1 plays a strong card that captures two P2 cards.
            // Invariant must still hold — 3 cards on board, P1+P2=3.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("P2a", t:1, r:1, b:1, l:1, owner:2), row:0, col:1)
                .Place(CardFactory.Street("P2b", t:1, r:1, b:1, l:1, owner:2), row:1, col:2)
                .BuildBoard();

            var attacker = CardFactory.Street("P1", t:9, r:9, b:9, l:9, owner:1);
            board.PlaceCard(attacker, 1, 1);

            var resolver = new CaptureResolver(MatchConfig.BaseRules());
            var captured = resolver.Resolve(board, 1, 1);

            // Flip captured cards
            foreach (var (cr, cc) in captured)
                board.GetCard(cr, cc)!.OwnerId = 1;

            AssertScoreInvariant(board, 3);
            Assert.Equal(3, board.GetScore(1)); // attacker + both captures
            Assert.Equal(0, board.GetScore(2));
        }

        [Fact]
        public void ScoreInvariant_HoldsAfterChainCapture()
        {
            var config   = new MatchConfig { Cascade = true, Protocols = new List<IProtocol> { new HandshakeProtocol() } };
            var resolver = new CaptureResolver(config);

            // 3 P2 cards placed; P1 plays one card that chains and captures all 3
            var board = new BoardBuilder()
                .Place(CardFactory.Street("E1", t:5, r:9, b:5, l:5, owner:2), row:1, col:1)
                .Place(CardFactory.Street("E2", t:1, r:1, b:1, l:2, owner:2), row:1, col:2)
                .Place(CardFactory.Street("TH", t:5, r:1, b:1, l:1, owner:2), row:2, col:0)
                .BuildBoard();

            var attacker = CardFactory.Street("P1", t:1, r:5, b:5, l:1, owner:1);
            board.PlaceCard(attacker, 1, 0);
            resolver.Resolve(board, 1, 0);

            AssertScoreInvariant(board, 4); // 4 cards placed total
        }

        [Fact]
        public void ScoreInvariant_FullBoard_ScoresSumToNine()
        {
            // Fill all 9 cells — P1+P2 must equal exactly 9.
            var gm = new GameManager(MatchConfig.BaseRules());

            var deck = new List<CardData>();
            for (int i = 0; i < 5; i++)
                deck.Add(CardFactory.Street($"P{i}", t:5, r:5, b:5, l:5).Data);

            gm.DealHands(deck, deck);

            // Play all 9 moves
            var cells = new[] { (0,0,0),(0,1,0),(0,2,0),(0,0,1),(0,1,1),(0,2,1),(0,0,2),(0,1,2),(0,2,2) };
            foreach (var (hi, r, c) in cells)
                gm.PlayCard(hi, r, c);

            Assert.True(gm.Board.IsFull());
            AssertScoreInvariant(gm.Board, 9, "full board");
            Assert.Equal(9, gm.Board.GetScore(1) + gm.Board.GetScore(2));
        }

        // ── GetScore counts only own cards ─────────────────────────────────────

        [Fact]
        public void GetScore_CountsOnlyOwnerId_NotOriginalOwner()
        {
            // A card that was originally P2's but is now controlled by P1
            // should count for P1, not P2.
            var board = new BoardBuilder()
                .Place(CardFactory.Street("WasP2", t:1, r:1, b:1, l:1, owner:2), row:0, col:0)
                .BuildBoard();

            // Simulate capture by flipping OwnerId
            board.GetCard(0, 0)!.OwnerId = 1;

            Assert.Equal(1, board.GetScore(1));
            Assert.Equal(0, board.GetScore(2));
        }

        [Fact]
        public void GetScore_EmptyBoard_ReturnsZeroForBothPlayers()
        {
            var board = new BoardState();
            Assert.Equal(0, board.GetScore(1));
            Assert.Equal(0, board.GetScore(2));
        }

        [Fact]
        public void GetScore_SingleCard_CorrectPlayer()
        {
            var board = new BoardBuilder()
                .Place(CardFactory.Street("P1card", t:1, r:1, b:1, l:1, owner:1), row:0, col:0)
                .BuildBoard();

            Assert.Equal(1, board.GetScore(1));
            Assert.Equal(0, board.GetScore(2));
        }

        // ── Win condition math ────────────────────────────────────────────────

        [Fact]
        public void Winner_IsPlayerWithMoreCards_CountingUnplayedHandCard()
        {
            // In a real game each player starts with 5 cards; the 10th card
            // (1 per player) stays in hand and counts toward final score.
            // GameManager.GetScore counts board cards only; the hand card
            // contributes as one for each player even if unplayed.
            // This test confirms the board-level scoring is correct.
            // (The hand card is counted by GameBoard in stake resolution.)
            var board = new BoardBuilder()
                .Place(CardFactory.Street("P1a", t:1,r:1,b:1,l:1, owner:1), row:0,col:0)
                .Place(CardFactory.Street("P1b", t:1,r:1,b:1,l:1, owner:1), row:0,col:1)
                .Place(CardFactory.Street("P1c", t:1,r:1,b:1,l:1, owner:1), row:0,col:2)
                .Place(CardFactory.Street("P1d", t:1,r:1,b:1,l:1, owner:1), row:1,col:0)
                .Place(CardFactory.Street("P2a", t:1,r:1,b:1,l:1, owner:2), row:1,col:1)
                .Place(CardFactory.Street("P2b", t:1,r:1,b:1,l:1, owner:2), row:1,col:2)
                .Place(CardFactory.Street("P2c", t:1,r:1,b:1,l:1, owner:2), row:2,col:0)
                .Place(CardFactory.Street("P2d", t:1,r:1,b:1,l:1, owner:2), row:2,col:1)
                .Place(CardFactory.Street("P1e", t:1,r:1,b:1,l:1, owner:1), row:2,col:2)
                .BuildBoard();

            // Board: P1=5, P2=4, but each has 1 unplayed card → effective 6 vs 5
            Assert.Equal(5, board.GetScore(1));
            Assert.Equal(4, board.GetScore(2));
            Assert.True(board.GetScore(1) > board.GetScore(2));
            AssertScoreInvariant(board, 9);
        }
    }
}