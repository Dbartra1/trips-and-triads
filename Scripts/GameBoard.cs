using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.UI;

public partial class GameBoard : Node2D
{
	[Export] public Control  BoardContainer { get; set; }
	[Export] public HandNode PlayerHand     { get; set; }
	[Export] public Label    ScoreP1        { get; set; }
	[Export] public Label    ScoreP2        { get; set; }
	[Export] public Panel    GameOverPanel  { get; set; }
	[Export] public Label    GameOverLabel  { get; set; }
	[Export] public Label    GameOverScores { get; set; }
	[Export] public Button   RestartButton  { get; set; }
	[Export] public Label    DistrictLabel  { get; set; } // optional — shows active district name

	private const int CardWidth   = 120;
	private const int CardHeight  = 160;
	private const int CellPadding = 16;
	private const int CellWidth   = CardWidth  + CellPadding * 2; // 152
	private const int CellHeight  = CardHeight + CellPadding * 2; // 192

	private GameManager  _game;
	private CellNode[,]  _cells                = new CellNode[BoardState.Size, BoardState.Size];
	private CardNode     _selectedCard         = null;
	private CardInstance _selectedCardInstance = null;
	private int          _selectedHandIndex    = -1;
	private MatchConfig  _matchConfig;

	private PackedScene _cardScene;
	private PackedScene _cellScene;

	public override void _Ready()
	{
		_cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");
		_cellScene = GD.Load<PackedScene>("res://Scenes/Board/CellNode.tscn");

		// Load databases
		CardDatabase.Instance.Load();
		DistrictDatabase.Instance.Load();
		DistrictManager.Instance.Initialize();

		// Select starting district — "the_stub" for base rules.
		// Change this string to test different districts:
		// "the_stub"          — base rules
		// "glass_spire"       — Intercept + Wall Signature + Handshake
		// "the_killfloor"     — Conscription + Standoff
		// "dead_channel"      — Intercept + Cascade
		// "the_sprawl_market" — Conscription
		// "the_powder_room"   — Tally + Handshake
		// "the_hush"          — Cascade + Wall Signature + Handshake
		// "the_vault"         — all protocols (locked by default)
		DistrictManager.Instance.SelectDistrict("the_stub");

		_matchConfig = DistrictManager.Instance.BuildMatchConfig();
		_game = new GameManager(_matchConfig);

		var district = DistrictManager.Instance.ActiveDistrict;
		GD.Print($"District: {district?.Name} | Stake: {district?.Stake}");
		GD.Print($"Active protocols: {string.Join(", ", _matchConfig.Protocols.ConvertAll(p => p.Name))}");

		if (DistrictLabel != null)
			DistrictLabel.Text = district?.Name ?? "";

		var p1Cards = BuildHand(
			"asc_hero_seraph_yune",
			"rzk_top_gristle",
			"gwi_top_echo",
			"com_hero_mara_kane",
			"lac_top_aoi"
		);

		var p2Cards = BuildHand(
			"hch_hero_vesna",
			"eff_top_verity",
			"rzk_hero_sister_grin",
			"free_pro_merc_sniper",
			"lac_hero_madame_sumi"
		);

		if (!_matchConfig.Conscription && (p1Cards.Count < 5 || p2Cards.Count < 5))
		{
			GD.PrintErr("GameBoard: could not build full hands — check cards.json IDs.");
			return;
		}

		_game.DealHands(p1Cards, p2Cards);

		GD.Print($"P1 hand count: {_game.GetHand(1).Count}");

		SpawnGrid();
		RefreshHand();
		UpdateScores();

		if (PlayerHand != null)
			PlayerHand.CardSelected += OnCardSelected;

		if (RestartButton != null)
			RestartButton.Pressed += () => GetTree().ReloadCurrentScene();

		GD.Print("Board ready. Player 1's turn.");
	}

	private List<CardData> BuildHand(params string[] ids)
	{
		var result = new List<CardData>();
		foreach (var id in ids)
		{
			var card = CardDatabase.Instance.GetCard(id);
			if (card == null)
				GD.PrintErr($"GameBoard: card not found — '{id}'");
			else
				result.Add(card);
		}
		return result;
	}

	private void SpawnGrid()
	{
		for (int row = 0; row < BoardState.Size; row++)
			for (int col = 0; col < BoardState.Size; col++)
			{
				var cell = _cellScene.Instantiate<CellNode>();
				BoardContainer.AddChild(cell);
				cell.Initialize(row, col);
				cell.Position = new Vector2(col * CellWidth, row * CellHeight);
				cell.CallDeferred("set_size", new Vector2(CellWidth, CellHeight));
				cell.CellClicked += OnCellClicked;
				_cells[row, col] = cell;
			}
	}

	private void RefreshHand()
	{
		if (PlayerHand == null) return;
		PlayerHand.PopulateHand(_game.GetHand(1));
	}

	private void UpdateScores()
	{
		if (ScoreP1 != null) ScoreP1.Text = $"P1  {_game.Board.GetScore(1)}";
		if (ScoreP2 != null) ScoreP2.Text = $"{_game.Board.GetScore(2)}  P2";
	}

	private void RefreshAllCells()
	{
		for (int r = 0; r < BoardState.Size; r++)
			for (int c = 0; c < BoardState.Size; c++)
				_cells[r, c].RefreshCard();
	}

	private void ShowGameOver()
	{
		if (GameOverPanel == null) return;

		int p1Score = _game.Board.GetScore(1);
		int p2Score = _game.Board.GetScore(2);

		bool playerWon = p1Score > p2Score;

		// Apply Spreading Rule — use the hero P1 fielded (first hero in hand if any)
		string heroFaction = GetPlayerHeroFaction();
		DistrictManager.Instance.ApplySpreading(
			DistrictManager.Instance.ActiveDistrictId,
			heroFaction,
			playerWon);

		string resultText;
		Color  resultColor;

		if (p1Score > p2Score)
		{
			resultText  = "Player 1 Wins!";
			resultColor = new Color("4a90d9");
		}
		else if (p2Score > p1Score)
		{
			resultText  = "Player 2 Wins!";
			resultColor = new Color("d94a4a");
		}
		else
		{
			resultText  = "Draw";
			resultColor = new Color("cccccc");
		}

		if (GameOverLabel != null)
		{
			GameOverLabel.Text = resultText;
			GameOverLabel.AddThemeColorOverride("font_color", resultColor);
		}

		if (GameOverScores != null)
			GameOverScores.Text = $"P1: {p1Score}   |   P2: {p2Score}";

		GameOverPanel.Visible = true;
	}

	private string GetPlayerHeroFaction()
	{
		// Find P1's hero on the board (used for Spreading Rule)
		for (int r = 0; r < BoardState.Size; r++)
			for (int c = 0; c < BoardState.Size; c++)
			{
				var card = _game.Board.GetCard(r, c);
				if (card != null && card.OwnerId == 1 && card.Data.Tier == Tier.Hero)
					return card.Data.Faction.ToString();
			}
		return "None";
	}

	private void OnCardSelected(int handIndex, CardNode cardNode)
	{
		if (_selectedCard == cardNode)
		{
			_selectedCard.SetSelected(false);
			_selectedCard         = null;
			_selectedCardInstance = null;
			return;
		}

		_selectedCard?.SetSelected(false);
		_selectedCard         = cardNode;
		_selectedCardInstance = cardNode.GetCardInstance();
		_selectedCard.SetSelected(true);

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

		var hand         = _game.GetHand(1);
		int currentIndex = hand.IndexOf(_selectedCardInstance);

		if (currentIndex < 0)
		{
			GD.PrintErr("Selected card not found in hand.");
			_selectedCard?.SetSelected(false);
			_selectedCard         = null;
			_selectedCardInstance = null;
			return;
		}

		var captured = _game.PlayCard(currentIndex, row, col);
		if (captured == null) return;

		_selectedCard.SetSelected(false);

		int visualIndex = PlayerHand.GetCardNodeIndex(_selectedCard);
		PlayerHand.RemoveCard(visualIndex);

		_cells[row, col].PlaceCard(_selectedCard);

		foreach (var (r, c) in captured)
			_cells[r, c].RefreshCard();

		RefreshAllCells();

		_selectedCard         = null;
		_selectedCardInstance = null;
		_selectedHandIndex    = -1;

		UpdateScores();
		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.StandoffTriggered)
		{
			GD.Print("Standoff — restarting with board-state hands.");
			GetTree().ReloadCurrentScene();
			return;
		}

		if (_game.GameOver)
		{
			ShowGameOver();
			return;
		}

		RunAI();
	}

	private void RunAI()
	{
		var hand = _game.GetHand(2);
		if (hand.Count == 0) return;

		int bestScore     = -1;
		int bestHandIndex = 0;
		int bestRow       = -1;
		int bestCol       = -1;

		for (int handIndex = 0; handIndex < hand.Count; handIndex++)
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					if (!_game.Board.IsEmpty(r, c)) continue;
					int captures = SimulateCaptures(hand[handIndex], r, c);
					if (captures > bestScore)
					{
						bestScore     = captures;
						bestHandIndex = handIndex;
						bestRow       = r;
						bestCol       = c;
					}
				}

		var aiCard = _cardScene.Instantiate<CardNode>();
		aiCard.Initialize(hand[bestHandIndex]);

		var captured = _game.PlayCard(bestHandIndex, bestRow, bestCol);
		if (captured == null) return;

		_cells[bestRow, bestCol].PlaceCard(aiCard);

		foreach (var (cr, cc) in captured)
			_cells[cr, cc].RefreshCard();

		RefreshAllCells();

		UpdateScores();
		GD.Print($"AI played {hand[bestHandIndex].Data.Name} at ({bestRow},{bestCol}) capturing {captured.Count}.");
		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.StandoffTriggered)
		{
			GD.Print("Standoff — restarting with board-state hands.");
			GetTree().ReloadCurrentScene();
			return;
		}

		if (_game.GameOver)
			ShowGameOver();
	}

	private int SimulateCaptures(CardInstance card, int row, int col)
	{
		int captures = 0;

		foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
		{
			var (nRow, nCol) = _game.Board.GetNeighbor(row, col, dir);
			if (!_game.Board.IsInBounds(nRow, nCol)) continue;

			var neighbor = _game.Board.GetCard(nRow, nCol);
			if (neighbor == null || neighbor.OwnerId == 2) continue;

			int attackVal = card.GetValue(dir);
			int defendVal = neighbor.GetValue(card.Data.Opposite(dir));

			if (attackVal > defendVal)
				captures++;
		}

		return captures;
	}

	public void SelectCardFromHand(int handIndex, CardNode cardNode)
	{
		_selectedHandIndex = handIndex;
		_selectedCard      = cardNode;
		GD.Print($"Selected card at hand index {handIndex}");
	}
}
