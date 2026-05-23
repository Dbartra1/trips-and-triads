using Godot;
using System.Collections.Generic;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Core
{
	public class GameManager
	{
		public BoardState Board { get; } = new BoardState();
		public int CurrentPlayerId { get; private set; } = 1;
		public bool GameOver { get; private set; } = false;

		private Dictionary<int, List<CardInstance>> _hands = new()
		{
			{ 1, new List<CardInstance>() },
			{ 2, new List<CardInstance>() }
		};

		private CaptureResolver _resolver = new CaptureResolver();

		public void DealHands(List<CardData> player1Cards, List<CardData> player2Cards)
		{
			foreach (var card in player1Cards)
				_hands[1].Add(new CardInstance(card, ownerId: 1));

			foreach (var card in player2Cards)
				_hands[2].Add(new CardInstance(card, ownerId: 2));

			GD.Print($"Hands dealt. P1: {_hands[1].Count} cards, P2: {_hands[2].Count} cards.");
		}

		public List<CardInstance> GetHand(int playerId) => _hands[playerId];

		// Returns captured positions, or null if the move was illegal
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

			var captured = _resolver.Resolve(Board, row, col);

			GD.Print($"P{CurrentPlayerId} played {card.Data.Name} at ({row},{col}). " +
					 $"Captured: {captured.Count}.");

			if (Board.IsFull())
				EndGame();
			else
				CurrentPlayerId = CurrentPlayerId == 1 ? 2 : 1;

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
