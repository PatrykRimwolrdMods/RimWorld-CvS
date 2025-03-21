using Verse;

namespace CapitalismVsSocjalism
{
    public class EntrepreneurEvents : GameComponent
    {
        private CapitalismVsSocjalism.Code.EntrepreneurManager entrepreneurManager;

        // Konstruktor bezparametrowy wymagany przez GameComponent
        public EntrepreneurEvents()
        {
            // Inicjalizacja w konstruktorze
        }

        // Konstruktor z parametrem Game
        public EntrepreneurEvents(Game game)
        {
            // Inicjalizacja w konstruktorze
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            entrepreneurManager = Current.Game.GetComponent<CapitalismVsSocjalism.Code.EntrepreneurManager>();

            if (entrepreneurManager == null)
            {
                Log.Error("EntrepreneurManager not found in game components!");
            }
            else
            {
                Log.Message("EntrepreneurEvents initialized with EntrepreneurManager");
            }
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            // Wywołuj UpdateEntrepreneurs co 250 ticków (około 4 sekundy)
            if (Find.TickManager.TicksGame % 250 == 0)
            {
                if (entrepreneurManager != null)
                {
                    entrepreneurManager.UpdateEntrepreneurs();
                }
            }
        }
    }
}