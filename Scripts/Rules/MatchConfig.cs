using System.Collections.Generic;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Defines the rules active for a single match.
	/// Created by DistrictManager.BuildMatchConfig() and passed to GameManager.
	///
	/// Protocol constructors use Scale-20 Path A confirmed parameters:
	///   HandshakeProtocol(tolerance: 2)
	///   TallyProtocol(sumTolerance: 2)
	///   WallSignatureProtocol(wallValue: 20, sumTolerance: 2)
	///
	/// These values are confirmed by Scale20PathATests in Tests/Simulation/.
	/// Defaults on each protocol class remain 0 so existing unit tests are unaffected.
	/// </summary>
	public class MatchConfig
	{
		public List<IProtocol> Protocols    { get; set; } = new();
		public bool            Intercept    { get; set; } = false;
		public bool            Conscription { get; set; } = false;
		public bool            Standoff     { get; set; } = false;
		public bool            Cascade      { get; set; } = false;

		// ── Factory helpers ────────────────────────────────────────────────────

		public static MatchConfig BaseRules() => new MatchConfig();

		public static MatchConfig GlassSpire() => new MatchConfig
		{
			Intercept = true,
			Protocols = new List<IProtocol>
			{
				new WallSignatureProtocol(wallValue: 20, sumTolerance: 2),
				new HandshakeProtocol(tolerance: 2),
			}
		};

		public static MatchConfig Killfloor() => new MatchConfig
		{
			Conscription = true,
			Standoff     = true
		};

		public static MatchConfig DeadChannel() => new MatchConfig
		{
			Intercept = true,
			Cascade   = true
		};

		public static MatchConfig SprawlMarket() => new MatchConfig
		{
			Conscription = true
		};

		public static MatchConfig PowderRoom() => new MatchConfig
		{
			Protocols = new List<IProtocol>
			{
				new TallyProtocol(sumTolerance: 2),
				new HandshakeProtocol(tolerance: 2),
			}
		};

		public static MatchConfig TheHush() => new MatchConfig
		{
			Cascade   = true,
			Protocols = new List<IProtocol>
			{
				new WallSignatureProtocol(wallValue: 20, sumTolerance: 2),
				new HandshakeProtocol(tolerance: 2),
			}
		};

		public static MatchConfig TheVault() => new MatchConfig
		{
			Intercept    = true,
			Conscription = true,
			Standoff     = true,
			Cascade      = true,
			Protocols    = new List<IProtocol>
			{
				new HandshakeProtocol(tolerance: 2),
				new TallyProtocol(sumTolerance: 2),
				new WallSignatureProtocol(wallValue: 20, sumTolerance: 2),
			}
		};

		/// <summary>Clone for reuse — Standoff triggers with board-state hands so GameManager
		/// resets and replays; we need a fresh config for the rematch.</summary>
		public MatchConfig Clone() => new MatchConfig
		{
			Protocols    = new List<IProtocol>(Protocols),
			Intercept    = Intercept,
			Conscription = Conscription,
			Standoff     = Standoff,
			Cascade      = Cascade,
		};
	}
}
