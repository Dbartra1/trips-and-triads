using Godot;
using System.Collections.Generic;
using TripsAndTriads.Core;
using TripsAndTriads.Rules;

/// <summary>
/// Persistent autoload singleton — lives for the entire session.
/// Carries all campaign state between scenes:
/// roster, selected deck, active district, match result, and Hunt state.
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
	public int    P1FinalScore { get; set; }
	public int    P2FinalScore { get; set; }
	public bool   PlayerWon   { get; set; }
	public string WinnerText  { get; set; }

	/// <summary>Cards won by the player this match (stake resolution).</summary>
	public List<CardData> CardsWon  { get; set; } = new();

	/// <summary>Cards lost by the player this match (stake resolution).</summary>
	public List<CardData> CardsLost { get; set; } = new();

	// ── Hunt state (systems.md §7) ────────────────────────────────────────────

	/// <summary>The player's hero that was captured. Non-null while Hunt is active.</summary>
	public CardData CapturedHero { get; private set; } = null;

	/// <summary>Faction of the crew that captured the hero (drives buyout pricing).</summary>
	public Faction CapturingFaction { get; private set; } = Faction.None;

	/// <summary>How many Reclaim duels the player has left (starts at 2, window closes at 0).</summary>
	public int ReclamationAttemptsLeft { get; private set; } = 0;

	/// <summary>Snapshot of the deck that was playing when the hero was captured (for Step Up candidates).</summary>
	public List<CardData> DeckWhenHeroWasCaptured { get; private set; } = new();

	/// <summary>True while a Hunt is open — player is Headless (no Domain, no bonds for the lost hero).</summary>
	public bool IsHeadless => CapturedHero != null;

	/// <summary>Set by PreMatchScreen before launching a Hunt match so GameBoard knows the rules.</summary>
	public bool IsHuntMatch { get; set; } = false;

	/// <summary>Written by GameBoard after a Hunt match; read by PostMatchScreen.</summary>
	public bool HeroReclaimed { get; set; } = false;

	// ── Session state ─────────────────────────────────────────────────────────
	public bool IsInitialized { get; private set; } = false;

	public override void _Ready()
	{
		_instance = this;
		if (!IsInitialized) InitializeNewRun();
	}

	// ── Run lifecycle ─────────────────────────────────────────────────────────

	/// <summary>
	/// Start a fresh run — generate a new procedural crew as the starting roster.
	/// Called once at session start, and again when the player starts a New Run.
	/// </summary>
	public void InitializeNewRun()
	{
		CardDatabase.Instance.Load();
		DistrictDatabase.Instance.Load();
		DistrictManager.Instance.Initialize();

		var crew = CrewGenerator.Generate();
		Roster   = new List<CardData>(crew);

		SelectedDeck       = new List<CardData>();
		SelectedDistrictId = "the_stub";
		IsInitialized      = true;

		ClearHunt();

		GD.Print($"GameSession: new run started. Roster: {Roster.Count} cards.");
		foreach (var c in Roster)
			GD.Print($"  [{c.Tier}] {c.Name} | {c.Top}/{c.Right}/{c.Bottom}/{c.Left}");
	}

	// ── Stake resolution ──────────────────────────────────────────────────────

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
		HeroReclaimed = false;
	}

	// ── Hunt system ───────────────────────────────────────────────────────────

	/// <summary>
	/// Called by GameBoard when the player's hero is captured.
	/// Opens the Reclaim window (2 attempts) and snaps the playing deck.
	/// </summary>
	public void SetCapturedHero(CardData hero, Faction capturingFaction)
	{
		CapturedHero              = hero;
		CapturingFaction          = capturingFaction;
		ReclamationAttemptsLeft   = 2;
		DeckWhenHeroWasCaptured   = new List<CardData>(SelectedDeck);
		HeroReclaimed             = false;

		// Remove hero from roster — it now belongs to the captor
		Roster.Remove(hero);

		GD.Print($"GameSession: hero captured — {hero.Name} ({capturingFaction}). " +
		         $"Hunt opens. 2 reclaim attempts. Roster: {Roster.Count}.");
	}

	/// <summary>
	/// Consume one reclaim attempt. Returns the number of attempts remaining.
	/// When it hits 0 the window is closed — player must Step Up.
	/// </summary>
	public int ConsumeReclaimAttempt()
	{
		if (ReclamationAttemptsLeft > 0) ReclamationAttemptsLeft--;
		GD.Print($"GameSession: reclaim attempt consumed. {ReclamationAttemptsLeft} remaining.");
		return ReclamationAttemptsLeft;
	}

	/// <summary>
	/// Called when the player wins a Hunt match — hero returns to roster.
	/// </summary>
	public void ReclaimHero()
	{
		if (CapturedHero == null) return;
		GD.Print($"GameSession: hero reclaimed — {CapturedHero.Name} returns to roster.");
		if (!Roster.Contains(CapturedHero))
			Roster.Add(CapturedHero);
		HeroReclaimed = true;
		ClearHunt();
	}

	/// <summary>
	/// Promote the best non-hero card from the capture deck to Hero (Step Up).
	/// Closes the Hunt window. Returns the promoted card, or null on failure.
	/// </summary>
	public CardData StepUp()
	{
		// Use the snapshot deck; fall back to current roster if snapshot is empty.
		var candidates = DeckWhenHeroWasCaptured.Count > 0
			? DeckWhenHeroWasCaptured
			: Roster;

		var promoted = StepUpPromoter.Promote(candidates);
		if (promoted == null)
		{
			GD.PrintErr("GameSession: StepUp — no eligible card found.");
			return null;
		}

		// Ensure promoted card is in roster (it should be already)
		if (!Roster.Contains(promoted)) Roster.Add(promoted);

		GD.Print($"GameSession: Step Up — {promoted.Name} promoted to Hero " +
		         $"| {promoted.Top}/{promoted.Right}/{promoted.Bottom}/{promoted.Left} " +
		         $"| Domain:{promoted.DomainType}");

		ClearHunt();
		return promoted;
	}

	/// <summary>Clear all Hunt state (used by Reclaim, StepUp, and new runs).</summary>
	public void ClearHunt()
	{
		CapturedHero            = null;
		CapturingFaction        = Faction.None;
		ReclamationAttemptsLeft = 0;
		DeckWhenHeroWasCaptured = new List<CardData>();
		IsHuntMatch             = false;
		HeroReclaimed           = false;
	}
}
