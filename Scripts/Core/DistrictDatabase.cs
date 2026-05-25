using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Singleton that loads and serves DistrictData from districts.json.
	/// Mirrors CardDatabase in pattern.
	/// </summary>
	public class DistrictDatabase
	{
		private static DistrictDatabase _instance;
		public  static DistrictDatabase Instance => _instance ??= new DistrictDatabase();

		private Dictionary<string, DistrictData> _districts = new();
		private List<DistrictData>               _ordered   = new(); // insertion order for UI

		public void Load()
		{
			var file = FileAccess.Open("res://Data/Districts/districts.json", FileAccess.ModeFlags.Read);
			if (file == null)
			{
				GD.PrintErr("DistrictDatabase: could not open districts.json");
				return;
			}

			var json = file.GetAsText();
			file.Close();

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true,
				Converters = { new JsonStringEnumConverter() }
			};

			var list = JsonSerializer.Deserialize<List<DistrictData>>(json, options);
			if (list == null)
			{
				GD.PrintErr("DistrictDatabase: deserialization returned null.");
				return;
			}

			foreach (var district in list)
			{
				_districts[district.Id] = district;
				_ordered.Add(district);
			}

			GD.Print($"DistrictDatabase: loaded {_districts.Count} districts.");
		}

		public DistrictData   GetDistrict(string id)    => _districts.TryGetValue(id, out var d) ? d : null;
		public List<DistrictData> GetAllDistricts()     => new List<DistrictData>(_ordered);
		public List<DistrictData> GetUnlockedDistricts() =>
			_ordered.FindAll(d => !d.IsLocked);
	}
}
