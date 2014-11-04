using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input.Touch;

namespace Error
{
    public class Screen
    {
        /* OHJE:
         * Jokaisessa ruudussa, jossa halutaan scrollata pit‰‰ m‰‰ritt‰‰ toi
         * MaxOffset. N‰iden arvoksi tulee sitten ruudun korkeus + niin monta pikseli‰
         * kuin haluaa pysty‰ kelaamaan ruutua alas.
         */
        public string Name = null;
        public float MaxOffset = 0;
        public float Offset = 0;
        public float Velocity = 0;
        public Dictionary<string, ClickableElement> ClickableElements;
        static int scrollbarWidth = 5;
        Rectangle scrollbarArea = new Rectangle(App.screenWidth - scrollbarWidth, 0, scrollbarWidth, 10);
        public int Height = App.screenHeight; // muutetaan todelliseen arvoon
        public bool IsScrollable = false;

        public Screen(string name)
        {
            Name = name;
            ClickableElements = new Dictionary<string, ClickableElement>();
        }
        public void Add(params ClickableElement[] elements)
        {
            foreach (var elem in elements)
            {
                if(ClickableElements.ContainsKey(elem.Name))
                {
                    ClickableElements.Remove(elem.Name);
                }
                ClickableElements.Add(elem.Name, elem);
            }
        }
        public void Update(float elapsedSeconds)
        {
            if (IsScrollable)
            {
                MaxOffset = (Height > App.screenHeight) ? Height - App.screenHeight : 0;
                Offset += Velocity * elapsedSeconds;
                // est‰‰ scrollauksen ruudun yl‰puolelle
                if (Offset > 0)
                {
                    Offset = 0;
                }
                // est‰‰ scrollauksen liian alas
                else if (Offset < -MaxOffset)
                {
                    Offset = -MaxOffset;
                }
                if (Offset == 0 || Offset == -MaxOffset)
                {
                    Velocity = 0;
                }
                // v‰hennet‰‰n nopeutta pikkuhiljaa (kitkaefekti)
                if (Velocity > 0)
                {
                    Velocity -= 100;    // kitkan suuruus
                    if (Velocity < 0)
                    {
                        Velocity = 0;
                    }
                }
                else if (Velocity < 0)
                {
                    Velocity += 100;
                    if (Velocity > 0)
                    {
                        Velocity = 0;
                    }
                }
            }
            else
                Offset = 0;
        }
        public virtual void ProcessInput(GestureSample gesture)
        {
            switch (gesture.GestureType)
            {
                case GestureType.Tap:

                    // pys‰yt‰ scrollaus
                    //TODO: pys‰ytys myˆs kosketuksella ilman sormen ylˆs nostamista
                    Velocity = 0;

                    // check if any of clickable elements is hit
                    foreach (var elem in ClickableElements.Values)
                    {
                        if (elem.Visible && elem.Click != null)
                        {
                            if (elem.IsFixedPosition && elem.TouchArea.Contains(gesture.Position))
                            {
                                elem.Click();
                                return;
                            }
                            if (!elem.IsFixedPosition && elem.TouchArea.CreateOffset(0, (int)Offset).Contains(gesture.Position))
                            {
                                elem.Click();
                                return;
                            }
                        }
                    }
                    break;
                case GestureType.FreeDrag:
                    // move the search screen vertically by the drag delta
                    // amount.
                    if (IsScrollable)
                    {
                        Offset += gesture.Delta.Y;
                    }
                    break;
                case GestureType.Flick:
                    // add velocity to screen (only interested in changes to Y velocity).
                    if (IsScrollable)
                    {
                        Velocity += gesture.Delta.Y;
                    }
                    break;
            }
        }
        public virtual void Draw()
        {
            App.SpriteBatch.Begin();
            // piirret‰‰n scrollbar jos on scrollattu
            if (IsScrollable)
            {
                // optimointi puuttuu: scrollbarSize muuttuu vain ruutua vaihtaessa tai uutta hakua tehdess‰
                int scrollbarSize = (int) (App.screenHeight * App.screenHeight / (App.screenHeight + MaxOffset));
                int scrollbarPos = (int)(Offset / MaxOffset * (App.screenHeight - scrollbarSize));
                scrollbarArea.Height = scrollbarSize;
                scrollbarArea.Y = - scrollbarPos;

                App.SpriteBatch.Draw(App.Pixel, scrollbarArea, UI.ForegroundColor);
            }
            // offset kokonaislukuna koska ohuet viivat j‰‰ piirt‰m‰tt‰ jos ei justiinsa pikselin kohdalla
            foreach (var elem in ClickableElements.Values)
            {
                if (elem.Visible && !elem.IsFixedPosition) elem.Draw(0, (int)Offset);
            }
            foreach (var elem in ClickableElements.Values)
            {
                if (elem.Visible && elem.IsFixedPosition) elem.Draw(0, 0);
            }
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
            Product product = App.Instance.Storage.GetProduct(App.Instance.CollectingData.CurrentProductKey);
            if (App.Instance.CollectingData.ShowLineInfo && line != null)
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
        public StartScreen() : base("Start") {}
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
