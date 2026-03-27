using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorClothing
{
    /// <summary>
    /// Extends Bill_Production to store the chosen recolor color.
    /// 
    /// Overrides IsFixedOrAllowedIngredient to reject apparel
    /// that is already the target color.
    /// </summary>
    public class RecolorBill : Bill_Production
    {
        public Color chosenColor = Color.white;

        public RecolorBill() { }

        public RecolorBill(RecipeDef recipe) : base(recipe, null)
        {
        }

        public override bool IsFixedOrAllowedIngredient(Thing thing)
        {
            if (!base.IsFixedOrAllowedIngredient(thing))
                return false;

            // Don't filter dye — only filter apparel
            if (thing is Apparel)
            {
                CompColorable comp = thing.TryGetComp<CompColorable>();
                if (comp != null && comp.Active && comp.Color.IndistinguishableFrom(chosenColor))
                {
                    return false;
                }
            }

            return true;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref chosenColor, "chosenColor", Color.white);
        }

        public override Bill Clone()
        {
            var clone = (RecolorBill)base.Clone();
            clone.chosenColor = chosenColor;
            return clone;
        }
    }
}