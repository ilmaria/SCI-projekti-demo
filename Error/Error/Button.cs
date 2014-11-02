using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Error
{
    public class Button
    {
        public string Name;
        public Rectangle TouchArea;
        public string Text = string.Empty;
        public Texture2D Icon;
        public float TextScale = 1f;// ei käytössä
        public Color TextColor = Color.DarkSlateGray;
        public Color BackgroundColor = Color.WhiteSmoke;
        public ClickDelegate Click;
        public bool Visible = true;

        public delegate void ClickDelegate();
        public void Draw()
        {
            if (!Visible) return;
            if (Icon == null)
            {
                App.SpriteBatch.Draw(App.Pixel, TouchArea, TextColor);
                var bkg = TouchArea;
                bkg.Inflate(-2, -2);
                App.SpriteBatch.Draw(App.Pixel, bkg, BackgroundColor);
                App.SpriteBatch.DrawStringCentered(App.Font, Text, TouchArea, TextColor, 1f);
            }
            else
            {
                var touchArea = TouchArea;
                var rect = touchArea;
                rect.X += 25;
                rect.Y += 10;
                rect.Width -= 50;
                rect.Height -= 50;
                var textrect = new Rectangle(touchArea.X, touchArea.Bottom - 40, touchArea.Width, 40);
                App.SpriteBatch.Draw(App.Pixel, touchArea, TextColor);
                touchArea.Inflate(-1, -2);
                App.SpriteBatch.Draw(App.Pixel, touchArea, BackgroundColor);
                App.SpriteBatch.Draw(Icon, rect, TextColor);
                App.SpriteBatch.DrawStringCentered(App.Font, Text, textrect, TextColor, 0.62f);
            }
        }
    }
}
