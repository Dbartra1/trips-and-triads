using System.Collections.Generic;

namespace TripsAndTriads.Rules
{
	public class MatchConfig
	{
		public List<IProtocol> Protocols    { get; set; } = new();
		public bool            Intercept    { get; set; } = false;
		public bool            Conscription { get; set; } = false;
		public bool            Standoff     { get; set; } = false;
		public bool            Cascade      { get; set; } = false;

		public static MatchConfig BaseRules()   => new MatchConfig();

		public static MatchConfig GlassSpire()  => new MatchConfig
		{
			Intercept = true,
			Protocols = new List<IProtocol> { new WallSignatureProtocol(), new HandshakeProtocol() }
		};

		public static MatchConfig Killfloor()   => new MatchConfig
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

		public static MatchConfig PowderRoom()  => new MatchConfig
		{
			Protocols = new List<IProtocol> { new TallyProtocol(), new HandshakeProtocol() }
		};

		public static MatchConfig TheHush()     => new MatchConfig
		{
			Cascade   = true,
			Protocols = new List<IProtocol> { new WallSignatureProtocol(), new HandshakeProtocol() }
		};

		public static MatchConfig TheVault()    => new MatchConfig
		{
			Intercept    = true,
			Conscription = true,
			Standoff     = true,
			Cascade      = true,
			Protocols    = new List<IProtocol>
			{
				new HandshakeProtocol(),
				new TallyProtocol(),
				new WallSignatureProtocol()
			}
		};
	}
}
