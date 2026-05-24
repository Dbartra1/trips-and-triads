namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Implemented by any card with a special ability.
	/// GameManager calls OnPlaced immediately after the card hits the board,
	/// and OnTurnEnd at the end of every turn belonging to the card's owner.
	/// </summary>
	public interface ICardAbility
	{
		/// <summary>Called once, immediately after this card is placed on the board.</summary>
		void OnPlaced(TripsAndTriads.Core.BoardState board,
		              TripsAndTriads.Core.CardInstance card,
		              int row, int col);

		/// <summary>Called at the end of each turn belonging to this card's owner.</summary>
		void OnTurnEnd(TripsAndTriads.Core.BoardState board,
		               TripsAndTriads.Core.CardInstance card,
		               int row, int col);
	}
}
