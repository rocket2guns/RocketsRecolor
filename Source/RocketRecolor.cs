using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;
using Verse.AI.Group;
using Verse.Grammar;

namespace RecolorClothing
{
    public class RecolorClothingMod : Mod
    {
        public RecolorClothingMod(ModContentPack content) : base(content)
        {
            Log.Message("[RecolorClothing] Mod loaded.");
        }
    }
}