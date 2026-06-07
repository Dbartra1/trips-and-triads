using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;
using static TripsAndTriads.Core.AbilityType;

namespace TripsAndTriads.Rules
{
	public class CaptureResolver
	{
		private MatchConfig _config;

		public CaptureResolver(MatchConfig config = null)
		{
			_config = config ?? new MatchConfig();
		}

		public List<(int row, int col)> Resolve(BoardState board, int row, int col)
			=> Resolve(board, row, col, null);

		public List<(int row, int col)> Resolve(
			BoardState board, int row, int col,
			List<CaptureEvent> events)
		{
			var allCaptured = new List<(int row, int col)>();
			var visited     = new HashSet<(int, int)>();
			visited.Add((row, col));

			var placed = board.GetCard(row, col);
			if (placed == null) return allCaptured;

			// ── The Rivalry ───────────────────────────────────────────────────────
			if (placed.RivalryActive)
				ResolveRivalry(board, placed, row, col, allCaptured, visited, events);

			// ── Base capture ──────────────────────────────────────────────────────
			var baseCaptured = ResolveSingle(board, row, col, placed, visited, events, "");
			allCaptured.AddRange(baseCaptured);
			foreach (var pos in baseCaptured) visited.Add(pos);

			// ── Protocol captures ─────────────────────────────────────────────────
			var protocolCaptured = new List<(int row, int col)>();
			foreach (var protocol in _config.Protocols)
			{
				var result = protocol.Resolve(board, placed, row, col, visited);
				foreach (var pos in result)
				{
					if (visited.Contains(pos)) continue;
					protocolCaptured.Add(pos);
					visited.Add(pos);

					// Record protocol capture event
					var captured = board.GetCard(pos.row, pos.col);
					if (captured != null && events != null)
						events.Add(new CaptureEvent(
							captured.Data.Name, captured.Data.Faction,
							placed.Data.Name,   placed.Data.Faction,
							0, 0, Direction.Top,
							"", protocol.Name));
				}
			}

			// ── Overflow ───────────────────────────────────────────────────────────
			if (_config.Overflow && protocolCaptured.Count > 0)
			{
				foreach (var (cr, cc) in protocolCaptured)
				{
					GD.Print($"Overflow triggered from protocol capture at ({cr},{cc}).");
					ResolveChain(board, cr, cc, allCaptured, visited, events, "Overflow");
				}
			}

			allCaptured.AddRange(protocolCaptured);

			// ── The Breach ────────────────────────────────────────────────────────
			if (IsAdjacentToVesna(board, row, col, placed.OwnerId))
				foreach (var (cr, cc) in baseCaptured)
				{
					if (!visited.Contains((cr, cc))) visited.Add((cr, cc));
					ResolveChain(board, cr, cc, allCaptured, visited, events, "The Breach");
				}

			return allCaptured;
		}

		// ── The Rivalry ───────────────────────────────────────────────────────────
		private void ResolveRivalry(
			BoardState board, CardInstance placed, int row, int col,
			List<(int, int)> allCaptured, HashSet<(int, int)> visited,
			List<CaptureEvent> events)
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
					events?.Add(new CaptureEvent(
						neighbor.Data.Name, neighbor.Data.Faction,
						placed.Data.Name,   placed.Data.Faction,
						attackVal, defendVal, dir,
						"", "The Rivalry"));
					GD.Print($"The Rivalry — {placed.Data.Name} wins.");
				}
				else
				{
					GD.Print($"The Rivalry — {neighbor.Data.Name} holds.");
				}
				break;
			}
		}

		// ── Chain resolver (The Breach / Overflow) ─────────────────────────────────
		private void ResolveChain(
			BoardState board, int row, int col,
			List<(int, int)> allCaptured, HashSet<(int, int)> visited,
			List<CaptureEvent> events, string protocolNote)
		{
			var card = board.GetCard(row, col);
			if (card == null) return;

			var newCaptures = ResolveSingle(board, row, col, card, visited, events, protocolNote);

			foreach (var (nr, nc) in newCaptures)
			{
				if (visited.Contains((nr, nc))) continue;
				allCaptured.Add((nr, nc));
				visited.Add((nr, nc));
				GD.Print($"Chain: {board.GetCard(nr, nc)?.Data.Name} captured at ({nr},{nc}).");
				ResolveChain(board, nr, nc, allCaptured, visited, events, protocolNote);
			}
		}

		// ── Single capture pass ───────────────────────────────────────────────────
		private List<(int row, int col)> ResolveSingle(
			BoardState board, int row, int col,
			CardInstance attacker, HashSet<(int, int)> visited,
			List<CaptureEvent> events, string protocolNote)
		{
			var captured = new List<(int row, int col)>();

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var neighbor = board.GetCard(nRow, nCol);
				if (neighbor == null) continue;
				if (neighbor.OwnerId == attacker.OwnerId) continue;
				if (visited.Contains((nRow, nCol))) continue;

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

					// Build domain note from the bonus delta
					string domainNote = BuildDomainNote(attacker, dir);

					events?.Add(new CaptureEvent(
						neighbor.Data.Name, neighbor.Data.Faction,
						attacker.Data.Name,  attacker.Data.Faction,
						attackVal, defendVal, dir,
						domainNote, protocolNote));
				}
			}

			return captured;
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		private static string BuildDomainNote(CardInstance attacker, Direction dir)
		{
			int bonus = dir switch
			{
				Direction.Top    => attacker.DomainBonusTop    + attacker.BondBonusTop,
				Direction.Right  => attacker.DomainBonusRight  + attacker.BondBonusRight,
				Direction.Bottom => attacker.DomainBonusBottom + attacker.BondBonusBottom,
				Direction.Left   => attacker.DomainBonusLeft   + attacker.BondBonusLeft,
				_                => 0
			};
			return bonus > 0 ? $"aura +{bonus}" : "";
		}

		private bool IsAdjacentToVesna(BoardState board, int row, int col, int ownerId)
		{
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(row, col, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var neighbor = board.GetCard(nRow, nCol);
				if (neighbor == null) continue;
				if (neighbor.OwnerId != ownerId) continue;
				if (neighbor.Data.AbilityType == AbilityType.Decay) return true;
			}
			return false;
		}
	}
}