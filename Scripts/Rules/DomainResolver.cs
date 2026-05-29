using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public static class DomainResolver
	{
		/// <summary>
		/// Multiplier for all domain bonuses.
		/// 1 = Scale-10 default (bonuses as designed: +1 Aegis, +2 Killzone/Lateral).
		/// 2 = Scale-20 (doubles all bonuses so they remain meaningful at doubled stats).
		/// Reset to 1 after any test that changes it.
		/// Driven by DistrictManager or GameBoard at match start.
		/// </summary>
		public static int BonusMultiplier { get; set; } = 1;

		public static void Apply(BoardState board)
		{
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
					board.GetCard(r, c)?.ResetDomainBonuses();

			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					var hero = board.GetCard(r, c);
					if (hero == null || hero.Data.Tier != Tier.Hero) continue;
					if (hero.Data.DomainType == DomainType.None) continue;
					ApplyDomain(board, hero, r, c);
				}
		}

		private static void ApplyDomain(BoardState board, CardInstance hero, int hr, int hc)
		{
			switch (hero.Data.DomainType)
			{
				case DomainType.AegisProtocol: ApplyAegisProtocol(board, hero, hr, hc); break;
				case DomainType.Killzone:       ApplyKillzone(board, hero, hr, hc);      break;
				case DomainType.LateralGrid:    ApplyLateralGrid(board, hero, hr, hc);   break;
				case DomainType.Sprawl:         ApplySprawl(board, hero, hr, hc);        break;
			}
		}

		private static void ApplyAegisProtocol(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				adj.DomainBonusTop    += 1 * BonusMultiplier;
				adj.DomainBonusRight  += 1 * BonusMultiplier;
				adj.DomainBonusBottom += 1 * BonusMultiplier;
				adj.DomainBonusLeft   += 1 * BonusMultiplier;
			}
		}

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
						case Direction.Top:    adj.DomainBonusTop    += 2 * BonusMultiplier; break;
						case Direction.Right:  adj.DomainBonusRight  += 2 * BonusMultiplier; break;
						case Direction.Bottom: adj.DomainBonusBottom += 2 * BonusMultiplier; break;
						case Direction.Left:   adj.DomainBonusLeft   += 2 * BonusMultiplier; break;
					}
			}
		}

		private static void ApplyLateralGrid(BoardState board, CardInstance hero, int hr, int hc)
		{
			foreach (var (adj, _, _) in AdjacentFriendly(board, hero, hr, hc))
			{
				adj.DomainBonusLeft  += 2 * BonusMultiplier;
				adj.DomainBonusRight += 2 * BonusMultiplier;
			}
		}

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
					adj.DomainBonusTop    += 1 * BonusMultiplier;
					adj.DomainBonusRight  += 1 * BonusMultiplier;
					adj.DomainBonusBottom += 1 * BonusMultiplier;
					adj.DomainBonusLeft   += 1 * BonusMultiplier;
				}
			}
			if (friendlyCount > 0)
			{
				hero.DomainBonusTop    += friendlyCount * BonusMultiplier;
				hero.DomainBonusRight  += friendlyCount * BonusMultiplier;
				hero.DomainBonusBottom += friendlyCount * BonusMultiplier;
				hero.DomainBonusLeft   += friendlyCount * BonusMultiplier;
			}
		}

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
