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

	/// <summary>Interim hero promoted to lead the Reclaim attempt. Non-null after Step Up.</summary>
	public CardData InterimHero { get; private set; } = null;

	/// <summary>True once a Step Up has been performed for the current Hunt.</summary>
	public bool HasInterim => InterimHero != null;

	/// <summary>Original stats of the interim card, saved so we can restore them if the player changes their choice.</summary>
	private class SavedStats
	{
		public int Top, Right, Bottom, Left, Level;
		public Tier Tier;
		public DomainType Domain;
		public AbilityType Ability;
	}
	private SavedStats _interimSavedStats = null;

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
	/// Promote a card to Hero (Step Up).
	/// If <paramref name="specific"/> is provided, promotes that card (player chose it).
	/// Otherwise auto-picks the highest-stat eligible card from the current roster.
	/// Closes the Hunt window. Returns the promoted card, or null on failure.
	/// </summary>
	public CardData StepUp(CardData specific = null)
	{
		// If the player is changing their interim choice, restore the previous card first.
		if (InterimHero != null && _interimSavedStats != null && specific != InterimHero)
		{
			GD.Print($"GameSession: restoring previous interim {InterimHero.Name} before re-selecting.");
			InterimHero.Top         = _interimSavedStats.Top;
			InterimHero.Right       = _interimSavedStats.Right;
			InterimHero.Bottom      = _interimSavedStats.Bottom;
			InterimHero.Left        = _interimSavedStats.Left;
			InterimHero.Level       = _interimSavedStats.Level;
			InterimHero.Tier        = _interimSavedStats.Tier;
			InterimHero.DomainType  = _interimSavedStats.Domain;
			InterimHero.AbilityType = _interimSavedStats.Ability;
			InterimHero      = null;
			_interimSavedStats = null;
		}

		// If the same card is re-selected (no-op), just return it.
		if (InterimHero != null && specific == InterimHero)
			return InterimHero;

		// Resolve the target card.
		CardData target;
		if (specific != null)
		{
			target = specific;
		}
		else
		{
			// Auto-pick from snapshot deck (filtered to current roster), fall back to full roster.
			var candidates = DeckWhenHeroWasCaptured.Count > 0
				? DeckWhenHeroWasCaptured.FindAll(c => Roster.Contains(c) && c.Tier != Tier.Hero)
				: new System.Collections.Generic.List<CardData>();
			if (candidates.Count == 0)
				candidates = Roster.FindAll(c => c.Tier != Tier.Hero);
			target = candidates.Count > 0
				? candidates.OrderByDescending(c => c.Top + c.Right + c.Bottom + c.Left).First()
				: null;
		}
		if (target == null) { GD.PrintErr("GameSession: StepUp — no candidate found."); return null; }

		_interimSavedStats = new SavedStats
		{
			Top     = target.Top,   Right   = target.Right,
			Bottom  = target.Bottom, Left   = target.Left,
			Level   = target.Level,  Tier   = target.Tier,
			Domain  = target.DomainType,
			Ability = target.AbilityType,
		};

		if (target.Tier == Tier.Hero)
		{
			GD.PrintErr("GameSession: StepUp — target is already a hero.");
			_interimSavedStats = null;
			return null;
		}

		var promoted = StepUpPromoter.PromoteSpecific(target);

		if (promoted == null)
		{
			GD.PrintErr("GameSession: StepUp — promotion failed.");
			_interimSavedStats = null;
			return null;
		}

		if (!Roster.Contains(promoted)) Roster.Add(promoted);

		// Record as interim — the Hunt stays open so the player can still Reclaim.
		InterimHero = promoted;

		GD.Print($"GameSession: Step Up — {promoted.Name} promoted to interim Hero " +
		         $"| {promoted.Top}/{promoted.Right}/{promoted.Bottom}/{promoted.Left} " +
		         $"| Domain:{promoted.DomainType}");

		return promoted;
	}

	/// <summary>Clear all Hunt state (used by Reclaim, window-close, and new runs).</summary>
	public void ClearHunt()
	{
		CapturedHero            = null;
		CapturingFaction        = Faction.None;
		ReclamationAttemptsLeft = 0;
		DeckWhenHeroWasCaptured = new List<CardData>();
		IsHuntMatch             = false;
		HeroReclaimed           = false;
		InterimHero             = null;
		_interimSavedStats      = null;
	}
}