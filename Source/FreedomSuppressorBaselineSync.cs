using MindMattersInterface;
using RimWorld;
using Verse;

namespace SlippersNmSpc;

/// <summary>
/// Keeps ConstraintNeed baseline slices in sync after load and during play (slices are not saved).
/// </summary>
public static class FreedomSuppressorBaselineSync
{
    public static void SyncConstraintSlicesForPawn(Pawn pawn)
    {
        if ((!ModsConfig.IsActive("cem.mindmatters") && !ModsConfig.IsActive("cem.mindmatterspr")) ||
            pawn == null || pawn.Dead)
        {
            return;
        }

        if (!MindMattersAPI.HasDynamicNeed(pawn, "ConstraintNeed"))
            return;

        if (pawn.apparel?.WornApparel == null)
            return;

        foreach (Apparel apparel in pawn.apparel.WornApparel)
        {
            apparel.TryGetComp<CompFreedomSuppressor>()?.PushConstraintBaselineSlices(pawn);
        }
    }
}
