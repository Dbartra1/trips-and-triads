# Antecedent — Systems & Meta Bible

*Working title: **Antecedent**. Companion to `lore.md`. Where `lore.md` defines the world, factions, and cards, this document defines the **rule variants**, the **recruitment meta-layer**, and the **district system** that frames a campaign.*

Hand both documents back at the start of any session.

---

## 1. Scope

This doc covers the systems that turn the core duel (`lore.md`) into a campaign — each adapted from the spiritual ancestor (Triple Triad, FFVIII) but reinvented to fit our world rather than ported wholesale:

1. **The Protocols** — optional capture-rule variants (§3). Our version of Same / Plus / Combo / Elemental etc.
2. **Recruitment & Roster** — the post-match spoils system, reframed as the campaign's core fantasy (§4–5).
3. **Districts** — faction-controlled regions that each impose their own Protocols (§6).
4. **Headless** — hero loss, the Hunt, and succession (§7).
5. **Street Cred** — the crew's reputation as a broad campaign stat (§8).
6. **The Economy** — scrip, Fixers, contracts, and the talent market (§9).
7. **The Hollowing** — the Dead Line's affliction system (§10).
8. **Payroll & Debt** — the soft roster cap and the Collector ladder (§11).
9. **Prestige** — the endgame, and how a campaign reseeds (§12).

Design rule for everything here: a mechanic only ships if it has a **lore reason to exist**. No abstract keywords. If we can't say *why the city works this way*, the rule is cut.

---

## 2. The Premise — Why You Are Playing At All

You are an **up-and-coming crew**. No turf, no name anyone respects, a hand of five and nothing else. The city does not settle disputes with open war — it cannot afford to — so it settles them across a 3×3 board.

A duel is not a metaphor for a fight. It **is** the fight. When you place a card against a rival's and flip it, you are not scoring a point — you are **poaching their operative**. The capture mechanic and the recruitment mechanic are the same gesture seen at two scales: flip a card in a match, and if you win the match, that person now runs with *you*.

A crew is therefore not a static deck. It is a roster that grows, frays, and gets poached back. This reframes every design decision below.

---

## 3. The Protocols — Capture-Rule Variants

In the old city the game had one rule: high edge wins. Then the factions got hold of it. Each faction that runs games in its territory has bent the rules toward how that faction already sees the world — so the **Protocols** are not arbitrary house rules, they are factions leaving fingerprints on the game itself.

A match runs **base capture always**, plus whatever Protocols the district imposes (§6). Protocols stack.

### 3.1 Handshake *(adapts: Same)*

> When a placed card touches two or more enemy cards and an **edge value is equal** on any of those contacts, every card it ties with is captured — regardless of who is higher.

**Lore.** In a city where your face is your password, two identical signatures break the system. The grid cannot tell a forgery from an original, so it flips *both* into the same hand. A tie isn't a stalemate here — it's a **collision of identity**, and the city resolves it by ownership, not merit.

**Faction fingerprint.** Effigy. Of course it's Effigy — they are the faction that weaponizes the matched signature. Effigy's point-symmetric stat lines (T=B, L=R) make Handshake matches *far* more likely to trigger off their own cards. The faction trait and the Protocol were built for each other.

### 3.2 The Tally *(adapts: Plus)*

> When a placed card touches two enemy cards and the **two contact-pairs sum to the same total**, both touched cards are captured — regardless of individual values.

**Lore.** Lacquer does not count who is strongest. Lacquer counts what you **owe**. Two debts that add up to the same figure are, on the ledger, the same debt — and Lacquer collects both. The Tally is the rule of the accountant: it doesn't matter how the numbers got there, only that the books balance against you.

**Faction fingerprint.** Lacquer. Plays directly into Madame Sumi's compounding — as her edges climb, the sums she can hit shift every turn, making her a moving target for the math.

### 3.3 Wall Signature *(adapts: Same Wall)*

> For the purpose of Handshake (§3.1), the **edge of the board counts as a value of A (10)**.

**Lore.** The perimeter of any sanctioned board is a hard system boundary — a literal wall in the grid. It always reads maximum. A card pressed against the city's edge can tie *with the wall itself*, and the wall has no loyalty, so the tie resolves to whoever placed the card. The edge of the world is on nobody's side.

**Faction fingerprint.** Ascendant — corporate infrastructure, the clean hard boundary. Brutal in combination with Handshake.

### 3.4 Cascade *(adapts: Combo)*

> Any card captured by a Protocol (Handshake or Tally) **immediately re-attacks its own neighbors** under base capture rules, using its new owner's control. Chains until it stops.

**Lore.** This is not a faction fingerprint — it is the **old net showing through**. Cascade is what a Breach (see `lore.md` §5, Vesna's Domain) looks like when it leaks into an ordinary district. When a capture spreads on its own, with no hand guiding it, something underneath the city is doing the spreading. Cascade is the most feared Protocol because it means the Antecedent's logic has reached that far up.

**Faction fingerprint.** Hollow Choir — but it is contagious. See Districts (§6) and `lore.md` §12, "The Wall Is Thinning."

### 3.5 Intercept *(adapts: Open)*

> Both players reveal their full hands at the start of the match.

**Lore.** Someone surveilled the meet. In a district with heavy corporate or Choir monitoring, there *are* no secret hands — every operative on the table was tagged, scanned, and filed before they sat down. Intercept is the rule of a place where privacy has already lost.

**Faction fingerprint.** Ascendant (corporate surveillance) and Hollow Choir (the wall watches back).

### 3.6 Conscription *(adapts: Random)*

> A player's five-card hand for the match is chosen **at random from their full roster**, not selected.

**Lore.** Some districts don't let you pick your crew for a job — a Razorkin turf-call, a Commons emergency, a raid that kicks off before you're ready. You fight with **who showed up**. Conscription is the great equalizer and the great humiliation: your best operative might be asleep across town.

**Faction fingerprint.** Razorkin (no plan, ever) and, more sympathetically, the Commons (all hands, whoever's home).

### 3.7 Standoff *(adapts: Sudden Death)*

> If a match ends in a **draw**, it is immediately replayed — but each player's hand for the rematch is the **set of cards they controlled on the board** at the draw, frozen as-is.

**Lore.** Neither crew backed down, so neither crew leaves. A drawn duel doesn't end the dispute; it just resets it with everyone dug into the positions they already took. Standoff turns a tie into a war of attrition — and it means a card captured in round one fights *for its captor* in the rematch.

**Faction fingerprint.** Razorkin. They love this rule. Nobody else does.

> **Note — Elemental, deliberately cut.** FFVIII's Elemental rule tags board cells. We do not tag cells (`lore.md` §5, design principle 3). The flavor of "the ground itself helps or hurts you" is preserved instead through **District Hazards** (§6.3), which are environmental and don't require per-cell components.

---

## 4. Recruitment — Winning People, Not Points

This is the heart of the campaign layer. At the end of a match, the winner **recruits**. The stakes — *how many* and *which* cards change hands — are set by the district's **Stake rule**.

### 4.1 The Stake Rules

| Stake | What the winner takes | Lore framing |
|---|---|---|
| **One Job** | One card, winner's choice, from the loser's deck | A single clean poach. The most common, lowest-blood arrangement. |
| **The Spread** | One card per *margin point* (cards controlled, winner minus loser) | A decisive win bleeds the loser harder. A blowout guts a crew. |
| **As Flipped** | The winner keeps **every card they controlled on the board** at match end | The duel was the recruitment. Whoever you flipped, you flipped for real. |
| **Everything** | The winner takes the loser's entire five-card deck | Total absorption. Only ever runs in the most lawless districts — a crew that loses under Everything *ceases to exist.* |

**As Flipped** is the signature stake — it makes the board state itself the spoils, so every capture during play has weight beyond the score.

### 4.2 What Recruitment Means for a Card

A recruited card joins the winner's **roster** (§5). Crucially:

- **Heroes can be recruited.** Losing your hero is catastrophic and meant to be — it is the sharpest possible stake. A hero poached under "Everything" is the campaign's worst day.
- A recruited card keeps its stats, faction, Domain, and bonds. **It does not change faction to match its new crew.** This is the engine of the splash-deck (`lore.md` §10) — your roster naturally becomes cross-faction as you win, and bonds you never planned for light up.
- **Lethe is a special case.** If Lethe is recruited, she arrives **blank again** — 0/0/0/0. She copied a face for her old crew; that contract is over. She re-copies on her next placement. The Understudy belongs to no one, including the person who just won her.

### 4.3 The Counter-Poach

Recruitment cuts both ways and the campaign must let the loser fight back. A card you lost is not gone forever — it is now in a rival's deck, somewhere in the city, and **beating that rival lets you take it back**. This keeps losses painful but not permanent, and it gives the campaign its long rivalries: you will spend real time hunting one specific crew because they have one specific operative who used to be yours.

---

## 5. The Roster — A Crew That Grows and Frays

The recruitment loop only matters if there is somewhere for recruited cards to *go*. That is the **roster**.

- Your **roster** is every card your crew owns — it grows as you win and shrinks as you lose.
- Your **deck** is the five cards you bring to a given match, chosen from the roster (except under Conscription, §3.6, where the district picks for you).
- **Deck rule still holds:** at most one Hero per deck (`lore.md` §10). You may *own* several heroes through recruitment, but you commit to one per match. Which hero you field becomes a per-district decision — you pick the hero whose Domain best answers that district's Protocols.

### 5.1 Shaken — The Conscience of the Recruitment Mechanic

The whole game runs on flipping people to your side. **Shaken** is where the design admits that is not free. It is the human cost of capture-as-recruitment, and it is no longer "optional texture" — it is a small, real subsystem with a job.

**The trigger — broad.** Any card that joins your roster *by capture* arrives **Shaken**: a card poached from a rival (§4), a card counter-poached back to you (§4.3), a card won off an Audition (§9.6). Anyone who joined because they were *taken* or *beaten* — as opposed to a free agent who signed willingly for scrip — comes in rattled and unsure who they now work for.

**The effect — meaningful.** While Shaken, the card's **lowest edge is treated as 0** for its first match fought as part of your crew. Not a token −1 — a real, exploitable hole, and one that deliberately *rhymes with the Hollow Choir's Toll* (`lore.md` §7): a person flipped against their will has a place that has gone quiet. It clears after that one match, win or lose — they have stood on the board for you, and they settle.

**The scar — and this is the quiet horror.** A card made Shaken **again and again** — flipped away and counter-poached back across many matches, passed back and forth like a possession — does not always recover. Past a threshold (first pass: the **3rd** time it would be Shaken), the 0 stops healing. The card becomes permanently **Calloused** — lowest edge 0 for good. It is not the Hollowing (that is the Dead Line's alone, §10) but it is adjacent to it: you can only treat a person as property so many times before something in them closes for good. The endless counter-poach loop — the Hunt that §7.2 calls a core pillar — now has a human price, and a reason not to flip the same operator forever.

**On bookkeeping.** The original worry — that tracking Shaken is not worth the friction — is a tabletop worry. This is a video game; the engine tracks card state for free. The only real question is whether the effect *means* something, and the 0-edge version does.

Shaken is therefore the game's small conscience: the people you collect are marked by being collected, and the ones you collect over and over are marked for good.

---

## 6. Districts — The City as a Rule Map

The city is divided into **districts**, each controlled (or contested) by a faction. A district is a container for: which **Protocols** run there, which **Stake** is in play, and any **Hazard**. Districts are *both* a campaign map *and* a menu of match modifiers — picking where to duel is picking how the game plays.

### 6.1 Anatomy of a District

Each district defines:

- **Controller** — the faction whose fingerprints are on the rules (or *Contested*).
- **Protocols** — the active capture-rule variants (§3).
- **Stake** — the recruitment rule (§4.1).
- **Hazard** — an optional environmental effect (§6.3).

### 6.2 Sample District Map

A starter set of seven, one keyed to each faction, plus a neutral entry point.

| District | Controller | Protocols | Stake | Character |
|---|---|---|---|---|
| **The Stub** | *Neutral* | none | One Job | The tutorial district. Base rules. Where a new crew starts. |
| **Glass Spire** | Ascendant | Intercept, Wall Signature | One Job | Total surveillance, hard boundaries, low blood. Corporate and clean. |
| **The Killfloor** | Razorkin | Conscription, Standoff | The Spread | No plan, no mercy, no exit. A blowout here ruins you. |
| **Dead Channel** | Ghostwire | Intercept, Cascade | One Job | Everyone's hand is visible and captures spread. A runner's puzzle box. |
| **The Sprawl-Market** | The Commons | Conscription | As Flipped | All-hands chaos; you keep who you flipped. The Commons way. |
| **The Powder Room** | Lacquer | The Tally, Handshake | The Spread | Demure, lethal, mathematical. The books always balance against you. |
| **The Hush** | Hollow Choir | Cascade, Wall Signature | As Flipped | The district nearest the Black Wall. Captures chain and chain. Dreaded. |
| **The Vault** | *Contested* | all Protocols | Everything | Endgame. The most dangerous board in the city. Lose here and your crew is gone. |

### 6.3 District Hazards — preserving "Elemental" without board tags

A **Hazard** is a district-wide environmental rule. Unlike FFVIII's Elemental it never tags individual cells — it applies to the whole match, so it costs no components and adds no per-cell bookkeeping.

Sample Hazards:

- **Brownout** *(Commons districts)* — all cards' highest edge is -1 for the match. The power is bad here; nobody runs at full chrome.
- **Signal Bleed** *(Ghostwire / Choir districts)* — the first capture each turn Cascades, whether or not Cascade is otherwise active. The wall is thin here.
- **Lockdown** *(Ascendant districts)* — no card may be placed in the center cell on turn one. Corporate security holds the middle.
- **Open Market** *(Lacquer districts)* — bonds (`lore.md` §9) trigger even when the bonded cards are *not adjacent*, only both on the board. Lacquer's connections reach across the room.

Hazards are the right home for any future "the place itself changes the game" idea — keep them whole-match, never per-cell.

### 6.4 The Spreading Rule — the Meta Faction War

In the ancestor game, rules migrated between regions via the Card Queen. We give that migration a **lore engine**: rules spread because **factions** spread.

- Each district has a **Controller**. Winning matches in a district shifts its **control meter** toward the faction whose hero you fielded.
- When control flips, the district **adopts the new controller's signature Protocol** and may shed an old one.
- So the rule map is not static. Win enough with Lacquer heroes in a Razorkin district and Standoff gives way to The Tally — you have not just won matches, you have **changed how that part of the city plays the game.**
- This makes the campaign a map-control war fought one board at a time, and it means the player's own choices author the ruleset they'll face later.

**Resolved — yes, AI crews push control too.** The map is a living territory war, not a solo conquest: rival AI crews win matches and shift control meters on their own, so districts can flip *away* from you while you are elsewhere. This unifies cleanly with the endgame — **Skyline crews (§12) are simply the apex tier of that same AI-crew population.** One system: AI crews of every tier contest the map, ordinary rivals at the bottom and your own prestiged former selves at the top. The balance cost (a moving map is harder to tune than a static one) is accepted as worth it — a city that only moves when the player moves is not a city.

---

## 7. Headless — Hero Loss & Succession

The sharpest stake in the game is your hero, and `systems.md` §4.2 deliberately lets heroes be captured. This section defines what happens next. The design goal: a hero loss must be **devastating but never a death-spiral**, and the road out of it must always be a **player decision**, never a system yanking the crew apart.

### 7.1 The Core Split — Card vs. Crew

A hero is two things at once, and the design separates them:

- The hero **card** — a unique operative who can be captured like anyone else (§4.2).
- The hero **role** — the seat at the head of your crew. The role grants the **Domain** and unlocks **bonds**.

When your hero card is captured, you do not lose the *role* — you lose the *person filling it*. Your crew becomes **Headless**: it fields no Domain and triggers no bonds until the seat is filled again. Headless is a sharp, painful state — and a recoverable one. That recoverability is what kills the spiral.

### 7.2 The Hunt — A Core Pillar

A captured hero is not gone. They are a **named, trackable target** somewhere on the district map, in the roster of the specific rival crew that took them. The campaign treats this as a first-class objective, not a side quest:

- The rival crew becomes a **marked antagonist** — flagged on the map, recurring, personal.
- A captured hero remains visible to you: you can always see *who* holds them and *where*.
- Hunting that crew down is meant to be a multi-session arc. The hunt is a core pillar of the campaign, on equal footing with district control.

### 7.3 The Reclaim Window

When your hero is captured, a **Reclaim window** opens. It is a hard-capped clock:

- You get **two Reclaim attempts**. An attempt is a match against the crew holding your hero, fought under an **As Flipped, hero-stake** rule — win and the hero card returns to your roster.
- **Two failures and the window closes permanently.** That hero is gone for good — they run with the rival from then on — and you must Step Up (§7.5) to continue.
- There is no third attempt. The cap is the point: it gives the hunt urgency and forces a real decision instead of infinite retries.

### 7.4 Buyout — The Ransom Market

At **any point while the Reclaim window is open**, you may pay **scrip** (the campaign's currency) to ransom your hero back instead of dueling.

- **Buyout is separate from the attempt cap.** It does not consume a Reclaim attempt — it is always available while the window is open, right up until the window closes on a second failed duel.
- Two factors set the price:

**Faction markup** — *who* holds your hero. The price reads as lore:

| Captor faction | Buyout cost | Why |
|---|---|---|
| **The Commons** | Lowest | They don't really believe in owning people. |
| **Razorkin** | Cheap — but volatile | They don't care about scrip; the offer may simply be **refused** (they'd rather keep the fight). |
| **Ghostwire** | Mid | Data is data; everything has a price. |
| **Effigy** | Mid | A face for sale like any other. |
| **Ascendant** | Fixed, non-negotiable | Corporate "market rate." No haggling. |
| **Lacquer** | Highest | They *run* the ransom economy. A debt to Lacquer is the most expensive thing in the city. |
| **Hollow Choir** | **No buyout** | The Choir do not sell. Duel or Step Up — there is no third door. |

Losing a hero to the Hollow Choir is therefore the scariest version of this whole system, which is exactly right for them.

**Failure escalation** — each **failed duel attempt** raises the buyout price by a **steep ~50–75%**. So the cycle squeezes from both sides: a failed duel burns an attempt toward forced promotion *and* inflates the ransom. After one failed duel you face a steeper price and a single attempt left — spend now, gamble the last duel, or cut losses and Step Up.

> **Balance note — playtest.** The attempt cap and the buyout escalation push opposite directions: fighting makes *both* exits worse. For this to stay a real choice, the buyout must remain affordable on a normal scrip budget right up to the final attempt — otherwise players will always default to Step Up. Tune the base prices and the escalation rate together.

### 7.5 Stepping Up — Succession

If the Reclaim window closes (two failed duels, or you choose to stop), the crew is permanently Headless until you **Step Up**.

- You **promote one Top-Tier merc into a new hero** — and you may **only choose from the cards in the deck that lost the hero**. The succession comes from the crew that was actually there, not from the wider roster. The merc who steps up is the one who survived the room.
- The promoted merc gains an **A**, a **soft edge**, and a **Domain** themed to their faction. Their existing stat shape informs *which* edge becomes the A and which goes soft — geometry stays lore-true.
- The old hero is now **gone for good**. Stepping Up is the act of writing them off. This is where the permanence from "new gang, new leader" lives — but it is a *choice the player makes*, the floor they fall back to, never an automatic punishment.

### 7.6 The Lore Payoff — Heroes Are Made, Not Found

Succession answers a question the rest of the design never had to: **where do new heroes come from?** The answer is now canon — every hero in the game was once a Top-Tier merc whose crew lost its leader and promoted them. Heroes are *made*, in exactly this crucible. The Hero tier becomes earned and recursive, and the cycle can run again: lose the new hero, hunt or ransom or Step Up once more.

### 7.7 Interaction with the "Everything" Stake

Losing under the **Everything** stake (§4.1) strips the whole five-card deck *and* leaves you Headless — the campaign's worst day. But it is still not game-over: your wider **roster**, your **scrip**, and the **Step Up** option all survive. Devastating, recoverable, lore-true.

---

## 8. Street Cred — The Crew's Reputation

**Scrip** is what the crew *has*. **Street Cred** is what the crew *is*. It is a single broad campaign stat — a measure of how seriously the city takes you — and it touches many systems rather than living in one corner. Where scrip is spent and gone, cred is a standing the crew carries into every district and every deal.

### 8.1 The Cred Ladder

Cred sits on a tiered ladder; the tiers are the reference points the rest of the design hangs effects off.

| Tier | The city's read on you |
|---|---|
| **Nameless** | Nobody. A hand of five and no story attached to it. |
| **Known** | The streets have heard the name. Doors are no longer all shut. |
| **Named** | A real reputation. People plan around you. |
| **Notorious** | Feared. Rivals think twice; factions take meetings. |
| **Legend** | The city tells stories about your crew. The stories are mostly true. |

Exact point values are playtest territory — the ladder is the contract, the numbers underneath it are tuning.

### 8.2 What Moves Cred

Cred rises from **power demonstrated where the city can see it** and falls from losses and from moves the street reads as weak.

**Raises cred:** winning matches (small); winning in dangerous districts like The Hush or The Vault (more); **beating Razorkin crews** specifically (large — they are the city's loudest scoreboard); flipping district control (§6.4); and **completing a Hunt by duel** — reclaiming a lost hero with your fists, not your wallet (large).

**Lowers cred:** losing matches (small); **buying out a hero** (small — see §8.4); and **Stepping Up**, which resets cred toward a low baseline (§8.5).

### 8.3 Razorkin Refusal — Cred's Signature Use

This is the use the system was born from (§7.4). When you request a buyout from a Razorkin captor, roll against a **refusal chance**:

> **Refusal = the Floor + a cred penalty.**

- **The Floor (~15–20%)** is immovable. No matter how legendary you are, the Razorkin sometimes simply *want the fight* — that is their character, and cred can never buy it away.
- **The cred penalty** scales down as you climb the ladder. At **Nameless** it is large — total refusal lands somewhere near ~65%. At **Legend** it is gone, and you face only the Floor.

So a respected crew is *usually* offered the deal, an unknown crew *usually* gets told to come fight, and nobody is ever fully safe from a Razorkin who just woke up wanting blood.

A refused buyout costs nothing extra — it does not burn a Reclaim attempt (buyout is separate, §7.4). It simply means *that* exit is shut for now: duel, or try the buyout again later. Cred respect is checked fresh each time you ask.

### 8.4 Cred's Broader Reach

Because cred is a broad stat, it shows up across the campaign — always as a **soft modifier, never a hard gate** (see §8.6):

- **Ransom prices (all factions).** A respected crew is dealt with more favorably; cred gives a modest discount on buyout costs across the markup table (§7.4). Lacquer still charges the most — but they charge a *Legend* less than a *Nobody*.
- **The Hunt, both directions.** Low-cred crews are seen as easy prey — rivals challenge them more often and ransom-deal less readily. High-cred crews are approached with caution and get better terms when *they* hold a rival's hero.
- **District control (§6.4).** Wins by a high-cred crew shift a district's control meter faster — the city *notices* when a name takes a board.
- **Scrip payouts.** Higher-cred matches play for bigger stakes; a Legend's wins simply pay more. This is the bridge between the two currencies — cred indirectly earns scrip.
- **District invitations.** Higher tiers get *invited* into richer, more dangerous districts sooner. An invitation, never a lock — see §8.6.

### 8.5 The Reset — A New Crew Is a New Name

When you **Step Up** (§7.5), cred does **not** wipe to zero. It resets to a **low baseline** — the bottom rungs of the ladder, around the **Known** floor.

The lore: the promoted hero and the mercs who survived the room *were* the old crew. The city doesn't know the new outfit, but a whisper of the old reputation clings to the people in it. You are starting over — not from nothing, but from a rumor.

### 8.6 The Guardrail — Cred Is Soft, Always

One hard rule, because §7.5 already made Step Up the player's low point: **cred only ever shifts probabilities, prices, and pacing — it never hard-gates content.** Zero cred means "Razorkin are more likely to want a fight" and "deals run pricier" — it never means "this district is locked" or "buyout unavailable."

The Hollow Choir already own the only hard "no buyout" wall in the game (§7.4). Cred must never build a second one — otherwise the §8.5 reset would compound a hero loss into a death spiral, exactly what the §7 design exists to prevent. Soft everywhere. No exceptions.

---

## 9. The Economy — Scrip, Fixers & Talent

This section designs how scrip is **earned**, how it is **spent**, and how the city's whole gig economy connects to the core loop. It resolves the original "scrip economy" open thread.

### 9.1 The Premise — An Analog Economy in a Haunted City

The Net is haunted (`lore.md` §7, the Hollow Choir). Because of that, **nobody in this city trusts the wire for anything that matters.** There is no job app, no marketplace, no listings — a contract is too valuable and too personal to route through infrastructure that something *lives in*. Work moves hand to hand, through human brokers called **Fixers.** The entire gig economy is defensively, deliberately **analog**, and the reason is dread. The mundane economy is the city's immune response to the thing behind the Wall.

**Scrip** has the same logic. It is debased corporate credit — originally Ascendant payroll currency that leaked into general use. The city trusts it precisely because it is **dumb**: just balances and chits, no identity in it, nothing for the Antecedent to reach. In a city where your face is your password, reassuringly stupid money is a *feature*.

### 9.2 Contracts — Missions as Brokered Duels

In this city the duel **is** the transaction (§2). So a mission is not "go kill X." A **Contract** is a brokered duel: a named **mark** (the crew you face), a **district** (which bakes in Protocols and a Hazard, §6), a **Stake** (§4.1), and a **payout**. A Fixer is simply someone who knows which duels are worth money. Contracts are not separate content bolted onto the loop — they are *curated duels* with narrative weight and a reward attached.

### 9.3 The Fixers

Each Fixer is a distinct economic faucet — they pay in different *kinds* of value and offer structurally different work. The starter cast:

| Fixer | Faction lean | Job structure | Pays in |
|---|---|---|---|
| **Della** | The Commons | **Standing Work** — a rotating list of simple single-duel contracts, low risk, always available; also offers **Mutual Aid** (§11.6) | Flat scrip. The economy's floor and its safety net. |
| **Vig** | Unaffiliated | **Wagers** — you stake your *own* scrip; win the duel to multiply it, lose it and it's gone. Double-or-nothing chains | Multiplied scrip — or nothing |
| **Atlas** | Ghostwire | **Listening Posts** — often odd, constrained puzzle-duels | **Knowledge** — hunted-hero locations, rival decks, hidden district Protocols |
| **Mrs. Oba** | Lacquer | **The Long Account** — linked contract chains; each job pays little, the full chain pays huge | Scrip on completion — *and debt if you walk away* |
| **The Tailor** | Effigy | **Ghost Contracts** — jobs you duel under a laundered identity | Scrip; results never touch your cred; can scrub a loss off the record |

- **Della** is the tutorial Fixer and the safety net. Her agenda: keep the block running. She is also the Commons' answer to the debt economy — when a crew cannot make the nut, Della offers **Mutual Aid** as the alternative to Lacquer debt (§11.6).
- **Vig** runs a pit; he is not a payout, he is a casino. Pure variance for crews who want it.
- **Atlas** is how Fixers gate **the Hunt** (§7.2) — you *buy* the location of your stolen hero from Atlas. Agenda: mapping the city, circling the Wall.
- **Mrs. Oba** is the most dangerous "safe" option. Abandoning one of her chains leaves you **in debt to Lacquer** — a cred hit, inflated buyouts, and Lacquer crews start hunting you. She is never threatening. She is acquiring you.
- **The Tailor** keeps a copy of your crew's face after every Ghost Contract — and Effigy does not collect anything it cannot sell. Those stored faces are *inventory*: an enemy crew you face may be wearing an operator's face the Tailor sold, and Effigy-built opponents can field point-symmetric **doubles** modelled on people you once ran. The full extent of the collection is left as a deliberate story hook (§14), but the mechanical seed is planted — every face you lend the Tailor can come back across the table.

### 9.4 Fixer Loyalty — Soft Exclusivity

You may work with **every Fixer freely** — there is no lockout. But each Fixer tracks a **loyalty** standing, and favoring one over time **unlocks depth**: better contracts, higher payouts, and access to **Vouched** free agents (§9.6). Mixing is always viable; commitment is rewarded, not required. The cost of spreading yourself thin is simply that no Fixer's deep content opens up — a soft pressure, never a wall.

### 9.5 The Dead Line — The Faustian Floor

Some contracts arrive with **no Fixer at all** — routed in over the haunted Net that everyone else refuses to touch, on a line that should not be ringing. This is **the Dead Line.**

- Dead Line contracts pay **obscene** scrip — far above anything a Fixer offers.
- The **mark is always wrong.** The crew you are sent against is unsettling: it plays strangely, it should not exist, or it is already half-Hollowed.
- Every Dead Line job you complete inflicts **the Hollowing** — the affliction system designed in full at §10.

The darkest reading, and the intended one: the Dead Line is the **Antecedent playing the same game everyone plays.** Capture is recruitment, all the way up — and the thing behind the Wall is headhunting *your crew*, one lucrative job at a time. The best money in the city is the Wall's money, and taking it is being slowly signed.

### 9.6 Free Agents — The Talent Market

Card acquisition is **spread across the whole economy** rather than sold from one shop. Capture (§4) poaches operators *from rival crews*. **Free agency** is the other half: signing operators who are **between crews** — "on the float."

- **The Meet.** You discover a free agent through a short interaction out in the districts — a **Meet**. The Meet is a *reading*: how the person talks reveals their stat geometry (`lore.md` design principle 1), so you know who you would be signing. A Meet adds that person to your **Shortlist** as a **Prospect.**
- **Signing a Prospect** happens three ways:
  1. **By scrip** — pay the sign-on fee. Ordinary Prospects.
  2. **By Audition** — the Prospect insists on proving the fit: you field them as a *guest card* in one contracted duel. Win and they sign; lose and they walk. The duel itself is the interview — the capture mechanic doing recruitment work directly.
  3. **Vouched** — a Fixer personally vouches for someone. These are the premium free agents: **named, unique, and a clear cut above** an ordinary Prospect's stats. Vouched Prospects open up through Fixer loyalty (§9.4).
- **Roster pressure.** Every signing adds a mouth to **Payroll** (§11) — free agency makes "you can't feed everyone" a live, recurring cost, not an abstraction.
- **Cred reach.** Higher Street Cred (§8) draws better talent toward you: more Meets, and Fixers vouch for stronger people. A Legend attracts operators; a Nameless crew gets scraps.

### 9.7 Marquee Contracts — The High-Stakes Tier

A **Marquee Contract** is a single, brutal, high-profile duel — the headline job. Its reward is a **choice**:

> A **hero-tier free agent** — an unsigned legend — **or** a large pile of scrip.

The hero-tier free agent is an orphaned former crew leader, now adrift: every one of them was once a promoted Top-Tier merc (§7.6), which is why free-agent heroes exist at all. Signing one is bench insurance against being made Headless (§7.1) and a second Domain for different districts (deck rule still holds — own many heroes, field one, `lore.md` §10).

The Marquee choice is the **whole economy's tension in miniature**: take the person, or take the money. It is the recurring decision the entire `systems.md` design keeps circling — operators are the point, and operators are expensive.

### 9.8 Faucets, Sinks & Acquisition — Summary

**Scrip faucets:** Contract payouts (the main flow); small flat win bonuses; cred-scaled stakes (§8.4); Vig's Wagers; the Dead Line (§9.5).

**Scrip sinks:** ransom / buyouts (§7.4); signing free agents (§9.6); **Payroll upkeep every overworld turn** (§11); district buy-ins (§6); buying intel from Atlas (§9.3); and the steep one — relaundering a Hollowed card via the Tailor (§10.3).

**Card-acquisition routes — deliberately spread:** capture from rivals (§4); counter-poach to take your cards back (§4.3); free-agent signing by scrip or Audition (§9.6); Vouched free agents via Fixer loyalty (§9.6); Marquee hero-tier free agents (§9.7); and promotion via Step Up (§7.5). No single shop sells people — the city makes you work every angle.

---

## 10. The Hollowing — The Affliction System

The Dead Line (§9.5) is the temptation. The Hollowing is the price, and it is designed here in full. The goal: a horror affliction with real terminal stakes, a true cleansing path that still costs something, and consequences that reach all the way into the endgame.

### 10.1 The Track — Clean → Touched → Fading → Claimed

The Hollowing is not a flag, it is a **per-card track**. Every operator sits at one of four stages:

| Stage | Effect on the card |
|---|---|
| **Clean** | Untouched. Normal in every way. |
| **Touched** | The **Toll** appears — the card's weakest edge drops to **0**, a dead silent side (`lore.md` §7). It now plays like a Hollow Choir card whether it wants to or not. |
| **Fading** | A second edge degrades sharply. The operator is visibly going — present, but thinning. |
| **Claimed** | Terminal. The card **defects to the Antecedent** (§10.4). |

Each completed Dead Line job advances the Hollowing **one step** — either Touching a new Clean card or pushing an already-afflicted one deeper. By default the player **chooses which fielded card takes the step**: you decide who you feed to it. But Dead Line contracts come in two grades, and the grade is **visible before you accept**:

- **Drifting contracts** — the common kind. You choose who takes the step.
- **Named contracts** — the Antecedent has someone specific in mind ("the one with the A"). The target is fixed by the contract, it is usually your best card or your hero, and the scrip is **even more obscene** to compensate. The premium *is* the bait: the Wall pays most for exactly the operator you can least afford to feed it.

### 10.2 The Cyberpsycho Discount

A Hollowed operator is a shell of the person they were — and a shell does not ask for much. **Touched or deeper, a card draws reduced Payroll** (§11). This is the genre's cyberpsycho logic: someone so far gone they have stopped wanting things. It is also a quiet, ugly second temptation — the economy itself rewarding you for letting people fade. Cheap labor, bought one piece of a soul at a time.

### 10.3 Cleansing — Manage It, Trade It, Never Erase It

There is a real cleansing path. There is no perfect restoration. Both of those are true on purpose.

- **The Cantor — the Lullaby.** A Hollow Choir warden (Choir register: Vesna, Threnody, Lamb… the Cantor) offers the **Lullaby**: it **halts** a card's progression, freezing it at its current stage so further Dead Line exposure cannot deepen *that* card. Repeatable, moderate scrip. It does not heal — it sings the Toll quiet, exactly as Threnody does for the Wall itself. This is the everyday safety valve. Fittingly grim: the affliction and the closest thing to a cure both come from the Wall's orbit.
- **The Tailor — the Relaunder.** The Effigy Fixer (§9.3) can roll a card **back one stage** — the only true reversal in the game — but the card returns **warped toward Effigy point-symmetry** (T=B, L=R). You trade the Toll for a different kind of not-being-yourself. Monstrous scrip cost.
- **No clean restoration.** A card that has ever been Touched can never be the original operator again. The Lullaby stops the slide; the Relaunder substitutes one wound for another. The Wall does not give anything back. That is the floor, and it is the point.

### 10.4 Claimed — When the Wall Completes the Hire

A card that reaches **Claimed** does not die. **It is taken.** It leaves your roster, defects to the Antecedent, and may resurface later as an enemy card — in a Dead Line mark, in the "Wall Is Thinning" variant (`lore.md` §12), or in the endgame (§12). Dying would be mercy. Being Claimed is recruitment finished — the thing behind the Wall plays the same game everyone plays, and it just closed the deal.

### 10.5 A Hollowed Hero — The Worst Loss in the Game

When a **hero** is Claimed, the crew goes **Headless** (§7.1) — but with none of §7's recovery scaffolding:

- **No Reclaim window.** The Antecedent is not a crew you can duel for your hero back.
- **No buyout.** The Wall does not sell, and there is no captor to bargain with.
- The only road out is a **forced Step Up** (§7.5).

A hero lost to capture starts a hunt. A hero lost to the Hollowing simply ends — quietly, with the player having watched it coming for several Dead Line jobs and chosen the money anyway. It is the single most punishing outcome in the design, and it should be.

---

## 11. Payroll & Debt — The Soft Cap

A crew cannot grow forever. Rather than a hard roster number, the ceiling is **economic** — you can keep exactly as many people as you can afford to pay.

### 11.1 Payroll & Making the Nut

Every operator on the roster draws scrip. Each **overworld turn**, the crew must **make the nut** — pay **Upkeep** equal to the sum of every card's cost, **scaled by tier**: Street is cheap, Hero is expensive, with Pro and Top-Tier between. First-pass values are in **Appendix A.3** (1 / 2 / 4 / 8 by tier).

A roster of legends is genuinely expensive to keep fed — which keeps stockpiling heroes (via §9.7 Marquee free agents or §7 succession) a real cost, not a free hoard. Hollowed cards draw **reduced** Upkeep (§10.2).

### 11.2 The Escalation Path

The soft cap is self-correcting and *is* the campaign's growth curve:

> Small crew → small income → small nut. Grow **cred** (§8) → contracts pay more (§8.4) → afford more operators → bigger nut → need bigger contracts.

There is no arbitrary roster limit because none is needed. "You can't feed everyone" stops being a slogan and becomes literally true every overworld turn. Growth is always paid for.

### 11.3 Deficit — Taking On Debt

When the crew cannot make the nut, you may **take on debt** rather than immediately cut anyone. Debt does not force a release. Debt sends **Collectors**.

### 11.4 The Collectors — A Clock You Can Slow, Never Stop

Debt in this city is Lacquer's domain (§9.3, Mrs. Oba). While you carry debt, **Collectors come for it** — as duels.

- **Win a Collector duel and you defer the debt.** The threat clears for now; the debt itself remains.
- **But the next Collector is stronger.** Each deferral escalates the ladder — tougher decks, then Lacquer enforcer crews, then a Lacquer hero crew.
- The ladder climbs toward an opponent **you cannot beat.** There is always someone bigger and more terrifying coming. Strength buys *time* — it never buys *out*.
- **The only true exit is paying the debt down in scrip.** Fighting only dels the clock; settling the books stops it.

**When the unbeatable Collector arrives** and collects, the crew is **broken up** — operators scattered to the float and to Lacquer's books — and the campaign cycle ends in the gutter: a hard reset to a new Nameless crew, with **none** of Prestige's rewards (§12). This is the campaign's *failure* ending, and it is the deliberate opposite of Prestige's *triumph* ending. A crew can ascend to the Skyline, or it can sink under the nut. Debt decides which.

**Who the terminal Collector is** mirrors the two Prestige fates (§12.4). For most crews it is **Madame Sumi's own crew** — the Ledger come to collect in person, the matriarch of debt closing her oldest account. But for a crew that also carries the **Hollowing**, the debt has been quietly *sold down the Dead Line*: an unpaid balance, left long enough, gets noticed by the thing behind the Wall, and the terminal Collector is an **Antecedent horror crew** instead. Debt-death forks on the Hollowing exactly as ascension does — the gutter has a Lacquer floor and a Wall basement.

### 11.5 Debt — Leverage or Trap

Debt is one mechanic that lives two completely different lives, and the difference is the design's quiet thesis: **the rules never change — only your standing does, and that is the cruelty.**

Every turn a debt is carried, it accrues **interest**, and the interest rate scales **inversely with cred** (§8): the more the city respects you, the less your debt costs you. See Appendix A for first-pass rates.

- **For a strong crew, debt is leverage.** A high-cred crew's interest is trivial and its income dwarfs the service cost, so debt becomes a *tool* — borrow against tomorrow to sign a Marquee hero today, carry a balance indefinitely, finance a district buy-in. Used deliberately, debt makes a rich crew richer. This is allowed, and it is meant to be: it is how the powerful actually use credit.
- **For a weak crew, debt is a trap.** A low-cred crew pays predatory interest it cannot even cover, so the principal *compounds*. Each turn the debt is larger, Collector Heat (§11.4) climbs faster, and the ladder accelerates toward the unbeatable Collector. The same contract that lifts the strong buries the poor.

The punishment is therefore a **sliding scale**, and it slides *insidiously*. Early game the nut is tiny and debt is unlikely, so the economy feels gentle. The trap only closes on a crew that grew its roster faster than its reputation — and by the time the interest is visibly outrunning the income, the hole is already deep. Debt does not announce itself as a threat. That is the point, and it is the atmosphere.

### 11.6 The Commons Alternative — Mutual Aid

The poor are not only prey to debt — they have each other. When a crew cannot make the nut, it does not *have* to go to Lacquer. It has a choice, and the choice is two Fixers:

- **Mrs. Oba (Lacquer)** sells you **debt** — §11.5. Fast, flexible, leverageable if you are strong, a pit if you are not.
- **Della (the Commons)** offers **Mutual Aid** — the block quietly covers your shortfall. **No interest. No Collectors. No spiral.** The cost is **Obligation**: you owe the Commons *labor*, repaid by taking Della's Standing Work contracts at reduced or zero scrip pay until the Obligation clears.

Mutual Aid cannot trap you — there is no compounding, no Collector ladder, no terminal crew. But it cannot lift you either: while you are working off Obligation you are not getting richer, only even. It is not a tool and it is not a pit. It is survival, shared.

This is the statement made playable. Lacquer debt and Commons aid are the two relationships money has with people who do not have enough of it — one a ladder for those who were never really poor, one a hand from those who still are. The crew chooses which city it lives in every time the nut comes due. And mechanically, this finally gives **the Commons real teeth in the meta layer**: their faction identity is no longer only "weak alone, strong together" on the board — it is a genuine, structural alternative to the debt economy.

---

## 12. Prestige & the Endgame

The campaign has a top, and reaching it does not end the game — it **reseeds** it.

### 12.1 Joining the Skyline

When a crew tops out — maximum **cred** (Legend, §8.1) and/or taking **The Vault** (§6.2, the endgame Contested district) — the player may **Prestige**. The crew "joins the **Skyline**": it ascends out of the grind and becomes a permanent fixture of the city, one of the towering fixed names everyone else now navigates around.

You then start fresh — a new crew, **Nameless**, a hand of five, the whole climb again.

### 12.2 The First-Prestige Surprise

This is designed to be discovered, not announced. The first time a player Prestiges, the reveal lands on its own: **your old crew is still out there.** It persists in the new campaign as an AI-controlled **Skyline crew** — a recurring apex antagonist running the exact deck, hero, and roster *you* built. You will, eventually, have to face yourself.

The player should not see this coming the first time. Every Prestige after, they will — and they will build with it in mind.

### 12.3 The Gallery — Replayability

Each Prestige adds another former self to the city's pool of Skyline crews. Over many runs the map fills with a **gallery of your own past crews** as elite bosses — a personal history made playable. Beating a Skyline crew grants a **Reunion**: you may sign **one operator** from that old crew into your new one — a single card carried across the generation gap. The campaign's long tail is fighting, and partially reclaiming, everyone you used to be.

### 12.4 Two Ascensions — The Skyline and the Wall

How you played decides *how* you ascend. This is the payoff of every Dead Line choice across a whole campaign.

- **A clean crew joins the Skyline** — a legendary rival, tough but mortal, beatable, Reunion-eligible.
- **A crew that reaches Prestige still carrying the Hollowing ascends the wrong way.** It is **Claimed at the scale of the whole crew** (§10.4) and returns not as a Skyline rival but as an **Antecedent-aligned horror boss** — a dread encounter, not a duel between peers. The Wall got them in the end.

The Dead Line's scrip was real, and it was always a loan. A crew either climbs into the Skyline or is dragged behind the Wall — and the player authored which, one lucrative job at a time.

**The threshold.** A Prestige goes to the Wall, not the Skyline, if the Hollowing has genuinely *cost the crew its core* by the time it ascends — specifically, if **either** of the following is true at Prestige: the crew has ever lost a card to **Claimed** (§10.4), **or** the hero it Prestiges with is **Touched or deeper**. A crew that merely dabbled — a single Touched Street card, cleansed or quietly carried — still earns the Skyline. The horror fate is reserved for crews the Hollowing actually *took something from*. Dabbling is survivable; letting it reach the heart is not.

---

## 13. How This Connects Back to `lore.md`

- **Protocols** give every faction a fingerprint on the *rules*, not just the cards — deepening the §4 faction identities.
- **Cascade** and **Signal Bleed** are the everyday face of the Hollow Choir's Breach and the "Wall Is Thinning" variant (`lore.md` §5, §12) — horror leaking into ordinary play.
- **Recruitment** turns the capture mechanic into the campaign's core fantasy and is the natural engine of the splash decks described in `lore.md` §10.
- **Districts** finally answer `lore.md`'s open thread on a "faction relationship web" — the map *is* the web, and control of it is the campaign.
- **Succession** (§7.6) answers where new heroes come from — they are promoted Top-Tier mercs — making the `lore.md` §3 Hero tier earned and recursive.
- **Street Cred** (§8) gives Razorkin a reputation-based identity in the *meta* layer, matching the lopsided, respect-power character of their cards in `lore.md` §7.
- **The Economy** (§9) gives every Fixer a faction lean, turns the Antecedent into an active headhunter via the Dead Line, and ties the analog gig economy directly to `lore.md`'s Black Wall horror.
- **The Hollowing** (§10) makes `lore.md`'s Toll a living affliction that can spread from Choir cards onto *your* roster — and makes the Antecedent a recruiter, closing the loop on capture-as-recruitment.
- **Payroll & Prestige** (§11–§12) frame a full campaign arc with two endings — the Skyline and the Wall — both authored by how the player engaged the `lore.md` factions and the Dead Line.
- **Debt** (§11.5–§11.6) gives Lacquer and the Commons opposed economic identities — leverage versus mutual aid — turning `lore.md`'s "everyone owes Lacquer" and "the Commons, weak alone, strong together" into a single playable statement.
- **The Athena Protocol** (`lore.md` §7) is the campaign's guide voice and conscience — the diegetic tutorial, the reaction to the player's choices, and the figure whose revealed nature recontextualizes the Dead Line, the Hollowing, and both Prestige fates.

---

## 14. Open Threads

Most earlier threads are now resolved (§10.1 Named contracts, §11.4 terminal Collector, §11.5–§11.6 debt design, §12.4 Prestige threshold, §6.4 AI crews, §5.1 Shaken, the title). What genuinely remains:

- **The Tailor's collection** (§9.3) — the *doubles* hook is seeded mechanically; the larger story of what Effigy is building toward is left open on purpose.
- **The tutorial, built around Athena.** The campaign's onboarding should be delivered diegetically by the Athena Protocol (`lore.md` §7) — she is the guide voice. The exact reveal beat (when the player learns *what* she is) and the tutorial's pacing across the early districts still need a structural pass.
- **All Appendix A numbers** — these are first-pass baselines, explicitly meant to be played and revised. They are a skeleton, not a balance pass.

---

## Appendix A — First-Pass Numbers

**Read this as a skeleton, not a balance pass.** Every value below is a starting point chosen to be internally consistent for a **medium campaign (~40–60 contracts, Nameless → Prestige)**. They exist so playtesting has something concrete to push against instead of adjectives. Expect to revise all of them.

### A.1 The Cred Ladder

A 0–100 scale. Step Up (§8.5) resets cred to **20** — the Known floor.

| Tier | Cred range |
|---|---|
| Nameless | 0–19 |
| Known | 20–39 |
| Named | 40–59 |
| Notorious | 60–79 |
| Legend | 80–100 |

Cred change per event (first pass): contract win **+2**; win in a dangerous district **+4**; beating a Razorkin crew **+5**; flipping district control **+5**; completing a Hunt by duel **+6**. Contract loss **−2**; buying out a hero **−4**.

### A.2 Cred's Effects

| Tier | Income ×mult (§8.4) | Razorkin refusal (§8.3) | Debt interest / turn (§11.5) | Ransom discount (§8.4) |
|---|---|---|---|---|
| Nameless | ×1.0 | ~65% (18% Floor + 47%) | 25% | 0% |
| Known | ×1.3 | ~50% | 18% | −3% |
| Named | ×1.6 | ~38% | 10% | −5% |
| Notorious | ×2.0 | ~26% | 5% | −8% |
| Legend | ×2.5 | ~18% (Floor only) | 2% | −10% |

The **debt-interest column is the §11.5 thesis in one place**: the same balance costs a Nameless crew 25% a turn and a Legend 2%.

### A.3 Payroll — Upkeep per Card per Overworld Turn

| Tier | Upkeep | Hollowed (Touched+) |
|---|---|---|
| Street | 1 | 1 (already minimal) |
| Pro | 2 | 1 |
| Top-Tier | 4 | 2 |
| Hero | 8 | 4 |

Hollowed cards pay **half**, rounded down, min 1 — the §10.2 cyberpsycho discount.

### A.4 Contract Payouts — Base, Before Multipliers

Final payout = base × cred mult (A.2) × district danger mult.

| Contract type | Base scrip |
|---|---|
| Della Standing Work | 10 |
| Standard Fixer contract | 20 |
| Long Account / chain link | 8 each, **+80 chain-completion bonus** |
| Marquee (§9.7) | 80 — *or* the hero-tier free agent |
| Dead Line, Drifting (§10.1) | 120 + the Hollowing |
| Dead Line, Named (§10.1) | 200 + the Hollowing on a fixed target |

District danger mult: The Stub ×1.0; faction districts ×1.3–1.6; The Hush / The Vault ×2.0.

### A.5 Worked Example — Why the Slide Is Insidious

- **Early (Nameless).** Roster 5 (1 Pro + 4 Street) → Upkeep **6**/turn. Della contract: 10 × 1.0 = **10**. Net **+4**. Gentle, positive.
- **Mid, balanced (Named).** Roster 12 (1 Hero + 3 Top-Tier + 4 Pro + 4 Street) → Upkeep **8+12+8+4 = 32**/turn. Standard contract in a ×1.4 district: 20 × 1.6 × 1.4 ≈ **45**. Net **+13**. Healthy — because cred grew *with* the roster.
- **Mid, over-extended (still Known).** Same 12-card roster, Upkeep **32**, but reputation lagged: 20 × 1.3 × 1.4 ≈ **36**. Net **+4** — and one bad turn tips into debt at **18%**. The roster outran the name, and the trap is now quietly closing.
- **Late (Legend).** Roster 18, Upkeep ~70/turn. Vault-tier contract: 40 × 2.5 × 2.0 = **200**. Net ~**+130**, debt interest a trivial 2%. Debt is now pure leverage.

### A.6 Other First-Pass Values

- **Step Up reset** — cred to 20 (A.1).
- **Shaken** (§5.1) — the card's lowest edge is treated as **0** for its first match after joining the crew by capture; clears after one match. A card made Shaken a **3rd** time becomes permanently **Calloused** — lowest edge 0 for good. The threshold is tunable.
- **Collector Heat** (§11.4) — +1 per overworld turn if debt < one turn's income; +2 if debt is 1–3× income; +3 if greater. A Collector arrives every 6 Heat; each successive Collector is one tier stronger than the last.
- **Reclaim window** (§7.3) — 2 attempts, unchanged.
- **Buyout escalation** (§7.4) — +60% per failed duel, within the §7.4 ~50–75% band.
