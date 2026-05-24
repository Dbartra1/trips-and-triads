using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Computes and applies transient domain bonuses from heroes to adjacent friendly cards.
	/// Must be called after every board state change (placement, capture, ability fire).
	/// Bonuses are reset and recomputed fresh each call — they never accumulate here.
	/// Sumi's Ledger is the exception: it accumulates in the override layer via SumiAbility.
	/// </summary>
	public static class DomainResolver
	{
		public static void Apply(BoardState board)
		{
			// Step 1 — clear all transient bonuses so we start from a clean slate
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
					board.GetCard(r, c)?.ResetDomainBonuses();

			// Step 2 — find every hero and apply their domain
			for (int r = 0; r < BoardState.Size; r++)
			{
				for (int c = 0; c < BoardState.Size; c++)
				{
					var hero = board.GetCard(r, c);
					if (hero == null || hero.Data.Tier != Tier.Hero) continue;

					ApplyDomain(board, hero, r, c);
				}
			}
		}

		private static void ApplyDomain(BoardState board, CardInstance hero, int hr, int hc)
		{
			switch (hero.Data.Id)
			{
				case "asc_hero_seraph_yune": ApplyAegisProtocol(board, hero, hr, hc); break;
				case "rzk_hero_sister_grin": ApplyKillzone(board, hero, hr, hc);      break;
				case "gwi_hero_riven":       ApplyLateralGrid(board, hero, hr, hc);   break;
				case "com_hero_mara_kane":   ApplySprawl(board, hero, hr, hc);        break;
				// Lethe   — domain is her placement mechanic (LetheAbility), no aura
				// Sumi    — Ledger accumulates in SumiAbility.OnTurnEnd, not here
				// Vesna   — The Breach (chain capture) is a CaptureResolver effect, not a stat buff
			}
		}

		// ── Aegis Protocol (Seraph Yune) ─────────────────────────────────────────
		// Friendly adjacent cards get +1 to all sides.
		// Built to face front — corporate infrastructure covering for its product.
		private static void ApplyAegisProtocol(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				adj.DomainBonusTop    += 1;
				adj.DomainBonusRight  += 1;
				adj.DomainBonusBottom += 1;
				adj.DomainBonusLeft   += 1;
				GD.Print($"Aegis: {adj.Data.Name} +1 all sides.");
			}
		}

		// ── Killzone (Sister Grin) ────────────────────────────────────────────────
		// Friendly adjacent cards get +2 to their two lowest sides.
		// Bristles on the weak flank — makes Grin un-flankable in a corner.
		private static void ApplyKillzone(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				// Collect base values (domain bonuses are 0 at this point in the pass)
				var edges = new (Direction dir, int val)[]
				{
					(Direction.Top,    adj.GetValue(Direction.Top)),
					(Direction.Right,  adj.GetValue(Direction.Right)),
					(Direction.Bottom, adj.GetValue(Direction.Bottom)),
					(Direction.Left,   adj.GetValue(Direction.Left)),
				};

				// Sort ascending — lowest two get the +2
				System.Array.Sort(edges, (a, b) => a.val.CompareTo(b.val));

				for (int i = 0; i < 2; i++)
				{
					switch (edges[i].dir)
					{
						case Direction.Top:    adj.DomainBonusTop    += 2; break;
						case Direction.Right:  adj.DomainBonusRight  += 2; break;
						case Direction.Bottom: adj.DomainBonusBottom += 2; break;
						case Direction.Left:   adj.DomainBonusLeft   += 2; break;
					}
				}

				GD.Print($"Killzone: {adj.Data.Name} +2 to {edges[0].dir} and {edges[1].dir}.");
			}
		}

		// ── Lateral Grid (Riven) ──────────────────────────────────────────────────
		// Friendly adjacent cards get +2 Left and +2 Right.
		// Lives sideways, in the data-flow.
		private static void ApplyLateralGrid(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				adj.DomainBonusLeft  += 2;
				adj.DomainBonusRight += 2;
				GD.Print($"Lateral Grid: {adj.Data.Name} +2 Left/Right.");
			}
		}

		// ── Sprawl (Mara Kane) ────────────────────────────────────────────────────
		// Adjacent friendly Commons cards get +1 all sides.
		// Mara herself gets +1 all sides per adjacent friendly card (any faction).
		private static void ApplySprawl(BoardState board, CardInstance hero, int hr, int hc)
		{
			int friendlyCount = 0;

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(hr, hc, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var adj = board.GetCard(nRow, nCol);
				if (adj == null || adj.OwnerId != hero.OwnerId) continue;

				friendlyCount++;

				// Only Commons faction cards receive the buff
				if (adj.Data.Faction == Faction.Commons)
				{
					adj.DomainBonusTop    += 1;
					adj.DomainBonusRight  += 1;
					adj.DomainBonusBottom += 1;
					adj.DomainBonusLeft   += 1;
					GD.Print($"Sprawl: {adj.Data.Name} +1 all sides.");
				}
			}

			// Mara grows with the crowd
			if (friendlyCount > 0)
			{
				hero.DomainBonusTop    += friendlyCount;
				hero.DomainBonusRight  += friendlyCount;
				hero.DomainBonusBottom += friendlyCount;
				hero.DomainBonusLeft   += friendlyCount;
				GD.Print($"Sprawl: Mara Kane +{friendlyCount} all sides ({friendlyCount} friendly adjacent).");
			}
		}

		// ── Helper ────────────────────────────────────────────────────────────────
		// Yields all adjacent cards owned by the same player as the hero.
		private static IEnumerable<(CardInstance card, int row, int col)> AdjacentFriendly(
			BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(hr, hc, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var adj = board.GetCard(nRow, nCol);
				if (adj == null || adj.OwnerId != hero.OwnerId) continue;

				yield return (adj, nRow, nCol);
			}
		}
	}
}
