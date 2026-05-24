using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Core
{
	public class GameManager
	{
		public BoardState Board           { get; } = new BoardState();
		public int        CurrentPlayerId { get; private set; } = 1;
		public bool       GameOver        { get; private set; } = false;

		private Dictionary<int, List<CardInstance>> _hands = new()
		{
			{ 1, new List<CardInstance>() },
			{ 2, new List<CardInstance>() }
		};

		private CaptureResolver _resolver = new CaptureResolver();

		public void DealHands(List<CardData> player1Cards, List<CardData> player2Cards)
		{
			foreach (var card in player1Cards)
			{
				var instance = new CardInstance(card, ownerId: 1);
				instance.Ability = CreateAbility(card);
				_hands[1].Add(instance);
			}

			foreach (var card in player2Cards)
			{
				var instance = new CardInstance(card, ownerId: 2);
				instance.Ability = CreateAbility(card);
				_hands[2].Add(instance);
			}

			GD.Print($"Hands dealt. P1: {_hands[1].Count} cards, P2: {_hands[2].Count} cards.");
		}

		// Maps card IDs to their ability class. Only heroes have abilities.
		private static ICardAbility CreateAbility(CardData data) => data.Id switch
		{
			"hch_hero_vesna"       => new VesnaAbility(),
			"lac_hero_madame_sumi" => new SumiAbility(),
			"eff_hero_lethe"       => new LetheAbility(),
			_                      => null
		};

		public List<CardInstance> GetHand(int playerId) => _hands[playerId];

		// Returns captured positions, or null if the move was illegal.
		public List<(int row, int col)> PlayCard(int handIndex, int row, int col)
		{
			if (GameOver)
			{
				GD.PrintErr("GameManager: game is already over.");
				return null;
			}

			var hand = _hands[CurrentPlayerId];

			if (handIndex < 0 || handIndex >= hand.Count)
			{
				GD.PrintErr($"GameManager: invalid hand index {handIndex}.");
				return null;
			}

			if (!Board.IsEmpty(row, col))
			{
				GD.PrintErr($"GameManager: cell ({row},{col}) is already occupied.");
				return null;
			}

			var card = hand[handIndex];
			hand.RemoveAt(handIndex);
			Board.PlaceCard(card, row, col);

			// Fire placement ability (Lethe copies here)
			card.Ability?.OnPlaced(Board, card, row, col);

			var captured = _resolver.Resolve(Board, row, col);

			GD.Print($"P{CurrentPlayerId} played {card.Data.Name} at ({row},{col}). " +
			         $"Captured: {captured.Count}.");

			// Fire turn-end abilities for all cards owned by the current player
			ApplyTurnEndAbilities();

			// After abilities fire, re-check captures for any card whose stats changed.
			// A decayed Vesna may now be beatable by cards already adjacent to her.
			var decayCaptured = ResolveDecayCaptures();
			captured.AddRange(decayCaptured);

			if (Board.IsFull())
				EndGame();
			else
				CurrentPlayerId = CurrentPlayerId == 1 ? 2 : 1;

			return captured;
		}

		// Calls OnTurnEnd on every board card owned by the player who just moved.
		private void ApplyTurnEndAbilities()
		{
			for (int r = 0; r < BoardState.Size; r++)
			{
				for (int c = 0; c < BoardState.Size; c++)
				{
					var card = Board.GetCard(r, c);
					if (card == null) continue;
					if (card.OwnerId != CurrentPlayerId) continue;
					card.Ability?.OnTurnEnd(Board, card, r, c);
				}
			}
		}

		// After turn-end abilities alter stats, check whether any modified card
		// is now beatable by an already-placed enemy neighbor.
		// This lets Vesna flip ownership as she decays below adjacent cards.
		private List<(int row, int col)> ResolveDecayCaptures()
		{
			var captured = new List<(int row, int col)>();

			for (int r = 0; r < BoardState.Size; r++)
			{
				for (int c = 0; c < BoardState.Size; c++)
				{
					var card = Board.GetCard(r, c);
					if (card == null || !card.IsModified) continue;

					// Check each direction — can an adjacent enemy now beat this card?
					foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
					{
						var (nRow, nCol) = Board.GetNeighbor(r, c, dir);
						if (!Board.IsInBounds(nRow, nCol)) continue;

						var neighbor = Board.GetCard(nRow, nCol);
						if (neighbor == null) continue;
						if (neighbor.OwnerId == card.OwnerId) continue;

						// neighbor attacks from the opposite direction into this card
						Direction attackDir = card.Data.Opposite(dir);
						int attackVal = neighbor.GetValue(attackDir);
						int defendVal = card.GetValue(dir);

						if (attackVal > defendVal)
						{
							GD.Print($"{card.Data.Name} flipped by decay — " +
							         $"{neighbor.Data.Name}'s {attackDir}({attackVal}) " +
							         $"beats {card.Data.Name}'s {dir}({defendVal}).");
							card.OwnerId = neighbor.OwnerId;
							captured.Add((r, c));
							break; // card is flipped, stop checking directions
						}
					}
				}
			}

			return captured;
		}

		private void EndGame()
		{
			GameOver = true;
			int p1Score = Board.GetScore(1);
			int p2Score = Board.GetScore(2);

			GD.Print($"Game Over! P1: {p1Score} | P2: {p2Score}");

			if (p1Score > p2Score)
				GD.Print("Player 1 wins!");
			else if (p2Score > p1Score)
				GD.Print("Player 2 wins!");
			else
				GD.Print("It's a draw!");
		}

		public void PrintScores()
		{
			GD.Print($"Scores — P1: {Board.GetScore(1)} | P2: {Board.GetScore(2)}");
		}
	}
}