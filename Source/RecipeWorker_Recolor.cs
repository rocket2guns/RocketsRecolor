using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RecolorClothing
{
    /// <summary>
    /// Custom RecipeWorker for the recolor recipe.
    /// 
    /// Both apparel and dye are listed as ingredients. The vanilla job system
    /// handles hauling both to the bench and reserving them properly.
    /// 
    /// ConsumeIngredient: lets dye be destroyed normally, but prevents
    ///   apparel destruction so it survives to be recolored.
    /// 
    /// Notify_IterationCompleted: applies the chosen color to the apparel.
    ///   The apparel is already at the bench location from hauling, so it
    ///   gets picked up by normal hauling jobs afterward.
    /// </summary>
    public class RecipeWorker_Recolor : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            // Dye gets consumed normally
            if (ingredient.def == ThingDefOf.Dye)
            {
                ingredient.Destroy();
                return;
            }

            // Apparel survives — don't destroy it
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            Bill bill = billDoer?.CurJob?.bill;
            RecolorBill recolorBill = bill as RecolorBill;

            if (recolorBill == null)
            {
                Log.Error("[RecolorClothing] Could not find RecolorBill on pawn's current job.");
                return;
            }

            foreach (Thing item in ingredients)
            {
                if (item == null || item.Destroyed)
                    continue;

                // Skip dye (already destroyed in ConsumeIngredient)
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