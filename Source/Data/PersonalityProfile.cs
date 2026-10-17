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
    public class AIPersonalityWorldComponent : WorldComponent
    {
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

            var activeLivingColonistIds = new HashSet<int>();

            var allAliveColonists = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_Colonists;
            if (allAliveColonists != null)
            {
                foreach (var pawn in allAliveColonists)
                {
                    if (pawn != null && pawn.thingIDNumber > 0)
                        activeLivingColonistIds.Add(pawn.thingIDNumber);
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
                            if (p != null && p.thingIDNumber > 0 && !p.Destroyed && (p.IsColonist || p.IsPrisonerOfColony || p.IsSlaveOfColony))
                                activeLivingColonistIds.Add(p.thingIDNumber);
                        }
                    }

                    var corpses = map?.listerThings?.ThingsInGroup(ThingRequestGroup.Corpse);
                    if (corpses != null)
                    {
                        foreach (var thing in corpses)
                        {
                            if (thing is Corpse corpse && corpse.InnerPawn is Pawn inner && inner.thingIDNumber > 0 && !inner.Destroyed && (inner.IsColonist || inner.IsPrisonerOfColony || inner.IsSlaveOfColony))
                                activeLivingColonistIds.Add(inner.thingIDNumber);
                        }
                    }
                }
            }

            var worldPawnsAlive = Find.WorldPawns?.AllPawnsAlive;
            if (worldPawnsAlive != null)
            {
                foreach (var p in worldPawnsAlive)
                {
                    if (p != null && p.thingIDNumber > 0 && !p.Destroyed && p.IsColonist)
                        activeLivingColonistIds.Add(p.thingIDNumber);
                }
            }

            var toRemove = new List<int>();
            foreach (var id in _profiles.Keys)
            {
                if (!activeLivingColonistIds.Contains(id))
                    toRemove.Add(id);
            }
            foreach (var id in toRemove)
                _profiles.Remove(id);
        }

        public PersonalityProfile GetOrCreate(Pawn pawn)
        {
            int id = pawn.thingIDNumber;
            if (!_profiles.TryGetValue(id, out var profile))
            {
                profile = new PersonalityProfile();
                _profiles[id] = profile;
            }
            return profile;
        }

        public void Remove(Pawn pawn) => _profiles.Remove(pawn.thingIDNumber);

        public override void ExposeData()
        {
            base.ExposeData();
            PersonalityProfilePersistence.Look(ref _profiles);
        }
    }
}
