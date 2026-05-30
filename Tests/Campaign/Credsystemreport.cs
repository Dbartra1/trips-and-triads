using System;
using Xunit;
using Xunit.Abstractions;
using TripsAndTriads.Core;

namespace TripsAndTriads.Tests.Campaign
{
	/// <summary>
	/// Not a correctness test — a diagnostic report.
	///
	/// Run this to see every value the cred system produces in a single
	/// formatted print. Useful for balance review and spotting drift.
	///
	/// Output is visible in:
	///   VS Test Explorer → test output panel
	///   CLI: dotnet test --verbosity normal (look for the test name)
	///   CLI with full output: dotnet test --logger "console;verbosity=detailed"
	/// </summary>
	public class CredSystemReport
	{
		private readonly ITestOutputHelper _out;

		public CredSystemReport(ITestOutputHelper output)
		{
			_out = output;
		}

		[Fact]
		public void PrintCredSystemReport()
		{
			var tiers = (CredTier[])Enum.GetValues(typeof(CredTier));

			_out.WriteLine("");
			_out.WriteLine("╔══════════════════════════════════════════════════════════════════╗");
			_out.WriteLine("║               STREET CRED SYSTEM — BALANCE REPORT               ║");
			_out.WriteLine("╚══════════════════════════════════════════════════════════════════╝");

			// ── Ladder ───────────────────────────────────────────────────────────
			_out.WriteLine("");
			_out.WriteLine("── THE LADDER ─────────────────────────────────────────────────────");
			_out.WriteLine($"  {"Tier",-12} {"Range",-12} {"Floor",-8} {"Ceiling",-8}");
			_out.WriteLine($"  {"────",-12} {"─────",-12} {"─────",-8} {"───────",-8}");
			foreach (var tier in tiers)
			{
				int floor   = CredManager.TierFloor(tier);
				int ceiling = CredManager.TierCeiling(tier);
				_out.WriteLine($"  {tier,-12} {floor,3}–{ceiling,-8} {floor,-8} {ceiling,-8}");
			}
			_out.WriteLine($"  Step Up reset always lands at: {CredEvents.StepUpResetValue} (Known floor)");

			// ── Event deltas ─────────────────────────────────────────────────────
			_out.WriteLine("");
			_out.WriteLine("── CRED EVENTS ────────────────────────────────────────────────────");
			_out.WriteLine($"  {"Event",-30} {"Delta",6}");
			_out.WriteLine($"  {"─────",-30} {"─────",6}");
			foreach (CredEvent ev in Enum.GetValues(typeof(CredEvent)))
			{
				int delta = CredEvents.DeltaFor(ev);
				string sign = delta >= 0 ? "+" : "";
				_out.WriteLine($"  {ev,-30} {sign}{delta,5}");
			}

			// ── Stacking examples ────────────────────────────────────────────────
			_out.WriteLine("");
			_out.WriteLine("── STACKING EXAMPLES ──────────────────────────────────────────────");
			PrintStack("Win in The Stub",
				CredEvent.WinMatch);
			PrintStack("Win in The Hush / Vault",
				CredEvent.WinMatch, CredEvent.WinDangerousDistrict);
			PrintStack("Win vs Razorkin (normal district)",
				CredEvent.WinMatch, CredEvent.WinVsRazorkin);
			PrintStack("Win vs Razorkin in The Hush (max stack)",
				CredEvent.WinMatch, CredEvent.WinDangerousDistrict, CredEvent.WinVsRazorkin);
			PrintStack("Win + flip district control",
				CredEvent.WinMatch, CredEvent.FlipDistrictControl);
			PrintStack("Hunt reclaim by duel",
				CredEvent.WinMatch, CredEvent.HuntReclaimByDuel);
			PrintStack("Loss",
				CredEvent.LoseMatch);
			PrintStack("Buyout hero ransom",
				CredEvent.BuyoutHero);

			// ── Effects by tier ──────────────────────────────────────────────────
			_out.WriteLine("");
			_out.WriteLine("── EFFECTS BY TIER ────────────────────────────────────────────────");
			_out.WriteLine($"  {"Tier",-12} {"Income×",-10} {"Debt%/turn",-12} {"Ransom disc",-13} {"Control×",-10}");
			_out.WriteLine($"  {"────",-12} {"───────",-10} {"──────────",-12} {"───────────",-13} {"────────",-10}");
			foreach (var tier in tiers)
			{
				float income   = CredEffects.IncomeMultiplier(tier);
				float debt     = CredEffects.DebtInterestRate(tier) * 100f;
				float discount = CredEffects.RansomDiscount(tier) * 100f;
				float ctrl     = CredEffects.ControlShiftMultiplier(tier);
				_out.WriteLine(
					$"  {tier,-12} {income,-10:F2} {debt,-12:F0}% {discount,-13:F0}% {ctrl,-10:F2}");
			}

			// ── Razorkin refusal rates ────────────────────────────────────────────
			_out.WriteLine("");
			_out.WriteLine("── RAZORKIN BUYOUT REFUSAL ────────────────────────────────────────");
			_out.WriteLine($"  Immovable floor: {RazorkinRefusal.Floor * 100:F0}%  " +
			               "(no crew can buy this down)");
			_out.WriteLine("");
			_out.WriteLine($"  {"Tier",-12} {"Penalty",-10} {"Total chance",-14} {"In 100 attempts"}");
			_out.WriteLine($"  {"────",-12} {"───────",-10} {"────────────",-14} {"───────────────"}");
			foreach (var tier in tiers)
			{
				float penalty = RazorkinRefusal.CreditPenalty(tier);
				float total   = RazorkinRefusal.RefusalChance(tier);
				int   per100  = (int)System.Math.Round(total * 100);
				_out.WriteLine(
					$"  {tier,-12} {penalty * 100,-10:F0}% {total * 100,-14:F0}% ~{per100} refusals");
			}

			// ── Debt interest simulation ──────────────────────────────────────────
			_out.WriteLine("");
			_out.WriteLine("── DEBT GROWTH SIMULATION (100 scrip debt, 5 turns) ───────────────");
			_out.WriteLine($"  {"Tier",-12} {"Turn 1",-10} {"Turn 2",-10} {"Turn 3",-10} {"Turn 4",-10} {"Turn 5",-10}");
			_out.WriteLine($"  {"────",-12} {"──────",-10} {"──────",-10} {"──────",-10} {"──────",-10} {"──────",-10}");
			foreach (var tier in tiers)
			{
				float rate  = CredEffects.DebtInterestRate(tier);
				float debt  = 100f;
				string row  = $"  {tier,-12}";
				for (int t = 1; t <= 5; t++)
				{
					debt  *= (1f + rate);
					row   += $" {debt,-10:F1}";
				}
				_out.WriteLine(row);
			}
			_out.WriteLine("");
			_out.WriteLine("  (A Nameless crew's 100 scrip debt becomes " +
			               $"{100f * MathF.Pow(1.25f, 5):F1} scrip after 5 turns.)");
			_out.WriteLine("  (A Legend crew's 100 scrip debt becomes " +
			               $"{100f * MathF.Pow(1.02f, 5):F1} scrip after 5 turns.)");

			// ── Matches to reach each tier from zero ─────────────────────────────
			_out.WriteLine("");
			_out.WriteLine("── MATCHES TO CLIMB (wins only, no bonuses) ───────────────────────");
			_out.WriteLine($"  +2 per win (The Stub), starting from 0:");
			int cred = 0;
			int wins = 0;
			CredTier prev = CredTier.Nameless;
			while (cred < 80)
			{
				cred += 2; wins++;
				CredTier current = CredManager.TierFor(cred);
				if (current != prev)
				{
					_out.WriteLine($"  → {current,-12} after {wins,3} wins  (cred {cred})");
					prev = current;
				}
			}
			_out.WriteLine($"  → Legend      after {wins,3} wins  (cred {cred})");

			_out.WriteLine("");
			_out.WriteLine("  +6 per win (The Hush, win + dangerous bonus), starting from 0:");
			cred = 0; wins = 0; prev = CredTier.Nameless;
			while (cred < 80)
			{
				cred = System.Math.Min(cred + 6, 100); wins++;
				CredTier current = CredManager.TierFor(cred);
				if (current != prev)
				{
					_out.WriteLine($"  → {current,-12} after {wins,3} wins  (cred {cred})");
					prev = current;
				}
			}
			_out.WriteLine($"  → Legend      after {wins,3} wins  (cred {cred})");

			_out.WriteLine("");
			_out.WriteLine("══════════════════════════════════════════════════════════════════");

			// The test always passes — this is a report, not an assertion.
			Assert.True(true);
		}

		private void PrintStack(string label, params CredEvent[] events)
		{
			int total = 0;
			var parts = new System.Collections.Generic.List<string>();
			foreach (var ev in events)
			{
				int d = CredEvents.DeltaFor(ev);
				parts.Add($"{(d >= 0 ? "+" : "")}{d}");
				total += d;
			}
			string sign = total >= 0 ? "+" : "";
			_out.WriteLine($"  {label,-42} = {string.Join(" ", parts)} = {sign}{total}");
		}
	}
}