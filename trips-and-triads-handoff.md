# Trips & Triads — Dev Handoff (Session 6)

*Last updated end of context window 6. Hand this document back at the start of every new session.*

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
- Run-over condition: roster < 5 → message replaces grid, button rewired to New Run

### Save States (Phase 8a — confirmed working)
Full implementation of campaign persistence via `Scripts/Core/SaveManager.cs`.

**`CardData.ShallowClone()`** — used by SaveManager to clone database templates before applying saved mutations.

**`DistrictManager.GetAllMeters()` / `SetMeter()`** — expose control meter state for save/load.

**`GameSession` load methods** — `LoadRoster`, `LoadHuntState`, `LoadInterimHero`, `LoadDeckSnapshot`, `LoadReunionState` — called exclusively by SaveManager.

**Save triggers:**
- `ApplyStakeResult()` — saves after every match (roster changes)
- `InitializeNewRun()` — deletes existing save (fresh start)
- `_Ready()` — attempts `SaveManager.LoadGame()` on startup; falls back to `InitializeNewRun()` if no save exists

**What is saved:** Roster (full CardData including procedural and mutated stats), Hunt state (captured hero stored in full — not a roster reference — captor faction, reclaim attempts, interim hero, deck snapshot), Reunion state, district control meters, active district ID.

**Save format:** JSON at `user://savegame.json`. Named cards use database Id as key. Procedural cards (all player-generated cards) use Name as key — `CardKey()` handles this automatically.

### MainMenu (Sessions 5–6)
`Scripts/MainMenu.cs` and `Scenes/MainMenu.tscn` — **Continue** button appears only when a save file exists; goes directly to PreMatchScreen with loaded state. **New Run** calls `InitializeNewRun()` (deletes save, fresh crew), then goes to PreMatchScreen. `project.godot` — `run/main_scene` = `res://Scenes/MainMenu.tscn`.

### AI Hand Always Visible (Session 6)
`GameBoard.tscn` — added `AIHandContainer` + `AIHandNode` (second HandNode instance) positioned above the board (offset_top = -200 to -8, symmetrically opposite the player hand at 680–872). `[Export] public HandNode AIHand` wired to `CanvasLayer/AIHandContainer/AIHandNode`.

`GameBoard.cs` — `RefreshAIHand()` called after `DealHands`: populates AI hand via `AIHand.PopulateHand(_game.GetHand(2))` then calls `AIHand.SetInteractive(false)`. In `RunAI()`, `AIHand?.RemoveCard(bestHandIndex)` is called before `PlayCard` so the display stays in sync as the AI plays out.

`HandNode.cs` — `SetInteractive(bool)` strips the invisible click-button overlay from every card node in the hand. When `false`, removes the Button child from each CardNode so cards are visible but unclickable.

### Lethe Base-Value Fix (Session 6)
`LetheAbility.cs` — `OnPlaced` now uses `GetBaseValue()` instead of `GetValue()` for both the total-comparison loop and the override assignment. Per `lore.md §7`: "copies the four numbers only — not Domains or bonds." `GetValue()` was including transient domain/bond bonuses (e.g., a Yune under Aegis Protocol would give Lethe buffed values); `GetBaseValue()` is correct.

### New Run → MainMenu routing (Session 6)
`PreMatchScreen.OnNewRun` — changed `ReloadCurrentScene()` to `ChangeSceneToFile("res://Scenes/MainMenu.tscn")`. When a run ends and the player clicks "Run Over — New Run", they now always return to the main menu rather than reloading PreMatchScreen in place.

### Other Session 5 fixes (still valid)
- **GameBoard redirect fix** — no valid 5-card deck → `CallDeferred` redirect to PreMatchScreen
- **Sumi ability fix** — `OriginalOwnerId != CurrentPlayerId` guard prevents captured Sumi from buffing enemy team
- **StartButton double-wire fix** — `if (_isRunOver) return true` early return in `CheckRunOver`
- Full Hunt system: capture → Headless → Step Up → Reclaim → Reunion banner
- Existing heroes designatable as interim (no stat mutation)

---

## Known Issues / Tech Debt

### Contamination Disabled
`BondResolver.ContaminationEnabled = false` — intentional. Re-enable per district when campaign layer is live:
```csharp
BondResolver.ContaminationEnabled = district.Controller == "HollowChoir";
```

### The Rivalry — double log
`BondResolver.Apply` called twice in `GameManager.PlayCard`. Cosmetic only, no gameplay impact. The first call exists for Contamination (disabled); the second follows DomainResolver. Will become non-redundant when Contamination is re-enabled.

### Conscription pool
AI always uses its fixed hand under Conscription — AI has no roster to draw from.

### Shaken mechanic not implemented
Per `systems.md §5.1` — cards joining roster by capture should arrive Shaken (lowest edge = 0 for first match). Not yet built.

### Buyout not implemented
Hunt panel shows disabled Buyout button with "Phase 9" label. Full buyout requires scrip economy (Phase 9).

### Hunt matches currently run district protocols
Hunt (Reclaim) matches inherit the district's active protocols. `systems.md §7.3` specifies only "AsFlipped, hero-stake rule" with no protocols. Fix: one `if (session.IsHuntMatch)` branch before `_matchConfig = DistrictManager.Instance.BuildMatchConfig()` in `GameBoard._Ready`, building a clean base-capture-only config instead. Deferred to Phase 9.

### Multi-Hunt not implemented
Only one Hero Hunt can be active at a time. Second hero capture while Headless is silently dropped (card removed from roster, no Hunt opens). Phase 7c: `List<HuntEntry>` in `GameSession`, Hunt cap of 3, oldest-Hunt expiry, Hunt selector UI in PreMatchScreen.

### AI hand position may need scene layout tuning
`AIHandContainer` uses `offset_top = -200.0` (above the board's 80px top). Depending on the window resolution this may clip. Tune the offset in the scene editor — the hand should sit in the negative-Y space above the board container (which starts at Y=80).

### Captured Sumi never benefits P1 after being won
`ApplyTurnEndAbilities` guards on `OriginalOwnerId == CurrentPlayerId`. A Sumi won from the AI (OriginalOwnerId=2) will never fire her Compound for P1. This is the conservative choice for a pre-campaign game; revisit when the roster system makes cross-origin heroes common.

### `_selectedHandIndex` dead field
`GameBoard._selectedHandIndex` is set but never read — the actual index is always computed via `hand.IndexOf`. Safe to remove in any cleanup pass.

### `SelectCardFromHand()` dead method
`GameBoard.SelectCardFromHand()` is never called anywhere. Safe to remove.

### `DistrictLabel` export unwired
`GameBoard.cs` exports `DistrictLabel` but `GameBoard.tscn` has no matching node. The code null-checks it. Wire a label node in the scene or remove the export.

### Bondresolver.cs filename casing
`Scripts/Rules/Bondresolver.cs` — lowercase `r`. Safe on Windows; Linux builds will fail if the namespace import ever case-sensitively fails. Rename to `BondResolver.cs` in a cleanup pass.

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

### Immediate — Testing suite
Dedicated Claude Code session. Automate game-state assertions and gameplay simulations to produce data on win rates, capture patterns, and system edge cases.

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
- **Dead code cleanup**: remove `_selectedHandIndex`, `SelectCardFromHand()`, wire or remove `DistrictLabel`
- **Bondresolver.cs rename**: `Bondresolver.cs` → `BondResolver.cs` for Linux safety

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
