using System.Collections.Generic;
using RimMind.Presentation.Api;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimMind.Personality.Data
{
    /// <summary>
    /// WorldComponent：持有所有 Pawn 的 PersonalityProfile，随存档序列化。
    /// </summary>
    /// <remarks>
    /// <para>Threading: <see cref="GetOrCreate"/>, <see cref="Remove"/>, cleanup and <see cref="ExposeData"/>
    /// are main-thread operations. <see cref="TryGet(int, out PersonalityProfile?)"/> is read-only and safe from
    /// any thread; context providers must use it (never <see cref="GetOrCreate"/>) and read profile fields
    /// inside <c>RimMindAPI.MainThreadSnapshot.GetAsync</c>. The dictionary itself is guarded by a lock so
    /// lookups never observe a resize in progress.</para>
    /// </remarks>
    public class AIPersonalityWorldComponent : WorldComponent
    {
        private readonly object _sync = new object();
        private Dictionary<int, PersonalityProfile> _profiles = new Dictionary<int, PersonalityProfile>();

        private static AIPersonalityWorldComponent? _instance;
        public static AIPersonalityWorldComponent? Instance => _instance;

        public AIPersonalityWorldComponent(World world) : base(world)
        {
            _instance = this;
        }

        private int _lastCleanupTick;

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            int ticksGame = Find.TickManager.TicksGame;
            if (_lastCleanupTick == 0)
            {
                _lastCleanupTick = ticksGame;
                return;
            }
            if (ticksGame - _lastCleanupTick < RimMind.Domain.Common.RimMindTime.TicksPerDay) return;
            _lastCleanupTick = ticksGame;

            var retainedIds = CollectRetainedPawnIds();
            lock (_sync)
            {
                PruneProfiles(_profiles, retainedIds);
            }
        }

        /// <summary>
        /// Main thread only. Pawns whose profile survives the daily cleanup: living colonists on maps,
        /// in caravans and travelling transporters; colony prisoners/slaves on maps; unresurrected colony
        /// corpses on maps; and pawns held by any faction's kidnapped tracker (so a rescued colonist keeps
        /// their personality). World pawns are never enumerated.
        /// </summary>
        private static HashSet<int> CollectRetainedPawnIds()
        {
            var retained = new HashSet<int>();

            var allAliveColonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
            if (allAliveColonists != null)
            {
                foreach (var pawn in allAliveColonists)
                {
                    if (pawn != null && pawn.thingIDNumber > 0)
                        retained.Add(pawn.thingIDNumber);
                }
            }

            if (Find.Maps != null)
            {
                foreach (var map in Find.Maps)
                {
                    var allMapPawns = map?.mapPawns?.AllPawns;
                    if (allMapPawns != null)
                    {
                        foreach (var p in allMapPawns)
                        {
                            if (p != null && p.thingIDNumber > 0 && !p.Destroyed
                                && (RimMindPawnLookup.IsEligibleColonist(p) || p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony))
                                retained.Add(p.thingIDNumber);
                        }
                    }

                    var corpses = map?.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
                    if (corpses != null)
                    {
                        foreach (var thing in corpses)
                        {
                            if (thing is Corpse corpse && corpse.InnerPawn is Pawn inner && inner.thingIDNumber > 0 && !inner.Destroyed && (inner.IsColonist || inner.IsPrisonerOfColony || inner.IsSlaveOfColony))
                                retained.Add(inner.thingIDNumber);
                        }
                    }
                }
            }

            var factions = Find.FactionManager?.AllFactionsListForReading;
            if (factions != null)
            {
                for (int i = 0; i < factions.Count; i++)
                {
                    var kidnapped = factions[i]?.kidnapped?.KidnappedPawnsListForReading;
                    if (kidnapped == null) continue;
                    for (int j = 0; j < kidnapped.Count; j++)
                    {
                        var p = kidnapped[j];
                        if (p != null && p.thingIDNumber > 0 && !p.Dead && !p.Destroyed)
                            retained.Add(p.thingIDNumber);
                    }
                }
            }

            return retained;
        }

        public static int PruneProfiles(Dictionary<int, PersonalityProfile> profiles, ISet<int> retainedIds)
        {
            if (profiles == null || profiles.Count == 0) return 0;
            var toRemove = new List<int>();
            foreach (var id in profiles.Keys)
            {
                if (retainedIds == null || !retainedIds.Contains(id))
                    toRemove.Add(id);
            }
            for (int i = 0; i < toRemove.Count; i++)
                profiles.Remove(toRemove[i]);
            return toRemove.Count;
        }

        /// <summary>Main thread only. Returns the pawn's profile, creating an empty one if missing.</summary>
        public PersonalityProfile GetOrCreate(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            lock (_sync)
            {
                if (!_profiles.TryGetValue(id, out var profile))
                {
                    profile = new PersonalityProfile();
                    _profiles[id] = profile;
                }
                return profile;
            }
        }

        /// <summary>
        /// Read-only lookup safe from any thread. Never creates a profile; returns false when the pawn
        /// has not been evaluated yet.
        /// </summary>
        public bool TryGet(int pawnId, out PersonalityProfile? profile)
        {
            lock (_sync)
            {
                if (_profiles.TryGetValue(pawnId, out var found))
                {
                    profile = found;
                    return true;
                }
            }
            profile = null;
            return false;
        }

        /// <summary>Read-only lookup; see <see cref="TryGet(int, out PersonalityProfile?)"/>.</summary>
        public PersonalityProfile? TryGet(Pawn? pawn)
        {
            if (pawn == null) return null;
            return TryGet(pawn.thingIDNumber, out var profile) ? profile : null;
        }

        public void Remove(Pawn pawn)
        {
            lock (_sync)
                _profiles.Remove(pawn.thingIDNumber);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            lock (_sync)
                PersonalityProfilePersistence.Look(ref _profiles);
        }
    }
}
