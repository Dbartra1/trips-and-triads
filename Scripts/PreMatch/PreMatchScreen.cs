using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;
using TripsAndTriads.UI;

/// <summary>
/// Pre-match screen — district selection, roster display, deck builder.
/// Player picks a district and their 5-card hand, then starts the match.
/// </summary>
public partial class PreMatchScreen : Control
{
	[Export] public GridContainer  DistrictGrid    { get; set; }
	[Export] public GridContainer  RosterGrid      { get; set; }
	[Export] public GridContainer  DeckGrid        { get; set; }
	[Export] public Label          DeckCountLabel  { get; set; }
	[Export] public Button         StartButton     { get; set; }
	[Export] public Label          DistrictNameLabel    { get; set; }
	[Export] public Label          DistrictDescLabel    { get; set; }
	[Export] public Label          DistrictStakeLabel   { get; set; }
	[Export] public Label          DistrictProtocolLabel{ get; set; }

	private PackedScene _cardScene;
	private string _selectedDistrictId = "the_stub";
	private List<CardData> _selectedDeck = new();
	private const int MaxDeckSize = 5;

	public override void _Ready()
	{
		_cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");

		if (StartButton != null)
			StartButton.Pressed += OnStartPressed;

		BuildDistrictButtons();
		RefreshRoster();
		RefreshDeckDisplay();
		SelectDistrict("the_stub");

		// Pre-populate deck with whatever GameSession has selected
		if (GameSession.Instance != null)
			_selectedDeck = new List<CardData>(GameSession.Instance.SelectedDeck);
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
			btn.Text         = district.Name;
			btn.Disabled     = district.IsLocked;
			btn.CustomMinimumSize = new Vector2(160, 40);

			var id = district.Id; // capture for lambda
			btn.Pressed += () => SelectDistrict(id);

			DistrictGrid.AddChild(btn);
		}
	}

	private void SelectDistrict(string districtId)
	{
		_selectedDistrictId = districtId;
		var district = DistrictDatabase.Instance.GetDistrict(districtId);
		if (district == null) return;

		if (DistrictNameLabel    != null) DistrictNameLabel.Text     = district.Name;
		if (DistrictDescLabel    != null) DistrictDescLabel.Text     = district.Description;
		if (DistrictStakeLabel   != null) DistrictStakeLabel.Text    = $"Stake: {district.Stake}";

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
			var cardNode = _cardScene.Instantiate<CardNode>();
			RosterGrid.AddChild(cardNode);
			var instance = new CardInstance(card, ownerId: 1);
			cardNode.Initialize(instance);
			cardNode.CustomMinimumSize = new Vector2(120, 160);

			// Clicking a roster card adds it to the deck
			var captured = card;
			var btn = new Button();
			btn.Text         = "+";
			btn.AnchorLeft   = 1; btn.AnchorRight  = 1;
			btn.AnchorTop    = 0; btn.AnchorBottom = 0;
			btn.OffsetLeft   = -28; btn.OffsetRight = 0;
			btn.OffsetTop    = 0;   btn.OffsetBottom = 20;
			btn.Pressed += () => AddToDeck(captured);
			cardNode.AddChild(btn);
		}
	}

	private void RefreshDeckDisplay()
	{
		if (DeckGrid == null) return;

		foreach (var child in DeckGrid.GetChildren())
			child.QueueFree();

		foreach (var card in _selectedDeck)
		{
			var cardNode = _cardScene.Instantiate<CardNode>();
			DeckGrid.AddChild(cardNode);
			var instance = new CardInstance(card, ownerId: 1);
			cardNode.Initialize(instance);
			cardNode.CustomMinimumSize = new Vector2(100, 133);

			// Clicking a deck card removes it
			var captured = card;
			var btn = new Button();
			btn.Text         = "✕";
			btn.AnchorLeft   = 1; btn.AnchorRight  = 1;
			btn.AnchorTop    = 0; btn.AnchorBottom = 0;
			btn.OffsetLeft   = -28; btn.OffsetRight = 0;
			btn.OffsetTop    = 0;   btn.OffsetBottom = 20;
			btn.Pressed += () => RemoveFromDeck(captured);
			cardNode.AddChild(btn);
		}

		if (DeckCountLabel != null)
			DeckCountLabel.Text = $"{_selectedDeck.Count} / {MaxDeckSize}";

		if (StartButton != null)
			StartButton.Disabled = _selectedDeck.Count != MaxDeckSize;
	}

	private void AddToDeck(CardData card)
	{
		if (_selectedDeck.Count >= MaxDeckSize) return;

		// At most one hero
		if (card.Tier == Tier.Hero && _selectedDeck.Exists(c => c.Tier == Tier.Hero))
		{
			GD.Print("PreMatch: deck already has a hero.");
			return;
		}

		_selectedDeck.Add(card);
		RefreshDeckDisplay();
	}

	private void RemoveFromDeck(CardData card)
	{
		_selectedDeck.Remove(card);
		RefreshDeckDisplay();
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
			GameSession.Instance.SelectedDeck      = new List<CardData>(_selectedDeck);
			GameSession.Instance.SelectedDistrictId = _selectedDistrictId;
			GameSession.Instance.ClearMatchResult();
		}

		GD.Print($"PreMatch: starting match in '{_selectedDistrictId}' with {_selectedDeck.Count} cards.");
		GetTree().ChangeSceneToFile("res://Scenes/Board/GameBoard.tscn");
	}
}
