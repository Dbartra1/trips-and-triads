using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The First Voice — A/A/A/A on placement, loses 1 from every edge
	/// at the end of each of her owner's turns. Five turns later she is a husk.
	/// </summary>
	public class VesnaAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col)
		{
			// Nothing on placement — she enters at full strength (A/A/A/A from CardData).
		}

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			card.AdjustAllEdges(-2);

			// Print base values (Override ?? Data), not GetValue() which includes
			// transient domain bonuses and would give a misleading number.
			int t = card.TopOverride    ?? card.Data.Top;
			int r = card.RightOverride  ?? card.Data.Right;
			int b = card.BottomOverride ?? card.Data.Bottom;
			int l = card.LeftOverride   ?? card.Data.Left;
			Log.Print($"Vesna decays — base now {t}/{r}/{b}/{l} (effective with domain bonuses may differ).");
		}
	}
}