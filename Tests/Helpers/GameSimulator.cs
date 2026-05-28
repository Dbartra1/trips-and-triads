using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Tests.Helpers
{
    /// <summary>
    /// Runs complete mock games and accumulates statistics for analysis.
    ///
    /// Two play strategies are available:
    ///   - Random: places cards in a random empty cell (pure Monte Carlo baseline)
    ///   - Greedy: places card + cell that maximises immediate captures (mirrors the
    ///             production AI in GameBoard.RunAI)
    ///
    /// Usage — run 1000 games and inspect the results:
    ///   var results = GameSimulator.RunBatch(
    ///       p1Factory: () => MyDeck(),
    ///       p2Factory: () => EnemyDeck(),
    ///       games: 1000,
    ///       p1Strategy: Strategy.Greedy,
    ///       p2Strategy: Strategy.Greedy,
    ///       config: MatchConfig.BaseRules());
    ///   Console.WriteLine(results.Summary());
    /// </summary>
    public static class GameSimulator
    {
        public enum Strategy { Random, Greedy }

        // ── Single game ────────────────────────────────────────────────────────

        public class GameResult
        {
            public int    Winner        { get; set; }   // 1, 2, or 0 (draw)
            public int    P1FinalScore  { get; set; }
            public int    P2FinalScore  { get; set; }
            public int    TurnsPlayed   { get; set; }
            public int    TotalCaptures { get; set; }   // captures across the whole game
            public bool   StandoffOccurred { get; set; }
        }

        public static GameResult RunGame(
            List<CardData>  p1Deck,
            List<CardData>  p2Deck,
            Strategy        p1Strategy = Strategy.Greedy,
            Strategy        p2Strategy = Strategy.Greedy,
            MatchConfig?    config     = null,
            System.Random?  rng        = null)
        {
            rng    ??= new System.Random();
            config ??= new MatchConfig();

            var gm = new GameManager(config);
            gm.DealHands(p1Deck, p2Deck);

            int  totalCaptures    = 0;
            int  turnsPlayed      = 0;
            bool standoffOccurred = false;

            while (!gm.GameOver)
            {
                int      playerId = gm.CurrentPlayerId;
                var      hand     = gm.GetHand(playerId);
                Strategy strategy = playerId == 1 ? p1Strategy : p2Strategy;

                if (hand.Count == 0) break;

                var (handIdx, row, col) = ChooseMove(gm.Board, hand, strategy, rng);
                var captured            = gm.PlayCard(handIdx, row, col);

                if (captured != null)
                    totalCaptures += captured.Count;

                turnsPlayed++;

                if (gm.StandoffTriggered)
                {
                    standoffOccurred = true;
                    // Rebuild hands from board state exactly as GameBoard does
                    RebuildHandsAfterStandoff(gm);
                }
            }

            return new GameResult
            {
                Winner          = gm.Board.GetScore(1) > gm.Board.GetScore(2) ? 1
                                : gm.Board.GetScore(2) > gm.Board.GetScore(1) ? 2
                                : 0,
                P1FinalScore    = gm.Board.GetScore(1),
                P2FinalScore    = gm.Board.GetScore(2),
                TurnsPlayed     = turnsPlayed,
                TotalCaptures   = totalCaptures,
                StandoffOccurred = standoffOccurred,
            };
        }

        // ── Batch run ──────────────────────────────────────────────────────────

        public class BatchResult
        {
            public int Games        { get; set; }
            public int P1Wins       { get; set; }
            public int P2Wins       { get; set; }
            public int Draws        { get; set; }
            public int Standoffs    { get; set; }

            public double P1WinRate  => Games == 0 ? 0 : (double)P1Wins  / Games;
            public double P2WinRate  => Games == 0 ? 0 : (double)P2Wins  / Games;
            public double DrawRate   => Games == 0 ? 0 : (double)Draws   / Games;

            public double AvgP1Score     { get; set; }
            public double AvgP2Score     { get; set; }
            public double AvgCaptures    { get; set; }
            public double AvgTurns       { get; set; }

            // Score margin histogram: key = P1score - P2score, value = game count
            public Dictionary<int, int> MarginHistogram { get; set; } = new();

            public string Summary()
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"=== Simulation Results ({Games} games) ===");
                sb.AppendLine($"P1 Win Rate   : {P1WinRate:P1}  ({P1Wins} wins)");
                sb.AppendLine($"P2 Win Rate   : {P2WinRate:P1}  ({P2Wins} wins)");
                sb.AppendLine($"Draw Rate     : {DrawRate:P1}  ({Draws} draws)");
                sb.AppendLine($"Standoffs     : {Standoffs}");
                sb.AppendLine($"Avg P1 Score  : {AvgP1Score:F2}");
                sb.AppendLine($"Avg P2 Score  : {AvgP2Score:F2}");
                sb.AppendLine($"Avg Captures  : {AvgCaptures:F2} per game");
                sb.AppendLine($"Avg Turns     : {AvgTurns:F2}");
                sb.AppendLine();
                sb.AppendLine("Score Margin (P1 - P2) distribution:");
                var sorted = new List<int>(MarginHistogram.Keys);
                sorted.Sort();
                foreach (int margin in sorted)
                {
                    double pct = (double)MarginHistogram[margin] / Games;
                    sb.AppendLine($"  {margin,+4}: {MarginHistogram[margin],5} ({pct:P1})");
                }
                return sb.ToString();
            }
        }

        public static BatchResult RunBatch(
            System.Func<List<CardData>> p1Factory,
            System.Func<List<CardData>> p2Factory,
            int          games      = 1000,
            Strategy     p1Strategy = Strategy.Greedy,
            Strategy     p2Strategy = Strategy.Greedy,
            MatchConfig? config     = null,
            int          seed       = 42)
        {
            var rng    = new System.Random(seed);
            var result = new BatchResult { Games = games };

            double totalP1Score  = 0;
            double totalP2Score  = 0;
            double totalCaptures = 0;
            double totalTurns    = 0;

            for (int i = 0; i < games; i++)
            {
                TestLogger.Clear(); // suppress noise between games

                var game = RunGame(
                    p1Factory(), p2Factory(),
                    p1Strategy, p2Strategy,
                    config?.Clone() ?? new MatchConfig(),
                    rng);

                switch (game.Winner)
                {
                    case 1: result.P1Wins++; break;
                    case 2: result.P2Wins++; break;
                    default: result.Draws++; break;
                }

                if (game.StandoffOccurred) result.Standoffs++;

                totalP1Score  += game.P1FinalScore;
                totalP2Score  += game.P2FinalScore;
                totalCaptures += game.TotalCaptures;
                totalTurns    += game.TurnsPlayed;

                int margin = game.P1FinalScore - game.P2FinalScore;
                result.MarginHistogram.TryGetValue(margin, out int existing);
                result.MarginHistogram[margin] = existing + 1;
            }

            result.AvgP1Score  = totalP1Score  / games;
            result.AvgP2Score  = totalP2Score  / games;
            result.AvgCaptures = totalCaptures / games;
            result.AvgTurns    = totalTurns    / games;

            return result;
        }

        // ── Move selection ─────────────────────────────────────────────────────

        private static (int handIdx, int row, int col) ChooseMove(
            BoardState board, List<CardInstance> hand, Strategy strategy, System.Random rng)
        {
            if (strategy == Strategy.Random)
                return RandomMove(board, hand, rng);
            return GreedyMove(board, hand);
        }

        private static (int handIdx, int row, int col) RandomMove(
            BoardState board, List<CardInstance> hand, System.Random rng)
        {
            var empty = EmptyCells(board);
            int cellIdx = rng.Next(empty.Count);
            int handIdx = rng.Next(hand.Count);
            return (handIdx, empty[cellIdx].row, empty[cellIdx].col);
        }

        private static (int handIdx, int row, int col) GreedyMove(
            BoardState board, List<CardInstance> hand)
        {
            int  bestScore   = -1;
            int  bestHand    = 0;
            int  bestRow     = -1;
            int  bestCol     = -1;
            var  empty       = EmptyCells(board);

            for (int hi = 0; hi < hand.Count; hi++)
            {
                foreach (var (er, ec) in empty)
                {
                    // Pure read-only estimate — never mutates the board.
                    // Counts adjacent enemy cards this card's edges beat.
                    // Sufficient as a greedy heuristic; avoids board corruption.
                    int score = EstimateCaptures(board, hand[hi], er, ec);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestHand  = hi;
                        bestRow   = er;
                        bestCol   = ec;
                    }
                }
            }

            // Fallback: first empty cell, first card
            if (bestRow == -1 && empty.Count > 0)
            {
                bestRow = empty[0].row;
                bestCol = empty[0].col;
            }

            return (bestHand, bestRow, bestCol);
        }

        /// <summary>
        /// Read-only capture estimate. Counts adjacent enemy cards whose
        /// defending edge the card beats. Does not place the card or mutate the board.
        /// </summary>
        private static int EstimateCaptures(BoardState board, CardInstance card, int row, int col)
        {
            int count = 0;
            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                var (nRow, nCol) = board.GetNeighbor(row, col, dir);
                if (!board.IsInBounds(nRow, nCol)) continue;
                var neighbor = board.GetCard(nRow, nCol);
                if (neighbor == null || neighbor.OwnerId == card.OwnerId) continue;
                if (card.GetValue(dir) > neighbor.GetValue(card.Data.Opposite(dir)))
                    count++;
            }
            return count;
        }

        private static List<(int row, int col)> EmptyCells(BoardState board)
        {
            var cells = new List<(int, int)>();
            for (int r = 0; r < BoardState.Size; r++)
                for (int c = 0; c < BoardState.Size; c++)
                    if (board.IsEmpty(r, c))
                        cells.Add((r, c));
            return cells;
        }

        private static void RebuildHandsAfterStandoff(GameManager gm)
        {
            // Mirrors GameBoard.HandleStandoff — collect board cards by owner,
            // clear them, then re-deal. GameManager doesn't expose a public
            // method for this, so we use DealHands with the board-state cards.
            var p1Cards = new List<CardData>();
            var p2Cards = new List<CardData>();

            for (int r = 0; r < BoardState.Size; r++)
                for (int c = 0; c < BoardState.Size; c++)
                {
                    var card = gm.Board.GetCard(r, c);
                    if (card == null) continue;
                    if (card.OwnerId == 1) p1Cards.Add(card.Data);
                    else                   p2Cards.Add(card.Data);
                }

            // Re-deal — GameManager.DealHands appends to existing hands, so we
            // rely on the fact that Standoff only triggers on a full board
            // (hands are empty). This is safe.
            gm.DealHands(p1Cards, p2Cards);
        }
    }

    /// <summary>Extension so MatchConfig can be cloned per game in a batch run.</summary>
    internal static class MatchConfigExtensions
    {
        public static MatchConfig Clone(this MatchConfig src) => new MatchConfig
        {
            Protocols    = new List<IProtocol>(src.Protocols),
            Intercept    = src.Intercept,
            Conscription = src.Conscription,
            Standoff     = src.Standoff,
            Cascade      = src.Cascade,
        };
    }
}