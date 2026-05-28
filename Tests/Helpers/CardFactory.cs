using TripsAndTriads.Core;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Tests.Helpers
{
    /// <summary>
    /// Fluent builder for CardData + CardInstance pairs used in tests.
    /// Keeps test setup readable and intention-clear.
    ///
    /// Usage:
    ///   var card = CardFactory.Create("Seraph Yune").Stats(10,8,8,3)
    ///               .Faction(Faction.Ascendant).Tier(Tier.Hero)
    ///               .Domain(DomainType.AegisProtocol)
    ///               .Id("asc_hero_seraph_yune")
    ///               .Build(ownerId: 1);
    /// </summary>
    public class CardFactory
    {
        private CardData _data = new CardData();

        private CardFactory() { }

        public static CardFactory Create(string name = "Test Card")
        {
            var f = new CardFactory();
            f._data.Name = name;
            f._data.Id   = name.ToLower().Replace(" ", "_");
            return f;
        }

        public CardFactory Id(string id)                     { _data.Id          = id;          return this; }
        public CardFactory Stats(int t, int r, int b, int l) { _data.Top = t; _data.Right = r;
                                                               _data.Bottom = b; _data.Left = l; return this; }
        public CardFactory Top(int v)                        { _data.Top         = v;            return this; }
        public CardFactory Right(int v)                      { _data.Right       = v;            return this; }
        public CardFactory Bottom(int v)                     { _data.Bottom      = v;            return this; }
        public CardFactory Left(int v)                       { _data.Left        = v;            return this; }
        public CardFactory Faction(Faction f)                { _data.Faction     = f;            return this; }
        public CardFactory Tier(Tier t)                      { _data.Tier        = t;            return this; }
        public CardFactory Domain(DomainType d)              { _data.DomainType  = d;            return this; }
        public CardFactory Ability(AbilityType a)            { _data.AbilityType = a;            return this; }

        /// <summary>Returns the raw CardData template (useful for DealHands).</summary>
        public CardData Data() => _data.ShallowClone();

        /// <summary>Returns a CardInstance ready to place on a board.</summary>
        public CardInstance Build(int ownerId = 1)
        {
            var instance = new CardInstance(_data.ShallowClone(), ownerId);

            // Wire ability if set
            instance.Ability = _data.AbilityType switch
            {
                AbilityType.Decay    => new VesnaAbility(),
                AbilityType.Compound => new SumiAbility(),
                AbilityType.Copy     => new LetheAbility(),
                _                    => null
            };

            return instance;
        }

        // ── Named card shortcuts ────────────────────────────────────────────────
        // These mirror the lore.md stat lines exactly. Use these in tests so the
        // numbers stay readable and lore-authoritative.

        public static CardInstance SeraphYune(int owner = 1) =>
            Create("Seraph Yune").Id("asc_hero_seraph_yune")
                .Stats(10, 8, 3, 8)
                .Faction(Core.Faction.Ascendant).Tier(Core.Tier.Hero)
                .Domain(DomainType.AegisProtocol)
                .Build(owner);

        public static CardInstance SisterGrin(int owner = 1) =>
            Create("Sister Grin").Id("rzk_hero_sister_grin")
                .Stats(10, 2, 3, 10)
                .Faction(Core.Faction.Razorkin).Tier(Core.Tier.Hero)
                .Domain(DomainType.Killzone)
                .Build(owner);

        public static CardInstance Riven(int owner = 1) =>
            Create("Riven").Id("gwi_hero_riven")
                .Stats(3, 9, 2, 9)
                .Faction(Core.Faction.Ghostwire).Tier(Core.Tier.Hero)
                .Domain(DomainType.LateralGrid)
                .Build(owner);

        public static CardInstance MaraKane(int owner = 1) =>
            Create("Mara Kane").Id("com_hero_mara_kane")
                .Stats(6, 6, 6, 6)
                .Faction(Core.Faction.Commons).Tier(Core.Tier.Hero)
                .Domain(DomainType.Sprawl)
                .Build(owner);

        public static CardInstance MadameSumi(int owner = 1) =>
            Create("Madame Sumi").Id("lac_hero_madame_sumi")
                .Stats(4, 4, 4, 4)
                .Faction(Core.Faction.Lacquer).Tier(Core.Tier.Hero)
                .Ability(AbilityType.Compound)
                .Build(owner);

        public static CardInstance Lethe(int owner = 1) =>
            Create("Lethe").Id("eff_hero_lethe")
                .Stats(0, 0, 0, 0)
                .Faction(Core.Faction.Effigy).Tier(Core.Tier.Hero)
                .Ability(AbilityType.Copy)
                .Build(owner);

        public static CardInstance Vesna(int owner = 1) =>
            Create("Vesna").Id("hch_hero_vesna")
                .Stats(10, 10, 10, 10)
                .Faction(Core.Faction.HollowChoir).Tier(Core.Tier.Hero)
                .Ability(AbilityType.Decay)
                .Build(owner);

        public static CardInstance CassiaVane(int owner = 1) =>
            Create("Dr. Cassia Vane").Id("asc_top_cassia_vane")
                .Stats(7, 8, 5, 6)
                .Faction(Core.Faction.Ascendant).Tier(Core.Tier.TopTier)
                .Build(owner);

        public static CardInstance TheHeir(int owner = 1) =>
            Create("The Heir").Id("lac_top_the_heir")
                .Stats(7, 5, 7, 7)
                .Faction(Core.Faction.Lacquer).Tier(Core.Tier.TopTier)
                .Build(owner);

        public static CardInstance Verity(int owner = 1) =>
            Create("Verity").Id("eff_top_verity")
                .Stats(7, 9, 7, 9)
                .Faction(Core.Faction.Effigy).Tier(Core.Tier.TopTier)
                .Build(owner);

        /// <summary>
        /// Generic Street card with controllable stats.
        /// Useful for filling boards without influencing the system under test.
        /// </summary>
        public static CardInstance Street(string name, int t, int r, int b, int l, int owner = 1) =>
            Create(name).Stats(t, r, b, l).Tier(Core.Tier.Street).Build(owner);
    }
}