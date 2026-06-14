using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Wall Signature (adapts: Same Wall) — the board edge counts as wallValue for
	/// sum matching. A card pressed against the edge ties with the wall, and the wall
	/// has no loyalty — so the tie resolves to whoever placed the card.
	///
	/// wallValue   = 10  → Scale-10 default (A = 10).
	/// wallValue   = 20  → Scale-20 (A = 20, Path A confirmed).
	/// sumTolerance = 0  → exact sum equality (default).
	/// sumTolerance = 2  → within ±2 (Scale-20, Path A confirmed).
	///
	/// Lore: The perimeter of a sanctioned board is a hard system boundary.
	/// The edge of the world is on nobody's side.
	/// Faction fingerprint: Ascendant — corporate infrastructure, clean hard boundary.
	/// </summary>
	public class WallSignatureProtocol : IProtocol
	{
		public string Name => "Wall Signature";

		/// <summary>
		/// The value a board edge contributes to sum matching.
		/// 10 = Scale-10 default (A = 10).
		/// 20 = Scale-20 (Path A confirmed).
		/// </summary>
		private readonly int _wallValue;

		/// <summary>
		/// How close two sums must be to match.
		/// 0 = exact equality (default, Scale-10).
		/// 2 = within ±2 (Scale-20, Path A confirmed).
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

			// Collect all contact sums — including board edges counted as _wallValue
			var contacts = new List<(int sum, int? nRow, int? nCol)>();

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				int attackVal    = placed.GetValue(dir);

				if (!board.IsInBounds(nRow, nCol))
				{
					// Board edge — counts as _wallValue for sum matching
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

			// Find matching sums (within tolerance) — capture real enemy cards
			for (int i = 0; i < contacts.Count; i++)
			{
				for (int j = i + 1; j < contacts.Count; j++)
				{
					if (System.Math.Abs(contacts[i].sum - contacts[j].sum) > _sumTolerance) continue;

					foreach (var (sum, tr, tc) in new[] { contacts[i], contacts[j] })
					{
						if (tr == null || tc == null) continue; // wall contact, skip
						if (alreadyCaptured.Contains(((int)tr, (int)tc))) continue;
						if (captured.Exists(c => c.row == tr && c.col == tc)) continue;

						var target = board.GetCard((int)tr, (int)tc);
						if (target == null) continue;

						target.OwnerId = placed.OwnerId;
						captured.Add(((int)tr, (int)tc));
						Log.Print($"Wall Signature — {target.Data.Name} captured " +
						         $"(sum={sum}, wallValue={_wallValue}, tolerance={_sumTolerance}).");
					}
				}
			}

			return captured;
		}
	}
}
