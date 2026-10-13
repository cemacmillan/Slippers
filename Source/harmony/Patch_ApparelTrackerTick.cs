using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace SlippersNmSpc;

[HarmonyPatch(typeof(Pawn_ApparelTracker), "ApparelTrackerTickInterval")]
public static class Patch_ApparelTrackerTick
{
    public static void Postfix(Pawn_ApparelTracker __instance, int delta)
    {
        // Validate the pawn owning the apparel tracker
        var pawn = __instance.pawn;
        if (pawn == null || pawn.Dead)
        {
            Log.Warning("[Slippers] Pawn_ApparelTrackerTick called with a null or dead pawn.");
            return;
        }

        foreach (var apparel in __instance.WornApparel)
        {
            // Iterate through all comps of the apparel
            foreach (var comp in apparel.AllComps)
            {
                // Only process comps that implement IDynamicComp
                if (comp is IDynamicComp dynamicComp)
                {
                    try
                    {
                        // Invoke the tick for equipped items
                        dynamicComp.onEquippedTick(pawn);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[Slippers] Error during onEquippedTick for {apparel.LabelCap} ({dynamicComp.GetType().Name}): {ex}");
                    }
                }
            }
        }

        MindMattersFormalityBaselineBridge.TryPushForPawn(pawn);
        FreedomSuppressorBaselineSync.SyncConstraintSlicesForPawn(pawn);
    }
}