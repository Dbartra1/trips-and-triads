using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Handshake (adapts: Same) — when a placed card touches two or more enemy cards
	/// and an edge value is EQUAL on any of those contacts, every card it ties with
	/// is captured regardless of who is higher.
	///
	/// Lore: Two identical signatures break the city's identity system.
	/// A tie isn't a stalemate — it's a collision of identity.
	/// Faction fingerprint: Effigy (point-symmetric stats make ties far more likely).
	/// </summary>
	public class HandshakeProtocol : IProtocol
	{
		public string Name => "Handshake";

		public List<(int row, int col)> Resolve(
			BoardState board, CardInstance placed,
			int row, int col,
			HashSet<(int, int)> alreadyCaptured)
		{
			var captured = new List<(int row, int col)>();

			// Count how many enemy contacts tie with this card
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

			// Handshake only triggers if there are two or more tie contacts
			if (tiedPositions.Count >= 2)
			{
				foreach (var (tr, tc) in tiedPositions)
				{
					var target = board.GetCard(tr, tc);
					if (target == null) continue;
					target.OwnerId = placed.OwnerId;
					captured.Add((tr, tc));
					GD.Print($"Handshake — {target.Data.Name} captured by tied edges.");
				}
			}

			return captured;
		}
	}
}
