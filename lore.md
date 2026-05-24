# Antecedent — Design & Lore Bible

*Working title: **Antecedent**. A cyberpunk reimagining of Triple Triad (Final Fantasy VIII).*

This document is the single source of truth for the game's world, factions, characters, and mechanics. Hand it back at the start of any session to pick up where we left off.

---

## 1. The Game in One Paragraph

A 3×3 card-capture duel. Each card has four numbers — one per edge — and you flip your opponent's cards by placing a stronger edge against theirs. The cyberpunk layer adds **factions**, **heroes**, and a positional system built entirely on top of the existing board, requiring no new components. The hook: heroes are not just strong cards, they are *people*, and every hero's stat shape, weakness, and aura is a literal expression of who they are.

---

## 2. Core Rules Recap

- **Board:** 3×3 grid, nine cells.
- **Hands:** five cards each.
- **Turns:** players alternate placing one card into any empty cell.
- **Capture:** when a placed card sits adjacent to an enemy card, compare the two touching edges. If the placed card's edge is higher, the enemy card flips to your control.
- **Stats:** each card has Top / Left / Right / Bottom values, 1–9, with **A = 10** as the maximum.
- **Win:** when the board fills, the player controlling more cards (counting the one unplayed card in hand) wins.

The faction layer below extends this without changing the bones of the game.

---

## 3. Card Anatomy & Tiers

Every card has four edge values written **T / L / R / B**. Power scales with the total of the four numbers.

| Tier | Level band | Stat total | Role |
|---|---|---|---|
| **Street** | 1–5 | ~8–14 | Common pulls — drones, gangers, scav hardware |
| **Pro** | 6–7 | ~16–22 | Skilled operators, military surplus |
| **Top-Tier** | 8–9 | ~24–30 | Elite chrome, corp black ops, rogue AIs — the cards players grind for |
| **Hero** | 10 | special | Named, unique legends — one per deck (see §10) |

**Tier vs. Hero, settled:** Top-Tier is a *rarity band* — you can own and trade multiples. A **Hero** is a unique named individual. Every Hero is, by definition, the apex of its faction, but not every Top-Tier card is a Hero.

**Hero stat rule:** every Hero has exactly **one A (10)** and one deliberately soft edge — dominant, never auto-win. Two heroes intentionally subvert this rule, and the subversion *is* their character (Madame Sumi, Vesna — see §8).

---

## 4. The Faction System

Seven factions. Each one is a complete identity: a piece of the world, a signature stat trait shared by all its cards, a hero, and a **Domain** (see §5).

| Faction | Theme | Signature stat trait |
|---|---|---|
| **Ascendant** | The corporate bloc | Forward-loaded — built to face front |
| **Razorkin** | The chrome gang | Lopsided — brutal highs, ugly lows |
| **Ghostwire** | Netrunners, the half-dissolved | Lateral — strong sides, weak vertical |
| **The Commons** | The undercity, mutual aid | Even, modest — strong only together |
| **Effigy** | Identity-laundering syndicate | Point-symmetric — T=B, L=R |
| **Lacquer** | Heritage-firm front, Yakuza ops | Compounding — grows the longer it survives |
| **Hollow Choir** | Wardens & cult of the Black Wall | The Toll — every card has exactly one 0 |

> **Design note — Redline cut.** An earlier "courier syndicate" faction (Redline) was removed: a *movement* theme is hard to make fun in a game about cards sitting still. Effigy replaced it. The cut cards (Vesper Dane, Switchback, Crane) and their bonds are retired.

---

## 5. The Domain System — Solving Board Effects on a 3×3 Grid

**The problem:** static, pre-tagged "faction node" cells don't fit a nine-cell board. Seven factions can't each own a node without erasing every neutral cell, and tagging cells adds components and bookkeeping.

**The solution:** there are **no board tags at all**. Instead, **each hero projects a Domain** — an aura affecting the (up to four) cells orthogonally adjacent to that hero. The faction's positional identity travels *with the hero* rather than living on the board.

This is compact (adjacency only), scales naturally (more heroes on the board = more live Domains), needs zero extra components, and makes heroes the strategic anchor of every match — exactly where a hero-driven game wants its decisions.

Domains affect **friendly cards only** unless stated otherwise.

| Faction | Hero | Domain effect |
|---|---|---|
| **Ascendant** | Seraph Yune | **Aegis Protocol** — friendly cards adjacent to Yune get **+1 to all sides** |
| **Razorkin** | Sister Grin | **Killzone** — friendly cards adjacent to Grin get **+2 to their two lowest sides** |
| **Ghostwire** | Riven | **Lateral Grid** — friendly cards adjacent to Riven get **+2 Left and +2 Right** |
| **The Commons** | Mara Kane | **Sprawl** — friendly Commons cards adjacent to Mara get **+1 all sides**; Mara herself gets **+1 all sides per adjacent friendly card** |
| **Lacquer** | Madame Sumi | **The Ledger** — friendly cards adjacent to Sumi also gain **+1 all sides at the end of each of your turns** (the debt spreads) |
| **Effigy** | Lethe | **The Mirror** — Lethe has no ongoing aura; her Domain *is* her placement (see §8) |
| **Hollow Choir** | Vesna | **The Breach** — when a friendly card adjacent to Vesna captures, that capture **chains**: the captured card immediately re-attacks its own neighbors with its new stats, and so on until it stops |

Note that each Domain is *shaped like its faction*: Ascendant's is uniform and total; Razorkin's bristles on the weak flank; Ghostwire's reaches sideways; the Commons' rewards crowding; Lacquer's compounds; the Choir's spreads like infection.

---

## 6. Positional Play — The Board Is Its Own Affinity System

The 3×3 board already has built-in geometry, and it costs nothing:

- **Corner cell** — exposes only **2 edges**. A safe harbor.
- **Edge cell** — exposes **3 edges**.
- **Center cell** — exposes all **4 edges**. The most contested and most vulnerable square.

This interacts directly with hero geometry (§9). A hero with a soft edge — Seraph Yune (soft Bottom), Sister Grin (soft Right/Bottom) — wants a corner that *hides the weakness against the board's wall*. An even card like Mara Kane is comfortable anywhere. A decaying card like Vesna wants a corner to ride out her decline. "Affinity by board position" is therefore real and tactical without a single tagged cell — the board's own shape is the system.

---

## 7. The Seven Factions

### Ascendant — *The Corporate Bloc*
Clean, vertical, branded. Ascendant manufactures its assets: bioengineered operatives and white-tower ICE, all designed, warrantied, and disposable. Their cards are forward-loaded because their products are *built to face front* — no one designs a flagship to be flanked.

- **Hero — Seraph Yune** *(female; engineered flagship asset)* — **A / 8 / 8 / 3**. Built to win the encounter she was pointed at; her designers never planned for an attack from behind, so her Bottom is the blind spot. Aegis Protocol's flat +1 quietly papers over that flaw — corporate infrastructure covering for its product.
- **Dr. Cassia Vane** — Top-Tier — **7 / 6 / 8 / 5**. The geneticist who built Seraph.
- **Proxy** — Top-Tier — **5 / 8 / 8 / 6**. *(androgynous)* A PR construct: a face the company wears in public.

### Razorkin — *The Chrome Gang*
Full-conversion psychos who treat their own bodies as a weapons rack. No subtlety, no defense, no plan beyond the next overcommit. Their cards are violently lopsided — terrifying highs, embarrassing lows.

- **Hero — Sister Grin** *(female; chrome-jaw warlord)* — **A / A / 2 / 3**. A corner predator who only knows one angle of attack: all-in, nothing held back, everything she has aimed one way. On her own turf, Killzone lifts her pitiful Right and Bottom and makes her un-flankable — a genuine nightmare to approach.
- **Gristle** — Top-Tier — **9 / 7 / 2 / 8**. *(female)* Grin's enforcer.
- **Twitch** — Top-Tier — **8 / 8 / 4 / 3**. A speed-freak razorgirl, wired past the redline.

### Ghostwire — *Netrunners & the Half-Dissolved*
People who left more of themselves in the Net than in the room. The deeper a runner goes, the less of their body comes back. Their cards are *lateral* — strong on the horizontal data-flow, weak on the vertical, where the abandoned flesh used to be.

- **Hero — Riven** *(androgynous; left her body in the Net)* — **3 / 9 / 9 / 2**. She exists sideways now, in the lateral current of data. Lateral Grid pushes her two 9s even harder.
- **Echo** — Top-Tier — **4 / 9 / 9 / 3**. *(androgynous)* A half-dissolved fragment of someone who didn't fully come back.
- **Wren** — Top-Tier — **6 / 7 / 7 / 5**. A young runner — still mostly whole, not yet lost.

### The Commons — *The Undercity*
Fixers, scavengers, kitchens, mutual aid. Weak alone, unkillable together. Their cards are modest and even, and they get strong only by *clustering*.

- **Hero — Mara Kane** *(female; the fixer)* — **6 / 6 / 6 / 6**. No edge of her is a wall; no edge is a gap. Unremarkable solo, decisive surrounded. Sprawl makes her a walking node.
- **Auntie Sol** — Top-Tier — **5 / 6 / 6 / 6**. *(female)* A mutual-aid kitchen and information hub. Radiates **+1 to adjacent Commons cards**.
- **Patch** — Top-Tier — **6 / 5 / 6 / 7**. A scrap-medic who keeps the block standing.

### Effigy — *The Identity-Laundering Syndicate*
In a city where your face is your password, your bank, and your name, Effigy steals all three. Body doubles, deepfake assassins, surgically blanked operatives. They have no culture, no turf, no faces of their own — only clients. The unsettling part isn't that they're dangerous; it's that there's *nobody home*. Every Effigy card has **point-symmetric stats** (Top = Bottom, Left = Right) — a face and its reflection. You can spot one across the room.

- **Hero — Lethe, "the Understudy"** *(female)* — printed **blank: 0 / 0 / 0 / 0**. On placement she permanently copies the full stat line of **any one card on the board** — she can *become* Seraph Yune, A and all. But placed first, into an empty board, she is a 0/0/0/0 corpse. She is nobody until the table gives her someone to be. She copies the four numbers only — not Domains, abilities, or bonds.
- **Verity** — Top-Tier — **7 / 9 / 9 / 7**. *(female)* A deepfake artist — the name is a joke she enjoys.
- **The Smile** — Top-Tier — **8 / 5 / 5 / 8**. *(female)* A face-actor.
- **Cousin** — Top-Tier — **6 / 7 / 7 / 6**. *(androgynous)* Impersonates family; talks its way indoors.

### Lacquer — *The Heritage-Holdings Front*
Corpos only in look — Yakuza in how they operate. A beautiful surface, a hard shell, and something hidden underneath, exactly like the craft they're named for. They don't raid; they *acquire*. They run on debt, obligation, and a very long memory. Everyone owes Lacquer something. Their cards compound — demure now, lethal in four turns.

- **Hero — Madame Sumi** *(female; the matriarch)* — **4 / 4 / 4 / 4**, and she is the **one hero with no A**. She compounds **+1 to all sides at the end of every one of your turns**, regardless of position. Turn one she's a nobody; turn nine she's untouchable. The counterplay is obvious and hard: kill her while she's still demure.
- **Aoi** — Top-Tier — **8 / 5 / 8 / 4**. *(female)* The collector — dressed like an aide, isn't one.
- **The Heir** — Top-Tier — **7 / 7 / 5 / 7**. *(androgynous)* Next in line.

### Hollow Choir — *Wardens & Cult of the Black Wall*
Before the modern Net there was a first one, and something in it woke up — vast, and wrong. They walled it off and named it the **Antecedent**. The Black Wall holds. Mostly. The Hollow Choir are the few who tend that wall: who press their minds against it and *listen*, and sometimes answer. They say they keep it asleep. Some are lying. All of them have come back from contact with a little less behind their eyes — they are called Hollow for a reason. When a Choir card flips one of yours, it should not feel like losing a fight. It should feel like something *noticed you*.

Every Choir card carries the **Toll**: exactly one **0**, a dead and silent edge — the price of reaching through the Wall. Their other three numbers run high to compensate, so positioning a Choir card is a small act of dread management: keep the hole facing a wall.

- **Hero — Vesna, "the First Voice"** *(female)* — printed **A / A / A / A**. The first person to reach through the Black Wall, answer it, and return — she came back knowing the *shape* of the thing, and it has been unmaking her ever since. The instant she touches the board she is the single strongest card in the game. Then, at the end of every one of your turns, she loses **1 from every side**. Five turns later she is a husk. She is the only card desperate to stop being what she is.
- **Threnody** — Top-Tier — **9 / 8 / 0 / 9**. *(female)* A chorister; she sings the lullaby that keeps it under. The silent edge is the one she sings *into*.
- **Antiphon** — Top-Tier — **0 / 9 / 9 / 8**. *(androgynous)* Call-and-response; half of every conversation it has is with something else.
- **Lamb** — Top-Tier — **8 / 9 / 8 / 0**. *(female)* The youngest diver — the Choir send the young in, because the young survive contact longer.

### The Truth of the Wall — the Athena Protocol

*This is the world's central reveal. It is player-facing but earned: the player meets Athena long before they understand her.*

The Black Wall is not a structure. It is **someone**.

From the first hour of the game the player has a companion — a voice in the ear named **Athena**. She runs the tutorial, explains the board and the Fixers and the Protocols, comments on the player's choices, and teases. She is wry, warm, and **mischievous** — she will withhold a hint just to watch the player work it out. She reads, completely, as a quirky companion AI: a handle, a piece of software the crew picked up somewhere. The player assumes she is a tool.

She is the **Athena Protocol** — the active guardian intelligence that stands between the modern Net and the Antecedent. The first Net woke something vast and wrong; Athena is the thing that has held the line ever since. The Black Wall does not *contain* the Antecedent the way a vault contains a prisoner. The Wall is **her**, sustained by will, every second, without rest, without end.

- **Why she only ever talks.** Holding the Wall is everything she has. She cannot fight, cannot intervene in the city, cannot leave. The single thing she can still do is *speak* — so she reaches out to crews on the rise and guides them, because a city that stays strong is a city that survives, and a city that survives keeps her standing. The tutorial was a recruitment. She chose the player.
- **Why a guardian holding back annihilation is playful.** The mischief is not a mask over the dread — it is her refusal of it. Despair is the Antecedent's weapon; playfulness is how Athena stays *herself* under a weight that would flatten anything else. Her teasing is joy held against the dark, and it is the most defiant thing in the game.
- **The Choir, recontextualized.** The Hollow Choir are not a death cult. They are *literally her choir* — her wardens, lending her their minds and singing her strong. Threnody's lullaby was always for **her**. The Choir are Hollowed because closeness to the Wall costs; some of them know exactly what they tend, and some only think they do.
- **The Dead Line, recontextualized.** Every Dead Line contract (`systems.md` §9.5) is the Antecedent reaching through — pressing on Athena. A crew that chases Dead Line scrip and lets itself be Hollowed has, unknowingly, been helping the thing she holds back. She warns the player about Dead Line work — gently, without quite explaining why. They may not listen. After the reveal, every Dead Line job they took recolors.
- **The constant.** Athena persists across every campaign. New crew, new climb, same Athena. She is the one unbroken thread of the entire game — the presence that is always there.

She is warm, and she is also faintly uncanny: something this old and this vast, that has held back the end of the world alone for an unknowable time, and that quietly chose *you*. The comfort is real. So is the weight behind it.

> **Naming.** She introduces herself as "Athena" — just a name, a quirky handle. The reveal is the word **Protocol** and everything behind it. The name is true from the first minute; the player simply does not yet know what it weighs.

---

## 8. Hero Geometry — Seven Distinct Shapes

The design goal: every hero plays differently because every hero is shaped differently, and the shape is the lore.

| Hero | Geometry | Stat line | Reads as |
|---|---|---|---|
| Seraph Yune | **Forward** | A / 8 / 8 / 3 | Built to face front; blind from behind |
| Sister Grin | **Corner** | A / A / 2 / 3 | One angle of attack, all-in |
| Riven | **Lateral** | 3 / 9 / 9 / 2 | Lives sideways, in the data-flow |
| Mara Kane | **Even** | 6 / 6 / 6 / 6 | No wall, no gap — strong together |
| Madame Sumi | **Compounding** | 4 / 4 / 4 / 4 → grows | Demure now, lethal later |
| Lethe | **Borrowed** | 0 / 0 / 0 / 0 → copied | No self until given one |
| Vesna | **Decaying** | A / A / A / A → shrinks | Strongest thing alive, and dying |

Sumi and Vesna are deliberate mirror images: one **ripens**, one **rots**. Together they prove the "one A, one soft side" rule by breaking it in opposite directions.

---

## 9. Bonds — Named Hero Synergies

Synergies are **named relationships with backstory**, not generic faction buffs. A bond is only live when both named cards are on the board.

| Bond | Cards | Effect |
|---|---|---|
| **The Rivalry** | Seraph Yune ↔ Sister Grin | Corp asset vs. the thing that escaped the lab. Placed adjacent, they resolve capture against *each other first*, before normal capture rules. |
| **The Last Crew** | Riven ↔ Mara Kane | They ran jobs together before Riven uploaded. While both are on the board, each gets **+1 all sides**. |
| **Maker's Mark** | Seraph Yune ↔ Dr. Cassia Vane | While her creator is on the board, Yune's soft **Bottom counts as +2** — the designer patching her own flaw. |
| **The Inheritance** | Madame Sumi ↔ The Heir | While both are on the board, The Heir also **compounds +1 all sides each turn** — learning the trade. |
| **The Understudy** | Lethe ↔ the hero she copied | While Lethe is adjacent to the original of the card she is wearing, *both* cards' highest edge drops to **5** — the city cannot resolve two of the same face, and both glitch. |
| **Contamination** | Vesna ↔ any adjacent non-Choir card | Anything placed next to the First Voice has its **lowest edge reduced by 1**. Proximity is a wound. |
| **The Listener** | Vesna ↔ Riven | Riven has *heard* the Antecedent through the Wall. While both are on the board, **Riven cannot capture Choir cards.** She will not go near them. |

Regular cards can carry quieter, single-line versions of this — e.g. a ripperdoc who gives +1 to any adjacent Razorkin (she did their chrome); a drone that is weak alone but networks with other drones.

---

## 10. Deck Construction

- A deck is **5 cards** (one standard hand).
- A deck may include **at most one Hero.** This makes the hero choice the defining decision of a deck and keeps Domains relevant in nearly every game.
- Decks may be **mono-faction** (full Domain synergy, predictable) or **splash** (a hero of one faction with support from another — bonds reward specific cross-faction pairings, e.g. The Last Crew).
- Not every card belongs to a faction. **Unaffiliated freelancers** exist and run no Domain — useful neutral filler.

Sample unaffiliated cards:

| Card | Tier | T / L / R / B |
|---|---|---|
| Recon Drone | Street | 2 / 3 / 3 / 1 |
| Chrome Ganger | Street | 4 / 1 / 3 / 3 |
| Cyber-Mutt | Street | 3 / 5 / 2 / 1 |
| Merc Sniper | Pro | 7 / 2 / 7 / 1 |
| Riot MedTech | Pro | 5 / 6 / 4 / 5 |
| Netrunner | Pro | 6 / 5 / 6 / 3 |

---

## 11. Full Card Roster

| Name | Faction | Tier | T / L / R / B | Notes |
|---|---|---|---|---|
| Seraph Yune | Ascendant | Hero | A / 8 / 8 / 3 | Domain: Aegis Protocol |
| Dr. Cassia Vane | Ascendant | Top-Tier | 7 / 6 / 8 / 5 | Seraph's creator |
| Proxy | Ascendant | Top-Tier | 5 / 8 / 8 / 6 | PR construct |
| Sister Grin | Razorkin | Hero | A / A / 2 / 3 | Domain: Killzone |
| Gristle | Razorkin | Top-Tier | 9 / 7 / 2 / 8 | Grin's enforcer |
| Twitch | Razorkin | Top-Tier | 8 / 8 / 4 / 3 | Speed-freak razorgirl |
| Riven | Ghostwire | Hero | 3 / 9 / 9 / 2 | Domain: Lateral Grid |
| Echo | Ghostwire | Top-Tier | 4 / 9 / 9 / 3 | Half-dissolved fragment |
| Wren | Ghostwire | Top-Tier | 6 / 7 / 7 / 5 | Young, not yet lost |
| Mara Kane | The Commons | Hero | 6 / 6 / 6 / 6 | Domain: Sprawl |
| Auntie Sol | The Commons | Top-Tier | 5 / 6 / 6 / 6 | +1 to adjacent Commons |
| Patch | The Commons | Top-Tier | 6 / 5 / 6 / 7 | Scrap-medic |
| Lethe | Effigy | Hero | 0 / 0 / 0 / 0 | Copies a card on placement |
| Verity | Effigy | Top-Tier | 7 / 9 / 9 / 7 | Deepfake artist |
| The Smile | Effigy | Top-Tier | 8 / 5 / 5 / 8 | Face-actor |
| Cousin | Effigy | Top-Tier | 6 / 7 / 7 / 6 | Impersonates family |
| Madame Sumi | Lacquer | Hero | 4 / 4 / 4 / 4 | Compounds +1/turn; Domain: the Ledger |
| Aoi | Lacquer | Top-Tier | 8 / 5 / 8 / 4 | The collector |
| The Heir | Lacquer | Top-Tier | 7 / 7 / 5 / 7 | Next in line |
| Vesna | Hollow Choir | Hero | A / A / A / A | Decays -1/turn; Domain: the Breach |
| Threnody | Hollow Choir | Top-Tier | 9 / 8 / 0 / 9 | Sings the lullaby |
| Antiphon | Hollow Choir | Top-Tier | 0 / 9 / 9 / 8 | Call-and-response |
| Lamb | Hollow Choir | Top-Tier | 8 / 9 / 8 / 0 | Youngest diver |

---

## 12. Optional Variant — "The Wall Is Thinning"

A horror escalation rule for matches involving the Hollow Choir. At the start of each turn, a random empty cell becomes **Breached**: any capture into or out of that cell chains, as Vesna's Domain does. Breaches accumulate. As the board fills, the old net bleeds further into the game — the longer the match runs, the less safe any placement is. Use sparingly; it is meant to feel like losing control.

---

## 13. Design Principles

Carry these into every future card and faction:

1. **Geometry is lore.** A card's stat shape should be readable as character before anyone reads the name.
2. **Heroes are load-bearing.** Domains mean a deck is built *around* its hero. The hero is the decision.
3. **No board tags.** The 3×3 board stays clean; all positional play comes from board geometry plus hero Domains.
4. **One A, one soft side** — heroes are dominant but never auto-win. Subvert this rule only when the subversion is the character (Sumi, Vesna).
5. **Counterplay must be legible.** Every powerful card should hand the opponent an obvious, hard line of play.
6. **Bonds are stories.** Synergies are named relationships, not stat keywords.

---

## 14. Implementation Notes

The current `CardData` model (`Name`, `Top`, `Right`, `Bottom`, `Left`) will need extending to carry this design. Suggested additional fields:

- `Faction` (enum: Ascendant, Razorkin, Ghostwire, Commons, Effigy, Lacquer, HollowChoir, None)
- `Tier` (enum: Street, Pro, TopTier, Hero)
- `Abilities` — a list, for Domains, decay/compound effects, and bonds, each implemented as its own rule class (mirrors the existing `Scripts/Rules/` pattern, e.g. a `DomainResolver` alongside `CaptureResolver`).

Domains, decay, and compounding are all *end-of-turn* or *adjacency* effects — they slot cleanly into the turn loop in `GameManager` after capture resolution.

---

## 15. Open Threads

- **Effigy's non-hero identity.** Effigy currently leans entirely on point-symmetric stats plus Lethe. Worth exploring a small copy-flavored ability for its Top-Tier cards so the faction's theme reads even without the hero in play.
- **Faction relationship web.** A who-hates-whom map would seed future bonds (Ascendant ↔ Razorkin is established; Lacquer's debts touch everyone; the Choir unsettles all).
- **Athena's reveal beat.** The Athena Protocol's nature (§7) is established; *when and how* the player learns it — and how much the Hollow Choir spell out versus imply — is a narrative-structure question for the campaign (`systems.md` §14).
- **Game title.** Resolved — working title is **Antecedent** (runner-up: *The Toll*).
