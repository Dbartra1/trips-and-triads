using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Scans the board for named bond pairs and applies their effects.
	/// Called after DomainResolver.Apply — bond bonuses layer on top of domain bonuses.
	/// Passive stat bonds go into BondBonus (transient).
	/// Behavioral bonds set flags on CardInstance (BlockChoir, RivalryActive).
	/// Permanent-accumulating bonds (The Inheritance) are handled in SumiAbility.
	/// Contamination is applied here as an override adjustment (permanent per placement).
	/// </summary>
	public static class BondResolver
	{
		public static void Apply(BoardState board)
		{
			// Reset all transient bond bonuses and flags first
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
					board.GetCard(r, c)?.ResetBondBonuses();

			// Scan every card and check bonds
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					var card = board.GetCard(r, c);
					if (card == null) continue;

					switch (card.Data.Id)
					{
						case "asc_hero_seraph_yune": ApplyYuneBonds(board, card, r, c);  break;
						case "rzk_hero_sister_grin": ApplyGrinBonds(board, card, r, c);  break;
						case "gwi_hero_riven":       ApplyRivenBonds(board, card, r, c); break;
						case "com_hero_mara_kane":   ApplyMaraBonds(board, card, r, c);  break;
						case "hch_hero_vesna":       ApplyVesnaBonds(board, card, r, c); break;
						case "eff_hero_lethe":       ApplyLetheBonds(board, card, r, c); break;
					}
				}
		}

		// ── The Rivalry (Yune ↔ Grin) ────────────────────────────────────────────
		// If both are on the board, set RivalryActive on both — CaptureResolver
		// will make them resolve capture against each other first when adjacent.
		// Also handles Maker's Mark: Cassia Vane on board → Yune's Bottom +2.
		private static void ApplyYuneBonds(BoardState board, CardInstance yune, int yr, int yc)
		{
			bool grinOnBoard = FindCard(board, "rzk_hero_sister_grin", out _, out _);
			if (grinOnBoard)
			{
				yune.RivalryActive = true;
				GD.Print("The Rivalry active — Seraph Yune and Sister Grin.");
			}

			// Maker's Mark — Cassia Vane on board patches Yune's blind spot
			if (FindCard(board, "asc_top_cassia_vane", out _, out _))
			{
				yune.BondBonusBottom += 2;
				GD.Print($"Maker's Mark — Yune's Bottom now {yune.GetValue(Direction.Bottom)} (+2 from Vane).");
			}
		}

		private static void ApplyGrinBonds(BoardState board, CardInstance grin, int gr, int gc)
		{
			if (FindCard(board, "asc_hero_seraph_yune", out _, out _))
			{
				grin.RivalryActive = true;
			}
		}

		// ── The Last Crew (Riven ↔ Mara) ─────────────────────────────────────────
		// Both on board → each gets +1 all sides.
		// The Listener (Riven ↔ Vesna) → Riven cannot capture Choir cards.
		private static void ApplyRivenBonds(BoardState board, CardInstance riven, int rr, int rc)
		{
			if (FindCard(board, "com_hero_mara_kane", out _, out _))
			{
				riven.BondBonusTop    += 1;
				riven.BondBonusRight  += 1;
				riven.BondBonusBottom += 1;
				riven.BondBonusLeft   += 1;
				GD.Print($"The Last Crew — Riven +1 all sides.");

				// Also buff Mara
				if (FindCard(board, "com_hero_mara_kane", out int mr, out int mc))
				{
					var mara = board.GetCard(mr, mc);
					if (mara != null)
					{
						mara.BondBonusTop    += 1;
						mara.BondBonusRight  += 1;
						mara.BondBonusBottom += 1;
						mara.BondBonusLeft   += 1;
						GD.Print($"The Last Crew — Mara Kane +1 all sides.");
					}
				}
			}

			if (FindCard(board, "hch_hero_vesna", out _, out _))
			{
				riven.BlockChoir = true;
				GD.Print("The Listener active — Riven will not capture Choir cards.");
			}
		}

		// Mara's bond buff is applied from Riven's side to avoid double-applying.
		private static void ApplyMaraBonds(BoardState board, CardInstance mara, int mr, int mc) { }

		// ── Contamination (Vesna ↔ adjacent non-Choir) ───────────────────────────
		// Any non-Choir card placed adjacent to Vesna has its lowest edge reduced by 1.
		// Applied as an override adjustment (permanent, applied once on placement).
		// BondResolver marks cards as contaminated via a flag to avoid re-applying.
		private static void ApplyVesnaBonds(BoardState board, CardInstance vesna, int vr, int vc)
		{
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(vr, vc, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var adj = board.GetCard(nRow, nCol);
				if (adj == null) continue;
				if (adj.Data.Faction == Faction.HollowChoir) continue;
				if (adj.OwnerId == vesna.OwnerId) continue; // only affects enemies

				// Find the lowest edge and reduce it by 1 in the override layer
				Direction lowest = LowestEdgeDirection(adj);
				int current = adj.GetBaseValue(lowest);
				if (current > 0)
				{
					switch (lowest)
					{
						case Direction.Top:    adj.TopOverride    = current - 1; break;
						case Direction.Right:  adj.RightOverride  = current - 1; break;
						case Direction.Bottom: adj.BottomOverride = current - 1; break;
						case Direction.Left:   adj.LeftOverride   = current - 1; break;
					}
					GD.Print($"Contamination — {adj.Data.Name}'s {lowest} reduced to {current - 1}.");
				}
			}
		}

		// ── The Understudy (Lethe ↔ the hero she copied) ─────────────────────────
		// If Lethe is adjacent to the original of the card she's wearing,
		// both cards' highest edge drops to 5 — the city cannot resolve two of the same face.
		private static void ApplyLetheBonds(BoardState board, CardInstance lethe, int lr, int lc)
		{
			// Lethe has copied someone if her overrides are set
			if (!lethe.IsModified) return;

			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				var (nRow, nCol) = board.GetNeighbor(lr, lc, dir);
				if (!board.IsInBounds(nRow, nCol)) continue;

				var adj = board.GetCard(nRow, nCol);
				if (adj == null || adj.Data.Tier != Tier.Hero) continue;

				// Check if Lethe's stat line matches this hero's stat line
				// (she copied their numbers)
				bool matches =
					lethe.GetBaseValue(Direction.Top)    == adj.GetBaseValue(Direction.Top)    &&
					lethe.GetBaseValue(Direction.Right)  == adj.GetBaseValue(Direction.Right)  &&
					lethe.GetBaseValue(Direction.Bottom) == adj.GetBaseValue(Direction.Bottom) &&
					lethe.GetBaseValue(Direction.Left)   == adj.GetBaseValue(Direction.Left);

				if (!matches) continue;

				// Both glitch — highest edge on each drops to 5
				Direction letheHigh = lethe.HighestEdgeDirection();
				Direction adjHigh   = adj.HighestEdgeDirection();

				lethe.ClampEdge(letheHigh, 5);
				adj.ClampEdge(adjHigh, 5);

				GD.Print($"The Understudy — Lethe adjacent to {adj.Data.Name}. " +
				         $"Both highest edges clamped to 5.");
				break;
			}
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		// Finds the first instance of a card by ID on the board.
		// Returns true and sets (row, col) if found.
		private static bool FindCard(BoardState board, string id, out int foundRow, out int foundCol)
		{
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					var card = board.GetCard(r, c);
					if (card != null && card.Data.Id == id)
					{
						foundRow = r; foundCol = c;
						return true;
					}
				}
			foundRow = foundCol = -1;
			return false;
		}

		private static Direction LowestEdgeDirection(CardInstance card)
		{
			var best    = Direction.Top;
			int bestVal = card.GetBaseValue(Direction.Top);
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				int v = card.GetBaseValue(dir);
				if (v < bestVal) { bestVal = v; best = dir; }
			}
			return best;
		}
	}
}