# Trips & Triads — Dev Handoff (Session 4)

*Last updated end of context window 4. Hand this document back at the start of every new session.*

---

## Quick Start

**Repo:** https://github.com/Dbartra1/trips-and-triads  
**Engine:** Godot 4.6.3 stable mono  
**Language:** C# (.NET / net8.0)  
**Editor:** Cursor  
**Platform:** Windows + RTX 4080

**To run:** Open project in Godot → Play. Main scene is `Scenes/MainMenu.tscn`.

**Autoload required:** `Project → Project Settings → Globals` → Add `res://Scripts/GameSession.cs` as `GameSession`. If this is missing the game will crash on launch.

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
    GameManager.cs        — Turn manager, VesnaStartingCap=7, DealHands, PlayCard,
                            ApplyTurnEndAbilities, ResolveDecayCaptures
    NameGenerator.cs      — Syllable tables, hero titles, usedFirstNames dedup
    SaveManager.cs        — NEW: Save/load campaign state to user://savegame.json
    StepUpPromoter.cs     — promotes highest-stat non-hero card to Hero tier (systems.md §7.5)
  Rules/
    ICardAbility.cs / IProtocol.cs
    CaptureResolver.cs    — Base capture, Rivalry, Breach chain, protocol passes, Cascade, Listener
    DomainResolver.cs     — AegisProtocol, Killzone, LateralGrid, Sprawl
    BondResolver.cs       — All 7 bonds; ContaminationEnabled=false (disabled until campaign layer)
    MatchConfig.cs        — Active protocols + flags + factory methods
    HandshakeProtocol.cs / TallyProtocol.cs / WallSignatureProtocol.cs
    VesnaAbility.cs / SumiAbility.cs / LetheAbility.cs
  GameBoard.cs            — Match runner; full stake resolution; Hunt AI injection; hero capture detection
  GameSession.cs          — Autoload: Roster, SelectedDeck, full Hunt state, StepUp(), ReclaimHero()
  MainMenu.cs
  PreMatch/
    PreMatchScreen.cs     — District picker, deck builder, Hunt panel (Reclaim / Buyout stub / Step Up)
  PostMatch/
    PostMatchScreen.cs    — Result + Hunt outcome annotation, CardsWon/Lost grids
  UI/
    CardNode.cs / CellNode.cs / HandNode.cs

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
- `GameSession` autoload persisting all state across scenes
- **All 4 stakes fully implemented** in `GameBoard.ResolveStake`:
  - **OneJob** — winner takes 1 card; heroes now capturable (no protection)
  - **TheSpread** — winner takes N cards equal to score margin; heroes sorted last
  - **AsFlipped** — player keeps AI-original cards they control; loses P1-original cards AI controls
  - **Everything** — winner takes loser's entire board hand
- Run-over condition: roster < 5 → message replaces grid, button rewired to New Run (horizontal label fix confirmed)

### Save States (Phase 8a — new this session)
Full implementation of campaign persistence via `Scripts/Core/SaveManager.cs`.

**`CardData.ShallowClone()`** — new method, used by SaveManager to clone database templates before applying saved mutations.

**`DistrictManager.GetAllMeters()` / `SetMeter()`** — new methods exposing control meter state for save/load.

**`GameSession` load methods** — `LoadRoster`, `LoadHuntState`, `LoadInterimHero`, `LoadDeckSnapshot`, `LoadReunionState` — called exclusively by SaveManager.

**Save triggers:**
- `ApplyStakeResult()` — saves after every match (roster changes)
- `InitializeNewRun()` — deletes existing save (fresh start)
- `_Ready()` — attempts `SaveManager.LoadGame()` on startup; falls back to `InitializeNewRun()` if no save exists

**What is saved:** Roster (full CardData including procedural and mutated stats), Hunt state (captured hero, captor faction, reclaim attempts, interim hero, deck snapshot), Reunion state, district control meters, active district ID.

**Save format:** JSON at `user://savegame.json`. Named cards (those with an Id in CardDatabase) are saved by Id + mutations. Procedural cards are saved in full.

### Sumi ability fix (this session)
`GameManager.ApplyTurnEndAbilities` — added `card.OwnerId != CurrentPlayerId` guard. A Compound hero that has been captured and flipped to AI control no longer fires its Ledger ability on P1's turn, which was causing the AI's cards to be buffed.

### StartButton double-wire fix (this session)
`PreMatchScreen.CheckRunOver` — added early return `if (_isRunOver) return true` to prevent double disconnect/reconnect of StartButton signal when called a second time via `OnPromoteCardSelected`.
Full implementation of `systems.md §7`.

**`Scripts/Core/StepUpPromoter.cs` (new)**
- `Promote(deckCards)`: finds highest-stat non-hero, sets A (10) on its highest edge, caps lowest edge to ≤3, assigns faction DomainType, promotes Tier to Hero

**`GameSession` Hunt state**
- `CapturedHero`, `CapturingFaction`, `ReclamationAttemptsLeft` (starts at 2), `DeckWhenHeroWasCaptured`
- `IsHeadless` (computed: CapturedHero != null), `IsHuntMatch`, `HeroReclaimed`
- `SetCapturedHero(hero, faction)` — opens window, removes hero from roster, snapshots deck
- `ConsumeReclaimAttempt()` — burns one attempt
- `ReclaimHero()` — returns hero to roster, sets HeroReclaimed, calls ClearHunt
- `StepUp()` — delegates to StepUpPromoter, clears Hunt
- `ClearHunt()` — resets all Hunt state; also called by InitializeNewRun

**`GameBoard` Hunt logic**
- AI hand: if IsHuntMatch, splices CapturedHero into AI hand (replaces last Street)
- `wasHuntMatch` captured before state mutation; win → ReclaimHero(); loss → ConsumeReclaimAttempt()
- `TryLoseCard(session, card)`: adds to CardsLost; if card is a Hero, calls SetCapturedHero
- `GetAllBoardCards(ownerId)`: new helper, heroes sorted last for margin-capped stakes
- `GetAIHeroFaction()`: new helper for captor faction detection

**`PreMatchScreen` Hunt panel**
- `BuildHuntPanel()` injected at top of HSplit/Right when IsHeadless
- Banner: hero name, captor faction, attempts remaining (or "Window closed" when 0)
- **Reclaim** button (visible while attempts > 0): requires full deck, sets IsHuntMatch=true, launches GameBoard
- **Buyout** button: disabled, labelled "Phase 9" — HollowChoir shows "The Choir do not sell." instead
- **Step Up** button: promotes best surviving card, clears Hunt, rebuilds roster display

**`PostMatchScreen` Hunt annotation**
- ResultLabel appends: "✓ Hero reclaimed!" / "✕ Hero still captured — N attempts remaining" / "✕ Reclaim window closed"

---

## Known Issues / Tech Debt

### Contamination Disabled
`BondResolver.ContaminationEnabled = false` — intentional. Re-enable per district when campaign layer is live:
```csharp
BondResolver.ContaminationEnabled = district.Controller == "HollowChoir";
```

### The Rivalry — double log
`BondResolver.Apply` called twice in `GameManager.PlayCard`. Cosmetic only, no gameplay impact.

### Spreading Rule resets on every session
`DistrictManager.Initialize()` was previously called in `GameBoard._Ready()`, resetting all meters. **Fixed in Phase 8a** — district meters now persist via `SaveManager`.

### Conscription pool
AI always uses its fixed hand under Conscription — AI has no roster to draw from.

### Shaken mechanic not implemented
Per `systems.md §5.1` — cards joining roster by capture should arrive Shaken (lowest edge = 0 for first match). Not yet built.

### Buyout not implemented
Hunt panel shows disabled Buyout button with "Phase 9" label. Full buyout requires scrip economy (Phase 9).

### Hunt matches currently run district protocols
Hunt (Reclaim) matches inherit the district's active protocols (Wall Signature, Cascade, etc.) on top of the AsFlipped stake. This is unintentional — `systems.md §7.3` specifies only "AsFlipped, hero-stake rule" with no mention of protocols. The fix (build a clean base-capture-only `MatchConfig` when `session.IsHuntMatch` is true in `GameBoard._Ready`) is deferred to Phase 9. Implementation: one `if (session.IsHuntMatch)` branch before `_matchConfig = DistrictManager.Instance.BuildMatchConfig()`.

### Multi-Hunt not implemented
Currently only one Hero Hunt can be active at a time. If your hero is captured while already Headless, the second capture is silently dropped (the card is removed from roster but no Hunt opens). Phase 7c will add `List<HuntEntry>` to `GameSession`, a Hunt cap of 3, oldest-Hunt expiry, and a Hunt selector UI in `PreMatchScreen`.

### MainMenu routing
`PreMatchScreen.OnNewRun` calls `InitializeNewRun()` directly and routes to `PreMatchScreen`, bypassing `MainMenu`. Should route back to `MainMenu.tscn`. Fix: change `GetTree().ChangeSceneToFile("res://Scenes/PreMatch/PreMatchScreen.tscn")` in `OnNewRun` to `"res://Scenes/MainMenu.tscn"`.

### Enemy hand not visible
AI hand exists as `List<CardInstance>` in `GameManager` but has no visual node. Only visible under the Intercept protocol. Fix: add a second `HandNode` to `Scenes/Board/GameBoard.tscn` and export it on `GameBoard` as `AIHand`; populate it alongside `PlayerHand` in `DealHands`.

---

## Balance Constants

| Constant | Location | Value | Notes |
|---|---|---|---|
| `VesnaStartingCap` | `GameManager.cs` line 24 | 7 | AI Vesna enters at 7/7/7/7 in The Stub |
| `StreetMin/Max` | `CrewGenerator.cs` line 14 | 10–14 | Street total, min edge 2 |
| `ProMin/Max` | `CrewGenerator.cs` line 15 | 16–22 | Pro total |
| `AbilityWeights` | `CrewGenerator.cs` line 19 | None=75%, Compound=15%, Copy=10% | Player hero ability pool |
| `ContaminationEnabled` | `BondResolver.cs` line 13 | false | Off until campaign districts |
| Reclaim attempts | `GameSession.SetCapturedHero` | 2 | Hard cap per `systems.md §7.3` |
| Step Up soft edge cap | `StepUpPromoter.Promote` | 3 | Max value for promoted hero's soft side |

---

## What's Next (Priority Order)

### Immediate cleanup (next session)
- **MainMenu routing** — one-line fix in `PreMatchScreen.OnNewRun` (see Known Issues)
- **Enemy hand visible** — add `AIHand` node to `GameBoard.tscn` + populate in `DealHands` (see Known Issues)
- **Testing suite** — dedicated Claude Code session; automate game-state assertions and gameplay simulations

### Phase 8b — Street Cred (`systems.md §8`)
Single broad campaign stat (Nameless → Known → Named → Notorious → Legend). Affects:
- Razorkin buyout refusal probability (floor 15–20%, scales with cred)
- Ransom prices across all factions
- Contract payouts (income multiplier)
- District control shift speed
- Talent attraction (better free agents at higher cred)

### Phase 9 — Economy & Fixers (`systems.md §9`)
- Scrip as campaign currency (enables Buyout in the Hunt panel)
- 5 Fixers: Della (Standing Work / Mutual Aid), Vig (Wagers), Atlas (Intel / Hunt location), Mrs. Oba (Long Account / debt), The Tailor (Ghost Contracts)
- Contract system as curated duels with scrip payouts
- Free agent Meet → Audition → Sign flow
- **Hunt protocol fix** — strip district protocols from Hunt matches (see Known Issues)

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

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
