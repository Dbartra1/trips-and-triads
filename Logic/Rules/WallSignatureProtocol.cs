using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class WallSignatureProtocol : IProtocol
	{
		public string Name => "Wall Signature";

		/// <summary>
		/// The value a board edge contributes to sum matching.
		/// 10 = Scale-10 default. 20 = Scale-20.
		/// </summary>
		private readonly int _wallValue;

		/// <summary>
		/// How close two sums must be to match.
		/// 0 = exact equality (default). 2 = within ±2 (Scale-20).
		/// </summary>
		private readonly int _sumTolerance;

		public WallSignatureProtocol(int wallValue = 10, int sumTolerance = 0)
		{
			_wallValue    = wallValue;
			_sumTolerance = sumTolerance;
		}

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
					contacts.Add((attackVal + _wallValue, null, null));
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
					if (System.Math.Abs(contacts[i].sum - contacts[j].sum) > _sumTolerance) continue;

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
						               $"(sum={sum}, wallValue={_wallValue}, tolerance={_sumTolerance}).");
					}
				}
			}

			return captured;
		}
	}
}