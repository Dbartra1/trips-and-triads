using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.UI;

/// <summary>
/// Post-match screen — shows match result, stake resolution, and cards won/lost.
/// Reads from GameSession and applies stake before displaying.
/// </summary>
public partial class PostMatchScreen : Control
{
	[Export] public Label         ResultLabel     { get; set; }
	[Export] public Label         ScoreLabel      { get; set; }
	[Export] public Label         StakeLabel      { get; set; }
	[Export] public GridContainer CardsWonGrid    { get; set; }
	[Export] public GridContainer CardsLostGrid   { get; set; }
	[Export] public Label         CardsWonLabel   { get; set; }
	[Export] public Label         CardsLostLabel  { get; set; }
	[Export] public Button        ContinueButton  { get; set; }

	private PackedScene _cardScene;

	public override void _Ready()
	{
		_cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");

		if (ContinueButton != null)
			ContinueButton.Pressed += () =>
				GetTree().ChangeSceneToFile("res://Scenes/PreMatch/PreMatchScreen.tscn");

		PopulateResults();
	}

	private void PopulateResults()
	{
		var session = GameSession.Instance;
		if (session == null) return;

		// Result header
		if (ResultLabel != null)
		{
			ResultLabel.Text = session.WinnerText;
			var color = session.PlayerWon ? new Color("4a90d9") : new Color("d94a4a");
			ResultLabel.AddThemeColorOverride("font_color", color);
		}

		if (ScoreLabel != null)
			ScoreLabel.Text = $"P1: {session.P1FinalScore}   |   P2: {session.P2FinalScore}";

		// Stake info
		var district = DistrictDatabase.Instance.GetDistrict(session.SelectedDistrictId);
		if (StakeLabel != null)
			StakeLabel.Text = $"Stake: {district?.Stake ?? "OneJob"}";

		// Cards won
		if (CardsWonLabel != null)
			CardsWonLabel.Text = session.CardsWon.Count > 0
				? $"Cards won ({session.CardsWon.Count}):"
				: "No cards won.";

		PopulateCardGrid(CardsWonGrid, session.CardsWon);

		// Cards lost
		if (CardsLostLabel != null)
			CardsLostLabel.Text = session.CardsLost.Count > 0
				? $"Cards lost ({session.CardsLost.Count}):"
				: "No cards lost.";

		PopulateCardGrid(CardsLostGrid, session.CardsLost);

		// Apply to roster
		session.ApplyStakeResult();
	}

	private void PopulateCardGrid(GridContainer grid, List<CardData> cards)
	{
		if (grid == null) return;

		foreach (var child in grid.GetChildren())
			child.QueueFree();

		foreach (var card in cards)
		{
			var cardNode = _cardScene.Instantiate<CardNode>();
			grid.AddChild(cardNode);
			var instance = new CardInstance(card, ownerId: 1);
			cardNode.Initialize(instance);
			cardNode.CustomMinimumSize = new Vector2(100, 133);
		}
	}
}
