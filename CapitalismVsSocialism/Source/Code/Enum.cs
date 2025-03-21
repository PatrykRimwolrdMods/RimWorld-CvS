namespace EntrepreneurMod
{
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
}