using Verse;
using UnityEngine;

namespace SlippersNmSpc;

public class SlippersModSettings : ModSettings
{
    public const float FaintingInternalMin = 0.0f;
    public const float FaintingInternalMax = 0.3f;
    public const float FaintingBaseline = 0.013f;

    public bool EnableLogging = false;
    /// <summary>0 = no constraint pinch/faint rolls. Higher = more collapse risk from constraining apparel; quality curve bites harder as this rises.</summary>
    public float FaintingBanality = FaintingBaseline; 

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref FaintingBanality, "FaintingBanality", FaintingBaseline);
        Scribe_Values.Look(ref EnableLogging, "EnableLogging", false);
    }

    public static float NormalizeFaintingBanality(float internalValue)
    {
        return Mathf.InverseLerp(FaintingInternalMin, FaintingInternalMax, internalValue) * 2f;
    }

    public static float DenormalizeFaintingBanality(float normalizedValue)
    {
        return Mathf.Lerp(FaintingInternalMin, FaintingInternalMax, normalizedValue / 2f);
    }
}