using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;

[HarmonyPatch(typeof(Pawn_JobTracker), "DetermineNextJob")]
public static class Patch_ThinkTree
{
    static void Postfix(Pawn_JobTracker __instance, ref ThinkResult __result)
    {
        Log.Message("[MOD] Patch do `DetermineNextJob()` działa! 🔧");

        Pawn pawn = (Pawn)AccessTools.Field(typeof(Pawn_JobTracker), "pawn").GetValue(__instance);

        if (pawn == null || !pawn.RaceProps.Humanlike)
        {
            return; // Ignorujemy zwierzęta i null
        }

        // Jeśli kolonista JUŻ pracuje, nie resetujemy zadania
        if (__result.Job != null && __result.Job.def != JobDefOf.Wait && __result.Job.def != JobDefOf.GotoWander)
        {
            Log.Message($"[MOD] {pawn.Name} już ma aktywne zadanie ({__result.Job.def.defName}) – pomijam.");
            return;
        }

        Log.Warning($"[MOD] {pawn.Name} jest IDLE! Resetuję i przypisuję nowe zadanie!");

        // Resetujemy stare zadania
        pawn.jobs.ClearQueuedJobs();
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);

        // Przypisujemy nowe zadanie
        JobGiver_AutoHarvest jobGiver = new JobGiver_AutoHarvest();
        MethodInfo tryGiveJobMethod = AccessTools.Method(typeof(JobGiver_AutoHarvest), "TryGiveJob");

        if (tryGiveJobMethod == null)
        {
            Log.Error("[MOD] Nie udało się znaleźć metody TryGiveJob()! ❌");
            return;
        }

        Job newJob = (Job)tryGiveJobMethod.Invoke(jobGiver, new object[] { pawn });

        if (newJob != null)
        {
            Log.Message($"[MOD] {pawn.Name} dostał zadanie: {newJob.def.defName} ✅");
            pawn.jobs.StartJob(newJob, JobCondition.InterruptForced);
            __result = new ThinkResult(newJob, null);
        }
        else
        {
            Log.Warning($"[MOD] {pawn.Name} NIE znalazł zadania. ❌");
        }
    }
}
