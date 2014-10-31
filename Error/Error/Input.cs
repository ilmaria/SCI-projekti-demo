using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;

namespace Error
{
    public static class Input
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
    }
}
