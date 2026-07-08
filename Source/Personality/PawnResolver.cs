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
        /// Searches world pawns first, then current map free colonists.
        /// </summary>
        public static Pawn? TryFindPawn(int pawnId)
        {
            if (pawnId <= 0) return null;
            return Find.WorldPawns.AllPawnsAlive.FirstOrDefault(p => p.thingIDNumber == pawnId)
                ?? Find.CurrentMap?.mapPawns?.FreeColonists.FirstOrDefault(p => p.thingIDNumber == pawnId);
        }
    }
}
