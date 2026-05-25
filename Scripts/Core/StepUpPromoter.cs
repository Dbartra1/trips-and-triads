using System.Collections.Generic;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Handles Step Up succession (systems.md §7.5).
	///
	/// When a Reclaim window closes, the player promotes one non-hero card
	/// from the deck that lost the hero into a new hero. That card gains:
	///   • An A (10) placed on its current highest edge.
	///   • Its current lowest edge capped to 3 (the "soft" side, lore-true).
	///   • A faction-appropriate DomainType.
	///   • AbilityType.None (most promoted heroes start without an active ability).
	///   • Tier promoted to Hero, Level set to 10.
	///
	/// The card is mutated in place — it remains the same CardData object in the
	/// roster so all existing list references stay valid.
	/// </summary>
	public static class StepUpPromoter
	{
		// ── Faction → default Domain for a newly-promoted hero ─────────────────

		private static DomainType DefaultDomain(Faction faction) => faction switch
		{
			Faction.Ascendant  => DomainType.AegisProtocol, // +1 all sides to adjacents
			Faction.Razorkin   => DomainType.Killzone,       // +2 to two lowest sides
			Faction.Ghostwire  => DomainType.LateralGrid,    // +2 Left/Right
			Faction.Commons    => DomainType.Sprawl,         // +1 Commons adjacents
			// Effigy, Lacquer, HollowChoir, None — generalist fallback
			_                  => DomainType.AegisProtocol,
		};

		// ── Public API ──────────────────────────────────────────────────────────

		/// <summary>
		/// Selects the best candidate from <paramref name="deckCards"/> and promotes
		/// it to Hero. Returns the promoted card, or <c>null</c> if no eligible
		/// non-hero card exists.
		/// </summary>
		public static CardData Promote(List<CardData> deckCards)
		{
			CardData best     = null;
			int      bestTotal = -1;

			foreach (var card in deckCards)
			{
				if (card.Tier == Tier.Hero) continue;
				int total = card.Top + card.Right + card.Bottom + card.Left;
				if (total > bestTotal) { bestTotal = total; best = card; }
			}

			if (best == null) return null;

			int[] edges = { best.Top, best.Right, best.Bottom, best.Left };

			// Find highest edge — this becomes the A (10).
			int highIdx = 0;
			for (int i = 1; i < 4; i++)
				if (edges[i] > edges[highIdx]) highIdx = i;

			// Find lowest edge — this becomes the soft side (capped to 3).
			int lowIdx = 0;
			for (int i = 1; i < 4; i++)
				if (edges[i] < edges[lowIdx]) lowIdx = i;

			edges[highIdx] = 10;
			if (edges[lowIdx] > 3) edges[lowIdx] = 3;

			best.Top    = edges[0];
			best.Right  = edges[1];
			best.Bottom = edges[2];
			best.Left   = edges[3];

			best.Tier        = Tier.Hero;
			best.Level       = 10;
			best.DomainType  = DefaultDomain(best.Faction);
			best.AbilityType = AbilityType.None;

			return best;
		}
	}
}
