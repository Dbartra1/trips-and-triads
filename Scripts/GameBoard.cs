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
	[Export] public Label    DistrictLabel  { get; set; }
	// End-of-match review overlay — reuses the existing GameOverPanel nodes
	[Export] public Panel    EndPanel       { get; set; }
	[Export] public Label    EndResultLabel { get; set; }
	[Export] public Label    EndScoreLabel  { get; set; }
	[Export] public Button   EndContinueBtn { get; set; }

	private const int CardWidth   = 120;
	private const int CardHeight  = 160;
	private const int CellPadding = 16;
	private const int CellWidth   = CardWidth  + CellPadding * 2;
	private const int CellHeight  = CardHeight + CellPadding * 2;

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

		// Databases may already be loaded by GameSession autoload — safe to call again
		CardDatabase.Instance.Load();
		DistrictDatabase.Instance.Load();
		DistrictManager.Instance.Initialize();

		// ── Read district and deck from GameSession if available ──────────────
		string districtId = "the_stub";
		List<CardData> p1Cards;

		var session = GameSession.Instance;
		if (session != null && session.SelectedDeck.Count == 5)
		{
			districtId = session.SelectedDistrictId;
			// Under Conscription, pass the full roster so the random draw has the whole pool.
			// Otherwise pass the selected 5-card deck.
			bool isConscription = DistrictDatabase.Instance
				.GetDistrict(districtId)?.Conscription ?? false;
			p1Cards = isConscription
				? new List<CardData>(session.Roster)
				: new List<CardData>(session.SelectedDeck);
			GD.Print($"GameBoard: loaded {(isConscription ? "roster" : "deck")} " +
			         $"from GameSession ({p1Cards.Count} cards).");
		}
		else if (session != null && session.Roster.Count >= 5)
		{
			// Session exists but no deck picked — auto-select best 5 from roster.
			// This ensures board cards are always from the Roster so stake removal works.
			districtId = session.SelectedDistrictId;
			p1Cards    = CrewGenerator.SelectBestFive(new System.Collections.Generic.List<CardData>(session.Roster));
			session.SelectedDeck = new System.Collections.Generic.List<CardData>(p1Cards);
			GD.Print($"GameBoard: auto-selected best 5 from roster ({p1Cards.Count} cards).");
			foreach (var c in p1Cards)
				GD.Print($"  {c.Name} ({c.Tier})");
		}
		else
		{
			// True standalone fallback (editor only) — generate a crew directly.
			// Note: stake resolution won't affect GameSession roster in this mode.
			GD.Print("GameBoard: no GameSession — generating crew directly (editor mode).");
			var crew = CrewGenerator.Generate();
			p1Cards  = CrewGenerator.SelectBestFive(crew);
			GD.Print("=== Generated Crew (editor standalone) ===");
			foreach (var c in crew)
				GD.Print($"  [{c.Tier}] {c.Name} | {c.Top}/{c.Right}/{c.Bottom}/{c.Left} | Domain:{c.DomainType} Ability:{c.AbilityType}");
			GD.Print("=== Playing best 5 ===");
			foreach (var c in p1Cards)
				GD.Print($"  {c.Name} ({c.Tier})");
		}

		DistrictManager.Instance.SelectDistrict(districtId);
		_matchConfig = DistrictManager.Instance.BuildMatchConfig();
		_game = new GameManager(_matchConfig);

		var district = DistrictManager.Instance.ActiveDistrict;
		GD.Print($"District: {district?.Name} | Stake: {district?.Stake}");
		GD.Print($"Active protocols: {string.Join(", ", _matchConfig.Protocols.ConvertAll(p => p.Name))}");

		if (DistrictLabel != null)
			DistrictLabel.Text = district?.Name ?? "";

		// ── AI hand ───────────────────────────────────────────────────────────
		var p2Cards = CrewGenerator.GenerateAIHand(CardDatabase.Instance);

		// Hunt match — the captured hero is the centrepiece; Vesna is replaced with
		// a generated Pro. Lore: apex faction leaders don't run errands — this is
		// foot-soldier work. Verity stays (face-level operative, plausible for the job).
		if (session?.IsHuntMatch == true && session.CapturedHero != null)
		{
			// Swap Vesna (index 0) for a generated Pro-tier card
			var rng       = new System.Random();
			var usedNames = new System.Collections.Generic.HashSet<string>(
				p2Cards.ConvertAll(c => c.Name));
			var proCard   = CrewGenerator.GeneratePro(rng, usedNames);
			p2Cards[0]    = proCard;

			// Replace the last Street with the captured hero
			GD.Print($"GameBoard: Hunt match — inserting {session.CapturedHero.Name} into AI hand.");
			p2Cards[p2Cards.Count - 1] = session.CapturedHero;
		}

		GD.Print("=== AI Hand ===");
		foreach (var c in p2Cards)
			GD.Print($"  [{c.Tier}] {c.Name} | {c.Top}/{c.Right}/{c.Bottom}/{c.Left}");

		if (p1Cards.Count < 5 || (!_matchConfig.Conscription && p2Cards.Count < 5))
		{
			GD.PrintErr("GameBoard: could not build full hands.");
			return;
		}

		_game.DealHands(p1Cards, p2Cards);
		GD.Print($"P1 hand count: {_game.GetHand(1).Count}");

		SpawnGrid();
		RefreshHand();
		UpdateScores();

		if (PlayerHand != null)
			PlayerHand.CardSelected += OnCardSelected;

		GD.Print("Board ready. Player 1's turn.");
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

	private void EndMatchAndTransition()
	{
		int  p1Score  = _game.Board.GetScore(1);
		int  p2Score  = _game.Board.GetScore(2);
		bool playerWon = p1Score > p2Score;

		string heroFaction = GetPlayerHeroFaction();
		DistrictManager.Instance.ApplySpreading(
			DistrictManager.Instance.ActiveDistrictId, heroFaction, playerWon);

		// Write result to GameSession for PostMatchScreen to read
		var session = GameSession.Instance;
		if (session != null)
		{
			session.P1FinalScore = p1Score;
			session.P2FinalScore = p2Score;
			session.PlayerWon    = playerWon;
			session.WinnerText   = playerWon ? "Player 1 Wins!"
				: p2Score > p1Score ? "Player 2 Wins!" : "Draw";

			// ── Hunt match resolution (systems.md §7.3) ───────────────────────
			// Capture the flag now; ReclaimHero() calls ClearHunt() which clears it.
			bool wasHuntMatch = session.IsHuntMatch;

			if (wasHuntMatch)
			{
				if (playerWon)
				{
					// Win the Hunt → hero returns; still apply AsFlipped for other cards
					GD.Print("GameBoard: Hunt won — hero reclaimed.");
					session.ReclaimHero();
				}
				else
				{
					// Loss → consume an attempt; window closes at 0
					int remaining = session.ConsumeReclaimAttempt();
					GD.Print($"GameBoard: Hunt lost. {remaining} attempt(s) left.");
					session.IsHuntMatch = false;
				}
			}

			// Stake resolution — wasHuntMatch drives AsFlipped path in ResolveStake
			ResolveStake(session, playerWon, wasHuntMatch);
		}

		// Show end-of-match overlay so player can review the board before continuing.
		if (EndPanel != null)
		{
			EndPanel.Visible = true;

			if (EndResultLabel != null)
			{
				EndResultLabel.Text = session?.WinnerText ?? "";
				var col = (session?.PlayerWon ?? false) ? new Color("4a90d9") : new Color("d94a4a");
				EndResultLabel.AddThemeColorOverride("font_color", col);
			}

			if (EndScoreLabel != null)
				EndScoreLabel.Text = $"P1: {p1Score}   |   P2: {p2Score}";

			if (EndContinueBtn != null)
			{
				EndContinueBtn.Pressed += () =>
					GetTree().ChangeSceneToFile("res://Scenes/PostMatch/PostMatchScreen.tscn");
			}
		}
		else
		{
			GetTree().ChangeSceneToFile("res://Scenes/PostMatch/PostMatchScreen.tscn");
		}
	}

	private void ResolveStake(GameSession session, bool playerWon, bool wasHuntMatch = false)
	{
		var    district = DistrictManager.Instance.ActiveDistrict;
		string stake    = district?.Stake ?? "OneJob";

		// Hunt matches always resolve as AsFlipped for normal card exchange;
		// hero reclaim is handled separately in EndMatchAndTransition.
		if (wasHuntMatch || stake == "AsFlipped")
		{
			// Player keeps every AI-original card they now control.
			// Player loses every player-original card the AI now controls.
			session.CardsWon.Clear();
			session.CardsLost.Clear();
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					var card = _game.Board.GetCard(r, c);
					if (card == null) continue;
					if (card.OriginalOwnerId == 2 && card.OwnerId == 1)
						session.CardsWon.Add(card.Data);
					else if (card.OriginalOwnerId == 1 && card.OwnerId == 2)
						TryLoseCard(session, card.Data);
				}
			return;
		}

		session.CardsWon.Clear();
		session.CardsLost.Clear();

		switch (stake)
		{
			case "OneJob":
				// Winner's choice: one card from the loser's on-board cards.
				// Prefers non-hero, but heroes are now fully capturable.
				if (playerWon)
				{
					var won = GetFirstBoardCard(originalOwnerId: 2);
					if (won != null) session.CardsWon.Add(won);
				}
				else
				{
					var lost = GetFirstBoardCard(originalOwnerId: 1);
					if (lost != null) TryLoseCard(session, lost);
				}
				break;

			case "TheSpread":
				// Winner takes N cards equal to the margin (winner score − loser score).
				int margin = System.Math.Abs(session.P1FinalScore - session.P2FinalScore);
				if (playerWon)
				{
					var aiCards = GetAllBoardCards(originalOwnerId: 2);
					for (int i = 0; i < margin && i < aiCards.Count; i++)
						session.CardsWon.Add(aiCards[i]);
				}
				else
				{
					var p1Cards = GetAllBoardCards(originalOwnerId: 1);
					for (int i = 0; i < margin && i < p1Cards.Count; i++)
						TryLoseCard(session, p1Cards[i]);
				}
				break;

			case "Everything":
				// Winner takes the loser's entire played hand.
				if (playerWon)
				{
					session.CardsWon.AddRange(GetAllBoardCards(originalOwnerId: 2));
				}
				else
				{
					foreach (var c in GetAllBoardCards(originalOwnerId: 1))
						TryLoseCard(session, c);
				}
				break;

			default:
				GD.PrintErr($"ResolveStake: unknown stake '{stake}' — defaulting to OneJob.");
				goto case "OneJob";
		}
	}

	/// <summary>
	/// Lose a card: add to CardsLost, and if it's the player's hero trigger the Hunt.
	/// Only opens a new Hunt if one is not already active — losing a second hero
	/// while Headless doesn't chain Hunts; the card is simply lost.
	/// </summary>
	private void TryLoseCard(GameSession session, CardData card)
	{
		session.CardsLost.Add(card);

		if (card.Tier == Tier.Hero && session != null && !session.IsHeadless)
		{
			var capturingFaction = GetAIHeroFaction();
			GD.Print($"GameBoard: player hero {card.Name} captured — Hunt opens " +
			         $"(captor faction: {capturingFaction}).");
			session.SetCapturedHero(card, capturingFaction);
		}
		else if (card.Tier == Tier.Hero && session != null && session.IsHeadless)
		{
			GD.Print($"GameBoard: hero {card.Name} lost while already Headless — " +
			         $"no second Hunt opened; card removed from roster.");
		}
	}

	/// <summary>
	/// Returns the first board card that was originally owned by <paramref name="originalOwnerId"/>.
	/// Heroes are no longer protected — they can be taken like any other card.
	/// </summary>
	private CardData GetFirstBoardCard(int originalOwnerId)
	{
		for (int r = 0; r < BoardState.Size; r++)
			for (int c = 0; c < BoardState.Size; c++)
			{
				var card = _game.Board.GetCard(r, c);
				if (card != null && card.OriginalOwnerId == originalOwnerId)
					return card.Data;
			}
		return null;
	}

	/// <summary>Returns all board cards originally owned by <paramref name="originalOwnerId"/>, sorted best-first.</summary>
	private List<CardData> GetAllBoardCards(int originalOwnerId)
	{
		var result = new List<CardData>();
		for (int r = 0; r < BoardState.Size; r++)
			for (int c = 0; c < BoardState.Size; c++)
			{
				var card = _game.Board.GetCard(r, c);
				if (card != null && card.OriginalOwnerId == originalOwnerId)
					result.Add(card.Data);
			}
		// Sort hero-last so regular cards are taken before heroes when margin < total.
		result.Sort((a, b) =>
		{
			if (a.Tier == Tier.Hero && b.Tier != Tier.Hero) return 1;
			if (a.Tier != Tier.Hero && b.Tier == Tier.Hero) return -1;
			return 0;
		});
		return result;
	}

	private string GetPlayerHeroFaction()
	{
		for (int r = 0; r < BoardState.Size; r++)
			for (int c = 0; c < BoardState.Size; c++)
			{
				var card = _game.Board.GetCard(r, c);
				if (card != null && card.OwnerId == 1 && card.Data.Tier == Tier.Hero)
					return card.Data.Faction.ToString();
			}
		return "None";
	}

	/// <summary>Returns the faction of the first hero found in the AI's original hand.</summary>
	private Faction GetAIHeroFaction()
	{
		for (int r = 0; r < BoardState.Size; r++)
			for (int c = 0; c < BoardState.Size; c++)
			{
				var card = _game.Board.GetCard(r, c);
				if (card != null && card.OriginalOwnerId == 2 && card.Data.Tier == Tier.Hero)
					return card.Data.Faction;
			}
		return Faction.None;
	}

	private void OnCardSelected(int handIndex, CardNode cardNode)
	{
		if (_selectedCard == cardNode)
		{
			_selectedCard.SetSelected(false);
			_selectedCard = null; _selectedCardInstance = null;
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
		{ GD.Print("No card selected from hand yet."); return; }

		if (_game.CurrentPlayerId != 1)
		{ GD.Print("Not Player 1's turn."); return; }

		var hand         = _game.GetHand(1);
		int currentIndex = hand.IndexOf(_selectedCardInstance);

		if (currentIndex < 0)
		{
			GD.PrintErr("Selected card not found in hand.");
			_selectedCard?.SetSelected(false);
			_selectedCard = null; _selectedCardInstance = null;
			return;
		}

		var captured = _game.PlayCard(currentIndex, row, col);
		if (captured == null) return;

		_selectedCard.SetSelected(false);
		int visualIndex = PlayerHand.GetCardNodeIndex(_selectedCard);
		PlayerHand.RemoveCard(visualIndex);
		_cells[row, col].PlaceCard(_selectedCard);

		foreach (var (r, c) in captured) _cells[r, c].RefreshCard();
		RefreshAllCells();

		_selectedCard = null; _selectedCardInstance = null; _selectedHandIndex = -1;

		UpdateScores();
		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.StandoffTriggered)
		{ GD.Print("Standoff — restarting."); GetTree().ReloadCurrentScene(); return; }
		if (_game.GameOver) { EndMatchAndTransition(); return; }

		RunAI();
	}

	private void RunAI()
	{
		var hand = _game.GetHand(2);
		if (hand.Count == 0) return;

		int bestScore = -1, bestHandIndex = 0, bestRow = -1, bestCol = -1;

		for (int handIndex = 0; handIndex < hand.Count; handIndex++)
			for (int r = 0; r < BoardState.Size; r++)
				for (int c = 0; c < BoardState.Size; c++)
				{
					if (!_game.Board.IsEmpty(r, c)) continue;
					int captures = SimulateCaptures(hand[handIndex], r, c);
					if (captures > bestScore)
					{ bestScore = captures; bestHandIndex = handIndex; bestRow = r; bestCol = c; }
				}

		if (bestRow < 0 || bestHandIndex >= hand.Count) return; // no valid move found
		// Capture name BEFORE PlayCard — PlayCard removes the card from hand,
		// making hand[bestHandIndex] stale after the call.
		string aiCardName = hand[bestHandIndex].Data.Name;

		var aiCard = _cardScene.Instantiate<CardNode>();
		aiCard.Initialize(hand[bestHandIndex]);

		var captured = _game.PlayCard(bestHandIndex, bestRow, bestCol);
		if (captured == null) return;

		_cells[bestRow, bestCol].PlaceCard(aiCard);
		foreach (var (cr, cc) in captured) _cells[cr, cc].RefreshCard();
		RefreshAllCells();

		UpdateScores();
		GD.Print($"AI played {aiCardName} at ({bestRow},{bestCol}) capturing {captured.Count}.");
		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.StandoffTriggered)
		{ GD.Print("Standoff — restarting."); GetTree().ReloadCurrentScene(); return; }
		if (_game.GameOver) EndMatchAndTransition();
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
			if (card.GetValue(dir) > neighbor.GetValue(card.Data.Opposite(dir))) captures++;
		}
		return captures;
	}

	public void SelectCardFromHand(int handIndex, CardNode cardNode)
	{
		_selectedHandIndex = handIndex; _selectedCard = cardNode;
	}
}