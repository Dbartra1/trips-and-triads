namespace TripsAndTriads.Core
{
    /// <summary>
    /// Tracks the recruitment state of a free agent.
    /// </summary>
    public class FreeAgent
    {
        public CardData Data { get; set; }
        public bool IsMet { get; set; }
        public bool IsAuditioned { get; set; }
        public bool AuditionPassed { get; set; }
        public bool IsSigned { get; set; }
    }
}