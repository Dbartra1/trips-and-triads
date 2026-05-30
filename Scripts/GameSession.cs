using Godot;
using System.Collections.Generic;
using System.Linq;
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

	// ── Reunion state (set after a successful Reclaim) ────────────────────────
	/// <summary>True when both original and interim heroes are in the roster awaiting a "Who leads?" decision.</summary>
	public bool ReunionPending { get; private set; } = false;
	/// <summary>The hero who was captured and just returned.</summary>
	public CardData ReunionOriginal { get; private set; } = null;
	/// <summary>The interim hero who led the Reclaim.</summary>
	public CardData ReunionInterim { get; private set; } = null;

	// ── Session state ─────────────────────────────────────────────────────────
	public bool IsInitialized { get; private set; } = false;

	// ── Scrip ─────────────────────────────────────────────────────────────────
	/// <summary>
	/// The crew's current scrip balance. Earned from contract payouts (§9),
	/// spent on buyouts (§7.4) and, later, free-agent signings and Payroll (§11).
	/// Floored at zero — cannot go negative.
	/// </summary>
	public int Scrip { get; private set; } = 0;

	/// <summary>Add scrip (amount may be negative to subtract).</summary>
	public void AddScrip(int amount)
	{
		Scrip = System.Math.Max(0, Scrip + amount);
		GD.Print($"GameSession: scrip {(amount >= 0 ? "+" : "")}{amount} → {Scrip}.");
	}

	/// <summary>
	/// Deduct scrip. Returns true and deducts if the balance is sufficient;
	/// returns false and does not deduct if the crew can't afford it.
	/// </summary>
	public bool SpendScrip(int amount)
	{
		if (Scrip < amount)
		{
			GD.PrintErr($"GameSession: SpendScrip({amount}) failed — only {Scrip} available.");
			return false;
		}
		Scrip -= amount;
		GD.Print($"GameSession: spent {amount} scrip → {Scrip} remaining.");
		return true;
	}

	/// <summary>Called exclusively by SaveManager.</summary>
	public void LoadScrip(int scrip) => Scrip = System.Math.Max(0, scrip);

	// ── Street Cred ───────────────────────────────────────────────────────────
	/// <summary>
	/// The crew's Street Cred. Persists across matches within a run.
	/// Fires events via Cred.ApplyEvents() in PostMatchScreen.
	/// Resets to Known floor (20) on Step Up.
	/// </summary>
	public CredManager Cred { get; private set; } = new CredManager();

	// ── District access & grace periods ──────────────────────────────────────
	/// <summary>
	/// Matches remaining in each district's grace period before it locks.
	/// Key = districtId. Only present while a grace period is active (> 0).
	/// Decremented by TickGracePeriods() in PostMatchScreen after each match.
	/// </summary>
	public Dictionary<string, int> DistrictGracePeriods { get; private set; } = new();

	/// <summary>
	/// Districts that newly entered grace period this match — read and cleared
	/// by PreMatchScreen to show the warning popup exactly once per event.
	/// </summary>
	public List<string> NewGracePeriodAlerts { get; private set; } = new();

	/// <summary>
	/// True if the player can currently enter the given district.
	/// Combines cred tier requirement with active grace periods.
	/// </summary>
	public bool IsDistrictAccessible(string districtId)
	{
		int graceLeft = DistrictGracePeriods.TryGetValue(districtId, out var g) ? g : 0;
		return DistrictAccess.IsAccessible(districtId, Cred.Tier, graceLeft);
	}

	public int GetGraceMatchesRemaining(string districtId)
		=> DistrictGracePeriods.TryGetValue(districtId, out var g) ? g : 0;

	/// <summary>
	/// Called by PostMatchScreen after cred events are applied.
	/// Starts grace periods for newly-below districts, decrements existing ones,
	/// and populates NewGracePeriodAlerts for PreMatchScreen to display.
	/// </summary>
	public void TickGracePeriods()
	{
		NewGracePeriodAlerts.Clear();
		foreach (var districtId in DistrictAccess.HardLockIds())
		{
			bool meets = DistrictAccess.MeetsTierRequirement(districtId, Cred.Tier);
			if (meets)
			{
				// Back above threshold — clear grace state entirely.
				// Next time cred drops, ContainsKey = false → fresh grace period starts.
				DistrictGracePeriods.Remove(districtId);
			}
			else if (!DistrictGracePeriods.ContainsKey(districtId))
			{
				// Truly newly dropped below threshold (was accessible, now isn't).
				DistrictGracePeriods[districtId] = DistrictAccess.GraceMatches;
				NewGracePeriodAlerts.Add(districtId);
				GD.Print($"Grace period started: {districtId} — {DistrictAccess.GraceMatches} matches remaining.");
			}
			else if (DistrictGracePeriods[districtId] == 0)
			{
				// Pre-marked at run start as never-accessible — do nothing.
				// No warning, no countdown. Just stay locked.
			}
			else
			{
				// Active grace period — decrement countdown.
				DistrictGracePeriods[districtId] = System.Math.Max(0, DistrictGracePeriods[districtId] - 1);
				GD.Print($"Grace period tick: {districtId} — {DistrictGracePeriods[districtId]} matches remaining.");
			}
		}
	}

	/// <summary>Clear grace period alerts after PreMatchScreen has displayed them.</summary>
	public void ClearGracePeriodAlerts() => NewGracePeriodAlerts.Clear();

	// ── Standoff rematch state ────────────────────────────────────────────────
	/// <summary>
	/// When a Standoff triggers, GameBoard saves the board-state hands here
	/// before reloading the scene. _Ready picks these up on the rematch and
	/// uses them instead of drawing from the roster/deck again.
	/// Cleared after use.
	/// </summary>
	public List<CardData> StandoffP1Hand { get; private set; } = null;
	public List<CardData> StandoffP2Hand { get; private set; } = null;
	public bool           HasStandoffHands => StandoffP1Hand != null;

	public void SetStandoffHands(List<CardData> p1, List<CardData> p2)
	{
		StandoffP1Hand = new List<CardData>(p1);
		StandoffP2Hand = new List<CardData>(p2);
	}

	public void ClearStandoffHands()
	{
		StandoffP1Hand = null;
		StandoffP2Hand = null;
	}

	public override void _Ready()
	{
		_instance = this;
		if (!IsInitialized)
		{
			// Databases must load before SaveManager can deserialize card IDs.
			// DistrictManager is initialized inside both LoadGame and InitializeNewRun.
			CardDatabase.Instance.Load();
			DistrictDatabase.Instance.Load();

			if (!SaveManager.LoadGame())
				InitializeNewRun();
		}
	}

	// ── Save / Load support ──────────────────────────────────────────────────
	// Called exclusively by SaveManager — do not call from game logic.

	public void LoadRoster(List<CardData> roster)
	{
		Roster = roster;
		IsInitialized = true;
	}

	public void LoadHuntState(CardData captured, Faction faction, int attempts)
	{
		CapturedHero            = captured;
		CapturingFaction        = faction;
		ReclamationAttemptsLeft = attempts;
	}

	public void LoadInterimHero(CardData interim) => InterimHero = interim;

	public void LoadDeckSnapshot(List<CardData> snap) => DeckWhenHeroWasCaptured = snap;

	public void LoadReunionState(CardData original, CardData interim)
	{
		ReunionPending  = true;
		ReunionOriginal = original;
		ReunionInterim  = interim;
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
		Scrip              = 0;
		Cred               = new CredManager(); // fresh run starts at Nameless
		DistrictGracePeriods.Clear();
		NewGracePeriodAlerts.Clear();

		// Pre-mark every hard-lock district the crew can't reach at run start.
		// TickGracePeriods uses ContainsKey to distinguish "newly dropped below
		// threshold" (not in dict → grace starts) from "was always below"
		// (in dict with value 0 → no grace warning). Without this, the first
		// match loss would incorrectly trigger grace periods for districts the
		// player has never had access to.
		foreach (var districtId in DistrictAccess.HardLockIds())
		{
			if (!DistrictAccess.MeetsTierRequirement(districtId, Cred.Tier))
				DistrictGracePeriods[districtId] = 0;
		}

		ClearHunt();

		// Delete any existing save — this is a fresh run
		SaveManager.DeleteSave();

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

		// Persist after every stake resolution so progress is never lost
		SaveManager.SaveGame();
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

		// If there's an interim, hold both heroes for the Reunion decision.
		// ClearHunt is called after the player resolves Who Leads.
		if (InterimHero != null)
		{
			ReunionPending  = true;
			ReunionOriginal = CapturedHero;
			ReunionInterim  = InterimHero;
			// Partial clear — Hunt window closes, but Reunion stays open
			CapturedHero            = null;
			CapturingFaction        = Faction.None;
			ReclamationAttemptsLeft = 0;
			DeckWhenHeroWasCaptured = new List<CardData>();
			IsHuntMatch             = false;
		}
		else
		{
			ClearHunt();
		}
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

		// If the target is already a hero (captured from AI, won from previous matches),
		// just designate them as interim — no stat mutation needed.
		// _interimSavedStats = null signals "no restoration needed on step-down or Change".
		if (target.Tier == Tier.Hero)
		{
			InterimHero        = target;
			_interimSavedStats = null;
			GD.Print($"GameSession: Step Up — {target.Name} designated as interim (already hero) " +
			         $"| {target.Top}/{target.Right}/{target.Bottom}/{target.Left}");
			return target;
		}

		_interimSavedStats = new SavedStats
		{
			Top     = target.Top,   Right   = target.Right,
			Bottom  = target.Bottom, Left   = target.Left,
			Level   = target.Level,  Tier   = target.Tier,
			Domain  = target.DomainType,
			Ability = target.AbilityType,
		};

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

		// Cred is preserved through succession — the surviving crew carries the
		// old reputation forward unchanged. (Spec flavor "not from nothing, but
		// from a rumor" describes the lore, not a cred floor reset.)
		GD.Print($"GameSession: Step Up — cred preserved at {Cred.Cred} ({Cred.Tier}).");

		GD.Print($"GameSession: Step Up — {promoted.Name} promoted to interim Hero " +
		         $"| {promoted.Top}/{promoted.Right}/{promoted.Bottom}/{promoted.Left} " +
		         $"| Domain:{promoted.DomainType}");

		return promoted;
	}

	/// <summary>
	/// Resolve the post-Reclaim "Who leads?" decision.
	/// keepOriginal=true: original hero leads, interim steps down with a stat boost.
	/// keepOriginal=false: interim hero leads, original steps down with a stat boost.
	/// </summary>
	public void ResolveReunion(bool keepOriginal)
	{
		if (!ReunionPending) return;

		var leader   = keepOriginal ? ReunionOriginal : ReunionInterim;
		var stepDown = keepOriginal ? ReunionInterim  : ReunionOriginal;

		GD.Print($"GameSession: Reunion resolved — {leader.Name} leads. " +
		         $"{stepDown.Name} steps down.");

		// Step-down logic depends on whether the interim was a promoted non-hero
		// (_interimSavedStats != null) or an existing hero (_interimSavedStats == null).
		if (keepOriginal)
		{
			if (_interimSavedStats != null)
			{
				// Interim was a promoted non-hero — restore original stats
				stepDown.Top         = _interimSavedStats.Top;
				stepDown.Right       = _interimSavedStats.Right;
				stepDown.Bottom      = _interimSavedStats.Bottom;
				stepDown.Left        = _interimSavedStats.Left;
				stepDown.Level       = _interimSavedStats.Level;
				stepDown.Tier        = _interimSavedStats.Tier;
				stepDown.DomainType  = _interimSavedStats.Domain;
				stepDown.AbilityType = _interimSavedStats.Ability;
			}
			else
			{
				// Interim was already a hero — demote to TopTier
				stepDown.Tier       = Tier.TopTier;
				stepDown.DomainType = DomainType.None;
			}
		}
		else
		{
			// Original steps down — always demote to TopTier
			stepDown.Tier       = Tier.TopTier;
			stepDown.DomainType = DomainType.None;
		}

		// Recognition boost: +2 on highest non-A edge of the stepping-down card
		ApplyRecognitionBoost(stepDown);

		GD.Print($"GameSession: {stepDown.Name} steps down → " +
		         $"{stepDown.Top}/{stepDown.Right}/{stepDown.Bottom}/{stepDown.Left} [{stepDown.Tier}]");

		ReunionPending  = false;
		ReunionOriginal = null;
		ReunionInterim  = null;
		_interimSavedStats = null;
		InterimHero     = null;
	}

	private static void ApplyRecognitionBoost(CardData card)
	{
		// Find highest edge that isn't already 10, boost it by 2 (capped at 9)
		int[] edges = { card.Top, card.Right, card.Bottom, card.Left };
		int bestIdx = -1;
		int bestVal = -1;
		for (int i = 0; i < 4; i++)
		{
			if (edges[i] < 10 && edges[i] > bestVal) { bestVal = edges[i]; bestIdx = i; }
		}
		if (bestIdx < 0) return;
		edges[bestIdx] = System.Math.Min(edges[bestIdx] + 2, 9);
		card.Top = edges[0]; card.Right = edges[1];
		card.Bottom = edges[2]; card.Left = edges[3];
	}
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
		ReunionPending          = false;
		ReunionOriginal         = null;
		ReunionInterim          = null;
	}
}