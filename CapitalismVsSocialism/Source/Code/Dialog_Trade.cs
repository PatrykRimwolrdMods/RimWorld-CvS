using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace EntrepreneurMod
{
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
        private Vector2 scrollPosition = Vector2.zero;

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
    }
}