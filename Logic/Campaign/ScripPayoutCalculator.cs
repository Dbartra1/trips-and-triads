using System;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Computes the scrip payout awarded to the player after winning a match
	/// (systems.md §9, Appendix A.4).
	///
	/// Formula: base × danger multiplier × income multiplier (cred-scaled).
	/// Loss always pays zero.
	///
	/// Base rates:
	///   The Stub  — 10 (Della Standing Work rate; tutorial district)
	///   All others — 20 (Standard Fixer contract)
	///
	/// Danger multipliers:
	///   The Stub        — ×1.0
	///   Faction districts — ×1.3
	///   The Hush / The Vault — ×2.0
	///
	/// Income multiplier is cred-scaled via <see cref="CredEffects.IncomeMultiplier"/>.
	/// </summary>
	public static class ScripPayoutCalculator
	{
		// ── Danger multipliers by district ────────────────────────────────────────

		public static float DangerMultiplier(string districtId) => districtId switch
		{
			"the_hush"  => 2.0f,
			"the_vault" => 2.0f,
			"the_stub"  => 1.0f,
			_           => 1.3f,   // all faction districts
		};

		/// <summary>Base payout before danger and income multipliers.</summary>
		public static float BasePayout(string districtId)
			=> districtId == "the_stub" ? 10f : 20f;

		// ── Main entry point ──────────────────────────────────────────────────────

		/// <summary>
		/// Returns the scrip awarded for this match result.
		/// Always 0 on a loss.
		/// Result is floored (no fractional scrip).
		/// </summary>
		public static int Calculate(string districtId, bool playerWon, CredTier tier)
		{
			if (!playerWon) return 0;

			float payout = BasePayout(districtId)
			             * DangerMultiplier(districtId)
			             * CredEffects.IncomeMultiplier(tier);

			return (int)payout; // floor — partial scrip is discarded
		}
	}
}
