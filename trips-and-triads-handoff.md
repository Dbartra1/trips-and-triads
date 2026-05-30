# Trips & Triads — Dev Handoff (Sessions 8–9)

*Last updated end of session 9. Hand this document back at the start of every new session.*

---

## Quick Start

**Repo:** https://github.com/Dbartra1/trips-and-triads  
**Engine:** Godot 4.6.3 stable mono  
**Language:** C# (.NET / net8.0)  
**Editor:** Cursor  
**Platform:** Windows + RTX 4080

**To run:** Open project in Godot → Play. Main scene is `Scenes/MainMenu.tscn`.

**Autoload required:** `Project → Project Settings → Globals` → Add `res://Scripts/GameSession.cs` as `GameSession`. Crash on launch if missing.

**Tests:** `dotnet test Tests/ --verbosity normal` from repo root. 368 tests, all green as of session 9.

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
| `GameSession` (Autoload) | `Scripts/GameSession.cs` | All persistent campaign state: Roster, Deck, CredManager, Hunt, StandoffHands, DistrictGracePeriods |
| `CardDatabase` | `Scripts/Core/CardDatabase.cs` | Loads `Data/Cards/cards.json` once |
| `DistrictDatabase` | `Scripts/Core/DistrictDatabase.cs` | Loads `Data/Districts/districts.json` once |
| `DistrictManager` | `Scripts/Core/DistrictManager.cs` | Control meters, MatchConfig building, Spreading Rule |

### Namespaces
- `TripsAndTriads.Core` — game logic (BoardState, CardData, CardInstance, GameManager, CrewGenerator, StepUpPromoter, CredManager, etc.)
- `TripsAndTriads.Rules` — rules engine (CaptureResolver, DomainResolver, BondResolver, protocols, abilities)
- `TripsAndTriads.UI` — visual nodes (CardNode, CellNode, HandNode, CredBarNode, KillFeedNode)
- No namespace — top-level scene scripts (GameBoard, PreMatchScreen, PostMatchScreen, MainMenu, GameSession)

---

## File Structure

```
Scripts/
  Campaign/
    CredTier.cs           — enum: Nameless/Known/Named/Notorious/Legend
    CredEvents.cs         — CredEvent enum + DeltaFor() lookup; StepUpResetValue=20 (unused in prod)
    CredEffects.cs        — pure static: IncomeMultiplier, DebtInterestRate, RansomDiscount, ControlShiftMultiplier
    CredManager.cs        — owns 0–100 cred int; Apply(), ApplyEvents(), StepUpReset(), TierFor()
    RazorkinRefusal.cs    — Floor=0.18, RefusalChance(tier), IsRefused(tier, rng)
    DistrictAccess.cs     — gate rules per district (AlwaysOpen/HardLock/SoftGate); GraceMatches=3
  Core/
    BoardState.cs         — 3×3 grid
    CardData.cs           — template; Faction, Tier, DomainType, AbilityType enums
    CardDatabase.cs       — singleton, loads cards.json
    CardInstance.cs       — in-play card: OwnerId, OriginalOwnerId, overrides, domain/bond bonuses
    CrewGenerator.cs      — Generate(), SelectBestFive(), GenerateFactionHand(controller), GeneratePro()
    DistrictData.cs       — district template model
    DistrictDatabase.cs   — singleton, loads districts.json
    DistrictManager.cs    — control meters, BuildMatchConfig() (Scale-20 params), ApplySpreading()
    GameManager.cs        — turn manager; VesnaStartingCap=14; DealHands; PlayCard; LastTurnEvents
    NameGenerator.cs      — procedural names
    SaveManager.cs        — JSON save/load at user://savegame.json; persists cred + grace periods
    StepUpPromoter.cs     — promotes highest-stat non-hero to Hero (A=20, soft cap=6)
  Rules/
    ICardAbility.cs / IProtocol.cs
    CaptureResolver.cs    — base capture, protocols, Cascade, Rivalry, Breach, Listener; records CaptureEvents
    DomainResolver.cs     — AegisProtocol, Killzone, LateralGrid, Sprawl; BonusMultiplier=1
    BondResolver.cs       — all 7 bonds; ContaminationEnabled=false
    MatchConfig.cs        — active protocols + flags + factory methods (Scale-20 confirmed params)
    HandshakeProtocol.cs  — tolerance=2 (Scale-20)
    TallyProtocol.cs      — sumTolerance=2
    WallSignatureProtocol.cs — wallValue=20, sumTolerance=2
    VesnaAbility.cs / SumiAbility.cs / LetheAbility.cs
  GameBoard.cs            — match runner; AI delay (0.75s); card flip animation; drag-to-play;
                            SaveStandoffHands(); faction AI; kill feed; hero capture + Hunt injection
  GameSession.cs          — Autoload: Roster, Cred, Hunt state, StandoffHands, DistrictGracePeriods,
                            StepUp(), ReclaimHero(), TickGracePeriods(), IsDistrictAccessible()
  MainMenu.cs
  PreMatch/
    PreMatchScreen.cs     — district picker (cred-gated), deck builder (Hero→Pro→Street order),
                            Hunt panel, Reunion banner, CredBar, grace period popup, Hunt reminder popup
  PostMatch/
    PostMatchScreen.cs    — result display, ApplyCredEvents(), TickGracePeriods()
  UI/
    CardNode.cs           — card visual; FlipToOwner() flip animation; drag source
    CellNode.cs           — board cell; drop target; FlipCard()
    HandNode.cs           — hand display; drag wiring; GetCardGlobalPosition()
    CredBarNode.cs        — CITY SIGNAL bar: 5 columns, pulse + scan animation, Refresh(CredManager)
    KillFeedNode.cs       — "The Wire" scrollable capture log; PushEvents(List<CaptureEvent>)

Data/
  Cards/cards.json        — 29 named cards, all Scale-20 (0–20 stat range)
  Districts/districts.json — 8 districts with controller, stake, protocols, conscription flags
```

---

## What Is Built and Confirmed Working

### Core Duel Loop
3×3 board, alternating turns, 5-card hands, drag-to-play, AI thinking delay, greedy capture AI, end panel.

### Card Systems (Phases 1–4)
29 named cards, all Scale-20. Hero mechanics: Vesna decay (OriginalOwnerId guard — only fires on owner's turn), Sumi compound, Lethe copy. Domain system, all 7 bonds.

### Protocols (Phase 5) — Scale-20 parameters
Handshake (tolerance=2), The Tally (sumTolerance=2), Wall Signature (wallValue=20, sumTolerance=2), Cascade, Intercept, Conscription, Standoff. `BuildProtocol()` in DistrictManager uses Scale-20 params. CaptureResolver records CaptureEvent for every capture.

### Districts (Phase 6)
8 districts, control meters, Spreading Rule scaled by `CredEffects.ControlShiftMultiplier`, MatchConfig factory, VesnaStartingCap per district (20 in The Hush, 14 elsewhere).

### Faction AI Crews (Phase 8b extension)
`CrewGenerator.GenerateFactionHand(controller)` matches enemy crew to district controller:
- Neutral → procedural hero + Streets (no named heroes)
- Contested (The Vault) → random apex faction
- Each faction → their named hero + 1–2 named top-tier + Streets

### Campaign Loop (Phase 7 — complete)
All 4 stakes: OneJob, TheSpread, AsFlipped, Everything (now includes unplayed hand card). Hunt system: hero capture, Reclaim (2 attempts), Step Up promotion, cred preserved through succession. Standoff rematch uses board-state hands (not fresh deck draw).

### Street Cred (Phase 8b — complete)
- `CredManager` — 0–100 clamped integer; tier derived from value
- `CredEvents` — 7 event types, all tested
- `CredEffects` — 4 downstream effects; all tested
- `RazorkinRefusal` — probabilistic buyout refusal; all tested
- **CITY SIGNAL bar** — 5-column signal meter with pulse + scan animation; completed tiers stay full height
- Fires in `PostMatchScreen.ApplyCredEvents()` after every match
- Cred preserved through Step Up (no reset)
- Saved and restored by `SaveManager`

### District Access Gating (Phase 8b extension)
`DistrictAccess.cs` defines gate rules per district. Grace period: 3 matches after dropping below threshold before hard lock. `TickGracePeriods()` called after every match. Pre-seeded on new run AND on save load to prevent false "newly dropped" warnings. PreMatchScreen shows lock icon, amber grace countdown, and one-time popup.

### Save System (Phase 8a + 8b additions)
Persists: Roster, Hunt state, Reunion state, district meters, active district, cred value, grace periods. Load pre-seeds missing grace entries for saves from before the grace system existed.

### UI Polish (Phase 8 series)
- Drag-to-play with centered ghost cursor (loaded from scene, not `new CardNode()`)
- Card flip animation: `PivotOffset=(60,80)` centered, 0.4s total, Sine/InOut
- AI card animates from hand to cell (Cubic/EaseOut, 0.3s)
- Kill feed: permanent scrollable log, auto-scrolls to latest, 5px entry padding
- PreMatchScreen: 24px margin, Hero→Pro→Street deck order, roster card dimming, cred-gated district buttons
- Hunt reminder popup (centered, both paths wired)
- Grace period warning popup

---

## Known Issues / Tech Debt

### Contamination Disabled
`BondResolver.ContaminationEnabled = false`. Re-enable when campaign districts are live:
```csharp
BondResolver.ContaminationEnabled = district.Controller == "HollowChoir";
```

### Hunt Matches Inherit District Protocols (deferred)
Hunt (Reclaim) matches should run base-capture only. One-line fix:
```csharp
// In GameBoard._Ready(), before BuildMatchConfig():
if (session?.IsHuntMatch == true)
    _matchConfig = new MatchConfig();
else
    _matchConfig = DistrictManager.Instance.BuildMatchConfig();
```

### Multi-Hunt Not Implemented
Only one hero Hunt active at a time. Second capture while Headless silently drops. Needs `List<HuntEntry>` + cap + expiry + selector UI.

### Shaken Not Implemented
`systems.md §5.1` — cards joining roster by capture should arrive Shaken (lowest edge = 0 for first match; 3rd Shaken → permanent Calloused).

### Conscription AI Has No Roster
AI always uses its generated hand under Conscription. Needs persistent AI roster.

### Scale20prototypetests.cs Warning
CS0219: unused variable `totalProtocolCaptures` at line 149. Remove `var totalProtocolCaptures = 0;`.

### DistrictAccess Lore Open Threads (marked in DistrictAccess.cs)
- Glass Spire: what does Ascendant "verified identity" look like mechanically?
- Dead Channel: why does Ghostwire care about cred tiers specifically?
- The Powder Room: Lacquer locking out desperate crews is off-brand; rethink as Fixer-vouch alternative?
- The Hush: the Choir's awareness of cred should be explained via the Antecedent noticing the crew

---

## Balance Constants

| Constant | Location | Value |
|---|---|---|
| `VesnaStartingCap` | `GameManager.cs` | 14 default; 20 in The Hush |
| `AiThinkDelay` | `GameBoard.cs` | 0.75f (tunable [Export]) |
| `StreetMin/Max` | `CrewGenerator.cs` | 20–28 (Scale-20) |
| `ProMin/Max` | `CrewGenerator.cs` | 32–44 (Scale-20) |
| `BonusMultiplier` | `DomainResolver.cs` | 1 (set to 2 for Scale-20 matches if needed) |
| `GraceMatches` | `DistrictAccess.cs` | 3 |
| `StepUpResetValue` | `CredEvents.cs` | 20 (constant exists; not called in prod — cred preserved) |
| Reclaim attempts | `GameSession.SetCapturedHero` | 2 |
| Step Up soft edge cap | `StepUpPromoter.Promote` | 6 (Scale-20) |
| Handshake tolerance | Protocol constructors | 2 (Scale-20) |
| Tally sumTolerance | Protocol constructors | 2 |
| WallSignature wallValue | Protocol constructors | 20 |

---

## What's Next (Priority Order)

### Immediate — Hunt protocol fix (1 line, deferred from Phase 7)
In `GameBoard._Ready()`, before `BuildMatchConfig()` — see Known Issues above.

### Phase 9 — Economy & Fixers (`systems.md §9`)
Recommended build order:
1. **Scrip** — add int to `GameSession`, persist in `SaveManager`, display in PreMatchScreen
2. **Post-match payout** — `PostMatchScreen` awards scrip using `CredEffects.IncomeMultiplier(tier)` × district danger multiplier
3. **Buyout** — enables the disabled Buyout button already in the Hunt panel (currently labelled "Phase 9")
4. **Della (Standing Work)** — simplest Fixer; rotating contracts, flat scrip; also the Mutual Aid safety net
5. **Remaining Fixers** — Vig (Wagers), Atlas (Intel/Hunt location), Mrs. Oba (Long Account/debt), The Tailor (Ghost Contracts)
6. **Free agent Meet → Audition → Sign flow**

### Phase 7c — Multi-Hunt
Multiple simultaneous Hunts, cap of 3, oldest-Hunt expiry.

### Phase 10 — The Hollowing
Dead Line contracts → Touched → Fading → Claimed affliction track.

### Phase 11 — Payroll & Debt
Upkeep per overworld turn. Debt → Collectors → escalating ladder. Mutual Aid vs Lacquer debt.

### Phase 12 — Prestige & Skyline
Prestige condition (Legend cred + take The Vault). Skyline rival system. Two endings.

### Near-term cleanup
- Shaken mechanic (`systems.md §5.1`)
- Contamination re-enable (wire to district controller check)
- Conscription AI roster
- Hunt protocol fix
- Remove unused `totalProtocolCaptures` variable warning

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
