namespace TripsAndTriads.Core
{
	public class BoardState
	{
		public const int Size = 3;

		private CardInstance?[,] _grid = new CardInstance[Size, Size];

		public CardInstance? GetCard(int row, int col) => _grid[row, col];

		public bool IsEmpty(int row, int col) => _grid[row, col] == null;

		public bool IsInBounds(int row, int col) =>
			row >= 0 && row < Size && col >= 0 && col < Size;

		public bool PlaceCard(CardInstance card, int row, int col)
		{
			if (!IsInBounds(row, col) || !IsEmpty(row, col))
				return false;

			_grid[row, col] = card;
			return true;
		}

		public (int row, int col) GetNeighbor(int row, int col, Direction dir) => dir switch
		{
			Direction.Top    => (row - 1, col),
			Direction.Right  => (row, col + 1),
			Direction.Bottom => (row + 1, col),
			Direction.Left   => (row, col - 1),
			_                => (-1, -1)
		};

		public int GetScore(int playerId)
		{
			int score = 0;
			for (int r = 0; r < Size; r++)
				for (int c = 0; c < Size; c++)
					if (_grid[r, c]?.OwnerId == playerId)
						score++;
			return score;
		}

		public bool IsFull()
		{
			for (int r = 0; r < Size; r++)
				for (int c = 0; c < Size; c++)
					if (_grid[r, c] == null)
						return false;
			return true;
		}

		/// <summary>Prints board state to TestLogger (replaces GD.Print in Godot version).</summary>
		public void PrintDebug()
		{
			for (int r = 0; r < Size; r++)
			{
				string row = "";
				for (int c = 0; c < Size; c++)
				{
					var card = _grid[r, c];
					row += card == null ? "[ . ] " : $"[P{card.OwnerId}] ";
				}
				TestLogger.Log(row);
			}
		}
	}
}
