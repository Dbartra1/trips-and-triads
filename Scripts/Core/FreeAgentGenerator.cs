using System;
using System.Collections.Generic;

namespace TripsAndTriads.Core
{
    public static class FreeAgentGenerator
    {
        public static List<FreeAgent> Generate(int count, CredTier playerTier, Random rng)
        {
            var agents = new List<FreeAgent>();
            for (int i = 0; i < count; i++)
                agents.Add(new FreeAgent { Data = GenerateSingleAgent(playerTier, rng) });
            return agents;
        }

        private static CardData GenerateSingleAgent(CredTier playerTier, Random rng)
        {
            Tier tier = Tier.Street;
            int roll = rng.Next(100);
            if (playerTier >= CredTier.Notorious && roll < 30) tier = Tier.TopTier;
            else if (playerTier >= CredTier.Named && roll < 50) tier = Tier.Pro;
            else if (playerTier >= CredTier.Known && roll < 70) tier = Tier.Pro;

            var factions = new[] { Faction.Ascendant, Faction.Razorkin, Faction.Ghostwire, Faction.Commons, Faction.Effigy };
            var faction = factions[rng.Next(factions.Length)];

            int t, r, b, l;
            if (tier == Tier.Street)
            {
                var edges = DistributeEdges(rng.Next(20, 29), 4, 10, rng);
                (t, r, b, l) = (edges[0], edges[1], edges[2], edges[3]);
            }
            else if (tier == Tier.Pro)
            {
                var edges = DistributeEdges(rng.Next(32, 45), 4, 18, rng);
                (t, r, b, l) = (edges[0], edges[1], edges[2], edges[3]);
            }
            else // TopTier
            {
                var edges = DistributeEdges(rng.Next(48, 61), 6, 20, rng);
                (t, r, b, l) = (edges[0], edges[1], edges[2], edges[3]);
            }

            return new CardData
            {
                Name = NameGenerator.GenerateOperatorName(rng),
                Top = t, Right = r, Bottom = b, Left = l,
                Tier = tier, Faction = faction,
                Level = tier == Tier.Street ? 3 : (tier == Tier.Pro ? 12 : 16),
                DomainType = DomainType.None, AbilityType = AbilityType.None
            };
        }

        private static int[] DistributeEdges(int total, int min, int max, Random rng)
        {
            int[] edges = new int[4];
            int remaining = total;
            for (int i = 0; i < 3; i++)
            {
                int maxVal = System.Math.Max(min, System.Math.Min(max, remaining - (3 - i) * min));
                edges[i] = rng.Next(min, maxVal + 1);
                remaining -= edges[i];
            }
            edges[3] = remaining;
            
            // Shuffle
            for (int i = 0; i < 4; i++)
            {
                int j = rng.Next(i, 4);
                (edges[i], edges[j]) = (edges[j], edges[i]);
            }
            return edges;
        }
    }
}