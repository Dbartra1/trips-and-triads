namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Implemented by each Protocol capture-rule variant.
	/// Resolve is called after base capture, given the full board state
	/// and the position of the card just placed. Returns any additional
	/// positions captured by this protocol.
	/// </summary>
	public interface IProtocol
	{
		/// <summary>The display name used in logs and future UI.</summary>
		string Name { get; }

		/// <summary>
		/// Evaluate additional captures triggered by this protocol.
		/// May modify board state (flip OwnerId on captured cards).
		/// Returns newly captured positions.
		/// </summary>
		System.Collections.Generic.List<(int row, int col)> Resolve(
			TripsAndTriads.Core.BoardState board,
			TripsAndTriads.Core.CardInstance placed,
			int row, int col,
			System.Collections.Generic.HashSet<(int, int)> alreadyCaptured);
	}
}
