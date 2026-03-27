using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorClothing
{
    public class RecolorBill : Bill_Production
    {
        public Color chosenColor = Color.white;
        public bool isRandomColor = false;

        public RecolorBill() { }

        public RecolorBill(RecipeDef recipe) : base(recipe, null)
        {
        }

        /// <summary>
        /// Get the color for this iteration. If random, roll a new one each time.
        /// </summary>
        public Color GetColorForIteration()
        {
            if (isRandomColor)
            {
                chosenColor = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            }
            return chosenColor;
        }

        public override bool IsFixedOrAllowedIngredient(Thing thing)
        {
            if (!base.IsFixedOrAllowedIngredient(thing))
                return false;

            // Don't filter apparel when random — any color is fine to recolor
            if (thing is Apparel && !isRandomColor)
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
            Scribe_Values.Look(ref isRandomColor, "isRandomColor", false);
        }

        public override Bill Clone()
        {
            var clone = (RecolorBill)base.Clone();
            clone.chosenColor = chosenColor;
            clone.isRandomColor = isRandomColor;
            return clone;
        }
    }
}