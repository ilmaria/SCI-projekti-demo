using System;

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
    }
}
