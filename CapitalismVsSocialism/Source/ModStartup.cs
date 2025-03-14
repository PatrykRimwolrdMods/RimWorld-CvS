using HarmonyLib;
using Verse;

[StaticConstructorOnStartup]
public static class ModStartup
{
    static ModStartup()
    {
        var harmony = new Harmony("com.minimalnymod.rimworld");
        harmony.PatchAll();
        Log.Message("[MOD] 🚀 Minimalny Mod załadowany – Koloniści będą zbierać jagody!");
    }
}