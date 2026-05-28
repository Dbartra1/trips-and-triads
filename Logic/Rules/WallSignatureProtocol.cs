using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class WallSignatureProtocol : IProtocol
	{
		public string Name => "Wall Signature";

		private const int WallValue = 10;

		public List<(int row, int col)> Resolve(
			BoardState board, CardInstance placed,
			int row, int col,
			HashSet<(int, int)> alreadyCaptured)
		{
			var captured = new List<(int row, int col)>();
			var contacts = new List<(int sum, int? nRow, int? nCol)>();

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				int attackVal    = placed.GetValue(dir);

				if (!board.IsInBounds(nRow, nCol))
				{
					contacts.Add((attackVal + WallValue, null, null));
					continue;
				}

				var neighbor = board.GetCard(nRow, nCol);
				if (neighbor == null) continue;
				if (neighbor.OwnerId == placed.OwnerId) continue;
				if (alreadyCaptured.Contains((nRow, nCol))) continue;

				int defendVal = neighbor.GetValue(placed.Data.Opposite(dir));
				contacts.Add((attackVal + defendVal, nRow, nCol));
			}

			for (int i = 0; i < contacts.Count; i++)
			{
				for (int j = i + 1; j < contacts.Count; j++)
				{
					if (contacts[i].sum != contacts[j].sum) continue;

					foreach (var (sum, tr, tc) in new[] { contacts[i], contacts[j] })
					{
						if (tr == null || tc == null) continue;
						if (alreadyCaptured.Contains(((int)tr, (int)tc))) continue;
						if (captured.Exists(c => c.row == tr && c.col == tc)) continue;

						var target = board.GetCard((int)tr, (int)tc);
						if (target == null) continue;

						target.OwnerId = placed.OwnerId;
						captured.Add(((int)tr, (int)tc));
						TestLogger.Log($"Wall Signature — {target.Data.Name} captured " +
						               $"(wall-extended sum={sum}).");
					}
				}
			}

			return captured;
		}
	}
}
