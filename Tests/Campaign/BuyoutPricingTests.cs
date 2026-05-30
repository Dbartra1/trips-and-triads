using Xunit;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// Pins the contract for BuyoutPricing (systems.md §7.4).
	///
	/// Covers:
	///   - HollowChoir always unavailable
	///   - Ascendant ignores cred discount (non-negotiable)
	///   - All other factions apply the cred discount at Named tier
	///   - Escalation: each failed attempt raises cost by 60%
	///   - Combined: escalation stacks on top of discount
	///   - GetBaseCost() returns raw values regardless of other factors
	/// </summary>
	public class BuyoutPricingTests
	{
		// ── Availability ──────────────────────────────────────────────────────────

		[Fact]
		public void HollowChoir_IsNeverAvailable()
		{
			Assert.False(BuyoutPricing.IsAvailable(Faction.HollowChoir));
		}

		[Fact]
		public void FactionNone_IsNeverAvailable()
		{
			Assert.False(BuyoutPricing.IsAvailable(Faction.None));
		}

		[Theory]
		[InlineData(Faction.Commons)]
		[InlineData(Faction.Razorkin)]
		[InlineData(Faction.Ghostwire)]
		[InlineData(Faction.Effigy)]
		[InlineData(Faction.Ascendant)]
		[InlineData(Faction.Lacquer)]
		public void AllNonChoirFactions_AreAvailable(Faction faction)
		{
			Assert.True(BuyoutPricing.IsAvailable(faction));
		}

		// ── ComputeCost returns -1 when unavailable ───────────────────────────────

		[Fact]
		public void ComputeCost_HollowChoir_ReturnsMinusOne()
		{
			int cost = BuyoutPricing.ComputeCost(Faction.HollowChoir, CredTier.Legend, 0);
			Assert.Equal(-1, cost);
		}

		// ── Base costs at Nameless (no discount, no escalation) ──────────────────

		[Theory]
		[InlineData(Faction.Commons,   10)]
		[InlineData(Faction.Razorkin,  15)]
		[InlineData(Faction.Ghostwire, 25)]
		[InlineData(Faction.Effigy,    25)]
		[InlineData(Faction.Ascendant, 40)]
		[InlineData(Faction.Lacquer,   60)]
		public void ComputeCost_Nameless_NoFails_ReturnsBaseCost(Faction faction, int expected)
		{
			// At Nameless, RansomPriceMultiplier = 1.0 (no discount).
			// Ascendant is non-negotiable but also has multiplier 1.0 at Nameless,
			// so the result is the same regardless.
			int cost = BuyoutPricing.ComputeCost(faction, CredTier.Nameless, 0);
			Assert.Equal(expected, cost);
		}

		// ── Ascendant ignores cred discount ───────────────────────────────────────

		[Theory]
		[InlineData(CredTier.Known)]
		[InlineData(CredTier.Named)]
		[InlineData(CredTier.Notorious)]
		[InlineData(CredTier.Legend)]
		public void ComputeCost_Ascendant_IgnoresCredDiscount(CredTier tier)
		{
			// Ascendant base = 40. Regardless of cred tier it should always be 40
			// (non-negotiable market rate).
			int cost = BuyoutPricing.ComputeCost(Faction.Ascendant, tier, 0);
			Assert.Equal(40, cost);
		}

		// ── Cred discount applies for non-Ascendant factions ─────────────────────

		[Fact]
		public void ComputeCost_Commons_Legend_AppliesDiscount()
		{
			// Commons base = 10. Legend discount = 10%. Expected: ceil(10 × 0.90) = 9.
			int cost = BuyoutPricing.ComputeCost(Faction.Commons, CredTier.Legend, 0);
			Assert.Equal(9, cost);
		}

		[Fact]
		public void ComputeCost_Lacquer_Named_AppliesDiscount()
		{
			// Lacquer base = 60. Named discount = 5%. Expected: ceil(60 × 0.95) = 57.
			int cost = BuyoutPricing.ComputeCost(Faction.Lacquer, CredTier.Named, 0);
			Assert.Equal(57, cost);
		}

		[Fact]
		public void ComputeCost_Ghostwire_Notorious_AppliesDiscount()
		{
			// Ghostwire base = 25. Notorious discount = 8%. Expected: ceil(25 × 0.92) = 23.
			int cost = BuyoutPricing.ComputeCost(Faction.Ghostwire, CredTier.Notorious, 0);
			Assert.Equal(23, cost);
		}

		// ── Escalation raises cost per failed attempt ─────────────────────────────

		[Fact]
		public void ComputeCost_Commons_OneFailedAttempt_RaisedBy60Percent()
		{
			// Commons base at Nameless = 10. One failure: ceil(10 × 1.60) = 16.
			int cost = BuyoutPricing.ComputeCost(Faction.Commons, CredTier.Nameless, 1);
			Assert.Equal(16, cost);
		}

		[Fact]
		public void ComputeCost_Razorkin_OneFailedAttempt_RaisedBy60Percent()
		{
			// Razorkin base at Nameless = 15. One failure: ceil(15 × 1.60) = 24.
			int cost = BuyoutPricing.ComputeCost(Faction.Razorkin, CredTier.Nameless, 1);
			Assert.Equal(24, cost);
		}

		[Fact]
		public void ComputeCost_Ascendant_OneFailedAttempt_StillRaises()
		{
			// Ascendant is non-negotiable on discount but escalation still applies.
			// base = 40. One failure: ceil(40 × 1.60) = 64.
			int cost = BuyoutPricing.ComputeCost(Faction.Ascendant, CredTier.Nameless, 1);
			Assert.Equal(64, cost);
		}

		// ── Escalation and discount stack correctly ───────────────────────────────

		[Fact]
		public void ComputeCost_Lacquer_Legend_OneFailedAttempt_DiscountThenEscalate()
		{
			// Lacquer base = 60. Legend discount = 10%: 60 × 0.90 = 54.
			// Then one failure: ceil(54 × 1.60) = ceil(86.4) = 87.
			int cost = BuyoutPricing.ComputeCost(Faction.Lacquer, CredTier.Legend, 1);
			Assert.Equal(87, cost);
		}

		// ── Negative failedAttempts clamped to zero ───────────────────────────────

		[Fact]
		public void ComputeCost_NegativeFailedAttempts_ClampedToZero()
		{
			int costAtZero  = BuyoutPricing.ComputeCost(Faction.Commons, CredTier.Nameless, 0);
			int costNeg     = BuyoutPricing.ComputeCost(Faction.Commons, CredTier.Nameless, -5);
			Assert.Equal(costAtZero, costNeg);
		}

		// ── GetBaseCost ───────────────────────────────────────────────────────────

		[Theory]
		[InlineData(Faction.Commons,   10)]
		[InlineData(Faction.Razorkin,  15)]
		[InlineData(Faction.Ghostwire, 25)]
		[InlineData(Faction.Effigy,    25)]
		[InlineData(Faction.Ascendant, 40)]
		[InlineData(Faction.Lacquer,   60)]
		public void GetBaseCost_ReturnsRawCost(Faction faction, int expected)
		{
			Assert.Equal(expected, BuyoutPricing.GetBaseCost(faction));
		}

		[Fact]
		public void GetBaseCost_HollowChoir_ReturnsZero()
		{
			Assert.Equal(0, BuyoutPricing.GetBaseCost(Faction.HollowChoir));
		}

		// ── Order: cheaper factions are cheaper ──────────────────────────────────

		[Fact]
		public void FactionOrder_CommonsChreaperThanLacquer_AtAllTiers()
		{
			foreach (CredTier tier in System.Enum.GetValues<CredTier>())
			{
				int commons = BuyoutPricing.ComputeCost(Faction.Commons,  tier, 0);
				int lacquer = BuyoutPricing.ComputeCost(Faction.Lacquer,  tier, 0);
				Assert.True(commons < lacquer,
					$"Expected Commons({commons}) < Lacquer({lacquer}) at {tier}");
			}
		}
	}
}
