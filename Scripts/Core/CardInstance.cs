namespace TripsAndTriads.Core
{
	public class CardInstance
	{
		public CardData Data    { get; }
		public int      OwnerId { get; set; }  // 1 = Player, 2 = Opponent

		// Per-instance edge overrides — null means "use CardData value".
		// Vesna writes these to decay; Sumi writes these to compound.
		// CaptureResolver and everything else calls GetValue(), never Data.Top etc. directly.
		public int? TopOverride    { get; set; }
		public int? RightOverride  { get; set; }
		public int? BottomOverride { get; set; }
		public int? LeftOverride   { get; set; }

		public CardInstance(CardData data, int ownerId)
		{
			Data    = data;
			OwnerId = ownerId;
		}

		// Single read path for any edge — override wins, falls back to CardData.
		public int GetValue(Direction direction) => direction switch
		{
			Direction.Top    => TopOverride    ?? Data.Top,
			Direction.Right  => RightOverride  ?? Data.Right,
			Direction.Bottom => BottomOverride ?? Data.Bottom,
			Direction.Left   => LeftOverride   ?? Data.Left,
			_                => 0
		};

		// Convenience: apply a flat delta to all four overrides (used by Sumi +1, Vesna -1).
		// Floors at 0 — an edge can decay to nothing but never go negative.
		public void AdjustAllEdges(int delta)
		{
			TopOverride    = System.Math.Max(0, GetValue(Direction.Top)    + delta);
			RightOverride  = System.Math.Max(0, GetValue(Direction.Right)  + delta);
			BottomOverride = System.Math.Max(0, GetValue(Direction.Bottom) + delta);
			LeftOverride   = System.Math.Max(0, GetValue(Direction.Left)   + delta);
		}

		// True if any override is active (used for debug / UI hint later).
		public bool IsModified =>
			TopOverride != null || RightOverride != null ||
			BottomOverride != null || LeftOverride != null;
	}
}