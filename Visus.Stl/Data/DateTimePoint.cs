// <copyright file="DateTimePoint.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Dominik Herr, Christoph Müller</author>

using System;
using System.Diagnostics.CodeAnalysis;


namespace Visus.Stl.Data {

    /// <summary>
    /// Represents a data point in time.
    /// </summary>
    public class DateTimePoint<TValue> : IComparable<DateTimePoint<TValue>>,
            IEquatable<DateTimePoint<TValue>> {

        #region Public constructors
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public DateTimePoint() : this(DateTime.MinValue, default) { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="time"></param>
        /// <param name="value"></param>
        public DateTimePoint(DateTime time, TValue value) {
            this.Time = time;
            this.Value = value;
        }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="ticks"></param>
        /// <param name="value"></param>
        public DateTimePoint(long ticks, TValue value)
            : this(new DateTime(ticks), value) { }

        /// <summary>
        /// Initialises an instance from another.
        /// </summary>
        /// <param name="dateTimePoint"></param>
        public DateTimePoint(DateTimePoint<TValue> dateTimePoint) {
            _ = dateTimePoint
                ?? throw new ArgumentNullException(nameof(dateTimePoint));
            this.Time = dateTimePoint.Time;
            this.Value = dateTimePoint.Value;
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Gets or sets the point in time the value is for.
        /// </summary>
        public DateTime Time {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the value at the given time.
        /// </summary>
        public TValue Value {
            get;
            set;
        }
        #endregion

        #region Public methods
        /// <inheritdoc />
        public int CompareTo(DateTimePoint<TValue> other) {
            return this.Time.CompareTo(other.Time);
        }

        /// <inheritdoc />
        public bool Equals([AllowNull] DateTimePoint<TValue> other) {
            if (other != null) {
                return this.Time.Equals(other.Time)
                    && (((this.Value == null) && (other.Value == null))
                    || this.Value.Equals(other.Value));
            } else {
                return false;
            }
        }

        /// <inheritdoc />
        public override bool Equals(object obj) {
            return this.Equals(obj as DateTimePoint<TValue>);
        }

        /// <inheritdoc />
        public override int GetHashCode() {
            return this.Time.GetHashCode() ^ this.Value.GetHashCode();
        }
        #endregion
    }
}
