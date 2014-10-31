using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace Error
{
    public static class XnaExtensions
    {
        public static bool Contains(this Rectangle rectangle, Vector2 point)
        {
            return rectangle.Contains(new Point((int)point.X, (int)point.Y));
        }
        public static bool Contains(this Rectangle rectangle, Vector2 point, int inflateAmount)
        {
            rectangle.Inflate(inflateAmount, inflateAmount);
            return rectangle.Contains(new Point((int)point.X, (int)point.Y));
        }

        public static Vector4 ToVector4(this Vector3 v, float w)
        {
            return new Vector4(v.X, v.Y, v.Z, w);
        }
        public static Vector3 ToVector3(this Vector2 v, float z)
        {
            return new Vector3(v.X, v.Y, z);
        }
        public static float[] ToArray(this Vector4 v)
        {
            return new float[] { v.X, v.Y, v.Z, v.W };
        }

        public static Vector3 Add(this Vector3 v3, Vector2 v2)
        {
            return new Vector3(v3.X + v2.X, v3.Y + v2.Y, v3.Z);
        }

        public static Vector2 XY(this Vector3 v)
        {
            return new Vector2(v.X, v.Y);
        }
        public static Vector2 XY(this Vector4 v)
        {
            return new Vector2(v.X, v.Y);
        }
        public static Vector3 XYZ(this Vector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static bool IsNaN(this Vector2 v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y);
        }
        public static bool IsNaN(this Vector3 v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z);
        }
        public static bool IsNaN(this Vector4 v)
        {
            return float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z) || float.IsNaN(v.W);
        }

        public static Rectangle Add(this Rectangle r, Point p)
        {
            r.X += p.X;
            r.Y += p.Y;
            return r;
        }

        public static void Draw(this SpriteBatch s, Texture2D texture, Rectangle destinationRectangle, Rectangle sourceRectangle, Color color, float depth)
        {
            s.Draw(texture, destinationRectangle, sourceRectangle, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
        }
        public static void Draw(this SpriteBatch s, Texture2D texture, Rectangle destinationRectangle, Color color, float depth)
        {
            s.Draw(texture, destinationRectangle, null, color, 0f, Vector2.Zero, SpriteEffects.None, depth);
        }
        public static void DrawString(this SpriteBatch s, SpriteFont spriteFont, string text, Vector2 position, Color color, float scale, float depth)
        {
            s.DrawString(spriteFont, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, depth);
        }
        /// <summary>
        /// Draws text horizontally and vertically centered in Rectangle rect
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="font"></param>
        /// <param name="text"></param>
        /// <param name="rect"></param>
        /// <param name="color"></param>
        /// <param name="scale"></param>
        public static void DrawStringCentered(this SpriteBatch sb, SpriteFont font, string text, Rectangle rect, Color color, float scale)
        {
            if (text == null) return;
            Vector2 textSize = (font.MeasureString(text)) * scale;
            Vector2 position = new Vector2(rect.X, rect.Y) + new Vector2(rect.Width - textSize.X, rect.Height - textSize.Y) * 0.5f;
            sb.DrawString(font, text, position, color, scale, 0f);
        }

        public static List<string> GetParts(this string str)
        {
            List<string> result = new List<string>();
            string[] splitted = str.Split(' ', '\t');
            foreach (var s in splitted)
            {
                string b = s.Trim(' ', '\t', '\n', '\r');
                if (b.Length > 0) result.Add(b);
            }
            return result;
        }

        public static BoundingBox Transform(this BoundingBox box, Matrix matrix)
        {
            box.Max = Vector3.Transform(box.Max, matrix);
            box.Min = Vector3.Transform(box.Min, matrix);
            return box;
        }
        public static Vector3 Center(this BoundingBox box)
        {
            return (box.Max + box.Min) * 0.5f;
        }
    }
}
