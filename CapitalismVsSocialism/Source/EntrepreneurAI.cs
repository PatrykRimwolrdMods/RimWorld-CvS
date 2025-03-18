using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Verse.AI;

public class EntrepreneurAI : GameComponent
{
    public Pawn entrepreneur;
    public List<Pawn> employees = new List<Pawn>();
    public int salary = 10;
    public int rawGraniteWealth = 0;
    public int processedGraniteWealth = 0;
    public bool canProduceBlocks = false;
    public bool hasFood = false;
    public Dictionary<Pawn, Zone_Stockpile> storageZones = new Dictionary<Pawn, Zone_Stockpile>();
    private bool initialized = false;
    private const int StorageSpacing = 3; // Odległość między magazynami
    private const int GraniteConversionRate = 20;
    private string primaryCurrency = "GraniteChunk";
    private string advancedCurrency = "BlocksGranite";
    private const int berryLimit = 100;

    public EntrepreneurAI(Game game) { }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref storageZones, "storageZones", LookMode.Reference, LookMode.Reference);
        Scribe_Values.Look(ref initialized, "initialized", false);
        Scribe_References.Look(ref entrepreneur, "entrepreneur");
        Scribe_Collections.Look(ref employees, "employees", LookMode.Reference);
        Scribe_Values.Look(ref salary, "salary", 10);
        Scribe_Values.Look(ref rawGraniteWealth, "rawGraniteWealth", 0);
        Scribe_Values.Look(ref processedGraniteWealth, "processedGraniteWealth", 0);
        Scribe_Values.Look(ref canProduceBlocks, "canProduceBlocks", false);
        Scribe_Values.Look(ref hasFood, "hasFood", false);
    }

    public override void GameComponentTick()
    {
        if (!initialized)
        {
            InitializeBusiness(Find.CurrentMap);
            InitializeStorageZones(Find.CurrentMap); // Tworzenie magazynów dla kolonistów
            initialized = true;
        }

        if (entrepreneur != null)
        {
            ManageBusiness(Find.CurrentMap);
        }
    }

    public void InitializeBusiness(Map map)
    {
        entrepreneur = FindRandomColonist(map);
        if (entrepreneur != null)
        {
            Messages.Message($"[DEBUG] Wybrano przedsiębiorcę: {entrepreneur.Name}", MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            Messages.Message("[ERROR] Nie znaleziono kolonisty do roli przedsiębiorcy!", MessageTypeDefOf.NegativeEvent);
        }
    }

    private Pawn FindRandomColonist(Map map)
    {
        return map.mapPawns.FreeColonists.RandomElement();
    }

    public void InitializeStorageZones(Map map)
    {
        List<Pawn> colonists = map.mapPawns.FreeColonists.ToList();
        if (colonists.Count == 0) return;

        Messages.Message($"[DEBUG] Tworzenie magazynów dla {colonists.Count} kolonistów.", MessageTypeDefOf.PositiveEvent);

        IntVec3 centralLocation = FindCentralLocation(map);
        int offset = 0;

        foreach (Pawn colonist in colonists)
        {
            IntVec3 storageLocation = centralLocation + new IntVec3(offset, 0, offset);
            offset += StorageSpacing; // Magazyny są rozmieszczone blisko siebie

            CreateStorageZone(colonist, storageLocation, map);
        }
    }

    private IntVec3 FindCentralLocation(Map map)
    {
        int centerX = map.Size.x / 2;
        int centerZ = map.Size.z / 2;
        return new IntVec3(centerX, 0, centerZ);
    }

    private void CreateStorageZone(Pawn colonist, IntVec3 location, Map map)
    {
        if (colonist == null || storageZones.ContainsKey(colonist)) return;

        ZoneManager zoneManager = map.zoneManager;
        Zone_Stockpile storageZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, zoneManager);
        zoneManager.RegisterZone(storageZone);

        // ✔ Magazyn jest teraz jeszcze większy (5x5)
        foreach (IntVec3 cell in GetStorageArea(location, map, 2))
        {
            storageZone.AddCell(cell);
        }

        storageZones[colonist] = storageZone;
        Messages.Message($"[DEBUG] {colonist.Name} utworzył magazyn w {location} ({storageZone.Cells.Count} kratek).", MessageTypeDefOf.PositiveEvent);
    }

    private List<IntVec3> GetStorageArea(IntVec3 center, Map map, int size)
    {
        List<IntVec3> storageArea = new List<IntVec3>();

        for (int x = -size; x <= size; x++)
        {
            for (int z = -size; z <= size; z++)
            {
                IntVec3 cell = center + new IntVec3(x, 0, z);
                if (cell.Standable(map) && cell.GetZone(map) == null)
                {
                    storageArea.Add(cell);
                }
            }
        }

        return storageArea;
    }

    private void ManageBusiness(Map map)
    {
        if (entrepreneur == null)
        {
            Messages.Message("[ERROR] Przedsiębiorca nie został wybrany!", MessageTypeDefOf.NegativeEvent);
            return;
        }

        Messages.Message($"[DEBUG] {entrepreneur.Name} sprawdza, co ma zrobić.", MessageTypeDefOf.NeutralEvent);

        if (!hasFood)
        {
            Messages.Message($"[DEBUG] {entrepreneur.Name} szuka pożywienia!", MessageTypeDefOf.NeutralEvent);
            GatherFoodOrHunt();
            return;
        }

        if (!storageZones.ContainsKey(entrepreneur))
        {
            Messages.Message($"[DEBUG] {entrepreneur.Name} nie ma magazynu – tworzenie nowego.", MessageTypeDefOf.NeutralEvent);
            CreateStorageZone(entrepreneur, FindSafeStorageLocation(map), map);
        }

        if (entrepreneur.CurJob != null && entrepreneur.CurJob.def != JobDefOf.Wait)
        {
            Messages.Message($"[DEBUG] {entrepreneur.Name} jest już zajęty ({entrepreneur.CurJob.def.defName}).", MessageTypeDefOf.NeutralEvent);
            return;
        }

        AssignGraniteMining();
    }

    private void GatherFoodOrHunt()
    {
        if (entrepreneur == null || entrepreneur.Map == null)
        {
            Messages.Message("[ERROR] Przedsiębiorca lub mapa nie istnieje!", MessageTypeDefOf.NegativeEvent);
            return;
        }

        // Debugowanie: wyświetlanie dostępnych ThingDef
        LogAvailableThingDefs();

        // Sprawdzenie poprawnej nazwy krzaka jagodowego i jagód.
        // Najpierw próbujemy standardowe nazwy z "gołego" RimWorld,
        // a potem ewentualnie inne nazwy, jeśli masz mody.
        ThingDef berryBushDef = DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Berry")
            ?? DefDatabase<ThingDef>.GetNamedSilentFail("Plant_BerryBush")
            ?? DefDatabase<ThingDef>.GetNamedSilentFail("BerryBush")
            ?? DefDatabase<ThingDef>.GetNamedSilentFail("Plant_Berries");

        ThingDef berryDef = DefDatabase<ThingDef>.GetNamedSilentFail("RawBerries")
            ?? DefDatabase<ThingDef>.GetNamedSilentFail("Berries");

        if (berryBushDef == null || berryDef == null)
        {
            Messages.Message("[ERROR] Nie znaleziono ThingDef dla jagód lub krzaków jagodowych!", MessageTypeDefOf.NegativeEvent);
            return;
        }

        // Sprawdzenie, ile jagód posiada przedsiębiorca
        int currentBerries = entrepreneur.inventory.innerContainer.TotalStackCountOfDef(berryDef);

        if (currentBerries >= berryLimit)
        {
            Messages.Message($"[DEBUG] {entrepreneur.Name} ma już {currentBerries} jagód – przechodzi do kolejnego zadania!", MessageTypeDefOf.PositiveEvent);
            hasFood = true;
            return;
        }

        List<Thing> berryBushes = entrepreneur.Map.listerThings.AllThings
            .Where(t => t.def == berryBushDef && t.HitPoints > 0)
            .ToList();

        if (berryBushes.Any())
        {
            Thing bush = berryBushes.RandomElement();
            Job harvestJob = new Job(JobDefOf.Harvest, bush);

            if (entrepreneur.CurJob != null && entrepreneur.CurJob.targetA == bush)
            {
                Messages.Message($"[DEBUG] {entrepreneur.Name} już zbiera jagody z krzaka w {bush.Position}.", MessageTypeDefOf.NeutralEvent);
                return;
            }

            entrepreneur.jobs.TryTakeOrderedJob(harvestJob);
            Messages.Message($"[DEBUG] {entrepreneur.Name} rozpoczął zbieranie jagód w {bush.Position}.", MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            // Jeżeli nie ma krzaków jagodowych, to sprawdźmy, czy są zwierzęta do polowania
            List<Pawn> huntableAnimals = entrepreneur.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.def != null && (p.def.defName == "Hare" || p.def.defName == "Rat"))
                .ToList();

            if (huntableAnimals.Any() && currentBerries < berryLimit)
            {
                Pawn target = huntableAnimals.RandomElement();
                Job huntJob = new Job(JobDefOf.Hunt, target);

                if (entrepreneur.CurJob != null && entrepreneur.CurJob.targetA == target)
                {
                    Messages.Message($"[DEBUG] {entrepreneur.Name} już poluje na {target.def.label}.", MessageTypeDefOf.NeutralEvent);
                    return;
                }

                entrepreneur.jobs.TryTakeOrderedJob(huntJob);
                Messages.Message($"[DEBUG] {entrepreneur.Name} rozpoczął polowanie na {target.def.label} w {target.Position}.", MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("[WARNING] Brak dostępnych krzaków jagodowych lub zwierząt do polowania!", MessageTypeDefOf.NeutralEvent);
            }
        }
    }

    private void LogAvailableThingDefs()
    {
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            Log.Message($"[DEBUG] ThingDef found: {def.defName}");
        }
    }

    private IntVec3 FindSafeStorageLocation(Map map)
    {
        if (map == null)
        {
            Messages.Message("[ERROR] Mapa nie została poprawnie załadowana!", MessageTypeDefOf.NegativeEvent);
            return IntVec3.Invalid;
        }

        IntVec3 centralLocation = new IntVec3(map.Size.x / 2, 0, map.Size.z / 2);

        foreach (IntVec3 cell in GenRadial.RadialCellsAround(centralLocation, 10, true))
        {
            if (cell.Standable(map) && cell.GetZone(map) == null)
            {
                Messages.Message($"[DEBUG] Znaleziono wolne miejsce na magazyn w {cell}.", MessageTypeDefOf.NeutralEvent);
                return cell;
            }
        }

        Messages.Message("[WARNING] Nie znaleziono wolnego miejsca na magazyn – ustawiono centralne.", MessageTypeDefOf.NeutralEvent);
        return centralLocation;
    }

    private void AssignGraniteMining()
    {
        if (entrepreneur.CurJob != null && entrepreneur.CurJob.def == JobDefOf.Mine)
        {
            Messages.Message($"[DEBUG] {entrepreneur.Name} już wydobywa granit.", MessageTypeDefOf.NeutralEvent);
            return;
        }

        List<Thing> mineableGranite = entrepreneur.Map.listerThings.AllThings
            .Where(t => t.def.defName == "MineableGranite")
            .ToList();

        if (mineableGranite.Any())
        {
            Thing graniteRock = mineableGranite.RandomElement();
            Job mineJob = new Job(JobDefOf.Mine, graniteRock);
            entrepreneur.jobs.TryTakeOrderedJob(mineJob);

            Messages.Message($"[DEBUG] {entrepreneur.Name} rozpoczął wydobycie granitu w {graniteRock.Position}.", MessageTypeDefOf.PositiveEvent);
        }
        else
        {
            Messages.Message("[ERROR] Brak skał granitu do wydobycia!", MessageTypeDefOf.NegativeEvent);
        }
    }

    private void GatherDroppedGranite()
    {
        Messages.Message($"[DEBUG] {entrepreneur.Name} zbiera surowy granit!", MessageTypeDefOf.PositiveEvent);
    }

    private void HireHungryColonists()
    {
        Messages.Message($"[DEBUG] {entrepreneur.Name} zatrudnia głodnych kolonistów!", MessageTypeDefOf.PositiveEvent);
    }

    private void ManageEmployees()
    {
        Messages.Message($"[DEBUG] {entrepreneur.Name} zarządza pracownikami!", MessageTypeDefOf.PositiveEvent);
    }

    private void CheckBlockProduction(Map map)
    {
        canProduceBlocks = map.mapPawns.FreeColonists.Any(p =>
            p.skills.GetSkill(SkillDefOf.Construction).Level >= 5 &&
            p.skills.GetSkill(SkillDefOf.Crafting).Level >= 5);

        if (canProduceBlocks)
        {
            Messages.Message("[DEBUG] Kolonia posiada budowniczego i rzemieślnika – produkcja bloków granitu możliwa!", MessageTypeDefOf.PositiveEvent);
        }
    }

    private void ConvertGraniteToBlocks()
    {
        if (canProduceBlocks && rawGraniteWealth >= 1)
        {
            int convertAmount = rawGraniteWealth;
            rawGraniteWealth -= convertAmount;
            processedGraniteWealth += convertAmount * GraniteConversionRate;
            Messages.Message($"[DEBUG] {entrepreneur.Name} przekształcił {convertAmount} jednostek surowego granitu na {convertAmount * GraniteConversionRate} bloków granitu!", MessageTypeDefOf.PositiveEvent);
        }
    }

    private void PaySalary(Pawn worker)
    {
        int payment = salary;

        if (processedGraniteWealth >= payment)
        {
            processedGraniteWealth -= payment;
            worker.inventory.innerContainer.TryAdd(
                ThingMaker.MakeThing(ThingDef.Named(advancedCurrency), null),
                payment
            );
            Messages.Message($"[DEBUG] {worker.Name} otrzymał {payment} {advancedCurrency}!", MessageTypeDefOf.PositiveEvent);
        }
        else if (rawGraniteWealth >= 1)
        {
            rawGraniteWealth -= 1;
            worker.inventory.innerContainer.TryAdd(
                ThingMaker.MakeThing(ThingDef.Named(primaryCurrency), null),
                GraniteConversionRate
            );
            Messages.Message($"[DEBUG] {worker.Name} otrzymał {GraniteConversionRate} {advancedCurrency} (z 1 {primaryCurrency})!", MessageTypeDefOf.PositiveEvent);
        }
    }
}
