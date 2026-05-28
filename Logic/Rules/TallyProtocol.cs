using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class TallyProtocol : IProtocol
	{
		public string Name => "The Tally";

		public List<(int row, int col)> Resolve(
			BoardState board, CardInstance placed,
			int row, int col,
			HashSet<(int, int)> alreadyCaptured)
		{
			var captured = new List<(int row, int col)>();
			var contacts = new List<(int sum, int nRow, int nCol)>();

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
				int sum       = attackVal + defendVal;

				contacts.Add((sum, nRow, nCol));
			}

			for (int i = 0; i < contacts.Count; i++)
			{
				for (int j = i + 1; j < contacts.Count; j++)
				{
					if (contacts[i].sum != contacts[j].sum) continue;

					foreach (var (sum, tr, tc) in new[] { contacts[i], contacts[j] })
					{
						if (alreadyCaptured.Contains((tr, tc))) continue;
						if (captured.Exists(c => c.row == tr && c.col == tc)) continue;

						var target = board.GetCard(tr, tc);
						if (target == null) continue;
						target.OwnerId = placed.OwnerId;
						captured.Add((tr, tc));
						TestLogger.Log($"The Tally — {target.Data.Name} captured (sum={sum}).");
					}
				}
			}

			return captured;
		}
	}
}
