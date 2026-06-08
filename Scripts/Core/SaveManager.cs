using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using TripsAndTriads.Core;

/// <summary>
/// Handles save and load of all persistent campaign state.
///
/// Save file: user://savegame.json
/// Format: JSON with two top-level keys — "session" and "districts".
///
/// Strategy for CardData:
///   • Named cards (Id != null and exists in CardDatabase) — save only the Id
///     plus any mutations (stats, tier, domain, ability) so mutations are
///     preserved even when the template changes.
///   • Procedurally generated cards (Id == null) — save all fields in full.
///
/// Call SaveGame() after every match result is applied.
/// Call LoadGame() once in GameSession._Ready() if a save file exists.
/// Call DeleteSave() on New Run.
/// </summary>
public static class SaveManager
{
    private const string SavePath = "user://savegame.json";

    // ── Public API ────────────────────────────────────────────────────────────

    public static bool SaveExists() => FileAccess.FileExists(SavePath);

    /// <summary>
    /// Returns the OS-level absolute path to the save file. Useful for bug reports
    /// and diagnosing distributed-build save failures. Logs on first call.
    /// </summary>
    public static string GetSaveFilePath()
    {
        var path = ProjectSettings.GlobalizePath(SavePath);
        GD.Print($"SaveManager: save file path = {path}");
        return path;
    }

    public static void SaveGame()
    {
        var session  = GameSession.Instance;
        var dm       = DistrictManager.Instance;
        if (session == null || dm == null) return;

        var root = new JsonObject();

        // ── Session ───────────────────────────────────────────────────────────
        var sess = new JsonObject();
        sess["roster"] = SerializeCardList(session.Roster);
        // SelectedDeck and IsHuntMatch are NOT persisted — transient match state.
        // PreMatchScreen always rebuilds the deck fresh each session.

        // Hunt state — captured hero is NOT in the roster so always save in full.
        // Interim and reunion cards ARE in the roster; save by Name for procedural cards
        // (procedural cards have no Id, so Name is the reliable unique key within a run).
        sess["capturedHeroFull"]        = session.CapturedHero != null
                                          ? SerializeCard(session.CapturedHero) : null;
        sess["capturingFaction"]        = (int)session.CapturingFaction;
        sess["reclamationAttemptsLeft"] = session.ReclamationAttemptsLeft;
        // isHuntMatch and heroReclaimed are transient — not saved
        sess["interimHeroKey"]          = CardKey(session.InterimHero);
        sess["interimHeroFull"]         = session.InterimHero != null
                                          ? SerializeCard(session.InterimHero) : null;
        sess["deckWhenHeroWasCaptured"] = SerializeCardKeyList(session.DeckWhenHeroWasCaptured);

        // Reunion state
        sess["reunionPending"]      = session.ReunionPending;
        sess["reunionOriginalKey"]  = CardKey(session.ReunionOriginal);
        sess["reunionInterimKey"]   = CardKey(session.ReunionInterim);

        // ── Street Cred & district access ─────────────────────────────────────
        sess["cred"]  = session.Cred.Cred;
        sess["scrip"] = session.Scrip;
        sess["dellaContracts"] = session.DellaContractsAvailable;

        var gracePeriods = new JsonObject();
        foreach (var kvp in session.DistrictGracePeriods)
            gracePeriods[kvp.Key] = kvp.Value;
        sess["gracePeriods"] = gracePeriods;

        root["session"] = sess;

        // ── Districts ─────────────────────────────────────────────────────────
        var districts = new JsonObject();
        districts["activeDistrictId"] = dm.ActiveDistrictId;
        var meters = new JsonObject();
        foreach (var (id, val) in dm.GetAllMeters())
            meters[id] = val;
        districts["controlMeters"] = meters;
        root["districts"] = districts;

        // ── Write ─────────────────────────────────────────────────────────────
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PrintErr($"SaveManager: could not open {SavePath} for writing. " +
                        $"OS path: {ProjectSettings.GlobalizePath(SavePath)}. " +
                        $"FileAccess error: {FileAccess.GetOpenError()}");
            return;
        }
        file.StoreString(root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        GD.Print("SaveManager: game saved.");
    }

    public static bool LoadGame()
    {
        if (!SaveExists()) return false;

        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);
        if (file == null) { GD.PrintErr("SaveManager: could not open save file."); return false; }

        JsonNode root;
        try { root = JsonNode.Parse(file.GetAsText()); }
        catch (System.Exception e) { GD.PrintErr($"SaveManager: JSON parse error — {e.Message}"); return false; }

        var session = GameSession.Instance;
        var dm      = DistrictManager.Instance;
        if (session == null || dm == null) return false;

        // Initialize meters to defaults first so SetMeter calls have valid keys.
        dm.Initialize();

        // ── Session ───────────────────────────────────────────────────────────
        var sess = root?["session"];
        if (sess == null) return false;

        session.LoadRoster(DeserializeCardList(sess["roster"]));
        // SelectedDeck is not restored — PreMatchScreen rebuilds it each session.

        // Hunt state
        // Captured hero is always deserialized from full data (not in roster).
        var captured = DeserializeSingleCard(sess["capturedHeroFull"]);
        if (captured != null)
        {
            var faction  = (Faction)(int)(sess["capturingFaction"] ?? 0);
            int attempts = (int)(sess["reclamationAttemptsLeft"] ?? 2);
            session.LoadHuntState(captured, faction, attempts);
        }

        // Interim is in the roster — find by Name (key) then fall back to full data.
        var interimKey = sess["interimHeroKey"]?.ToString();
        var interim    = FindByKey(session.Roster, interimKey)
                         ?? DeserializeSingleCard(sess["interimHeroFull"]);
        if (interim != null) session.LoadInterimHero(interim);

        var deckSnap = sess["deckWhenHeroWasCaptured"]?.AsArray();
        if (deckSnap != null)
        {
            var snap = new List<CardData>();
            foreach (var keyNode in deckSnap)
            {
                var card = FindByKey(session.Roster, keyNode?.ToString());
                if (card != null) snap.Add(card);
            }
            session.LoadDeckSnapshot(snap);
        }

        // isHuntMatch and heroReclaimed are transient — not restored

        // Reunion state
        bool reunionPending = (bool)(sess["reunionPending"] ?? false);
        if (reunionPending)
        {
            var origCard  = FindByKey(session.Roster, sess["reunionOriginalKey"]?.ToString());
            var interCard = FindByKey(session.Roster, sess["reunionInterimKey"]?.ToString());
            if (origCard != null && interCard != null)
                session.LoadReunionState(origCard, interCard);
        }

        // ── Street Cred & district access ─────────────────────────────────────
        int savedCred = (int)(sess["cred"] ?? 0);
        session.Cred.Apply(savedCred); // Apply from 0 to the saved value

        int savedScrip = (int)(sess["scrip"] ?? 0);
        session.LoadScrip(savedScrip);

        int savedDella = (int)(sess["dellaContracts"] ?? 3);
        session.DellaContractsAvailable = System.Math.Clamp(savedDella, 0, GameSession.MaxDellaContracts);

        var savedGrace = sess["gracePeriods"]?.AsObject();
        if (savedGrace != null)
            foreach (var kvp in savedGrace)
                session.DistrictGracePeriods[kvp.Key] = (int)(kvp.Value ?? 0);

        // Pre-seed any hard-lock districts missing from the save file.
        // Covers: saves created before grace period support was added, and any
        // newly added districts. Without this, a missing entry looks "newly
        // dropped below threshold" to TickGracePeriods and fires a false warning.
        foreach (var districtId in DistrictAccess.HardLockIds())
            if (!session.DistrictGracePeriods.ContainsKey(districtId))
                if (!DistrictAccess.MeetsTierRequirement(districtId, session.Cred.Tier))
                    session.DistrictGracePeriods[districtId] = 0;

        // ── Districts ─────────────────────────────────────────────────────────
        var dists = root["districts"];
        if (dists != null)
        {
            var activeId = dists["activeDistrictId"]?.ToString() ?? "the_stub";
            dm.SelectDistrict(activeId);

            var meters = dists["controlMeters"]?.AsObject();
            if (meters != null)
                foreach (var kvp in meters)
                    dm.SetMeter(kvp.Key, (int)(kvp.Value ?? 50));
        }

        GD.Print($"SaveManager: game loaded. Roster: {session.Roster.Count} cards. " +
                 $"Cred: {session.Cred.Cred} ({session.Cred.Tier}).");
        return true;
    }

    public static void DeleteSave()
    {
        if (!SaveExists()) return;

        // DirAccess.Open("user://") is reliable in both editor and exported builds.
        // DirAccess.RemoveAbsolute(GlobalizePath(...)) can fail on some platforms
        // when the globalized path format differs from what the OS expects.
        var dir = DirAccess.Open("user://");
        if (dir != null)
        {
            var err = dir.Remove("savegame.json");
            if (err == Error.Ok)
                GD.Print("SaveManager: save deleted.");
            else
                GD.PrintErr($"SaveManager: failed to delete save — error {err}.");
        }
        else
        {
            GD.PrintErr("SaveManager: could not open user:// directory to delete save.");
        }
    }

    // ── Serialization helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the best unique key for a card.
    /// Named cards (in cards.json) use their database Id.
    /// Procedural cards have no Id — use Name, which is unique within a run.
    /// </summary>
    private static string CardKey(CardData card)
    {
        if (card == null) return "";
        return !string.IsNullOrEmpty(card.Id) ? card.Id : card.Name;
    }

    /// <summary>Find a card in the roster by its CardKey (Id first, then Name).</summary>
    private static CardData FindByKey(List<CardData> roster, string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        return roster.Find(c => !string.IsNullOrEmpty(c.Id) && c.Id == key)
               ?? roster.Find(c => c.Name == key);
    }

    private static JsonArray SerializeCardList(List<CardData> cards)
    {
        var arr = new JsonArray();
        foreach (var card in cards)
            arr.Add(SerializeCard(card));
        return arr;
    }

    /// <summary>For deck/snapshot references — save CardKey so lookup works for both named and procedural cards.</summary>
    private static JsonArray SerializeCardKeyList(List<CardData> cards)
    {
        var arr = new JsonArray();
        foreach (var card in cards)
            arr.Add(CardKey(card));
        return arr;
    }

    private static JsonObject SerializeCard(CardData card)
    {
        var obj = new JsonObject();
        obj["id"]          = card.Id ?? "";
        obj["name"]        = card.Name;
        obj["top"]         = card.Top;
        obj["right"]       = card.Right;
        obj["bottom"]      = card.Bottom;
        obj["left"]        = card.Left;
        obj["level"]       = card.Level;
        obj["element"]     = card.Element ?? "";
        obj["artPath"]     = card.ArtPath ?? "";
        obj["faction"]     = (int)card.Faction;
        obj["tier"]        = (int)card.Tier;
        obj["domainType"]  = (int)card.DomainType;
        obj["abilityType"] = (int)card.AbilityType;
        return obj;
    }

    private static List<CardData> DeserializeCardList(JsonNode node)
    {
        var list = new List<CardData>();
        if (node is not JsonArray arr) return list;
        foreach (var item in arr)
        {
            var card = DeserializeSingleCard(item);
            if (card != null) list.Add(card);
        }
        return list;
    }

    private static CardData DeserializeSingleCard(JsonNode node)
    {
        if (node is not JsonObject obj) return null;

        var id = obj["id"]?.ToString();

        // If it's a named card, start from the database template then apply saved mutations
        CardData card = null;
        if (!string.IsNullOrEmpty(id))
            card = CardDatabase.Instance.GetCard(id)?.ShallowClone();

        // Fall back to full construction (procedural card or template not found)
        card ??= new CardData();

        card.Id          = id ?? "";
        card.Name        = obj["name"]?.ToString() ?? "";
        card.Top         = (int)(obj["top"]    ?? 0);
        card.Right       = (int)(obj["right"]  ?? 0);
        card.Bottom      = (int)(obj["bottom"] ?? 0);
        card.Left        = (int)(obj["left"]   ?? 0);
        card.Level       = (int)(obj["level"]  ?? 1);
        card.Element     = obj["element"]?.ToString() ?? "";
        card.ArtPath     = obj["artPath"]?.ToString() ?? "";
        card.Faction     = (Faction)(int)(obj["faction"]     ?? 0);
        card.Tier        = (Tier)(int)(obj["tier"]           ?? 0);
        card.DomainType  = (DomainType)(int)(obj["domainType"]  ?? 0);
        card.AbilityType = (AbilityType)(int)(obj["abilityType"] ?? 0);
        return card;
    }
}