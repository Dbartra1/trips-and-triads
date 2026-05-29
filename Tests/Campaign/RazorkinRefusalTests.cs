using System;
using Xunit;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// Tests for the Razorkin buyout refusal mechanic (systems.md §8.3).
	///
	/// Structural tests (deterministic): verify the floor, the penalty curve,
	/// and the combined refusal probability at each tier.
	///
	/// Statistical tests (10,000 rolls): verify that IsRefused() produces the
	/// expected distribution at Nameless and Legend. Tolerance is ±5 percentage
	/// points — wide enough to avoid flakes, tight enough to catch bugs.
	/// </summary>
	public class RazorkinRefusalTests
	{
		// ── Floor ────────────────────────────────────────────────────────────────

		[Fact]
		public void Floor_IsEighteenPercent()
		{
			Assert.Equal(0.18f, RazorkinRefusal.Floor, precision: 4);
		}

		// ── Combined refusal chance matches spec ──────────────────────────────────

		[Theory]
		[InlineData(CredTier.Nameless,  0.65f)]
		[InlineData(CredTier.Known,     0.50f)]
		[InlineData(CredTier.Named,     0.38f)]
		[InlineData(CredTier.Notorious, 0.26f)]
		[InlineData(CredTier.Legend,    0.18f)]
		public void RefusalChance_MatchesSpecAtEachTier(CredTier tier, float expected)
		{
			Assert.Equal(expected, RazorkinRefusal.RefusalChance(tier), precision: 2);
		}

		// ── Floor is always present ───────────────────────────────────────────────

		[Fact]
		public void RefusalChance_AlwaysAtLeastFloor()
		{
			foreach (CredTier tier in Enum.GetValues(typeof(CredTier)))
				Assert.True(
					RazorkinRefusal.RefusalChance(tier) >= RazorkinRefusal.Floor,
					$"Tier {tier} refusal chance falls below the floor.");
		}

		[Fact]
		public void RefusalChance_LegendEqualsFloorOnly()
		{
			// At Legend the penalty is 0 — only the floor remains
			Assert.Equal(0.0f, RazorkinRefusal.CreditPenalty(CredTier.Legend));
			Assert.Equal(RazorkinRefusal.Floor,
			             RazorkinRefusal.RefusalChance(CredTier.Legend),
			             precision: 4);
		}

		// ── Monotonically decreasing with tier ────────────────────────────────────

		[Fact]
		public void RefusalChance_StrictlyDecreasing_NamelessToLegend()
		{
			var tiers = (CredTier[])Enum.GetValues(typeof(CredTier));
			for (int i = 1; i < tiers.Length; i++)
				Assert.True(
					RazorkinRefusal.RefusalChance(tiers[i]) <
					RazorkinRefusal.RefusalChance(tiers[i - 1]),
					$"Expected tier {tiers[i]} chance < {tiers[i-1]} chance.");
		}

		// ── CreditPenalty decays to zero at Legend ────────────────────────────────

		[Fact]
		public void CreditPenalty_NamelessIsHighest()
		{
			float namelessPenalty = RazorkinRefusal.CreditPenalty(CredTier.Nameless);
			foreach (CredTier tier in Enum.GetValues(typeof(CredTier)))
				Assert.True(
					RazorkinRefusal.CreditPenalty(tier) <= namelessPenalty,
					$"Tier {tier} penalty exceeds Nameless penalty.");
		}

		// ── Statistical distribution (seeded, deterministic) ─────────────────────

		[Fact]
		public void IsRefused_Nameless_RefusesApproximatelySixtyFivePercent()
		{
			// 10,000 rolls with a fixed seed — result is deterministic.
			// Tolerance ±5pp: if this flakes, widen the tolerance, don't change the seed.
			int rolls   = 10_000;
			int refused = 0;
			var rng     = new Random(42);

			for (int i = 0; i < rolls; i++)
				if (RazorkinRefusal.IsRefused(CredTier.Nameless, rng))
					refused++;

			double rate = (double)refused / rolls;
			Assert.InRange(rate, 0.60, 0.70); // 65% ±5pp
		}

		[Fact]
		public void IsRefused_Legend_RefusesApproximatelyFloor()
		{
			int rolls   = 10_000;
			int refused = 0;
			var rng     = new Random(42);

			for (int i = 0; i < rolls; i++)
				if (RazorkinRefusal.IsRefused(CredTier.Legend, rng))
					refused++;

			double rate = (double)refused / rolls;
			Assert.InRange(rate, 0.13, 0.23); // 18% floor ±5pp
		}

		[Fact]
		public void IsRefused_AllTiers_RefusalNeverZero()
		{
			// No tier should ever produce 0 refusals in 1,000 rolls.
			// The floor guarantees at least ~18% chance.
			int rolls = 1_000;
			foreach (CredTier tier in Enum.GetValues(typeof(CredTier)))
			{
				var rng     = new Random(99);
				int refused = 0;
				for (int i = 0; i < rolls; i++)
					if (RazorkinRefusal.IsRefused(tier, rng))
						refused++;

				Assert.True(refused > 0,
					$"Tier {tier} produced zero refusals in {rolls} rolls — " +
					"floor is not being applied.");
			}
		}
	}
}
