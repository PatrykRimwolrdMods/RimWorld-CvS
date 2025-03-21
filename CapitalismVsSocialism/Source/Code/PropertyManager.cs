using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace EntrepreneurMod
{
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
}