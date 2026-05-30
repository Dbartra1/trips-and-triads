using Xunit;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// Pins the contract for ScripPayoutCalculator (systems.md §9, Appendix A.4).
	///
	/// Covers:
	///   - Loss always returns 0
	///   - The Stub uses Della's base rate (10) with ×1.0 danger
	///   - Faction districts use Standard base (20) with ×1.3 danger
	///   - The Hush and Vault use ×2.0 danger
	///   - Income multiplier scales with cred tier
	///   - Known representative values match spec arithmetic
	/// </summary>
	public class ScripPayoutTests
	{
		// ── Loss always pays nothing ──────────────────────────────────────────────

		[Theory]
		[InlineData("the_stub",        CredTier.Nameless)]
		[InlineData("glass_spire",     CredTier.Known)]
		[InlineData("the_hush",        CredTier.Notorious)]
		[InlineData("the_vault",       CredTier.Legend)]
		public void Loss_AlwaysReturnsZero(string districtId, CredTier tier)
		{
			int payout = ScripPayoutCalculator.Calculate(districtId, playerWon: false, tier);
			Assert.Equal(0, payout);
		}

		// ── The Stub: base 10 × 1.0 danger ───────────────────────────────────────

		[Fact]
		public void TheStub_Win_Nameless_Returns10()
		{
			// 10 × 1.0 (danger) × 1.0 (Nameless income) = 10
			int payout = ScripPayoutCalculator.Calculate("the_stub", playerWon: true, CredTier.Nameless);
			Assert.Equal(10, payout);
		}

		[Fact]
		public void TheStub_Win_Legend_Returns25()
		{
			// 10 × 1.0 (danger) × 2.5 (Legend income) = 25
			int payout = ScripPayoutCalculator.Calculate("the_stub", playerWon: true, CredTier.Legend);
			Assert.Equal(25, payout);
		}

		// ── Faction districts: base 20 × 1.3 danger ──────────────────────────────

		[Fact]
		public void FactionDistrict_Win_Nameless_Returns26()
		{
			// 20 × 1.3 × 1.0 = 26.0 → floor = 26
			int payout = ScripPayoutCalculator.Calculate("glass_spire", playerWon: true, CredTier.Nameless);
			Assert.Equal(26, payout);
		}

		[Fact]
		public void FactionDistrict_Win_Known_Returns33()
		{
			// 20 × 1.3 × 1.3 = 33.8 → floor = 33
			int payout = ScripPayoutCalculator.Calculate("the_killfloor", playerWon: true, CredTier.Known);
			Assert.Equal(33, payout);
		}

		[Fact]
		public void FactionDistrict_Win_Named_Returns41()
		{
			// 20 × 1.3 × 1.6 = 41.6 → floor = 41
			int payout = ScripPayoutCalculator.Calculate("the_powder_room", playerWon: true, CredTier.Named);
			Assert.Equal(41, payout);
		}

		[Fact]
		public void FactionDistrict_Win_Legend_Returns65()
		{
			// 20 × 1.3 × 2.5 = 65.0 → floor = 65
			int payout = ScripPayoutCalculator.Calculate("dead_channel", playerWon: true, CredTier.Legend);
			Assert.Equal(65, payout);
		}

		// ── The Hush: base 20 × 2.0 danger ───────────────────────────────────────

		[Fact]
		public void TheHush_Win_Nameless_Returns40()
		{
			// 20 × 2.0 × 1.0 = 40
			int payout = ScripPayoutCalculator.Calculate("the_hush", playerWon: true, CredTier.Nameless);
			Assert.Equal(40, payout);
		}

		[Fact]
		public void TheHush_Win_Named_Returns64()
		{
			// 20 × 2.0 × 1.6 = 64
			int payout = ScripPayoutCalculator.Calculate("the_hush", playerWon: true, CredTier.Named);
			Assert.Equal(64, payout);
		}

		// ── The Vault: same danger as The Hush ────────────────────────────────────

		[Fact]
		public void TheVault_Win_Legend_Returns100()
		{
			// 20 × 2.0 × 2.5 = 100
			int payout = ScripPayoutCalculator.Calculate("the_vault", playerWon: true, CredTier.Legend);
			Assert.Equal(100, payout);
		}

		// ── DangerMultiplier helper exposed correctly ─────────────────────────────

		[Theory]
		[InlineData("the_stub",         1.0f)]
		[InlineData("the_hush",         2.0f)]
		[InlineData("the_vault",        2.0f)]
		[InlineData("glass_spire",      1.3f)]
		[InlineData("the_killfloor",    1.3f)]
		[InlineData("dead_channel",     1.3f)]
		[InlineData("the_sprawl_market",1.3f)]
		[InlineData("the_powder_room",  1.3f)]
		public void DangerMultiplier_MatchesDistrictCategory(string districtId, float expected)
		{
			Assert.Equal(expected, ScripPayoutCalculator.DangerMultiplier(districtId), precision: 2);
		}

		// ── BasePayout helper ─────────────────────────────────────────────────────

		[Fact]
		public void BasePayout_TheStub_Returns10()
		{
			Assert.Equal(10f, ScripPayoutCalculator.BasePayout("the_stub"), precision: 2);
		}

		[Fact]
		public void BasePayout_FactionDistrict_Returns20()
		{
			Assert.Equal(20f, ScripPayoutCalculator.BasePayout("the_killfloor"), precision: 2);
		}

		// ── Payout strictly increases with cred tier (same district) ─────────────

		[Fact]
		public void Payout_IncreasesWithCredTier_FactionDistrict()
		{
			var tiers = System.Enum.GetValues<CredTier>();
			int prev = -1;
			foreach (CredTier tier in tiers)
			{
				int payout = ScripPayoutCalculator.Calculate("glass_spire", playerWon: true, tier);
				Assert.True(payout > prev,
					$"Expected payout at {tier}({payout}) > prior tier({prev})");
				prev = payout;
			}
		}
	}
}
