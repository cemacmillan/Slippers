using System;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using MindMattersInterface;

namespace SlippersNmSpc;

public class CompDynamicApparelComps : ThingComp
{
    private bool compsAdded = false;
    private bool isEquipped;

    public bool IsEquipped => isEquipped;
    protected Pawn PawnOwner => ((Apparel)parent).Wearer;

    public void Initialize()
    {
        base.Initialize(props);
        
        if (props == null)
        {
            MMToolkit.DebugLog($"[CompDynamicApparelComps] Initialized with null props. Expect trouble.");
        }
        //MMToolkit.DebugLog($"[CompDynamicApparelComps] Initializing.");
    }
    
/*
 * XXX - this is plain wrong because we are almost certain that base.PostSpawnSetup isn't going to help us
 * configure things in a derived class it knows zip about.
 */
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);

        if (respawningAfterLoad)
        {
           // MMToolkit.DebugLog($"[CompDynamicApparelComps] PostSpawnSetup (reload) for {parent.LabelCap}.");
        }
        else
        {
            // MMToolkit.DebugLog($"[CompDynamicApparelComps] PostSpawnSetup (new object) for {parent.LabelCap}.");
            RecreateComps(); // Only recreate comps for new objects
        }
    }


    public override void PostExposeData()
    {
        base.PostExposeData();

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            //MMToolkit.DebugLog($"[CompDynamicApparelComps] PostExposeData (reload) for {parent.LabelCap}. Recreating comps.");
            RecreateComps();
        }
    }

    public override void Notify_Equipped(Pawn pawn)
    {
        if (pawn == null)
        { 
            MMToolkit.GripeOnce($"[CompDynamicApparelComps] Null pawn in Notify_Equipped. Cannot proceed");
        }
        base.Notify_Equipped(pawn);
        isEquipped = true;
        // rarely useful
        // MMToolkit.DebugLog($"[CompDynamicApparelComps] {parent.LabelCap} equipped by {pawn.LabelShort}.");
        UpdateEquippedState(pawn);
    }

    public override void Notify_Unequipped(Pawn pawn)
    {
        base.Notify_Unequipped(pawn);
        isEquipped = false;
        // rarely useful
        // MMToolkit.DebugLog($"[CompDynamicApparelComps] {parent.LabelCap} unequipped by {pawn.LabelShort}.");
        UpdateEquippedState(null);
    }
    
    private void UpdateEquippedState(Pawn pawn)
    {
        foreach (var comp in parent.AllComps.OfType<IDynamicComp>())
        {
            if (pawn != null)
            {
                comp.NotifyEquipped(pawn);
            }
            else
            {
                comp.NotifyUnequipped(pawn);
            }
        }
    }
    
    // force build comment
    private void RecreateComps()
    {
        // MMToolkit.DebugLog($"[CompDynamicApparelComps] Recreating comps for {parent.LabelCap}.");

        FieldInfo compsField = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic);
        if (compsField == null)
        {
            Log.Error("[CompDynamicApparelComps] Failed to access `comps` field.");
            return;
        }

        List<ThingComp> comps = (List<ThingComp>)compsField.GetValue(parent);
        if (comps == null)
        {
            Log.Error("[CompDynamicApparelComps] `comps` field is null.");
            return;
        }

        // Remove all dynamic comps
        int removedCount = comps.RemoveAll(c => c is IDynamicComp);
        // we know this is called, so logging is utterly useless so far
        //MMToolkit.DebugLog($"[CompDynamicApparelComps] Removed {removedCount} dynamic comps from {parent.LabelCap}.");

        // Reset the compsAdded flag to allow reattachment
        compsAdded = false;

        // Add comps based on modExtensions or explicit definitions
        AttemptAttachComps();
    }
    
    
    private void AttemptAttachComps()
    {
        if (compsAdded)
        {
            // Double-check if comps are actually present
            if (parent.AllComps.OfType<IDynamicComp>().Any())
            {
                MMToolkit.GripeOnce($"[CompDynamicApparelComps] AttemptAttachComps called but already initialized for {parent.LabelCap}.");
                return;
            }
        
            // If the flag was set incorrectly, reset it
            compsAdded = false;
        }

        // MMToolkit.DebugLog($"[CompDynamicApparelComps] Attempting to attach comps for {parent.LabelCap}.");
        
        string defName = parent.def.defName;
        DynAppCompModExtension dynNeedExt = parent.def.GetModExtension<DynAppCompModExtension>();
        bool hasNeedSatisfierExt = dynNeedExt?.needSatisfiers != null && dynNeedExt.needSatisfiers.Count > 0;

        switch (defName)
        {
            case "SLPPR_Footwear_Slippers":
                AddComp(typeof(CompFilthReducer));
                AddComp(typeof(CompComfortEnhancer));
                AddComp(typeof(CompClamorReducer));
                AddComp(typeof(CompTerrainDetector));
                break;
            case "SLPPR_Footwear_CruelShoes":
                // AddComp(typeof(CompQuality));
                AddComp(typeof(CompPainCauser));
                AddComp(typeof(CompFreedomSuppressor));
                AddComp(typeof(CompQualityEffects));
                break;
            case "SLPPR_Footwear_Moccasins":
                AddComp(typeof(CompComfortEnhancer));
                AddComp(typeof(CompTerrainDetector));
                break;
            case "SLPPR_Footwear_Mukluks":
                AddComp(typeof(CompComfortEnhancer));
                AddComp(typeof(CompTerrainDetector));
                AddComp(typeof(CompClamorReducer));
                break;
            case "SLPPR_Apparel_TrainingCorset":
                AddComp(typeof(CompPainCauser));
                AddComp(typeof(CompFreedomSuppressor));
                break;
            case "SLPPR_Apparel_SafetyCorset":
                AddComp(typeof(CompQualityEffects));
                break;
            //
            // [CompDynamicApparelComps] parent.def.defName SLPPR_TrainingCorset isn't recognized.
            default:
                if (!hasNeedSatisfierExt)
                {
                    MMToolkit.GripeOnce($"[CompDynamicApparelComps] parent.def.defName {parent.def.defName} isn't recognized.");
                }
                break;
        }

        // Need satisfiers (baseline / per-interval satisfaction) are applied by CompFreedomSuppressor reading
        // DynAppCompModExtension from the ThingDef. Without this comp, modExtensions > needSatisfiers in XML have no effect.
        if (hasNeedSatisfierExt)
        {
            AddComp(typeof(CompFreedomSuppressor));
        }

        compsAdded = true;
        // rarely useful
        // SlippersUtility.DebugLog($"[CompDynamicApparelComps] Successfully attached comps for {parent.LabelCap}.");
    }

    private void AddComp(Type compType)
    {
        // Safeguard: Ensure `comps` reflects the latest state
        FieldInfo compsField = typeof(ThingWithComps).GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic);
        if (compsField == null)
        {
            Log.Error("[CompDynamicApparelComps] Could not access `comps` field to add comp.");
            return;
        }

        List<ThingComp> comps = (List<ThingComp>)compsField.GetValue(parent);
        if (comps == null)
        {
            MMToolkit.GripeOnce("[CompDynamicApparelComps] `comps` field is null.");
            return;
        }

        // Revised: Directly check `comps` for an existing instance of this type
        if (comps.Any(c => c.GetType() == compType))
        {
            // spammy on hot reload
            // MMToolkit.DebugLog($"[CompDynamicApparelComps] Skipping addition: {compType.Name} already exists for {parent.LabelCap}.");
            return;
        }

        // Create and initialize the new comp
        ThingComp comp = (ThingComp)Activator.CreateInstance(compType);
        comp.parent = parent;

        CompProperties internalProps = DynamicCompPropertiesRegistry.GetPropertiesFor(compType);
        if (internalProps != null)
        {
            comp.Initialize(internalProps);
            // MMToolkit.DebugLog($"[CompDynamicApparelComps] Initialized {compType.Name} with dynamic properties for {parent.LabelCap}.");
        }
        else
        {
            MMToolkit.GripeOnce($"[CompDynamicApparelComps] Missing properties for {compType.Name} on {parent.LabelCap}. Skipping.");
            return;
        }

        // Add the comp to the internal list
        comps.Add(comp);
        // MMToolkit.DebugLog($"[CompDynamicApparelComps] Added comp {compType.Name} to {parent.LabelCap}.");
    }
}