namespace TripsAndTriads.Core
{
	public class CardInstance
	{
		public CardData Data { get; }
		public int OwnerId { get; set; }  // 1 = Player, 2 = Opponent

		public CardInstance(CardData data, int ownerId)
		{
			Data = data;
			OwnerId = ownerId;
		}
	}
}
