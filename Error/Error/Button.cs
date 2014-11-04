using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Error
{
    public class ClickableElement
    {
        public string Name;
        public Rectangle TouchArea;
        public bool Visible = true;
        public bool IsFixedPosition = true;
        public ClickDelegate Click;
        public delegate void ClickDelegate();
        public virtual void Draw(int offsetX, int offsetY) { }
    }
    public class Button : ClickableElement
    {
        public string Text = string.Empty;
        public Texture2D Icon;
        public override void Draw(int offsetX, int offsetY)
        {
            if (!Visible) return;

            Rectangle r = TouchArea;
            if (!IsFixedPosition)
            {
                r.X += offsetX;
                r.Y += offsetY;
            }

            App.SpriteBatch.Draw(App.Pixel, r, UI.BackgroundColor);
            UI.DrawBorders(r, UI.ThickLineWidth);

            if (Icon == null)
            {
                App.SpriteBatch.DrawStringCentered(App.Font, Text, r, UI.ForegroundColor, UI.LargeTextScale);
            }
            else
            {
                var rect = r;
                rect.X += 25;
                rect.Y += 10;
                rect.Width -= 50;
                rect.Height -= 50;
                var textrect = new Rectangle(r.X, r.Bottom - 40, TouchArea.Width, 40);
                App.SpriteBatch.Draw(Icon, rect, UI.ForegroundColor);
                App.SpriteBatch.DrawStringCentered(App.Font, Text, textrect, UI.ForegroundColor, UI.SmallTextScale);
            }
        }
    }
    public class ListItem : ClickableElement
    {
        public int Index;
        public string Title;
        public List<string> Lines;

        public static int Width = 450;
        public static int TitleBoxHeight = 60;
        public static int LineBoxHeight = 40;

        public ListItem(int index, Point offset, string title, List<string> lines, ClickDelegate click, string name, bool isFixedPosition)
        {
            Index = index;
            Title = title;
            Lines = lines;
            Click = click;
            TouchArea = new Rectangle(offset.X + 10, offset.Y, Width, TitleBoxHeight + Lines.Count * LineBoxHeight);
            Name = name;
            IsFixedPosition = isFixedPosition;
        }
        public override void Draw(int offsetX, int offsetY)
        {
            Rectangle r = TouchArea;
            if (!IsFixedPosition)
            {
                r.X += offsetX;
                r.Y += offsetY;
            }

            App.SpriteBatch.Draw(App.Pixel, r, UI.BackgroundColor);
            UI.DrawBorders(r, UI.ThickLineWidth);
            App.SpriteBatch.DrawStringCentered(App.Font, Index.ToString(), 
                new Rectangle(r.X, r.Y, 60, r.Height), UI.ForegroundColor, UI.LargeTextScale);
            App.SpriteBatch.DrawStringCentered(App.Font, Title,
                new Rectangle(r.X + 60, r.Y, r.Width - 60, TitleBoxHeight), UI.ForegroundColor, UI.NormalTextScale);
            App.SpriteBatch.Draw(App.Pixel, new Rectangle(r.X + 60, r.Y, UI.ThinLineWidth, r.Height), UI.ForegroundColor);
            Rectangle lineBox = new Rectangle(r.X + 60, r.Y + TitleBoxHeight, r.Width - 60, LineBoxHeight);
            foreach (string line in Lines)
            {
                App.SpriteBatch.DrawStringCentered(App.Font, line, lineBox, UI.ForegroundColor, UI.SmallTextScale);
                App.SpriteBatch.Draw(App.Pixel, new Rectangle(lineBox.X, lineBox.Y, lineBox.Width, UI.ThinLineWidth), UI.ForegroundColor);
                lineBox.Y += LineBoxHeight;
            }
        }
    }

    internal static class UI
    {
        internal static float HugeTextScale = 1.5f;
        internal static float LargeTextScale = 1f;
        internal static float NormalTextScale = 0.8f;
        internal static float SmallTextScale = 0.62f;
        internal static Color ForegroundColor = Color.DarkSlateGray;
        internal static Color BackgroundColor = Color.WhiteSmoke;
        internal static int ThinLineWidth = 1;
        internal static int ThickLineWidth = 2;

        /// <summary>
        /// Draw borders of rectangle r with ForegroundColor. Width of lines is w
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="r"></param>
        /// <param name="w"></param>
        /// <param name="c"></param>
        public static void DrawBorders(Rectangle rect, int w)
        {
            int hw = w / 2;

            // top
            Rectangle r = new Rectangle(rect.Left - hw, rect.Top - hw, rect.Width + w, w);
            App.SpriteBatch.Draw(App.Pixel, r, ForegroundColor);

            // bottom
            r = new Rectangle(rect.Left - hw, rect.Bottom - hw, rect.Width + w, w);
            App.SpriteBatch.Draw(App.Pixel, r, ForegroundColor);

            // left
            r = new Rectangle(rect.Left - hw, rect.Top - hw, w, rect.Height + w);
            App.SpriteBatch.Draw(App.Pixel, r, ForegroundColor);

            // right
            r = new Rectangle(rect.Right - hw, rect.Top - hw, w, rect.Height + w);
            App.SpriteBatch.Draw(App.Pixel, r, ForegroundColor);
        }
    }
}
