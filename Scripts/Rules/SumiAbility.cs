using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The Matriarch — starts 4/4/4/4, compounds +1 to all her own edges each turn.
	/// The Ledger: adjacent friendly cards also gain +1 to all edges per turn.
	/// The Inheritance: if The Heir is on the board, it also compounds +1/turn.
	/// </summary>
	public class SumiAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col) { }

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			// Sumi compounds herself
			card.AdjustAllEdges(+1);
			GD.Print($"Sumi compounds — base now {card.GetBaseValue(Direction.Top)}/" +
			         $"{card.GetBaseValue(Direction.Right)}/" +
			         $"{card.GetBaseValue(Direction.Bottom)}/" +
			         $"{card.GetBaseValue(Direction.Left)}");

			// The Ledger — adjacent friendly cards compound +1 permanently
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var adj = board.GetCard(nRow, nCol);
				if (adj == null || adj.OriginalOwnerId != card.OriginalOwnerId) continue;

				adj.AdjustAllEdges(+1);
				GD.Print($"Ledger: {adj.Data.Name} compounds +1 via Sumi's Ledger.");
			}

			// The Inheritance — The Heir also compounds if on the board (any position)
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					var heir = board.GetCard(r, c);
					if (heir == null) continue;
					if (heir.Data.Id != "lac_top_the_heir") continue;
					if (heir.OriginalOwnerId != card.OriginalOwnerId) continue;

					heir.AdjustAllEdges(+1);
					GD.Print($"The Inheritance — The Heir compounds +1 alongside Sumi.");
				}
		}
	}
}