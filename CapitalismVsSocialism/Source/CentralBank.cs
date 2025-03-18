using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CentralBank : GameComponent
{
    public int totalBerries = 0;
    public int totalSilver = 0;
    public float interestRate = 0.05f;
    public float taxRate = 0.10f;
    public Dictionary<Pawn, int> loans = new Dictionary<Pawn, int>();
    public Zone_Stockpile bankStockpile;
    public Dictionary<Pawn, int> contracts = new Dictionary<Pawn, int>();

    public CentralBank(Game game) { }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref totalBerries, "totalBerries", 0);
        Scribe_Values.Look(ref totalSilver, "totalSilver", 0);
        Scribe_Values.Look(ref interestRate, "interestRate", 0.05f);
        Scribe_Values.Look(ref taxRate, "taxRate", 0.10f);
        Scribe_Collections.Look(ref loans, "loans", LookMode.Reference, LookMode.Value);
        Scribe_Collections.Look(ref contracts, "contracts", LookMode.Reference, LookMode.Value);
        Scribe_References.Look(ref bankStockpile, "bankStockpile");
    }

    public void SetBankStockpile(Zone_Stockpile stockpile)
    {
        bankStockpile = stockpile;
        Messages.Message("Bank centralny przypisany do nowej strefy!", MessageTypeDefOf.PositiveEvent);
    }

    public void OpenBankUI()
    {
        Find.WindowStack.Add(new Dialog_BankUI(this));
    }

    public void SetTaxRate(float newRate)
    {
        taxRate = newRate;
        Messages.Message($"Nowa stopa podatkowa: {newRate * 100}%", MessageTypeDefOf.NeutralEvent);
    }

    public void CollectTaxes(List<Pawn> colonists)
    {
        foreach (var colonist in colonists)
        {
            int taxAmount = (int)(GetWealth(colonist) * taxRate);

            if (colonist.inventory.innerContainer.Contains(ThingDef.Named("Silver"), taxAmount))
            {
                RemoveItemsFromInventory(colonist, ThingDef.Named("Silver"), taxAmount);
                totalSilver += taxAmount;
                Messages.Message($"{colonist.Name} zapłacił {taxAmount} srebra!", MessageTypeDefOf.PositiveEvent);
            }
            else if (colonist.inventory.innerContainer.Contains(ThingDef.Named("Berries"), taxAmount))
            {
                RemoveItemsFromInventory(colonist, ThingDef.Named("Berries"), taxAmount);
                totalBerries += taxAmount;
                Messages.Message($"{colonist.Name} zapłacił {taxAmount} jagód!", MessageTypeDefOf.PositiveEvent);
            }
        }
    }

    private void RemoveItemsFromInventory(Pawn colonist, ThingDef currencyDef, int amount)
    {
        int remaining = amount;
        List<Thing> items = colonist.inventory.innerContainer.Where(t => t.def == currencyDef).ToList();

        foreach (Thing item in items)
        {
            if (item.stackCount <= remaining)
            {
                remaining -= item.stackCount;
                colonist.inventory.innerContainer.Remove(item);
                if (remaining <= 0) break;
            }
            else
            {
                item.SplitOff(remaining);
                break;
            }
        }
    }

    private int GetWealth(Pawn colonist)
    {
        int wealth = 0;
        wealth += colonist.inventory.innerContainer.TotalStackCountOfDef(ThingDef.Named("Silver"));
        wealth += colonist.inventory.innerContainer.TotalStackCountOfDef(ThingDef.Named("Berries"));
        return wealth;
    }
}

public class Dialog_BankUI : Window
{
    private CentralBank bank;

    public Dialog_BankUI(CentralBank bank)
    {
        this.bank = bank;
        doCloseX = true;
        closeOnClickedOutside = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.Label($"Srebro w banku: {bank.totalSilver}");
        listing.Label($"Jagody w banku: {bank.totalBerries}");
        listing.Label($"Podatek: {bank.taxRate * 100}%");

        if (listing.ButtonText("Zmień podatek"))
        {
            bank.SetTaxRate(bank.taxRate + 0.01f);
        }
        if (listing.ButtonText("Zamknij"))
        {
            this.Close();
        }

        listing.End();
    }
}
