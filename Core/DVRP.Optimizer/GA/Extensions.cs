using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer.GA
{
    public static class Extensions
    {
        private static Random random = new Random();

        /// <summary>
        /// Applies Fisher-Yates shuffle
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T[] Shuffle<T>(this T[] array) {
            int n = array.Length;

            for(int i = 0; i < (n - 1); i++) {
                int r = i + random.Next(n - 1);
                var temp = array[r];
                array[r] = array[i];
                array[i] = temp;
            }

            return array;
        }

        /// <summary>
        /// Returns a random element of an array
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T RandomElement<T>(this T[] array) {
            return array[random.Next(0, array.Length - 1)];
        }
    }
}
