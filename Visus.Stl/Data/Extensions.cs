// <copyright file="Extensions.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Dominik Herr, Christoph Müller</author>

using System;
using System.Collections.Generic;
using System.Linq;


namespace Visus.Stl.Data {

    /// <summary>
    /// Extension methods for manipulating data.
    /// </summary>
    public static class Extensions {

        /// <summary>
        /// Creates an evenly spaced series of values from the ordered time
        /// series <paramref name="that"/>.
        /// </summary>
        /// <typeparam name="TValue">The type of values to be enumerated.
        /// </typeparam>
        /// <param name="that">The collection which is to be spaced evenly.
        /// Note that this collection must be sorted. It is safe to pass
        /// <c>null</c>.</param>
        /// <param name="dt">The bin size.</param>
        /// <param name="aggregator">An aggregator function that combines
        /// values that fall into one bin.</param>
        /// <param name="nothing">The value to return for empty bins, which
        /// defaults to the default value of <typeparamref name="TValue"/>.
        /// </param>
        /// <returns>The evenly spaced values.</returns>
        public static IEnumerable<TValue> SpaceEvenly<TValue>(
                this IEnumerable<DateTimePoint<TValue>> that,
                TimeSpan dt,
                Func<TValue, TValue, TValue> aggregator,
                TValue nothing = default) {
            if (that != null) {
                var e = that.GetEnumerator();

                if (e.MoveNext()) {
                    var begin = e.Current.Time;
                    var end = begin + dt;
                    var val = e.Current.Value;

                    while (e.MoveNext()) {
                        if (e.Current.Time < end) {
                            // The falls into the previous bin, so aggregate it.
                            val = aggregator(val, e.Current.Value);

                        } else {
                            // The value is beyond the current bin, so emit the
                            // bin now.
                            yield return val;

                            // Determine whether there are empty bins between
                            // the current one and the one determined by the
                            // current position of the enumerator.
                            var cnt = (long) ((e.Current.Time - end) / dt);
                            for (int i = 0; i < cnt; ++i) {
                                yield return nothing;
                            }

                            // Begin a new bin.
                            begin += (cnt + 1) * dt;
                            end = begin + dt;
                            val = e.Current.Value;
                        }
                    }

                    yield return val;
                }
            }
        }
    }
}
