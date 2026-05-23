namespace TripsAndTriads.Core
{
	public class CardData
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public int Top { get; set; }
		public int Right { get; set; }
		public int Bottom { get; set; }
		public int Left { get; set; }
		public int Level { get; set; }        // 1-10, like FF8
		public string Element { get; set; }   // null if no element
		public string ArtPath { get; set; }   // path to card art texture

		public int GetValue(Direction direction) => direction switch
		{
			Direction.Top    => Top,
			Direction.Right  => Right,
			Direction.Bottom => Bottom,
			Direction.Left   => Left,
			_ => 0
		};

		public Direction Opposite(Direction direction) => direction switch
		{
			Direction.Top    => Direction.Bottom,
			Direction.Right  => Direction.Left,
			Direction.Bottom => Direction.Top,
			Direction.Left   => Direction.Right,
			_ => Direction.Top
		};
	}

	public enum Direction
	{
		Top,
		Right,
		Bottom,
		Left
	}
}
