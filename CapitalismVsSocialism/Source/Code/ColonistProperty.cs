using System.Collections.Generic;
using Verse;

namespace EntrepreneurMod
{
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
}