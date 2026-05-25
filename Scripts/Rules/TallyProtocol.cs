using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The Tally (adapts: Plus) — when a placed card touches two enemy cards and
	/// the two contact-pair SUMS are equal, both touched cards are captured
	/// regardless of individual values.
	///
	/// Lore: Lacquer counts what you owe. Two debts that add to the same figure
	/// are the same debt — and Lacquer collects both.
	/// Faction fingerprint: Lacquer. Plays into Sumi's compounding perfectly.
	/// </summary>
	public class TallyProtocol : IProtocol
	{
		public string Name => "The Tally";

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

			// Find any sum that appears two or more times — those contacts are captured
			for (int i = 0; i < contacts.Count; i++)
			{
				for (int j = i + 1; j < contacts.Count; j++)
				{
					if (contacts[i].sum != contacts[j].sum) continue;

					// Both contacts share the same sum — capture them
					foreach (var (sum, tr, tc) in new[] { contacts[i], contacts[j] })
					{
						if (alreadyCaptured.Contains((tr, tc))) continue;
						if (captured.Exists(c => c.row == tr && c.col == tc)) continue;

						var target = board.GetCard(tr, tc);
						if (target == null) continue;
						target.OwnerId = placed.OwnerId;
						captured.Add((tr, tc));
						GD.Print($"The Tally — {target.Data.Name} captured (sum={sum}).");
					}
				}
			}

			return captured;
		}
	}
}
