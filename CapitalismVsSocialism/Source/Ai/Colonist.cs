using System;

namespace CapitalismVsSocialism.AI
{
    public class Colonist
    {
        public string Name { get; private set; }
        public int Hunger { get; private set; }
        public int Energy { get; private set; }
        public int Happiness { get; private set; }
        public string CurrentTask { get; private set; }

        public Colonist(string name)
        {
            Name = name;
            Hunger = 50;
            Energy = 50;
            Happiness = 50;
            CurrentTask = "Idle";
        }

        public void AdjustHunger(int amount)
        {
            Hunger = Clamp(Hunger + amount, 0, 100);
        }

        public void AdjustEnergy(int amount)
        {
            Energy = Clamp(Energy + amount, 0, 100);
        }

        public void AdjustHappiness(int amount)
        {
            Happiness = Clamp(Happiness + amount, 0, 100);
        }

        public void SetTask(string task)
        {
            CurrentTask = task;
        }

        public override string ToString()
        {
            return $"{Name}: Hunger({Hunger}) Energy({Energy}) Happiness({Happiness}) Task({CurrentTask})";
        }

        private int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}