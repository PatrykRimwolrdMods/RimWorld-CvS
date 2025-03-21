using Verse;

namespace CapitalismVsSocjalism
{
    public class CapitalismVsSocjalismMod : Mod
    {
        public CapitalismVsSocjalismMod(ModContentPack content) : base(content)
        {
            // Inicjalizacja moda
            Log.Message("CapitalismVsSocjalism mod loaded");
        }
    }

    // Klasa inicjalizująca komponenty gry
    [StaticConstructorOnStartup]
    public static class CapitalismVsSocjalismGameComponentInitializer
    {
        static CapitalismVsSocjalismGameComponentInitializer()
        {
            Log.Message("CapitalismVsSocjalism mod initialized");
        }
    }
}