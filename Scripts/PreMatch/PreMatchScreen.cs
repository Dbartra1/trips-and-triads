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
		RefreshRoster();
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

		if (StartButton != null)
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

	// ── Run over check ──────────────────────────────────────────────────────────

	private void CheckRunOver()
	{
		var session = GameSession.Instance;
		if (session == null) return;

		if (session.Roster.Count >= MaxDeckSize) return;

		// Not enough cards to field a full deck — run is over
		_isRunOver = true;
		GD.Print($"PreMatch: roster has {session.Roster.Count} cards — run over.");

		if (StartButton != null)
		{
			StartButton.Disabled = true;
			StartButton.Text     = "Run Over — New Run";
			StartButton.Pressed -= OnStartPressed;
			StartButton.Pressed += OnNewRun;
		}

		if (DeckCountLabel != null)
			DeckCountLabel.Text = $"Only {session.Roster.Count} cards remain — crew is gone.";

		// Replace roster with a message — no + buttons in run-over state
		if (RosterGrid != null)
		{
			foreach (var child in RosterGrid.GetChildren())
				child.QueueFree();
			var msg = new Label();
			msg.Text = $"Crew lost. {session.Roster.Count} card(s) remain — not enough to field a team.";
			msg.AutowrapMode = TextServer.AutowrapMode.WordSmart;
			RosterGrid.AddChild(msg);
		}
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