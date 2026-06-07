using System.Collections.Generic;

namespace TripsAndTriads.Core
{
	/// <summary>
	/// Data template for a district — loaded from districts.json.
	/// Defines the rules that apply when dueling in this district.
	/// </summary>
	public class DistrictData
	{
		public string       Id           { get; set; }  // e.g. "the_stub"
		public string       Name         { get; set; }  // display name
		public string       Controller   { get; set; }  // faction Id or "Neutral"/"Contested"
		public string       Stake        { get; set; }  // "OneJob"|"TheSpread"|"AsFlipped"|"Everything"
		public List<string> Protocols    { get; set; } = new(); // protocol names active here
		public bool         Intercept    { get; set; } = false;
		public bool         Conscription { get; set; } = false;
		public bool         Standoff     { get; set; } = false;
		public bool         Overflow     { get; set; } = false;
		public string       Hazard       { get; set; } = null; // optional hazard name
		public string       Description  { get; set; }  // flavour text
		public bool         IsLocked     { get; set; } = false; // unlocked by cred later
	}
}
