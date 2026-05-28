using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class VesnaAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col) { }

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			card.AdjustAllEdges(-2);

			int t = card.TopOverride    ?? card.Data.Top;
			int r = card.RightOverride  ?? card.Data.Right;
			int b = card.BottomOverride ?? card.Data.Bottom;
			int l = card.LeftOverride   ?? card.Data.Left;
			TestLogger.Log($"Vesna decays — base now {t}/{r}/{b}/{l}.");
		}
	}
}