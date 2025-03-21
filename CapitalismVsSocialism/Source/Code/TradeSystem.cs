using RimWorld;
using Verse;

namespace EntrepreneurMod
{
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
}