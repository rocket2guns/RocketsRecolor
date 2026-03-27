using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RecolorClothing
{
    public class RecipeWorker_Recolor : RecipeWorker
    {
        internal static Bill_Production CurrentBill;

        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            if (ingredient.def == ThingDefOf.Dye)
            {
                ingredient.Destroy();
                return;
            }
            // Apparel survives
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            // Try multiple sources for the bill
            RecolorBill recolorBill = CurrentBill as RecolorBill;

            if (recolorBill == null)
            {
                recolorBill = billDoer?.CurJob?.bill as RecolorBill;
            }

            // Walk the bill stack to find our bill if the above failed
            if (recolorBill == null && billDoer?.CurJob?.bill != null)
            {
                Bill jobBill = billDoer.CurJob.bill;
                Log.Warning($"[RecolorClothing] Debug: CurrentBill type = {CurrentBill?.GetType()?.Name ?? "null"}, " +
                            $"CurJob.bill type = {jobBill?.GetType()?.Name ?? "null"}, " +
                            $"recipe = {jobBill?.recipe?.defName ?? "null"}");

                // CraftWithColor may have wrapped our bill or swapped the recipe.
                // Check if the job's bill is actually our RecolorBill under the hood,
                // or find it in the bill stack.
                if (jobBill?.billStack != null)
                {
                    foreach (Bill b in jobBill.billStack)
                    {
                        if (b is RecolorBill rb)
                        {
                            recolorBill = rb;
                            break;
                        }
                    }
                }
            }

            if (recolorBill == null)
            {
                Log.Error("[RecolorClothing] Could not find RecolorBill. Color not applied.");
                return;
            }

            foreach (Thing item in ingredients)
            {
                if (item == null || item.Destroyed)
                    continue;

                if (item.def == ThingDefOf.Dye)
                    continue;

                CompColorable comp = item.TryGetComp<CompColorable>();
                if (comp != null)
                {
                    comp.SetColor(recolorBill.chosenColor);
                }
                else
                {
                    Log.Warning($"[RecolorClothing] {item.def.defName} has no CompColorable. Cannot apply color.");
                }
            }
        }
    }
}