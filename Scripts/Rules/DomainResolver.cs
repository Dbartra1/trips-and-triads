using System.Collections.Generic;
using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Computes and applies transient domain bonuses from heroes to adjacent friendly cards.
	/// Called after every board state change. Bonuses reset and recompute fresh each call.
	/// Sumi's Ledger is the exception — it accumulates in the override layer via SumiAbility.
	/// </summary>
	public static class DomainResolver
	{
		public static void Apply(BoardState board)
		{
			// Step 1 — clear all transient bonuses
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
					board.GetCard(r, c)?.ResetDomainBonuses();

			// Step 2 — find every hero and apply their domain
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					var hero = board.GetCard(r, c);
					if (hero == null || hero.Data.Tier != Tier.Hero) continue;
					ApplyDomain(board, hero, r, c);
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
				// Lethe   — domain is her placement mechanic (LetheAbility)
				// Sumi    — Ledger accumulates in SumiAbility.OnTurnEnd
				// Vesna   — The Breach (chain capture) deferred to Phase 3b
			}
		}

		// ── Aegis Protocol (Seraph Yune) ─────────────────────────────────────────
		// Friendly adjacent cards get +1 to all sides.
		private static void ApplyAegisProtocol(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				adj.DomainBonusTop    += 1;
				adj.DomainBonusRight  += 1;
				adj.DomainBonusBottom += 1;
				adj.DomainBonusLeft   += 1;
			}
		}

		// ── Killzone (Sister Grin) ────────────────────────────────────────────────
		// Friendly adjacent cards get +2 to their two lowest sides.
		private static void ApplyKillzone(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				var edges = new (Direction dir, int val)[]
				{
					(Direction.Top,    adj.GetValue(Direction.Top)),
					(Direction.Right,  adj.GetValue(Direction.Right)),
					(Direction.Bottom, adj.GetValue(Direction.Bottom)),
					(Direction.Left,   adj.GetValue(Direction.Left)),
				};

				System.Array.Sort(edges, (a, b) => a.val.CompareTo(b.val));

				for (int i = 0; i < 2; i++)
					switch (edges[i].dir)
					{
						case Direction.Top:    adj.DomainBonusTop    += 2; break;
						case Direction.Right:  adj.DomainBonusRight  += 2; break;
						case Direction.Bottom: adj.DomainBonusBottom += 2; break;
						case Direction.Left:   adj.DomainBonusLeft   += 2; break;
					}
			}
		}

		// ── Lateral Grid (Riven) ──────────────────────────────────────────────────
		// Friendly adjacent cards get +2 Left and +2 Right.
		private static void ApplyLateralGrid(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				adj.DomainBonusLeft  += 2;
				adj.DomainBonusRight += 2;
			}
		}

		// ── Sprawl (Mara Kane) ────────────────────────────────────────────────────
		// Adjacent friendly Commons cards get +1 all sides.
		// Mara herself gets +1 all sides per adjacent friendly (any faction).
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

				if (adj.Data.Faction == Faction.Commons)
				{
					adj.DomainBonusTop    += 1;
					adj.DomainBonusRight  += 1;
					adj.DomainBonusBottom += 1;
					adj.DomainBonusLeft   += 1;
				}
			}

			if (friendlyCount > 0)
			{
				hero.DomainBonusTop    += friendlyCount;
				hero.DomainBonusRight  += friendlyCount;
				hero.DomainBonusBottom += friendlyCount;
				hero.DomainBonusLeft   += friendlyCount;
			}
		}

		// ── Helper ────────────────────────────────────────────────────────────────
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