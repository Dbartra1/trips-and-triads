using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.UI;

public partial class PreMatchScreen : Control
{
	[Export] public GridContainer DistrictGrid         { get; set; }
	[Export] public GridContainer RosterGrid           { get; set; }
	[Export] public GridContainer DeckGrid             { get; set; }
	[Export] public Label         DeckCountLabel       { get; set; }
	[Export] public Button        StartButton          { get; set; }
	[Export] public Label         DistrictNameLabel    { get; set; }
	[Export] public Label         DistrictDescLabel    { get; set; }
	[Export] public Label         DistrictStakeLabel   { get; set; }
	[Export] public Label         DistrictProtocolLabel{ get; set; }

	private PackedScene _cardScene;
	private string _selectedDistrictId = "the_stub";
	private List<CardData> _selectedDeck = new();
	private const int MaxDeckSize = 5;
	private bool _isRunOver = false;
	private VBoxContainer _huntPanel = null; // injected when Hunt is active
	private bool _stepUpMode = false;        // true while player is choosing who to promote
	private Button _stepUpToggleBtn = null;  // reference so we can relabel it on toggle

	public override void _Ready()
	{
		_cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");

		// Resolve node refs by path as fallback
		DistrictGrid          ??= GetNodeOrNull<GridContainer>("HSplit/Left/DistrictSection/DistrictGrid");
		RosterGrid            ??= GetNodeOrNull<GridContainer>("HSplit/Right/RosterSection/RosterScroll/RosterGrid");
		DeckGrid              ??= GetNodeOrNull<GridContainer>("HSplit/Left/DeckSection/DeckGrid");
		DeckCountLabel        ??= GetNodeOrNull<Label>("HSplit/Left/DeckSection/DeckCountLabel");
		StartButton           ??= GetNodeOrNull<Button>("HSplit/Left/StartButton");
		DistrictNameLabel     ??= GetNodeOrNull<Label>("HSplit/Left/DistrictSection/DistrictNameLabel");
		DistrictDescLabel     ??= GetNodeOrNull<Label>("HSplit/Left/DistrictSection/DistrictDescLabel");
		DistrictStakeLabel    ??= GetNodeOrNull<Label>("HSplit/Left/DistrictSection/DistrictStakeLabel");
		DistrictProtocolLabel ??= GetNodeOrNull<Label>("HSplit/Left/DistrictSection/DistrictProtocolLabel");

		if (StartButton != null)
			StartButton.Pressed += OnStartPressed;

		BuildDistrictButtons();
		// Check run-over BEFORE building roster — avoids rendering + buttons
		// that would need to be torn down immediately after.
		if (!CheckRunOver())
			RefreshRoster();
		BuildHuntPanel();
		RefreshDeckDisplay();
		SelectDistrict("the_stub");

		// Deck always starts empty — player picks manually each visit.
	}

	// ── District selection ────────────────────────────────────────────────────

	private void BuildDistrictButtons()
	{
		if (DistrictGrid == null) return;

		foreach (var child in DistrictGrid.GetChildren())
			child.QueueFree();

		var districts = DistrictDatabase.Instance.GetAllDistricts();
		foreach (var district in districts)
		{
			var btn = new Button();
			btn.Text              = district.Name;
			btn.Disabled          = district.IsLocked;
			btn.CustomMinimumSize = new Vector2(160, 40);

			var id = district.Id;
			btn.Pressed += () => SelectDistrict(id);
			DistrictGrid.AddChild(btn);
		}
	}

	private void SelectDistrict(string districtId)
	{
		_selectedDistrictId = districtId;
		var district = DistrictDatabase.Instance.GetDistrict(districtId);
		if (district == null) return;

		if (DistrictNameLabel    != null) DistrictNameLabel.Text  = district.Name;
		if (DistrictDescLabel    != null) DistrictDescLabel.Text  = district.Description;
		if (DistrictStakeLabel   != null) DistrictStakeLabel.Text = $"Stake: {district.Stake}";

		var protocols = new List<string>(district.Protocols);
		if (district.Intercept)    protocols.Add("Intercept");
		if (district.Conscription) protocols.Add("Conscription");
		if (district.Cascade)      protocols.Add("Cascade");
		if (district.Standoff)     protocols.Add("Standoff");

		if (DistrictProtocolLabel != null)
			DistrictProtocolLabel.Text = protocols.Count > 0
				? $"Rules: {string.Join(", ", protocols)}"
				: "Rules: Base capture only";

		GD.Print($"PreMatch: selected district '{district.Name}'.");
	}

	// ── Roster + deck builder ─────────────────────────────────────────────────

	private void RefreshRoster()
	{
		if (RosterGrid == null || GameSession.Instance == null) return;

		// Always reset to the standard column layout before populating.
		// CheckRunOver sets Columns=1 for its message; RefreshRoster must undo that.
		RosterGrid.Columns = 4;

		foreach (var child in RosterGrid.GetChildren())
			child.QueueFree();

		foreach (var card in GameSession.Instance.Roster)
		{
			// Wrap card + button in a VBoxContainer so the button sits below,
			// fully outside the CardNode — no mouse filter issues.
			var wrapper = new VBoxContainer();
			wrapper.CustomMinimumSize = new Vector2(120, 0);
			RosterGrid.AddChild(wrapper);

			var cardNode = _cardScene.Instantiate<CardNode>();
			wrapper.AddChild(cardNode);
			cardNode.Initialize(new CardInstance(card, ownerId: 1));
			cardNode.CustomMinimumSize = new Vector2(120, 160);

			var captured = card;
			if (_stepUpMode)
			{
				// Step-up mode: show projected post-promotion stats and a Promote button.
				// Heroes already in the roster can't be promoted again.
				if (card.Tier == Tier.Hero)
				{
					var alreadyLbl = new Label();
					alreadyLbl.Text              = "(current hero)";
					alreadyLbl.CustomMinimumSize = new Vector2(120, 30);
					wrapper.AddChild(alreadyLbl);
				}
				else
				{
					var (pt, pr, pb, pl) = StepUpPromoter.PreviewPromotion(card);
					var promoteBtn = new Button();
					promoteBtn.Text              = $"↑ {card.Name}\n→ {pt}/{pr}/{pb}/{pl}";
					promoteBtn.ClipText          = false;
					promoteBtn.CustomMinimumSize = new Vector2(120, 46);
					promoteBtn.Pressed += () => OnPromoteCardSelected(captured);
					wrapper.AddChild(promoteBtn);
				}
			}
			else
			{
				var btn = new Button();
				btn.Text              = $"+ {card.Name}";
				btn.ClipText          = true;
				btn.CustomMinimumSize = new Vector2(120, 30);
				btn.Pressed += () =>
				{
					AddToDeck(captured);
					RefreshDeckDisplay();
				};
				wrapper.AddChild(btn);
			}
		}
	}

	private void RefreshDeckDisplay()
	{
		if (DeckGrid == null) return;

		foreach (var child in DeckGrid.GetChildren())
			child.QueueFree();

		foreach (var card in _selectedDeck)
		{
			var wrapper = new VBoxContainer();
			wrapper.CustomMinimumSize = new Vector2(100, 0);
			DeckGrid.AddChild(wrapper);

			var cardNode = _cardScene.Instantiate<CardNode>();
			wrapper.AddChild(cardNode);
			cardNode.Initialize(new CardInstance(card, ownerId: 1));
			cardNode.CustomMinimumSize = new Vector2(100, 133);

			var captured = card;
			var btn = new Button();
			btn.Text              = "✕ Remove";
			btn.CustomMinimumSize = new Vector2(100, 36);
			btn.Pressed += () =>
			{
				RemoveFromDeck(captured);
				RefreshDeckDisplay();
			};
			wrapper.AddChild(btn);
		}

		if (DeckCountLabel != null)
			DeckCountLabel.Text = $"{_selectedDeck.Count} / {MaxDeckSize}";

		if (StartButton != null && !_isRunOver)
			StartButton.Disabled = _selectedDeck.Count != MaxDeckSize;
	}

	private void AddToDeck(CardData card)
	{
		if (_isRunOver) return;
		if (_selectedDeck.Count >= MaxDeckSize)
		{
			GD.Print("PreMatch: deck is full (5 cards).");
			return;
		}
		if (_selectedDeck.Contains(card))
		{
			GD.Print($"PreMatch: {card.Name} is already in the deck.");
			return;
		}
		if (card.Tier == Tier.Hero && _selectedDeck.Exists(c => c.Tier == Tier.Hero))
		{
			GD.Print("PreMatch: deck already has a hero.");
			return;
		}
		_selectedDeck.Add(card);
		GD.Print($"PreMatch: added {card.Name} to deck ({_selectedDeck.Count}/5).");
	}

	private void RemoveFromDeck(CardData card)
	{
		_selectedDeck.Remove(card);
		GD.Print($"PreMatch: removed {card.Name} from deck ({_selectedDeck.Count}/5).");
	}

	// ── Hunt panel ───────────────────────────────────────────────────────────
	// Shown when the player's hero has been captured (systems.md §7).
	// Injected as the first child of HSplit/Right so it sits above the roster.

	private void BuildHuntPanel()
	{
		var session = GameSession.Instance;
		if (session == null || !session.IsHeadless) return;

		var hero    = session.CapturedHero;
		var right   = GetNodeOrNull<VBoxContainer>("HSplit/Right");
		if (right == null) return;

		_huntPanel = new VBoxContainer();
		_huntPanel.CustomMinimumSize = new Vector2(0, 0);

		// ── Warning banner ────────────────────────────────────────────────────
		var banner = new PanelContainer();
		var bannerVBox = new VBoxContainer();
		banner.AddChild(bannerVBox);

		var titleLbl = new Label();
		titleLbl.Text = $"⚠  HEADLESS  —  {hero.Name} CAPTURED";
		titleLbl.AddThemeColorOverride("font_color", new Color("d94a4a"));
		bannerVBox.AddChild(titleLbl);

		var capturorLbl = new Label();
		capturorLbl.Text = $"Captor faction: {session.CapturingFaction}";
		bannerVBox.AddChild(capturorLbl);

		var attemptsLbl = new Label();
		attemptsLbl.Text = $"Reclaim window: {session.ReclamationAttemptsLeft} attempt(s) remaining";
		bannerVBox.AddChild(attemptsLbl);

		if (session.ReclamationAttemptsLeft == 0)
		{
			var closedLbl = new Label();
			closedLbl.Text = "Window closed — you must Step Up or start a New Run.";
			closedLbl.AddThemeColorOverride("font_color", new Color("ff8844"));
			bannerVBox.AddChild(closedLbl);
		}

		_huntPanel.AddChild(banner);

		// ── Action buttons ────────────────────────────────────────────────────
		var btnRow = new HBoxContainer();
		btnRow.AddThemeConstantOverride("separation", 8);

		// Reclaim button — only while attempts remain
		if (session.ReclamationAttemptsLeft > 0)
		{
			var reclaimBtn = new Button();
			reclaimBtn.Text              = $"⚔  Reclaim {hero.Name}";
			reclaimBtn.CustomMinimumSize = new Vector2(200, 40);
			reclaimBtn.TooltipText       = "Fight the rival crew to win your hero back (AsFlipped rules).";
			reclaimBtn.Pressed += OnHuntReclaimPressed;
			btnRow.AddChild(reclaimBtn);
		}

		// Buyout — disabled until scrip economy (Phase 9)
		if (session.CapturingFaction != Faction.HollowChoir)
		{
			var buyoutBtn = new Button();
			buyoutBtn.Text              = "💳  Buyout  (Phase 9)";
			buyoutBtn.CustomMinimumSize = new Vector2(180, 40);
			buyoutBtn.Disabled          = true;
			buyoutBtn.TooltipText       = "Pay scrip to ransom your hero — available in Phase 9.";
			btnRow.AddChild(buyoutBtn);
		}
		else
		{
			var noSellLbl = new Label();
			noSellLbl.Text = "The Choir do not sell.";
			noSellLbl.AddThemeColorOverride("font_color", new Color("8888cc"));
			btnRow.AddChild(noSellLbl);
		}

		// Step Up — enters card selection mode; label toggles to Cancel if already in mode
		_stepUpToggleBtn = new Button();
		_stepUpToggleBtn.Text              = _stepUpMode ? "✕  Cancel Step Up" : "↑  Step Up";
		_stepUpToggleBtn.CustomMinimumSize = new Vector2(160, 40);
		_stepUpToggleBtn.TooltipText       =
			"Choose a card from your roster to promote to Hero.";
		_stepUpToggleBtn.Pressed += OnStepUpTogglePressed;
		btnRow.AddChild(_stepUpToggleBtn);

		_huntPanel.AddChild(btnRow);

		// Separator
		var sep = new HSeparator();
		sep.CustomMinimumSize = new Vector2(0, 8);
		_huntPanel.AddChild(sep);

		// Insert at top of right column, before the roster section.
		// AddChild appends; then MoveChild repositions to index 0.
		right.AddChild(_huntPanel);
		right.MoveChild(_huntPanel, 0);
	}

	private void OnHuntReclaimPressed()
	{
		if (_selectedDeck.Count != MaxDeckSize)
		{
			GD.Print("PreMatch: Hunt — must select a full 5-card deck first.");
			return;
		}

		var session = GameSession.Instance;
		if (session == null || !session.IsHeadless) return;

		session.SelectedDeck       = new List<CardData>(_selectedDeck);
		session.SelectedDistrictId = _selectedDistrictId;
		session.IsHuntMatch        = true;
		session.ClearMatchResult();

		GD.Print($"PreMatch: launching Hunt match vs {session.CapturingFaction} " +
		         $"for {session.CapturedHero.Name}. Attempts left after this: " +
		         $"{session.ReclamationAttemptsLeft - 1}.");

		GetTree().ChangeSceneToFile("res://Scenes/Board/GameBoard.tscn");
	}

	private void OnStepUpTogglePressed()
	{
		_stepUpMode = !_stepUpMode;
		if (_stepUpToggleBtn != null)
			_stepUpToggleBtn.Text = _stepUpMode ? "✕  Cancel Step Up" : "↑  Step Up";
		RefreshRoster();
	}

	private void OnPromoteCardSelected(CardData card)
	{
		var session = GameSession.Instance;
		if (session == null) return;

		var promoted = session.StepUp(card);
		if (promoted == null)
		{
			GD.PrintErr("PreMatch: Step Up — promotion failed.");
			return;
		}

		GD.Print($"PreMatch: Step Up — {promoted.Name} is the new hero.");

		_stepUpMode      = false;
		_stepUpToggleBtn = null;
		if (_huntPanel != null) { _huntPanel.QueueFree(); _huntPanel = null; }

		if (!CheckRunOver())
			RefreshRoster();

		RefreshDeckDisplay();
	}

	// ── Run over check ──────────────────────────────────────────────────────────

	// Returns true if the run is over (roster too small to field a deck).
	private bool CheckRunOver()
	{
		var session = GameSession.Instance;
		if (session == null) return false;

		// While a Hunt is active the hero is captured but still yours — don't
		// declare the run over until the player has resolved the Hunt (Step Up
		// or Reclaim). After Step Up, OnStepUpPressed calls CheckRunOver again.
		if (session.IsHeadless) return false;

		if (session.Roster.Count >= MaxDeckSize) return false;

		// Not enough cards to field a full deck — run is over
		_isRunOver = true;
		GD.Print($"PreMatch: roster has {session.Roster.Count} cards — run over.");

		if (StartButton != null)
		{
			StartButton.Text     = "Run Over — New Run";
			StartButton.Pressed -= OnStartPressed;
			StartButton.Pressed += OnNewRun;
			StartButton.Disabled = false; // re-enable after rewiring to OnNewRun
		}

		if (DeckCountLabel != null)
			DeckCountLabel.Text = $"Only {session.Roster.Count} cards remain — crew is gone.";

		// Replace roster with a message — no + buttons in run-over state
		if (RosterGrid != null)
		{
			foreach (var child in RosterGrid.GetChildren())
				child.Free(); // Free immediately, not QueueFree, so + buttons can't fire
			RosterGrid.Columns = 1;
			var msg = new Label();
			msg.Text = $"Crew lost. {session.Roster.Count} card(s) remain — not enough to field a team.";
			msg.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			msg.CustomMinimumSize = new Vector2(380, 0);
			RosterGrid.AddChild(msg);
		}
		return true;
	}

	private void OnNewRun()
	{
		GD.Print("PreMatch: starting new run.");
		GameSession.Instance?.InitializeNewRun();
		GetTree().ReloadCurrentScene();
	}

	// ── Start match ───────────────────────────────────────────────────────────

	private void OnStartPressed()
	{
		if (_selectedDeck.Count != MaxDeckSize)
		{
			GD.Print("PreMatch: need exactly 5 cards to start.");
			return;
		}

		if (GameSession.Instance != null)
		{
			GameSession.Instance.SelectedDeck       = new List<CardData>(_selectedDeck);
			GameSession.Instance.SelectedDistrictId = _selectedDistrictId;
			GameSession.Instance.ClearMatchResult();
		}

		GD.Print($"PreMatch: starting match in '{_selectedDistrictId}'.");
		GetTree().ChangeSceneToFile("res://Scenes/Board/GameBoard.tscn");
	}
}