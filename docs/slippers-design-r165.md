# Slippers design notes (r165-oriented)

Internal reference: layering expectations vs vanilla 1.6, Mind Matters baseline integration, dynamic comps, and rescue / bed selection (for fainting couch planning). Not a player-facing changelog.

---

## 1. Apparel layers (vanilla 1.6 Core)

Core defines six layers with fixed draw order (`RimWorldGameData/1.6/Core/Defs/Misc/ApparelLayerDefs/ApparelLayerDefs.xml`):

| Layer      | drawOrder |
|-----------|-----------|
| OnSkin    | 0         |
| Middle    | 100       |
| Shell     | 200       |
| Belt      | 300       |
| Overhead  | 400       |
| EyeCover  | 500       |

No separate “scarf” layer exists; wraps use Middle or Shell plus `bodyPartGroups`.

### Imperial-style stacking (Royalty)

- Formal shirt (`Apparel_ShirtRuffle`): **OnSkin**
- Royal corset / formal vest (`Apparel_Corset`, `Apparel_VestRoyal`): **Middle**
- Prestige robe (`Apparel_RobeRoyal`): **Shell** (broad coverage)

Slippers corsets on **Middle** align with vanilla royal corset / vest.

### Powered armor and Odyssey vacuum stats

Recon / cataphract–style torso armor uses **both Middle and Shell** on one `ThingDef`. Vacuum tolerance is added via **`VacuumResistance`** (`MayRequire="Ludeon.RimWorld.Odyssey"`) on armor and helmets—not via extra apparel layers.

Conflict note: a **Middle** garment on torso/neck conflicts with that armor on the same regions. Presentation pieces either use narrower **bodyPartGroups**, different layer choices, or accept incompatibility with full powered suits.

### Cape / outer decorative (Core, Royalty-or-Biotech gated)

Vanilla **cape** is **Shell** with torso, neck, shoulders, arms, legs—same outer bucket as generic robes and the Shell portion of powered armor, so overlapping slots compete.

**Future scarf/shawl:** Middle (under outer robe/armor where groups allow) vs Shell (cape-like); narrow groups reduce clashes.

---

## 2. Slippers → Mind Matters baseline (current patterns)

- **`CompFreedomSuppressor`** reads `DynAppCompModExtension.needSatisfiers`, applies `satisfactionBaseContrib` / tick pulses, and maintains **per-apparel baseline slices** for Constraint-related needs (`FreedomSuppressorBaselineSync`, `PushConstraintBaselineSlices` / `ClearConstraintBaselineSlices`) so slices stay coherent after load.
- **`MindMattersFormalityBaselineBridge`** runs on each `ApparelTrackerTickInterval` postfix: aggregates **positive `SocialImpact`** from worn apparel defs (`×2`, clamp 01) into a Formality baseline slice (`BaselineProviders.SlippersApparelFormality`).
- **`CompDynamicApparelComps`**: if `DynAppCompModExtension.needSatisfiers` is non-empty, **`CompFreedomSuppressor`** is attached automatically (not only via the hardcoded `defName` switch), so XML-driven satisfiers work without forgetting the comp.

---

## 3. Rescue destination vs medical beds (RimWorld 1.6 source)

Question: can a **non-medical** building be a rescue target when medical beds exist? Is rescue independent from “suitable for medicine”?

### Where the bed is chosen

- Player **Rescue** float menu: `FloatMenuOptionProvider_RescuePawn` calls `RestUtility.FindBedFor(clickedPawn, rescuer, …)` and creates `JobDefOf.Rescue` with that bed.
- Colonist **automatic rescue**: `WorkGiver_RescueDowned` → `WorkGiver_TakeToBed.FindBed` → same `RestUtility.FindBedFor`.

So **one central API** decides the destination.

### Medical vs non-medical branches inside `RestUtility.FindBedFor`

1. If **`HealthAIUtility.ShouldSeekMedicalRest(sleeper)`** is true, the method searches **only beds with `Building_Bed.Medical == true`**, in medical preference order (plus slab-bed variants for ideos), then returns the closest valid candidate.

2. If that condition is **false**, it prefers owned bed / partner bed, then searches **only beds with `Medical == false`** (`!((Building_Bed)b).Medical` in the predicate)—it does **not** mix medical and non-medical in one pass.

So **rescue destination is not independent of medical rest logic**: the game first decides whether the pawn should seek **medical** rest; that gates **all medical-only vs non-medical-only** bed search.

### When `ShouldSeekMedicalRest` is true (important for corset faint)

For a typical **downed adult colonist** (`Downed == true`, not `AlwaysDowned`), **`ShouldSeekMedicalRestUrgent`** short-circuits to **`true`** because `(!pawn.Downed || …)` is false in its first branch. That makes **`ShouldSeekMedicalRest` almost always `true`** for downed humans—**independent** of bleeding or tend needs.

So **vanilla rescue of a downed pawn** → **`FindBedFor` medical branch only** → hospital beds, not a social fainting couch.

**Exhaustion-style collapse** is different: involuntary **`LayDown`** from `Need_Rest` often leaves **`Downed == false`**, so **`WantsToBeRescued`** is false and colonists are not auto-hauled—unless danger/medical pipelines apply elsewhere.

**Slippers “faint”** (historical presentation + aftercare, not injury) should be modeled with that split in mind: either stay **not Downed** (LayDown + custom care job) or accept **Downed** and override bed selection (see §5).

### Practical implication for a “fainting couch”

- **Non-medical couch**: Works as a rescue target **only when** the downed pawn is **not** in the “seek medical rest” state—consistent with “nuisance faint,” not with serious injury.
- **Medical-flagged couch**: Would compete in the **medical** bed list (sorted by tend quality / rest effectiveness among `ThingDef`s with `IsBed`). It would **pull doctors and tend behavior** toward it unless other systems exclude it—usually **not** what players want for a purely social/faint recovery spot.

### Modding levers (not implemented here)

1. **Hediff design**: Ensure benign faint hediffs do **not** force conditions that keep `ShouldSeekMedicalRest` true when you want non-medical recovery (avoid needing tend, healing-track injuries, etc., unless intended).

2. **Harmony**: Patch **`RestUtility.FindBedFor`** or **`HealthAIUtility.ShouldSeekMedicalRest`** for a narrow predicate (e.g. Slippers faint hediff only, no bleed, no tend-needed hediffs) so a **`FaintingCouch`** `ThingDef` wins before generic medical search—**only when** the patient is still **`Downed`**.

3. **Custom job (preferred for benign faint)**: Do **not** inject **`JobDefOf.Rescue`** for LayDown-only states—`JobDriver_TakeToBed` fails Rescue when `!Takee.Downed`. Use **`JobDefOf.DeliverToBed`** (same driver, no Downed check) or a dedicated **`JobDef`** with target B = non-medical couch bed, resolved explicitly (not `FindBedFor` medical path).

4. **Vacuum (Odyssey)**: `RestUtility.CanUseBedNow` rejects beds in dangerous vacuum for `sleeper.HarmedByVacuum`; space maps still need appropriate survival gear regardless of couch.

### Bed def ordering

`RestUtility.Reset()` builds global lists of **all** `ThingDef` where `IsBed`, ordered by body size, rest effectiveness, and (for medical) tend quality. Any new couch **`ThingDef`** must be a proper bed in vanilla terms to appear in those scans.

---

## 4. Mind Matters r167 — cross-link (faint, care, trust)

Slippers fainting is **simulation**, not vanilla “faint” flavor text: presentation, bond-mediated aftercare, and optional **Ideology** normalization (training corset precept) sit on the same social stack Mind Matters is building in **r167**.

**Related MM doc:** [`../../MindMatters/docs/r167.md`](../../MindMatters/docs/r167.md) (layer model, social field Phases A–D, Organ Shell, Witness).

| Slippers concern | MM today / r167 direction |
|------------------|-------------------------|
| Who notices faint | Pawn **awareness**: `DraggingCache` (Ring I salience), Witness dyad ledger + fan-out; not a full map scan every tick. |
| Who helps | **Organ Shell** `RescueDownedColonist` → vanilla **`Rescue` + `FindBedFor`** (medical beds, **`Downed` required**). **Not** yet Slippers faint-couch aware. |
| Aftercare / bonds | **Social field** (`RecognitionHunger`, dyad AB), **undertone catch** (`SocialPrimingProcessor`), **field priming** jobs (approach/group)—architecture for “care as social ritual,” separate from hospital. |
| Trust / relationship churn | r167 **AssociationBattery**, Witness accretion, pariah/trust wiring (checklist)—expected to **increase dyad turnover** in play, not only UI labels. |

**Design intent (coordination):** Benign apparel faint should **avoid** marking the couch **medical** (no hospital room side effects). Prefer **LayDown + DeliverToBed/couch** or a **scoped Harmony** on bed find when only Slippers faint hediffs are active. MM Organ Shell rescue should **not** treat corset faint like combat downed without an explicit tag/channel.

**r167 completion vs advisory:** Early MM psyche was **read-only bias** (`BehaviorBias`, quasi-attractors). r167 already ships **PreMain job issuers** (field priming, catch wait, Organ Shell hold/rescue). Finishing the r167 plan should shift further from “explain pressure” to **active path alteration**—more visible colony **relationship churn** as trust/field/arbitration hooks land, with settings to gate churn for players who want a lighter touch.

**Productivity note (MM, not Slippers):** Positive “things going well → work a little better” is **not** wired like vanilla inspiration yet; grounding mostly affects **chroma peace recovery** and social adapters. Slippers should not depend on that for faint recovery; optional future MM **Integration / high grounding** bonuses are a separate lever.

---

## 5. References (source paths)

| Topic | Location |
|-------|-----------|
| Find bed / medical gate | `RimWorldSourceCode/1.6/RimWorld/RestUtility.cs` — `FindBedFor`, `FindPatientBedFor` |
| Medical rest flag | `RimWorldSourceCode/1.6/RimWorld/HealthAIUtility.cs` — `ShouldSeekMedicalRest`, `ShouldSeekMedicalRestUrgent` |
| Player rescue UI | `RimWorldSourceCode/1.6/RimWorld/FloatMenuOptionProvider_RescuePawn.cs` |
| Workgiver rescue | `RimWorldSourceCode/1.6/RimWorld/WorkGiver_RescueDowned.cs`, `WorkGiver_TakeToBed.cs` |
| Rescue fails if not Downed | `RimWorldSourceCode/1.6/RimWorld/JobDriver_TakeToBed.cs` (Rescue/Capture) |
| DeliverToBed (no Downed gate) | `RimWorldGameData/1.6/Core/Defs/JobDefs/Jobs_Misc.xml` — `JobDriver_TakeToBed` |
| MM Organ Shell rescue | `MindMatters/Source/OrganShell/` — `RescueDownedOrganShellContributor`, `OrganShellJobDispatch` |

---

*Document for Slippers / Mind Matters coordination; revise when defs, faint hediffs, or r167 care channels change.*
