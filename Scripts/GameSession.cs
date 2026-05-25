using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;

/// <summary>
/// Persistent autoload singleton — lives for the entire session.
/// Carries all campaign state between scenes:
/// roster, selected deck, active district, and match result.
///
/// Add to Project → Project Settings → Autoload as "GameSession".
/// </summary>
public partial class GameSession : Node
{
	private static GameSession _instance;
	public  static GameSession Instance => _instance;

	// ── Roster ───────────────────────────────────────────────────────────────
	/// <summary>All cards the player currently owns.</summary>
	public List<CardData> Roster { get; private set; } = new();

	// ── Pre-match selections ──────────────────────────────────────────────────
	/// <summary>The 5-card hand the player chose from their roster.</summary>
	public List<CardData> SelectedDeck { get; set; } = new();

	/// <summary>The district the player chose to duel in.</summary>
	public string SelectedDistrictId { get; set; } = "the_stub";

	// ── Match result (written by GameBoard, read by PostMatchScreen) ──────────
	public int  P1FinalScore   { get; set; }
	public int  P2FinalScore   { get; set; }
	public bool PlayerWon      { get; set; }
	public string WinnerText   { get; set; }

	/// <summary>Cards won by the player this match (stake resolution).</summary>
	public List<CardData> CardsWon  { get; set; } = new();

	/// <summary>Cards lost by the player this match (stake resolution).</summary>
	public List<CardData> CardsLost { get; set; } = new();

	// ── Session state ─────────────────────────────────────────────────────────
	public bool IsInitialized { get; private set; } = false;

	public override void _Ready()
	{
		_instance = this;

		if (!IsInitialized)
			InitializeNewRun();
	}

	/// <summary>
	/// Start a fresh run — generate a new procedural crew as the starting roster.
	/// Called once at session start, and again when the player Prestiges.
	/// </summary>
	public void InitializeNewRun()
	{
		CardDatabase.Instance.Load();
		DistrictDatabase.Instance.Load();
		DistrictManager.Instance.Initialize();

		// Generate starting crew and add all 7 to the roster
		var crew = CrewGenerator.Generate();
		Roster   = new List<CardData>(crew);

		// Deck starts empty — player picks manually on PreMatchScreen
		SelectedDeck = new List<CardData>();

		SelectedDistrictId = "the_stub";
		IsInitialized      = true;

		GD.Print($"GameSession: new run started. Roster: {Roster.Count} cards.");
		foreach (var c in Roster)
			GD.Print($"  [{c.Tier}] {c.Name} | {c.Top}/{c.Right}/{c.Bottom}/{c.Left}");
	}

	/// <summary>
	/// Apply stake resolution after a match.
	/// Adds CardsWon to roster, removes CardsLost from roster.
	/// </summary>
	public void ApplyStakeResult()
	{
		foreach (var card in CardsWon)
			if (!Roster.Contains(card))
				Roster.Add(card);

		foreach (var card in CardsLost)
			Roster.Remove(card);

		GD.Print($"GameSession: stake applied. Roster now {Roster.Count} cards.");
	}

	/// <summary>Clear match result data before starting a new match.</summary>
	public void ClearMatchResult()
	{
		CardsWon.Clear();
		CardsLost.Clear();
		P1FinalScore = P2FinalScore = 0;
		PlayerWon    = false;
		WinnerText   = "";
	}
}