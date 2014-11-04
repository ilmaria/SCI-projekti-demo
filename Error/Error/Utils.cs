using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Error
{
    public static class Utils
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            T tmp = a;
            a = b;
            b = tmp;
        }
        public static T[][] GetPermutations<T>(T[] items)
        {
            int n = Factorial(items.Length);
            T[][] permutations = new T[n][];
            for (int p = 0; p < n; p++)
            {
                permutations[p] = new T[items.Length];
                Array.Copy(items, permutations[p], items.Length);

                // no idea why this works...
                int k = p + 1;
                for (int j = 2; j <= items.Length; j++)
                {
                    Swap<T>(ref permutations[p][j - 1], ref permutations[p][k % j]);
                    k = k / j + 1;
                }
            }
            return permutations;
        }
        public static int Factorial(int x)
        {
            if (x <= 1) return 1;
            int factorial = 1;
            while (x > 1)
            {
                factorial *= x;
                x--;
            }
            return factorial;
        }
        public static bool AreSimilar(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Contains(b)) return true;
            if (b.Contains(a)) return true;
            return false;
        }
        // how many chars need to change make the strings match
        public static int LevenshteinDistance(string source, string target)
        {
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            int distance = 0;
            if (source[source.Length - 1] != target[target.Length - 1]) distance = 1;

            return Math.Min(Math.Min(LevenshteinDistance(source.Substring(0, source.Length - 1), target) + 1,
                                     LevenshteinDistance(source, target.Substring(0, target.Length - 1))) + 1,
                                     LevenshteinDistance(source.Substring(0, source.Length - 1), target.Substring(0, target.Length - 1)) + distance);
        }
        // minimize this
        public static float LevenshteinScore(string source, string target)
        {
            if (source == null || target == null || target.Length == 0 || source.Length == 0)
                return 100f;
            return (float)LevenshteinDistance(source, target) / Math.Max(source.Length, target.Length);
        }
        public struct TFloat<T> : IComparable<TFloat<T>>
        {
            public T Value;
            public float Float;

            int IComparable<TFloat<T>>.CompareTo(TFloat<T> other)
            {
                return Float.CompareTo(other.Float);
            }
        }
    }
}
