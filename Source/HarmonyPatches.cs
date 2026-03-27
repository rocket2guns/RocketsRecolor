using System.Collections.Generic;
using System.Reflection;
using FloatSubMenus;
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
    /// Hide Dye from the ingredient filter UI for our recolor bills.
    /// </summary>
    [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
    public static class Patch_ThingFilterUI_HideDye
    {
        internal static bool IsRecolorBillFilter = false;

        public static void Prefix(ref IEnumerable<ThingDef> forceHiddenDefs)
        {
            if (!IsRecolorBillFilter)
                return;

            if (forceHiddenDefs == null)
            {
                forceHiddenDefs = new List<ThingDef> { ThingDefOf.Dye };
            }
            else
            {
                var list = new List<ThingDef>(forceHiddenDefs) { ThingDefOf.Dye };
                forceHiddenDefs = list;
            }
        }
    }

    /// <summary>
    /// Draw the color controls in the bill config dialog.
    /// </summary>
    [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
    public static class Patch_BillConfig_UI
    {
        private static readonly FieldInfo billField =
            AccessTools.Field(typeof(Dialog_BillConfig), "bill");

        private const float IconSize = 28f;
        private const float CheckSize = 24f;
        private const float BottomOffset = 62f;

        public static void Prefix(Dialog_BillConfig __instance)
        {
            Bill bill = billField.GetValue(__instance) as Bill;
            Patch_ThingFilterUI_HideDye.IsRecolorBillFilter = bill is RecolorBill;
        }

        public static void Postfix(Dialog_BillConfig __instance, Rect inRect)
        {
            Patch_ThingFilterUI_HideDye.IsRecolorBillFilter = false;

            Bill bill = billField.GetValue(__instance) as Bill;
            if (!(bill is RecolorBill recolorBill))
                return;

            DrawColorControls(inRect, recolorBill);
        }

        private static void DrawColorControls(Rect inRect, RecolorBill bill)
        {
            var columnWidth = Mathf.Floor((inRect.width - 34f) / 3f);
            var y = inRect.yMax - BottomOffset - IconSize;
            var x = inRect.x;

            Text.Font = GameFont.Small;
            Color oldColor = GUI.color;

            Rect labelRect = new Rect(x, y + (IconSize - CheckSize) / 2f,
                columnWidth - IconSize - 10f, CheckSize);
            Widgets.Label(labelRect, "Recolor");

            Rect swatchRect = new Rect(x + columnWidth - IconSize, y, IconSize, IconSize);
            Widgets.DrawBoxSolid(swatchRect, bill.chosenColor);

            if (bill.isRandomColor)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                Widgets.Label(new Rect(swatchRect.x + 4f, swatchRect.y + 2f,
                    swatchRect.width, swatchRect.height), "?");
                GUI.color = oldColor;
                TooltipHandler.TipRegion(swatchRect, "Random color");
            }

            GUI.color = new Color(0.4f, 0.4f, 0.4f);
            Widgets.DrawBox(swatchRect);
            GUI.color = oldColor;

            if (Widgets.ButtonInvisible(swatchRect))
            {
                OpenColorMenu(bill);
            }

            Text.Font = GameFont.Tiny;
            string hex = "#" + ColorUtility.ToHtmlStringRGB(bill.chosenColor);
            Rect hexRect = new Rect(x, y + IconSize + 2f, columnWidth, 16f);
            GUI.color = new Color(0.6f, 0.6f, 0.6f);
            Widgets.Label(hexRect, hex);
            GUI.color = oldColor;
            Text.Font = GameFont.Small;
        }

        private static void OpenColorMenu(RecolorBill bill)
        {
            var options = new List<FloatMenuOption>();

            options.Add(new FloatMenuOption("Select...", () =>
            {
                bill.isRandomColor = false;
                Find.WindowStack.Add(new Dialog_ChooseRecolorColor(bill));
            }));

            options.Add(new FloatMenuOption("Random", () =>
            {
                bill.isRandomColor = true;
                bill.chosenColor = Random.ColorHSV(0f, 1f, 0.3f, 1f, 0.3f, 1f);
            }));

            var favorites = BuildFavoriteOptions(bill);
            if (favorites.Count > 0)
            {
                options.Add(new FloatSubMenu("Favorite", favorites));
            }

            if (!Find.IdeoManager.classicMode)
            {
                var ideoOptions = BuildIdeoOptions(bill);
                if (ideoOptions.Count > 0)
                {
                    options.Add(new FloatSubMenu("Ideoligion", ideoOptions));
                }
            }

            List<Color> savedColors = CraftWithColorCompat.GetSavedColors();
            if (savedColors != null && savedColors.Count > 0)
            {
                var savedOptions = BuildSavedColorOptions(bill, savedColors);
                options.Add(new FloatSubMenu("Saved Colors", savedOptions));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static List<FloatMenuOption> BuildFavoriteOptions(RecolorBill bill)
        {
            var list = new List<FloatMenuOption>();
            if (Find.CurrentMap == null) return list;

            foreach (Pawn pawn in Find.CurrentMap.mapPawns.FreeColonists)
            {
                Color? favColor = pawn.story?.favoriteColor?.color;
                if (favColor.HasValue)
                {
                    Color color = favColor.Value;
                    list.Add(new FloatMenuOption(pawn.LabelShort, () =>
                    {
                        bill.isRandomColor = false;
                        bill.chosenColor = color;
                    }, BaseContent.WhiteTex, color));
                }
            }
            return list;
        }

        private static List<FloatMenuOption> BuildIdeoOptions(RecolorBill bill)
        {
            var list = new List<FloatMenuOption>();
            foreach (Ideo ideo in Find.IdeoManager.IdeosInViewOrder)
            {
                Color color = ideo.ApparelColor;
                list.Add(new FloatMenuOption(ideo.name, () =>
                {
                    bill.isRandomColor = false;
                    bill.chosenColor = color;
                }, BaseContent.WhiteTex, color));
            }
            return list;
        }

        private static List<FloatMenuOption> BuildSavedColorOptions(
            RecolorBill bill, List<Color> savedColors)
        {
            var list = new List<FloatMenuOption>();
            foreach (Color color in savedColors)
            {
                Color c = color;
                list.Add(new FloatMenuOption(" ", () =>
                {
                    bill.isRandomColor = false;
                    bill.chosenColor = c;
                }, BaseContent.WhiteTex, c));
            }
            return list;
        }
    }
}