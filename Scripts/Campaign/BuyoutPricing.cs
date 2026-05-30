using System;
using System.Collections.Generic;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Computes the scrip cost to buy out a captured hero (systems.md §7.4).
	///
	/// Three factors drive the final cost:
	///   1. Faction markup — who holds the hero determines the base price.
	///   2. Cred discount — respected crews pay less (except Ascendant, fixed-rate).
	///   3. Escalation — each failed duel attempt raises the price by
	///      <see cref="EscalationPerFailedAttempt"/> (Appendix §7.4: ~50–75%, first-pass 60%).
	///
	/// HollowChoir: no buyout available — always returns -1.
	/// Razorkin: base price applies but the offer may be refused (see
	///   <see cref="RazorkinRefusal"/>). The cost itself is not special.
	/// Ascendant: "market rate" — cred discount does NOT apply.
	/// </summary>
	public static class BuyoutPricing
	{
		// ── Faction base costs (first-pass; tune in playtesting) ──────────────────
		// Designed to be affordable on a normal scrip budget even after one failed
		// duel — a Nameless crew running Della contracts (10/match) can reach
		// Commons/Razorkin buyout in a couple of matches.

		private static readonly Dictionary<Faction, int> BaseCosts = new()
		{
			[Faction.Commons]  = 10,   // lowest — Commons don't believe in owning people
			[Faction.Razorkin] = 15,   // cheap but volatile (refusal mechanic)
			[Faction.Ghostwire]= 25,   // data has a price; this is the going rate
			[Faction.Effigy]   = 25,   // a face for sale like any other
			[Faction.Ascendant]= 40,   // "market rate" — non-negotiable, no cred discount
			[Faction.Lacquer]  = 60,   // highest — Lacquer runs the ransom economy
		};

		/// <summary>
		/// Escalation per failed duel attempt (§7.4: ~50–75%, first-pass 60%).
		/// Applied multiplicatively: base × (1 + Escalation)^failedAttempts.
		/// </summary>
		public const float EscalationPerFailedAttempt = 0.60f;

		// ── Public API ────────────────────────────────────────────────────────────

		/// <summary>
		/// True if a buyout exists for this captor faction.
		/// HollowChoir always returns false. Faction.None returns false.
		/// </summary>
		public static bool IsAvailable(Faction captor)
			=> captor != Faction.HollowChoir
			&& captor != Faction.None
			&& BaseCosts.ContainsKey(captor);

		/// <summary>
		/// Returns the final scrip cost after applying cred discount and escalation.
		/// Returns -1 if no buyout exists for this faction (HollowChoir, unknown).
		/// </summary>
		/// <param name="captor">Faction that holds the captured hero.</param>
		/// <param name="credTier">Player crew's current cred tier.</param>
		/// <param name="failedAttempts">
		///   Number of Reclaim duels the player has already lost (0–1 in practice).
		///   Equal to (2 − ReclamationAttemptsLeft) when the window is still open.
		/// </param>
		public static int ComputeCost(Faction captor, CredTier credTier, int failedAttempts)
		{
			if (!IsAvailable(captor)) return -1;

			float cost = BaseCosts[captor];

			// Ascendant is non-negotiable — "market rate" ignores cred.
			if (captor != Faction.Ascendant)
				cost *= CredEffects.RansomPriceMultiplier(credTier);

			// Escalation: +60% per prior failed reclaim duel.
			int clampedFails = Math.Max(0, failedAttempts);
			for (int i = 0; i < clampedFails; i++)
				cost *= 1f + EscalationPerFailedAttempt;

			// Round up — always charge at least the base.
			return (int)Math.Ceiling(cost);
		}

		/// <summary>
		/// The raw base cost for a faction, before any modifiers.
		/// Returns 0 if unavailable. Useful for UI tooltips.
		/// </summary>
		public static int GetBaseCost(Faction captor)
			=> BaseCosts.TryGetValue(captor, out int cost) ? cost : 0;
	}
}
