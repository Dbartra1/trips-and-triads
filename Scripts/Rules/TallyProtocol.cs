using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The Tally (adapts: Plus) — when a placed card touches two enemy cards and
	/// the two contact-pair SUMS are equal (within sumTolerance), both touched cards
	/// are captured regardless of individual values.
	///
	/// sumTolerance = 0  → exact sum equality (Scale-10 default).
	/// sumTolerance = 2  → fires when |sum1 – sum2| ≤ 2 (Scale-20, Path A confirmed).
	///
	/// Lore: Lacquer counts what you owe. Two debts that add to the same figure
	/// are the same debt — and Lacquer collects both.
	/// Faction fingerprint: Lacquer. Plays into Sumi's compounding perfectly.
	/// </summary>
	public class TallyProtocol : IProtocol
	{
		public string Name => "The Tally";

		/// <summary>
		/// How close two contact sums must be to count as a match.
		/// 0 = exact equality (default, Scale-10).
		/// 2 = within ±2 (Scale-20, Path A confirmed).
		/// </summary>
		private readonly int _sumTolerance;

		public TallyProtocol(int sumTolerance = 0)
		{
			_sumTolerance = sumTolerance;
		}

		public List<(int row, int col)> Resolve(
			BoardState board, CardInstance placed,
			int row, int col,
			HashSet<(int, int)> alreadyCaptured)
		{
			var captured = new List<(int row, int col)>();

			// Collect all enemy contact pairs and their sums
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

			// Find any pair of contact sums within tolerance — capture both
			for (int i = 0; i < contacts.Count; i++)
			{
				for (int j = i + 1; j < contacts.Count; j++)
				{
					if (System.Math.Abs(contacts[i].sum - contacts[j].sum) > _sumTolerance) continue;

					foreach (var (sum, tr, tc) in new[] { contacts[i], contacts[j] })
					{
						if (alreadyCaptured.Contains((tr, tc))) continue;
						if (captured.Exists(c => c.row == tr && c.col == tc)) continue;

						var target = board.GetCard(tr, tc);
						if (target == null) continue;
						target.OwnerId = placed.OwnerId;
						captured.Add((tr, tc));
						Log.Print($"The Tally — {target.Data.Name} captured (sum={sum}, tolerance={_sumTolerance}).");
					}
				}
			}

			return captured;
		}
	}
}
