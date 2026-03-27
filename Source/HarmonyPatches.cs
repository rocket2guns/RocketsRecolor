using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorClothing
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("rocket.recolorclothing");
            harmony.PatchAll();
            Log.Message("[RecolorClothing] Harmony patches applied.");
        }
    }

    /// <summary>
    /// When a bill is added for our recolor recipe, substitute our custom RecolorBill.
    /// </summary>
    [HarmonyPatch(typeof(BillStack), nameof(BillStack.AddBill))]
    public static class Patch_BillStack_AddBill
    {
        public static void Prefix(ref Bill bill)
        {
            if (bill is Bill_Production && !(bill is RecolorBill)
                && bill.recipe == RecolorDefOf.RecolorClothing)
            {
                var recolorBill = new RecolorBill(bill.recipe);
                bill = recolorBill;
            }
        }
    }

    /// <summary>
    /// Capture the Bill_Production instance before it calls
    /// recipe.Worker.Notify_IterationCompleted, so our RecipeWorker
    /// can reliably access the bill even when other mods (like CraftWithColor)
    /// wrap the finish toil and alter the job state.
    /// </summary>
    [HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.Notify_IterationCompleted))]
    public static class Patch_BillProduction_NotifyIteration
    {
        public static void Prefix(Bill_Production __instance)
        {
            RecipeWorker_Recolor.CurrentBill = __instance;
        }

        public static void Postfix()
        {
            RecipeWorker_Recolor.CurrentBill = null;
        }
    }

    /// <summary>
    /// Draw compact color indicator in the bill config dialog.
    /// Just shows "Target Color" label + clickable swatch. 
    /// Clicking opens the full color picker dialog.
    /// </summary>
    [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
    public static class Patch_BillConfig_UI
    {
        private static readonly FieldInfo billField =
            AccessTools.Field(typeof(Dialog_BillConfig), "bill");

        private const float SwatchSize = 28f;
        private const float RowHeight = 32f;
        private const float BottomOffset = 62f;

        public static void Postfix(Dialog_BillConfig __instance, Rect inRect)
        {
            Bill bill = billField.GetValue(__instance) as Bill;
            if (!(bill is RecolorBill recolorBill))
                return;

            float columnWidth = Mathf.Floor((inRect.width - 34f) / 3f);
            float panelWidth = Mathf.Min(columnWidth, 220f);
            float y = inRect.yMax - BottomOffset - RowHeight;
            float x = inRect.x;

            // Label
            Text.Font = GameFont.Small;
            Rect labelRect = new Rect(x, y + 4f, panelWidth - SwatchSize - 8f, RowHeight);
            Widgets.Label(labelRect, "Target Color");

            // Clickable swatch
            Rect swatchRect = new Rect(x + panelWidth - SwatchSize, y + 2f, SwatchSize, SwatchSize);
            Widgets.DrawBoxSolid(swatchRect, recolorBill.chosenColor);
            Color oldGui = GUI.color;
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            Widgets.DrawBox(swatchRect);
            GUI.color = oldGui;

            if (Widgets.ButtonInvisible(swatchRect))
            {
                Find.WindowStack.Add(new Dialog_ChooseRecolorColor(recolorBill));
            }

            // Hex next to label for quick reference
            Text.Font = GameFont.Tiny;
            string hex = "#" + ColorUtility.ToHtmlStringRGB(recolorBill.chosenColor);
            Rect hexRect = new Rect(x, y + RowHeight - 2f, panelWidth - SwatchSize - 8f, 16f);
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(hexRect, hex);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}