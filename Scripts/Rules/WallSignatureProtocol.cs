using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Wall Signature (adapts: Same Wall) — for the purpose of Handshake,
	/// the edge of the board counts as a value of A (10).
	/// A card pressed against the board edge can tie WITH the wall itself,
	/// and the wall has no loyalty — so the tie resolves to whoever placed the card.
	///
	/// Wall Signature only adds value when combined with Handshake.
	/// It extends the Handshake by counting board-edge contacts as value-10 ties.
	///
	/// Lore: The perimeter of a sanctioned board is a hard system boundary.
	/// The edge of the world is on nobody's side.
	/// Faction fingerprint: Ascendant — corporate infrastructure, clean hard boundary.
	/// </summary>
	public class WallSignatureProtocol : IProtocol
	{
		public string Name => "Wall Signature";

		private const int WallValue = 10; // A

		public List<(int row, int col)> Resolve(
			BoardState board, CardInstance placed,
			int row, int col,
			HashSet<(int, int)> alreadyCaptured)
		{
			var captured = new List<(int row, int col)>();

			// Collect all contact sums — including board edges counted as WallValue
			var contacts = new List<(int sum, int? nRow, int? nCol)>();

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				int attackVal    = placed.GetValue(dir);

				if (!board.IsInBounds(nRow, nCol))
				{
					// Board edge — counts as WallValue for Handshake tie purposes
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

			// Find matching sums — if any two contacts (including wall contacts) share
			// a sum, capture all real enemy cards in those matching contacts
			for (int i = 0; i < contacts.Count; i++)
			{
				for (int j = i + 1; j < contacts.Count; j++)
				{
					if (contacts[i].sum != contacts[j].sum) continue;

					// Capture any real (non-wall) cards in these contacts
					foreach (var (sum, tr, tc) in new[] { contacts[i], contacts[j] })
					{
						if (tr == null || tc == null) continue; // wall contact, skip
						if (alreadyCaptured.Contains(((int)tr, (int)tc))) continue;
						if (captured.Exists(c => c.row == tr && c.col == tc)) continue;

						var target = board.GetCard((int)tr, (int)tc);
						if (target == null) continue;

						target.OwnerId = placed.OwnerId;
						captured.Add(((int)tr, (int)tc));
						GD.Print($"Wall Signature — {target.Data.Name} captured " +
						         $"(wall-extended sum={sum}).");
					}
				}
			}

			return captured;
		}
	}
}
