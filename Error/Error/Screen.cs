using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;

namespace Error
{
    public class Screen
    {
        // Offset is for scrolling
        // TODO: implement MaxOffset, add kinetic scrolling
        public string Name = null;
        public float MaxOffsetX = 0;
        public float MaxOffsetY = 0;
        public float OffsetX = 0;
        public float OffsetY = 0;
        public Dictionary<string, Button> Buttons;

        public Screen(string name)
        {
            Name = name;
            Buttons = new Dictionary<string, Button>();
        }
        public void Add(params Button[] buttons)
        {
            foreach (var btn in buttons)
            {
                Buttons.Add(btn.Name, btn);
            }
        }
        public virtual void Update(GestureSample gesture)
        {
            switch (gesture.GestureType)
            {
                case GestureType.Tap:
                    foreach (var btn in Buttons.Values)
                    {
                        if (btn.Visible && btn.TouchArea.Contains(gesture.Position) && btn.Click != null)
                        {
                            btn.Click();
                            return;
                        }
                    }
                    break;
            }
        }
        public virtual void Draw()
        {
            App.SpriteBatch.Begin();
            foreach (var btn in Buttons.Values) btn.Draw();
            App.SpriteBatch.End();
        }
    }
    public class CollectingScreen : Screen
    {
        public CollectingScreen() : base("Collecting") { }
        public override void Draw()
        {
            App.SpriteBatch.Begin();
            Order order = App.Instance.CollectingData.CurrentOrder;
            if (App.Instance.CollectingData.ShowOrderInfo && order != null)
            {
                App.SpriteBatch.DrawStringCentered(App.Font, order.Customer, new Rectangle(0, 0, 240, 50), Color.Black, 0.5f);
                App.SpriteBatch.DrawStringCentered(App.Font, order.RequestedShippingDate.ToString(), new Rectangle(240, 0, 240, 50), Color.Black, 0.5f);
            }
            OrderLine line = App.Instance.CollectingData.CurrentLine;
            Product product = App.Instance.CollectingData.CurrentProduct;
            if (App.Instance.CollectingData.ShowLineInfo && line != null && product != null)
            {
                App.SpriteBatch.DrawStringCentered(App.Font,
                    "tuote " + (App.Instance.CollectingData.CurrentLineIndex + 1) + "/" + order.Lines.Count,
                    new Rectangle(0, 90, 480, 50), Color.Black, 0.6f);
                App.SpriteBatch.DrawStringCentered(App.Font, product.Description, new Rectangle(40, 100, 400, 100), Color.Black, 1f);
                App.SpriteBatch.DrawStringCentered(App.Font, line.Amount + " kpl, " + line.Amount / product.PackageSize + " pakettia", new Rectangle(40, 200, 400, 100), Color.Black, 1f);
                App.SpriteBatch.DrawStringCentered(App.Font, "Tuotekoodi: " + product.Code, new Rectangle(40, 300, 400, 100), Color.Black, 1f);
                App.SpriteBatch.DrawStringCentered(App.Font, "Hyllypaikka: " + product.ShelfCode, new Rectangle(40, 400, 400, 100), Color.Black, 1f);
            }
            App.SpriteBatch.End();
            base.Draw();
        }
    }
    public class StartScreen : Screen
    {
        public StartScreen() : base("Start") { }
        public override void Draw()
        {
            App.SpriteBatch.Begin();
            App.SpriteBatch.DrawStringCentered(App.Font, "Error", new Rectangle(0, 0, 480, 120), Color.Black, 1f);
            if (App.Message != null)
            {
                App.SpriteBatch.DrawStringCentered(App.Font, App.Message, new Rectangle(50, 200, 380, 60), Color.Black, 0.75f);
            }
            App.SpriteBatch.End();
            base.Draw();
        }
    }
    public class SearchScreen : Screen
    {
        public List<string> SearchResult;

        public SearchScreen() : base("Search") { }
        public override void Draw()
        {
            App.SpriteBatch.Begin(SpriteSortMode.Texture, null, null, null, null, null, Matrix.CreateTranslation(OffsetX, OffsetY, 0));
            var v = new Vector2(10, 100);
            if (SearchResult != null)
            {
                foreach (var textLine in SearchResult)
                {
                    App.SpriteBatch.DrawString(App.Font, textLine, v, Color.DarkSlateGray, 0.6f, 0f);
                    v.Y += 50;
                }
            }
            App.SpriteBatch.End();
            base.Draw();
        }
        public override void Update(GestureSample gesture)
        {
            switch (gesture.GestureType)
            {
                // FreeDragin kanssa ei tarvi olla niin tarkkana suunnan kanssa.
                // Lisäks kun vaan yksi drag-tyyppinen gesture on enabled,
                // niin scrollaaminen ei pätki kun GestureType ei koko ajan vaihtele vertical ja free välillä
                case GestureType.FreeDrag:
                    // move the search screen vertically by the drag delta
                    // amount.
                    OffsetY += gesture.Delta.Y;
                    break;
            }
            base.Update(gesture);
        }
    }
    public class MapScreen : Screen
    {
        public MapScreen() : base("Map") { }
        public override void Draw()
        {
            App.Instance.DrawMapScreen();
            base.Draw();
        }
    }    
}
