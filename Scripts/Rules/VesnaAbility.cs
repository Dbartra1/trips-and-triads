using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The First Voice — A/A/A/A on placement, then loses 1 from every edge
	/// at the end of each of her owner's turns. Five turns later she is a husk.
	/// </summary>
	public class VesnaAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col)
		{
			// Nothing on placement — she enters at full strength (A/A/A/A from CardData).
		}

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			card.AdjustAllEdges(-1);
			GD.Print($"Vesna decays — now {card.GetValue(Direction.Top)}/" +
			         $"{card.GetValue(Direction.Right)}/" +
			         $"{card.GetValue(Direction.Bottom)}/" +
			         $"{card.GetValue(Direction.Left)}");
		}
	}
}
