using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;

public class JobGiver_AutoHarvest : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn == null || pawn.Downed || pawn.Dead)
        {
            Log.Warning("[MOD] `TryGiveJob()` → Kolonista jest martwy lub nieprzytomny! ❌");
            return null;
        }

        Log.Message($"[MOD] `TryGiveJob()` WYWOŁANE dla {pawn.Name}!");

        // Sprawdzamy, czy kolonista MA już aktywne zadanie "Harvest"
        if (pawn.CurJob != null && pawn.CurJob.def == JobDefOf.Harvest)
        {
            Log.Warning($"[MOD] {pawn.Name} już zbiera jagody! Nie przypisuję nowego zadania.");
            return null;
        }

        // Szukamy najbliższego dostępnego krzaka jagód
        Thing berryBush = FindClosestBerryBush(pawn);
        if (berryBush == null)
        {
            Log.Warning($"[MOD] {pawn.Name} NIE znalazł nowych jagód! ❌");
            return null;
        }

        Log.Message($"[MOD] {pawn.Name} idzie zbierać jagody z krzaka w {berryBush.Position}! 🍇");
        return JobMaker.MakeJob(JobDefOf.Harvest, berryBush);
    }

    private Thing FindClosestBerryBush(Pawn pawn)
    {
        // Pobieramy wszystkie krzaki jagód na mapie
        var allBerryBushes = pawn.Map.listerThings.ThingsOfDef(ThingDef.Named("Plant_Berry"));

        if (allBerryBushes == null || allBerryBushes.Count == 0)
        {
            Log.Warning("[MOD] ❌ Brak dostępnych krzaków jagód!");
            return null;
        }

        // Wybieramy najbliższy krzak, który:
        // 1. NIE jest już zbierany przez kogoś innego
        // 2. MA jagody do zebrania (growthStage >= 0.65)
        Thing berryBush = allBerryBushes
            .Where(bush => !pawn.Map.reservationManager.IsReservedByAnyoneOf(bush, pawn.Faction))
            .Where(bush => bush is Plant plant && plant.LifeStage == PlantLifeStage.Mature && plant.Growth >= 0.65f)
            .OrderBy(bush => pawn.Position.DistanceTo(bush.Position))
            .FirstOrDefault();

        if (berryBush != null)
        {
            Log.Message($"[MOD] 🟢 Znaleziono NOWY krzak jagód w {berryBush.Position}, który ma owoce!");
            return berryBush;
        }

        Log.Warning("[MOD] ❌ Wszystkie krzaki jagód są już zajęte LUB nie mają owoców!");
        return null;
    }
}
