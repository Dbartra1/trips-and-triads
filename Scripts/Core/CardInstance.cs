using TripsAndTriads.Rules;

namespace TripsAndTriads.Core
{
	public class CardInstance
	{
		public CardData     Data    { get; }
		public int          OwnerId { get; set; }  // 1 = Player, 2 = Opponent
		public ICardAbility Ability { get; set; }  // null for non-hero cards

		// Per-instance edge overrides — null means "use CardData value".
		// Vesna writes these to decay; Sumi/Ledger write these to compound.
		// Always access edges through GetValue(), never Data.Top etc. directly.
		public int? TopOverride    { get; set; }
		public int? RightOverride  { get; set; }
		public int? BottomOverride { get; set; }
		public int? LeftOverride   { get; set; }

		// Transient domain bonuses — reset and recomputed by DomainResolver each turn.
		// Represent the aura a hero projects onto adjacent friendly cards.
		public int DomainBonusTop    { get; set; }
		public int DomainBonusRight  { get; set; }
		public int DomainBonusBottom { get; set; }
		public int DomainBonusLeft   { get; set; }

		public CardInstance(CardData data, int ownerId)
		{
			Data    = data;
			OwnerId = ownerId;
		}

		// Single read path for any edge.
		// Priority: (Override ?? CardData) + DomainBonus
		public int GetValue(Direction direction) => direction switch
		{
			Direction.Top    => (TopOverride    ?? Data.Top)    + DomainBonusTop,
			Direction.Right  => (RightOverride  ?? Data.Right)  + DomainBonusRight,
			Direction.Bottom => (BottomOverride ?? Data.Bottom) + DomainBonusBottom,
			Direction.Left   => (LeftOverride   ?? Data.Left)   + DomainBonusLeft,
			_                => 0
		};

		// Flat delta applied to all four override slots (Sumi +1, Vesna -1, Ledger +1).
		// Floors at 0 — edges cannot go negative.
		public void AdjustAllEdges(int delta)
		{
			// Use base value (override ?? data) without domain bonus for the calculation,
			// since domain bonuses are transient and shouldn't compound into overrides.
			int baseTop    = TopOverride    ?? Data.Top;
			int baseRight  = RightOverride  ?? Data.Right;
			int baseBottom = BottomOverride ?? Data.Bottom;
			int baseLeft   = LeftOverride   ?? Data.Left;

			TopOverride    = System.Math.Max(0, baseTop    + delta);
			RightOverride  = System.Math.Max(0, baseRight  + delta);
			BottomOverride = System.Math.Max(0, baseBottom + delta);
			LeftOverride   = System.Math.Max(0, baseLeft   + delta);
		}

		// Zeros all domain bonuses. Called by DomainResolver before recomputing.
		public void ResetDomainBonuses()
		{
			DomainBonusTop = DomainBonusRight = DomainBonusBottom = DomainBonusLeft = 0;
		}

		// True if any edge override is active.
		public bool IsModified =>
			TopOverride != null || RightOverride != null ||
			BottomOverride != null || LeftOverride != null;
	}
}
