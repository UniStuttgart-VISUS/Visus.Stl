// <copyright file="Decomposition.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Dominik Herr, Christoph Müller</author>

using System;
using System.Collections.Generic;
using Visus.Stl.Data;


namespace Visus.Stl {

    /// <summary>
    /// Holds the result of an STL decomposition.
    /// </summary>
    public sealed class Decomposition<TValue> {

        #region Public properties
        /// <summary>
        /// Gets the configuration data that have been used for the
        /// decomposition.
        /// </summary>
        public MetaData MetaData { get; }

        /// <summary>
        /// Gets the data points of the remainder.
        /// </summary>
        public IEnumerable<DateTimePoint<TValue>> Remainder { get; }

        /// <summary>
        /// Gets the data points of the season.
        /// </summary>
        public IEnumerable<DateTimePoint<TValue>> Seasonal { get; }

        /// <summary>
        /// Gets the data points of the trend.
        /// </summary>
        public IEnumerable<DateTimePoint<TValue>> Trend { get; }
        #endregion

        #region Internal constructors
        internal Decomposition(IEnumerable<DateTimePoint<TValue>> trend,
                IEnumerable<DateTimePoint<TValue>> seasonal,
                IEnumerable<DateTimePoint<TValue>> remainder,
                MetaData metaData) {
            this.MetaData = metaData
                ?? throw new ArgumentNullException(nameof(metaData));
            this.Remainder = remainder
                ?? throw new ArgumentNullException(nameof(remainder));
            this.Seasonal = seasonal
                ?? throw new ArgumentNullException(nameof(seasonal));
            this.Trend = trend
                ?? throw new ArgumentNullException(nameof(trend));
        }
        #endregion
    }
}
