using System;
using System.Collections.Generic;

namespace CapitalismVsSocialism.AI
{
    public class ColonistAI
    {
        private List<Colonist> colonists;

        public ColonistAI()
        {
            colonists = new List<Colonist>();
        }

        public void AddColonist(Colonist colonist)
        {
            colonists.Add(colonist);
        }

        // Dodajemy metodę GetColonists, aby umożliwić dostęp do listy kolonistów
        public List<Colonist> GetColonists()
        {
            return colonists;
        }

        public void UpdateAI()
        {
            foreach (var colonist in colonists)
            {
                if (colonist.Hunger > 80)
                {
                    colonist.SetTask("Eating");
                    colonist.AdjustHunger(-20);
                }
                else if (colonist.Energy < 20)
                {
                    colonist.SetTask("Sleeping");
                    colonist.AdjustEnergy(+30);
                }
                else if (colonist.Happiness < 30)
                {
                    colonist.SetTask("Relaxing");
                    colonist.AdjustHappiness(+20);
                }
                else
                {
                    colonist.SetTask("Working");
                    colonist.AdjustHunger(+5);
                    colonist.AdjustEnergy(-5);
                }

                Console.WriteLine(colonist.ToString());
            }
        }
    }
}