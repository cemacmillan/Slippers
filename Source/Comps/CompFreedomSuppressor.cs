using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using MindMattersInterface;
using System.Linq;

namespace SlippersNmSpc;

// I don't like being told about typos in identifiers when they aren't typos
[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("ReSharper", "IdentifierTypo")]
public class CompFreedomSuppressor : ThingComp, IDynamicComp
{
    public CompProperties_FreedomSuppressor Props => (CompProperties_FreedomSuppressor)props;

    private Pawn wearer;
    private int pinchInterval;
    private float constraintFactor;
    
    private DynAppCompModExtension cachedModExtension;
    private List<DynAppCompModExtension.NeedSatisfier> cachedNeedSatisfiers;
    private int updateCounter = 0; // To align with NeedInterval
    private const int UpdateFrequency = 150; // Adjust as needed
   
    public int PinchInterval
    {
        get => pinchInterval;
        set => pinchInterval = value;
    }

    public float ConstraintFactor
    {
        get => constraintFactor;
        set => constraintFactor = value;
    }
    
    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        
        cachedModExtension = parent.def.GetModExtension<DynAppCompModExtension>();
        cachedNeedSatisfiers = cachedModExtension?.needSatisfiers?.ToList();

        if (props is CompProperties_FreedomSuppressor constraintProps)
        {
            PinchInterval = constraintProps.pinchInterval;
            ConstraintFactor = constraintProps.constraintFactor;
        }
        else
        {
            MMToolkit.GripeOnce($"[CompFreedomSuppressor] Missing or invalid properties for {parent.LabelCap}.");
        }
    }

    public void NotifyEquipped(Pawn pawn)
    {
        wearer = pawn;
        cachedModExtension = parent.def.GetModExtension<DynAppCompModExtension>();
        cachedNeedSatisfiers = cachedModExtension?.needSatisfiers?.ToList();

        if (wearer != null && wearer.Spawned && cachedNeedSatisfiers != null)
        {
            PushConstraintBaselineSlices(wearer);
            foreach (DynAppCompModExtension.NeedSatisfier needSatisfier in cachedNeedSatisfiers)
            {
                if (string.Equals(needSatisfier.needDefName, "ConstraintNeed", StringComparison.Ordinal))
                    continue;
                DynamicNeeds.UpdateBaseline(wearer, needSatisfier.needDefName, needSatisfier.satisfactionBaseContrib,
                    isAdding: true);
            }
        }
    }

    public void NotifyUnequipped(Pawn pawn)
    {
        if (pawn == null || wearer != pawn)
        {
            wearer = null;
            return;
        }

        if (cachedNeedSatisfiers != null)
        {
            ClearConstraintBaselineSlices(pawn);
            foreach (DynAppCompModExtension.NeedSatisfier needSatisfier in cachedNeedSatisfiers)
            {
                if (string.Equals(needSatisfier.needDefName, "ConstraintNeed", StringComparison.Ordinal))
                    continue;
                DynamicNeeds.UpdateBaseline(pawn, needSatisfier.needDefName, needSatisfier.satisfactionBaseContrib,
                    isAdding: false);
            }
        }

        wearer = null;
        cachedModExtension = null;
        cachedNeedSatisfiers = null;
    }

    /// <summary>Per-apparel ConstraintNeed slices (idempotent — safe from apparel tick resync).</summary>
    public void PushConstraintBaselineSlices(Pawn targetPawn)
    {
        if (targetPawn == null || cachedNeedSatisfiers == null)
            return;

        foreach (DynAppCompModExtension.NeedSatisfier needSatisfier in cachedNeedSatisfiers)
        {
            if (!string.Equals(needSatisfier.needDefName, "ConstraintNeed", StringComparison.Ordinal))
                continue;

            string key = BaselineProviders.FreedomSuppressorConstraintKey(parent.thingIDNumber, needSatisfier.needDefName);
            DynamicNeeds.SetBaselineContributionSlice(targetPawn, needSatisfier.needDefName, key,
                Mathf.Clamp01(needSatisfier.satisfactionBaseContrib));
        }
    }

    public void ClearConstraintBaselineSlices(Pawn targetPawn)
    {
        if (targetPawn == null || cachedNeedSatisfiers == null)
            return;

        foreach (DynAppCompModExtension.NeedSatisfier needSatisfier in cachedNeedSatisfiers)
        {
            if (!string.Equals(needSatisfier.needDefName, "ConstraintNeed", StringComparison.Ordinal))
                continue;

            string key = BaselineProviders.FreedomSuppressorConstraintKey(parent.thingIDNumber, needSatisfier.needDefName);
            DynamicNeeds.ClearBaselineContributionSlice(targetPawn, needSatisfier.needDefName, key);
        }
    }

    public void onEquippedTick(Pawn pawn)
    {
        if (pawn == null || pawn.Dead || cachedNeedSatisfiers == null) return;

        updateCounter++;
        if (updateCounter < UpdateFrequency) return; // Skip until the update frequency matches
        updateCounter = 0;

        foreach (DynAppCompModExtension.NeedSatisfier needSatisfier in cachedNeedSatisfiers)
        {
            DynamicNeeds.SatisfyNeedOnTick(pawn, needSatisfier.needDefName, needSatisfier.satisfactionPerTick);
        }
    }

    public void onEquippedTickRare(Pawn pawn)
    {
        if (pawn == null || pawn.Dead) return;

        // Check and apply pinch effects
        TryApplyPinchEffect(pawn);
    }

    private void SatisfyConstraintNeed(Pawn pawn)
    {
        if (pawn == null || pawn.Dead) return;

        float qualityFactor = QualityFactor();
        float satisfactionIncrease = ConstraintFactor * qualityFactor;

        // Apply satisfaction for ConstraintNeed
        DynamicNeeds.SatisfyNeed(pawn, "ConstraintNeed", satisfactionIncrease);

        // Optionally apply satisfaction to FormalityNeed
        if (!pawn.Downed && !pawn.InMentalState)
        {
            DynamicNeeds.SatisfyNeed(pawn, "FormalityNeed", satisfactionIncrease * 0.5f);
        }
    }

    private void TryApplyPinchEffect(Pawn pawn)
    {
        if (pawn == null || pawn.health?.hediffSet == null) return;

        // Avoid pinch if pain level is already critical
        if (pawn.health.hediffSet.PainTotal > pawn.GetStatValue(StatDefOf.PainShockThreshold)) return;

        float banality = SlippersMod.Settings.FaintingBanality;
        if (banality <= 0f) return;

        CompQuality compQuality = parent.TryGetComp<CompQuality>();
        QualityCategory tier = compQuality?.Quality ?? QualityCategory.Normal;
        float steepDivisor = QualityPinchDivisor(tier);
        // Banality scales how strongly craft quality reduces pinches: at low banality the curve is flatter;
        // at high banality, excellent+ items get drastically fewer rolls than awful/poor (Pow steepens with slider).
        float effectiveDivisor = Mathf.Pow(steepDivisor, Mathf.Clamp01(banality));
        float probability = Mathf.Clamp01(banality / effectiveDivisor);

        if (Rand.Chance(probability))
        {
            ApplyPinchEffect(pawn, QualityFactor());
        }
    }

    /// <summary>
    /// Higher divisor → lower pinch probability. Awful/poor pinch often; excellent+ is heavily protected.
    /// </summary>
    private static float QualityPinchDivisor(QualityCategory q)
    {
        return q switch
        {
            QualityCategory.Awful => 0.48f,
            QualityCategory.Poor => 0.62f,
            QualityCategory.Normal => 1f,
            QualityCategory.Good => 1.5f,
            QualityCategory.Excellent => 3.4f,
            QualityCategory.Masterwork => 8f,
            QualityCategory.Legendary => 12f,
            _ => 1f
        };
    }

    private void ApplyPinchEffect(Pawn pawn, float qualityFactor)
    {
        if (pawn == null) return;

        Hediff pinchHediff;
        string eventType;
        HashSet<string> tags;

        if (Rand.Range(0f, 1f) < qualityFactor / 2)
        {
            pinchHediff = HediffMaker.MakeHediff(SlippersNmSpc.DefOfs.LittleFaint, pawn);
            eventType = "MinorFaint";
            tags = ["ConstrainingApparel", "MinorDiscomfort"];
        }
        else
        {
            pinchHediff = HediffMaker.MakeHediff(SlippersNmSpc.DefOfs.SevereFaint, pawn);
            eventType = "MajorFaint";
            tags = ["ConstrainingApparel", "SevereDiscomfort"];

            DynamicNeeds.SatisfyNeed(pawn, "ConstraintNeed", 0f, satisfyToMax: true);
        }

        if (pinchHediff != null)
        {
            pawn.health.AddHediff(pinchHediff);
            pinchHediff.Severity += ConstraintFactor * (1 / qualityFactor);

            DynamicNeeds.NotifyExperience(pawn, eventType, ExperienceValency.Negative, tags);
        }
    }

    private float QualityFactor()
    {
        CompQuality compQuality = parent.TryGetComp<CompQuality>();
        return compQuality?.Quality switch
        {
            QualityCategory.Awful => 0.5f,
            QualityCategory.Poor => 0.75f,
            QualityCategory.Normal => 1.0f,
            QualityCategory.Good => 1.25f,
            QualityCategory.Excellent => 1.5f,
            QualityCategory.Masterwork or QualityCategory.Legendary => 2.0f,
            _ => 1.0f
        };
    }
}