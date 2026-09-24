using System.Linq;
using Verse;
using RimWorld;

namespace RimMind.Personality
{
    /// <summary>
    /// Shared helper for resolving a Pawn by thingIDNumber from the same data sources.
    /// Eliminates duplication across ContextProvider lambdas in RimMindPersonalityMod.
    /// Uses AllPawnsAlive (world) then FreeColonists (current map) as fallback.
    /// </summary>
    public static class PawnResolver
    {
        /// <summary>
        /// Tries to find a pawn by thingIDNumber.
        /// Delegates to the unified RimMindPawnLookup.
        /// </summary>
        public static Pawn? TryFindPawn(int pawnId)
        {
            return RimMind.Presentation.Api.RimMindPawnLookup.FindPawnByNumber(pawnId);
        }
    }
}
