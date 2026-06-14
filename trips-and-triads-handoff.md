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

**Tests:** `dotnet test Tests/ --verbosity normal` from repo root. ~369 tests expected. Run and confirm green before starting any new work.

**Design bibles:** `lore.md` and `systems.md` are in the repo root. Read both before touching faction mechanics, card stats, or campaign systems. The scope additions from session 10 are fully documented in `systems.md §15–§20`.

---

## Session 10 Summary

### Changes landed (commit `af85eea`)
- **`Logic/` retired.** 29 files deleted. `Tests/` now compiles directly against `Scripts/` via the root `Trips_and_Triads.csproj`. No porting step needed going forward — prototype in `Scripts/` + `Tests/` together.
- **`Trips_and_Triads.csproj`** — removed stale `Compile Remove="Logic/**"` exclusion.
- **`Scripts/Core/StepUpPromoter.cs`** — `Level = 20` (Scale-20 hero tier value, consistent with simulation tests). Doc comment updated with NOTE explaining Level inconsistency.
- **`Tests/Integration/CrewGeneratorTests.cs`** — Scale-10 stat bands corrected to Scale-20 (Street 20–28/edges 4–10, Pro 32–44/edges 4–18, Hero A==20, soft≤8).
- **`Tests/Math/StepUpPromoterTests.cs`** — All A==10 assertions corrected to A==20; soft-cap tests corrected from cap=3 to cap=6; `Promote_LevelBecomesTen` renamed to `Promote_LevelBecomesHeroTierValue` asserting Level==20; new `PreviewPromotion_CapsLowestAboveSix` test added.
- **Asset move** — `GameBoard.png` and `MainMenuBackground.png` moved to `Assets/Art/UI/`. Both `.import` files and scene references updated.
- **Main menu GIF** — `Assets/Art/UI/From_selection.gif` added as animated background. `Scenes/MainMenu.tscn` now references the GIF. `Assets/Art/UI/From_selection.gif.import` created (Godot 4.3+ imports GIFs as AnimatedTexture automatically; TextureRect plays it natively).
- **`lore.md` and `systems.md`** — updated with Scale-20 corrections, asset notes, and full scope additions (§15–§20).

### Scope additions approved (systems.md §15–§20)
Full design specs written into systems.md. Summary:
- **§15** — District node maps: procedurally generated per run, branching horizontal graph, 8 encounter types (Fight, Free Agent Meet, Ripper Doc, Debt Collector, Fixer Contact, Narrative Event, Cache, Mini-Boss, Boss).
- **§16** — Bosses & mini-bosses: one per district, rule-bending fight modifiers (grid expansion, rotating board, card migration, blind hand, decaying board, etc.).
- **§17** — Debt Collector as named character: 3-rung ladder (The Notary → The Auditor → Aoi) leading to Madame Sumi. Terminal fight has a ~2–3% easter egg: beat Sumi's crew and recruit one of her named cards (not Sumi herself).
- **§18** — Fixer reputation & cross-run boons: loyalty threshold per fixer per run; one boon offered at run start from a per-fixer pool; not permanent progression, player can decline.
- **§19** — Kill feed narrative layer: named card capture flavour lines + ambient narrative injections from Wire.
- **§20** — Narrative missions: 3–5 event chains spread across districts, triggered via node map events. 5 seed missions documented (The First Voice, The Ledger Never Closes, Ghost in the Machine, The Long Con, What the Stub Remembers).

---

## Architecture Overview

### Scene Flow
```
MainMenu → PreMatchScreen → GameBoard → PostMatchScreen → PreMatchScreen
```
Node map screen and boss fight variants will slot between PreMatchScreen and GameBoard when §15–§16 are implemented.

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
Assets/
  Art/
    UI/
      GameBoard.png             — board background art
      GameBoard.png.import
      MainMenuBackground.png    — static fallback (unused now)
      MainMenuBackground.png.import
      From_selection.gif        — animated main menu background (active)
      From_selection.gif.import — Godot AnimatedTexture import config

Scripts/
  Campaign/
    CredTier.cs / CredEvents.cs / CredEffects.cs / CredManager.cs
    RazorkinRefusal.cs
    DistrictAccess.cs     — gate rules per district; GraceMatches=3
    ScripPayoutCalculator.cs / BuyoutPricing.cs
  Core/
    BoardState.cs / CardData.cs / CardInstance.cs
    CardDatabase.cs / DistrictData.cs / DistrictDatabase.cs / DistrictManager.cs
    CrewGenerator.cs      — Scale-20: StreetMin/Max=20/28, ProMin/Max=32/44
    GameManager.cs        — VesnaStartingCap=14 (20 in The Hush)
    NameGenerator.cs / SaveManager.cs
    StepUpPromoter.cs     — A=20, soft cap=6, Level=20 (see Level NOTE in class doc)
    FreeAgent.cs / FreeAgentGenerator.cs
  Rules/
    ICardAbility.cs / IProtocol.cs
    CaptureResolver.cs    — base capture, protocols, Cascade, Rivalry, Breach, Listener
    DomainResolver.cs     — BonusMultiplier=1
    BondResolver.cs       — all 7 bonds; ContaminationEnabled=false
    MatchConfig.cs        — Scale-20 params: tolerance=2, sumTolerance=2, wallValue=20
    HandshakeProtocol.cs / TallyProtocol.cs / WallSignatureProtocol.cs
    VesnaAbility.cs / SumiAbility.cs / LetheAbility.cs
  GameBoard.cs / GameSession.cs / MainMenu.cs
  PreMatch/PreMatchScreen.cs
  PostMatch/PostMatchScreen.cs
  UI/CardNode.cs / CellNode.cs / HandNode.cs / CredBarNode.cs / KillFeedNode.cs

Data/
  Cards/cards.json        — 29 named cards, Scale-20
  Districts/districts.json — 8 districts

Scenes/
  MainMenu.tscn           — references From_selection.gif as Background texture
  Board/GameBoard.tscn    — references Assets/Art/UI/GameBoard.png
  PreMatch/ / PostMatch/ / UI/ / Card/

Tests/
  Integration/CrewGeneratorTests.cs   — Scale-20 stat bands
  Math/StepUpPromoterTests.cs         — A=20, cap=6, Level=20
  Math/HeroAbilityProgressionTests.cs — uses CardFactory fixtures (Scale-10; internally consistent, not stale)
  Simulation/ / Capture/ / Campaign/
  Helpers/CardFactory.cs / GameSimulator.cs / BoardBuilder.cs / CampaignSimulator.cs
  TripsAndTriads.Tests.csproj         — ProjectReference → Trips_and_Triads.csproj (Scripts/)
```

---

## What Is Built and Confirmed Working

### Core Duel Loop
3×3 board, alternating turns, 5-card hands, drag-to-play, AI thinking delay (0.75s), greedy capture AI, end panel.

### Card Systems (Phases 1–4)
29 named cards, Scale-20. Hero mechanics: Vesna decay (-2/turn, Scale-20), Sumi compound, Lethe copy. Domain system, all 7 bonds.

### Protocols (Phase 5) — Scale-20 parameters
Handshake (tolerance=2), The Tally (sumTolerance=2), Wall Signature (wallValue=20, sumTolerance=2), Cascade, Intercept, Conscription, Standoff.

### Districts (Phase 6)
8 districts, control meters, Spreading Rule, MatchConfig factory, VesnaStartingCap (20 in The Hush, 14 elsewhere).

### Faction AI Crews (Phase 8b extension)
`CrewGenerator.GenerateFactionHand(controller)` — each faction fields their named hero + signature top-tier. Neutral → procedural. Contested (Vault) → random apex faction.

### Campaign Loop (Phase 7 — complete)
All 4 stakes. Hunt: hero capture, Reclaim (2 attempts), Step Up promotion. Standoff rematch uses board-state hands.

### Street Cred (Phase 8b — complete)
CredManager (0–100), CredEvents (7 types), CredEffects (4 effects), RazorkinRefusal (probabilistic). CITY SIGNAL bar with pulse/scan animation. Cred preserved through Step Up. Saved/restored.

### District Access Gating (Phase 8b extension)
DistrictAccess.cs gate rules. Grace period: 3 matches. TickGracePeriods() after every match. Lock icon, amber grace countdown, popup.

### Save System (Phase 8a + 8b)
Persists roster, Hunt, Reunion, district meters, active district, cred, grace periods.

### Economy (Phase 9 — partial)
Scrip persistence, post-match payouts, buyout with Razorkin refusal, Della's Standing Work (3-match limit), Free Agent Recruitment (Meet → Audition → Sign).

### UI Polish (Phase 8 series)
Drag ghost, card flip animation, AI move animation, kill feed, PreMatchScreen deck order, Hunt panel, grace period popup.

---

## Known Issues / Tech Debt

### Contamination Disabled
`BondResolver.ContaminationEnabled = false`. Re-enable when campaign districts live:
```csharp
BondResolver.ContaminationEnabled = district.Controller == "HollowChoir";
```

### Shaken Not Implemented
`systems.md §5.1` — cards joining roster by capture should arrive Shaken (lowest edge = 0 for first match; 3rd Shaken → permanent Calloused).

### Hunt Protocol Bleeding
Hunt (Reclaim) matches inherit district protocols. Should be base-capture-only AsFlipped (`systems.md §7.3`). Fix: branch on `session.IsHuntMatch` before `BuildMatchConfig()` in `GameBoard._Ready`.

### Multi-Hunt Not Implemented
Only one Hero Hunt active. Second capture while Headless silently drops. Needs `List<HuntEntry>`, cap=3, expiry, selector UI.

### Conscription AI No Roster
AI uses generated hand under Conscription. Needs persistent AI roster.

### Level Field Inconsistency
`CardData.Level` is inert metadata, not read by game logic. Named heroes in cards.json = 10; StepUpPromoter = 20; FreeAgentGenerator = 16. Resolve when Level is wired to gameplay (Payroll tiers, district access, display).

### GIF Import Heads-Up
`From_selection.gif.import` is a hand-authored stub. Godot will overwrite it with proper values on first project open. If the background is broken after pulling: open the project in Godot, let it reimport assets, the TextureRect will pick up the AnimatedTexture automatically.

### DistrictAccess Lore Open Threads
Glass Spire (what is Ascendant "verified identity"?), Dead Channel (why cred tiers specifically?), The Powder Room (Lacquer locking out weak crews is off-brand — rethink as Fixer-vouch?), The Hush (Choir awareness should be the Antecedent noticing the crew).

### Minor Test Warning
`Scale20prototypetests.cs` line 149: CS0219 unused variable `totalProtocolCaptures`. Cosmetic.

---

## Art Pipeline Guidance

All art lives in `Assets/Art/UI/`. For **animated assets**, the preferred export formats from the art director, in order of preference:

1. **APNG** — lossless, full-color, Godot-native animated texture. Best quality, reasonable file size for short loops. Export from Photoshop via APNG plugin, Krita natively, or After Effects via plug-in.
2. **WebM (VP8 or VP9)** — best for long/complex animations. Godot plays WebM as a `VideoStreamPlayer` node rather than a `TextureRect`. Use when the animation is > a few seconds or has many colors/gradients.
3. **GIF** — 256-color palette limit causes banding on gradients. Acceptable for short, flat-color loops. Currently in use for the main menu background (`From_selection.gif`).

For **static assets**: PNG with transparent alpha, authored at 2× display resolution.

For the main menu specifically: if the art director can re-export `From_selection.gif` as an APNG, that swap is a one-file change — just replace the GIF and update the `.import` file path. No scene changes needed.

---

## Balance Constants

| Constant | Location | Value |
|---|---|---|
| `VesnaStartingCap` | `GameManager.cs` | 14 default; 20 in The Hush |
| `AiThinkDelay` | `GameBoard.cs` | 0.75f ([Export]) |
| `StreetMin/Max` | `CrewGenerator.cs` | 20–28 (Scale-20) |
| `ProMin/Max` | `CrewGenerator.cs` | 32–44 (Scale-20) |
| `BonusMultiplier` | `DomainResolver.cs` | 1 |
| `GraceMatches` | `DistrictAccess.cs` | 3 |
| Reclaim attempts | `GameSession.SetCapturedHero` | 2 |
| Step Up A value | `StepUpPromoter` | 20 |
| Step Up soft edge cap | `StepUpPromoter` | 6 |
| Step Up Level | `StepUpPromoter` | 20 (see Level NOTE) |
| Handshake tolerance | Protocol constructors | 2 |
| Tally sumTolerance | Protocol constructors | 2 |
| WallSignature wallValue | Protocol constructors | 20 |

---

## What's Next (Priority Order)

**First action of every session:** `dotnet test Tests/ --verbosity normal` → confirm green.

### Immediate correctness items (Track A)
These unblock everything else and are small, self-contained:
1. **Hunt protocol stripping** — one `if (session.IsHuntMatch)` branch in `GameBoard._Ready` before `BuildMatchConfig()`.
2. **Shaken mechanic** (`systems.md §5.1`) — `CardInstance` flag, lowest-edge=0 for one match, 3rd Shaken = Calloused.
3. **Contamination re-enable** — wire `BondResolver.ContaminationEnabled` to district controller.

### Phase 9 remainder (Track B)
- Remaining Fixers: Vig (Wagers), Atlas (Intel/Hunt location), Mrs. Oba (Long Account + debt), The Tailor (Ghost Contracts).
- TabContainer UI is already built to accommodate.
- Mutual Aid / Obligation wire-up (deferred to Phase 11 debt system).

### Phase 11 — Payroll & Debt
Upkeep per overworld turn. Collector ladder. Mutual Aid vs Lacquer debt.

### Phase 15 — District Node Maps (new scope)
Start with a design spike: a single hardcoded map for The Stub with 3 node types (Fight, Free Agent Meet, Cache). Validate the UI, the scene transition into GameBoard, and SaveManager persistence before generalising the generator. See `systems.md §15`.

### Phase 16 — Bosses (new scope)
Requires Phase 15 (node maps) as infrastructure. Design The Stub mini-boss and boss first — they're the simplest (Neutral district, no faction modifier). See `systems.md §16`.

### Phase 17 — Debt Collector character (new scope)
Requires Phase 11 (Payroll/Debt). Name the ladder rungs, build The Notary's deck. See `systems.md §17`.

### Phase 18 — Fixer reputation (new scope)
Requires Phase 9 remainder (all Fixers built). See `systems.md §18`.

### Phase 19 — Kill feed narrative (new scope)
Can be done any time — it's additive to the existing KillFeedNode. Good session filler. See `systems.md §19`.

### Phase 20 — Narrative missions (new scope)
Requires Phase 15 (node maps) as infrastructure. See `systems.md §20`.

### Phase 10 — The Hollowing
Dead Line → Touched → Fading → Claimed. Requires Phase 9 (economy/Fixers).

### Phase 12 — Prestige & Skyline
Requires Phase 10 + Phase 11.

---

## Design Principles (from `lore.md §13`)
1. Geometry is lore — stat shape is readable as character
2. Heroes are load-bearing — Domains mean decks are built around the hero
3. No board tags — all positional play from board geometry + hero Domains
4. One A, one soft side — heroes dominant but never auto-win (Sumi/Vesna exceptions intentional)
5. Counterplay must be legible
6. Bonds are stories — named relationships, not stat keywords
7. Mechanics need lore justification before shipping
