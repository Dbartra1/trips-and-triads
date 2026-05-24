using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class CaptureResolver
	{
		// Returns list of (row, col) positions that were captured.
		public List<(int row, int col)> Resolve(BoardState board, int row, int col)
		{
			var captured = new List<(int, int)>();
			var placed   = board.GetCard(row, col);

			if (placed == null) return captured;

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);

				if (!board.IsInBounds(nRow, nCol)) continue;

				var neighbor = board.GetCard(nRow, nCol);

				if (neighbor == null) continue;
				if (neighbor.OwnerId == placed.OwnerId) continue;

				// Use CardInstance.GetValue() so per-instance overrides (Vesna decay,
				// Sumi compound) are respected. Never read Data.GetValue() directly.
				int attackVal = placed.GetValue(dir);
				int defendVal = neighbor.GetValue(placed.Data.Opposite(dir));

				if (attackVal > defendVal)
				{
					neighbor.OwnerId = placed.OwnerId;
					captured.Add((nRow, nCol));
				}
			}

			return captured;
		}
	}
}