using System;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Owns the crew's Street Cred value and exposes mutators for every event
	/// that changes it (systems.md §8).
	///
	/// Cred is a clamped integer 0–100. Tier is derived, never stored.
	/// All delta constants live in <see cref="CredEvents"/> so tests can
	/// assert against the same values the manager applies.
	/// </summary>
	public class CredManager
	{
		private int _cred;

		public int      Cred => _cred;
		public CredTier Tier => TierFor(_cred);

		/// <summary>Start a new run at Nameless (0).</summary>
		public CredManager() : this(0) { }

		public CredManager(int startingCred)
		{
			_cred = Math.Clamp(startingCred, 0, 100);
		}

		// ── Mutators ─────────────────────────────────────────────────────────────

		/// <summary>Add (or subtract if negative) delta, clamped to [0, 100].</summary>
		public void Apply(int delta)
		{
			_cred = Math.Clamp(_cred + delta, 0, 100);
		}

		/// <summary>
		/// Apply one or more CredEvents in a single call.
		/// Events are additive — pass every event that fired this turn.
		/// </summary>
		public void ApplyEvents(params CredEvent[] events)
		{
			int total = 0;
			foreach (var ev in events)
				total += CredEvents.DeltaFor(ev);
			Apply(total);
		}

		/// <summary>
		/// Step Up reset — returns cred to the Known floor (20).
		/// Always lands at Known regardless of current value (systems.md §8.5).
		/// </summary>
		public void StepUpReset()
		{
			_cred = CredEvents.StepUpResetValue;
		}

		// ── Tier lookup ──────────────────────────────────────────────────────────

		/// <summary>
		/// Pure function — derive tier from any cred value without a CredManager instance.
		/// </summary>
		public static CredTier TierFor(int cred) => cred switch
		{
			<= 19 => CredTier.Nameless,
			<= 39 => CredTier.Known,
			<= 59 => CredTier.Named,
			<= 79 => CredTier.Notorious,
			_     => CredTier.Legend,
		};

		/// <summary>Lowest cred value that falls in the given tier.</summary>
		public static int TierFloor(CredTier tier) => tier switch
		{
			CredTier.Nameless  => 0,
			CredTier.Known     => 20,
			CredTier.Named     => 40,
			CredTier.Notorious => 60,
			CredTier.Legend    => 80,
			_                  => 0,
		};

		/// <summary>Highest cred value that falls in the given tier.</summary>
		public static int TierCeiling(CredTier tier) => tier switch
		{
			CredTier.Nameless  => 19,
			CredTier.Known     => 39,
			CredTier.Named     => 59,
			CredTier.Notorious => 79,
			CredTier.Legend    => 100,
			_                  => 19,
		};
	}
}
