using Xunit;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// Verifies that every CredEvent produces the correct delta and that events
	/// stack additively when multiple fire on the same turn.
	///
	/// These tests pin the contract between CredEvents.DeltaFor() and the
	/// spec (systems.md §8.2, Appendix A.1). If a value changes in the spec,
	/// update DeltaFor() — and this test will catch any mismatches.
	/// </summary>
	public class CredEventTests
	{
		// ── Individual deltas match the spec ─────────────────────────────────────

		[Theory]
		[InlineData(CredEvent.WinMatch,             2)]
		[InlineData(CredEvent.WinDangerousDistrict,  4)]
		[InlineData(CredEvent.WinVsRazorkin,         5)]
		[InlineData(CredEvent.FlipDistrictControl,   5)]
		[InlineData(CredEvent.HuntReclaimByDuel,     6)]
		[InlineData(CredEvent.LoseMatch,            -2)]
		[InlineData(CredEvent.BuyoutHero,           -4)]
		public void DeltaFor_MatchesSpecValues(CredEvent ev, int expected)
		{
			Assert.Equal(expected, CredEvents.DeltaFor(ev));
		}

		// ── ApplyEvents — single event ────────────────────────────────────────────

		[Fact]
		public void ApplyEvents_WinMatch_AddsPlusTwo()
		{
			var mgr = new CredManager(10);
			mgr.ApplyEvents(CredEvent.WinMatch);
			Assert.Equal(12, mgr.Cred);
		}

		[Fact]
		public void ApplyEvents_LoseMatch_GivesMinusTwo()
		{
			var mgr = new CredManager(10);
			mgr.ApplyEvents(CredEvent.LoseMatch);
			Assert.Equal(8, mgr.Cred);
		}

		[Fact]
		public void ApplyEvents_HuntReclaim_AddsPlusSix()
		{
			var mgr = new CredManager(30);
			mgr.ApplyEvents(CredEvent.HuntReclaimByDuel);
			Assert.Equal(36, mgr.Cred);
		}

		[Fact]
		public void ApplyEvents_Buyout_GivesMinusFour()
		{
			var mgr = new CredManager(30);
			mgr.ApplyEvents(CredEvent.BuyoutHero);
			Assert.Equal(26, mgr.Cred);
		}

		// ── Stacking — real combat scenarios ─────────────────────────────────────

		[Fact]
		public void Stack_WinMatchPlusDangerous_GivesSix()
		{
			// Win in The Hush or The Vault
			var mgr = new CredManager(20);
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.WinDangerousDistrict);
			Assert.Equal(26, mgr.Cred); // +2 +4
		}

		[Fact]
		public void Stack_WinMatchPlusRazorkin_GivesSeven()
		{
			// Beat a Razorkin crew in an ordinary district
			var mgr = new CredManager(20);
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.WinVsRazorkin);
			Assert.Equal(27, mgr.Cred); // +2 +5
		}

		[Fact]
		public void Stack_WinMatchPlusDangerousPlusRazorkin_GivesEleven()
		{
			// The maximum normal win stack:
			// Win + Dangerous district + Razorkin crew = +2 +4 +5 = +11
			var mgr = new CredManager(20);
			mgr.ApplyEvents(
				CredEvent.WinMatch,
				CredEvent.WinDangerousDistrict,
				CredEvent.WinVsRazorkin);
			Assert.Equal(31, mgr.Cred);
		}

		[Fact]
		public void Stack_WinMatchPlusFlipControl_GivesSeven()
		{
			var mgr = new CredManager(20);
			mgr.ApplyEvents(CredEvent.WinMatch, CredEvent.FlipDistrictControl);
			Assert.Equal(27, mgr.Cred); // +2 +5
		}

		[Fact]
		public void Stack_MultipleEventsClampsAtOneHundred()
		{
			var mgr = new CredManager(98);
			mgr.ApplyEvents(
				CredEvent.WinMatch,
				CredEvent.WinDangerousDistrict,
				CredEvent.WinVsRazorkin); // +11 — but cap at 100
			Assert.Equal(100, mgr.Cred);
		}

		[Fact]
		public void Stack_LossFollowedByWin_NetZero()
		{
			var mgr = new CredManager(30);
			mgr.ApplyEvents(CredEvent.LoseMatch);    // -2 → 28
			mgr.ApplyEvents(CredEvent.WinMatch);     // +2 → 30
			Assert.Equal(30, mgr.Cred);
		}

		// ── StepUpResetValue constant ─────────────────────────────────────────────

		[Fact]
		public void StepUpResetValue_IsTwenty()
		{
			// The spec says Step Up always resets to Known floor = 20 (§8.5).
			Assert.Equal(20, CredEvents.StepUpResetValue);
		}
	}
}
