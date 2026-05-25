using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class CaptureResolver
	{
		public List<(int row, int col)> Resolve(BoardState board, int row, int col)
		{
			var allCaptured = new List<(int row, int col)>();
			var visited     = new HashSet<(int, int)>();

			visited.Add((row, col));

			var placed = board.GetCard(row, col);
			if (placed == null) return allCaptured;

			// ── The Rivalry ───────────────────────────────────────────────────────
			// If this card has RivalryActive and the rival is adjacent, resolve
			// capture between just those two cards first before normal capture.
			if (placed.RivalryActive)
				ResolveRivalry(board, placed, row, col, allCaptured, visited);

			// ── Normal capture pass ───────────────────────────────────────────────
			var initial = ResolveSingle(board, row, col, placed);
			allCaptured.AddRange(initial);

			// ── The Breach ────────────────────────────────────────────────────────
			// If placed adjacent to a friendly Vesna, initial captures chain.
			bool breachActive = IsAdjacentToVesna(board, row, col, placed.OwnerId);
			if (breachActive)
				foreach (var (cr, cc) in initial)
				{
					visited.Add((cr, cc));
					ResolveChain(board, cr, cc, allCaptured, visited);
				}

			return allCaptured;
		}

		// ── The Rivalry ───────────────────────────────────────────────────────────
		// Yune and Grin resolve capture against each other first when adjacent.
		// Whichever wins flips the other; then normal capture continues.
		private void ResolveRivalry(
			BoardState board, CardInstance placed, int row, int col,
			List<(int, int)> allCaptured, HashSet<(int, int)> visited)
		{
			string rivalId = placed.Data.Id == "asc_hero_seraph_yune"
				? "rzk_hero_sister_grin"
				: "asc_hero_seraph_yune";

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var neighbor = board.GetCard(nRow, nCol);
				if (neighbor == null || neighbor.Data.Id != rivalId) continue;
				if (neighbor.OwnerId == placed.OwnerId) continue;

				int attackVal = placed.GetValue(dir);
				int defendVal = neighbor.GetValue(placed.Data.Opposite(dir));

				GD.Print($"The Rivalry — {placed.Data.Name}({attackVal}) vs " +
				         $"{neighbor.Data.Name}({defendVal}).");

				if (attackVal > defendVal)
				{
					neighbor.OwnerId = placed.OwnerId;
					allCaptured.Add((nRow, nCol));
					visited.Add((nRow, nCol));
					GD.Print($"The Rivalry — {placed.Data.Name} wins.");
				}
				else
				{
					GD.Print($"The Rivalry — {neighbor.Data.Name} holds.");
				}
				break; // only one rival can be adjacent
			}
		}

		// ── Chain resolver (The Breach) ───────────────────────────────────────────
		private void ResolveChain(
			BoardState board, int row, int col,
			List<(int, int)> allCaptured, HashSet<(int, int)> visited)
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
				ResolveChain(board, nr, nc, allCaptured, visited);
			}
		}

		// ── Single capture pass ───────────────────────────────────────────────────
		// Respects BlockChoir (The Listener) — Riven will not capture Choir cards.
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

				// The Listener — Riven refuses to capture Choir cards
				if (attacker.BlockChoir && neighbor.Data.Faction == Faction.HollowChoir)
				{
					GD.Print($"The Listener — Riven will not capture {neighbor.Data.Name}.");
					continue;
				}

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

		// ── Helpers ───────────────────────────────────────────────────────────────
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