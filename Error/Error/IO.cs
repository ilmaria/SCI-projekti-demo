using System;
using System.IO;
using System.IO.IsolatedStorage;
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
        public static void SaveText(string fileName, string[] lines)
        {
            try
            {
                IsolatedStorageFile storageFile = IsolatedStorageFile.GetUserStoreForApplication();
                IsolatedStorageFileStream fs = storageFile.OpenFile(fileName, System.IO.FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);

                sw.WriteLine(lines.Length.ToString());
                foreach (string line in lines)
                {
                    sw.WriteLine(line);
                }

                sw.Close();
                fs.Close();

            }
            catch { }
        }
        public static string[] LoadText(string fileName)
        {
            try
            {
                IsolatedStorageFile storageFile = IsolatedStorageFile.GetUserStoreForApplication();
                if (storageFile.FileExists(fileName))
                {

                    IsolatedStorageFileStream fs = storageFile.OpenFile(fileName, System.IO.FileMode.Open);
                    StreamReader sr = new StreamReader(fs);

                    int len = int.Parse(sr.ReadLine());
                    string[] lines = new string[len];
                    for (int i = 0; i < len; i++)
                    {
                        lines[i] = sr.ReadLine();
                    }

                    sr.Close();
                    fs.Close();
                    return lines;
                }
            }
            catch { }
            return null;
        }
    }
}
