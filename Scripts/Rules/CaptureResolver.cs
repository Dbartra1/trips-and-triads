using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class CaptureResolver
	{
		// Returns list of (row, col) positions that were captured.
		// If the placed card is adjacent to a friendly Vesna, any capture chains:
		// the captured card re-attacks its own neighbors under its new owner.
		public List<(int row, int col)> Resolve(BoardState board, int row, int col)
		{
			var allCaptured = new List<(int row, int col)>();
			var visited     = new HashSet<(int, int)>();

			// Mark the originally placed cell as visited so chains never loop back to it
			visited.Add((row, col));

			// Perform the initial capture pass
			var placed = board.GetCard(row, col);
			if (placed == null) return allCaptured;

			var initial = ResolveSingle(board, row, col, placed);
			allCaptured.AddRange(initial);

			// Check if this placement is adjacent to a friendly Vesna
			bool breachActive = IsAdjacentToVesna(board, row, col, placed.OwnerId);

			if (breachActive)
			{
				// For each initially captured card, chain-resolve outward
				foreach (var (cr, cc) in initial)
				{
					visited.Add((cr, cc));
					ResolveChain(board, cr, cc, allCaptured, visited);
				}
			}

			return allCaptured;
		}

		// Recursive chain resolver — the captured card re-attacks its neighbors.
		// Stops when no new captures occur or all reachable cards are visited.
		private void ResolveChain(
			BoardState board,
			int row, int col,
			List<(int, int)> allCaptured,
			HashSet<(int, int)> visited)
		{
			var card = board.GetCard(row, col);
			if (card == null) return;

			var newCaptures = ResolveSingle(board, row, col, card);

			foreach (var (nr, nc) in newCaptures)
			{
				if (visited.Contains((nr, nc))) continue;

				allCaptured.Add((nr, nc));
				visited.Add((nr, nc));

				GD.Print($"The Breach chains: {board.GetCard(nr, nc)?.Data.Name} captured at ({nr},{nc}).");

				// Recurse — the newly captured card re-attacks its own neighbors
				ResolveChain(board, nr, nc, allCaptured, visited);
			}
		}

		// Single-card capture pass — compares the card at (row,col) against all neighbors.
		// Does not recurse. Returns newly captured positions.
		private List<(int row, int col)> ResolveSingle(
			BoardState board, int row, int col, CardInstance attacker)
		{
			var captured = new List<(int row, int col)>();

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);

				if (!board.IsInBounds(nRow, nCol)) continue;

				var neighbor = board.GetCard(nRow, nCol);
				if (neighbor == null) continue;
				if (neighbor.OwnerId == attacker.OwnerId) continue;

				int attackVal = attacker.GetValue(dir);
				int defendVal = neighbor.GetValue(attacker.Data.Opposite(dir));

				if (attackVal > defendVal)
				{
					neighbor.OwnerId = attacker.OwnerId;
					captured.Add((nRow, nCol));
				}
			}

			return captured;
		}

		// Returns true if any orthogonal neighbor of (row,col) is Vesna, owned by ownerId.
		private bool IsAdjacentToVesna(BoardState board, int row, int col, int ownerId)
		{
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var neighbor = board.GetCard(nRow, nCol);
				if (neighbor == null) continue;
				if (neighbor.OwnerId != ownerId) continue;
				if (neighbor.Data.Id == "hch_hero_vesna") return true;
			}
			return false;
		}
	}
}