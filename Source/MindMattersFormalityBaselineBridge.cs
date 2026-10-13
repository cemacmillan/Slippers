using MindMattersInterface;
using RimWorld;
using UnityEngine;
using Verse;

namespace SlippersNmSpc;

/// <summary>
/// Pushes aggregate apparel-derived Formality baseline into Mind Matters each Slippers apparel tick.
/// Policy matches MM fallback: sum positive <see cref="StatDefOf.SocialImpact"/> on worn apparel, ×2, clamp 01 — tune here only.
/// </summary>
public static class MindMattersFormalityBaselineBridge
{
    public static void TryPushForPawn(Pawn pawn)
    {
        if ((!ModsConfig.IsActive("cem.mindmatters") && !ModsConfig.IsActive("cem.mindmatterspr")) ||
            pawn == null || pawn.Dead)
        {
            return;
        }

        if (!MindMattersAPI.HasDynamicNeed(pawn, "FormalityNeed"))
        {
            return;
        }

        float v = ComputeFormalBaseline01FromWornApparel(pawn);
        DynamicNeeds.SetBaselineContributionSlice(pawn, "FormalityNeed", BaselineProviders.SlippersApparelFormality, v);
    }

    public static float ComputeFormalBaseline01FromWornApparel(Pawn pawn)
    {
        if (pawn?.apparel?.WornApparel == null || pawn.apparel.WornApparel.Count == 0)
        {
            return 0f;
        }

        float totalSocialImpact = 0f;

        foreach (Apparel apparel in pawn.apparel.WornApparel)
        {
            if (apparel?.def?.equippedStatOffsets == null)
            {
                continue;
            }

            foreach (StatModifier statMod in apparel.def.equippedStatOffsets)
            {
                if (statMod.stat == StatDefOf.SocialImpact)
                {
                    totalSocialImpact += Mathf.Max(0f, statMod.value);
                }
            }
        }

        return Mathf.Clamp01(totalSocialImpact * 2f);
    }
}
