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
			return ApplyPromotion(best);
		}

		/// <summary>
		/// Promotes a specific card chosen by the player rather than auto-selecting.
		/// The card must be a non-hero. Returns the promoted card.
		/// </summary>
		public static CardData PromoteSpecific(CardData target)
		{
			if (target == null || target.Tier == Tier.Hero) return null;
			return ApplyPromotion(target);
		}

		/// <summary>
		/// Returns the projected stat line after promotion WITHOUT mutating the card.
		/// Useful for showing the player what they'd be getting before they commit.
		/// </summary>
		public static (int Top, int Right, int Bottom, int Left) PreviewPromotion(CardData card)
		{
			int[] edges = { card.Top, card.Right, card.Bottom, card.Left };

			int highIdx = 0;
			for (int i = 1; i < 4; i++)
				if (edges[i] > edges[highIdx]) highIdx = i;

			edges[highIdx] = 20; // Scale-20 A

			int lowIdx = -1;
			for (int i = 0; i < 4; i++)
			{
				if (i == highIdx) continue;
				if (lowIdx == -1 || edges[i] < edges[lowIdx]) lowIdx = i;
			}

			if (lowIdx >= 0 && edges[lowIdx] > 6) edges[lowIdx] = 6; // Scale-20 soft cap

			return (edges[0], edges[1], edges[2], edges[3]);
		}

		// ── Internal ────────────────────────────────────────────────────────────

		private static CardData ApplyPromotion(CardData best)
		{
			int[] edges = { best.Top, best.Right, best.Bottom, best.Left };

			// Find highest edge — this becomes the A (10).
			int highIdx = 0;
			for (int i = 1; i < 4; i++)
				if (edges[i] > edges[highIdx]) highIdx = i;

			// Promote highest to A FIRST, then find lowest among remaining edges.
			// Finding lowIdx before promotion causes a bug when all edges are equal:
			// highIdx and lowIdx would both be 0, setting edges[0] to 10 then to 3,
			// resulting in no A on the card.
			edges[highIdx] = 20; // Scale-20 A

			// Find lowest edge that is NOT the promoted edge.
			int lowIdx = -1;
			for (int i = 0; i < 4; i++)
			{
				if (i == highIdx) continue;
				if (lowIdx == -1 || edges[i] < edges[lowIdx]) lowIdx = i;
			}

			if (lowIdx >= 0 && edges[lowIdx] > 6) edges[lowIdx] = 6; // Scale-20 soft cap

			best.Top    = edges[0];
			best.Right  = edges[1];
			best.Bottom = edges[2];
			best.Left   = edges[3];

			best.Tier        = Tier.Hero;
			best.Level       = 20;
			best.DomainType  = DefaultDomain(best.Faction);
			best.AbilityType = AbilityType.None;

			return best;
		}
}
}