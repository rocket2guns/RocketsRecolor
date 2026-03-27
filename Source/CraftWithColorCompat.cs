using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace RecolorClothing
{
    /// <summary>
    /// Soft dependency on Craft With Color.
    /// If present, we read their saved colors so the user sees
    /// the same palette in our recolor dialog.
    /// </summary>
    public static class CraftWithColorCompat
    {
        private static bool checkedAvailability;
        private static bool isAvailable;
        private static PropertyInfo savedColorsProp;

        public static bool IsAvailable
        {
            get
            {
                if (!checkedAvailability)
                {
                    checkedAvailability = true;
                    isAvailable = DetectMod();
                }
                return isAvailable;
            }
        }

        private static bool DetectMod()
        {
            try
            {
                if (!ModLister.AllInstalledMods.Any(
                    m => m.PackageIdNonUnique == "kathanon.craftwithcolor" && m.Active))
                    return false;

                Type stateType = AccessTools.TypeByName("CraftWithColor.State");
                if (stateType == null) return false;

                savedColorsProp = AccessTools.Property(stateType, "SavedColors");
                if (savedColorsProp == null) return false;

                Log.Message("[RecolorClothing] Craft With Color detected. Shared saved colors enabled.");
                return true;
            }
            catch (Exception e)
            {
                Log.Warning($"[RecolorClothing] Failed to detect Craft With Color: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get CraftWithColor's saved colors. Returns null if CWC is not loaded.
        /// This is their live list — reads and writes are shared.
        /// </summary>
        public static List<Color> GetSavedColors()
        {
            if (!IsAvailable) return null;
            try
            {
                return savedColorsProp.GetValue(null) as List<Color>;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the standard ColorDef colors, matching CWC's filtering.
        /// </summary>
        public static List<Color> GetStandardColors()
        {
            // Match CraftWithColor's SelectColorDialog.DefaultColors logic
            var colors = (
                from x in DefDatabase<ColorDef>.AllDefsListForReading
                where x.colorType == ColorType.Ideo || x.colorType == ColorType.Misc
                select x.color
            ).ToList();
            colors.SortByColor(c => c);
            return colors;
        }
    }
}