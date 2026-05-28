namespace TripsAndTriads.Core
{
	public class CardData
	{
		public string     Id          { get; set; } = string.Empty;
		public string     Name        { get; set; } = string.Empty;
		public int        Top         { get; set; }
		public int        Right       { get; set; }
		public int        Bottom      { get; set; }
		public int        Left        { get; set; }
		public int        Level       { get; set; }
		public string     Element     { get; set; } = string.Empty;
		public string     ArtPath     { get; set; } = string.Empty;
		public Faction    Faction     { get; set; } = Faction.None;
		public Tier       Tier        { get; set; } = Tier.Street;
		public DomainType DomainType  { get; set; } = DomainType.None;
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

	public enum Direction { Top, Right, Bottom, Left }

	public enum Faction
	{
		None, Ascendant, Razorkin, Ghostwire, Commons, Effigy, Lacquer, HollowChoir
	}

	public enum Tier { Street, Pro, TopTier, Hero }

	public enum DomainType
	{
		None,
		AegisProtocol,
		Killzone,
		LateralGrid,
		Sprawl
	}

	public enum AbilityType
	{
		None,
		Decay,
		Compound,
		Copy
	}
}
