namespace TripsAndTriads.Core
{
	/// <summary>
	/// The five rungs of the Street Cred ladder (systems.md §8.1).
	/// Cred is a 0–100 integer; tier is derived from it.
	/// </summary>
	public enum CredTier
	{
		Nameless  = 0,   //  0–19
		Known     = 1,   // 20–39
		Named     = 2,   // 40–59
		Notorious = 3,   // 60–79
		Legend    = 4,   // 80–100
	}
}
