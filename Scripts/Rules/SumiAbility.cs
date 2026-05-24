using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The Matriarch — starts 4/4/4/4 with no A, compounds +1 to all edges
	/// at the end of each of her owner's turns. Turn one nobody; turn nine untouchable.
	/// </summary>
	public class SumiAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col)
		{
			// Nothing on placement — she enters demure.
		}

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			card.AdjustAllEdges(+1);
			GD.Print($"Sumi compounds — now {card.GetValue(Direction.Top)}/" +
			         $"{card.GetValue(Direction.Right)}/" +
			         $"{card.GetValue(Direction.Bottom)}/" +
			         $"{card.GetValue(Direction.Left)}");
		}
	}
}
