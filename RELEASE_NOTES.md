# Trips & Triads - Build Release Notes
**Version**: Phase 9 Economy & Recruitment Update  
**Date**: 2026-06-08  

---

## 🚀 New Features

### 💰 Scrip Economy System
- Added persistent **Scrip** currency to the player's campaign state.
- Scrip is now saved and loaded correctly between sessions.
- Post-match payouts now correctly calculate and award Scrip based on district danger multipliers and Street Cred tier.

### 📋 Free Agent Recruitment
- Added a new **Recruitment** tab to the Pre-Match screen.
- Implemented a 3-step hiring flow:
  1. **Meet (5 Scrip)**: Reveals the agent's identity and stats.
  2. **Audition (10 Scrip)**: Tests the agent (includes a 10% failure chance for realism).
  3. **Sign (15 Scrip)**: Permanently adds the agent to your roster.
- Recruitment board automatically refreshes with new agents after every standard District Match.
- Newly signed agents are immediately visible in the roster and saved to disk.

### 🛠️ Della Standing Work Contracts
- Added low-risk, base-rule matches accessible via the **Della** tab.
- Contracts are limited to **3 per district cycle** to prevent infinite farming.
- Successfully completing a standard District Match automatically refreshes Della's contract board back to 3.
- Contract rewards scale dynamically with the player's Street Cred tier.

---

## 🎨 UI / UX Improvements

### Pre-Match Screen Overhaul
- **Compact Fixer Tabs**: Replaced the sprawling vertical layout with a clean `TabContainer` (Della / Recruitment), freeing up crucial screen space.
- **Popup Recruitment Board**: Moved the recruitment UI into a centered, scrollable modal window to prevent main-screen clutter.
- **Deck Builder Width Lock**: Forced the deck grid to reserve space for 5 cards at all times, preventing the "New Run" button from being pushed off-screen when the deck is empty.
- **Visible Validation Warnings**: Attempting to start a match with an incomplete deck now triggers a bright, temporary warning on the deck counter (`⚠ Must select 5 cards first!`).
- **Text Padding**: Added consistent left-margin padding to tab text and popup lists for better visual alignment.

### Recruitment Card Polish
- Increased card size by ~40% (`140x180`) for better readability.
- Upgraded font sizes for stats (16/18px) and names (18px) to prevent text from running together.
- Improved internal spacing and margins within the card layout.

---

## 🐛 Bug Fixes

- **Fixed**: Compilation error in `ScoreInvariantTests.cs` (`Cascade` property renamed to `Overflow`).
- **Fixed**: Della contract button occasionally rendering with blank text.
- **Fixed**: Recruitment cards stretching into long, broken rectangles due to Godot 4 `VBoxContainer` size flag behavior (now properly locked to `ShrinkBegin`).
- **Fixed**: Recruitment cards touching the left edge of the popup window (resolved using `MarginContainer` wrappers).
- **Fixed**: Newly signed Free Agents not visually appearing in the roster until a secondary UI refresh was triggered.
- **Fixed**: "Run Over" state hijacking the "Start Match" button. The button now cleanly hides when the run is over, leaving only the dedicated "New Run" button accessible.

---

## 📝 Known Issues / Future Work
- **Mutual Aid / Debt System**: The scaffolding for Obligation is noted, but the full Phase 11 debt/upkeep loop is not yet active.
- **Additional Fixers**: Vig (Wagers), Atlas (Intel), Mrs. Oba (Debt), and The Tailor (Relaundering) are queued for future tabs in the Fixer menu.

---
*For detailed mechanical rules, please refer to `systems.md` and `lore.md`.*
