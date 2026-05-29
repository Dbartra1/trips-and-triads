using Xunit;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// End-to-end tests that simulate a sequence of matches and verify that
	/// cred accumulates correctly, tier transitions happen at the right time,
	/// and downstream effects track the tier correctly throughout.
	///
	/// These tests act as the contract between the cred system and everything
	/// that reads it — if a downstream effect ever reads stale cred or the
	/// wrong tier, these tests will catch it.
	/// </summary>
	public class CredIntegrationTests
	{
		// ── Match outcome → cred update ───────────────────────────────────────────

		[Fact]
		public void WinInStub_UpdatesCredByTwo()
		{
			var mgr = new CredManager(0);
			mgr.ApplyEvents(CredEvent.WinMatch);
			Assert.Equal(2, mgr.Cred);
			Assert.Equal(CredTier.Nameless, mgr.Tier);
		}

		[Fact]
		public void LossInStub_UpdatesCredByMinusTwo()
		{
			var mgr = new CredManager(10);
			mgr.ApplyEvents(CredEvent.LoseMatch);
			Assert.Equal(8, mgr.Cred);
		}

		[Fact]
		public void WinInHush_GivesCorrectBonusStack()
		{
			// Win (+2) + Dangerous district (+4) = +6
			var mgr = new CredManager(30);
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.WinDangerousDistrict);
			Assert.Equal(36, mgr.Cred);
		}

		[Fact]
		public void HuntReclaim_AddsSix()
		{
			var mgr = new CredManager(25);
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.HuntReclaimByDuel);
			Assert.Equal(33, mgr.Cred); // +2 +6
		}

		// ── Multi-match accumulation ──────────────────────────────────────────────

		[Fact]
		public void TenWins_InStub_ReachKnown()
		{
			// 10 × +2 = +20 — should push a Nameless crew to Known
			var mgr = new CredManager(0);
			for (int i = 0; i < 10; i++)
				mgr.ApplyEvents(CredEvent.WinMatch);
			Assert.Equal(20, mgr.Cred);
			Assert.Equal(CredTier.Known, mgr.Tier);
		}

		[Fact]
		public void MultipleMatches_CredAccumulatesCorrectly()
		{
			var mgr = new CredManager(0);

			// Sequence: win, win, lose, win in Hush, Hunt reclaim
			// +2, +2, -2, (+2+4), (+2+6) = +16 total
			mgr.ApplyEvents(CredEvent.WinMatch);
			mgr.ApplyEvents(CredEvent.WinMatch);
			mgr.ApplyEvents(CredEvent.LoseMatch);
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.WinDangerousDistrict);
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.HuntReclaimByDuel);

			Assert.Equal(16, mgr.Cred);
		}

		// ── Effects track tier throughout a run ───────────────────────────────────

		[Fact]
		public void IncomeMultiplier_UpdatesAfterTierCrossing()
		{
			var mgr = new CredManager(39); // Known ceiling

			float incomeAtKnown = CredEffects.IncomeMultiplier(mgr.Tier);
			Assert.Equal(1.30f, incomeAtKnown, precision: 2);

			mgr.ApplyEvents(CredEvent.WinMatch); // +2 → 41, Named
			Assert.Equal(CredTier.Named, mgr.Tier);

			float incomeAtNamed = CredEffects.IncomeMultiplier(mgr.Tier);
			Assert.Equal(1.60f, incomeAtNamed, precision: 2);
		}

		[Fact]
		public void DebtRate_UpdatesAfterTierCrossing()
		{
			var mgr = new CredManager(59); // Named ceiling

			float rateAtNamed = CredEffects.DebtInterestRate(mgr.Tier);
			Assert.Equal(0.10f, rateAtNamed, precision: 2);

			mgr.ApplyEvents(CredEvent.WinMatch); // → 61, Notorious
			Assert.Equal(CredTier.Notorious, mgr.Tier);

			float rateAtNotorious = CredEffects.DebtInterestRate(mgr.Tier);
			Assert.Equal(0.05f, rateAtNotorious, precision: 2);
		}

		// ── Step Up resets correctly mid-run ─────────────────────────────────────

		[Fact]
		public void StepUp_ResetsToKnown_ThenContinuesClimbing()
		{
			var mgr = new CredManager(75); // Notorious

			mgr.StepUpReset();
			Assert.Equal(20, mgr.Cred);
			Assert.Equal(CredTier.Known, mgr.Tier);

			// New crew continues accumulating from Known floor
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.WinDangerousDistrict);
			Assert.Equal(26, mgr.Cred);
		}

		[Fact]
		public void StepUp_EffectsDropToKnown_NotNameless()
		{
			// After Step Up the crew is Known, not Nameless — they keep some rep
			var mgr = new CredManager(80); // Legend
			mgr.StepUpReset();

			// Known income, not Nameless income
			Assert.Equal(1.30f, CredEffects.IncomeMultiplier(mgr.Tier), precision: 2);
			// Known interest, not Nameless interest
			Assert.Equal(0.18f, CredEffects.DebtInterestRate(mgr.Tier), precision: 2);
		}

		// ── Legend cap behaviour ──────────────────────────────────────────────────

		[Fact]
		public void Legend_EffectsAreBest_AndCapped()
		{
			var mgr = new CredManager(100);

			Assert.Equal(2.50f, CredEffects.IncomeMultiplier(mgr.Tier),    precision: 2);
			Assert.Equal(0.02f, CredEffects.DebtInterestRate(mgr.Tier),    precision: 2);
			Assert.Equal(0.10f, CredEffects.RansomDiscount(mgr.Tier),      precision: 2);
			Assert.Equal(2.00f, CredEffects.ControlShiftMultiplier(mgr.Tier), precision: 2);

			// Further wins don't change anything
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.WinDangerousDistrict);
			Assert.Equal(100, mgr.Cred);
		}
	}
}
