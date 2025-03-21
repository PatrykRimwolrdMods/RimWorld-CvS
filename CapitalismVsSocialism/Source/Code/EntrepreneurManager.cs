using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace CapitalismVsSocjalism.Code
{
    public class EntrepreneurManager : GameComponent
    {
        public List<Entrepreneur> Entrepreneurs { get; private set; } = new List<Entrepreneur>();

        // Konstruktor bezparametrowy wymagany przez GameComponent
        public EntrepreneurManager()
        {
            // Inicjalizacja w konstruktorze
            Entrepreneurs = new List<Entrepreneur>();
        }

        // Konstruktor z parametrem Game
        public EntrepreneurManager(Game game)
        {
            // Inicjalizacja w konstruktorze
            Entrepreneurs = new List<Entrepreneur>();
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();

            // Dodaj wszystkich kolonistów jako przedsiębiorców, jeśli lista jest pusta
            if (Entrepreneurs.Count == 0 && Current.Game != null)
            {
                foreach (Pawn pawn in PawnsFinder.AllMaps_FreeColonists)
                {
                    Entrepreneurs.Add(new Entrepreneur(pawn));
                    Log.Message("Added entrepreneur: " + pawn.Name);
                }
            }
        }

        public void UpdateEntrepreneurs()
        {
            // Dodaj logowanie, aby sprawdzić czy ta metoda jest wywoływana
            Log.Message("UpdateEntrepreneurs called. Entrepreneurs count: " + Entrepreneurs.Count);

            // Sprawdź, czy są nowi koloniści
            CheckForNewColonists();

            // Wywołaj wszystkie metody przydzielające zadania
            AssignHuntingJobs();
            AssignHealingJobs();
            AssignButcheringJobs();
            AssignEatingJobs();
            AssignGraniteCollectionJobs();
            AssignMiningJob();
        }

        public void CheckForNewColonists()
        {
            List<Pawn> allColonists = PawnsFinder.AllMaps_FreeColonists.ToList();

            // Znajdź kolonistów, którzy nie są jeszcze przedsiębiorcami
            foreach (Pawn pawn in allColonists)
            {
                if (!Entrepreneurs.Any(e => e.Pawn == pawn))
                {
                    Entrepreneurs.Add(new Entrepreneur(pawn));
                    Log.Message("Added new entrepreneur: " + pawn.Name);
                }
            }

            // Usuń przedsiębiorców, których koloniści nie istnieją
            Entrepreneurs.RemoveAll(e => e.Pawn == null || e.Pawn.Dead || !allColonists.Contains(e.Pawn));
        }

        public void OnGraniteChunkDestroyed(Thing chunk)
        {
            // Logika obsługi zniszczenia kawałka granitu
            Log.Message("Granite chunk destroyed: " + chunk);
        }

        public void AssignHuntingJobs()
        {
            foreach (var entrepreneur in Entrepreneurs)
            {
                Pawn pawn = entrepreneur.Pawn;
                if (pawn == null || pawn.Dead || pawn.Downed) continue;

                // Sprawdź, czy kolonista może polować
                if (pawn.workSettings == null || !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Hunting)) continue;

                // Znajdź zwierzę do upolowania
                Pawn targetAnimal = FindHuntableAnimal(pawn);
                if (targetAnimal != null)
                {
                    // Upewnij się, że kolonista ma broń
                    EnsureHasWeapon(pawn);

                    // Utwórz i przydziel zadanie polowania
                    Verse.AI.Job huntJob = new Verse.AI.Job(JobDefOf.Hunt, targetAnimal);
                    pawn.jobs.TryTakeOrderedJob(huntJob);

                    entrepreneur.CurrentActivity = "Hunting";
                    Log.Message(pawn.Name + " assigned hunting job for " + targetAnimal.Label);
                }
            }
        }

        private Pawn FindHuntableAnimal(Pawn hunter)
        {
            if (hunter.Map == null) return null;

            // Implementacja znajdowania zwierzęcia do upolowania
            return hunter.Map.mapPawns.AllPawnsSpawned
                .Where(p => p != null && p.RaceProps != null && p.RaceProps.Animal && !p.IsForbidden(hunter) && hunter.CanReserveAndReach(p, PathEndMode.Touch, Danger.Deadly))
                .FirstOrDefault();
        }

        public void EnsureHasWeapon(Pawn pawn)
        {
            if (pawn.equipment == null) return;

            // Sprawdź, czy kolonista ma już broń
            if (pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsRangedWeapon)
                return;

            if (pawn.Map == null) return;

            // Znajdź broń do wzięcia
            Thing weapon = pawn.Map.listerThings.AllThings
                .Where(t => t != null && t.def != null && t.def.IsRangedWeapon && !t.IsForbidden(pawn))
                .FirstOrDefault();

            if (weapon != null)
            {
                // Sprawdź, czy kolonista może zarezerwować broń
                if (weapon.Map.reservationManager.CanReserve(pawn, weapon, 1) && pawn.CanReach(weapon, PathEndMode.Touch, Danger.Deadly))
                {
                    // Utwórz i przydziel zadanie wzięcia broni
                    Verse.AI.Job getWeaponJob = new Verse.AI.Job(JobDefOf.Equip, weapon);
                    pawn.jobs.TryTakeOrderedJob(getWeaponJob);
                    Log.Message(pawn.Name + " assigned job to equip " + weapon.Label);
                }
            }
            else
            {
                // Jeśli nie ma broni palnej, spróbuj znaleźć nóż
                Thing knife = pawn.Map.listerThings.AllThings
                    .Where(t => t != null && t.def != null && t.def.IsMeleeWeapon && !t.IsForbidden(pawn))
                    .FirstOrDefault();

                if (knife != null && knife.Map.reservationManager.CanReserve(pawn, knife, 1) && pawn.CanReach(knife, PathEndMode.Touch, Danger.Deadly))
                {
                    Verse.AI.Job getKnifeJob = new Verse.AI.Job(JobDefOf.Equip, knife);
                    pawn.jobs.TryTakeOrderedJob(getKnifeJob);
                    Log.Message(pawn.Name + " assigned job to equip " + knife.Label);
                }
            }
        }

        public void AssignHealingJobs()
        {
            foreach (var entrepreneur in Entrepreneurs)
            {
                Pawn pawn = entrepreneur.Pawn;
                if (pawn == null || pawn.Dead || pawn.Downed) continue;

                // Sprawdź, czy kolonista może leczyć
                if (pawn.workSettings == null || !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Doctor)) continue;

                if (pawn.Map == null) continue;

                // Znajdź pacjenta do leczenia
                Pawn patient = pawn.Map.mapPawns.AllPawnsSpawned
                    .Where(p => p != null && p.health != null && p.health.HasHediffsNeedingTend() && !p.IsForbidden(pawn) && pawn.CanReserveAndReach(p, PathEndMode.Touch, Danger.Deadly))
                    .FirstOrDefault();

                if (patient != null)
                {
                    // Utwórz i przydziel zadanie leczenia
                    JobDef tendJob = DefDatabase<JobDef>.GetNamed("TendPatient");
                    Verse.AI.Job healJob = new Verse.AI.Job(tendJob, patient);
                    pawn.jobs.TryTakeOrderedJob(healJob);

                    entrepreneur.CurrentActivity = "Healing";
                    Log.Message(pawn.Name + " assigned healing job for " + patient.Label);
                }
            }
        }

        public void AssignButcheringJobs()
        {
            foreach (var entrepreneur in Entrepreneurs)
            {
                Pawn pawn = entrepreneur.Pawn;
                if (pawn == null || pawn.Dead || pawn.Downed) continue;

                // Sprawdź, czy kolonista może gotować
                WorkTypeDef cookingWorkType = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
                if (pawn.workSettings == null || !pawn.workSettings.WorkIsActive(cookingWorkType)) continue;

                if (pawn.Map == null) continue;

                // Znajdź zwłoki do rzeźnictwa
                Thing corpse = pawn.Map.listerThings.AllThings
                    .Where(t => t != null && t.def != null && t.def.defName.Contains("Corpse") && !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly))
                    .FirstOrDefault();

                if (corpse != null)
                {
                    // Znajdź miejsce do rzeźnictwa
                    Building butcherSpot = pawn.Map.listerBuildings.allBuildingsColonist
                        .Where(b => b != null && b.def != null && b.def.defName.Contains("ButcherSpot") && !b.IsForbidden(pawn) && pawn.CanReserveAndReach(b, PathEndMode.Touch, Danger.Deadly))
                        .FirstOrDefault();

                    if (butcherSpot != null)
                    {
                        // Utwórz i przydziel zadanie rzeźnictwa
                        JobDef butcherJobDef = DefDatabase<JobDef>.GetNamed("ButcherCorpse");
                        Verse.AI.Job butcherJob = new Verse.AI.Job(butcherJobDef, corpse, butcherSpot);
                        pawn.jobs.TryTakeOrderedJob(butcherJob);

                        entrepreneur.CurrentActivity = "Butchering";
                        Log.Message(pawn.Name + " assigned butchering job for " + corpse.Label);
                    }
                }
            }
        }

        public void AssignEatingJobs()
        {
            foreach (var entrepreneur in Entrepreneurs)
            {
                Pawn pawn = entrepreneur.Pawn;
                if (pawn == null || pawn.Dead || pawn.Downed) continue;

                // Sprawdź, czy kolonista jest głodny
                if (pawn.needs == null || pawn.needs.food == null || pawn.needs.food.CurLevel > 0.3f) continue;

                if (pawn.Map == null) continue;

                // Znajdź jedzenie
                Thing food = pawn.Map.listerThings.AllThings
                    .Where(t => t != null && t.def != null && t.def.IsIngestible && !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly))
                    .FirstOrDefault();

                if (food != null)
                {
                    // Utwórz i przydziel zadanie jedzenia
                    Verse.AI.Job eatJob = new Verse.AI.Job(JobDefOf.Ingest, food);
                    pawn.jobs.TryTakeOrderedJob(eatJob);

                    entrepreneur.CurrentActivity = "Eating";
                    Log.Message(pawn.Name + " assigned eating job for " + food.Label);
                }
            }
        }

        public void AssignGraniteCollectionJobs()
        {
            foreach (var entrepreneur in Entrepreneurs)
            {
                Pawn pawn = entrepreneur.Pawn;
                if (pawn == null || pawn.Dead || pawn.Downed) continue;

                // Sprawdź, czy kolonista może przenosić rzeczy
                if (pawn.workSettings == null || !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Hauling)) continue;

                if (pawn.Map == null) continue;

                // Znajdź kawałek granitu
                Thing graniteChunk = pawn.Map.listerThings.AllThings
                    .Where(t => t != null && t.def != null && t.def.defName == "ChunkGranite" && !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly))
                    .FirstOrDefault();

                if (graniteChunk != null)
                {
                    // Znajdź miejsce do składowania
                    if (pawn.Map.areaManager == null || pawn.Map.areaManager.Home == null) continue;

                    IntVec3 storageCell = pawn.Map.areaManager.Home.ActiveCells
                        .Where(c => pawn.CanReserveAndReach(c, PathEndMode.Touch, Danger.Deadly))
                        .FirstOrDefault();

                    if (storageCell != default(IntVec3))
                    {
                        // Utwórz i przydziel zadanie przeniesienia
                        Verse.AI.Job haulJob = new Verse.AI.Job(JobDefOf.HaulToCell, graniteChunk, storageCell);
                        pawn.jobs.TryTakeOrderedJob(haulJob);

                        entrepreneur.CurrentActivity = "Hauling Granite";
                        Log.Message(pawn.Name + " assigned hauling job for " + graniteChunk.Label);
                    }
                }
            }
        }

        public void AssignMiningJob()
        {
            foreach (var entrepreneur in Entrepreneurs)
            {
                Pawn pawn = entrepreneur.Pawn;
                if (pawn == null || pawn.Dead || pawn.Downed) continue;

                // Sprawdź, czy kolonista może kopać
                if (pawn.workSettings == null || !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Mining)) continue;

                if (pawn.Map == null) continue;

                // Znajdź złoże do kopania
                Thing deposit = pawn.Map.listerThings.AllThings
                    .Where(t => t != null && t.def != null && t.def.mineable && !t.IsForbidden(pawn) && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly))
                    .FirstOrDefault();

                if (deposit != null)
                {
                    // Utwórz i przydziel zadanie kopania
                    Verse.AI.Job mineJob = new Verse.AI.Job(JobDefOf.Mine, deposit);
                    pawn.jobs.TryTakeOrderedJob(mineJob);

                    entrepreneur.CurrentActivity = "Mining";
                    Log.Message(pawn.Name + " assigned mining job for " + deposit.Label);
                }
            }
        }
    }

    // Klasa Entrepreneur
    public class Entrepreneur
    {
        public Pawn Pawn { get; set; }
        public float Profit { get; set; }
        public string CurrentActivity { get; set; }

        public Entrepreneur()
        {
            Profit = 0f;
            CurrentActivity = "Idle";
        }

        public Entrepreneur(Pawn pawn)
        {
            Pawn = pawn;
            Profit = 0f;
            CurrentActivity = "Idle";
        }
    }
}