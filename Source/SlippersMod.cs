using HarmonyLib;
using UnityEngine;
using Verse;

namespace SlippersNmSpc
{
    public class SlippersMod : Mod
    {
        public static SlippersModSettings Settings;

        public SlippersMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<SlippersModSettings>();
            var harmony = new Harmony("cem.slippers");
            harmony.PatchAll();
            Log.Message("<color=#00FF7F>[Slippers]</color>v1.5.2 chez");
        }

        public override string SettingsCategory() => "Slippers Mod";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings == null)
            {
                Log.Error("Slippers Mod settings is not initialized.");
                return;
            }

            // Open the settings window
            SlippersModSettingsWindow settingsWindow = new SlippersModSettingsWindow(Settings);
            settingsWindow.DoWindowContents(inRect);
        }

    }
}