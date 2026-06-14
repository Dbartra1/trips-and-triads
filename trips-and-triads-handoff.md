# Trips & Triads — Dev Handoff (Session 10)

*Last updated end of session 10. Hand this document back at the start of every new session.*

---

## Quick Start

**Repo:** https://github.com/Dbartra1/trips-and-triads
**Engine:** Godot 4.6.3 stable mono
**Language:** C# (.NET / net8.0)
**Editor:** Cursor
**Platform:** Windows + RTX 4080

**To run:** Open project in Godot → Play. Main scene is `Scenes/MainMenu.tscn`.

**Autoload required:** `Project → Project Settings → Globals` → Add `res://Scripts/GameSession.cs` as `GameSession`. Crash on launch if missing.

**Tests:** `dotnet test Tests/ --verbosity normal` from repo root. **369 tests expected** (368 + 1 new this session) — **please run this and confirm green before starting the next session.** The sandbox this session ran in has no NuGet access, so the fixes below were done by careful static review, not by running the suite. See "Session 10" below for what to watch for if anything is still red.

**Design bibles:** `lore.md` and `systems.md` are attached to this project. Read both before touching faction mechanics, card stats, or campaign systems.

---

## Session 10 — Test Suite Trustworthiness (this session)

This session was entirely foundation work: settling the `Logic/` vs `Scripts/` question before building anything new, per the standing rule that mechanics need a trustworthy suite under them.

### Decision: Logic/ is retired (not re-synced)

**`Logic/` has been deleted from the repo.** Rationale:

- `Tests/TripsAndTriads.Tests.csproj` already points its `ProjectReference` at the root `Trips_and_Triads.csproj` (the Godot/Scripts project) as of commit `5323819` — the dual-layer prototype workflow was already abandoned in practice, just not formally closed out.
- `Logic/` was missing `CardDatabase`, `DistrictData`, `DistrictDatabase`, `DistrictManager`, `SaveManager`, `FreeAgent`, `FreeAgentGenerator`, and `DistrictAccess` — re-syncing it would have meant porting six-plus files and then maintaining two copies of everything going forward.
- `Logic/TestLogger.cs` was only referenced from within `Logic/` itself (all `Tests/` references to it are commented out) — nothing outside `Logic/` depended on it.

**New workflow (replaces the old prototype-first rule):** `Tests/` now compiles directly against `Scripts/`. Prototype changes **in `Scripts/` + `Tests/` together**, run `dotnet test`, confirm green, then verify in Godot. There is no longer a separate porting step — this is simpler than the old two-layer setup, not a downgrade. (If you'd like this reflected in Claude's stored memory/preferences too, just say so — the standing instruction about "prototype in Logic/Tests/ first, then port to Scripts/" should be retired alongside the directory.)

Also cleaned up: `Trips_and_Triads.csproj` no longer excludes `Logic/**` (the exclusion is gone since the directory is gone), and two stray comments in `Tests/Simulation/Statscaletests.cs` and `Tests/Simulation/Scale20pathatests.cs` that referenced `Logic/` were reworded.

### Bug found and fixed: `StepUpPromoter.Level` was set to 20

While auditing `StepUpPromoter.cs` against its tests, found that `ApplyPromotion()` set `best.Level = 20`. **`Level` is a tier-band marker (lore.md §3: Street ≈1–5, Pro ≈6–7, Hero=10), not a Scale-20 edge stat** — `CrewGenerator` consistently uses `Level = 10` for generated heroes, and the StepUpPromoter doc comment itself said "Level set to 10." The `20` was very likely an artifact of a global "double everything for Scale-20" pass that shouldn't have touched this field.

**Fixed:** `best.Level = 10`, with a comment explaining why it does *not* scale. This also means `Promote_LevelBecomesToen` (now renamed `Promote_LevelBecomesTen`) needed no assertion change — only the source was wrong.

### Stale Scale-10 test assertions fixed

Two files asserted against pre-Scale-20 constants for algorithms (`CrewGenerator`, `StepUpPromoter`) that were already migrated to Scale-20 in production. Both are corrected files, ready to drop in:

**`Tests/Integration/CrewGeneratorTests.cs`** — `CrewGenerator`'s real constants are `StreetMin/Max=20/28` (edges 4–10) and `ProMin/Max=32/44` (edges 4–18), and Hero "A" = 20 with soft edges drawn from 4–8 (Lacquer: 6–8). The test file was still asserting the old Scale-10 bands (10–14 / 16–22, edges 2–5 / 2–9, A==10, soft≤5). Renamed and corrected 7 tests:
- `Street_TotalAlwaysInBand_10to14` → `Street_TotalAlwaysInBand_20to28`
- `Street_AllEdgesAtLeast2` / `_AtMost5` → `_AtLeast4` / `_AtMost10`
- `Pro_TotalAlwaysInBand_16to22` → `Pro_TotalAlwaysInBand_32to44`
- `Pro_AllEdgesAtLeast2` / `_AtMost9` → `_AtLeast4` / `_AtMost18`
- `Hero_AlwaysHasExactlyOneOrTwoA` — now checks `e == 20`
- `Hero_AlwaysHasAtLeastOneSoftEdge_AtMostFive` → `_AtMostEight` — now checks `e <= 8`

(`Hero_TierIsHero_LevelIsTen` and the HollowChoir Toll==0 test were already correct — `Level` and the Toll don't scale.)

**`Tests/Math/StepUpPromoterTests.cs`** — `StepUpPromoter` promotes the highest edge to **A=20** and caps the lowest *remaining* edge to **6** (not the old A=10/cap=3). Fixed all affected assertions:
- The four `Promote_HighestEdge_*` tests now expect `20`.
- `Promote_LowestEdgeAboveThree_CappedToThree` / `_AlreadyThree_Unchanged` → replaced with `Promote_LowestEdgeAboveSix_CappedToSix` / `_AlreadySix_Unchanged`, using stat lines that actually exercise the 6-cap boundary. (`_LowestEdgeBelowThree_LeftAlone` and `_LowestEdgeTwo_LeftAlone` needed no change — values below 6 were already left alone either way, just renamed the "below three" one to "below six" for accuracy.)
- `Promote_MiddleEdges_Unchanged` — Top now expected as `20`.
- `PreviewPromotion_ReturnsCorrectProjectedStats` — corrected to `20`/unchanged-at-4 (was wrongly expecting a cap that doesn't trigger below 6); added a new `PreviewPromotion_CapsLowestAboveSix` test to cover the capping branch that the old test missed.
- `Promote_LateralCard_HighestSideBecomesA_LowestSoftened` — Right now expected as `20`.
- `Promote_EvenCard_HighestEdgeGetsA_LowestCapped` — rewritten to use Mara Kane's actual Scale-20 stat line (`12/12/12/12`, i.e. lore.md's `6/6/6/6` ×2) so the A==20 and cap-to-6 checks are both meaningfully exercised.
- Class-level doc comment updated to describe the Scale-20 promotion rules (A=20, cap=6, Level=10).

### What was *not* changed (and why)

`Tests/Helpers/CardFactory.cs` still defines hero fixtures at their original lore.md Scale-10 values (Vesna=10/10/10/10, Seraph Yune=10/8/3/8, Mara Kane=6/6/6/6, etc.), and dozens of tests in `Tests/Math/`, `Tests/Capture/`, etc. assert against those fixture values directly (e.g. `HeroAbilityProgressionTests`, `BondTests`). **These are not stale** — they're a self-contained test fixture scale, internally consistent with the ability/protocol *rates* under test (e.g. Vesna's -2/turn decay, which is itself the correct Scale-20 rate, just applied to a smaller starting fixture). They don't assert against `CrewGenerator` or `StepUpPromoter`'s production constants, so there's no mismatch. Left untouched — rewriting `CardFactory` to Scale-20 fixtures would be a much larger, separate effort with no correctness payoff.

### If `dotnet test` still shows red after this

The fixes above were derived by reading `Scripts/Core/CrewGenerator.cs` and `Scripts/Core/StepUpPromoter.cs` directly and tracing through `ApplyPromotion`'s algorithm by hand for each test's inputs — but this was **not verified by actually running the suite** (no NuGet access in this session's sandbox). If something is still red:
1. Check whether the failure is in `CrewGeneratorTests.cs` or `StepUpPromoterTests.cs` first — paste the exact assertion and actual value, and we can re-derive it.
2. Otherwise it's likely unrelated pre-existing flakiness (e.g. a `Random(42)` seed producing an edge case in 500 samples) — report the failing test name and we'll dig in.

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
    StepUpPromoter.cs     — promotes highest-stat non-hero to Hero (A=20, soft cap=6, Level=10)
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

(Logic/ — retired in Session 10; see above.)
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

### Economy (Phase 9 — partial, see below)
Scrip persistence, post-match payout (Cred × Danger multiplier), buyout integration with Razorkin refusal, Della's Standing Work (3-match limit), Free Agent Recruitment (Meet → Audition → Sign).

---

## Known Issues / Tech Debt

### Contamination Disabled
`BondResolver.ContaminationEnabled = false`. Re-enable when campaign districts are live:
```csharp
BondResolver.ContaminationEnabled = district.Controller == "HollowChoir";
```

### Multi-Hunt Not Implemented
Only one hero Hunt active at a time. Second capture while Headless silently drops. Needs `List<HuntEntry>` + cap + expiry + selector UI.

### Shaken Not Implemented
`systems.md §5.1` — cards joining roster by capture should arrive Shaken (lowest edge = 0 for first match; 3rd Shaken → permanent Calloused).

### Conscription AI Has No Roster
AI always uses its generated hand under Conscription. Needs persistent AI roster.

### Minor Test Warning
`Scale20prototypetests.cs` line 149: CS0219 unused variable `totalProtocolCaptures`. (Cosmetic, does not affect gameplay).

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
| Step Up Level (promoted hero) | `StepUpPromoter.Promote` | 10 (tier-band marker; **fixed this session**, was 20) |
| Handshake tolerance | Protocol constructors | 2 (Scale-20) |
| Tally sumTolerance | Protocol constructors | 2 |
| WallSignature wallValue | Protocol constructors | 20 |

---

## What's Next (Priority Order)

**First action of next session:** run `dotnet test Tests/ --verbosity normal` and confirm green (see "Session 10" notes above for what to do if not). Once confirmed, pick a track:

- **Track A — Correctness items:** Shaken mechanic (`systems.md §5.1`), Contamination re-enable (wire to district controller check), Hunt-protocol stripping (Hunt matches should be base-capture-only AsFlipped per `systems.md §7.3`, not inherit district protocols), Conscription AI persistent roster, Multi-Hunt (`List<HuntEntry>`, cap 3, expiry).
- **Track B — Fixer/Contract system:** Build out the remaining Fixers (Vig/Wagers, Atlas/Intel, Mrs. Oba/Long Account+debt, The Tailor/Ghost Contracts) and the curated-duel Contract system (`systems.md §9`). The TabContainer UI is already built to accommodate these.

### Phase 11 — Payroll & Debt (after Track A/B)
Upkeep per overworld turn. Debt → Collectors → escalating ladder. Mutual Aid (Della) vs Lacquer debt resolution.

### Phase 10 — The Hollowing
Dead Line contracts → Touched → Fading → Claimed affliction track.

### Phase 12 — Prestige & Skyline
Prestige condition (Legend cred + take The Vault). Skyline rival system. Two endings.

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
