using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class SumiAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col) { }

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			card.AdjustAllEdges(+1);
			TestLogger.Log($"Sumi compounds — base now {card.GetBaseValue(Direction.Top)}/" +
			               $"{card.GetBaseValue(Direction.Right)}/" +
			               $"{card.GetBaseValue(Direction.Bottom)}/" +
			               $"{card.GetBaseValue(Direction.Left)}");

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var adj = board.GetCard(nRow, nCol);
				if (adj == null || adj.OwnerId != card.OwnerId) continue;

				adj.AdjustAllEdges(+1);
				TestLogger.Log($"Ledger: {adj.Data.Name} compounds +1 via Sumi's Ledger.");
			}

			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					var heir = board.GetCard(r, c);
					if (heir == null) continue;
					if (heir.Data.Id != "lac_top_the_heir") continue;
					if (heir.OwnerId != card.OwnerId) continue;

					heir.AdjustAllEdges(+1);
					TestLogger.Log("The Inheritance — The Heir compounds +1 alongside Sumi.");
				}
		}
	}
}
