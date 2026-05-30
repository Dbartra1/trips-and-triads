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
	private VBoxContainer _huntPanel = null;
	private bool _stepUpMode = false;
	private Button _stepUpToggleBtn = null;
	private Button _reclaimBtn = null;
	private CredBarNode _credBar = null; // Street Cred signal meter
	private Label _scripLabel = null;   // scrip balance display

	public override void _Ready()
	{
		_cardScene = GD.Load<PackedScene>("res://Scenes/Card/CardNode.tscn");

		// Resolve node refs by path as fallback
		DistrictGrid          ??= GetNodeOrNull<GridContainer>("Margin/HSplit/Left/DistrictSection/DistrictGrid");
		RosterGrid            ??= GetNodeOrNull<GridContainer>("Margin/HSplit/Right/RosterSection/RosterScroll/RosterGrid");
		DeckGrid              ??= GetNodeOrNull<GridContainer>("Margin/HSplit/Left/DeckSection/DeckGrid");
		DeckCountLabel        ??= GetNodeOrNull<Label>("Margin/HSplit/Left/DeckSection/DeckCountLabel");
		StartButton           ??= GetNodeOrNull<Button>("Margin/HSplit/Left/StartButton");
		DistrictNameLabel     ??= GetNodeOrNull<Label>("Margin/HSplit/Left/DistrictSection/DistrictNameLabel");
		DistrictDescLabel     ??= GetNodeOrNull<Label>("Margin/HSplit/Left/DistrictSection/DistrictDescLabel");
		DistrictStakeLabel    ??= GetNodeOrNull<Label>("Margin/HSplit/Left/DistrictSection/DistrictStakeLabel");
		DistrictProtocolLabel ??= GetNodeOrNull<Label>("Margin/HSplit/Left/DistrictSection/DistrictProtocolLabel");

		if (StartButton != null)
			StartButton.Pressed += OnStartPressed;

		BuildDistrictButtons();
		if (!CheckRunOver())
			RefreshRoster();
		BuildHuntPanel();
		BuildReunionBanner();
		BuildCredBar();
		BuildScripLabel();
		RefreshDeckDisplay();
		SelectDistrict("the_stub");

		// Deck always starts empty — player picks manually each visit.
	}

	// ── District selection ────────────────────────────────────────────────────

	private void BuildCredBar()
	{
		var session = GameSession.Instance;
		if (session == null) return;

		// Inject into the Left column, between the district section and deck section.
		var left = GetNodeOrNull<VBoxContainer>("Margin/HSplit/Left");
		if (left == null) return;

		_credBar = new CredBarNode();
		_credBar.CustomMinimumSize = new Vector2(0, 90);

		// Place it between DistrictSection (index 0) and DeckSection (index 1)
		left.AddChild(_credBar);
		left.MoveChild(_credBar, 1);

		_credBar.Refresh(session.Cred);
	}

	private void BuildScripLabel()
	{
		var session = GameSession.Instance;
		if (session == null) return;

		var left = GetNodeOrNull<VBoxContainer>("Margin/HSplit/Left");
		if (left == null) return;

		_scripLabel = new Label();
		_scripLabel.CustomMinimumSize = new Vector2(0, 28);
		_scripLabel.AddThemeColorOverride("font_color", new Color("f0c040")); // gold

		left.AddChild(_scripLabel);
		// Place right after the CredBarNode (index 2 = after District=0, Cred=1)
		left.MoveChild(_scripLabel, 2);

		RefreshScripLabel();
	}

	private void RefreshScripLabel()
	{
		if (_scripLabel == null) return;
		var session = GameSession.Instance;
		int scrip = session?.Scrip ?? 0;
		_scripLabel.Text = $"💵  Scrip:  {scrip}";
	}

	private void BuildDistrictButtons()
	{
		if (DistrictGrid == null) return;

		foreach (var child in DistrictGrid.GetChildren())
			child.QueueFree();

		var session   = GameSession.Instance;
		var districts = DistrictDatabase.Instance.GetAllDistricts();

		foreach (var district in districts)
		{
			bool accessible = session?.IsDistrictAccessible(district.Id) ?? true;
			int  graceLeft  = session?.GetGraceMatchesRemaining(district.Id) ?? 0;
			bool inGrace    = graceLeft > 0;
			var  gate       = DistrictAccess.GetGate(district.Id);

			var btn = new Button();
			btn.CustomMinimumSize = new Vector2(160, 44);

			if (!accessible && !inGrace)
			{
				// Hard locked — greyed, lock icon, full reason in tooltip
				btn.Text        = $"\U0001F512  {district.Name}";
				btn.Disabled    = true;
				btn.TooltipText = $"Requires {gate.MinTier} reputation.\n\n{gate.LockReason}";
				btn.Modulate    = new Color(0.55f, 0.55f, 0.6f, 1f);
			}
			else if (inGrace)
			{
				// Grace period — amber tint, countdown in button text, reason in tooltip
				btn.Text        = $"\u26A0  {district.Name}  ({graceLeft})";
				btn.Modulate    = new Color(1.0f, 0.72f, 0.28f, 1f);
				btn.TooltipText = $"Losing access in {graceLeft} match{(graceLeft == 1 ? "" : "es")}.\n\n{gate.LockReason}";
				var id = district.Id;
				btn.Pressed += () => SelectDistrict(id);
			}
			else
			{
				// Normal access
				btn.Text     = district.Name;
				btn.Disabled = district.IsLocked;
				if (!district.IsLocked)
				{
					var id = district.Id;
					btn.Pressed += () => SelectDistrict(id);
				}
			}

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

			// Dim cards already in the deck so the player can see at a glance
			// what's selected. Full opacity when removed.
			bool alreadySelected = _selectedDeck.Contains(card);
			cardNode.Modulate = alreadySelected
				? new Color(1f, 1f, 1f, 0.35f)
				: Colors.White;

			var cardRef = card; // closure-safe copy
			if (_stepUpMode)
			{
				var session      = GameSession.Instance;
				var capturedHero = session?.CapturedHero;

				if (card.Tier == Tier.Hero && card == capturedHero)
				{
					// This is the captured hero — can't use it as interim
					var lbl = new Label();
					lbl.Text              = "(captured)";
					lbl.CustomMinimumSize = new Vector2(120, 30);
					wrapper.AddChild(lbl);
				}
				else if (card.Tier == Tier.Hero)
				{
					// Existing hero in roster — selectable as interim directly, no stat change
					var promoteBtn = new Button();
					promoteBtn.Text              = $"↑ {card.Name}\n(hero)";
					promoteBtn.ClipText          = false;
					promoteBtn.CustomMinimumSize = new Vector2(120, 46);
					promoteBtn.Pressed += () => OnPromoteCardSelected(cardRef);
					wrapper.AddChild(promoteBtn);
				}
				else
				{
					// Non-hero — show projected post-promotion stats
					var (pt, pr, pb, pl) = StepUpPromoter.PreviewPromotion(card);
					var promoteBtn = new Button();
					promoteBtn.Text              = $"↑ {card.Name}\n→ {pt}/{pr}/{pb}/{pl}";
					promoteBtn.ClipText          = false;
					promoteBtn.CustomMinimumSize = new Vector2(120, 46);
					promoteBtn.Pressed += () => OnPromoteCardSelected(cardRef);
					wrapper.AddChild(promoteBtn);
				}
			}
			else
			{
				var btn = new Button();
				btn.Text              = alreadySelected ? $"✓ {card.Name}" : $"+ {card.Name}";
				btn.ClipText          = true;
				btn.Disabled          = alreadySelected;
				btn.CustomMinimumSize = new Vector2(120, 30);
				btn.Pressed += () =>
				{
					AddToDeck(cardRef);
					RefreshDeckDisplay();
					RefreshRoster();
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

		// Enforce Hero → Pro → Street display order
		var ordered = new List<CardData>(_selectedDeck);
		ordered.Sort((a, b) =>
		{
			int Rank(Tier t) => t switch { Tier.Hero => 0, Tier.Pro => 1, _ => 2 };
			return Rank(a.Tier).CompareTo(Rank(b.Tier));
		});

		foreach (var card in ordered)
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
				RefreshRoster(); // re-brighten the card in the roster
			};
			wrapper.AddChild(btn);
		}

		if (DeckCountLabel != null)
			DeckCountLabel.Text = $"{_selectedDeck.Count} / {MaxDeckSize}";

		if (StartButton != null && !_isRunOver)
			StartButton.Disabled = _selectedDeck.Count != MaxDeckSize;

		// Keep Reclaim enabled only when a full deck is selected
		if (_reclaimBtn != null)
			_reclaimBtn.Disabled = (_selectedDeck.Count != MaxDeckSize);

		// Keep scrip display current
		RefreshScripLabel();
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

	private VBoxContainer _reunionPanel = null; // shown after a successful Reclaim

	// ── Reunion banner ────────────────────────────────────────────────────────
	// Shown when both original and interim hero are in the roster simultaneously.

	private void BuildReunionBanner()
	{
		var session = GameSession.Instance;
		if (session == null || !session.ReunionPending) return;

		var right = GetNodeOrNull<VBoxContainer>("Margin/HSplit/Right")
		         ?? GetNodeOrNull<VBoxContainer>("HSplit/Right");
		if (right == null) return;

		_reunionPanel = new VBoxContainer();

		var banner    = new PanelContainer();
		var bannerBox = new VBoxContainer();
		banner.AddChild(bannerBox);

		var titleLbl = new Label();
		titleLbl.Text = $"🎖  REUNION  —  {session.ReunionOriginal.Name} has returned.";
		titleLbl.AddThemeColorOverride("font_color", new Color("3ecdef"));
		bannerBox.AddChild(titleLbl);

		var subLbl = new Label();
		subLbl.Text = "Who leads from here?";
		bannerBox.AddChild(subLbl);

		_reunionPanel.AddChild(banner);

		var btnRow = new HBoxContainer();
		btnRow.AddThemeConstantOverride("separation", 8);

		var orig    = session.ReunionOriginal;
		var interim = session.ReunionInterim;

		var keepOrigBtn = new Button();
		keepOrigBtn.Text              = $"★  {orig.Name} leads\n(interim gets +2 recognition)";
		keepOrigBtn.ClipText          = false;
		keepOrigBtn.CustomMinimumSize = new Vector2(220, 48);
		keepOrigBtn.Pressed += () => OnReunionChoice(keepOriginal: true);
		btnRow.AddChild(keepOrigBtn);

		var keepInterimBtn = new Button();
		keepInterimBtn.Text              = $"★  {interim.Name} leads\n(original gets +2 recognition)";
		keepInterimBtn.ClipText          = false;
		keepInterimBtn.CustomMinimumSize = new Vector2(220, 48);
		keepInterimBtn.Pressed += () => OnReunionChoice(keepOriginal: false);
		btnRow.AddChild(keepInterimBtn);

		_reunionPanel.AddChild(btnRow);

		var sep = new HSeparator();
		sep.CustomMinimumSize = new Vector2(0, 8);
		_reunionPanel.AddChild(sep);

		right.AddChild(_reunionPanel);
		right.MoveChild(_reunionPanel, 0);
	}

	private void OnReunionChoice(bool keepOriginal)
	{
		var session = GameSession.Instance;
		if (session == null) return;

		session.ResolveReunion(keepOriginal);
		GD.Print($"PreMatch: Reunion resolved — keepOriginal={keepOriginal}.");

		if (_reunionPanel != null) { _reunionPanel.QueueFree(); _reunionPanel = null; }
		RefreshRoster();
		RefreshDeckDisplay();
	}

	// ── Hunt panel ───────────────────────────────────────────────────────────
	// Shown when the player's hero has been captured (systems.md §7).
	// Injected as the first child of HSplit/Right so it sits above the roster.

	private void BuildHuntPanel()
	{
		var session = GameSession.Instance;
		if (session == null || !session.IsHeadless) return;

		var hero  = session.CapturedHero;

		// Try both paths — the scene may or may not have the Margin wrapper
		var right = GetNodeOrNull<VBoxContainer>("Margin/HSplit/Right")
		         ?? GetNodeOrNull<VBoxContainer>("HSplit/Right");

		if (right == null)
		{
			GD.PrintErr("BuildHuntPanel: could not find Right VBoxContainer. " +
			            "Hunt panel will not display.");
			return;
		}

		_huntPanel = new VBoxContainer();

		// ── Warning banner ─────────────────────────────────────────────────────
		var banner    = new PanelContainer();
		var bannerBox = new VBoxContainer();
		banner.AddChild(bannerBox);

		var titleLbl = new Label();
		titleLbl.Text = $"⚠  HEADLESS  —  {hero.Name} CAPTURED";
		titleLbl.AddThemeColorOverride("font_color", new Color("d94a4a"));
		bannerBox.AddChild(titleLbl);

		var captorLbl = new Label();
		captorLbl.Text = $"Captor faction: {session.CapturingFaction}";
		bannerBox.AddChild(captorLbl);

		var attemptsLbl = new Label();
		attemptsLbl.Text = session.ReclamationAttemptsLeft > 0
			? $"Reclaim window: {session.ReclamationAttemptsLeft} attempt(s) remaining"
			: "Reclaim window closed.";
		bannerBox.AddChild(attemptsLbl);

		// Interim status line
		if (session.HasInterim)
		{
			var interimLbl = new Label();
			interimLbl.Text = $"Interim hero: {session.InterimHero.Name}  —  build a deck and hit Reclaim.";
			interimLbl.AddThemeColorOverride("font_color", new Color("4a90d9"));
			bannerBox.AddChild(interimLbl);
		}
		else
		{
			var needsStepUp = new Label();
			needsStepUp.Text = "Step Up an interim hero before you can launch Reclaim.";
			needsStepUp.AddThemeColorOverride("font_color", new Color("ff8844"));
			bannerBox.AddChild(needsStepUp);
		}

		_huntPanel.AddChild(banner);

		// ── Action buttons ─────────────────────────────────────────────────────
		var btnRow = new HBoxContainer();
		btnRow.AddThemeConstantOverride("separation", 8);

		// Reclaim — only shown after interim is assigned AND attempts remain
		if (session.HasInterim && session.ReclamationAttemptsLeft > 0)
		{
			_reclaimBtn                  = new Button();
			_reclaimBtn.Text             = $"⚔  Reclaim {hero.Name}";
			_reclaimBtn.CustomMinimumSize = new Vector2(200, 40);
			_reclaimBtn.TooltipText      = "Field your interim hero and fight to win back your captured hero.";
			_reclaimBtn.Disabled         = (_selectedDeck.Count != MaxDeckSize);
			_reclaimBtn.Pressed         += OnHuntReclaimPressed;
			btnRow.AddChild(_reclaimBtn);
		}

		// Buyout — enabled for all factions except HollowChoir (they never sell).
		// Razorkin may refuse; button click rolls the refusal check first.
		if (session.CapturingFaction == Faction.HollowChoir)
		{
			var noSellLbl = new Label();
			noSellLbl.Text = "The Choir do not sell.";
			noSellLbl.AddThemeColorOverride("font_color", new Color("8888cc"));
			btnRow.AddChild(noSellLbl);
		}
		else
		{
			int failedAttempts = 2 - session.ReclamationAttemptsLeft;
			int cost = BuyoutPricing.ComputeCost(
				session.CapturingFaction,
				session.Cred.Tier,
				failedAttempts);
			bool canAfford = (cost >= 0) && (session.Scrip >= cost);

			var buyoutBtn = new Button();
			buyoutBtn.ClipText          = false;
			buyoutBtn.CustomMinimumSize = new Vector2(180, 40);

			if (session.CapturingFaction == Faction.Razorkin)
				buyoutBtn.Text = $"💳  Razorkin buyout — {cost} scrip\n(may refuse)";
			else
				buyoutBtn.Text = $"💳  Buyout — {cost} scrip";

			if (!canAfford)
			{
				buyoutBtn.Disabled    = true;
				buyoutBtn.TooltipText = cost < 0
					? "No buyout available."
					: $"Need {cost - session.Scrip} more scrip.";
			}
			else
			{
				var captorFaction = session.CapturingFaction;
				var capturedHero  = session.CapturedHero;
				buyoutBtn.Pressed += () => OnBuyoutPressed(captorFaction, capturedHero, cost);
			}

			btnRow.AddChild(buyoutBtn);
		}

		// Step Up toggle — always available so player can pick or change interim
		_stepUpToggleBtn = new Button();
		_stepUpToggleBtn.Text = _stepUpMode ? "✕  Cancel"
			: (session.HasInterim ? "↑  Change Interim" : "↑  Step Up");
		_stepUpToggleBtn.CustomMinimumSize = new Vector2(160, 40);
		_stepUpToggleBtn.TooltipText       = "Choose a roster card to serve as interim hero.";
		_stepUpToggleBtn.Pressed          += OnStepUpTogglePressed;
		btnRow.AddChild(_stepUpToggleBtn);

		_huntPanel.AddChild(btnRow);

		var sep = new HSeparator();
		sep.CustomMinimumSize = new Vector2(0, 8);
		_huntPanel.AddChild(sep);

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

	// ── Buyout flow ───────────────────────────────────────────────────────────
	// 1. Razorkin: roll refusal check first → show refusal banner OR proceed.
	// 2. All other factions (once Razorkin clears): show confirm dialog.
	// 3. On confirm: spend scrip, reclaim hero, apply BuyoutHero cred event.

	private Control _buyoutPopup = null;

	private void OnBuyoutPressed(Faction captor, CardData hero, int cost)
	{
		var session = GameSession.Instance;
		if (session == null) return;

		// Razorkin refusal check
		if (captor == Faction.Razorkin)
		{
			bool refused = RazorkinRefusal.IsRefused(session.Cred.Tier);
			GD.Print($"PreMatch: Razorkin buyout roll — refused={refused} " +
			         $"(chance={RazorkinRefusal.RefusalChance(session.Cred.Tier):P0}).");

			if (refused)
			{
				ShowBuyoutRefusedBanner();
				return;
			}
		}

		ShowBuyoutConfirmDialog(hero, captor, cost);
	}

	private void ShowBuyoutRefusedBanner()
	{
		if (_buyoutPopup != null) return;

		// Small non-blocking banner above the hunt panel
		var banner = new PanelContainer();
		banner.CustomMinimumSize = new Vector2(380, 0);
		var style = new StyleBoxFlat();
		style.BgColor    = new Color(0.4f, 0.15f, 0.1f, 0.95f);
		style.SetCornerRadiusAll(4);
		banner.AddThemeStyleboxOverride("panel", style);

		var innerBox = new VBoxContainer();
		innerBox.AddThemeConstantOverride("margin_left",   16);
		innerBox.AddThemeConstantOverride("margin_right",  16);
		innerBox.AddThemeConstantOverride("margin_top",    12);
		innerBox.AddThemeConstantOverride("margin_bottom", 12);
		banner.AddChild(innerBox);

		var msg = new Label();
		msg.Text = "Refused.\nThey want the fight. Try again or duel for it.";
		msg.AddThemeColorOverride("font_color", new Color("fd7a50"));
		msg.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		innerBox.AddChild(msg);

		var dismissBtn = new Button();
		dismissBtn.Text              = "Dismiss";
		dismissBtn.CustomMinimumSize = new Vector2(100, 30);
		dismissBtn.Pressed          += () =>
		{
			_buyoutPopup?.QueueFree();
			_buyoutPopup = null;
		};
		innerBox.AddChild(dismissBtn);

		_buyoutPopup = banner;

		// Inject into Right column above hunt panel
		var right = GetNodeOrNull<VBoxContainer>("Margin/HSplit/Right")
		         ?? GetNodeOrNull<VBoxContainer>("HSplit/Right");
		right?.AddChild(banner);
		right?.MoveChild(banner, 0);
	}

	private void ShowBuyoutConfirmDialog(CardData hero, Faction captor, int cost)
	{
		if (_buyoutPopup != null) return;

		var session = GameSession.Instance;
		if (session == null) return;

		// Full-screen dim overlay (same pattern as hunt reminder popup)
		_buyoutPopup = new Control();
		_buyoutPopup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_buyoutPopup.MouseFilter = MouseFilterEnum.Stop;

		var overlay = new Panel();
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var overlayStyle = new StyleBoxFlat();
		overlayStyle.BgColor = new Color(0f, 0f, 0f, 0.72f);
		overlay.AddThemeStyleboxOverride("panel", overlayStyle);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		_buyoutPopup.AddChild(overlay);

		var dialog = new PanelContainer();
		dialog.CustomMinimumSize = new Vector2(440, 0);
		dialog.AnchorLeft        = 0.5f;
		dialog.AnchorTop         = 0.5f;
		dialog.AnchorRight       = 0.5f;
		dialog.AnchorBottom      = 0.5f;
		dialog.GrowHorizontal    = Control.GrowDirection.Both;
		dialog.GrowVertical      = Control.GrowDirection.Both;
		dialog.OffsetLeft        = -220f;
		dialog.OffsetRight       =  220f;
		dialog.OffsetTop         = -120f;
		dialog.OffsetBottom      =  120f;
		var dialogStyle = new StyleBoxFlat();
		dialogStyle.BgColor           = new Color(0.06f, 0.07f, 0.10f, 1f);
		dialogStyle.BorderWidthLeft   = 2; dialogStyle.BorderWidthTop    = 2;
		dialogStyle.BorderWidthRight  = 2; dialogStyle.BorderWidthBottom = 2;
		dialogStyle.BorderColor       = new Color("f0c040");
		dialogStyle.SetCornerRadiusAll(6);
		dialog.AddThemeStyleboxOverride("panel", dialogStyle);
		_buyoutPopup.AddChild(dialog);

		var padBox = new MarginContainer();
		padBox.AddThemeConstantOverride("margin_left",   24);
		padBox.AddThemeConstantOverride("margin_right",  24);
		padBox.AddThemeConstantOverride("margin_top",    20);
		padBox.AddThemeConstantOverride("margin_bottom", 20);
		dialog.AddChild(padBox);

		var inner = new VBoxContainer();
		inner.AddThemeConstantOverride("separation", 12);
		padBox.AddChild(inner);

		var titleLbl = new Label();
		titleLbl.Text = $"💳  Buyout {hero.Name}?";
		titleLbl.AddThemeColorOverride("font_color", new Color("f0c040"));
		titleLbl.AddThemeFontSizeOverride("font_size", 18);
		titleLbl.HorizontalAlignment = HorizontalAlignment.Center;
		inner.AddChild(titleLbl);

		var bodyLbl = new Label();
		bodyLbl.Text = $"Pay {cost} scrip to {captor} to ransom your hero.\n" +
		               $"Balance after: {session.Scrip - cost} scrip.\n\n" +
		               "This does not consume a Reclaim attempt.";
		bodyLbl.AutowrapMode        = TextServer.AutowrapMode.WordSmart;
		bodyLbl.HorizontalAlignment = HorizontalAlignment.Center;
		bodyLbl.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f, 1f));
		inner.AddChild(bodyLbl);

		var btnRow = new HBoxContainer();
		btnRow.Alignment = BoxContainer.AlignmentMode.Center;
		btnRow.AddThemeConstantOverride("separation", 16);
		inner.AddChild(btnRow);

		var confirmBtn = new Button();
		confirmBtn.Text              = $"Pay {cost} scrip";
		confirmBtn.CustomMinimumSize = new Vector2(160, 42);
		confirmBtn.Pressed          += () =>
		{
			DismissBuyoutPopup();
			ExecuteBuyout(cost);
		};
		btnRow.AddChild(confirmBtn);

		var cancelBtn = new Button();
		cancelBtn.Text              = "Cancel";
		cancelBtn.CustomMinimumSize = new Vector2(100, 42);
		cancelBtn.Pressed          += () => DismissBuyoutPopup();
		btnRow.AddChild(cancelBtn);

		AddChild(_buyoutPopup);
	}

	private void DismissBuyoutPopup()
	{
		_buyoutPopup?.QueueFree();
		_buyoutPopup = null;
	}

	private void ExecuteBuyout(int cost)
	{
		var session = GameSession.Instance;
		if (session == null || !session.IsHeadless) return;

		if (!session.SpendScrip(cost))
		{
			GD.PrintErr($"PreMatch: buyout — could not spend {cost} scrip.");
			return;
		}

		GD.Print($"PreMatch: buyout confirmed — paying {cost} scrip, reclaiming {session.CapturedHero.Name}.");

		// Reclaim hero (clears Hunt window)
		session.ReclaimHero();

		// Buyout carries a −4 cred hit (systems.md §8.4)
		session.Cred.ApplyEvents(CredEvent.BuyoutHero);
		GD.Print($"Cred after buyout: {session.Cred.Cred} ({session.Cred.Tier}).");

		// Persist the new state
		SaveManager.SaveGame();

		// Rebuild UI — Hunt panel is gone, cred bar and scrip label need refresh
		if (_huntPanel != null) { _huntPanel.QueueFree(); _huntPanel = null; }
		if (_credBar  != null) _credBar.Refresh(session.Cred);
		RefreshScripLabel();

		// If Reunion pending (interim hero exists), banner needs building
		BuildReunionBanner();

		if (!CheckRunOver())
			RefreshRoster();

		RefreshDeckDisplay();
		GD.Print("PreMatch: buyout complete — roster rebuilt.");
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

		GD.Print($"PreMatch: Step Up — {promoted.Name} is the interim hero. Hunt still open.");

		_stepUpMode      = false;
		_stepUpToggleBtn = null;
		_reclaimBtn      = null;

		// Rebuild Hunt panel — Hunt is still open, Reclaim button should now appear
		if (_huntPanel != null) { _huntPanel.QueueFree(); _huntPanel = null; }
		BuildHuntPanel();

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

		// If already in run-over state, return true immediately — don't re-wire
		// the StartButton. This prevents the double-disconnect error when
		// OnPromoteCardSelected calls CheckRunOver a second time.
		if (_isRunOver) return true;

		// A valid deck needs 5 cards with at most 1 hero.
		// Selectable slots = non-heroes + min(heroes, 1).
		int heroes     = session.Roster.FindAll(c => c.Tier == Tier.Hero).Count;
		int nonHeroes  = session.Roster.Count - heroes;
		int selectable = nonHeroes + System.Math.Min(heroes, 1);

		// While a Hunt is active the hero is captured but still yours — don't
		// declare the run over until the player has resolved the Hunt (Step Up
		// or Reclaim). After Step Up, OnPromoteCardSelected calls CheckRunOver again.
		// Exception: if there are fewer than 4 non-hero cards, even a successful
		// Reclaim can't produce a valid deck (need 1 hero + 4 non-heroes = 5).
		if (session.IsHeadless && nonHeroes >= MaxDeckSize - 1) return false;
		// If Headless and nonHeroes < 4, fall through to run-over.

		if (selectable >= MaxDeckSize) return false;

		// Not enough cards to field a full deck — run is over
		_isRunOver = true;
		GD.Print($"PreMatch: roster has {session.Roster.Count} cards ({selectable} selectable) — run over.");

		if (StartButton != null)
		{
			StartButton.Text     = "Run Over — New Run";
			StartButton.Pressed -= OnStartPressed;
			StartButton.Pressed += OnNewRun;
			StartButton.Disabled = false;
		}

		if (DeckCountLabel != null)
			DeckCountLabel.Text = $"Only {selectable} selectable card(s) remain — crew is gone.";

		// Replace roster with a message — no + buttons in run-over state
		if (RosterGrid != null)
		{
			foreach (var child in RosterGrid.GetChildren())
				child.QueueFree();
			RosterGrid.Columns = 1;
			var msg = new Label();
			msg.Text = (session.Roster.Count >= MaxDeckSize && selectable < MaxDeckSize)
				? $"Too many heroes, not enough crew — only {nonHeroes} non-hero card(s) left. Can't field a full team."
				: $"Crew lost. {session.Roster.Count} card(s) remain — not enough to field a team.";
			msg.AutowrapMode      = TextServer.AutowrapMode.WordSmart;
			msg.CustomMinimumSize = new Vector2(380, 0);
			RosterGrid.AddChild(msg);
		}
		return true;
	}

	private void OnNewRun()
	{
		GD.Print("PreMatch: starting new run — returning to main menu.");
		GameSession.Instance?.InitializeNewRun();
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	// ── Hunt reminder popup ───────────────────────────────────────────────────
	// Shown when the player hits Start while a Hunt is still open.

	private Control _huntPopup = null;

	private void ShowHuntReminderPopup()
	{
		if (_huntPopup != null) return; // already showing

		var session = GameSession.Instance;
		var hero    = session?.CapturedHero;

		// Full-screen dim overlay
		_huntPopup = new Control();
		_huntPopup.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_huntPopup.MouseFilter = MouseFilterEnum.Stop; // block clicks behind it

		var overlay = new Panel();
		overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var overlayStyle   = new StyleBoxFlat();
		overlayStyle.BgColor = new Color(0f, 0f, 0f, 0.72f);
		overlay.AddThemeStyleboxOverride("panel", overlayStyle);
		overlay.MouseFilter = MouseFilterEnum.Ignore;
		_huntPopup.AddChild(overlay);

		// Centered dialog box
		// Center the dialog: anchor all four sides to 0.5 (screen center),
		// then grow outward symmetrically. SetAnchorsAndOffsetsPreset(Center)
		// anchors the TOP-LEFT corner at center — this is the correct approach.
		var dialog = new PanelContainer();
		dialog.CustomMinimumSize  = new Vector2(480, 0);
		dialog.AnchorLeft         = 0.5f;
		dialog.AnchorTop          = 0.5f;
		dialog.AnchorRight        = 0.5f;
		dialog.AnchorBottom       = 0.5f;
		dialog.GrowHorizontal     = Control.GrowDirection.Both;
		dialog.GrowVertical       = Control.GrowDirection.Both;
		dialog.OffsetLeft         = -240f;
		dialog.OffsetRight        =  240f;
		dialog.OffsetTop          = -175f;
		dialog.OffsetBottom       =  175f;
		var dialogStyle = new StyleBoxFlat();
		dialogStyle.BgColor           = new Color(0.08f, 0.06f, 0.12f, 1f);
		dialogStyle.BorderWidthLeft   = 2;
		dialogStyle.BorderWidthTop    = 2;
		dialogStyle.BorderWidthRight  = 2;
		dialogStyle.BorderWidthBottom = 2;
		dialogStyle.BorderColor       = new Color(0.7f, 0.2f, 0.2f, 0.9f);
		dialogStyle.SetCornerRadiusAll(6);
		dialog.AddThemeStyleboxOverride("panel", dialogStyle);
		_huntPopup.AddChild(dialog);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 14);
		dialog.AddChild(box);

		// Padding inside the box
		var padBox = new MarginContainer();
		padBox.AddThemeConstantOverride("margin_left",   24);
		padBox.AddThemeConstantOverride("margin_right",  24);
		padBox.AddThemeConstantOverride("margin_top",    24);
		padBox.AddThemeConstantOverride("margin_bottom", 24);
		box.AddChild(padBox);

		var innerBox = new VBoxContainer();
		innerBox.AddThemeConstantOverride("separation", 14);
		padBox.AddChild(innerBox);

		// Title
		var titleLbl = new Label();
		titleLbl.Text = "⚠  YOUR HERO IS STILL OUT THERE";
		titleLbl.AddThemeColorOverride("font_color", new Color(0.9f, 0.3f, 0.3f, 1f));
		titleLbl.AddThemeFontSizeOverride("font_size", 20);
		titleLbl.HorizontalAlignment = HorizontalAlignment.Center;
		innerBox.AddChild(titleLbl);

		// Body
		var bodyLbl = new Label();
		string heroName    = hero?.Name ?? "your hero";
		string faction     = session?.CapturingFaction.ToString() ?? "unknown";
		string attemptsStr = session?.ReclamationAttemptsLeft > 0
			? $"{session.ReclamationAttemptsLeft} reclaim attempt(s) remaining"
			: "reclaim window is closed";
		bodyLbl.Text = $"{heroName} was captured by {faction}.\n" +
		               $"{attemptsStr}.\n\n" +
		               "Would you like to reclaim them before this match?";
		bodyLbl.AutowrapMode        = TextServer.AutowrapMode.WordSmart;
		bodyLbl.HorizontalAlignment = HorizontalAlignment.Center;
		bodyLbl.AddThemeColorOverride("font_color", new Color(0.85f, 0.85f, 0.85f, 1f));
		bodyLbl.AddThemeFontSizeOverride("font_size", 15);
		innerBox.AddChild(bodyLbl);

		// Buttons
		var btnRow = new HBoxContainer();
		btnRow.Alignment = BoxContainer.AlignmentMode.Center;
		btnRow.AddThemeConstantOverride("separation", 20);
		innerBox.AddChild(btnRow);

		// Reclaim button — only if attempts remain and interim is ready
		bool canReclaim = session?.HasInterim == true && session?.ReclamationAttemptsLeft > 0;

		var reclaimBtn = new Button();
		reclaimBtn.Text              = $"⚔  Reclaim {heroName}";
		reclaimBtn.CustomMinimumSize = new Vector2(200, 46);
		reclaimBtn.Disabled          = !canReclaim;
		reclaimBtn.TooltipText       = canReclaim
			? "Fight to win your hero back before this match."
			: "Step Up an interim hero first to unlock Reclaim.";
		reclaimBtn.Pressed += () =>
		{
			DismissHuntPopup();
			OnHuntReclaimPressed();
		};
		btnRow.AddChild(reclaimBtn);

		var continueBtn = new Button();
		continueBtn.Text              = "Continue without them →";
		continueBtn.CustomMinimumSize = new Vector2(200, 46);
		continueBtn.Pressed += () =>
		{
			DismissHuntPopup();
			LaunchMatch();
		};
		btnRow.AddChild(continueBtn);

		AddChild(_huntPopup);
	}

	private void DismissHuntPopup()
	{
		_huntPopup?.QueueFree();
		_huntPopup = null;
	}

	// ── Start match ───────────────────────────────────────────────────────────

	private void OnStartPressed()
	{
		if (_selectedDeck.Count != MaxDeckSize)
		{
			GD.Print("PreMatch: need exactly 5 cards to start.");
			return;
		}

		var session = GameSession.Instance;

		// If a Hunt is active and reclaim window is still open, prompt the player.
		if (session != null && session.IsHeadless && session.ReclamationAttemptsLeft > 0)
		{
			ShowHuntReminderPopup();
			return;
		}

		LaunchMatch();
	}

	private void LaunchMatch()
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