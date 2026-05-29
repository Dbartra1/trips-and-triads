using Xunit;

namespace TripsAndTriads.Tests
{
    /// <summary>
    /// Forces test classes that read or write DomainResolver.BonusMultiplier
    /// to run sequentially rather than in parallel.
    ///
    /// Problem: Scale20PathATests.PathA_DomainRelevance_WithDoubledBonuses sets
    /// BonusMultiplier = 2 for ~10 000 game iterations. DomainStackingTests and
    /// HeroAbilityProgressionTests assume BonusMultiplier = 1 (Scale-10).
    /// xUnit runs classes in parallel by default, so the math tests see the
    /// contaminated value and fail.
    ///
    /// Solution: put all three classes in the same named collection. xUnit runs
    /// all tests in a collection sequentially on a single thread.
    /// </summary>
    [CollectionDefinition("DomainState")]
    public class DomainStateCollection { }
}
