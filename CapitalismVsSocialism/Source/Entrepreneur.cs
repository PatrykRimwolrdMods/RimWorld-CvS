using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;

namespace EntrepreneurMod
{
    // Klasa reprezentująca przedsiębiorcę
    public class Entrepreneur : IExposable
    {
        public Pawn pawn;
        public List<Pawn> employees = new List<Pawn>();
        public IntVec3 storageCenter;
        public int storageRadius = 5;
        public Dictionary<Pawn, int> wages = new Dictionary<Pawn, int>();
        public int graniteCurrency = 0;

        // Konstruktor pusty dla serializacji
        public Entrepreneur() { }

        // Konstruktor z kolonistą
        public Entrepreneur(Pawn pawn, IntVec3 storageCenter)
        {
            this.pawn = pawn;
            this.storageCenter = storageCenter;
        }

        // Metoda do zatrudnienia kolonisty
        public bool HireColonist(Pawn colonist)
        {
            if (employees.Contains(colonist)) return false;
            employees.Add(colonist);
            wages[colonist] = 0; // Na początku bez wynagrodzenia
            return true;
        }

        // Metoda do zwolnienia kolonisty
        public bool FireColonist(Pawn colonist)
        {
            if (!employees.Contains(colonist)) return false;
            employees.Remove(colonist);
            wages.Remove(colonist);
            return true;
        }

        // Metoda do ustawienia wynagrodzenia
        public void SetWage(Pawn colonist, int amount)
        {
            if (employees.Contains(colonist))
            {
                wages[colonist] = amount;
            }
        }

        // Metoda do wypłacenia wynagrodzenia
        public void PayWages()
        {
            foreach (Pawn employee in employees)
            {
                int wage = wages[employee];
                if (graniteCurrency >= wage && wage > 0)
                {
                    graniteCurrency -= wage;

                    // Dodaj fragmenty granitu do magazynu pracownika
                    ColonistProperty property = Find.World.GetComponent<PropertyManager>().GetPropertyForColonist(employee);
                    if (property != null)
                    {
                        property.AddGraniteCurrency(wage);
                        Messages.Message($"{pawn.Name} wypłacił {wage} fragmentów granitu dla {employee.Name}.", MessageTypeDefOf.PositiveEvent);
                    }
                }
            }
        }

        // Metoda do serializacji/deserializacji
        public void ExposeData()
        {
            Scribe_References.Look(ref pawn, "pawn");
            Scribe_Collections.Look(ref employees, "employees", LookMode.Reference);
            Scribe_Values.Look(ref storageCenter, "storageCenter");
            Scribe_Values.Look(ref storageRadius, "storageRadius", 5);
            Scribe_Collections.Look(ref wages, "wages", LookMode.Reference, LookMode.Value);
            Scribe_Values.Look(ref graniteCurrency, "graniteCurrency", 0);
        }
    }

    // Klasa reprezentująca własność kolonisty
    public class ColonistProperty : IExposable
    {
        public Pawn owner;
        public IntVec3 storageCenter;
        public int storageRadius = 3;
        public int graniteCurrency = 0;
        public List<Thing> ownedItems = new List<Thing>();

        // Konstruktor pusty dla serializacji
        public ColonistProperty() { }

        // Konstruktor z kolonistą
        public ColonistProperty(Pawn owner, IntVec3 storageCenter)
        {
            this.owner = owner;
            this.storageCenter = storageCenter;
        }

        // Metoda do dodania waluty granitowej
        public void AddGraniteCurrency(int amount)
        {
            graniteCurrency += amount;
        }

        // Metoda do wydania waluty granitowej
        public bool SpendGraniteCurrency(int amount)
        {
            if (graniteCurrency >= amount)
            {
                graniteCurrency -= amount;
                return true;
            }
            return false;
        }

        // Metoda do dodania przedmiotu do własności
        public void AddItem(Thing item)
        {
            if (!ownedItems.Contains(item))
            {
                ownedItems.Add(item);
            }
        }

        // Metoda do usunięcia przedmiotu z własności
        public void RemoveItem(Thing item)
        {
            ownedItems.Remove(item);
        }

        // Metoda do serializacji/deserializacji
        public void ExposeData()
        {
            Scribe_References.Look(ref owner, "owner");
            Scribe_Values.Look(ref storageCenter, "storageCenter");
            Scribe_Values.Look(ref storageRadius, "storageRadius", 3);
            Scribe_Values.Look(ref graniteCurrency, "graniteCurrency", 0);
            Scribe_Collections.Look(ref ownedItems, "ownedItems", LookMode.Reference);
        }
    }

    // Klasa zarządzająca własnością kolonistów
    public class PropertyManager : WorldComponent
    {
        private List<ColonistProperty> properties = new List<ColonistProperty>();

        // Konstruktor
        public PropertyManager(World world) : base(world) { }

        // Metoda do uzyskania własności kolonisty
        public ColonistProperty GetPropertyForColonist(Pawn colonist)
        {
            return properties.FirstOrDefault(p => p.owner == colonist);
        }

        // Metoda do utworzenia własności dla kolonisty
        public ColonistProperty CreatePropertyForColonist(Pawn colonist, IntVec3 storageCenter)
        {
            ColonistProperty property = new ColonistProperty(colonist, storageCenter);
            properties.Add(property);
            return property;
        }

        // Metoda do serializacji/deserializacji
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref properties, "properties", LookMode.Deep);
        }
    }

    // Klasa zarządzająca systemem przedsiębiorców
    public class EntrepreneurManager : GameComponent
    {
        private Entrepreneur entrepreneur;
        private ColonyPhase currentPhase = ColonyPhase.Hunting;
        private int phaseTicks = 0;
        private const int phaseTicksThreshold = 60000; // Około 1 dnia gry

        // Konstruktor dla nowej gry
        public EntrepreneurManager(Game game) { }

        // Metoda wywoływana przy starcie nowej gry
        public override void StartedNewGame()
        {
            base.StartedNewGame();

            // Wybierz jednego kolonistę jako przedsiębiorcę
            SelectInitialEntrepreneur();

            // Utwórz magazyny dla wszystkich kolonistów
            CreateStoragesForColonists();

            // Rozpocznij pierwszą fazę - polowanie
            StartHuntingPhase();
        }

        // Metoda wywoływana przy wczytaniu gry
        public override void LoadedGame()
        {
            base.LoadedGame();

            // Kontynuuj bieżącą fazę
            ContinueCurrentPhase();
        }

        // Metoda wywoływana co tick
        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (entrepreneur == null) return;

            phaseTicks++;

            // Sprawdź, czy należy przejść do następnej fazy
            if (phaseTicks >= phaseTicksThreshold)
            {
                AdvanceToNextPhase();
                phaseTicks = 0;
            }

            // Wykonaj akcje specyficzne dla bieżącej fazy
            TickCurrentPhase();
        }

        // Metoda do wyboru początkowego przedsiębiorcy
        private void SelectInitialEntrepreneur()
        {
            // Znajdź wszystkich kolonistów
            List<Pawn> colonists = PawnsFinder.AllMaps_FreeColonists.ToList();

            if (colonists.Count > 0)
            {
                // Wybierz kolonistę z najwyższymi umiejętnościami społecznymi
                Pawn bestCandidate = FindBestEntrepreneurCandidate(colonists);

                // Znajdź centrum mapy jako miejsce na magazyn
                IntVec3 mapCenter = bestCandidate.Map.Center;

                // Utwórz przedsiębiorcę
                entrepreneur = new Entrepreneur(bestCandidate, mapCenter);

                // Zatrudnij wszystkich pozostałych kolonistów
                foreach (Pawn colonist in colonists)
                {
                    if (colonist != bestCandidate)
                    {
                        entrepreneur.HireColonist(colonist);
                    }
                }

                // Powiadom gracza
                Messages.Message($"{bestCandidate.Name} został wybrany jako przedsiębiorca kolonii!", MessageTypeDefOf.PositiveEvent);
            }
        }

        // Metoda do znalezienia najlepszego kandydata na przedsiębiorcę
        private Pawn FindBestEntrepreneurCandidate(List<Pawn> colonists)
        {
            Pawn best = colonists[0];
            float bestScore = CalculateEntrepreneurScore(best);

            foreach (Pawn colonist in colonists)
            {
                float score = CalculateEntrepreneurScore(colonist);
                if (score > bestScore)
                {
                    best = colonist;
                    bestScore = score;
                }
            }

            return best;
        }

        // Metoda do obliczenia punktacji przedsiębiorczej kolonisty
        private float CalculateEntrepreneurScore(Pawn pawn)
        {
            float score = 0;

            // Umiejętności ważne dla przedsiębiorcy
            if (pawn.skills.GetSkill(SkillDefOf.Social) != null)
                score += pawn.skills.GetSkill(SkillDefOf.Social).Level * 2;

            if (pawn.skills.GetSkill(SkillDefOf.Intellectual) != null)
                score += pawn.skills.GetSkill(SkillDefOf.Intellectual).Level;

            return score;
        }

        // Metoda do tworzenia magazynów dla kolonistów
        private void CreateStoragesForColonists()
        {
            if (entrepreneur == null || entrepreneur.pawn.Map == null) return;

            Map map = entrepreneur.pawn.Map;
            IntVec3 mapCenter = map.Center;

            // Utwórz magazyn dla przedsiębiorcy
            CreateStorageZone(map, entrepreneur.storageCenter, entrepreneur.storageRadius, "Magazyn przedsiębiorcy");

            // Utwórz magazyny dla pracowników
            PropertyManager propertyManager = Find.World.GetComponent<PropertyManager>();

            int angle = 0;
            int distance = 15;

            foreach (Pawn employee in entrepreneur.employees)
            {
                // Oblicz pozycję magazynu w okręgu wokół centrum mapy
                float radians = Mathf.Deg2Rad * angle;
                IntVec3 storagePos = new IntVec3(
                    mapCenter.x + (int)(distance * Mathf.Cos(radians)),
                    0,
                    mapCenter.z + (int)(distance * Mathf.Sin(radians))
                );

                // Upewnij się, że pozycja jest w granicach mapy
                storagePos = new IntVec3(
                    Mathf.Clamp(storagePos.x, 10, map.Size.x - 10),
                    0,
                    Mathf.Clamp(storagePos.z, 10, map.Size.z - 10)
                );

                // Utwórz własność dla kolonisty
                ColonistProperty property = propertyManager.CreatePropertyForColonist(employee, storagePos);

                // Utwórz strefę magazynową
                CreateStorageZone(map, storagePos, property.storageRadius, $"Magazyn {employee.Name}");

                // Zwiększ kąt dla następnego magazynu
                angle += 45;
            }

            // Utwórz miejsce rzeźnicze
            IntVec3 butcherSpot = new IntVec3(mapCenter.x + 10, 0, mapCenter.z + 10);
            Building_WorkTable butcherTable = (Building_WorkTable)ThingMaker.MakeThing(ThingDefOf.ButcherSpot);
            GenSpawn.Spawn(butcherTable, butcherSpot, map);

            Messages.Message("Utworzono magazyny dla wszystkich kolonistów oraz miejsce rzeźnicze.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do tworzenia strefy magazynowej
        private void CreateStorageZone(Map map, IntVec3 center, int radius, string label)
        {
            Zone_Stockpile stockpile = new Zone_Stockpile(label, map.zoneManager);

            // Dodaj komórki w promieniu do strefy magazynowej
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map) && cell.Walkable(map))
                {
                    stockpile.AddCell(cell);
                }
            }

            // Ustaw ustawienia magazynu
            StorageSettings settings = stockpile.GetStoreSettings();
            settings.filter.SetDisallowAll();

            // Pozwól na przechowywanie fragmentów granitu
            settings.filter.SetAllow(ThingDefOf.ChunkGranite, true);

            // Dodaj strefę do mapy
            map.zoneManager.RegisterZone(stockpile);
        }

        // Metoda do rozpoczęcia fazy polowania
        private void StartHuntingPhase()
        {
            currentPhase = ColonyPhase.Hunting;

            // Przydziel zadania polowania
            AssignHuntingJobs();

            // Powiadom gracza
            Messages.Message("Rozpoczęto fazę polowania. Koloniści będą polować na najsłabsze zwierzęta.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do przydzielania zadań polowania
        private void AssignHuntingJobs()
        {
            if (entrepreneur == null || entrepreneur.pawn.Map == null) return;

            Map map = entrepreneur.pawn.Map;

            // Przydziel zadania polowania wszystkim kolonistom
            foreach (Pawn colonist in entrepreneur.employees)
            {
                // Upewnij się, że kolonista ma broń
                EnsureHasWeapon(colonist);

                // Znajdź najsłabsze zwierzę do polowania
                Pawn targetAnimal = FindWeakestAnimal(map);

                // Jeśli znaleziono cel, wydaj rozkaz polowania
                if (targetAnimal != null)
                {
                    Job huntJob = JobMaker.MakeJob(JobDefOf.Hunt, targetAnimal);
                    colonist.jobs.TryTakeOrderedJob(huntJob);
                }
            }
        }

        // Metoda do upewnienia się, że kolonista ma broń
        private void EnsureHasWeapon(Pawn pawn)
        {
            if (pawn.equipment.Primary != null) return; // Już ma broń

            // Znajdź broń w kolonii
            Thing weapon = pawn.Map.listerThings.AllThings
                .Where(t => t.def.IsRangedWeapon || t.def.IsMeleeWeapon)
                .OrderByDescending(t => t.MarketValue)
                .FirstOrDefault();

            if (weapon != null)
            {
                // Podnieś broń
                if (pawn.CanReserveAndReach(weapon, PathEndMode.Touch, Danger.Deadly))
                {
                    Job getWeaponJob = JobMaker.MakeJob(JobDefOf.Equip, weapon);
                    pawn.jobs.TryTakeOrderedJob(getWeaponJob);
                }
            }
            else
            {
                // Jeśli nie ma broni, stwórz nóż
                ThingDef knifeDef = ThingDefOf.MeleeWeapon_Knife;
                Thing knife = ThingMaker.MakeThing(knifeDef);
                GenPlace.TryPlaceThing(knife, pawn.Position, pawn.Map, ThingPlaceMode.Near);

                // Podnieś nóż
                Job getKnifeJob = JobMaker.MakeJob(JobDefOf.Equip, knife);
                pawn.jobs.TryTakeOrderedJob(getKnifeJob);
            }
        }

        // Metoda do znalezienia najsłabszego zwierzęcia na mapie
        private Pawn FindWeakestAnimal(Map map)
        {
            return map.mapPawns.AllPawnsSpawned
                .Where(p => p.AnimalOrWildMan() && !p.IsPrisoner && !p.Downed && !p.Dead)
                .OrderBy(p => p.health.summaryHealth.SummaryHealthPercent)
                .FirstOrDefault();
        }

        // Metoda do rozpoczęcia fazy leczenia
        private void StartHealingPhase()
        {
            currentPhase = ColonyPhase.Healing;

            // Przydziel zadania leczenia
            AssignHealingJobs();

            // Powiadom gracza
            Messages.Message("Rozpoczęto fazę leczenia. Najlepsi medycy będą leczyć rannych kolonistów.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do przydzielania zadań leczenia
        private void AssignHealingJobs()
        {
            if (entrepreneur == null) return;

            // Znajdź kolonistów z umiejętnościami medycznymi
            List<Pawn> medics = entrepreneur.employees
                .Where(p => p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                .OrderByDescending(p => p.skills.GetSkill(SkillDefOf.Medicine).Level)
                .ToList();

            if (medics.Count == 0) return;

            // Znajdź rannych kolonistów
            List<Pawn> patients = entrepreneur.employees
                .Concat(new[] { entrepreneur.pawn })
                .Where(p => p.health.HasHediffsNeedingTend())
                .ToList();

            // Przydziel medyków do pacjentów
            for (int i = 0; i < patients.Count; i++)
            {
                Pawn patient = patients[i];
                Pawn medic = medics[i % medics.Count]; // Cyklicznie przydzielaj medyków

                // Wydaj rozkaz leczenia
                Job tendJob = JobMaker.MakeJob(JobDefOf.Tend, patient);
                medic.jobs.TryTakeOrderedJob(tendJob);
            }
        }

        // Metoda do rozpoczęcia fazy rzeźniczej
        private void StartButcheringPhase()
        {
            currentPhase = ColonyPhase.Butchering;

            // Przydziel zadania rzeźnicze
            AssignButcheringJobs();

            // Powiadom gracza
            Messages.Message("Rozpoczęto fazę rzeźniczą. Koloniści będą zbierać mięso z upolowanych zwierząt.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do przydzielania zadań rzeźniczych
        private void AssignButcheringJobs()
        {
            if (entrepreneur == null || entrepreneur.pawn.Map == null) return;

            Map map = entrepreneur.pawn.Map;

            // Znajdź miejsce rzeźnicze
            Building_WorkTable butcherSpot = map.listerBuildings.AllBuildingsColonistOfDef(ThingDefOf.ButcherSpot).FirstOrDefault() as Building_WorkTable;

            if (butcherSpot == null) return;

            // Znajdź martwe zwierzęta
            List<Corpse> corpses = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                .OfType<Corpse>()
                .Where(c => c.InnerPawn.RaceProps.Animal && !c.IsBusy())
                .ToList();

            // Przydziel zadania rzeźnicze
            foreach (Pawn colonist in entrepreneur.employees)
            {
                // Jeśli są zwłoki do przetworzenia
                if (corpses.Any())
                {
                    Corpse corpse = corpses.First();
                    corpses.Remove(corpse);

                    // Wydaj rozkaz rzeźniczenia
                    Job butcherJob = JobMaker.MakeJob(JobDefOf.DoBill, butcherSpot);
                    butcherJob.targetA = butcherSpot;
                    butcherJob.targetB = corpse;
                    butcherJob.count = 1;

                    colonist.jobs.TryTakeOrderedJob(butcherJob);
                }
            }
        }

        // Metoda do rozpoczęcia fazy jedzenia
        private void StartEatingPhase()
        {
            currentPhase = ColonyPhase.Eating;

            // Przydziel zadania jedzenia
            AssignEatingJobs();

            // Powiadom gracza
            Messages.Message("Rozpoczęto fazę jedzenia. Koloniści będą jeść zebrane pożywienie.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do przydzielania zadań jedzenia
        private void AssignEatingJobs()
        {
            if (entrepreneur == null || entrepreneur.pawn.Map == null) return;

            Map map = entrepreneur.pawn.Map;

            // Znajdź dostępne jedzenie
            List<Thing> food = map.listerThings.ThingsInGroup(ThingRequestGroup.FoodSource)
                .Where(f => f.def.IsNutritionGivingIngestible && !f.IsForbidden(null))
                .ToList();

            // Przydziel zadania jedzenia
            foreach (Pawn colonist in entrepreneur.employees.Concat(new[] { entrepreneur.pawn }))
            {
                // Jeśli kolonista jest głodny i jest dostępne jedzenie
                if (colonist.needs.food.CurLevel < 0.7f && food.Any())
                {
                    Thing foodSource = food.First();

                    // Wydaj rozkaz jedzenia
                    Job eatJob = JobMaker.MakeJob(JobDefOf.Ingest, foodSource);
                    eatJob.count = FoodUtility.WillIngestStackCountOf(colonist, foodSource.def);

                    colonist.jobs.TryTakeOrderedJob(eatJob);
                }
            }
        }

        // Metoda do rozpoczęcia fazy zbierania granitu
        private void StartGraniteCollectionPhase()
        {
            currentPhase = ColonyPhase.GraniteCollection;

            // Przydziel zadania zbierania granitu
            AssignGraniteCollectionJobs();

            // Powiadom gracza
            Messages.Message("Rozpoczęto fazę zbierania granitu. Koloniści będą zbierać fragmenty granitu jako walutę.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do przydzielania zadań zbierania granitu
        private void AssignGraniteCollectionJobs()
        {
            if (entrepreneur == null || entrepreneur.pawn.Map == null) return;

            Map map = entrepreneur.pawn.Map;

            // Znajdź fragmenty granitu na mapie
            List<Thing> graniteChunks = map.listerThings.ThingsOfDef(ThingDefOf.ChunkGranite)
                .Where(c => !c.IsForbidden(null))
                .ToList();

            // Jeśli jest mało fragmentów granitu, oznacz miejsca do wydobycia
            if (graniteChunks.Count < entrepreneur.employees.Count * 2)
            {
                DesignateGraniteForMining(map);
            }

            // Przydziel zadania zbierania granitu
            foreach (Pawn colonist in entrepreneur.employees)
            {
                // Jeśli są fragmenty granitu do zebrania
                if (graniteChunks.Any())
                {
                    Thing chunk = graniteChunks.First();
                    graniteChunks.Remove(chunk);

                    // Wydaj rozkaz przeniesienia do magazynu przedsiębiorcy
                    IntVec3 storageCell = FindStorageCellFor(chunk, entrepreneur.storageCenter, entrepreneur.storageRadius, map);

                    if (storageCell.IsValid)
                    {
                        Job haulJob = JobMaker.MakeJob(JobDefOf.HaulToCell, chunk, storageCell);
                        haulJob.count = 1;

                        colonist.jobs.TryTakeOrderedJob(haulJob);
                    }
                }
                else
                {
                    // Jeśli nie ma fragmentów do zebrania, przydziel zadanie wydobycia
                    AssignMiningJob(colonist);
                }
            }
        }

        // Metoda do oznaczania granitu do wydobycia
        private void DesignateGraniteForMining(Map map)
        {
            // Znajdź złoża granitu
            List<Thing> graniteRocks = map.listerThings.ThingsOfDef(ThingDefOf.MineableGranite)
                .Where(r => !r.IsForbidden(null) && !map.designationManager.DesignationOn(r, DesignationDefOf.Mine))
                .ToList();

            // Oznacz złoża do wydobycia
            foreach (Thing rock in graniteRocks.Take(10))
            {
                map.designationManager.AddDesignation(new Designation(rock, DesignationDefOf.Mine));
            }
        }

        // Metoda do przydzielania zadania wydobycia
        private void AssignMiningJob(Pawn colonist)
        {
            Map map = colonist.Map;

            // Znajdź oznaczone złoża granitu
            List<Thing> designatedRocks = map.designationManager.SpawnedDesignationsOfDef(DesignationDefOf.Mine)
                .Select(d => d.target.Thing)
                .Where(r => r.def == ThingDefOf.MineableGranite)
                .ToList();

            // Jeśli są oznaczone złoża, przydziel zadanie wydobycia
            if (designatedRocks.Any())
            {
                Thing rock = designatedRocks.First();

                Job mineJob = JobMaker.MakeJob(JobDefOf.Mine, rock);
                colonist.jobs.TryTakeOrderedJob(mineJob);
            }
        }

        // Metoda do znalezienia komórki magazynowej dla przedmiotu
        private IntVec3 FindStorageCellFor(Thing thing, IntVec3 center, int radius, Map map)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map) && cell.Walkable(map) && !cell.Filled(map) &&
                    StoreUtility.IsGoodStoreCell(cell, map, thing, null, map.ParentFaction))
                {
                    return cell;
                }
            }

            return IntVec3.Invalid;
        }

        // Metoda do rozpoczęcia fazy handlu
        private void StartTradingPhase()
        {
            currentPhase = ColonyPhase.Trading;

            // Ustaw wynagrodzenia dla pracowników
            SetWagesForEmployees();

            // Wypłać wynagrodzenia
            entrepreneur.PayWages();

            // Powiadom gracza
            Messages.Message("Rozpoczęto fazę handlu. Pracownicy otrzymują wynagrodzenie i mogą handlować.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do ustawiania wynagrodzeń dla pracowników
        private void SetWagesForEmployees()
        {
            if (entrepreneur == null) return;

            // Oblicz dostępne środki
            int availableFunds = entrepreneur.graniteCurrency;
            int totalWage = availableFunds / 2; // Przeznacz połowę środków na wypłaty
            int wagePerEmployee = totalWage / Math.Max(1, entrepreneur.employees.Count);

            // Ustaw wynagrodzenia
            foreach (Pawn employee in entrepreneur.employees)
            {
                entrepreneur.SetWage(employee, wagePerEmployee);
            }

            Messages.Message($"Przedsiębiorca {entrepreneur.pawn.Name} ustalił wynagrodzenie w wysokości {wagePerEmployee} fragmentów granitu dla każdego pracownika.", MessageTypeDefOf.PositiveEvent);
        }

        // Metoda do kontynuowania bieżącej fazy
        private void ContinueCurrentPhase()
        {
            switch (currentPhase)
            {
                case ColonyPhase.Hunting:
                    AssignHuntingJobs();
                    break;
                case ColonyPhase.Healing:
                    AssignHealingJobs();
                    break;
                case ColonyPhase.Butchering:
                    AssignButcheringJobs();
                    break;
                case ColonyPhase.Eating:
                    AssignEatingJobs();
                    break;
                case ColonyPhase.GraniteCollection:
                    AssignGraniteCollectionJobs();
                    break;
                case ColonyPhase.Trading:
                    // Nic nie rób, handel jest inicjowany przez graczy
                    break;
            }
        }

        // Metoda do wykonywania akcji specyficznych dla bieżącej fazy
        private void TickCurrentPhase()
        {
            switch (currentPhase)
            {
                case ColonyPhase.Hunting:
                    // Sprawdź, czy wszystkie zwierzęta zostały upolowane
                    CheckIfHuntingComplete();
                    break;
                case ColonyPhase.Healing:
                    // Sprawdź, czy wszyscy koloniści zostali wyleczeni
                    CheckIfHealingComplete();
                    break;
                case ColonyPhase.Butchering:
                    // Sprawdź, czy wszystkie zwłoki zostały przetworzone
                    CheckIfButcheringComplete();
                    break;
                case ColonyPhase.Eating:
                    // Sprawdź, czy wszyscy koloniści się najedli
                    CheckIfEatingComplete();
                    break;
                case ColonyPhase.GraniteCollection:
                    // Sprawdź, czy zebrano wystarczająco dużo granitu
                    CheckIfGraniteCollectionComplete();
                    break;
                case ColonyPhase.Trading:
                    // Sprawdź, czy handel został zakończony
                    CheckIfTradingComplete();
                    break;
            }
        }

        // Metoda do sprawdzania, czy polowanie zostało zakończone
        private void CheckIfHuntingComplete()
        {
            if (entrepreneur == null || entrepreneur.pawn.Map == null) return;

            Map map = entrepreneur.pawn.Map;

            // Sprawdź, czy są jeszcze zwierzęta do polowania
            bool animalsRemain = map.mapPawns.AllPawnsSpawned.Any(p => p.AnimalOrWildMan() && !p.IsPrisoner && !p.Downed && !p.Dead);

            // Jeśli nie ma zwierząt lub minęło wystarczająco dużo czasu, przejdź do leczenia
            if (!animalsRemain || phaseTicks >= phaseTicksThreshold / 2)
            {
                StartHealingPhase();
                phaseTicks = 0;
            }
        }

        // Metoda do sprawdzania, czy leczenie zostało zakończone
        private void CheckIfHealingComplete()
        {
            if (entrepreneur == null) return;

            // Sprawdź, czy są jeszcze koloniści wymagający leczenia
            bool patientsRemain = entrepreneur.employees
                .Concat(new[] { entrepreneur.pawn })
                .Any(p => p.health.HasHediffsNeedingTend());

            // Jeśli nie ma pacjentów lub minęło wystarczająco dużo czasu, przejdź do rzeźniczenia
            if (!patientsRemain || phaseTicks >= phaseTicksThreshold / 4)
            {
                StartButcheringPhase();
                phaseTicks = 0;
            }
        }

        // Metoda do sprawdzania, czy rzeźniczenie zostało zakończone
        private void CheckIfButcheringComplete()
        {
            if (entrepreneur == null || entrepreneur.pawn.Map == null) return;

            Map map = entrepreneur.pawn.Map;

            // Sprawdź, czy są jeszcze zwłoki do przetworzenia
            bool corpsesRemain = map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
                .OfType<Corpse>()
                .Any(c => c.InnerPawn.RaceProps.Animal && !c.IsBusy());

            // Jeśli nie ma zwłok lub minęło wystarczająco dużo czasu, przejdź do jedzenia
            if (!corpsesRemain || phaseTicks >= phaseTicksThreshold / 4)
            {
                StartEatingPhase();
                phaseTicks = 0;
            }
        }

        // Metoda do sprawdzania, czy jedzenie zostało zakończone
        private void CheckIfEatingComplete()
        {
            if (entrepreneur == null) return;

            // Sprawdź, czy wszyscy koloniści są najedzeni
            bool allFed = entrepreneur.employees
                .Concat(new[] { entrepreneur.pawn })
                .All(p => p.needs.food.CurLevel >= 0.9f);

            // Jeśli wszyscy są najedzeni lub minęło wystarczająco dużo czasu, przejdź do zbierania granitu
            if (allFed || phaseTicks >= phaseTicksThreshold / 4)
            {
                StartGraniteCollectionPhase();
                phaseTicks = 0;
            }
        }

        // Metoda do sprawdzania, czy zbieranie granitu zostało zakończone
        private void CheckIfGraniteCollectionComplete()
        {
            if (entrepreneur == null) return;

            // Sprawdź, czy zebrano wystarczająco dużo granitu
            bool enoughGranite = entrepreneur.graniteCurrency >= entrepreneur.employees.Count * 10;

            // Jeśli zebrano wystarczająco dużo granitu lub minęło wystarczająco dużo czasu, przejdź do handlu
            if (enoughGranite || phaseTicks >= phaseTicksThreshold)
            {
                StartTradingPhase();
                phaseTicks = 0;
            }
        }

        // Metoda do sprawdzania, czy handel został zakończony
        private void CheckIfTradingComplete()
        {
            // Po określonym czasie wróć do polowania
            if (phaseTicks >= phaseTicksThreshold)
            {
                StartHuntingPhase();
                phaseTicks = 0;
            }
        }

        // Metoda do przejścia do następnej fazy
        private void AdvanceToNextPhase()
        {
            switch (currentPhase)
            {
                case ColonyPhase.Hunting:
                    StartHealingPhase();
                    break;
                case ColonyPhase.Healing:
                    StartButcheringPhase();
                    break;
                case ColonyPhase.Butchering:
                    StartEatingPhase();
                    break;
                case ColonyPhase.Eating:
                    StartGraniteCollectionPhase();
                    break;
                case ColonyPhase.GraniteCollection:
                    StartTradingPhase();
                    break;
                case ColonyPhase.Trading:
                    StartHuntingPhase();
                    break;
            }
        }

        // Metoda do uzyskania przedsiębiorcy
        public Entrepreneur GetEntrepreneur()
        {
            return entrepreneur;
        }

        // Metoda do serializacji/deserializacji
        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Deep.Look(ref entrepreneur, "entrepreneur");
            Scribe_Values.Look(ref currentPhase, "currentPhase", ColonyPhase.Hunting);
            Scribe_Values.Look(ref phaseTicks, "phaseTicks", 0);
        }
    }

    // Enum definiujący fazy kolonii
    public enum ColonyPhase
    {
        Hunting,        // Polowanie na zwierzęta
        Healing,        // Leczenie rannych
        Butchering,     // Rzeźniczenie zwierząt
        Eating,         // Jedzenie
        GraniteCollection, // Zbieranie granitu
        Trading         // Handel
    }

    // Enum definiujący typy pracy
    public enum WorkType
    {
        Hunting,        // Polowanie
        Healing,        // Leczenie
        Butchering,     // Rzeźniczenie
        Cooking,        // Gotowanie
        Mining,         // Górnictwo
        Hauling,        // Przenoszenie
        Building,       // Budowanie
        Farming,        // Uprawa
        Research,       // Badania
        Defense,        // Obrona
        Lumberjack      // Drwal
    }

    // Klasa do obsługi zdarzeń związanych z przedsiębiorcą
    public class EntrepreneurEvents
    {
        // Metoda wywoływana, gdy zwierzę zostanie zabite przez kolonistę
        public static void OnAnimalKilled(Pawn animal, Pawn killer)
        {
            if (killer == null || animal == null) return;

            EntrepreneurManager manager = Current.Game?.GetComponent<EntrepreneurManager>();
            if (manager == null) return;

            Entrepreneur entrepreneur = manager.GetEntrepreneur();
            if (entrepreneur == null) return;

            // Sprawdź, czy zabójca jest pracownikiem przedsiębiorcy
            if (entrepreneur.employees.Contains(killer) || killer == entrepreneur.pawn)
            {
                // Dodaj myśl o udanym polowaniu
                killer.needs.mood.thoughts.memories.TryGainMemory(ThoughtDef.Named("KilledAnimal"));

                // Powiadom o sukcesie
                Messages.Message($"{killer.Name} zabił {animal.Label} podczas polowania.", MessageTypeDefOf.PositiveEvent);

                // Znajdź nowy cel do polowania
                Pawn newTarget = manager.FindWeakestAnimal(killer.Map);
                if (newTarget != null)
                {
                    Job huntJob = JobMaker.MakeJob(JobDefOf.Hunt, newTarget);
                    killer.jobs.TryTakeOrderedJob(huntJob);
                }
            }
        }

        // Metoda wywoływana, gdy fragment granitu zostanie przeniesiony do magazynu przedsiębiorcy
        public static void OnGraniteHauled(Thing granite, Pawn hauler)
        {
            if (granite == null || hauler == null) return;

            EntrepreneurManager manager = Current.Game?.GetComponent<EntrepreneurManager>();
            if (manager == null) return;

            Entrepreneur entrepreneur = manager.GetEntrepreneur();
            if (entrepreneur == null) return;

            // Sprawdź, czy przenoszący jest pracownikiem przedsiębiorcy
            if (entrepreneur.employees.Contains(hauler))
            {
                // Zwiększ walutę przedsiębiorcy
                entrepreneur.graniteCurrency++;

                // Powiadom o sukcesie
                Messages.Message($"{hauler.Name} dostarczył fragment granitu do magazynu {entrepreneur.pawn.Name}. Obecna ilość: {entrepreneur.graniteCurrency}.", MessageTypeDefOf.PositiveEvent);
            }
        }
    }

    // Klasa do dodania przycisków interfejsu
    [StaticConstructorOnStartup]
    public static class EntrepreneurInterface
    {
        // Tekstury przycisków
        private static readonly Texture2D TradeIcon = ContentFinder<Texture2D>.Get("UI/Commands/Trade", true);

        // Konstruktor statyczny
        static EntrepreneurInterface()
        {
            // Dodaj gizmo do kolonistów
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("EntrepreneurMod");
                harmony.Patch(
                    AccessTools.Method(typeof(Pawn), "GetGizmos"),
                    null,
                    new HarmonyLib.HarmonyMethod(typeof(EntrepreneurInterface), nameof(GetGizmosPostfix))
                );
            });
        }

        // Metoda dodająca gizmo do kolonistów
        public static IEnumerable<Gizmo> GetGizmosPostfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            // Zwróć oryginalne gizmo
            foreach (Gizmo gizmo in __result)
            {
                yield return gizmo;
            }

            // Sprawdź, czy kolonista jest graczem
            if (__instance.Faction == Faction.OfPlayer && __instance.RaceProps.Humanlike)
            {
                EntrepreneurManager manager = Current.Game?.GetComponent<EntrepreneurManager>();
                PropertyManager propertyManager = Find.World?.GetComponent<PropertyManager>();

                if (manager != null && propertyManager != null)
                {
                    Entrepreneur entrepreneur = manager.GetEntrepreneur();

                    if (entrepreneur != null)
                    {
                        // Dodaj przycisk handlu dla wszystkich kolonistów
                        yield return new Command_Action
                        {
                            defaultLabel = "Handluj",
                            defaultDesc = "Handluj z innymi kolonistami używając fragmentów granitu jako waluty.",
                            icon = TradeIcon,
                            action = delegate { OpenTradeDialog(__instance, entrepreneur, propertyManager); }
                        };
                    }
                }
            }
        }

        // Metoda do otwierania okna dialogowego handlu
        private static void OpenTradeDialog(Pawn trader, Entrepreneur entrepreneur, PropertyManager propertyManager)
        {
            // Utwórz listę kolonistów, z którymi można handlować
            List<Pawn> tradingPartners = new List<Pawn>();

            // Dodaj przedsiębiorcę, jeśli trader nie jest przedsiębiorcą
            if (trader != entrepreneur.pawn)
            {
                tradingPartners.Add(entrepreneur.pawn);
            }

            // Dodaj innych pracowników
            foreach (Pawn employee in entrepreneur.employees)
            {
                if (employee != trader)
                {
                    tradingPartners.Add(employee);
                }
            }

            // Utwórz opcje menu dla każdego partnera handlowego
            List<FloatMenuOption> options = new List<FloatMenuOption>();

            foreach (Pawn partner in tradingPartners)
            {
                options.Add(new FloatMenuOption($"Handluj z {partner.Name}", delegate
                {
                    // Pobierz własność obu stron
                    ColonistProperty traderProperty = propertyManager.GetPropertyForColonist(trader);
                    ColonistProperty partnerProperty = propertyManager.GetPropertyForColonist(partner);

                    if (traderProperty != null && partnerProperty != null)
                    {
                        // Tutaj można otworzyć okno dialogowe handlu
                        // Na razie tylko wyświetl informacje o posiadanych środkach
                        Messages.Message($"{trader.Name} ma {traderProperty.graniteCurrency} fragmentów granitu. {partner.Name} ma {partnerProperty.graniteCurrency} fragmentów granitu.", MessageTypeDefOf.NeutralEvent);
                    }
                }));
            }

            // Wyświetl menu
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    // Patch do przechwytywania zabicia zwierzęcia
    // Patch do przechwytywania zabicia zwierzęcia
    [HarmonyPatch(typeof(Pawn), "Kill")]
    public class Patch_Pawn_Kill
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn __instance, DamageInfo? dinfo)
        {
            if (__instance.AnimalOrWildMan() && dinfo.HasValue && dinfo.Value.Instigator is Pawn killer)
            {
                EntrepreneurEvents.OnAnimalKilled(__instance, killer);
            }
        }
    }

    // Patch do przechwytywania przeniesienia fragmentu granitu
    [HarmonyPatch(typeof(Pawn_CarryTracker), "TryDropCarriedThing")]
    public class Patch_Pawn_CarryTracker_TryDropCarriedThing
    {
        [HarmonyPostfix]
        public static void Postfix(Pawn_CarryTracker __instance, IntVec3 dropLoc, ThingPlaceMode mode, Thing resultingThing, bool __result)
        {
            if (__result && resultingThing != null && resultingThing.def == ThingDefOf.ChunkGranite)
            {
                EntrepreneurManager manager = Current.Game?.GetComponent<EntrepreneurManager>();
                if (manager == null) return;

                Entrepreneur entrepreneur = manager.GetEntrepreneur();
                if (entrepreneur == null) return;

                // Sprawdź, czy lokalizacja upuszczenia jest w magazynie przedsiębiorcy
                if (dropLoc.InHorDistOf(entrepreneur.storageCenter, entrepreneur.storageRadius))
                {
                    EntrepreneurEvents.OnGraniteHauled(resultingThing, __instance.pawn);
                }
            }
        }
    }

    // Klasa do implementacji handlu między kolonistami
    public class TradeSystem
    {
        // Metoda do przeprowadzenia transakcji między kolonistami
        public static bool ExecuteTrade(Pawn buyer, Pawn seller, Thing item, int price)
        {
            if (buyer == null || seller == null || item == null || price <= 0) return false;

            PropertyManager propertyManager = Find.World.GetComponent<PropertyManager>();
            if (propertyManager == null) return false;

            // Pobierz własność obu stron
            ColonistProperty buyerProperty = propertyManager.GetPropertyForColonist(buyer);
            ColonistProperty sellerProperty = propertyManager.GetPropertyForColonist(seller);

            if (buyerProperty == null || sellerProperty == null) return false;

            // Sprawdź, czy kupujący ma wystarczająco dużo waluty
            if (buyerProperty.graniteCurrency < price) return false;

            // Sprawdź, czy sprzedający posiada przedmiot
            if (!sellerProperty.ownedItems.Contains(item)) return false;

            // Przeprowadź transakcję
            buyerProperty.SpendGraniteCurrency(price);
            sellerProperty.AddGraniteCurrency(price);

            // Przenieś przedmiot do własności kupującego
            sellerProperty.RemoveItem(item);
            buyerProperty.AddItem(item);

            // Przenieś przedmiot fizycznie do magazynu kupującego
            if (item.Spawned)
            {
                item.DeSpawn();
            }

            IntVec3 storageCell = FindStorageCellFor(item, buyerProperty.storageCenter, buyerProperty.storageRadius, buyer.Map);
            if (storageCell.IsValid)
            {
                GenPlace.TryPlaceThing(item, storageCell, buyer.Map, ThingPlaceMode.Near);
            }

            // Powiadom o transakcji
            Messages.Message($"{buyer.Name} kupił {item.Label} od {seller.Name} za {price} fragmentów granitu.", MessageTypeDefOf.PositiveEvent);

            return true;
        }

        // Metoda do znalezienia komórki magazynowej dla przedmiotu
        private static IntVec3 FindStorageCellFor(Thing thing, IntVec3 center, int radius, Map map)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(center, radius, true))
            {
                if (cell.InBounds(map) && cell.Walkable(map) && !cell.Filled(map) &&
                    StoreUtility.IsGoodStoreCell(cell, map, thing, null, map.ParentFaction))
                {
                    return cell;
                }
            }

            return IntVec3.Invalid;
        }
    }

    // Klasa do implementacji okna dialogowego handlu
    public class Dialog_Trade : Window
    {
        private Pawn trader;
        private Pawn partner;
        private ColonistProperty traderProperty;
        private ColonistProperty partnerProperty;
        private List<Thing> traderItems = new List<Thing>();
        private List<Thing> partnerItems = new List<Thing>();
        private Thing selectedItem;
        private int price = 1;

        // Konstruktor
        public Dialog_Trade(Pawn trader, Pawn partner, ColonistProperty traderProperty, ColonistProperty partnerProperty)
        {
            this.trader = trader;
            this.partner = partner;
            this.traderProperty = traderProperty;
            this.partnerProperty = partnerProperty;

            // Pobierz przedmioty obu stron
            this.traderItems = traderProperty.ownedItems.Where(i => i.Spawned).ToList();
            this.partnerItems = partnerProperty.ownedItems.Where(i => i.Spawned).ToList();

            // Ustaw właściwości okna
            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
        }

        // Metoda do rysowania okna
        public override void DoWindowContents(Rect inRect)
        {
            // Rysuj nagłówek
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, 30f);
            Widgets.Label(titleRect, $"Handel między {trader.Name} a {partner.Name}");

            // Rysuj informacje o walucie
            Text.Font = GameFont.Small;
            Rect traderCurrencyRect = new Rect(inRect.x, titleRect.yMax + 5f, inRect.width / 2f, 25f);
            Widgets.Label(traderCurrencyRect, $"{trader.Name}: {traderProperty.graniteCurrency} fragmentów granitu");

            Rect partnerCurrencyRect = new Rect(inRect.x + inRect.width / 2f, titleRect.yMax + 5f, inRect.width / 2f, 25f);
            Widgets.Label(partnerCurrencyRect, $"{partner.Name}: {partnerProperty.graniteCurrency} fragmentów granitu");

            // Rysuj listy przedmiotów
            float listY = traderCurrencyRect.yMax + 10f;
            float listHeight = inRect.height - listY - 70f;

            Rect traderItemsRect = new Rect(inRect.x, listY, inRect.width / 2f - 5f, listHeight);
            Rect partnerItemsRect = new Rect(inRect.x + inRect.width / 2f + 5f, listY, inRect.width / 2f - 5f, listHeight);

            // Rysuj nagłówki list
            Widgets.Label(new Rect(traderItemsRect.x, traderItemsRect.y, traderItemsRect.width, 25f), $"Przedmioty {trader.Name}:");
            Widgets.Label(new Rect(partnerItemsRect.x, partnerItemsRect.y, partnerItemsRect.width, 25f), $"Przedmioty {partner.Name}:");

            // Rysuj listy przedmiotów
            Rect traderItemsListRect = new Rect(traderItemsRect.x, traderItemsRect.y + 25f, traderItemsRect.width, traderItemsRect.height - 25f);
            Rect partnerItemsListRect = new Rect(partnerItemsRect.x, partnerItemsRect.y + 25f, partnerItemsRect.width, partnerItemsRect.height - 25f);

            DrawItemList(traderItemsListRect, traderItems, true);
            DrawItemList(partnerItemsListRect, partnerItems, false);

            // Rysuj kontrolki handlu
            float tradeY = listY + listHeight + 10f;

            // Rysuj pole ceny
            Rect priceRect = new Rect(inRect.x, tradeY, 150f, 30f);
            Widgets.Label(priceRect, "Cena:");

            Rect priceFieldRect = new Rect(priceRect.xMax + 5f, tradeY, 100f, 30f);
            string priceBuffer = price.ToString();
            Widgets.TextFieldNumeric(priceFieldRect, ref price, ref priceBuffer, 1, 1000);

            // Rysuj przycisk handlu
            Rect tradeButtonRect = new Rect(inRect.x + inRect.width - 150f, tradeY, 150f, 30f);

            if (selectedItem != null)
            {
                if (Widgets.ButtonText(tradeButtonRect, "Handluj"))
                {
                    // Określ kupującego i sprzedającego
                    Pawn buyer = traderItems.Contains(selectedItem) ? partner : trader;
                    Pawn seller = traderItems.Contains(selectedItem) ? trader : partner;

                    // Przeprowadź transakcję
                    if (TradeSystem.ExecuteTrade(buyer, seller, selectedItem, price))
                    {
                        // Odśwież listy przedmiotów
                        traderItems = traderProperty.ownedItems.Where(i => i.Spawned).ToList();
                        partnerItems = partnerProperty.ownedItems.Where(i => i.Spawned).ToList();
                        selectedItem = null;
                    }
                }
            }

            // Rysuj informacje o wybranym przedmiocie
            if (selectedItem != null)
            {
                Rect selectedItemRect = new Rect(priceFieldRect.xMax + 10f, tradeY, tradeButtonRect.x - priceFieldRect.xMax - 15f, 30f);
                Widgets.Label(selectedItemRect, $"Wybrany: {selectedItem.Label}");
            }
        }

        // Metoda do rysowania listy przedmiotów
        private void DrawItemList(Rect rect, List<Thing> items, bool isTraderList)
        {
            Widgets.DrawMenuSection(rect);

            if (items.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "Brak przedmiotów");
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float viewHeight = items.Count * 30f;
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, viewHeight);

            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            for (int i = 0; i < items.Count; i++)
            {
                Thing item = items[i];
                Rect itemRect = new Rect(0f, i * 30f, viewRect.width, 30f);

                // Podświetl wybrany przedmiot
                if (selectedItem == item)
                {
                    Widgets.DrawHighlight(itemRect);
                }

                // Rysuj ikonę przedmiotu
                Rect iconRect = new Rect(itemRect.x, itemRect.y, 30f, 30f);
                Widgets.ThingIcon(iconRect, item);

                // Rysuj etykietę przedmiotu
                Rect labelRect = new Rect(iconRect.xMax + 5f, itemRect.y, itemRect.width - iconRect.width - 5f, 30f);
                Widgets.Label(labelRect, item.LabelCap);

                // Obsługa kliknięcia
                if (Widgets.ButtonInvisible(itemRect))
                {
                    selectedItem = item;
                }
            }

            Widgets.EndScrollView();
        }

        private Vector2 scrollPosition = Vector2.zero;
    }

    // Klasa bazowa moda
    public class EntrepreneurMod : Mod
    {
        public EntrepreneurMod(ModContentPack content) : base(content)
        {
            // Inicjalizacja Harmony
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("EntrepreneurMod");
            harmony.PatchAll();

            Log.Message("Entrepreneur System mod initialized");
        }
    }
}