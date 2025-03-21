using RimWorld;
using System.Collections.Generic;
using Verse;

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
}