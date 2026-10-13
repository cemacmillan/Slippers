using UnityEngine;
using Verse;

namespace SlippersNmSpc
{
    public class SlippersModSettingsWindow : Window
    {
        private Vector2 scrollPosition;
        private SlippersModSettings _modSettings;

        public SlippersModSettingsWindow(SlippersModSettings settings)
        {
            doCloseX = true;
            forcePause = false;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;

            Settings = settings;
        }

        public SlippersModSettings Settings
        {
            get => _modSettings;
            set => _modSettings = value;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect contentRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 30f);

            Widgets.BeginScrollView(contentRect, ref scrollPosition, contentRect);

            Listing_Standard listing = new Listing_Standard();
            listing.Begin(contentRect);
            listing.Label(
                "Constraint pinch / faint pressure (apparel only, not pain-stun): " + Settings.FaintingBanality.ToString("0.00"),
                tooltip: "0 = off. Higher = more random pinch events from constraining pieces. Better craft quality reduces chance sharply from Good upward; the slider scales how much quality matters.");
            Settings.FaintingBanality = listing.Slider(Settings.FaintingBanality, 0f, 1f);
            // Checkbox for enabling logging
            listing.CheckboxLabeled("Enable Logging", ref Settings.EnableLogging, "EnableLogging");

            listing.End();
            Widgets.EndScrollView();

            // Save button
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 25f, inRect.width, 25f), "Save"))
            {
                Settings.Write();
                SlippersMod.Settings.FaintingBanality = Settings.FaintingBanality;
                SlippersMod.Settings.EnableLogging = Settings.EnableLogging; // Synchronize settings
                Close();
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();

            windowRect = new Rect(0f, 0f, 400f, 300f);
            windowRect.center = new Vector2(UI.screenWidth / 2f, UI.screenHeight / 2f);
        }

        public override void Close(bool doCloseSound = true)
        {
            base.Close(doCloseSound);
            Settings.Write();
        }
    }
}