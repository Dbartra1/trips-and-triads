using Xunit;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// Pure math tests for the CredManager ladder.
	/// No game state, no board — only integer arithmetic and tier derivation.
	///
	/// Invariants:
	///   - Cred is always in [0, 100]
	///   - Tier boundaries are exactly 0/20/40/60/80
	///   - Step Up always resets to 20 (Known floor)
	/// </summary>
	public class CredLadderTests
	{
		// ── Tier boundaries ──────────────────────────────────────────────────────

		[Theory]
		[InlineData( 0,  CredTier.Nameless)]
		[InlineData(19,  CredTier.Nameless)]
		[InlineData(20,  CredTier.Known)]
		[InlineData(39,  CredTier.Known)]
		[InlineData(40,  CredTier.Named)]
		[InlineData(59,  CredTier.Named)]
		[InlineData(60,  CredTier.Notorious)]
		[InlineData(79,  CredTier.Notorious)]
		[InlineData(80,  CredTier.Legend)]
		[InlineData(100, CredTier.Legend)]
		public void TierFor_CorrectAtEveryBoundary(int cred, CredTier expected)
		{
			Assert.Equal(expected, CredManager.TierFor(cred));
		}

		// ── Clamping ─────────────────────────────────────────────────────────────

		[Fact]
		public void Apply_CannotExceedOneHundred()
		{
			var mgr = new CredManager(95);
			mgr.Apply(20);
			Assert.Equal(100, mgr.Cred);
		}

		[Fact]
		public void Apply_CannotGoBelowZero()
		{
			var mgr = new CredManager(5);
			mgr.Apply(-20);
			Assert.Equal(0, mgr.Cred);
		}

		[Fact]
		public void Apply_ExactlyAtCeilingStaysAtCeiling()
		{
			var mgr = new CredManager(100);
			mgr.Apply(1);
			Assert.Equal(100, mgr.Cred);
		}

		[Fact]
		public void Apply_ExactlyAtFloorStaysAtFloor()
		{
			var mgr = new CredManager(0);
			mgr.Apply(-1);
			Assert.Equal(0, mgr.Cred);
		}

		// ── Tier transitions ─────────────────────────────────────────────────────

		[Fact]
		public void Apply_PositiveDelta_CrossesTierBoundaryCorrectly()
		{
			var mgr = new CredManager(39); // Known ceiling
			Assert.Equal(CredTier.Known, mgr.Tier);
			mgr.Apply(1);
			Assert.Equal(CredTier.Named, mgr.Tier);
			Assert.Equal(40, mgr.Cred);
		}

		[Fact]
		public void Apply_NegativeDelta_CrossesTierBoundaryCorrectly()
		{
			var mgr = new CredManager(40); // Named floor
			Assert.Equal(CredTier.Named, mgr.Tier);
			mgr.Apply(-1);
			Assert.Equal(CredTier.Known, mgr.Tier);
			Assert.Equal(39, mgr.Cred);
		}

		[Fact]
		public void Apply_CanCrossMultipleTiersInOneStep()
		{
			var mgr = new CredManager(10); // Nameless
			mgr.Apply(55);
			Assert.Equal(65, mgr.Cred);
			Assert.Equal(CredTier.Notorious, mgr.Tier);
		}

		// ── Step Up reset ────────────────────────────────────────────────────────

		[Theory]
		[InlineData(0)]
		[InlineData(10)]
		[InlineData(50)]
		[InlineData(100)]
		public void StepUpReset_AlwaysLandsAtTwenty(int startingCred)
		{
			var mgr = new CredManager(startingCred);
			mgr.StepUpReset();
			Assert.Equal(CredEvents.StepUpResetValue, mgr.Cred);
		}

		[Fact]
		public void StepUpReset_LandsAtKnownFloor()
		{
			var mgr = new CredManager(90); // Legend
			mgr.StepUpReset();
			Assert.Equal(CredTier.Known, mgr.Tier);
		}

		// ── TierFloor / TierCeiling ──────────────────────────────────────────────

		[Theory]
		[InlineData(CredTier.Nameless,  0,  19)]
		[InlineData(CredTier.Known,    20,  39)]
		[InlineData(CredTier.Named,    40,  59)]
		[InlineData(CredTier.Notorious,60,  79)]
		[InlineData(CredTier.Legend,   80, 100)]
		public void TierFloorAndCeiling_AreConsistentWithTierFor(
			CredTier tier, int floor, int ceiling)
		{
			Assert.Equal(floor,   CredManager.TierFloor(tier));
			Assert.Equal(ceiling, CredManager.TierCeiling(tier));
			Assert.Equal(tier,    CredManager.TierFor(floor));
			Assert.Equal(tier,    CredManager.TierFor(ceiling));
		}

		// ── Initialisation ───────────────────────────────────────────────────────

		[Fact]
		public void Constructor_DefaultStartsAtZero()
		{
			var mgr = new CredManager();
			Assert.Equal(0, mgr.Cred);
			Assert.Equal(CredTier.Nameless, mgr.Tier);
		}

		[Fact]
		public void Constructor_ClampsNegativeStartingValue()
		{
			var mgr = new CredManager(-10);
			Assert.Equal(0, mgr.Cred);
		}

		[Fact]
		public void Constructor_ClampsOverOneHundred()
		{
			var mgr = new CredManager(999);
			Assert.Equal(100, mgr.Cred);
		}
	}
}
