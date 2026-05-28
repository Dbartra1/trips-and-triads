namespace TripsAndTriads.Rules
{
	public interface ICardAbility
	{
		void OnPlaced(TripsAndTriads.Core.BoardState board,
		              TripsAndTriads.Core.CardInstance card,
		              int row, int col);

		void OnTurnEnd(TripsAndTriads.Core.BoardState board,
		               TripsAndTriads.Core.CardInstance card,
		               int row, int col);
	}
}
