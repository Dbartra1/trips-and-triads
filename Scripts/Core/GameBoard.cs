using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.UI;

public partial class GameBoard : Node2D
{
	[Export] public Control BoardContainer { get; set; }
	[Export] public HandNode PlayerHand { get; set; }

	private const int CellSize = 130;
	private GameManager _game;
	private CellNode[,] _cells = new CellNode[BoardState.Size, BoardState.Size];
	private CardNode _selectedCard = null;
	private CardInstance _selectedCardInstance = null;
	private int _selectedHandIndex = -1;

	private PackedScene _cardScene;
	private PackedScene _cellScene;

	public override void _Ready()
	{
		_cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");
		_cellScene = GD.Load<PackedScene>("res://Scenes/Board/CellNode.tscn");

		CardDatabase.Instance.Load();
		_game = new GameManager();

		var allCards = CardDatabase.Instance.GetAllCards();
		var p1Cards = new List<CardData> { allCards[0], allCards[1], allCards[2],
										   allCards[0], allCards[1] };
		var p2Cards = new List<CardData> { allCards[2], allCards[1], allCards[0],
										   allCards[2], allCards[1] };

		_game.DealHands(p1Cards, p2Cards);

		GD.Print($"PlayerHand is null: {PlayerHand == null}");
		GD.Print($"BoardContainer is null: {BoardContainer == null}");
		GD.Print($"P1 hand count: {_game.GetHand(1).Count}");

		SpawnGrid();
		RefreshHand();

		if (PlayerHand != null)
			PlayerHand.CardSelected += OnCardSelected;

		GD.Print("Board ready. Player 1's turn.");
	}

	private void SpawnGrid()
	{
		for (int row = 0; row < BoardState.Size; row++)
		{
			for (int col = 0; col < BoardState.Size; col++)
			{
				var cell = _cellScene.Instantiate<CellNode>();
				BoardContainer.AddChild(cell);
				cell.Initialize(row, col);
				cell.Position = new Vector2(col * CellSize, row * CellSize);
				cell.CallDeferred("set_size", new Vector2(120, 160));
				cell.CellClicked += OnCellClicked;
				_cells[row, col] = cell;
			}
		}
	}

	private void RefreshHand()
	{
		if (PlayerHand == null) return;
		PlayerHand.PopulateHand(_game.GetHand(1));
	}

	private void OnCardSelected(int handIndex, CardNode cardNode)
	{
		_selectedCard = cardNode;
		_selectedCardInstance = cardNode.GetCardInstance();
		GD.Print($"Card selected: {_selectedCardInstance.Data.Name}");
	}

	private void OnCellClicked(int row, int col)
	{
		if (_selectedCard == null || _selectedCardInstance == null)
		{
			GD.Print("No card selected from hand yet.");
			return;
		}

		if (_game.CurrentPlayerId != 1)
		{
			GD.Print("Not Player 1's turn.");
			return;
		}

		// Find current index of selected card in game hand
		var hand = _game.GetHand(1);
		int currentIndex = hand.IndexOf(_selectedCardInstance);

		if (currentIndex < 0)
		{
			GD.PrintErr("Selected card not found in hand.");
			_selectedCard = null;
			_selectedCardInstance = null;
			return;
		}

		var captured = _game.PlayCard(currentIndex, row, col);
		if (captured == null) return;

		// Find and remove visual card from hand
		int visualIndex = PlayerHand.GetCardNodeIndex(_selectedCard);
		PlayerHand.RemoveCard(visualIndex);

		_cells[row, col].PlaceCard(_selectedCard);

		foreach (var (r, c) in captured)
			_cells[r, c].RefreshCard();

		_selectedCard = null;
		_selectedCardInstance = null;
		_selectedHandIndex = -1;

		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.GameOver)
		{
			GD.Print("Game Over!");
			return;
		}

		RunAI();
	}

	private void RunAI()
	{
		var hand = _game.GetHand(2);
		if (hand.Count == 0) return;

		int bestScore = -1;
		int bestHandIndex = 0;
		int bestRow = -1;
		int bestCol = -1;

		for (int handIndex = 0; handIndex < hand.Count; handIndex++)
		{
			for (int r = 0; r < BoardState.Size; r++)
			{
				for (int c = 0; c < BoardState.Size; c++)
				{
					if (!_game.Board.IsEmpty(r, c)) continue;

					int captures = SimulateCaptures(hand[handIndex].Data, r, c);

					if (captures > bestScore)
					{
						bestScore     = captures;
						bestHandIndex = handIndex;
						bestRow       = r;
						bestCol       = c;
					}
				}
			}
		}

		var aiCard = _cardScene.Instantiate<CardNode>();
		aiCard.Initialize(hand[bestHandIndex]);

		var captured = _game.PlayCard(bestHandIndex, bestRow, bestCol);
		if (captured == null) return;

		_cells[bestRow, bestCol].PlaceCard(aiCard);

		foreach (var (cr, cc) in captured)
			_cells[cr, cc].RefreshCard();

		GD.Print($"AI played {hand[bestHandIndex].Data.Name} at ({bestRow},{bestCol}) capturing {captured.Count}.");
		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.GameOver)
			GD.Print("Game Over!");
	}

	private int SimulateCaptures(CardData card, int row, int col)
	{
		int captures = 0;

		foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
		{
			var (nRow, nCol) = _game.Board.GetNeighbor(row, col, dir);

			if (!_game.Board.IsInBounds(nRow, nCol)) continue;

			var neighbor = _game.Board.GetCard(nRow, nCol);
			if (neighbor == null) continue;
			if (neighbor.OwnerId == 2) continue;

			int attackVal = card.GetValue(dir);
			int defendVal = neighbor.Data.GetValue(card.Opposite(dir));

			if (attackVal > defendVal)
				captures++;
		}

		return captures;
	}

	public void SelectCardFromHand(int handIndex, CardNode cardNode)
	{
		_selectedHandIndex = handIndex;
		_selectedCard = cardNode;
		GD.Print($"Selected card at hand index {handIndex}");
	}
}
