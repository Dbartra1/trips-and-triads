# Trips & Triads — Dev Handoff (Session 8)

*Last updated end of context window 8. Hand this document back at the start of every new session.*

---

## Quick Start

**Repo:** https://github.com/Dbartra1/trips-and-triads  
**Branch:** `session-8` (two commits; merge to main when green)  
**Engine:** Godot 4.6.3 stable mono  
**Language:** C# (.NET / net8.0)  
**Editor:** Cursor  
**Platform:** Windows + RTX 4080

**To run:** Open project in Godot → Play. Main scene is `Scenes/MainMenu.tscn`.

**Autoload required:** `Project → Project Settings → Globals` → Add `res://Scripts/GameSession.cs` as `GameSession`. If this is missing the game will crash on launch.

**Before merging session-8:** Run `dotnet test Tests/ --verbosity normal` locally and confirm all tests green. The .NET SDK was not installable in the session-8 CI environment (Ubuntu package mirror 404s); changes were written from source review only.

**Design bibles:** `lore.md` and `systems.md` are attached to this project. Read both before touching faction mechanics, card stats, or campaign systems.

---

## Architecture Overview

### Scene Flow
```
MainMenu → PreMatchScreen → GameBoard → PostMatchScreen → PreMatchScreen
```

### Key Singletons
| Singleton | File | Role |
|---|---|---|
| `GameSession` (Autoload) | `Scripts/GameSession.cs` | Persistent campaign state: Roster, SelectedDeck, SelectedDistrictId, match result, full Hunt state |
| `CardDatabase` | `Scripts/Core/CardDatabase.cs` | Loads `Data/Cards/cards.json` once; `_loaded` guard prevents double-load |
| `DistrictDatabase` | `Scripts/Core/DistrictDatabase.cs` | Loads `Data/Districts/districts.json` once; same guard |
| `DistrictManager` | `Scripts/Core/DistrictManager.cs` | Control meters, district selection, MatchConfig building, Spreading Rule |

### Namespaces
- `TripsAndTriads.Core` — game logic (BoardState, CardData, CardInstance, GameManager, StepUpPromoter, etc.)
- `TripsAndTriads.Rules` — rules engine (CaptureResolver, DomainResolver, BondResolver, protocols, abilities)
- `TripsAndTriads.UI` — visual nodes (CardNode, CellNode, HandNode)
- No namespace — top-level scene scripts (GameBoard, PreMatchScreen, PostMatchScreen, MainMenu, GameSession)

---

## File Structure

```
Scripts/
  Core/
    BoardState.cs         — 3×3 grid, PlaceCard, GetCard, IsEmpty, GetNeighbor, GetScore, IsFull
    CardData.cs           — Template: Faction, Tier, DomainType, AbilityType enums + stat fields
    CardDatabase.cs       — Singleton, loads cards.json, _loaded guard
    CardInstance.cs       — In-play card: OwnerId (mutable), OriginalOwnerId (locked), edge overrides,
                            domain/bond bonuses, behavioral flags
    CrewGenerator.cs      — Generate(7 cards) + SelectBestFive + GenerateAIHand(db)
    DistrictData.cs       — District template model
    DistrictDatabase.cs   — Singleton, loads districts.json, _loaded guard
    DistrictManager.cs    — Control meters, SelectDistrict, BuildMatchConfig, ApplySpreading
    GameManager.cs        — Turn manager, VesnaStartingCap=20, DealHands, PlayCard,
                            ApplyTurnEndAbilities, ResolveDecayCaptures
    NameGenerator.cs      — Syllable tables, hero titles, usedFirstNames dedup
    SaveManager.cs        — Save/load campaign state to user://savegame.json
    StepUpPromoter.cs     — promotes highest-stat non-hero card to Hero tier (systems.md §7.5)
  Rules/
    ICardAbility.cs / IProtocol.cs
    CaptureResolver.cs    — Base capture, Rivalry, Breach chain, protocol passes, Cascade, Listener
    DomainResolver.cs     — AegisProtocol, Killzone, LateralGrid, Sprawl; BonusMultiplier property
    BondResolver.cs       — All 7 bonds; ContaminationEnabled=false (disabled until campaign layer)
    MatchConfig.cs        — Active protocols + flags + factory methods (Scale-20 Path A params)
    HandshakeProtocol.cs  — tolerance param (default 0; Scale-20 uses 2)
    TallyProtocol.cs      — sumTolerance param (default 0; Scale-20 uses 2)
    WallSignatureProtocol.cs — wallValue (default 10) + sumTolerance (default 0) params
    VesnaAbility.cs / SumiAbility.cs / LetheAbility.cs
  GameBoard.cs            — Match runner; full stake resolution; Hunt AI injection; hero capture detection;
                            AiThinkDelay export (0.75s); async AI with card movement tween
  GameSession.cs          — Autoload: Roster, SelectedDeck, full Hunt state, StepUp(), ReclaimHero()
  MainMenu.cs
  PreMatch/
    PreMatchScreen.cs     — District picker, deck builder, Hunt panel (Reclaim / Buyout stub / Step Up)
  PostMatch/
    PostMatchScreen.cs    — Result + Hunt outcome annotation, CardsWon/Lost grids
  UI/
    CardNode.cs           — Card visual; FlipToOwner(int) flip animation; drag source (_GetDragData)
    CellNode.cs           — Board cell; drag-drop target (_CanDropData/_DropData); FlipCard()
    HandNode.cs           — Hand display; drag wiring; GetCardGlobalPosition(int); Count property

Data/
  Cards/cards.json
  Districts/districts.json
```

---

## What Is Built and Confirmed Working

### Core Duel Loop
- 3×3 board, alternating turns, 5-card hands, capture by edge comparison, score at fill
- AI greedy capture simulation
- End panel overlay → PostMatchScreen → PreMatchScreen

### Card Systems (Phases 1–4)
- 29 named cards, all hero mechanics (Vesna decay, Sumi compound, Lethe copy)
- Domain system, all 7 bonds

### Protocols (Phase 5)
Handshake, The Tally, Wall Signature, Cascade, Intercept, Conscription, Standoff

### Districts (Phase 6)
8 districts, DistrictManager control meters, Spreading Rule, MatchConfig factory methods, VesnaStartingCap

### Procedural Crew Generation
Player 7-card crew (Hero + Pro + 5 Street), AI fixed hand (Vesna + Verity + 3 Streets), NameGenerator

### Campaign Loop (Phase 7 — complete)
- Full stake resolution (OneJob, TheSpread, AsFlipped, Everything)
- Hunt system: hero capture, Reclaim attempts, Step Up promotion
- Save/load via SaveManager

### Save States (Phase 8a)
Full campaign persistence. See earlier sessions for detail.

### Scale-20 Balance Port (Session 8 — commit 083242f)
All Scale-20 Path A parameters ported from Logic/ to Scripts/:
- `HandshakeProtocol(tolerance: 2)` — fires at ±2 instead of exact match
- `TallyProtocol(sumTolerance: 2)` — fires when sums within ±2
- `WallSignatureProtocol(wallValue: 20, sumTolerance: 2)` — wall counts as 20
- `DomainResolver.BonusMultiplier = 1` (default); set to 2 for Scale-20 matches
- `GameManager.VesnaStartingCap = 20` (no-op at both Scale-10 and Scale-20)
- `MatchConfig` factory methods updated to Scale-20 params

Protocol constructors default to Scale-10 values — existing math tests unaffected.

**Note:** `Scale20PathATests.cs` doc comment says `tolerance: 1` but code uses `tolerance: 2`. Comment is wrong; code is right. Update comment when convenient.

### AI Thinking Delay (Session 8 — commit e7713bd)
- `GameBoard`: `[Export] float AiThinkDelay = 0.75f` (tunable from inspector; set to 0 for tests)
- `RunAIDelayed()` (async void) waits the timer then calls `RunAI()` (async Task)
- Guards re-check `GameOver` / `StandoffTriggered` after each await

### Card Flip Animation (Session 8 — commit e7713bd)
- `CardNode.FlipToOwner(int newOwnerId)` — Tween squeezes X to 0 (0.1s), swaps color + refreshes stats, unsqueezes to 1 (0.1s). Sine transition.
- `CellNode.FlipCard()` — reads new OwnerId from CardInstance and calls FlipToOwner
- Both player and AI captures trigger the flip animation

### Drag-to-Play (Session 8 — commit e7713bd)
- **CardNode**: `SetDraggable(bool, int handIndex)` + `_GetDragData` override. Returns hand index as Variant. Ghost preview via `SetDragPreview`. Emits `DragStarted` signal.
- **CellNode**: `CellButton.MouseFilter = Ignore` (critical — lets drops reach CellNode). `_CanDropData` / `_DropData` overrides. `_MouseEnter/_MouseExit` hover. `_GuiInput` fallback click.
- **HandNode**: Button overlay removed from `PopulateHand`. Cards wired via `DragStarted` → `CardSelected`. `GetCardGlobalPosition(int)` and `Count` added.
- AI card animates from AIHand slot position to target cell (Cubic/EaseOut, 0.30s).

### Save System Fix (Session 8 — commit e7713bd)
- `DeleteSave()` now uses `DirAccess.Open("user://").Remove("savegame.json")` instead of `RemoveAbsolute(GlobalizePath(...))`. Cross-platform reliable.
- `SaveGame()` error message includes OS path + `FileAccess.GetOpenError()`.
- `GetSaveFilePath()` helper logs and returns OS absolute path for debug builds.

---

## Known Issues / Tech Debt

### Contamination Disabled
`BondResolver.ContaminationEnabled = false` — intentional. Re-enable per district when campaign layer is live:
```csharp
BondResolver.ContaminationEnabled = district.Controller == "HollowChoir";
```

### The Rivalry — double log
`BondResolver.Apply` called twice in `GameManager.PlayCard`. Cosmetic only, no gameplay impact.

### Conscription pool
AI always uses its fixed hand under Conscription — AI has no roster to draw from.

### Shaken mechanic not implemented
Per `systems.md §5.1` — cards joining roster by capture should arrive Shaken (lowest edge = 0 for first match). Not yet built.

### Buyout not implemented
Hunt panel shows disabled Buyout button with "Phase 9" label. Full buyout requires scrip economy (Phase 9).

### Hunt matches run district protocols (deferred to Phase 9)
Hunt (Reclaim) matches still inherit the active district's protocols. `systems.md §7.3` specifies only "AsFlipped, hero-stake rule" with no protocols. Fix:

```csharp
// In GameBoard._Ready(), before BuildMatchConfig():
if (session.IsHuntMatch)
    _matchConfig = new MatchConfig(); // base rules only
else
    _matchConfig = DistrictManager.Instance.BuildMatchConfig();
```

### Multi-Hunt not implemented
Only one Hero Hunt active at a time. Second capture while Headless is silently dropped. Phase 7c will add `List<HuntEntry>` + Hunt cap + expiry + selector UI.

### Scale20PathATests comment mismatch
Doc comment in test class header says `HandshakeProtocol(tolerance: 1)` but every test call uses `tolerance: 2`. Comment is wrong; code is right. Update comment.

---

## Balance Constants

| Constant | Location | Value | Notes |
|---|---|---|---|
| `VesnaStartingCap` | `GameManager.cs` | **20** | Scale-20 no-op (was 7, then 10 in Logic) |
| `AiThinkDelay` | `GameBoard.cs` | **0.75f** | Seconds before AI places; [Export], 0 = instant |
| `StreetMin/Max` | `CrewGenerator.cs` line 14 | 10–14 | Street total, min edge 2 |
| `ProMin/Max` | `CrewGenerator.cs` line 15 | 16–22 | Pro total |
| `AbilityWeights` | `CrewGenerator.cs` line 19 | None=75%, Compound=15%, Copy=10% | Player hero ability pool |
| `ContaminationEnabled` | `BondResolver.cs` line 13 | false | Off until campaign districts |
| Reclaim attempts | `GameSession.SetCapturedHero` | 2 | Hard cap per `systems.md §7.3` |
| Step Up soft edge cap | `StepUpPromoter.Promote` | 3 | Max value for promoted hero's soft side |
| `BonusMultiplier` | `DomainResolver.cs` | **1** | Set to 2 for Scale-20 matches; reset after tests |
| HandshakeProtocol tolerance | `MatchConfig` factory methods | **2** | Scale-20 Path A confirmed |
| TallyProtocol sumTolerance | `MatchConfig` factory methods | **2** | Scale-20 Path A confirmed |
| WallSignatureProtocol wallValue | `MatchConfig` factory methods | **20** | Scale-20 Path A confirmed |

---

## What's Next (Priority Order)

### Immediate — merge + test
Run `dotnet test Tests/ --verbosity normal`. All math tests should be green unchanged (Scale-20 defaults are backward-compatible). The Scale20 simulation tests are slow (~10,000 games each) but should also pass.

### Immediate — Hunt protocol fix (1 line)
In `GameBoard._Ready()`, before `BuildMatchConfig()`:
```csharp
if (session?.IsHuntMatch == true)
    _matchConfig = new MatchConfig(); // base-capture only, no district protocols
else
    _matchConfig = DistrictManager.Instance.BuildMatchConfig();
```

### Phase 8b — Street Cred (`systems.md §8`)
Single broad campaign stat (Nameless → Known → Named → Notorious → Legend). Affects:
- Razorkin buyout refusal probability (floor 15–20%, scales with cred)
- Ransom prices across all factions
- Contract payouts (income multiplier)
- District control shift speed
- Talent attraction (better free agents at higher cred)

### Phase 9 — Economy & Fixers (`systems.md §9`)
- Scrip as campaign currency (enables Buyout in Hunt panel — currently "Phase 9" stub)
- 5 Fixers: Della (Standing Work / Mutual Aid), Vig (Wagers), Atlas (Intel / Hunt location), Mrs. Oba (Long Account / debt), The Tailor (Ghost Contracts)
- Contract system as curated duels with scrip payouts
- Free agent Meet → Audition → Sign flow
- **Hunt protocol fix** included here (if not done immediately above)

### Phase 7c — Multi-Hunt (deferred)
Multiple simultaneous Hunts, Hunt cap of 3, oldest-Hunt expiry (see Known Issues).

### Phase 10 — The Hollowing (`systems.md §10`)
Dead Line contracts → Touched → Fading → Claimed affliction track.

### Phase 11 — Payroll & Debt (`systems.md §11`)
Upkeep per overworld turn. Debt → Collectors → escalating ladder. Mutual Aid vs Lacquer debt.

### Phase 12 — Prestige & Skyline (`systems.md §12`)
Prestige condition (Legend cred + take The Vault). Skyline rival system. Two endings.

### Near-term cleanup (any session)
- **Shaken mechanic** (`systems.md §5.1`): lowest edge = 0 for first match after capture; 3rd Shaken → permanent Calloused
- **Contamination re-enable**: wire `BondResolver.ContaminationEnabled` to district controller check
- **Conscription AI roster**: give AI a persistent roster so Conscription draws from it
- **Faction-specific AI decks**: AI always fields Vesna+Verity; Phase 8b+ should vary by district controller
- **DomainResolver.BonusMultiplier**: wire to DistrictManager so Scale-20 districts set it automatically

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
