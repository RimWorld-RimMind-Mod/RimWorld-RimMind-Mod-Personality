using System;
using RimMind.Presentation.Api;
using Verse;

namespace RimMind.Personality
{
    /// <summary>
    /// Thin backwards-compatible forwarding stub; new callers should use <see cref="RimMindPawnLookup.FindPawnByNumber"/> directly.
    /// </summary>
    [Obsolete("Use RimMind.Presentation.Api.RimMindPawnLookup.FindPawnByNumber directly.")]
    public static class PawnResolver
    {
        public static Pawn? TryFindPawn(int pawnId)
            => RimMindPawnLookup.FindPawnByNumber(pawnId);
    }
}
