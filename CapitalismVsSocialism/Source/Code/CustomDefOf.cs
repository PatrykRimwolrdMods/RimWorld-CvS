using RimWorld;
using Verse;

namespace CapitalismVsSocjalism
{
    [DefOf]
    public static class CustomDefOf
    {
        public static ThingDef ChunkGranite;

        static CustomDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(CustomDefOf));
        }
    }
}