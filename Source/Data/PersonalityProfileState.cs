using System;
using System.Collections.Generic;
using RimMind.Application.Common.Models.Agent;
using Verse;

namespace RimMind.Personality.Data
{
    /// <summary>
    /// Persistent, game-independent state of one Pawn's personality profile.
    /// </summary>
    public class PersonalityProfile : IExposable
    {
        public string description = string.Empty;
        public string workTendencies = string.Empty;
        public string socialTendencies = string.Empty;
        public string aiNarrative = string.Empty;
        public int lastNarrativeUpdateTick;
        public List<ShapingRecord> playerShapingHistory = new List<ShapingRecord>();
        public AgentIdentity? agentIdentity;

        public void AddShapingRecord(ShapingRecord record, int maxCount)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            playerShapingHistory ??= new List<ShapingRecord>();
            playerShapingHistory.Add(record);
            int effectiveMax = Math.Max(maxCount, 1);
            if (playerShapingHistory.Count > effectiveMax)
                playerShapingHistory.RemoveRange(0, playerShapingHistory.Count - effectiveMax);
        }

        public bool IsEmpty =>
            description.NullOrEmpty() &&
            workTendencies.NullOrEmpty() &&
            socialTendencies.NullOrEmpty() &&
            aiNarrative.NullOrEmpty();

        public void ExposeData()
        {
#pragma warning disable CS8601
            Scribe_Values.Look(ref description, "description", string.Empty);
            Scribe_Values.Look(ref workTendencies, "workTendencies", string.Empty);
            Scribe_Values.Look(ref socialTendencies, "socialTendencies", string.Empty);
            Scribe_Values.Look(ref aiNarrative, "aiNarrative", string.Empty);
#pragma warning restore CS8601
            Scribe_Values.Look(ref lastNarrativeUpdateTick, "lastNarrativeUpdateTick");

            bool legacyRimTalkSynced = false;
            Scribe_Values.Look(ref legacyRimTalkSynced, "rimTalkSynced");

            Scribe_Collections.Look(ref playerShapingHistory, "playerShapingHistory", LookMode.Deep);
            playerShapingHistory ??= new List<ShapingRecord>();
            Scribe_Deep.Look(ref agentIdentity, "agentIdentity");
            agentIdentity ??= new AgentIdentity();
        }
    }

    public static class PersonalityProfilePersistence
    {
        public static void Look(ref Dictionary<int, PersonalityProfile> profiles)
        {
            Scribe_Collections.Look(ref profiles, "profiles", LookMode.Value, LookMode.Deep);
            profiles ??= new Dictionary<int, PersonalityProfile>();
        }
    }
}
