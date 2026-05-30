using System;
using System.Collections.Generic;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Generates procedural card crews for both the player and AI.
	///
	/// Player: 7 cards (1 Hero + 1 Pro + 5 Street), then SelectBestFive picks the hand.
	/// AI:     exactly 5 cards (fixed hero + fixed Pro + 3 generated Streets).
	/// </summary>
	public static class CrewGenerator
	{
		private const int StreetMin = 20, StreetMax = 28; // Scale-20
		private const int ProMin    = 32, ProMax    = 44;  // Scale-20

		// Decay is Vesna's curse — thematically wrong on a player hero who
		// is supposed to be building toward something. Removed from the pool.
		private static readonly (AbilityType ability, int weight)[] AbilityWeights =
		{
			(AbilityType.None,    75),
			(AbilityType.Compound,15),
			(AbilityType.Copy,    10),
		};

		private static readonly DomainType[] AllDomains =
		{
			DomainType.AegisProtocol, DomainType.Killzone,
			DomainType.LateralGrid,   DomainType.Sprawl,
			DomainType.None,
		};

		private static readonly Faction[] AllFactions =
		{
			Faction.Ascendant, Faction.Razorkin, Faction.Ghostwire,
			Faction.Commons, Faction.Effigy, Faction.Lacquer, Faction.HollowChoir
		};

		// ── Player crew ───────────────────────────────────────────────────────────

		/// <summary>Generate a 7-card player crew: Hero + Pro + 5 Street.</summary>
		public static List<CardData> Generate(Random? rng = null)
		{
			rng ??= new Random();
			var usedFirstNames = new HashSet<string>();
			var crew = new List<CardData>();

			crew.Add(GenerateHero(rng, usedFirstNames));
			crew.Add(GeneratePro(rng, usedFirstNames));
			for (int i = 0; i < 5; i++)
				crew.Add(GenerateStreet(rng, usedFirstNames));

			return crew;
		}

		/// <summary>
		/// Auto-select the best 5 from a 7-card crew.
		/// Hero is always included; remaining 4 chosen by highest stat total.
		/// </summary>
		public static List<CardData> SelectBestFive(List<CardData> crew)
		{
			if (crew == null || crew.Count == 0) return new List<CardData>();

			var hero = crew.Find(c => c.Tier == Tier.Hero);
			var rest = crew.FindAll(c => c.Tier != Tier.Hero);

			rest.Sort((a, b) =>
				(b.Top + b.Right + b.Bottom + b.Left)
				.CompareTo(a.Top + a.Right + a.Bottom + a.Left));

			var hand = new List<CardData>();
			if (hero != null) hand.Add(hero);
			for (int i = 0; i < 4 && i < rest.Count; i++)
				hand.Add(rest[i]);

			return hand;
		}

		// ── Hero generation ───────────────────────────────────────────────────────

		private static CardData GenerateHero(Random rng, HashSet<string> usedFirstNames)
		{
			var faction = Pick(AllFactions, rng);
			var domain  = Pick(AllDomains, rng);
			var ability = PickWeighted(AbilityWeights, rng);
			int[] edges = GenerateHeroEdges(faction, rng);

			return new CardData
			{
				Id          = $"gen_hero_{Guid.NewGuid():N}",
				Name        = NameGenerator.GenerateHeroName(rng, usedFirstNames),
				Top         = edges[0], Right  = edges[1],
				Bottom      = edges[2], Left   = edges[3],
				Level       = 10,
				Faction     = faction,
				Tier        = Tier.Hero,
				DomainType  = domain,
				AbilityType = ability,
			};
		}

		private static int[] GenerateHeroEdges(Faction faction, Random rng)
		{
			int soft = rng.Next(4, 9);    // Scale-20 soft range
			int midA = rng.Next(10, 17);  // Scale-20 mid range
			int midB = rng.Next(10, 17);
			var slots = new int[4];

			switch (faction)
			{
				case Faction.Ascendant:
					slots[0] = 20; slots[2] = soft;
					slots[1] = midA; slots[3] = midB;
					break;

				case Faction.Razorkin:
					if (rng.Next(2) == 0) { slots[0] = 20; slots[1] = soft; }
					else                  { slots[3] = 20; slots[2] = soft; }
					FillRemainingMid(slots, rng.Next(4, 9), rng.Next(4, 9));
					break;

				case Faction.Ghostwire:
					if (rng.Next(2) == 0) { slots[3] = 20; slots[0] = soft; }
					else                  { slots[1] = 20; slots[2] = soft; }
					FillRemainingMid(slots, midA, midB);
					break;

				case Faction.Commons:
					AssignRandom(slots, 20, soft, rng.Next(10, 16), rng.Next(10, 16), rng);
					break;

				case Faction.Effigy:
					if (rng.Next(2) == 0)
					{ slots[0] = 20; slots[2] = 20; slots[1] = soft; slots[3] = soft; }
					else
					{ slots[1] = 20; slots[3] = 20; slots[0] = soft; slots[2] = soft; }
					break;

				case Faction.Lacquer:
					AssignRandom(slots, 20, rng.Next(6, 9), rng.Next(10, 17), rng.Next(10, 17), rng);
					break;

				case Faction.HollowChoir:
					int tollDir = rng.Next(4);
					slots[tollDir] = 0;
					var remaining = new List<int>();
					for (int i = 0; i < 4; i++) if (i != tollDir) remaining.Add(i);
					int aIdx = remaining[rng.Next(remaining.Count)];
					slots[aIdx] = 20;
					remaining.Remove(aIdx);
					slots[remaining[0]] = rng.Next(14, 19); // Scale-20 Choir mids
					slots[remaining[1]] = rng.Next(14, 19);
					break;

				default:
					AssignRandom(slots, 20, soft, midA, midB, rng);
					break;
			}

			return slots;
		}

		// ── Pro generation ────────────────────────────────────────────────────────

		public static CardData GeneratePro(Random? rng, HashSet<string> usedFirstNames)
		{
			rng ??= new System.Random();
			var faction = Pick(AllFactions, rng);
			int total   = rng.Next(ProMin, ProMax + 1);
			int[] edges = DistributeStats(total, 4, 18, rng); // Scale-20 Pro
			ApplyFactionBias(edges, faction, rng);

			return new CardData
			{
				Id      = $"gen_pro_{Guid.NewGuid():N}",
				Name    = NameGenerator.GenerateOperatorName(rng, usedFirstNames),
				Top     = edges[0], Right  = edges[1],
				Bottom  = edges[2], Left   = edges[3],
				Level   = rng.Next(6, 8),
				Faction = faction,
				Tier    = Tier.Pro,
			};
		}

		// ── Street generation ─────────────────────────────────────────────────────


		// ── AI hand ───────────────────────────────────────────────────────────────

		/// <summary>
		/// Generate the AI's 5-card hand: fixed hero + fixed TopTier + 3 generated Streets.
		/// </summary>
		/// <summary>
		/// Generates an AI hand matched to the district's controlling faction.
		/// Structure: Hero (named) + 1–2 named Top-Tier + generated Streets to fill 5.
		///
		/// Each faction fields their own hero and signature operatives so every
		/// district feels like fighting a different crew. Neutral/Contested districts
		/// pick a random faction. Unrecognised controllers fall back to a random faction.
		/// </summary>
		public static List<CardData> GenerateFactionHand(
			CardDatabase db, string controller, Random? rng = null)
		{
			rng ??= new Random();
			var hand          = new List<CardData>();
			var usedNames     = new HashSet<string>();

			// Map controller string → faction-specific named cards
			switch (controller)
			{
				case "Ascendant":
					AddNamed(db, hand, "asc_hero_seraph_yune");
					AddNamed(db, hand, rng.Next(2) == 0 ? "asc_top_cassia_vane" : "asc_top_proxy");
					break;

				case "Razorkin":
					AddNamed(db, hand, "rzk_hero_sister_grin");
					AddNamed(db, hand, rng.Next(2) == 0 ? "rzk_top_gristle" : "rzk_top_twitch");
					break;

				case "Ghostwire":
					AddNamed(db, hand, "gwi_hero_riven");
					AddNamed(db, hand, rng.Next(2) == 0 ? "gwi_top_echo" : "gwi_top_wren");
					break;

				case "Commons":
					AddNamed(db, hand, "com_hero_mara_kane");
					AddNamed(db, hand, rng.Next(2) == 0 ? "com_top_auntie_sol" : "com_top_patch");
					break;

				case "Effigy":
					AddNamed(db, hand, "eff_hero_lethe");
					// Effigy fields two Top-Tier — mirroring their identity theme
					AddNamed(db, hand, rng.Next(2) == 0 ? "eff_top_verity" : "eff_top_the_smile");
					AddNamed(db, hand, "eff_top_cousin");
					break;

				case "Lacquer":
					AddNamed(db, hand, "lac_hero_madame_sumi");
					AddNamed(db, hand, rng.Next(2) == 0 ? "lac_top_aoi" : "lac_top_the_heir");
					break;

				case "HollowChoir":
					AddNamed(db, hand, "hch_hero_vesna");
					// Choir adds two named cards — the Wall sends more than one warden
					var choirTopTier = new[] { "hch_top_threnody", "hch_top_antiphon", "hch_top_lamb" };
					var choirIdx     = rng.Next(3);
					AddNamed(db, hand, choirTopTier[choirIdx]);
					AddNamed(db, hand, choirTopTier[(choirIdx + 1) % 3]);
					break;

				default:
					// Neutral / Contested / The Stub — fully procedural crew.
					// Named heroes and top-tier cards only appear in their home district.
					// The Vault (Contested) is an exception — see below.
					if (controller == "Contested")
					{
						// The Vault: pick a random apex faction for a true all-comers fight
						var factions = new[] {
							"Ascendant","Razorkin","Ghostwire","Commons","Lacquer","HollowChoir"
						};
						return GenerateFactionHand(db, factions[rng.Next(factions.Length)], rng);
					}
					// Every other neutral district: generated hero + streets, no named cards
					hand.Add(GenerateHero(rng, new HashSet<string>()));
					break;
			}

			// Track used names so generated Streets don't collide
			foreach (var c in hand) usedNames.Add(c.Name);

			// Fill remaining slots with generated Streets
			while (hand.Count < 5)
				hand.Add(GenerateStreet(rng, usedNames));

			return hand;
		}

		/// <summary>
		/// Add a named card from the database to the hand.
		/// Silently skips if the card isn't found (handles missing data gracefully).
		/// </summary>
		private static void AddNamed(CardDatabase db, List<CardData> hand, string id)
		{
			var card = db.GetCard(id);
			if (card != null) hand.Add(CloneCard(card));
			// silently skip if not found — Streets fill the gap
		}

		/// <summary>
		/// Legacy method — kept for compatibility. Calls GenerateFactionHand
		/// with a random faction (same behaviour as before).
		/// </summary>
		public static List<CardData> GenerateAIHand(CardDatabase db, Random? rng = null)
			=> GenerateFactionHand(db, "Neutral", rng);

		/// <summary>Shallow clone of a CardData — all fields copied, new reference.</summary>
		private static CardData CloneCard(CardData src) => new CardData
		{
			Id          = src.Id,
			Name        = src.Name,
			Top         = src.Top,
			Right       = src.Right,
			Bottom      = src.Bottom,
			Left        = src.Left,
			Level       = src.Level,
			Element     = src.Element,
			ArtPath     = src.ArtPath,
			Faction     = src.Faction,
			Tier        = src.Tier,
			DomainType  = src.DomainType,
			AbilityType = src.AbilityType,
		};

		private static CardData GenerateStreet(Random? rng, HashSet<string> usedFirstNames)
		{
			rng ??= new System.Random();
			var faction = Pick(AllFactions, rng);
			int total   = rng.Next(StreetMin, StreetMax + 1);
			int[] edges = DistributeStats(total, 4, 10, rng); // Scale-20 Street

			return new CardData
			{
				Id      = $"gen_street_{Guid.NewGuid():N}",
				Name    = NameGenerator.GenerateOperatorName(rng, usedFirstNames),
				Top     = edges[0], Right  = edges[1],
				Bottom  = edges[2], Left   = edges[3],
				Level   = rng.Next(1, 6),
				Faction = faction,
				Tier    = Tier.Street,
			};
		}

		// ── Helpers ───────────────────────────────────────────────────────────────

		private static int[] DistributeStats(int total, int minEdge, int maxEdge, Random rng)
		{
			var edges     = new int[4];
			int remaining = total;

			for (int i = 0; i < 3; i++)
			{
				int lo = Math.Max(minEdge, remaining - maxEdge * (3 - i));
				int hi = Math.Min(maxEdge, remaining - minEdge * (3 - i));
				if (lo > hi) lo = hi;
				edges[i]  = rng.Next(lo, hi + 1);
				remaining -= edges[i];
			}
			edges[3] = Math.Clamp(remaining, minEdge, maxEdge);

			Shuffle(edges, rng);
			return edges;
		}

		private static void ApplyFactionBias(int[] edges, Faction faction, Random rng)
		{
			switch (faction)
			{
				case Faction.Ascendant:
					SwapToPosition(edges, IndexOfMax(edges), 0);
					break;
				case Faction.Razorkin:
					SwapToPosition(edges, IndexOfMax(edges), rng.Next(2) == 0 ? 0 : 3);
					SwapToPosition(edges, IndexOfMin(edges), rng.Next(2) == 0 ? 1 : 2);
					break;
				case Faction.Ghostwire:
					SwapToPosition(edges, IndexOfMax(edges), rng.Next(2) == 0 ? 1 : 3);
					break;
				case Faction.HollowChoir:
					SwapToPosition(edges, IndexOfMin(edges), rng.Next(4));
					break;
			}
		}

		private static void AssignRandom(int[] slots, int a, int b, int c, int d, Random rng)
		{
			var vals = new[] { a, b, c, d };
			Shuffle(vals, rng);
			for (int i = 0; i < 4; i++) slots[i] = vals[i];
		}

		private static void FillRemainingMid(int[] slots, int mid1, int mid2)
		{
			int filled = 0;
			for (int i = 0; i < 4; i++)
				if (slots[i] == 0)
				{
					slots[i] = filled == 0 ? mid1 : mid2;
					filled++;
				}
		}

		private static void Shuffle<T>(T[] arr, Random rng)
		{
			for (int i = arr.Length - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(arr[i], arr[j]) = (arr[j], arr[i]);
			}
		}

		private static void SwapToPosition(int[] arr, int from, int to)
		{
			if (from == to) return;
			(arr[from], arr[to]) = (arr[to], arr[from]);
		}

		private static int IndexOfMax(int[] arr)
		{
			int idx = 0;
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] > arr[idx]) idx = i;
			return idx;
		}

		private static int IndexOfMin(int[] arr)
		{
			int idx = 0;
			for (int i = 1; i < arr.Length; i++)
				if (arr[i] < arr[idx]) idx = i;
			return idx;
		}

		private static T Pick<T>(T[] table, Random? rng)
		{
			rng ??= new System.Random();
			return table[rng.Next(table.Length)];
		}

		private static AbilityType PickWeighted(
			(AbilityType ability, int weight)[] table, Random rng)
		{
			int roll       = rng.Next(100);
			int cumulative = 0;
			foreach (var (ability, weight) in table)
			{
				cumulative += weight;
				if (roll < cumulative) return ability;
			}
			return AbilityType.None;
		}
	}
}