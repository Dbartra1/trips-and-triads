using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripsAndTriads.Core
{
	public class CardDatabase
	{
		private static CardDatabase _instance;
		public static CardDatabase Instance => _instance ??= new CardDatabase();

		private Dictionary<string, CardData> _cards = new();
		private bool _loaded = false;

		public void Load()
		{
			if (_loaded) return; // guard against double-load

			var file = FileAccess.Open("res://Data/Cards/cards.json", FileAccess.ModeFlags.Read);
			if (file == null)
			{
				Log.PrintErr("CardDatabase: could not open cards.json");
				return;
			}

			var json = file.GetAsText();
			file.Close();

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				Converters = { new JsonStringEnumConverter() }
			};

			var cards = JsonSerializer.Deserialize<List<CardData>>(json, options);
			if (cards == null)
			{
				Log.PrintErr("CardDatabase: deserialization returned null.");
				return;
			}

			foreach (var card in cards)
				_cards[card.Id] = card;

			_loaded = true;
			Log.Print($"CardDatabase: loaded {_cards.Count} cards.");
		}

		public CardData GetCard(string id) =>
			_cards.TryGetValue(id, out var card) ? card : null;

		public List<CardData> GetAllCards() =>
			new List<CardData>(_cards.Values);
	}
}