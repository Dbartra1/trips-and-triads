using System.Collections.Generic;

namespace TripsAndTriads.Rules
{
	/// <summary>
	/// Defines the rules active for a single match.
	/// Created by GameBoard (or later DistrictManager) and passed to GameManager.
	/// </summary>
	public class MatchConfig
	{
		/// <summary>Capture-rule variant protocols active for this match.</summary>
		public List<IProtocol> Protocols { get; } = new();

		/// <summary>
		/// Intercept — both hands are revealed at match start.
		/// When true, GameBoard shows P2's hand to P1.
		/// </summary>
		public bool Intercept { get; set; } = false;

		/// <summary>
		/// Conscription — hands are dealt randomly from the full roster,
		/// not chosen by the player.
		/// </summary>
		public bool Conscription { get; set; } = false;

		/// <summary>
		/// Standoff — draws trigger an immediate rematch with board-state hands.
		/// Handled in GameManager.EndGame.
		/// </summary>
		public bool Standoff { get; set; } = false;

		/// <summary>
		/// Cascade — any Protocol capture immediately chains under base capture rules.
		/// Injected into CaptureResolver when active.
		/// </summary>
		public bool Cascade { get; set; } = false;

		// ── Factory helpers — build common district configs ───────────────────────

		/// <summary>The Stub — no protocols, base rules only.</summary>
		public static MatchConfig BaseRules() => new MatchConfig();

		/// <summary>Glass Spire — Ascendant district: Intercept + Wall Signature.</summary>
		public static MatchConfig GlassSpire() => new MatchConfig
		{
			Intercept = true,
			Protocols = { new WallSignatureProtocol(), new HandshakeProtocol() }
		};

		/// <summary>The Killfloor — Razorkin district: Conscription + Standoff.</summary>
		public static MatchConfig Killfloor() => new MatchConfig
		{
			Conscription = true,
			Standoff     = true,
			Protocols    = { }
		};

		/// <summary>Dead Channel — Ghostwire district: Intercept + Cascade.</summary>
		public static MatchConfig DeadChannel() => new MatchConfig
		{
			Intercept = true,
			Cascade   = true,
			Protocols = { }
		};

		/// <summary>The Sprawl-Market — Commons district: Conscription only.</summary>
		public static MatchConfig SprawlMarket() => new MatchConfig
		{
			Conscription = true,
			Protocols    = { }
		};

		/// <summary>The Powder Room — Lacquer district: The Tally + Handshake.</summary>
		public static MatchConfig PowderRoom() => new MatchConfig
		{
			Protocols = { new TallyProtocol(), new HandshakeProtocol() }
		};

		/// <summary>The Hush — Hollow Choir district: Cascade + Wall Signature.</summary>
		public static MatchConfig TheHush() => new MatchConfig
		{
			Cascade   = true,
			Protocols = { new WallSignatureProtocol(), new HandshakeProtocol() }
		};

		/// <summary>The Vault — Contested endgame: all protocols.</summary>
		public static MatchConfig TheVault() => new MatchConfig
		{
			Intercept    = true,
			Conscription = true,
			Standoff     = true,
			Cascade      = true,
			Protocols    = { new HandshakeProtocol(), new TallyProtocol(), new WallSignatureProtocol() }
		};
	}
}
