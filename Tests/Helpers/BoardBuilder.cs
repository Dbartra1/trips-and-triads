using TripsAndTriads.Core;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Tests.Helpers
{
    /// <summary>
    /// Fluent helper for setting up a BoardState with cards already placed.
    /// Handles domain/bond resolution after placement so tests start in a
    /// game-accurate state.
    ///
    /// Usage:
    ///   var (board, placed) = new BoardBuilder()
    ///       .Place(CardFactory.SeraphYune(), row: 0, col: 0)
    ///       .Place(CardFactory.Street("Enemy", 5,5,5,5, owner: 2), row: 0, col: 1)
    ///       .Build();
    ///
    /// The returned `placed` list is in placement order — useful for asserting
    /// the card you just placed triggers the right captures.
    /// </summary>
    public class BoardBuilder
    {
        private readonly BoardState _board = new BoardState();
        private readonly System.Collections.Generic.List<(CardInstance card, int row, int col)>
            _placements = new();

        public BoardBuilder Place(CardInstance card, int row, int col)
        {
            _board.PlaceCard(card, row, col);
            card.Ability?.OnPlaced(_board, card, row, col);
            _placements.Add((card, row, col));
            return this;
        }

        /// <summary>
        /// Finalises the board by resolving all domain and bond bonuses.
        /// Returns the board and the ordered placement list.
        /// </summary>
        public (BoardState board,
                System.Collections.Generic.List<(CardInstance card, int row, int col)> placements)
            Build()
        {
            DomainResolver.Apply(_board);
            BondResolver.Apply(_board);
            return (_board, _placements);
        }

        /// <summary>
        /// Quick helper — returns just the board after resolving bonuses.
        /// </summary>
        public BoardState BuildBoard()
        {
            DomainResolver.Apply(_board);
            BondResolver.Apply(_board);
            return _board;
        }
    }
}
