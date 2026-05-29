namespace TripsAndTriads.Core
{
	/// <summary>
	/// Pure functions — every downstream system that reads Street Cred goes
	/// through this class (systems.md §8.4, Appendix A.2).
	///
	/// All values are first-pass baselines from Appendix A.2.
	/// The methods are static so they can be called from tests and from
	/// GameSession/Economy without needing a live CredManager instance.
	/// </summary>
	public static class CredEffects
	{
		// ── Income multiplier ─────────────────────────────────────────────────
		// Applied to all scrip contract payouts (§8.4).
		// A Legend crew earns 2.5× what a Nameless crew earns for the same job.

		public static float IncomeMultiplier(CredTier tier) => tier switch
		{
			CredTier.Nameless  => 1.00f,
			CredTier.Known     => 1.30f,
			CredTier.Named     => 1.60f,
			CredTier.Notorious => 2.00f,
			CredTier.Legend    => 2.50f,
			_                  => 1.00f,
		};

		// ── Debt interest rate per overworld turn (§11.5) ─────────────────────
		// The cruelest cred effect: same debt, five times the cost for the poor.
		// A Nameless crew pays 25% interest per turn; a Legend pays 2%.

		public static float DebtInterestRate(CredTier tier) => tier switch
		{
			CredTier.Nameless  => 0.25f,
			CredTier.Known     => 0.18f,
			CredTier.Named     => 0.10f,
			CredTier.Notorious => 0.05f,
			CredTier.Legend    => 0.02f,
			_                  => 0.25f,
		};

		// ── Ransom / buyout discount (§8.4) ──────────────────────────────────
		// Flat percentage off buyout prices across all factions.
		// Lacquer still charges the most; Commons still charges the least.
		// Returned as a multiplier (1.0 = no discount, 0.90 = 10% off).

		public static float RansomPriceMultiplier(CredTier tier) => tier switch
		{
			CredTier.Nameless  => 1.00f,
			CredTier.Known     => 0.97f,
			CredTier.Named     => 0.95f,
			CredTier.Notorious => 0.92f,
			CredTier.Legend    => 0.90f,
			_                  => 1.00f,
		};

		/// <summary>
		/// Convenience: raw discount as a fraction (0.00 – 0.10).
		/// Tests can assert this directly.
		/// </summary>
		public static float RansomDiscount(CredTier tier) =>
			1.0f - RansomPriceMultiplier(tier);

		// ── District control shift multiplier (§6.4) ─────────────────────────
		// A Legend crew's wins move the control meter twice as fast as a
		// Nameless crew's wins.

		public static float ControlShiftMultiplier(CredTier tier) => tier switch
		{
			CredTier.Nameless  => 1.00f,
			CredTier.Known     => 1.25f,
			CredTier.Named     => 1.50f,
			CredTier.Notorious => 1.75f,
			CredTier.Legend    => 2.00f,
			_                  => 1.00f,
		};
	}
}
