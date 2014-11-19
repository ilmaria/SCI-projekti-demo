using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Linq;

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
        //// how many chars need to change make the strings match
        //public static int LevenshteinDistance(string source, string target)
        //{
        //    if (source.Length == 0) return target.Length;
        //    if (target.Length == 0) return source.Length;

        //    int distance = 0;
        //    if (source[source.Length - 1] != target[target.Length - 1]) distance = 1;

        //    return Math.Min(Math.Min(LevenshteinDistance(source.Substring(0, source.Length - 1), target) + 1,
        //                             LevenshteinDistance(source, target.Substring(0, target.Length - 1))) + 1,
        //                             LevenshteinDistance(source.Substring(0, source.Length - 1), target.Substring(0, target.Length - 1)) + distance);
        //}
        // minimize this
        public static float LevenshteinScore(string source, string target)
        {
            if (source == null || target == null || target.Length == 0 || source.Length == 0)
                return 100f;
            return (float)levenshtein(source, target) / Math.Max(source.Length, target.Length);
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
        public static T Combine<T>(params Array[] arrays)
        {
            Array rslt = Array.CreateInstance(typeof(T).GetElementType(), arrays.Sum(x => x.Length));
            int offset = 0;
            foreach (Array arr in arrays)
            {
                Array.Copy(arr, 0, rslt, offset, arr.Length);
                offset += arr.Length;
            }
            return (T)(object)rslt;
        }
        /// <summary>
        /// Compute the distance between two strings (the parameters).
        /// </summary>
        /// <param name="s1">The first of the two strings you want to compare.</param>
        /// <param name="s2">The second of the two strings you want to compare.</param>
        /// <returns>The Levenshtein Distance (higher is a bigger difference).</returns>
        public static int levenshtein(string s1, string s2)
        {
            if (s1 == s2) return 0;
            if (s1.Length == 0) return s2.Length;
            if (s2.Length == 0) return s1.Length;

            int n1 = s1.Length + 1;
            int n2 = s2.Length + 1;
            int I = 0, i = 0, c, j, J;
            int[,] d = new int[n1, n2]; // allokoinnit voisi v‰ltt‰‰ static int[,] d = new int[100,100]

            while (i < n2) { d[0, i] = i++; }

            i = 0;
            while (++i < n1)
            {
                J = j = 0;
                c = s1[I];
                d[i, 0] = i;
                while (++j < n2)
                {
                    d[i, j] = Math.Min(Math.Min(d[I, j] + 1, d[i, J] + 1), d[I, J] + (c == s2[J] ? 0 : 1));
                    ++J;
                }
                ++I;
            }
            return d[n1 - 1, n2 - 1];
        }
    }
}
