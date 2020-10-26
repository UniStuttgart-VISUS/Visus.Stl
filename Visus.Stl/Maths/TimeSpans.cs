// <copyright file="TimeSpans.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Enumerates a set of well-known time spans.
    /// </summary>
    public static class TimeSpans {

        /// <summary>
        /// The time span of a single day.
        /// </summary>
        public static readonly TimeSpan Day = TimeSpan.FromDays(1.0);

        /// <summary>
        /// The time span of an hour.
        /// </summary>
        public static readonly TimeSpan Hour = TimeSpan.FromHours(1.0);

        /// <summary>
        /// A banking month of 30 days.
        /// </summary>
        public static readonly TimeSpan Month = 30 * Day;

        /// <summary>
        /// A week.
        /// </summary>
        public static readonly TimeSpan Week = 7 * Day;

        /// <summary>
        /// A standard, 365-day year.
        /// </summary>
        public static readonly TimeSpan Year = 365 * Day;
    }
}
