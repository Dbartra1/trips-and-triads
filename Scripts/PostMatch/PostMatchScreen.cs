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
	[Export] public Label         ScripEarnedLabel{ get; set; }  // optional — shows "+ X scrip"
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
			string resultText = session.WinnerText;

			// Annotate with Hunt outcome if this was a Hunt match
			if (session.HeroReclaimed)
				resultText += "\n✓  Hero reclaimed!";
			else if (session.IsHeadless && !session.HeroReclaimed)
			{
				int left = session.ReclamationAttemptsLeft;
				resultText += left > 0
					? $"\n✕  Hero still captured  —  {left} attempt(s) remaining"
					: "\n✕  Reclaim window closed  —  Step Up on the next screen";
			}

			ResultLabel.Text = resultText;
			var color = session.PlayerWon ? new Color("3ecdef") : new Color("fd1d75");
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

		// ── Street Cred ───────────────────────────────────────────────────────
		ApplyCredEvents(session);
		session.TickGracePeriods(); // start/tick grace periods based on new cred tier

		// Refresh free agents after a standard district match
		if (!session.IsDellaMatch)
			session.RefreshFreeAgents();

		// ── Scrip payout (Phase 9) ───────────────────────────────────────────
		if (session.IsDellaMatch)
		{
			// Della Standing Work: flat 10 scrip base, scaled by cred.
			// Contract was already consumed on match start.
			int payout = (int)(10f * CredEffects.IncomeMultiplier(session.Cred.Tier));
			if (session.PlayerWon)
			{
				session.AddScrip(payout);
				GD.Print($"Scrip payout (Della): +{payout} (total: {session.Scrip}).");
			}
			else
			{
				GD.Print("Scrip payout (Della): 0 (match lost).");
			}

			if (ScripEarnedLabel != null)
			{
				ScripEarnedLabel.Visible = true;
				if (session.PlayerWon && payout > 0)
				{
					ScripEarnedLabel.Text = $"+ {payout} scrip (Standing Work)";
					ScripEarnedLabel.AddThemeColorOverride("font_color", new Color("f0c040"));
				}
				else
				{
					ScripEarnedLabel.Text = "No payout (lost contract)";
					ScripEarnedLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f, 1f));
				}
			}
			// Reset flag for next match
			session.IsDellaMatch = false;
		}
		else
		{
			// Normal district match: apply standard payout AND refresh Della's board.
			ApplyScripPayout(session);
			session.RefreshDellaContracts();
		}

		// Apply to roster
		session.ApplyStakeResult();
	}

	private void ApplyScripPayout(GameSession session)
	{
		if (session == null) return;

		int payout = ScripPayoutCalculator.Calculate(
			session.SelectedDistrictId,
			session.PlayerWon,
			session.Cred.Tier);

		if (payout > 0)
		{
			session.AddScrip(payout);
			GD.Print($"Scrip payout: +{payout} (total: {session.Scrip}).");
		}

		// Update optional label (may not exist in scene — safe to skip)
		if (ScripEarnedLabel != null)
		{
			ScripEarnedLabel.Visible = true;
			if (payout > 0)
			{
				ScripEarnedLabel.Text = $"+ {payout} scrip";
				ScripEarnedLabel.AddThemeColorOverride("font_color", new Color("f0c040"));
			}
			else
			{
				ScripEarnedLabel.Text = "No payout";
				ScripEarnedLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.5f, 1f));
			}
		}
	}

	private void ApplyCredEvents(GameSession session)
	{
		if (session?.Cred == null) return;

		var events = new System.Collections.Generic.List<CredEvent>();

		// Base win/loss
		if (session.PlayerWon)
		{
			events.Add(CredEvent.WinMatch);

			// Dangerous district bonus
			var districtId = session.SelectedDistrictId;
			if (districtId == "the_hush" || districtId == "the_vault")
				events.Add(CredEvent.WinDangerousDistrict);

			// Razorkin district win
			var district = DistrictDatabase.Instance.GetDistrict(districtId);
			if (district?.Controller == "Razorkin")
				events.Add(CredEvent.WinVsRazorkin);

			// Hunt reclaim by duel (not buyout)
			if (session.HeroReclaimed)
				events.Add(CredEvent.HuntReclaimByDuel);
		}
		else
		{
			events.Add(CredEvent.LoseMatch);
		}

		int credBefore = session.Cred.Cred;
		CredTier tierBefore = session.Cred.Tier;

		session.Cred.ApplyEvents(events.ToArray());

		int credAfter = session.Cred.Cred;
		CredTier tierAfter = session.Cred.Tier;
		int delta = credAfter - credBefore;

		GD.Print($"Street Cred: {credBefore} → {credAfter} ({(delta >= 0 ? "+" : "")}{delta}) | Tier: {tierAfter}");

		if (tierAfter != tierBefore)
			GD.Print($"  ▶ Tier crossed: {tierBefore} → {tierAfter}");
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