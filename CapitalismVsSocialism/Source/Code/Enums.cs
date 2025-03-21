namespace CapitalismVsSocjalism.Code
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

    // Enum definiujący systemy ekonomiczne
    public enum EconomicSystem
    {
        Capitalism,     // Kapitalizm - jeden przedsiębiorca zatrudnia pracowników
        Socialism,      // Socjalizm - wspólna własność, równe wynagrodzenia
        Communism,      // Komunizm - brak pieniędzy, każdy według potrzeb
        FreeMarket,     // Wolny rynek - każdy jest przedsiębiorcą
        MixedEconomy    // Gospodarka mieszana - elementy kapitalizmu i socjalizmu
    }

    // Enum definiujący poziomy umiejętności
    public enum SkillLevel
    {
        Unskilled = 0,      // Brak umiejętności
        Beginner = 1,       // Początkujący
        Novice = 2,         // Nowicjusz
        Apprentice = 4,     // Praktykant
        Journeyman = 6,     // Czeladnik
        Skilled = 8,        // Wykwalifikowany
        Expert = 10,        // Ekspert
        Master = 12,        // Mistrz
        Legendary = 15      // Legendarny
    }

    // Enum definiujący typy transakcji
    public enum TransactionType
    {
        Purchase,       // Zakup
        Sale,           // Sprzedaż
        Wage,           // Wynagrodzenie
        Tax,            // Podatek
        Investment,     // Inwestycja
        Loan,           // Pożyczka
        Repayment,      // Spłata
        Donation,       // Darowizna
        Theft           // Kradzież
    }

    // Enum definiujący statusy zadań
    public enum TaskStatus
    {
        Pending,        // Oczekujące
        InProgress,     // W trakcie
        Completed,      // Zakończone
        Failed,         // Nieudane
        Cancelled       // Anulowane
    }
}