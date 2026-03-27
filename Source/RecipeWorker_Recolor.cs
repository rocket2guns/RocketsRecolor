using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RecolorClothing
{
    public class RecipeWorker_Recolor : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient.def != ThingDefOf.Dye) return;
            ingredient.Destroy();
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            var recolorBill = FindRecolorBill(billDoer);
            if (recolorBill is null)
            {
                Log.Error("[RecolorClothing] Could not find RecolorBill. Color not applied.");
                return;
            }
            var color = recolorBill.GetColorForIteration();
            foreach (var item in ingredients)
            {
                if (item == null || item.Destroyed)
                    continue;

                if (item.def == ThingDefOf.Dye)
                    continue;

                var comp = item.TryGetComp<CompColorable>();
                if (comp != null)
                {
                    comp.SetColor(color);
                }
                else
                {
                    Log.Warning($"[RecolorClothing] {item.def.defName} has no CompColorable.");
                }
            }
        }

        private static RecolorBill FindRecolorBill(Pawn billDoer)
        {
            var job = billDoer?.CurJob;
            if (job is null)
                return null;

            var jobBill = job.bill;
            if (jobBill is RecolorBill rb)
                return rb;

            // Check the bill stack on job.bill
            if (jobBill?.billStack != null)
            {
                foreach (var b in jobBill.billStack)
                {
                    if (b is RecolorBill candidate)
                        return candidate;
                }
            }

            var workbench = job.GetTarget(TargetIndex.A).Thing;
            if (workbench is IBillGiver { BillStack: not null } giver)
            {
                foreach (var b in giver.BillStack)
                {
                    if (b is RecolorBill candidate)
                        return candidate;
                }
            }
            return null;
        }
    }
}