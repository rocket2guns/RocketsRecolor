using RimWorld;
using Verse;

namespace RecolorClothing
{
    [DefOf]
    public static class RecolorDefOf
    {
        public static RecipeDef RecolorClothing;

        static RecolorDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(RecolorDefOf));
        }
    }
}