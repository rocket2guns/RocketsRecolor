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
    /// Draw color picker in the bill config dialog.
    /// Positioned at bottom-left, following CraftWithColor's pattern of
    /// offsetting from inRect.yMax.
    /// </summary>
    [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
    public static class Patch_BillConfig_UI
    {
        private static readonly FieldInfo billField =
            AccessTools.Field(typeof(Dialog_BillConfig), "bill");

        public static void Postfix(Dialog_BillConfig __instance, Rect inRect)
        {
            Bill bill = billField.GetValue(__instance) as Bill;
            if (!(bill is RecolorBill recolorBill))
                return;

            DrawColorPicker(inRect, recolorBill);
        }

        private const float SwatchSize = 28f;
        private const float SliderHeight = 22f;
        private const float Padding = 8f;
        private const float Gap = 4f;
        private const float LabelHeight = 24f;

        // Total height: label + swatch row + hex + 3 sliders
        private const float ContentHeight = LabelHeight + SwatchSize + Gap + 18f + Gap
                                          + (SliderHeight + Gap) * 3f;
        // Offset from bottom: close button height + some margin
        private const float BottomOffset = 62f;

        private static void DrawColorPicker(Rect inRect, RecolorBill bill)
        {
            // Position: left third of dialog, above close button
            // CraftWithColor uses Mathf.Floor((inRect.width - 34f) / 3f) for column width
            float columnWidth = Mathf.Floor((inRect.width - 34f) / 3f);
            float panelWidth = Mathf.Min(columnWidth, 210f);

            float panelTop = inRect.yMax - BottomOffset - ContentHeight;
            Rect panel = new Rect(inRect.x, panelTop, panelWidth, ContentHeight);

            // Background
            Widgets.DrawBoxSolid(panel, new Color(0.12f, 0.12f, 0.12f, 0.85f));
            Color oldColor = GUI.color;
            GUI.color = new Color(0.4f, 0.4f, 0.4f);
            Widgets.DrawBox(panel);
            GUI.color = oldColor;

            float x = panel.x + Padding;
            float y = panel.y + Padding;
            float innerWidth = panel.width - Padding * 2f;

            // Label + swatch on same row
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(x, y, innerWidth - SwatchSize - Gap, LabelHeight), "Target Color");

            // Color swatch — click to open full picker dialog
            Rect swatchRect = new Rect(x + innerWidth - SwatchSize, y, SwatchSize, SwatchSize);
            Widgets.DrawBoxSolid(swatchRect, bill.chosenColor);
            GUI.color = new Color(0.5f, 0.5f, 0.5f);
            Widgets.DrawBox(swatchRect);
            GUI.color = oldColor;
            if (Widgets.ButtonInvisible(swatchRect))
            {
                Find.WindowStack.Add(new Dialog_ChooseRecolorColor(bill));
            }
            y += Mathf.Max(LabelHeight, SwatchSize) + Gap;

            // Hex display
            Text.Font = GameFont.Tiny;
            string hexStr = "#" + ColorUtility.ToHtmlStringRGB(bill.chosenColor);
            Widgets.Label(new Rect(x, y, innerWidth, 18f), hexStr);
            y += 18f + Gap;

            // RGB sliders
            Color c = bill.chosenColor;

            c.r = Widgets.HorizontalSlider(
                new Rect(x, y, innerWidth, SliderHeight),
                c.r, 0f, 1f, false, $"R: {(int)(c.r * 255)}");
            y += SliderHeight + Gap;

            c.g = Widgets.HorizontalSlider(
                new Rect(x, y, innerWidth, SliderHeight),
                c.g, 0f, 1f, false, $"G: {(int)(c.g * 255)}");
            y += SliderHeight + Gap;

            c.b = Widgets.HorizontalSlider(
                new Rect(x, y, innerWidth, SliderHeight),
                c.b, 0f, 1f, false, $"B: {(int)(c.b * 255)}");

            Text.Font = GameFont.Small;
            bill.chosenColor = c;
        }
    }
}