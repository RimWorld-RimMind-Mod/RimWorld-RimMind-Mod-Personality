using System.Collections.Generic;
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
            int now = Find.TickManager.TicksGame;
            if (now - _lastCleanupTick < 60000) return;
            _lastCleanupTick = now;

            var aliveIds = new HashSet<int>();
            foreach (var map in Find.Maps)
            {
                foreach (var pawn in map.mapPawns.AllPawns)
                {
                    if (pawn.thingIDNumber > 0)
                        aliveIds.Add(pawn.thingIDNumber);
                }
            }
            foreach (var wp in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                if (wp.thingIDNumber > 0)
                    aliveIds.Add(wp.thingIDNumber);
            }

            var toRemove = new List<int>();
            foreach (var id in _profiles.Keys)
            {
                if (!aliveIds.Contains(id))
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
