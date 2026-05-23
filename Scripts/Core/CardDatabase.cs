using Godot;
using System.Collections.Generic;
using System.Text.Json;

namespace TripsAndTriads.Core
{
	public class CardDatabase
	{
		private static CardDatabase _instance;
		public static CardDatabase Instance => _instance ??= new CardDatabase();

		private Dictionary<string, CardData> _cards = new();

		public void Load()
		{
			var file = FileAccess.Open("res://Data/Cards/cards.json", FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PrintErr("CardDatabase: could not open cards.json");
				return;
			}

			var json = file.GetAsText();
			file.Close();

			var cards = JsonSerializer.Deserialize<List<CardData>>(json, new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			});

			foreach (var card in cards)
				_cards[card.Id] = card;

			GD.Print($"CardDatabase: loaded {_cards.Count} cards.");
		}

		public CardData GetCard(string id) => 
			_cards.TryGetValue(id, out var card) ? card : null;

		public List<CardData> GetAllCards() => 
			new List<CardData>(_cards.Values);
	}
}
