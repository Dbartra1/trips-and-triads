namespace TripsAndTriads.Core
{
	public class CardData
	{
		public string     Id         { get; set; }
		public string     Name       { get; set; }
		public int        Top        { get; set; }
		public int        Right      { get; set; }
		public int        Bottom     { get; set; }
		public int        Left       { get; set; }
		public int        Level      { get; set; }
		public string     Element    { get; set; }
		public string     ArtPath    { get; set; }
		public Faction    Faction    { get; set; } = Faction.None;
		public Tier       Tier       { get; set; } = Tier.Street;
		public DomainType DomainType { get; set; } = DomainType.None;
		public AbilityType AbilityType { get; set; } = AbilityType.None;

		public int GetValue(Direction direction) => direction switch
		{
			Direction.Top    => Top,
			Direction.Right  => Right,
			Direction.Bottom => Bottom,
			Direction.Left   => Left,
			_                => 0
		};

		public Direction Opposite(Direction direction) => direction switch
		{
			Direction.Top    => Direction.Bottom,
			Direction.Right  => Direction.Left,
			Direction.Bottom => Direction.Top,
			Direction.Left   => Direction.Right,
			_                => Direction.Top
		};

		/// <summary>Returns a new CardData with all fields copied. Used by SaveManager
		/// to clone database templates before applying saved mutations.</summary>
		public CardData ShallowClone() => new CardData
		{
			Id          = Id,
			Name        = Name,
			Top         = Top,
			Right       = Right,
			Bottom      = Bottom,
			Left        = Left,
			Level       = Level,
			Element     = Element,
			ArtPath     = ArtPath,
			Faction     = Faction,
			Tier        = Tier,
			DomainType  = DomainType,
			AbilityType = AbilityType,
		};
	}

	public enum Direction  { Top, Right, Bottom, Left }

	public enum Faction
	{
		None, Ascendant, Razorkin, Ghostwire, Commons, Effigy, Lacquer, HollowChoir
	}

	public enum Tier { Street, Pro, TopTier, Hero }

	/// <summary>
	/// Which domain aura this hero projects onto adjacent friendly cards.
	/// Handled by DomainResolver. Named and generated heroes both use this.
	/// </summary>
	public enum DomainType
	{
		None,
		AegisProtocol,  // +1 all sides to adjacent friendlies (Yune)
		Killzone,        // +2 to two lowest sides of adjacent friendlies (Grin)
		LateralGrid,     // +2 Left/Right to adjacent friendlies (Riven)
		Sprawl           // +1 Commons adjacents; Mara grows with crowd (Mara)
		// TheLedger and TheBreach are handled in SumiAbility / CaptureResolver respectively
	}

	/// <summary>
	/// Which special per-turn ability this card has.
	/// Handled by GameManager.CreateAbility. Named and generated heroes both use this.
	/// </summary>
	public enum AbilityType
	{
		None,
		Decay,    // −1 all edges per owner turn-end (Vesna)
		Compound, // +1 all edges per owner turn-end, spreads Ledger to adjacents (Sumi)
		Copy      // copies highest-value card on placement (Lethe)
	}
}
