// <copyright file="Extensions.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Provides extension methods to compute some mathematical stuff.
    /// </summary>
    public static class Extensions {

        /// <summary>
        /// Determines the media of the given list.
        /// </summary>
        /// <remarks>
        /// <para>The order of the elements in <paramref name="that"/> may be
        /// changed by the operation.</para>
        /// <para>Implementation from
        /// https://stackoverflow.com/questions/4140719/calculate-median-in-c-sharp/22702269
        /// </para>
        /// </remarks>
        /// <param name="that">The list to determine the media of.</param>
        /// <returns>The media of the list.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="that"/>
        /// is <c>null</c>.</exception>
        public static T Median<T>(this IList<T> that) where T : IComparable<T> {
            _ = that ?? throw new ArgumentNullException(nameof(that));
            return NthOrderStatistic(that, (that.Count - 1) / 2);
        }

        //public static double Median<T>(this IEnumerable<T> sequence, Func<T, double> getValue) {
        //    var list = sequence.Select(getValue).ToList();
        //    var mid = (list.Count - 1) / 2;
        //    return list.NthOrderStatistic(mid);
        //}

        /// <summary>
        /// Returns <paramref name="n"/>th smallest element from a list.
        /// </summary>
        /// <remarks>
        /// <para>Here, <paramref name="n"/> starts from 0 so that
        /// <paramref name="n"/> = 0 returns the minimum,
        /// <paramref name="n"/> = 1 returns 2nd smallest element etc.</para>
        /// <para>Note: the specified list might be mutated in the process.
        /// </para>
        /// <para>Reference: Introduction to Algorithms 3rd Edition,
        /// Corman et al., pp 216</para>
        public static T NthOrderStatistic<T>(this IList<T> that, int n,
                Random rng = null) where T : IComparable<T> {
            _ = that ?? throw new ArgumentNullException(nameof(that));
            return NthOrderStatistic(that, n, 0, that.Count - 1, rng);
        }

        /// <summary>
        /// Swaps two elements in a list.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        public static void Swap<T>(this IList<T> list, int i, int j) {
            if (i != j) {
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        #region Private methods
        private static T NthOrderStatistic<T>(this IList<T> list, int n,
                int start, int end, Random rng) where T : IComparable<T> {
            Debug.Assert(list != null);
            Debug.Assert(start >= 0);
            Debug.Assert(end < list.Count);
            Debug.Assert(start <= end);
            Debug.Assert(n < list.Count);

            while (true) {
                var pivotIndex = list.Partition(start, end, rng);

                if (pivotIndex == n) {
                    return list[pivotIndex];
                }

                if (n < pivotIndex) {
                    end = pivotIndex - 1;
                } else {
                    start = pivotIndex + 1;
                }
            }
        }

        /// <summary>
        /// Partitions the given list around a pivot element such that all
        /// elements on left of pivot are smaller than or equal to the pivot
        /// element and the ones at the right are larger.
        /// </summary>
        /// <remarks>
        /// <para>This method can be used for sorting, Nth-order statistics such
        /// as median finding algorithms. The pivot element is selected randomly
        /// if a random number generator is supplied. Otherwise, it is selected
        /// as last element in the list.</para>
        /// <para>Reference: Introduction to Algorithms 3rd Edition,
        /// Corman et al., pp 171</para>
        /// </remarks>
        private static int Partition<T>(this IList<T> list, int start, int end,
                Random rng = null) where T : IComparable<T> {
            Debug.Assert(list != null);
            Debug.Assert(start >= 0);
            Debug.Assert(end < list.Count);
            Debug.Assert(start <= end);

            if (rng != null) {
                list.Swap(end, rng.Next(start, end + 1));
            }

            var pivot = list[end];
            var lastLow = start - 1;

            for (var i = start; i < end; i++) {
                if (list[i].CompareTo(pivot) <= 0) {
                    list.Swap(i, ++lastLow);
                }
            }

            list.Swap(end, ++lastLow);

            return lastLow;
        }
        #endregion
    }
}
