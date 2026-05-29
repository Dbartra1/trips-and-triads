using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Handshake (adapts: Same) — when a placed card touches two or more enemy cards
	/// and an edge value is EQUAL (within tolerance) on any of those contacts, every
	/// card it ties with is captured regardless of who is higher.
	///
	/// tolerance = 0  → exact equality (Scale-10 default).
	/// tolerance = 2  → fires when |attack – defend| ≤ 2 (Scale-20, Path A).
	///
	/// Lore: Two identical signatures break the city's identity system.
	/// Faction fingerprint: Effigy (point-symmetric stats make ties far more likely).
	/// </summary>
	public class HandshakeProtocol : IProtocol
	{
		public string Name => "Handshake";

		/// <summary>
		/// How close two edges must be to count as a "tie".
		/// 0 = exact equality (default, Scale-10).
		/// 2 = within ±2 (Scale-20, Path A confirmed).
		/// </summary>
		private readonly int _tolerance;

		public HandshakeProtocol(int tolerance = 0)
		{
			_tolerance = tolerance;
		}

		public List<(int row, int col)> Resolve(
			BoardState board, CardInstance placed,
			int row, int col,
			HashSet<(int, int)> alreadyCaptured)
		{
			var captured      = new List<(int row, int col)>();
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

				if (System.Math.Abs(attackVal - defendVal) <= _tolerance)
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
					GD.Print($"Handshake — {target.Data.Name} captured by tied edges (tolerance={_tolerance}).");
				}
			}

			return captured;
		}
	}
}
