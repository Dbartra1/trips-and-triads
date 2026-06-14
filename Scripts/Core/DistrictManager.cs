using Godot;
using System.Collections.Generic;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Manages district state for a campaign session:
	/// - Tracks which district is currently selected for a match
	/// - Tracks control meters per district (shifts via the Spreading Rule)
	/// - Builds the MatchConfig for the selected district
	/// - Applies the Spreading Rule when a match is won
	///
	/// For Phase 6 the control meter is a simple 0–100 int per district,
	/// starting at 50 for contested districts and 75 for faction-owned ones.
	/// </summary>
	public class DistrictManager
	{
		private static DistrictManager _instance;
		public  static DistrictManager Instance => _instance ??= new DistrictManager();

		// Control meters keyed by district Id. 0 = fully P1 control, 100 = fully AI control.
		private Dictionary<string, int> _controlMeters = new();

		// Currently selected district Id
		public string ActiveDistrictId { get; private set; } = "the_stub";

		public DistrictData ActiveDistrict =>
			DistrictDatabase.Instance.GetDistrict(ActiveDistrictId);

		public void Initialize()
		{
			_controlMeters.Clear();
			foreach (var district in DistrictDatabase.Instance.GetAllDistricts())
			{
				// Contested starts at 50, faction-owned at 75 (favoring the controller)
				_controlMeters[district.Id] = district.Controller == "Contested"
					|| district.Controller == "Neutral" ? 50 : 75;
			}
			Log.Print("DistrictManager: initialized.");
		}

		/// <summary>Select a district for the next match.</summary>
		public bool SelectDistrict(string districtId)
		{
			var district = DistrictDatabase.Instance.GetDistrict(districtId);
			if (district == null)
			{
				Log.PrintErr($"DistrictManager: district '{districtId}' not found.");
				return false;
			}
			if (district.IsLocked)
			{
				Log.Print($"DistrictManager: '{district.Name}' is locked.");
				return false;
			}

			ActiveDistrictId = districtId;
			Log.Print($"DistrictManager: selected '{district.Name}'.");
			return true;
		}

		/// <summary>
		/// Builds a MatchConfig from the active district's rules.
		/// Called by GameBoard before creating GameManager.
		/// </summary>
		public MatchConfig BuildMatchConfig()
		{
			var district = ActiveDistrict;
			if (district == null) return new MatchConfig();

			var protocols = new List<IProtocol>();
			foreach (var name in district.Protocols)
				protocols.Add(BuildProtocol(name));

			return new MatchConfig
			{
				Protocols    = protocols,
				Intercept    = district.Intercept,
				Conscription = district.Conscription,
				Standoff     = district.Standoff,
				Overflow      = district.Overflow
			};
		}

		/// <summary>
		/// Apply the Spreading Rule after a match win.
		/// heroFaction: the faction of the hero the winning player fielded.
		/// won: true if the player won, false if AI won.
		/// </summary>
		public void ApplySpreading(string districtId, string heroFaction, bool playerWon)
		{
			if (!_controlMeters.ContainsKey(districtId)) return;

			// Base shift scaled by the player's cred tier (systems.md §6.4, §8.4).
			// A Legend crew's wins move the meter twice as fast as a Nameless crew's.
			float mult  = GameSession.Instance?.Cred != null
				? CredEffects.ControlShiftMultiplier(GameSession.Instance.Cred.Tier)
				: 1.0f;
			int baseShift = playerWon ? -10 : +10;
			int shift     = (int)System.Math.Round(baseShift * mult);

			_controlMeters[districtId] = System.Math.Clamp(
				_controlMeters[districtId] + shift, 0, 100);

			int meter = _controlMeters[districtId];
			Log.Print($"Spreading Rule: {districtId} control meter now {meter}.");

			// When meter crosses a threshold, update the controller
			var district = DistrictDatabase.Instance.GetDistrict(districtId);
			if (district == null) return;

			if (meter <= 25)
				Log.Print($"{district.Name} shifting toward player control.");
			else if (meter >= 75)
				Log.Print($"{district.Name} shifting toward AI control.");
		}

		public int GetControlMeter(string districtId) =>
			_controlMeters.TryGetValue(districtId, out var m) ? m : 50;

		/// <summary>Returns a snapshot of all control meters (for save).</summary>
		public IEnumerable<(string id, int value)> GetAllMeters()
		{
			foreach (var kvp in _controlMeters)
				yield return (kvp.Key, kvp.Value);
		}

		/// <summary>Directly sets a control meter value (for load).</summary>
		public void SetMeter(string districtId, int value)
		{
			if (_controlMeters.ContainsKey(districtId))
				_controlMeters[districtId] = System.Math.Clamp(value, 0, 100);
		}

		// ── Protocol factory ──────────────────────────────────────────────────────
		private static IProtocol BuildProtocol(string name) => name switch
		{
			"Handshake"     => new HandshakeProtocol(tolerance: 2),
			"Tally"         => new TallyProtocol(sumTolerance: 2),
			"WallSignature" => new WallSignatureProtocol(wallValue: 20, sumTolerance: 2),
			_               => throw new System.ArgumentException($"Unknown protocol: {name}")
		};
	}
}