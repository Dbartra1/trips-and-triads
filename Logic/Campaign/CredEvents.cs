namespace TripsAndTriads.Core
{
	/// <summary>
	/// Every discrete event that changes Street Cred (systems.md §8.2, Appendix A.1).
	///
	/// Events are additive — multiple events can fire on the same turn.
	/// Example: winning in The Hush vs a Razorkin crew fires
	///   WinMatch + WinDangerousDistrict + WinVsRazorkin = +2 +4 +5 = +11.
	/// </summary>
	public enum CredEvent
	{
		// ── Gains ──────────────────────────────────────────────────────────────
		WinMatch,              // +2  — any victory
		WinDangerousDistrict,  // +4  — The Hush or The Vault; stacks with WinMatch
		WinVsRazorkin,         // +5  — beating a Razorkin crew; stacks with others
		FlipDistrictControl,   // +5  — wresting control of a district
		HuntReclaimByDuel,     // +6  — reclaiming a captured hero via the fight path

		// ── Losses ─────────────────────────────────────────────────────────────
		LoseMatch,             // −2
		BuyoutHero,            // −4  — ransoming a hero rather than dueling for them
	}

	/// <summary>
	/// Delta lookup table — single source of truth for all cred values.
	/// Tests assert against DeltaFor() to stay in sync with the manager.
	/// </summary>
	public static class CredEvents
	{
		/// <summary>The cred value a Step Up reset lands on (systems.md §8.5).</summary>
		public const int StepUpResetValue = 20;

		public static int DeltaFor(CredEvent ev) => ev switch
		{
			CredEvent.WinMatch             =>  2,
			CredEvent.WinDangerousDistrict =>  4,
			CredEvent.WinVsRazorkin        =>  5,
			CredEvent.FlipDistrictControl  =>  5,
			CredEvent.HuntReclaimByDuel    =>  6,
			CredEvent.LoseMatch            => -2,
			CredEvent.BuyoutHero           => -4,
			_                              =>  0,
		};
	}
}
