using HarmonyLib;
using RimMind.Application.Common.Interfaces.Extension;
using RimMind.Presentation.Api;
using RimMind.Presentation.Settings;
using UnityEngine;
using Verse;

namespace RimMind.Personality
{
    public class RimMindPersonalityMod : Mod
    {
        public static AIPersonalitySettings Settings = null!;

        public RimMindPersonalityMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<AIPersonalitySettings>();
            new Harmony("mcocdaa.RimMindPersonality").PatchAll();

            PersonalityProviderRegistrar.RegisterAll();
            RimMindAPI.Extensions<ISettingsTab>().Register(new PersonalitySettingsTab());
            RimMindAPI.Extensions<IToggleBehavior>().Register(new PersonalityToggleBehavior());
            RimMindAPI.Extensions<IModCooldown>().Register(new PersonalityModCooldown());
            RimMindAPI.Extensions<ISkipCheck>().Register(new PersonalityActionSkipCheck());
            Log.Message("[RimMind-Personality] Initialized.");
        }

        public override string SettingsCategory() => "RimMind - Personality";

        public override void DoSettingsWindowContents(Rect rect) =>
            PersonalitySettingsDrawer.Draw(rect);
    }
}
