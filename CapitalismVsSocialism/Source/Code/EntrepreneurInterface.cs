using RimWorld;
using UnityEngine;
using Verse;

namespace CapitalismVsSocjalism
{
    public class EntrepreneurInterface : MainTabWindow
    {
        private Vector2 scrollPosition = Vector2.zero;
        private float viewHeight = 0f;

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 40f), "Entrepreneurs");
            Text.Font = GameFont.Small;

            Rect outRect = new Rect(0f, 50f, inRect.width, inRect.height - 50f);
            Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

            // Pobierz instancję EntrepreneurManager
            CapitalismVsSocjalism.Code.EntrepreneurManager entrepreneurManager = Current.Game.GetComponent<CapitalismVsSocjalism.Code.EntrepreneurManager>();

            if (entrepreneurManager != null)
            {
                Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
                float curY = 0f;

                foreach (var entrepreneur in entrepreneurManager.Entrepreneurs)
                {
                    Rect rowRect = new Rect(0f, curY, viewRect.width, 30f);
                    if (Mouse.IsOver(rowRect))
                    {
                        Widgets.DrawHighlight(rowRect);
                    }
                    Widgets.DrawBox(rowRect);

                    Rect nameRect = new Rect(10f, curY, 200f, 30f);
                    Widgets.Label(nameRect, entrepreneur.Pawn.Name.ToStringFull);

                    Rect profitRect = new Rect(220f, curY, 100f, 30f);
                    Widgets.Label(profitRect, "Profit: " + entrepreneur.Profit.ToString("F0"));

                    Rect activityRect = new Rect(330f, curY, 200f, 30f);
                    Widgets.Label(activityRect, "Activity: " + entrepreneur.CurrentActivity);

                    curY += 35f;
                }

                viewHeight = curY;
                Widgets.EndScrollView();
            }
            else
            {
                Widgets.Label(new Rect(0f, 50f, inRect.width, 30f), "EntrepreneurManager not found!");
            }
        }
    }
}