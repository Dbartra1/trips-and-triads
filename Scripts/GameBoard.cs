using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.UI;

public partial class GameBoard : Node2D
{
	[Export] public Control  BoardContainer   { get; set; }
	[Export] public Control  HandContainer    { get; set; }
	[Export] public Control  AIHandContainer  { get; set; }
	[Export] public Control  KillFeedContainer { get; set; } // optional scene node; created in code if null
	[Export] public HandNode PlayerHand     { get; set; }
	[Export] public HandNode AIHand         { get; set; }
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
	private KillFeedNode _killFeed;

	/// <summary>
	/// How long (seconds) the AI "thinks" before placing its card.
	/// 0.5–1.0 s makes it feel deliberate without feeling slow.
	/// Set to 0 to disable the delay (useful during automated tests).
	/// </summary>
	[Export] public float AiThinkDelay { get; set; } = 0.75f;

	private PackedScene _cardScene;
	private PackedScene _cellScene;

	public override void _Ready()
	{
		_cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");
		_cellScene = GD.Load<PackedScene>("res://Scenes/Board/CellNode.tscn");

		// Databases may already be loaded by GameSession autoload — safe to call again.
		// Do NOT call DistrictManager.Initialize() here — it would reset control meters
		// that were just loaded from a save file.
		CardDatabase.Instance.Load();
		DistrictDatabase.Instance.Load();

		// ── Read district and deck from GameSession if available ──────────────
		string districtId = "the_stub";
		List<CardData> p1Cards;

		var session = GameSession.Instance;

		// Validate that SelectedDeck cards are still in the roster — a saved deck
		// can become stale if cards were captured between sessions.
		if (session != null)
		{
			session.SelectedDeck.RemoveAll(c => !session.Roster.Contains(c));
		}

		if (session != null && session.HasStandoffHands)
		{
			// Standoff rematch — use the board-state hands saved before the reload.
			districtId = session.SelectedDistrictId;
			p1Cards    = session.StandoffP1Hand;
			session.ClearStandoffHands();
			GD.Print($"GameBoard: Standoff rematch — P1 hand has {p1Cards.Count} cards.");
		}
		else if (session != null && session.SelectedDeck.Count == 5)
		{
			districtId = session.SelectedDistrictId;
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
		// No valid deck — redirect to PreMatchScreen so the player can pick one.
		// Must use CallDeferred because ChangeSceneToFile can't be called
		// while the scene tree is still initializing in _Ready().
		GD.Print("GameBoard: no valid deck in session — redirecting to PreMatchScreen.");
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile,
			"res://Scenes/PreMatch/PreMatchScreen.tscn");
		return;
		}
		else
		{
			// True standalone fallback (editor only) — generate a crew directly.
			GD.Print("GameBoard: no GameSession — generating crew directly (editor mode).");
			var crew = CrewGenerator.Generate();
			p1Cards  = CrewGenerator.SelectBestFive(crew);
		}

		DistrictManager.Instance.SelectDistrict(districtId);
		_matchConfig = DistrictManager.Instance.BuildMatchConfig();
		_game = new GameManager(_matchConfig);

		var district = DistrictManager.Instance.ActiveDistrict;
		GD.Print($"District: {district?.Name} | Stake: {district?.Stake}");
		GD.Print($"Active protocols: {string.Join(", ", _matchConfig.Protocols.ConvertAll(p => p.Name))}");

		if (DistrictLabel != null)
		{
			bool isHunt       = session?.IsHuntMatch == true;
			string stakeName  = isHunt ? "As Flipped (Reclaim)" : (district?.Stake ?? "");
			DistrictLabel.Text = $"{district?.Name ?? ""}  ·  {stakeName}";
		}

		// ── AI hand — faction-matched to district controller ──────────────────
		string controller = district?.Controller ?? "Neutral";
		var p2Cards = CrewGenerator.GenerateFactionHand(
			CardDatabase.Instance, controller, new System.Random());

		// Set Vesna's starting cap for this district.
		// In The Hush (home district) she enters near full strength.
		// Neutral/Stub districts cap her low. She doesn't appear outside HollowChoir.
		_game.VesnaStartingCap = controller switch
		{
			"HollowChoir" => 20,   // The Hush — full threat
			_             => 14,   // anywhere else she's just a street-level ghost
		};

		// Hunt match — replace the AI's hero slot with the captured hero.
		// The faction crew guards the prize; the captured hero is the centrepiece.
		if (session?.IsHuntMatch == true && session.CapturedHero != null)
		{
			GD.Print($"GameBoard: Hunt match — inserting {session.CapturedHero.Name} into AI hand.");
			// Hero is always index 0 in faction hands
			p2Cards[0] = session.CapturedHero;
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
		SizeHandContainers();
		InitKillFeed();
		RefreshHand();
		RefreshAIHand();
		UpdateScores();

		if (PlayerHand != null)
			PlayerHand.CardSelected += OnCardSelected;

		GD.Print("Board ready. Player 1's turn.");
	}

	private void InitKillFeed()
	{
		// If a KillFeedContainer node was assigned in the scene editor, use it.
		// Otherwise create a Control and position it below the board automatically.
		var parent = KillFeedContainer ?? GetNodeOrNull<Control>("CanvasLayer");

		_killFeed = new KillFeedNode();
		_killFeed.MouseFilter = Control.MouseFilterEnum.Ignore;

		if (KillFeedContainer != null)
		{
			// Fill whatever container was assigned
			_killFeed.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
			KillFeedContainer.AddChild(_killFeed);
		}
		else
		{
			// Auto-position below the board: same X span, fixed 40px height
			var boardPos = BoardContainer?.GlobalPosition ?? new Vector2(424, 168);
			float boardW = BoardState.Size * CellWidth;
			float boardBottom = boardPos.Y + BoardState.Size * CellHeight;

			_killFeed.GlobalPosition    = new Vector2(boardPos.X, boardBottom + 12f);
			_killFeed.CustomMinimumSize = new Vector2(boardW, 120f);
			_killFeed.Size              = new Vector2(boardW, 120f);

			// Add to CanvasLayer if found, else to GameBoard itself
			var canvas = GetNodeOrNull<CanvasLayer>("CanvasLayer");
			if (canvas != null) canvas.AddChild(_killFeed);
			else                AddChild(_killFeed);
		}

		GD.Print("KillFeed: initialized.");
	}

	private void SizeHandContainers()
	{
		float boardH     = BoardState.Size * CellHeight;
		var   handSz     = new Vector2(150f, boardH);
		var   boardPos   = BoardContainer?.GlobalPosition ?? Vector2.Zero;
		float boardRight = boardPos.X + BoardState.Size * CellWidth;

		// Player hand on the RIGHT, AI hand on the LEFT.
		// 90 px gap between board edge and each hand container.
		if (HandContainer != null)
		{
			HandContainer.MouseFilter       = Control.MouseFilterEnum.Ignore;
			HandContainer.CustomMinimumSize = handSz;
			HandContainer.Size              = handSz;
			HandContainer.GlobalPosition    = new Vector2(boardRight + 90f, boardPos.Y);
			GD.Print($"HandContainer (player): pos={HandContainer.GlobalPosition}");
		}

		if (AIHandContainer != null)
		{
			AIHandContainer.MouseFilter       = Control.MouseFilterEnum.Ignore;
			AIHandContainer.CustomMinimumSize = handSz;
			AIHandContainer.Size              = handSz;
			AIHandContainer.GlobalPosition    = new Vector2(boardPos.X - 240f, boardPos.Y);
			GD.Print($"AIHandContainer: pos={AIHandContainer.GlobalPosition}");
		}
	}

	private void SaveStandoffHands()
	{
		// Collect each player's board-controlled cards + any unplayed hand card.
		// These become the hands for the rematch, as per systems.md §3.7.
		var p1 = new List<CardData>();
		var p2 = new List<CardData>();

		for (int r = 0; r < BoardState.Size; r++)
			for (int c = 0; c < BoardState.Size; c++)
			{
				var card = _game.Board.GetCard(r, c);
				if (card == null) continue;
				if (card.OwnerId == 1) p1.Add(card.Data);
				else                   p2.Add(card.Data);
			}

		// Include unplayed hand card (the one card not yet placed)
		foreach (var c in _game.GetHand(1)) p1.Add(c.Data);
		foreach (var c in _game.GetHand(2)) p2.Add(c.Data);

		GameSession.Instance?.SetStandoffHands(p1, p2);
		GD.Print($"Standoff: saved P1={p1.Count} cards, P2={p2.Count} cards for rematch.");
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

	/// <summary>
	/// Populates the AI hand display. Called once after DealHands.
	/// AI cards show with OwnerId=2 (magenta) and are non-interactive —
	/// HandNode's click buttons are removed after population.
	/// Under Intercept this was the only time the AI hand was visible;
	/// now it is always shown so the player can see what they're up against.
	/// </summary>
	private void RefreshAIHand()
	{
		if (AIHand == null) return;
		AIHand.PopulateHand(_game.GetHand(2));
		AIHand.SetInteractive(false);
	}

	private void UpdateScores()
	{
		if (ScoreP1 != null)
		{
			ScoreP1.Text = $"P1  {_game.Board.GetScore(1)}";
			ScoreP1.AddThemeColorOverride("font_color", new Color("3ecdef"));
		}
		if (ScoreP2 != null)
		{
			ScoreP2.Text = $"{_game.Board.GetScore(2)}  P2";
			ScoreP2.AddThemeColorOverride("font_color", new Color("fd1d75"));
		}
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
				var col = (session?.PlayerWon ?? false) ? new Color("3ecdef") : new Color("fd1d75");
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
						TryLoseCard(session, card.Data, wasHuntMatch);
				}
			return;
		}

		session.CardsWon.Clear();
		session.CardsLost.Clear();

		switch (stake)
		{
			case "OneJob":
				if (playerWon)
				{
					var won = GetFirstBoardCard(originalOwnerId: 2);
					if (won != null) session.CardsWon.Add(won);
				}
				else
				{
					var lost = GetFirstBoardCard(originalOwnerId: 1);
					if (lost != null) TryLoseCard(session, lost, wasHuntMatch);
				}
				break;

			case "TheSpread":
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
						TryLoseCard(session, p1Cards[i], wasHuntMatch);
				}
				break;

			case "Everything":
				// Winner takes ALL of the loser's cards — board cards AND the one
				// unplayed card still in hand. The board only has 9 cards; the 10th
				// belongs to whoever didn't play last.
				if (playerWon)
				{
					session.CardsWon.AddRange(GetAllBoardCards(originalOwnerId: 2));
					// Add any AI card still in hand (unplayed)
					foreach (var c in _game.GetHand(2))
						session.CardsWon.Add(c.Data);
				}
				else
				{
					foreach (var c in GetAllBoardCards(originalOwnerId: 1))
						TryLoseCard(session, c, wasHuntMatch);
					// Add any player card still in hand (unplayed)
					foreach (var c in _game.GetHand(1))
						TryLoseCard(session, c.Data, wasHuntMatch);
				}
				break;

			default:
				GD.PrintErr($"ResolveStake: unknown stake '{stake}' — defaulting to OneJob.");
				goto case "OneJob";
		}
	}

	/// <summary>
	/// <summary>
	/// Lose a card: add to CardsLost, and if it's the player's hero trigger the Hunt.
	/// Only opens a new Hunt if one is not already active — losing a second hero
	/// while Headless doesn't chain Hunts; the card is simply lost.
	///
	/// Option A immunity: during a Hunt match, heroes are never capturable.
	/// The reclaim duel is a sanctioned engagement — hero stakes are not on the table.
	/// This prevents the reclaim→interim-capture→new-Hunt loop.
	/// </summary>
	private void TryLoseCard(GameSession session, CardData card, bool isHuntMatch = false)
	{
		// Option A: heroes are immune to capture during Hunt matches
		if (isHuntMatch && card.Tier == Tier.Hero)
		{
			GD.Print($"GameBoard: Hunt match — hero {card.Name} immune to capture (Option A).");
			return;
		}

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
		// Toggle-deselect if same card tapped
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
		GD.Print($"Card selected (drag starting): {_selectedCardInstance.Data.Name}");
	}

	private void OnCellClicked(int row, int col)
	{
		if (_selectedCard == null || _selectedCardInstance == null)
		{ GD.Print("No card dragged from hand yet."); return; }

		if (_game.CurrentPlayerId != 1)
		{ GD.Print("Not Player 1's turn."); return; }

		var hand         = _game.GetHand(1);
		int currentIndex = hand.IndexOf(_selectedCardInstance);

		if (currentIndex < 0)
		{
			GD.PrintErr("Dragged card not found in hand.");
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

		// Flip animation for every captured card
		foreach (var (r, c) in captured) _cells[r, c].FlipCard();
		RefreshAllCells();

		_killFeed?.PushEvents(_game.LastTurnEvents);

		_selectedCard = null; _selectedCardInstance = null; _selectedHandIndex = -1;

		UpdateScores();
		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.StandoffTriggered)
		
		{
			GD.Print("Standoff — saving board hands and restarting.");
			SaveStandoffHands();
			GetTree().ReloadCurrentScene();
			return;
		}
		if (_game.GameOver) { EndMatchAndTransition(); return; }

		// AI turn begins after a thinking delay
		RunAIDelayed();
	}

	// ── AI thinking delay + animated card placement ───────────────────────────

	/// <summary>
	/// Entry point after the player places a card. Waits AiThinkDelay seconds
	/// before the AI places its card, so it feels like the AI is deciding.
	/// </summary>
	private async void RunAIDelayed()
	{
		// Guard: game may have ended before the timer fires (e.g. fast Standoff)
		if (_game.GameOver || _game.StandoffTriggered) return;

		if (AiThinkDelay > 0f)
			await ToSignal(GetTree().CreateTimer(AiThinkDelay), SceneTreeTimer.SignalName.Timeout);

		// Re-check after the delay
		if (_game.GameOver || _game.StandoffTriggered) return;

		await RunAI();
	}

	private async Task RunAI()
	{
		var hand = _game.GetHand(2);
		if (hand.Count == 0) return;

		// ── Greedy decision ───────────────────────────────────────────────────
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

		if (bestRow < 0 || bestHandIndex >= hand.Count) return;

		string aiCardName = hand[bestHandIndex].Data.Name;

		var aiCard = _cardScene.Instantiate<CardNode>();
		aiCard.Initialize(hand[bestHandIndex]);

		// Remove from AI hand display BEFORE the tween so the slot empties visually
		AIHand?.RemoveCard(bestHandIndex);

		// ── Animated card movement: hand area → board cell ────────────────────
		Vector2 startPos = GetAICardStartPosition(bestHandIndex);
		Vector2 cellGlobal = _cells[bestRow, bestCol].GlobalPosition
		                   + new Vector2(CellPadding, CellPadding);

		// Temporarily place card in the scene root for animation
		AddChild(aiCard);
		aiCard.GlobalPosition  = startPos;
		aiCard.ZIndex          = 10; // float above everything
		aiCard.CustomMinimumSize = new Vector2(CardWidth, CardHeight);
		aiCard.Size              = new Vector2(CardWidth, CardHeight);

		var tween = CreateTween();
		tween.TweenProperty(aiCard, "global_position", cellGlobal, 0.30f)
		     .SetTrans(Tween.TransitionType.Cubic)
		     .SetEase(Tween.EaseType.Out);

		await ToSignal(tween, Tween.SignalName.Finished);

		// ── Update game state after animation completes ───────────────────────
		var captured = _game.PlayCard(bestHandIndex, bestRow, bestCol);
		if (captured == null) { aiCard.QueueFree(); return; }

		// Transfer card from scene root into the cell container
		RemoveChild(aiCard);
		aiCard.ZIndex = 0;
		_cells[bestRow, bestCol].PlaceCard(aiCard);

		// Flip animation for every captured card
		foreach (var (cr, cc) in captured) _cells[cr, cc].FlipCard();
		RefreshAllCells();

		_killFeed?.PushEvents(_game.LastTurnEvents);

		UpdateScores();
		GD.Print($"AI played {aiCardName} at ({bestRow},{bestCol}) capturing {captured.Count}.");
		GD.Print($"P1: {_game.Board.GetScore(1)} | P2: {_game.Board.GetScore(2)}");

		if (_game.StandoffTriggered)
		
		{
			GD.Print("Standoff — saving board hands and restarting.");
			SaveStandoffHands();
			GetTree().ReloadCurrentScene();
			return;
		}
		if (_game.GameOver) EndMatchAndTransition();
	}

	/// <summary>
	/// Returns the global start position for the AI card animation.
	/// Uses the AIHand card node's position if available; falls back to a
	/// reasonable off-screen position above the board.
	/// </summary>
	private Vector2 GetAICardStartPosition(int handIndex)
	{
		if (AIHand != null && handIndex >= 0 && handIndex < AIHand.Count)
			return AIHand.GetCardGlobalPosition(handIndex);

		// Fallback: position at the top-right of the viewport
		var viewportSize = GetViewport()?.GetVisibleRect().Size ?? new Vector2(1280, 720);
		return new Vector2(viewportSize.X - 160, 80);
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