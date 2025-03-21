using HarmonyLib;
using Verse;

namespace EntrepreneurMod
{
    // Klasa bazowa moda
    public class EntrepreneurMod : Mod
    {
        public EntrepreneurMod(ModContentPack content) : base(content)
        {
            // Inicjalizacja Harmony
            Harmony harmony = new Harmony("EntrepreneurMod");
            harmony.PatchAll();

            Log.Message("Entrepreneur System mod initialized");
        }
    }
}