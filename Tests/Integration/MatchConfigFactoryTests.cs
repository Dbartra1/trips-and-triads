using Xunit;
using System.Linq;
using TripsAndTriads.Rules;

namespace TripsAndTriads.Tests.Integration
{
    /// <summary>
    /// Verifies that each MatchConfig factory method produces the exact
    /// protocol list and flag combination documented in systems.md §6.2.
    ///
    /// District → Protocol/flag mapping (systems.md §6.2):
    ///   BaseRules       — no protocols, no flags
    ///   GlassSpire      — Intercept + WallSignature + Handshake
    ///   Killfloor       — Conscription + Standoff
    ///   DeadChannel     — Intercept + Overflow
    ///   SprawlMarket    — Conscription
    ///   PowderRoom      — Tally + Handshake
    ///   TheHush         — Overflow + WallSignature + Handshake
    ///   TheVault        — ALL: Intercept + Conscription + Standoff + Overflow
    ///                          + Handshake + Tally + WallSignature
    /// </summary>
    public class MatchConfigFactoryTests
    {
        // ── Helper ────────────────────────────────────────────────────────────

        private static bool HasProtocol<T>(MatchConfig config) where T : IProtocol =>
            config.Protocols.Any(p => p is T);

        // ── Base Rules ────────────────────────────────────────────────────────

        [Fact]
        public void BaseRules_NoProtocols_NoFlags()
        {
            var config = MatchConfig.BaseRules();

            Assert.Empty(config.Protocols);
            Assert.False(config.Intercept);
            Assert.False(config.Conscription);
            Assert.False(config.Standoff);
            Assert.False(config.Overflow);
        }

        // ── Glass Spire (Ascendant) ───────────────────────────────────────────

        [Fact]
        public void GlassSpire_HasInterceptAndWallSignatureAndHandshake()
        {
            var config = MatchConfig.GlassSpire();

            Assert.True(config.Intercept);
            Assert.True(HasProtocol<WallSignatureProtocol>(config));
            Assert.True(HasProtocol<HandshakeProtocol>(config));
        }

        [Fact]
        public void GlassSpire_NoConscription_NoStandoff_NoOverflow()
        {
            var config = MatchConfig.GlassSpire();

            Assert.False(config.Conscription);
            Assert.False(config.Standoff);
            Assert.False(config.Overflow);
        }

        // ── Killfloor (Razorkin) ──────────────────────────────────────────────

        [Fact]
        public void Killfloor_HasConscriptionAndStandoff()
        {
            var config = MatchConfig.Killfloor();

            Assert.True(config.Conscription);
            Assert.True(config.Standoff);
        }

        [Fact]
        public void Killfloor_NoProtocolObjects_NoInterceptNoOverflow()
        {
            var config = MatchConfig.Killfloor();

            Assert.Empty(config.Protocols);
            Assert.False(config.Intercept);
            Assert.False(config.Overflow);
        }

        // ── Dead Channel (Ghostwire) ──────────────────────────────────────────

        [Fact]
        public void DeadChannel_HasInterceptAndOverflow()
        {
            var config = MatchConfig.DeadChannel();

            Assert.True(config.Intercept);
            Assert.True(config.Overflow);
        }

        [Fact]
        public void DeadChannel_NoProtocolObjects_NoConscriptionNoStandoff()
        {
            var config = MatchConfig.DeadChannel();

            Assert.Empty(config.Protocols);
            Assert.False(config.Conscription);
            Assert.False(config.Standoff);
        }

        // ── Sprawl Market (Commons) ───────────────────────────────────────────

        [Fact]
        public void SprawlMarket_HasConscriptionOnly()
        {
            var config = MatchConfig.SprawlMarket();

            Assert.True(config.Conscription);
            Assert.Empty(config.Protocols);
            Assert.False(config.Intercept);
            Assert.False(config.Standoff);
            Assert.False(config.Overflow);
        }

        // ── Powder Room (Lacquer) ─────────────────────────────────────────────

        [Fact]
        public void PowderRoom_HasTallyAndHandshake()
        {
            var config = MatchConfig.PowderRoom();

            Assert.True(HasProtocol<TallyProtocol>(config));
            Assert.True(HasProtocol<HandshakeProtocol>(config));
        }

        [Fact]
        public void PowderRoom_NoFlags()
        {
            var config = MatchConfig.PowderRoom();

            Assert.False(config.Intercept);
            Assert.False(config.Conscription);
            Assert.False(config.Standoff);
            Assert.False(config.Overflow);
        }

        // ── The Hush (Hollow Choir) ───────────────────────────────────────────

        [Fact]
        public void TheHush_HasOverflowAndWallSignatureAndHandshake()
        {
            var config = MatchConfig.TheHush();

            Assert.True(config.Overflow);
            Assert.True(HasProtocol<WallSignatureProtocol>(config));
            Assert.True(HasProtocol<HandshakeProtocol>(config));
        }

        [Fact]
        public void TheHush_NoInterceptNoConscriptionNoStandoff()
        {
            var config = MatchConfig.TheHush();

            Assert.False(config.Intercept);
            Assert.False(config.Conscription);
            Assert.False(config.Standoff);
        }

        // ── The Vault (Contested — endgame) ──────────────────────────────────

        [Fact]
        public void TheVault_HasAllProtocols()
        {
            var config = MatchConfig.TheVault();

            Assert.True(HasProtocol<HandshakeProtocol>(config));
            Assert.True(HasProtocol<TallyProtocol>(config));
            Assert.True(HasProtocol<WallSignatureProtocol>(config));
        }

        [Fact]
        public void TheVault_HasAllFlags()
        {
            var config = MatchConfig.TheVault();

            Assert.True(config.Intercept);
            Assert.True(config.Conscription);
            Assert.True(config.Standoff);
            Assert.True(config.Overflow);
        }

        // ── Protocol uniqueness ───────────────────────────────────────────────

        [Fact]
        public void NoFactory_HasDuplicateProtocols()
        {
            // Each protocol type should appear at most once in any config.
            var configs = new[]
            {
                MatchConfig.BaseRules(),   MatchConfig.GlassSpire(),
                MatchConfig.Killfloor(),   MatchConfig.DeadChannel(),
                MatchConfig.SprawlMarket(),MatchConfig.PowderRoom(),
                MatchConfig.TheHush(),     MatchConfig.TheVault(),
            };

            foreach (var config in configs)
            {
                var types = config.Protocols.Select(p => p.GetType()).ToList();
                var distinct = types.Distinct().ToList();
                Assert.Equal(distinct.Count, types.Count);
            }
        }

        // ── Protocol Name property sanity ─────────────────────────────────────

        [Fact]
        public void AllProtocols_HaveNonEmptyName()
        {
            var protocols = new IProtocol[]
            {
                new HandshakeProtocol(),
                new TallyProtocol(),
                new WallSignatureProtocol(),
            };

            foreach (var p in protocols)
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Name),
                    $"{p.GetType().Name}.Name is null or empty");
            }
        }
    }
}
