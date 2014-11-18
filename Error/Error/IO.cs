using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Error
{
    public static class IO
    {
        static IAsyncResult kbResult;
        static string typedText;

        public static void ShowKeyboard(string title, string description, string defaultText)
        {
            if (!Guide.IsVisible)
            {
                kbResult = Guide.BeginShowKeyboardInput(
                    PlayerIndex.One, title, description, defaultText, GetTypedChars, null);
            }
        }
        public static string GetTypedText()
        {
            GetTypedChars(kbResult);
            return typedText;
        }
        static void GetTypedChars(IAsyncResult r)
        {
            typedText = Guide.EndShowKeyboardInput(r) ?? string.Empty;
        }
        public static string[] ReadAllLines(string filename)
        {
            string text;
            using (Stream stream = TitleContainer.OpenStream(filename))
            {
                using (StreamReader r = new StreamReader(stream))
                {
                    text = r.ReadToEnd();
                }
            }
            return text.Split('\n');
        }
    }
}
