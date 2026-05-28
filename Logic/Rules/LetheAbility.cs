using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	public class LetheAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col)
		{
			CardInstance? best  = null;
			int bestTotal       = -1;

			for (int r = 0; r < BoardState.Size; r++)
			{
				for (int c = 0; c < BoardState.Size; c++)
				{
					if (r == row && c == col) continue;

					var other = board.GetCard(r, c);
					if (other == null) continue;

					int total = other.GetBaseValue(Direction.Top)
					          + other.GetBaseValue(Direction.Right)
					          + other.GetBaseValue(Direction.Bottom)
					          + other.GetBaseValue(Direction.Left);

					if (total > bestTotal)
					{
						bestTotal = total;
						best      = other;
					}
				}
			}

			if (best == null)
			{
				TestLogger.Log("Lethe placed on empty board — stays 0/0/0/0.");
				return;
			}

			card.TopOverride    = best.GetBaseValue(Direction.Top);
			card.RightOverride  = best.GetBaseValue(Direction.Right);
			card.BottomOverride = best.GetBaseValue(Direction.Bottom);
			card.LeftOverride   = best.GetBaseValue(Direction.Left);

			TestLogger.Log($"Lethe copies {best.Data.Name} (base values) — " +
			               $"becomes {card.GetBaseValue(Direction.Top)}/" +
			               $"{card.GetBaseValue(Direction.Right)}/" +
			               $"{card.GetBaseValue(Direction.Bottom)}/" +
			               $"{card.GetBaseValue(Direction.Left)}");
		}

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col) { }
	}
}
