using Godot;
using TripsAndTriads.Core;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// The Understudy — printed 0/0/0/0. On placement, permanently copies the
	/// four edge values of the highest-total card currently on the board.
	/// Placed into an empty board she stays a corpse. Copies numbers only —
	/// not abilities, faction, or bonds.
	/// </summary>
	public class LetheAbility : ICardAbility
	{
		public void OnPlaced(BoardState board, CardInstance card, int row, int col)
		{
			CardInstance best     = null;
			int          bestTotal = -1;

			for (int r = 0; r < BoardState.Size; r++)
			{
				for (int c = 0; c < BoardState.Size; c++)
				{
					if (r == row && c == col) continue; // skip herself

					var other = board.GetCard(r, c);
					if (other == null) continue;

					int total = other.GetValue(Direction.Top)
					          + other.GetValue(Direction.Right)
					          + other.GetValue(Direction.Bottom)
					          + other.GetValue(Direction.Left);

					if (total > bestTotal)
					{
						bestTotal = total;
						best      = other;
					}
				}
			}

			if (best == null)
			{
				GD.Print("Lethe placed on empty board — stays 0/0/0/0.");
				return;
			}

			card.TopOverride    = best.GetValue(Direction.Top);
			card.RightOverride  = best.GetValue(Direction.Right);
			card.BottomOverride = best.GetValue(Direction.Bottom);
			card.LeftOverride   = best.GetValue(Direction.Left);

			GD.Print($"Lethe copies {best.Data.Name} — " +
			         $"becomes {card.GetValue(Direction.Top)}/" +
			         $"{card.GetValue(Direction.Right)}/" +
			         $"{card.GetValue(Direction.Bottom)}/" +
			         $"{card.GetValue(Direction.Left)}");
		}

		public void OnTurnEnd(BoardState board, CardInstance card, int row, int col)
		{
			// Lethe has no ongoing effect — her ability is purely on placement.
		}
	}
}
