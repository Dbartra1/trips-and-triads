using System;
using System.Collections.Generic;

namespace TripsAndTriads.Core
{
	public static class NameGenerator
	{
		private static readonly string[] FirstNames =
		{
			"Vex", "Ryn", "Thresh", "Kas", "Sol", "Dren", "Zyl", "Cor",
			"Ash", "Nyx", "Vel", "Grim", "Crow", "Pike", "Kael", "Rue",
			"Fen", "Jax", "Mira", "Torch", "Cass", "Lyss", "Thorn", "Shade",
			"Bryn", "Ore", "Flint", "Dusk", "Wren", "Sable", "Cipher", "Null",
			"Fray", "Haze", "Lace", "Mote", "Quill", "Sparx", "Twitch", "Vane"
		};

		private static readonly string[] LastNames =
		{
			"Kade", "Sola", "Mourne", "Wire", "Volt", "Cross", "Blade", "Null",
			"Shard", "Rune", "Tide", "Smoke", "Edge", "Crest", "Blaze", "Vale",
			"Drake", "Holt", "Voss", "Crane", "Steel", "Frost", "Marsh", "Stone",
			"Ward", "Knox", "Vane", "Briar", "Chalk", "Dirk", "Fenn", "Gale",
			"Ink", "Lorn", "Nave", "Orin", "Pell", "Quay", "Rook", "Slag"
		};

		private static readonly string[] HeroTitles =
		{
			"the Ashwalker",  "the Hollow",      "the Wired",      "the Ghost",
			"the Last",       "the Scorned",      "the Bright",     "the Null",
			"the Wanderer",   "the Burned",       "the Silent",     "the Marked",
			"the Severed",    "the Twice-Turned", "the Unnamed",    "the Erased",
			"the Borrowed",   "the Still",        "the Unfinished", "the Signal"
		};

		/// <summary>
		/// Generate a callsign for a non-hero operator.
		/// Pass usedFirstNames to prevent duplicate first names within a crew.
		/// </summary>
		public static string GenerateOperatorName(Random? rng, HashSet<string>? usedFirstNames = null)
		{
			rng ??= new System.Random();
			usedFirstNames ??= new HashSet<string>();
			string first = PickUnique(FirstNames, rng, usedFirstNames);
			return $"{first} {Pick(LastNames, rng)}";
		}

		/// <summary>
		/// Generate a full name + title for a hero.
		/// Pass usedFirstNames to prevent duplicate first names within a crew.
		/// </summary>
		public static string GenerateHeroName(Random? rng, HashSet<string>? usedFirstNames = null)
		{
			rng ??= new System.Random();
			usedFirstNames ??= new HashSet<string>();
			string first = PickUnique(FirstNames, rng, usedFirstNames);
			return $"{first} {Pick(LastNames, rng)}, {Pick(HeroTitles, rng)}";
		}

		/// <summary>
		/// Pick a first name not already in the crew.
		/// Falls back to any name if all are exhausted (unlikely with 40 names and 7 cards).
		/// </summary>
		private static string PickUnique(string[] table, Random? rng, HashSet<string>? used)
		{
			if (used == null) return Pick(table, rng);

			// Try up to 20 times to find an unused name
			for (int i = 0; i < 20; i++)
			{
				string candidate = table[rng.Next(table.Length)];
				if (used.Add(candidate)) // Add returns false if already present
					return candidate;
			}

			// Exhausted retries — just pick anything
			return Pick(table, rng);
		}

		private static string Pick(string[] table, Random? rng) =>
			table[rng.Next(table.Length)];
	}
}