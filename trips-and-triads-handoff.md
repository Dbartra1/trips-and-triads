# Trips & Triads — Dev Handoff (Session 6)

*Last updated end of context window 6. Hand this document back at the start of every new session.*

---

## Quick Start

**Repo:** https://github.com/Dbartra1/trips-and-triads  
**Engine:** Godot 4.6.3 stable mono  
**Language:** C# (.NET / net8.0)  
**Editor:** Cursor  
**Platform:** Windows + RTX 4080

**To run the game:** Open project in Godot → Play. Main scene is `Scenes/MainMenu.tscn`.

**To run the test suite:**
```
dotnet build Logic/TripsAndTriads.Logic.csproj
dotnet test  Tests/TripsAndTriads.Tests.csproj --verbosity normal
```
`--verbosity normal` is required — the simulation tests print win-rate summaries to output.

**Autoload required:** `Project → Project Settings → Globals` → Add `res://Scripts/GameSession.cs` as `GameSession`. If this is missing the game will crash on launch.

**Design bibles:** `lore.md` and `systems.md` are attached to this project. Read both before touching faction mechanics, card stats, or campaign systems.

---

## Architecture Overview

### Scene Flow
```
MainMenu → PreMatchScreen → GameBoard → PostMatchScreen → PreMatchScreen
```

### Project Layout
```
trips-and-triads/
  Logic/    ← Pure C# class library. No Godot dependency. The test suite's source of truth.
  Tests/    ← xUnit test project. References Logic/ only.
  Scripts/  ← Godot game scripts. Contain the live GD.Print / Godot API calls.
  Scenes/   ← Godot scene files.
  Data/     ← cards.json, districts.json
```

**The Logic/ and Scripts/ relationship:** `Logic/` contains extracted copies of all game logic files with `GD.Print` replaced by `TestLogger.Log`. `Scripts/` retains the originals with live Godot calls. When logic changes, **both copies must be updated**. See §Testing Suite for details.

### Key Singletons (Godot layer)
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
    SaveManager.cs        — Save/load campaign state to user://savegame.json
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

Logic/                              ← Godot-free extractions for testing
  TripsAndTriads.Logic.csproj
  TestLogger.cs                     — GD.Print shim; captures log messages for assertions
  Core/   (CardData, CardInstance, BoardState, GameManager)
  Rules/  (all resolvers, protocols, abilities, interfaces)

Tests/
  TripsAndTriads.Tests.csproj
  Helpers/
    CardFactory.cs                  — fluent card builder; named shortcuts for every hero
    BoardBuilder.cs                 — fluent board setup; resolves domains/bonds after placement
    GameSimulator.cs                — full mock games; Random/Greedy strategies; BatchResult + Summary()
  Capture/
    BaseCaptureTests.cs             — 10 unit tests: edge comparison, geometry, named hero shapes
    ProtocolTests.cs                — 9 tests: Handshake, Tally, Wall Signature, stacking
    ChainTests.cs                   — 7 tests: The Breach, Cascade, The Listener
  Simulation/
    WinRateTests.cs                 — hero matrix, district protocol impact, 1000-game Monte Carlo

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
- 29 named cards, all hero mechanics (Vesna decay, Sumi compound, Lethe copy — base value fix applied)
- Domain system, all 7 bonds

### Protocols (Phase 5)
Handshake, The Tally, Wall Signature, Cascade, Intercept, Conscription, Standoff

### Districts (Phase 6)
8 districts, DistrictManager control meters, Spreading Rule, MatchConfig factory methods, VesnaStartingCap

### Procedural Crew Generation
Player 7-card crew (Hero + Pro + 5 Street), AI fixed hand (Vesna + Verity + 3 Streets), NameGenerator

### Campaign Loop (Phase 7 — complete)
All 4 stakes, run-over condition, full Hunt system (capture → Headless → Step Up → Reclaim → Reunion)

### Save States (Phase 8a — confirmed working)
Full persistence via `SaveManager`. Roster, Hunt state, district meters, active district ID all survive sessions.

### Board Layout (Session 6 — confirmed working)
Classic Triple Triad layout: hands on left (AI) and right (Player) sides of the board.
- `HandNode` uses `VBoxContainer` — cards stack vertically
- `mouse_filter = 2` (Ignore) on container nodes — bounding box does not block board clicks
- `SetInteractive(false)` strips click buttons from AI hand cards

### AI Hand Always Visible (Session 6)
`AIHandNode` wired to `[Export] public HandNode AIHand` in `GameBoard.cs`. Populated via `RefreshAIHand()` after `DealHands`. Cards removed from display in `RunAI()` as the AI plays. Non-interactive.

### Lethe Base-Value Fix (Session 6)
`LetheAbility.OnPlaced` uses `GetBaseValue()` — copies stat numbers only, not transient domain/bond bonuses. Applied in both `Scripts/Rules/LetheAbility.cs` and `Logic/Rules/LetheAbility.cs`.

### New Run → MainMenu (Session 6)
`PreMatchScreen.OnNewRun` routes to `MainMenu.tscn`. Player always hits the main menu between runs.

---

## Testing Suite (Session 6)

### Architecture
The suite lives entirely outside Godot. `Logic/` is a pure .NET 8 class library — an exact copy of all game logic files with `GD.Print` replaced by `TestLogger.Log`. `Tests/` is an xUnit project referencing `Logic/` only.

**`TestLogger`** — the shim. `TestLogger.Log(msg)` buffers to `Messages` list; `TestLogger.Clear()` resets between tests. Set `WriteToConsole = true` to see output during debugging.

**`CardFactory`** — fluent builder. Named shortcuts (`SeraphYune()`, `SisterGrin()`, etc.) use exact lore.md stat lines. Generic `Street(name, t, r, b, l)` fills boards without affecting the system under test.

**`BoardBuilder`** — fluent board setup. Calls `DomainResolver.Apply` and `BondResolver.Apply` after all placements so tests start in game-accurate state.

**`GameSimulator`** — runs full mock games. Two strategies: `Random` (Monte Carlo baseline) and `Greedy` (mirrors production AI). `RunBatch(p1Factory, p2Factory, games)` returns a `BatchResult` with win rates, score margins, capture averages, and a `Summary()` string.

### Keeping Logic/ in sync
When any game logic file in `Scripts/` changes, the matching file in `Logic/` must be updated to match — only difference is `GD.Print` → `TestLogger.Log`. This is a manual step. A future session could automate this with a source generator or shared project reference.

### Running
```
dotnet test Tests/ --verbosity normal
```
The simulation tests (`WinRateTests`) print full output including the hero matchup matrix and protocol impact table. They assert only sanity ranges (win rates between 30–85%) — the interesting data is in the output, not the pass/fail.

### Extending the suite
- **New card:** add a named shortcut to `CardFactory` and a deck factory method in `WinRateTests`.
- **New protocol:** add a class in `Logic/Rules/`, add it to `MatchConfig`, write tests in `ProtocolTests.cs`.
- **New bond/ability:** add to `Logic/Rules/`, write tests in a new `Tests/Abilities/` folder.
- **New system (Hollowing, Payroll, etc.):** add a new test file under `Tests/` in the appropriate folder.

---

## Known Issues / Tech Debt

### Logic/ sync is manual
When `Scripts/` logic changes, `Logic/` must be updated by hand. No automation yet.

### Contamination Disabled
`BondResolver.ContaminationEnabled = false` — re-enable per district when campaign layer is live:
```csharp
BondResolver.ContaminationEnabled = district.Controller == "HollowChoir";
```

### The Rivalry — double log
`BondResolver.Apply` called twice in `GameManager.PlayCard`. Cosmetic only.

### Conscription pool
AI always uses its fixed hand under Conscription — no roster to draw from.

### Shaken mechanic not implemented
Per `systems.md §5.1` — cards joining roster by capture should arrive Shaken (lowest edge = 0 for first match). Not yet built.

### Buyout not implemented
Hunt panel shows disabled Buyout button. Requires scrip economy (Phase 9).

### Hunt matches currently run district protocols
`systems.md §7.3` specifies base-capture-only for Hunt matches. Fix: one `if (session.IsHuntMatch)` branch in `GameBoard._Ready` before `BuildMatchConfig()`. Deferred to Phase 9.

### Multi-Hunt not implemented
Only one Hunt active at a time. Second hero capture while Headless is silently dropped.

### Captured Sumi never benefits P1
`OriginalOwnerId` guard prevents a won Sumi from firing Compound for P1. Revisit when cross-origin heroes are common.

### `_selectedHandIndex` dead field / `SelectCardFromHand()` dead method
Safe to remove in any cleanup pass.

### `DistrictLabel` export unwired
`GameBoard.cs` exports `DistrictLabel`; no matching node in scene. Wire or remove.

### `Bondresolver.cs` filename casing
Lowercase `r` — safe on Windows, risk on Linux builds. Rename to `BondResolver.cs`.

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

### Immediate — run the test suite
```
dotnet test Tests/ --verbosity normal
```
Review the hero matchup matrix and protocol impact table. Flag any win rate outside 40–60% on the balanced mirror match as a balance concern.

### Phase 8b — Street Cred (`systems.md §8`)
Single broad campaign stat (Nameless → Known → Named → Notorious → Legend). Affects:
- Razorkin buyout refusal probability (floor 15–20%, scales with cred)
- Ransom prices across all factions
- Contract payouts (income multiplier)
- District control shift speed
- Talent attraction (better free agents at higher cred)

### Phase 9 — Economy & Fixers (`systems.md §9`)
- Scrip as campaign currency (enables Buyout in the Hunt panel)
- 5 Fixers: Della, Vig, Atlas, Mrs. Oba, The Tailor
- Contract system as curated duels with scrip payouts
- Free agent Meet → Audition → Sign flow
- **Hunt protocol fix** (see Known Issues)

### Phase 7c — Multi-Hunt (deferred)
Multiple simultaneous Hunts, Hunt cap of 3, oldest-Hunt expiry.

### Phase 10 — The Hollowing (`systems.md §10`)
Dead Line contracts → Touched → Fading → Claimed affliction track.

### Phase 11 — Payroll & Debt (`systems.md §11`)
Upkeep per overworld turn. Debt → Collectors → escalating ladder. Mutual Aid vs Lacquer debt.

### Phase 12 — Prestige & Skyline (`systems.md §12`)
Prestige condition (Legend cred + take The Vault). Skyline rival system. Two endings.

### Near-term cleanup (any session)
- Shaken mechanic (`systems.md §5.1`)
- Contamination re-enable
- Conscription AI roster
- Faction-specific AI decks (Phase 8b+)
- Dead code: `_selectedHandIndex`, `SelectCardFromHand()`, `DistrictLabel`
- `Bondresolver.cs` → `BondResolver.cs`
- `Logic/` sync automation

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
