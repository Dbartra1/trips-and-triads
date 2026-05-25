using System;
using System.Collections.Generic;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Generates a procedural starting crew of 7 cards:
	/// 1 Hero + 1 Pro + 5 Street.
	///
	/// The hero has a random faction, stat shape biased by faction,
	/// a random domain, and a weighted-random ability.
	/// Non-hero cards have faction-biased stats within tier ranges.
	/// </summary>
	public static class CrewGenerator
	{
		// Stat total ranges by tier
		private const int StreetMin = 8,  StreetMax = 14;
		private const int ProMin    = 16, ProMax    = 22;

		// Ability weights for hero generation (must sum to 100)
		// None is heavily weighted — special abilities should feel rare
		private static readonly (AbilityType ability, int weight)[] AbilityWeights =
		{
			(AbilityType.None,    60),
			(AbilityType.Decay,   15),
			(AbilityType.Compound,15),
			(AbilityType.Copy,    10),
		};

		private static readonly DomainType[] AllDomains =
		{
			DomainType.AegisProtocol,
			DomainType.Killzone,
			DomainType.LateralGrid,
			DomainType.Sprawl,
			DomainType.None, // some heroes have no visible aura
		};

		private static readonly Faction[] AllFactions =
		{
			Faction.Ascendant, Faction.Razorkin, Faction.Ghostwire,
			Faction.Commons, Faction.Effigy, Faction.Lacquer, Faction.HollowChoir
		};

		/// <summary>
		/// Generate a full starting crew of 7 cards.
		/// Returns them ordered: [Hero, Pro, Street×5].
		/// </summary>
		public static List<CardData> Generate(Random rng = null)
		{
			rng ??= new Random();
			var crew = new List<CardData>();

			crew.Add(GenerateHero(rng));
			crew.Add(GeneratePro(rng));
			for (int i = 0; i < 5; i++)
				crew.Add(GenerateStreet(rng));

			return crew;
		}

		/// <summary>
		/// Auto-select the best 5 from a 7-card crew for a match hand.
		/// Hero is always included. Remaining 4 chosen by highest stat total.
		/// </summary>
		public static List<CardData> SelectBestFive(List<CardData> crew)
		{
			if (crew == null || crew.Count == 0) return new List<CardData>();

			// Find the hero
			var hero = crew.Find(c => c.Tier == Tier.Hero);
			var rest = crew.FindAll(c => c.Tier != Tier.Hero);

			// Sort rest by stat total descending
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
		private static CardData GenerateHero(Random rng)
		{
			var faction = Pick(AllFactions, rng);
			var domain  = Pick(AllDomains, rng);
			var ability = PickWeighted(AbilityWeights, rng);

			// Generate hero stats: one A, one soft (2–4), two middles (5–8)
			// Shape is biased by faction
			int[] edges = GenerateHeroEdges(faction, rng);

			return new CardData
			{
				Id          = $"gen_hero_{Guid.NewGuid():N}",
				Name        = NameGenerator.GenerateHeroName(rng),
				Top         = edges[0],
				Right       = edges[1],
				Bottom      = edges[2],
				Left        = edges[3],
				Level       = 10,
				Faction     = faction,
				Tier        = Tier.Hero,
				DomainType  = domain,
				AbilityType = ability,
			};
		}

		private static int[] GenerateHeroEdges(Faction faction, Random rng)
		{
			// Each faction has a preferred A direction and soft direction
			// Returns [Top, Right, Bottom, Left]
			int soft = rng.Next(2, 5);       // 2–4
			int midA = rng.Next(5, 9);        // 5–8
			int midB = rng.Next(5, 9);        // 5–8

			// Build a 4-slot array and assign by faction shape bias
			var slots = new int[4]; // [Top, Right, Bottom, Left]

			switch (faction)
			{
				case Faction.Ascendant:
					// Forward-loaded: A on Top, soft on Bottom
					slots[0] = 10; slots[2] = soft;
					slots[1] = midA; slots[3] = midB;
					break;

				case Faction.Razorkin:
					// Corner predator: A on Top+Left (pick one), soft on Right or Bottom
					if (rng.Next(2) == 0) { slots[0] = 10; slots[1] = soft; }
					else                  { slots[3] = 10; slots[2] = soft; }
					// Fill remaining two with mid values (Razorkin has ugly lows too)
					FillRemainingMid(slots, rng.Next(2, 5), rng.Next(2, 5));
					break;

				case Faction.Ghostwire:
					// Lateral: A on Left or Right, soft on Top or Bottom
					if (rng.Next(2) == 0) { slots[3] = 10; slots[0] = soft; }
					else                  { slots[1] = 10; slots[2] = soft; }
					FillRemainingMid(slots, midA, midB);
					break;

				case Faction.Commons:
					// Even: A anywhere, soft anywhere else, others close together
					AssignRandom(slots, 10, soft, rng.Next(5, 8), rng.Next(5, 8), rng);
					break;

				case Faction.Effigy:
					// Near-symmetric: T≈B, L≈R — A on one axis pair, soft on other
					if (rng.Next(2) == 0)
					{ slots[0] = 10; slots[2] = 10; slots[1] = soft; slots[3] = soft; }
					else
					{ slots[1] = 10; slots[3] = 10; slots[0] = soft; slots[2] = soft; }
					// Two As violates the rule — we accept this for Effigy as their
					// lore-exception (like Sumi/Vesna break the rule differently)
					break;

				case Faction.Lacquer:
					// Compounding: starts modest, A anywhere, all edges closer together
					AssignRandom(slots, 10, rng.Next(3, 6), rng.Next(4, 7), rng.Next(4, 7), rng);
					break;

				case Faction.HollowChoir:
					// The Toll: one 0 instead of a soft edge, other three high
					int tollDir = rng.Next(4);
					slots[tollDir] = 0;
					// A goes on one of the remaining three
					var remaining = new List<int>();
					for (int i = 0; i < 4; i++) if (i != tollDir) remaining.Add(i);
					int aIdx = remaining[rng.Next(remaining.Count)];
					slots[aIdx] = 10;
					remaining.Remove(aIdx);
					slots[remaining[0]] = rng.Next(7, 10);
					slots[remaining[1]] = rng.Next(7, 10);
					break;

				default:
					AssignRandom(slots, 10, soft, midA, midB, rng);
					break;
			}

			return slots;
		}

		// ── Pro generation ────────────────────────────────────────────────────────
		private static CardData GeneratePro(Random rng)
		{
			var faction = Pick(AllFactions, rng);
			int total   = rng.Next(ProMin, ProMax + 1);
			int[] edges = DistributeStats(total, 1, 9, rng);
			ApplyFactionBias(edges, faction, rng);

			return new CardData
			{
				Id      = $"gen_pro_{Guid.NewGuid():N}",
				Name    = NameGenerator.GenerateOperatorName(rng),
				Top     = edges[0], Right  = edges[1],
				Bottom  = edges[2], Left   = edges[3],
				Level   = rng.Next(6, 8),
				Faction = faction,
				Tier    = Tier.Pro,
			};
		}

		// ── Street generation ─────────────────────────────────────────────────────
		private static CardData GenerateStreet(Random rng)
		{
			var faction = Pick(AllFactions, rng);
			int total   = rng.Next(StreetMin, StreetMax + 1);
			int[] edges = DistributeStats(total, 1, 5, rng);

			return new CardData
			{
				Id      = $"gen_street_{Guid.NewGuid():N}",
				Name    = NameGenerator.GenerateOperatorName(rng),
				Top     = edges[0], Right  = edges[1],
				Bottom  = edges[2], Left   = edges[3],
				Level   = rng.Next(1, 6),
				Faction = faction,
				Tier    = Tier.Street,
			};
		}

		// ── Stat distribution helpers ─────────────────────────────────────────────

		/// <summary>
		/// Distribute a total across 4 edges within [minEdge, maxEdge],
		/// then shuffle the result.
		/// </summary>
		private static int[] DistributeStats(int total, int minEdge, int maxEdge, Random rng)
		{
			var edges = new int[4];
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

		/// <summary>Nudge edges toward faction's signature shape after distribution.</summary>
		private static void ApplyFactionBias(int[] edges, Faction faction, Random rng)
		{
			// Light touch — swap edges to make shape more recognizable
			switch (faction)
			{
				case Faction.Ascendant:
					// Highest → Top
					SwapToPosition(edges, IndexOfMax(edges), 0);
					break;
				case Faction.Razorkin:
					// Highest → Top or Left, lowest → Right or Bottom
					SwapToPosition(edges, IndexOfMax(edges), rng.Next(2) == 0 ? 0 : 3);
					SwapToPosition(edges, IndexOfMin(edges), rng.Next(2) == 0 ? 1 : 2);
					break;
				case Faction.Ghostwire:
					// Highest → Left or Right
					SwapToPosition(edges, IndexOfMax(edges), rng.Next(2) == 0 ? 1 : 3);
					break;
				case Faction.HollowChoir:
					// Lowest → any one position (the Toll direction)
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

		private static T Pick<T>(T[] table, Random rng) =>
			table[rng.Next(table.Length)];

		private static AbilityType PickWeighted(
			(AbilityType ability, int weight)[] table, Random rng)
		{
			int roll = rng.Next(100);
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
