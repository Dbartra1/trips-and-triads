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

        // Hunt state
        sess["capturedHeroId"]          = session.CapturedHero?.Id ?? "";
        sess["capturedHeroFull"]        = session.CapturedHero != null && string.IsNullOrEmpty(session.CapturedHero.Id)
                                          ? SerializeCard(session.CapturedHero) : null;
        sess["capturingFaction"]        = (int)session.CapturingFaction;
        sess["reclamationAttemptsLeft"] = session.ReclamationAttemptsLeft;
        // isHuntMatch and heroReclaimed are transient — not saved
        sess["interimHeroId"]           = session.InterimHero?.Id ?? "";
        sess["interimHeroFull"]         = session.InterimHero != null && string.IsNullOrEmpty(session.InterimHero.Id)
                                          ? SerializeCard(session.InterimHero) : null;
        sess["deckWhenHeroWasCaptured"] = SerializeCardRefList(session.DeckWhenHeroWasCaptured);

        // Reunion state
        sess["reunionPending"]      = session.ReunionPending;
        sess["reunionOriginalId"]   = session.ReunionOriginal?.Id ?? "";
        sess["reunionInterimId"]    = session.ReunionInterim?.Id ?? "";

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
            GD.PrintErr($"SaveManager: could not open {SavePath} for writing.");
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
        var capturedId = sess["capturedHeroId"]?.ToString();
        if (!string.IsNullOrEmpty(capturedId))
        {
            var captured = FindCardInRoster(session.Roster, capturedId)
                           ?? DeserializeSingleCard(sess["capturedHeroFull"]);
            if (captured != null)
            {
                var faction = (Faction)(int)(sess["capturingFaction"] ?? 0);
                int attempts = (int)(sess["reclamationAttemptsLeft"] ?? 2);
                session.LoadHuntState(captured, faction, attempts);
            }
        }

        var interimId = sess["interimHeroId"]?.ToString();
        if (!string.IsNullOrEmpty(interimId))
        {
            var interim = FindCardInRoster(session.Roster, interimId)
                          ?? DeserializeSingleCard(sess["interimHeroFull"]);
            if (interim != null) session.LoadInterimHero(interim);
        }

        var deckSnap = sess["deckWhenHeroWasCaptured"]?.AsArray();
        if (deckSnap != null)
        {
            var snap = new List<CardData>();
            foreach (var idNode in deckSnap)
            {
                var card = FindCardInRoster(session.Roster, idNode?.ToString());
                if (card != null) snap.Add(card);
            }
            session.LoadDeckSnapshot(snap);
        }

        // isHuntMatch and heroReclaimed are transient — not restored

        // Reunion state
        bool reunionPending = (bool)(sess["reunionPending"] ?? false);
        if (reunionPending)
        {
            var origId   = sess["reunionOriginalId"]?.ToString();
            var interId  = sess["reunionInterimId"]?.ToString();
            var origCard = FindCardInRoster(session.Roster, origId);
            var interCard = FindCardInRoster(session.Roster, interId);
            if (origCard != null && interCard != null)
                session.LoadReunionState(origCard, interCard);
        }

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

        GD.Print($"SaveManager: game loaded. Roster: {session.Roster.Count} cards.");
        return true;
    }

    public static void DeleteSave()
    {
        if (SaveExists())
        {
            DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));
            GD.Print("SaveManager: save deleted.");
        }
    }

    // ── Serialization helpers ─────────────────────────────────────────────────

    private static JsonArray SerializeCardList(List<CardData> cards)
    {
        var arr = new JsonArray();
        foreach (var card in cards)
            arr.Add(SerializeCard(card));
        return arr;
    }

    /// <summary>For deck/snapshot references — save Id only (card is already in roster).</summary>
    private static JsonArray SerializeCardRefList(List<CardData> cards)
    {
        var arr = new JsonArray();
        foreach (var card in cards)
            arr.Add(card.Id ?? "");
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

    private static CardData FindCardInRoster(List<CardData> roster, string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        // Match by Id first, then by Name for procedural cards (Id is empty)
        return roster.Find(c => c.Id == id)
               ?? roster.Find(c => c.Name == id);
    }
}