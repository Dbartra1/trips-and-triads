using Godot;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripsAndTriads.Core
{
	public class DistrictDatabase
	{
		private static DistrictDatabase _instance;
		public  static DistrictDatabase Instance => _instance ??= new DistrictDatabase();

		private Dictionary<string, DistrictData> _districts = new();
		private List<DistrictData>               _ordered   = new();
		private bool _loaded = false;

		public void Load()
		{
			if (_loaded) return; // guard against double-load

			var file = FileAccess.Open("res://Data/Districts/districts.json", FileAccess.ModeFlags.Read);
			if (file == null)
			{
				Log.PrintErr("DistrictDatabase: could not open districts.json");
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
				Log.PrintErr("DistrictDatabase: deserialization returned null.");
				return;
			}

			foreach (var district in list)
			{
				_districts[district.Id] = district;
				_ordered.Add(district);
			}

			_loaded = true;
			Log.Print($"DistrictDatabase: loaded {_districts.Count} districts.");
		}

		public DistrictData       GetDistrict(string id)    => _districts.TryGetValue(id, out var d) ? d : null;
		public List<DistrictData> GetAllDistricts()         => new List<DistrictData>(_ordered);
		public List<DistrictData> GetUnlockedDistricts()    => _ordered.FindAll(d => !d.IsLocked);
	}
}