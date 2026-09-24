using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation;
using RimMind.Presentation.Api;
using RimMind.Presentation.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Personality
{
    public class RimMindPersonalityMod : RimMindSubmodBase<AIPersonalitySettings>
    {
        public static new AIPersonalitySettings Settings = null!;

        public RimMindPersonalityMod(ModContentPack content) : base(content)
        {
            Settings = base.Settings;
            InitializeHarmony();

            PersonalityProviderRegistrar.RegisterAll();
            RimMindAPI.Extensions<ISettingsTab>().Register(new PersonalitySettingsTab());
            RimMindAPI.Extensions<IToggleBehavior>().Register(new PersonalityToggleBehavior());
            RimMindAPI.Extensions<IModCooldown>().Register(new PersonalityModCooldown());
            RimMindAPI.Extensions<ISkipCheck>().Register(new PersonalityActionSkipCheck());
            Log.Message("[RimMind-Personality] Initialized.");
        }

        public override void DoSettingsWindowContents(Rect rect) =>
            PersonalitySettingsDrawer.Draw(rect);
    }
}
