using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The Matriarch — starts 4/4/4/4, compounds +1 to all her own edges each turn.
	/// The Ledger: adjacent friendly cards also gain +1 to all edges per turn
	/// (goes into their override layer, so it accumulates — the debt spreads).
	/// </summary>
	public class SumiAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col)
		{
			// Nothing on placement — she enters demure.
		}

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			// Sumi compounds herself
			card.AdjustAllEdges(+1);
			GD.Print($"Sumi compounds — now {card.GetValue(Direction.Top)}/" +
			         $"{card.GetValue(Direction.Right)}/" +
			         $"{card.GetValue(Direction.Bottom)}/" +
			         $"{card.GetValue(Direction.Left)}");

			// The Ledger — adjacent friendly cards also compound +1 (cumulative)
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var adj = board.GetCard(nRow, nCol);
				if (adj == null || adj.OwnerId != card.OwnerId) continue;

				adj.AdjustAllEdges(+1);
				GD.Print($"Ledger: {adj.Data.Name} compounds +1 via Sumi's Ledger.");
			}
		}
	}
}
