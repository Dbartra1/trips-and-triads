# Trips & Triads — Dev Handoff (Session 7)

*Last updated end of Session 7. Hand this document back at the start of every new session.*

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
dotnet test Tests/ --verbosity normal
```

**To run with output (simulation/balance tests):**
```
dotnet test Tests/ --filter "FullyQualifiedName~<TestClass>" --logger "console;verbosity=detailed"
```

**Current test count: ~246 tests (dotnet count), all passing.**

**Autoload required:** `Project → Project Settings → Globals` → Add `res://Scripts/GameSession.cs` as `GameSession`.

**Design bibles:** `lore.md` and `systems.md` are attached to this project.

---

## Workflow Convention

1. All changes prototype in `Logic/` and `Tests/` first
2. Confirm passing tests
3. New chat session to port confirmed changes to `Scripts/` (production)
4. File naming: just the filename, no suffix. Path clarified in text when ambiguous.

---

## Architecture Overview

### Scene Flow
```
MainMenu → PreMatchScreen → GameBoard → PostMatchScreen → PreMatchScreen
```

### Project Layout
```
trips-and-triads/
  Logic/    ← Pure C# library. No Godot dependency. Test suite source of truth.
  Tests/    ← xUnit test project. References Logic/ only.
  Scripts/  ← Godot game scripts.
  Scenes/   ← Godot scene files.
  Data/     ← cards.json, districts.json
```

**Logic/ ↔ Scripts/ sync:** Logic/ is a Godot-free copy. When Scripts/ logic changes, Logic/ must be updated manually. The key difference: `GD.Print` → `TestLogger.Log`.

---

## File Structure

```
Scripts/
  Core/
    BoardState, CardData, CardInstance, GameManager
    CardDatabase, DistrictDatabase, DistrictManager
    CrewGenerator, NameGenerator, SaveManager, StepUpPromoter
  Rules/
    ICardAbility, IProtocol, MatchConfig
    CaptureResolver, DomainResolver, BondResolver
    HandshakeProtocol, TallyProtocol, WallSignatureProtocol
    VesnaAbility, SumiAbility, LetheAbility
  GameBoard, GameSession, MainMenu
  PreMatch/PreMatchScreen
  PostMatch/PostMatchScreen
  UI/CardNode, CellNode, HandNode

Logic/                              ← Godot-free extractions
  Core/  BoardState, CardData, CardInstance, GameManager,
         CrewGenerator, NameGenerator, StepUpPromoter
  Rules/ all resolvers, protocols, abilities

Tests/
  Helpers/  CardFactory, BoardBuilder, GameSimulator
  Capture/  BaseCaptureTests, ProtocolTests, ChainTests
  Math/     EdgeValueArithmetic, CaptureBoundary, HeroAbilityProgression,
            DomainStacking, ScoreInvariant, ProtocolArithmetic,
            BondTests, StepUpPromoterTests
  Integration/ GameManagerIntegrationTests, CrewGeneratorTests,
               MatchConfigFactoryTests
  Simulation/  WinRateTests, ProceduralBalanceTests, StatScaleTests,
               Scale20PrototypeTests, Scale20ExtendedTests,
               Scale20PathATests
```

---

## What Is Built and Confirmed Working

### Core Duel + Card Systems (Phases 1–7)
Full game loop, all 29 named cards, all hero mechanics, domains, bonds, protocols, districts, campaign loop, Hunt system, save states.

### Testing Suite (~246 tests, all passing)
Three-layer structure: unit (Capture/Math), integration, simulation.

**Bugs found and fixed by the test suite:**
- `LetheAbility` copying buffed values instead of base values
- `StepUpPromoter` index collision on equal-edge cards (no A generated)
- `CardFactory.SisterGrin` missing Killzone domain type
- Lacquer hero soft edge could reach 5 (should cap at 4)
- `Scripts/Core/CrewGenerator.cs` had `GenerateAIHand`/`CloneCard` accidentally stripped

---

## Scale-20 Balance Changes — CONFIRMED IN TEST, NOT YET IN PRODUCTION

All of the following are prototyped and validated in `Logic/` and `Tests/`. They need porting to `Scripts/` in the next session.

### Stat Scale

| Constant | Scale-10 (current prod) | Scale-20 (confirmed) |
|---|---|---|
| A (max stat) | 10 | **20** |
| Street total | 10–14, edges 2–5 | **20–28, edges 4–10** |
| Pro total | 16–22, edges 2–9 | **32–44, edges 4–18** |
| Hero soft edge | 1–3 | **4–8** |
| Hero mid edge | 4–8 | **10–16** |
| HollowChoir mid | 7–9 | **14–18** |
| Vesna start | 10/10/10/10 (capped 7 in prod) | **20/20/20/20 (no cap)** |
| Vesna decay | -1/turn | **-2/turn** |
| Verity | 7/9/7/9 | **14/18/14/18** |

### Protocol Scaling (Path A v2)

| Protocol | Scale-10 | Scale-20 Path A |
|---|---|---|
| HandshakeProtocol | `tolerance=0` | **`tolerance=2`** |
| TallyProtocol | `sumTolerance=0` | **`sumTolerance=2`** |
| WallSignatureProtocol | `wallValue=10` | **`wallValue=20, sumTolerance=2`** |

`WallSignatureProtocol` in Logic already has `sumTolerance` param. Scripts version needs it added.

### Domain Bonuses

`DomainResolver.BonusMultiplier = 2` doubles all domain bonuses. Currently implemented in `Logic/Rules/DomainResolver.cs`. Needs porting to `Scripts/Rules/DomainResolver.cs`. Decision pending: use multiplier=2 (marginal, 2–3% domain lift) or multiplier=3 (crosses "meaningful" threshold). Data supports either.

### Balance Results (10,000 games each)

| Metric | Scale-10 prod | Scale-20 Path A |
|---|---|---|
| Player win rate vs AI | 19.4% | **48.2%** |
| Handshake fire rate | 0.085 | **0.081** ✓ |
| Tally fire rate | 0.173 | **0.145** ✓ |
| Wall Signature fire rate | 0.470 | **0.631** ✓ |
| Cascade fire rate | 0.111 | **0.127** ✓ |

6 of 8 districts in 40–65% target. Killfloor (31.5%) and Sprawl Market (32.8%) both use Conscription — deliberately risky, accepted.

---

## Priority Task List for Next Session

**1. Full code review + math verification**
Run a thorough review of the repo, focusing on:
- Test suite correctness — verify the math tests actually test what they claim
- Logic/ ↔ Scripts/ sync — confirm all Logic/ changes are reflected in Scripts/
- Known issues list below

**2. Port Scale-20 balance changes to production**
Files to update in `Scripts/`:
- `Core/CrewGenerator.cs` — double all stat constants
- `Core/GameManager.cs` — remove VesnaStartingCap kludge (default to 10, no-op)
- `Rules/VesnaAbility.cs` — decay -2/turn (already done in Scripts from earlier session)
- `Rules/HandshakeProtocol.cs` — add `tolerance` param
- `Rules/TallyProtocol.cs` — add `sumTolerance` param
- `Rules/WallSignatureProtocol.cs` — add `sumTolerance` param (wallValue already exists if added)
- `Rules/DomainResolver.cs` — add `BonusMultiplier`
- `Data/Cards/cards.json` — update Vesna and Verity stats
- `Core/DistrictManager.cs` — update MatchConfig factory calls to use new protocol params

**3. AI card play delay**
Add a configurable delay (suggested 0.5–1.0s) before the AI places each card. Should feel like the AI is "thinking." Implement in `Scripts/GameBoard.cs` using `await ToSignal(GetTree().CreateTimer(delay), "timeout")` or equivalent Godot timer pattern.

**4. Card flip animation**
When a card is captured (OwnerId changes), it should physically flip over and change to the capturing player's color. Implement in `Scripts/UI/CardNode.cs`. Suggested: scale X to 0 (half-flip), swap color/sprite, scale X back to 1. Use Godot `Tween`.

**5. Drag-to-play input**
Replace click-to-play with drag-and-drop:
- Player drags a card from hand to a board cell
- AI mimics this with an animated card movement from its hand position to the board
- Implement in `Scripts/UI/CardNode.cs` (drag source) and `Scripts/UI/CellNode.cs` (drop target)

**6. Fix save system for distributed builds**
`SaveManager.cs` currently uses `user://savegame.json`. This works in editor but may fail in distributed builds if the path isn't resolved correctly. Investigate `OS.GetUserDataDir()` and ensure the path works across platforms. Also audit for any hardcoded paths.

---

## Known Issues / Tech Debt

### Contamination disabled
`BondResolver.ContaminationEnabled = false`. Re-enable per district when Hollowing system is built.

### Hunt matches run district protocols
Should be base-capture only per `systems.md §7.3`. Fix: `if (session.IsHuntMatch)` branch before `BuildMatchConfig()` in `GameBoard._Ready`.

### Multi-Hunt not implemented
Second hero capture while Headless is silently dropped.

### Shaken mechanic not implemented
Per `systems.md §5.1`.

### Conscription AI
AI uses a fixed hand under Conscription — no roster to draw from.

### Bondresolver.cs filename casing
Lowercase `r` — Linux build risk.

### Logic/ sync is manual
No automation. When Scripts/ changes, Logic/ must be updated by hand.

### `_selectedHandIndex` dead field / `SelectCardFromHand()` dead method
Safe to remove.

### `DistrictLabel` export unwired in GameBoard.cs

---

## Balance Constants (Scale-20, Logic — not yet in Scripts)

| Constant | Location | Value |
|---|---|---|
| StreetMin/Max | `CrewGenerator.cs` | 20–28 |
| StreetEdgeMin/Max | `CrewGenerator.cs` | 4–10 |
| ProMin/Max | `CrewGenerator.cs` | 32–44 |
| ProEdgeMin/Max | `CrewGenerator.cs` | 4–18 |
| A (hero max) | `CrewGenerator.cs` | 20 |
| SoftMin/Max | `CrewGenerator.cs` | 4–8 |
| MidMin/Max | `CrewGenerator.cs` | 10–16 |
| VesnaStartingCap | `GameManager.cs` | 10 (no-op) |
| Vesna decay | `VesnaAbility.cs` | -2/turn |
| BonusMultiplier | `DomainResolver.cs` | 2 |
| Handshake tolerance | `MatchConfig factories` | 2 |
| Tally sumTolerance | `MatchConfig factories` | 2 |
| WallSig wallValue | `MatchConfig factories` | 20 |
| WallSig sumTolerance | `MatchConfig factories` | 2 |

---

## Phases Not Yet Built

- **Phase 8b** — Street Cred (`systems.md §8`)
- **Phase 9** — Economy & Fixers (`systems.md §9`)
- **Phase 7c** — Multi-Hunt
- **Phase 10** — The Hollowing (`systems.md §10`)
- **Phase 11** — Payroll & Debt (`systems.md §11`)
- **Phase 12** — Prestige & Skyline (`systems.md §12`)

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
