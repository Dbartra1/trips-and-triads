using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class HandshakeProtocol : IProtocol
	{
		public string Name => "Handshake";

		public List<(int row, int col)> Resolve(
			BoardState board, CardInstance placed,
			int row, int col,
			HashSet<(int, int)> alreadyCaptured)
		{
			var captured     = new List<(int row, int col)>();
			var tiedPositions = new List<(int r, int c)>();

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var neighbor = board.GetCard(nRow, nCol);
				if (neighbor == null) continue;
				if (neighbor.OwnerId == placed.OwnerId) continue;
				if (alreadyCaptured.Contains((nRow, nCol))) continue;

				int attackVal = placed.GetValue(dir);
				int defendVal = neighbor.GetValue(placed.Data.Opposite(dir));

				if (attackVal == defendVal)
					tiedPositions.Add((nRow, nCol));
			}

			if (tiedPositions.Count >= 2)
			{
				foreach (var (tr, tc) in tiedPositions)
				{
					var target = board.GetCard(tr, tc);
					if (target == null) continue;
					target.OwnerId = placed.OwnerId;
					captured.Add((tr, tc));
					TestLogger.Log($"Handshake — {target.Data.Name} captured by tied edges.");
				}
			}

			return captured;
		}
	}
}
