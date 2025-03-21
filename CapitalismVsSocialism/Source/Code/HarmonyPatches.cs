using HarmonyLib;
using Verse;

namespace CapitalismVsSocjalism
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.yourname.capitalismvssocjalism");
            harmony.PatchAll();
            Log.Message("CapitalismVsSocjalism: Harmony patches applied");
        }

        [HarmonyPatch(typeof(Thing), "Destroy")]
        public static class Thing_Destroy_Patch
        {
            public static void Postfix(Thing __instance)
            {
                Thing thing = __instance;
                // Użyj DefDatabase zamiast ThingDefOf
                if (thing.def == DefDatabase<ThingDef>.GetNamed("ChunkGranite"))
                {
                    // Użyj pełnej ścieżki do EntrepreneurManager
                    CapitalismVsSocjalism.Code.EntrepreneurManager entrepreneurManager = Current.Game.GetComponent<CapitalismVsSocjalism.Code.EntrepreneurManager>();
                    if (entrepreneurManager != null)
                    {
                        entrepreneurManager.OnGraniteChunkDestroyed(thing);
                    }
                }
            }
        }
    }
}