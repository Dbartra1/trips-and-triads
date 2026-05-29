using System;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Models the Razorkin buyout refusal mechanic (systems.md §8.3).
	///
	/// When a crew tries to ransom a hero from a Razorkin captor, Razorkin
	/// may simply refuse — they'd rather fight. Refusal probability has a
	/// hard floor (~18%) that no amount of cred can eliminate, plus a
	/// cred-scaled penalty that decays to zero at Legend.
	///
	/// A refused buyout costs nothing and does not burn a Reclaim attempt.
	/// </summary>
	public static class RazorkinRefusal
	{
		/// <summary>
		/// Immovable floor — even a Legend crew faces this chance.
		/// Some Razorkin just want the fight regardless of who you are.
		/// </summary>
		public const float Floor = 0.18f;

		/// <summary>
		/// Cred-scaled penalty on top of the floor.
		/// Decays to 0 at Legend (floor only).
		/// </summary>
		public static float CreditPenalty(CredTier tier) => tier switch
		{
			CredTier.Nameless  => 0.47f,   // 0.18 + 0.47 = 0.65  (~65% refusal)
			CredTier.Known     => 0.32f,   // 0.18 + 0.32 = 0.50  (~50%)
			CredTier.Named     => 0.20f,   // 0.18 + 0.20 = 0.38  (~38%)
			CredTier.Notorious => 0.08f,   // 0.18 + 0.08 = 0.26  (~26%)
			CredTier.Legend    => 0.00f,   // 0.18 + 0.00 = 0.18  (floor only)
			_                  => 0.47f,
		};

		/// <summary>Combined refusal probability for the given tier (0–1).</summary>
		public static float RefusalChance(CredTier tier) =>
			Floor + CreditPenalty(tier);

		/// <summary>
		/// Roll the refusal check. Returns true if Razorkin refuses the buyout.
		/// Pass a seeded Random in tests for reproducibility.
		/// </summary>
		public static bool IsRefused(CredTier tier, Random? rng = null)
		{
			rng ??= new Random();
			return rng.NextDouble() < RefusalChance(tier);
		}
	}
}
