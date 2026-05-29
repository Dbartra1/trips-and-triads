using Xunit;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// Verifies that every downstream cred effect returns the correct value at
	/// every tier (systems.md §8.4, Appendix A.2).
	///
	/// These tests nail the spec values as constants. If balance tuning changes
	/// a value, update CredEffects and the test together — no silent drift.
	///
	/// Also verifies two structural invariants that must hold regardless of
	/// specific values:
	///   - Effects are monotonically improving with tier (income goes up,
	///     interest goes down, discount goes up, control shift goes up).
	///   - Nameless never gets a discount; Legend never pays 25% interest.
	/// </summary>
	public class CredEffectsTests
	{
		// ── Income multiplier ─────────────────────────────────────────────────────

		[Theory]
		[InlineData(CredTier.Nameless,  1.00f)]
		[InlineData(CredTier.Known,     1.30f)]
		[InlineData(CredTier.Named,     1.60f)]
		[InlineData(CredTier.Notorious, 2.00f)]
		[InlineData(CredTier.Legend,    2.50f)]
		public void IncomeMultiplier_MatchesSpecAtEachTier(CredTier tier, float expected)
		{
			Assert.Equal(expected, CredEffects.IncomeMultiplier(tier), precision: 2);
		}

		[Fact]
		public void IncomeMultiplier_MonotonicallyIncreasing()
		{
			var tiers = (CredTier[])System.Enum.GetValues(typeof(CredTier));
			for (int i = 1; i < tiers.Length; i++)
				Assert.True(
					CredEffects.IncomeMultiplier(tiers[i]) >
					CredEffects.IncomeMultiplier(tiers[i - 1]),
					$"Expected {tiers[i]} multiplier > {tiers[i-1]} multiplier");
		}

		[Fact]
		public void IncomeMultiplier_NamelessIsOne()
		{
			Assert.Equal(1.0f, CredEffects.IncomeMultiplier(CredTier.Nameless));
		}

		// ── Debt interest rate ────────────────────────────────────────────────────

		[Theory]
		[InlineData(CredTier.Nameless,  0.25f)]
		[InlineData(CredTier.Known,     0.18f)]
		[InlineData(CredTier.Named,     0.10f)]
		[InlineData(CredTier.Notorious, 0.05f)]
		[InlineData(CredTier.Legend,    0.02f)]
		public void DebtInterestRate_MatchesSpecAtEachTier(CredTier tier, float expected)
		{
			Assert.Equal(expected, CredEffects.DebtInterestRate(tier), precision: 2);
		}

		[Fact]
		public void DebtInterestRate_MonotonicallyDecreasing()
		{
			var tiers = (CredTier[])System.Enum.GetValues(typeof(CredTier));
			for (int i = 1; i < tiers.Length; i++)
				Assert.True(
					CredEffects.DebtInterestRate(tiers[i]) <
					CredEffects.DebtInterestRate(tiers[i - 1]),
					$"Expected {tiers[i]} rate < {tiers[i-1]} rate");
		}

		[Fact]
		public void DebtInterestRate_LegendIsNotTwentyFivePercent()
		{
			// Structural sanity: the best rate must be far below the worst rate
			Assert.True(CredEffects.DebtInterestRate(CredTier.Legend) <
			            CredEffects.DebtInterestRate(CredTier.Nameless) * 0.2f);
		}

		// ── Ransom discount ───────────────────────────────────────────────────────

		[Theory]
		[InlineData(CredTier.Nameless,  0.00f)]
		[InlineData(CredTier.Known,     0.03f)]
		[InlineData(CredTier.Named,     0.05f)]
		[InlineData(CredTier.Notorious, 0.08f)]
		[InlineData(CredTier.Legend,    0.10f)]
		public void RansomDiscount_MatchesSpecAtEachTier(CredTier tier, float expected)
		{
			Assert.Equal(expected, CredEffects.RansomDiscount(tier), precision: 2);
		}

		[Fact]
		public void RansomDiscount_MonotonicallyIncreasing()
		{
			var tiers = (CredTier[])System.Enum.GetValues(typeof(CredTier));
			for (int i = 1; i < tiers.Length; i++)
				Assert.True(
					CredEffects.RansomDiscount(tiers[i]) >=
					CredEffects.RansomDiscount(tiers[i - 1]),
					$"Expected {tiers[i]} discount >= {tiers[i-1]} discount");
		}

		[Fact]
		public void RansomDiscount_NamelessGetsNoDiscount()
		{
			Assert.Equal(0.0f, CredEffects.RansomDiscount(CredTier.Nameless));
		}

		[Fact]
		public void RansomPriceMultiplier_AndDiscount_SumToOne()
		{
			foreach (CredTier tier in System.Enum.GetValues(typeof(CredTier)))
				Assert.Equal(1.0f,
					CredEffects.RansomPriceMultiplier(tier) +
					CredEffects.RansomDiscount(tier),
					precision: 4);
		}

		// ── Control shift multiplier ──────────────────────────────────────────────

		[Theory]
		[InlineData(CredTier.Nameless,  1.00f)]
		[InlineData(CredTier.Known,     1.25f)]
		[InlineData(CredTier.Named,     1.50f)]
		[InlineData(CredTier.Notorious, 1.75f)]
		[InlineData(CredTier.Legend,    2.00f)]
		public void ControlShiftMultiplier_MatchesSpecAtEachTier(CredTier tier, float expected)
		{
			Assert.Equal(expected, CredEffects.ControlShiftMultiplier(tier), precision: 2);
		}

		[Fact]
		public void ControlShiftMultiplier_LegendIsDoubleNameless()
		{
			Assert.Equal(
				CredEffects.ControlShiftMultiplier(CredTier.Nameless) * 2.0f,
				CredEffects.ControlShiftMultiplier(CredTier.Legend),
				precision: 4);
		}
	}
}
