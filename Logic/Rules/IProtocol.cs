namespace TripsAndTriads.Rules
{
	public interface IProtocol
	{
		string Name { get; }

		System.Collections.Generic.List<(int row, int col)> Resolve(
			TripsAndTriads.Core.BoardState board,
			TripsAndTriads.Core.CardInstance placed,
			int row, int col,
			System.Collections.Generic.HashSet<(int, int)> alreadyCaptured);
	}
}
