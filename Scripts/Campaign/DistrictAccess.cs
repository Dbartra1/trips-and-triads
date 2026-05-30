using System.Collections.Generic;
using TripsAndTriads.Core;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Defines the cred-gating rules for every district (systems.md §8.4, Option D+C).
	///
	/// Gate types:
	///   AlwaysOpen  — any crew enters regardless of tier
	///   HardLock    — requires minimum tier; enforced with a 3-match grace period
	///   SoftGate    — no lock, but adds a protocol penalty below the threshold
	///
	/// ⚠ LORE OPEN THREADS — marked where faction gating needs narrative justification.
	/// These should be addressed through in-game events or dialogue before release.
	/// </summary>
	public static class DistrictAccess
	{
		public enum GateType { AlwaysOpen, HardLock, SoftGate }

		public record DistrictGate(
			GateType  Type,
			CredTier  MinTier,
			string    LockReason,   // shown to player when locked/warned
			string    LoreNote      // open thread — addressed in story pass
		);

		/// <summary>How many matches a crew has after dropping below threshold.</summary>
		public const int GraceMatches = 3;

		private static readonly Dictionary<string, DistrictGate> Gates = new()
		{
			["the_stub"] = new(
				GateType.AlwaysOpen, CredTier.Nameless,
				"",
				""),

			["glass_spire"] = new(
				GateType.HardLock, CredTier.Known,
				"Ascendant requires a verified identity. Nameless crews are turned away at the door.",
				// ⚠ LORE OPEN THREAD: What exactly is 'verified identity' in this city?
				// Is it a biometric tag, a debt record, a corporate file? Needs a lore beat
				// — perhaps a one-time registration event when the crew first reaches Known.
				"LORE OPEN: Define what Ascendant's 'verified identity' check looks like mechanically."),

			["the_killfloor"] = new(
				GateType.AlwaysOpen, CredTier.Nameless,
				"",
				// ⚠ LORE NOTE: Razorkin actively want to fight unknowns — it's how they
				// prove dominance. The Killfloor being always-open is correct and intentional.
				"LORE NOTE: Always-open is correct. Razorkin fight anyone. No thread."),

			["dead_channel"] = new(
				GateType.SoftGate, CredTier.Known,
				"Below-tier crews run under Intercept — Ghostwire is watching.",
				// ⚠ LORE OPEN THREAD: Ghostwire doesn't bar anyone, but the runners
				// who run Dead Channel want to know who they're dealing with. The Intercept
				// penalty is the mechanic expression of being surveilled — but why would a
				// Ghostwire runner care about cred specifically vs. just running comms?
				// Needs a lore beat explaining why the channel is 'open to all' but surveilled.
				"LORE OPEN: Why does Dead Channel apply Intercept to low-cred crews specifically?"),

			["the_sprawl_market"] = new(
				GateType.AlwaysOpen, CredTier.Nameless,
				"",
				// ⚠ LORE NOTE: The Commons doesn't gatekeep. Everyone eats, everyone plays.
				// Always-open is a core part of Commons identity.
				"LORE NOTE: Always-open is correct. Commons gatekeeping would be off-character."),

			["the_powder_room"] = new(
				GateType.HardLock, CredTier.Named,
				"Lacquer has standards. The Powder Room doesn't receive unnamed crews.",
				// ⚠ LORE OPEN THREAD: Lacquer runs on debt and obligation — they'd *want*
				// desperate crews in their district. The social vetting angle needs work.
				// Consider: maybe Lacquer locks it not because they want to keep people out,
				// but because they want people to *earn* their way in — arriving already
				// in debt to someone who vouches for you. A Fixer vouch (Phase 9) could
				// be an alternative unlock path.
				"LORE OPEN: Lacquer would profit from desperate crews. Rethink gate as social vetting or Fixer-vouch alternative."),

			["the_hush"] = new(
				GateType.HardLock, CredTier.Notorious,
				"The Choir only receives crews the city has already noticed.",
				// ⚠ LORE OPEN THREAD: How does the Hollow Choir know a crew's cred tier?
				// They're not corporate. They don't run surveillance. The most interesting
				// answer: the Antecedent tells them. Named or below simply aren't visible
				// to the thing behind the Wall — they don't register. Only when a crew
				// becomes Notorious does the Black Wall 'notice' them. This could be a
				// meaningful reveal beat — the Choir aren't gatekeeping, they're reporting
				// what the Wall has observed.
				"LORE OPEN: The Choir's awareness of cred tiers should be explained via the Antecedent — the Wall notices who matters. Needs a story beat."),

			["the_vault"] = new(
				GateType.HardLock, CredTier.Legend,
				"The Vault demands a Legend's reputation. The city does not open this door for anyone less.",
				// No lore thread — this gate is structural and intentional.
				""),
		};

		// ── Public API ────────────────────────────────────────────────────────────

		public static DistrictGate GetGate(string districtId)
			=> Gates.TryGetValue(districtId, out var gate) ? gate : new(GateType.AlwaysOpen, CredTier.Nameless, "", "");

		/// <summary>
		/// True if the crew's current cred meets the gate's minimum tier.
		/// Always true for AlwaysOpen districts.
		/// </summary>
		public static bool MeetsTierRequirement(string districtId, CredTier credTier)
		{
			var gate = GetGate(districtId);
			if (gate.Type == GateType.AlwaysOpen) return true;
			return (int)credTier >= (int)gate.MinTier;
		}

		/// <summary>
		/// True if the district is currently accessible — meets requirement OR is within
		/// its grace period. AlwaysOpen districts always return true.
		/// </summary>
		public static bool IsAccessible(string districtId, CredTier credTier, int graceMatchesRemaining)
		{
			if (MeetsTierRequirement(districtId, credTier)) return true;
			if (GetGate(districtId).Type == GateType.SoftGate) return true; // soft gate never locks
			return graceMatchesRemaining > 0;
		}

		/// <summary>All HardLock district IDs — used to tick grace periods.</summary>
		public static IEnumerable<string> HardLockIds()
		{
			foreach (var kvp in Gates)
				if (kvp.Value.Type == GateType.HardLock)
					yield return kvp.Key;
		}
	}
}
