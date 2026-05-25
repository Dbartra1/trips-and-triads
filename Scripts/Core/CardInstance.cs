using TripsAndTriads.Rules;

namespace TripsAndTriads.Core
{
	public class CardInstance
	{
		public CardData     Data    { get; }
		public int          OwnerId { get; set; }
		public ICardAbility Ability { get; set; }

		// Per-instance edge overrides — null means "use CardData value".
		public int? TopOverride    { get; set; }
		public int? RightOverride  { get; set; }
		public int? BottomOverride { get; set; }
		public int? LeftOverride   { get; set; }

		// Transient domain bonuses — reset and recomputed by DomainResolver each turn.
		public int DomainBonusTop    { get; set; }
		public int DomainBonusRight  { get; set; }
		public int DomainBonusBottom { get; set; }
		public int DomainBonusLeft   { get; set; }

		// Transient bond bonuses — reset and recomputed by BondResolver each turn.
		public int BondBonusTop    { get; set; }
		public int BondBonusRight  { get; set; }
		public int BondBonusBottom { get; set; }
		public int BondBonusLeft   { get; set; }

		// Behavioral bond flags — reset each BondResolver pass (transient).
		public bool BlockChoir    { get; set; }
		public bool RivalryActive { get; set; }

		// Permanent state flags — never reset by BondResolver.
		// IsContaminated: Contamination has already been applied once — don't re-apply.
		public bool IsContaminated { get; set; }

		public CardInstance(CardData data, int ownerId)
		{
			Data    = data;
			OwnerId = ownerId;
		}

		// Single read path. Priority: (Override ?? CardData) + DomainBonus + BondBonus
		public int GetValue(Direction direction) => direction switch
		{
			Direction.Top    => (TopOverride    ?? Data.Top)    + DomainBonusTop    + BondBonusTop,
			Direction.Right  => (RightOverride  ?? Data.Right)  + DomainBonusRight  + BondBonusRight,
			Direction.Bottom => (BottomOverride ?? Data.Bottom) + DomainBonusBottom + BondBonusBottom,
			Direction.Left   => (LeftOverride   ?? Data.Left)   + DomainBonusLeft   + BondBonusLeft,
			_                => 0
		};

		// Flat delta on all four override slots. Floors at 0.
		public void AdjustAllEdges(int delta)
		{
			int baseTop    = TopOverride    ?? Data.Top;
			int baseRight  = RightOverride  ?? Data.Right;
			int baseBottom = BottomOverride ?? Data.Bottom;
			int baseLeft   = LeftOverride   ?? Data.Left;

			TopOverride    = System.Math.Max(0, baseTop    + delta);
			RightOverride  = System.Math.Max(0, baseRight  + delta);
			BottomOverride = System.Math.Max(0, baseBottom + delta);
			LeftOverride   = System.Math.Max(0, baseLeft   + delta);
		}

		// Clamps a specific edge override to a maximum value.
		public void ClampEdge(Direction dir, int max)
		{
			switch (dir)
			{
				case Direction.Top:    TopOverride    = System.Math.Min(GetBaseValue(dir), max); break;
				case Direction.Right:  RightOverride  = System.Math.Min(GetBaseValue(dir), max); break;
				case Direction.Bottom: BottomOverride = System.Math.Min(GetBaseValue(dir), max); break;
				case Direction.Left:   LeftOverride   = System.Math.Min(GetBaseValue(dir), max); break;
			}
		}

		// Base value without domain or bond bonuses.
		public int GetBaseValue(Direction dir) => dir switch
		{
			Direction.Top    => TopOverride    ?? Data.Top,
			Direction.Right  => RightOverride  ?? Data.Right,
			Direction.Bottom => BottomOverride ?? Data.Bottom,
			Direction.Left   => LeftOverride   ?? Data.Left,
			_                => 0
		};

		// Returns the Direction of the highest base edge.
		public Direction HighestEdgeDirection()
		{
			var best    = Direction.Top;
			int bestVal = GetBaseValue(Direction.Top);
			foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
			{
				int v = GetBaseValue(dir);
				if (v > bestVal) { bestVal = v; best = dir; }
			}
			return best;
		}

		public void ResetDomainBonuses()
		{
			DomainBonusTop = DomainBonusRight = DomainBonusBottom = DomainBonusLeft = 0;
		}

		// Resets transient bond state only — never touches IsContaminated.
		public void ResetBondBonuses()
		{
			BondBonusTop = BondBonusRight = BondBonusBottom = BondBonusLeft = 0;
			BlockChoir    = false;
			RivalryActive = false;
		}

		public bool IsModified =>
			TopOverride != null || RightOverride != null ||
			BottomOverride != null || LeftOverride != null;
	}
}