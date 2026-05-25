using System;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Generates cyberpunk-flavoured names for procedurally created operators.
	/// All names are built from syllable tables — no external data files needed.
	/// </summary>
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

		/// <summary>Generate a callsign for a non-hero operator.</summary>
		public static string GenerateOperatorName(Random rng) =>
			$"{Pick(FirstNames, rng)} {Pick(LastNames, rng)}";

		/// <summary>Generate a full name + title for a hero.</summary>
		public static string GenerateHeroName(Random rng) =>
			$"{Pick(FirstNames, rng)} {Pick(LastNames, rng)}, {Pick(HeroTitles, rng)}";

		private static string Pick(string[] table, Random rng) =>
			table[rng.Next(table.Length)];
	}
}
