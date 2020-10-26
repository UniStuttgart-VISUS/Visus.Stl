// <copyright file="MetaData.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Dominik Herr, Christoph Müller</author>

using System;
using Visus.Stl.Maths;


namespace Visus.Stl.Data {


    /// <summary>
    /// Holds the meta data, ie the configuration, of an STL decomposition.
    /// </summary>
    public class MetaData {

        /** The paper stated that n_i = 1 should be ok, but recommended to use n_i = 2 to be sure. */
        internal static readonly int DefaultInnerLoopPasses = 2;

        internal static readonly int DefaultOuterLoopPasses = 1;

        #region Public constructors
        /// <summary>
        /// Constructor for custom window sizes
        /// </summary>
        /// <param name="numberOfObservations"></param>
        /// <param name="subseriesLength"></param>
        /// <param name="seasonalWindow"></param>
        /// <param name="seasonalLoessDegree"></param>
        /// <param name="trendWindow"></param>
        /// <param name="trendLoessDegree"></param>
        /// <param name="robustCalculation"></param>
        public MetaData(int numberOfObservations,
                int subseriesLength,
                int seasonalWindow,
                int seasonalLoessDegree = 0,
                int? trendWindow = null,
                int trendLoessDegree = 1,
                bool robustCalculation = false) {
            //                NumberOfObservations = numberOfObservations;
            SubseriesLength = subseriesLength;
            //                SeasonalWindowInterval = TimeIntervalEnum.Custom;
            SeasonalWindow = seasonalWindow;
            SeasonalLoessDegree = seasonalLoessDegree;
            //                TrendWindowInterval = TimeIntervalEnum.Custom;
            //                TrendWindow = trendWindow;
            TrendLoessDegree = trendLoessDegree;
            RobustCalculation = robustCalculation;
        }

        public MetaData() : this(TimeSpans.Hour, TimeSpans.Week) { }


        public MetaData(TimeSpan aggregationLevel,
                TimeSpan subseriesFrequency,
                int seasonalLoessDegree = 0,
                int trendLoessDegree = 1,
                bool robustCalculation = false) {
            AggregationLevel = aggregationLevel;
            SubseriesFrequency = subseriesFrequency;
            SeasonalLoessDegree = seasonalLoessDegree;
            TrendLoessDegree = trendLoessDegree;
            RobustCalculation = robustCalculation;
        }


        public MetaData(MetaData stlMetaData) {
            SeasonalWindow = stlMetaData.SeasonalWindow;
            IsPeriodic = stlMetaData.IsPeriodic;
            RobustCalculation = stlMetaData.RobustCalculation;
            AggregationLevel = stlMetaData.AggregationLevel;
            SubseriesFrequency = stlMetaData.SubseriesFrequency;
            //                SeasonalWindowInterval = stlMetaData.SeasonalWindowInterval;
            SeasonalLoessDegree = stlMetaData.SeasonalLoessDegree;
            TrendLoessDegree = stlMetaData.TrendLoessDegree;
            //                TrendWindowInterval = stlMetaData.TrendWindowInterval;
        }
        #endregion

        //        public int NumberOfObservations { get; set; }

        /// <summary>
        /// Gets or sets whether the series is known to be periodic.
        /// </summary>
        public bool IsPeriodic { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of inner loop passes.
        /// </summary>
        public int NumberOfInnerLoopPasses {
            get { return (RobustCalculation) ? RobustInnerLoopPasses : DefaultInnerLoopPasses; }
            set { RobustInnerLoopPasses = value; }
        }

        //        public int NumberOfInnerLoopPasses { get; set; } = DefaultInnerLoopPasses;
        /** n_o: The number of robustness iterations of the outer loop. */

        public int NumberOfOuterLoopPasses {
            get { return (RobustCalculation) ? RobustOuterLoopPasses : DefaultOuterLoopPasses; }
            set { RobustOuterLoopPasses = value; }
        }

        /** n_t: The smoothing parameter for the trend component. */
        //            public double TrendComponentBandwidth { get; set; } = DefaultTrendBandwidth;
        /** n_s: The smoothing parameter for the seasonal component. */
        //            public double SeasonalComponentBandwidth { get; set; } = DefaultSeasonalBandwidth;
        /** The number of robustness iterations in each invocation of Loess. */
        //            public int LoessRobustnessIterations { get; set; } = DefaultLoessRobustnessIterations;

        public TimeSpan AggregationLevel {
            get { return _aggregationLevel; }
            set {
                if (value == _aggregationLevel) return;
                _aggregationLevel = value;
            }
        }

        public int SeasonalLoessDegree {
            get => this._seasonalLoessDegree;
            set {
                if ((value < 0) || (value > 1)) {
                    throw new ArgumentException($"{nameof(SeasonalLoessDegree)} "
                        + $"must be within [0, 1], but is {value}.");
                }
                this._seasonalLoessDegree = value;
            }
        }

        public int TrendLoessDegree {
            get => this._trendLoessDegree;
            set {
                if ((value < 0) || (value > 1)) {
                    throw new ArgumentException($"{nameof(TrendLoessDegree)} "
                        + $"must be within [0, 1], but is {value}.");
                }
                this._trendLoessDegree = value;
            }
        }


        public TimeSpan SubseriesFrequency {
            get { return _subseriesFrequency; }
            set {
                if (value == _subseriesFrequency) return;
                _subseriesFrequency = value;
            }
        }

        public int SubseriesLength {
            get {
                return //_subseriesLength;
                       //ConvertTimeIntervalEnumToDatapointCount(SubseriesFrequency) / AggregationAmount;
                    (int) this.SubseriesFrequency.TotalHours;
            }
            set {
                if (value == _subseriesLength) return;
                _subseriesLength = value;
            }
        }

        public int AggregationAmount {
            //get { return ConvertTimeIntervalEnumToDatapointCount(AggregationLevel); }
            get => (int) this.AggregationLevel.TotalHours;
        }


        public int? SeasonalWindow {
            get => this._seasonalWindow;
            set {
                this._seasonalWindow = value;
                this._lowPassWindow = null;
                this._trendWindow = null;
            }
        }

        /// <summary>
        /// in STL this is nt
        /// </summary>
        public int TrendWindow {
            get {
                if (_trendWindow == null) {
                    _trendWindow = Math.Ceiling(1.5 * SubseriesLength / (1D - 1.5 / (double) SeasonalWindow));
                    _trendWindow = (_trendWindow % 2 == 1) ? _trendWindow : _trendWindow + 1;
                }
                return (int) _trendWindow;
            }
        }

        public int LowPassWindow {
            get {
                if (_lowPassWindow == null) {
                    _lowPassWindow = (SubseriesLength % 2 == 1) ? SubseriesLength : SubseriesLength + 1;
                }
                return (int) _lowPassWindow;
            }
        }

        public bool RobustCalculation {
            get { return _robustCalculation; }
            set { _robustCalculation = value; }
        }


        /**
        * Checks consistency of configuration parameters.
        *
        * <p>
        *   Must be called each time this configuration is used.
        * </p>
        *
        * <p>
        *   There must be at least two observations, and at least two periods
        *   in the data.
        * </p>
        *
        * @param numberOfDataPoints The number of data points in the target series.
        */
        internal void Check(int numberOfDataPoints) {

            //                TrendComponentBandwidth = DefaultTrendBandwidth;
            if (SubseriesLength < 2) {
                throw new ArgumentException(
                    "Periodicity (numberOfObservations) must be >= 2");
            }

            if (numberOfDataPoints <= 2 * SubseriesLength) {
                throw new ArgumentException(
                    "numberOfDataPoints(total length) must contain at least " +
                    "2 * Periodicity (numberOfObservations) points");
            }
        }


        //private int ConvertTimeIntervalEnumToDatapointCount(TimeIntervalEnum input) {
        //    switch (input) {
        //        case TimeIntervalEnum.Hour:
        //            return 1;
        //        case TimeIntervalEnum.Shift:
        //            return 8;
        //        case TimeIntervalEnum.Day:
        //            // hourly data -> daily season
        //            // f = 24(h) = 24
        //            return 24;
        //        case TimeIntervalEnum.Week:
        //            // hourly data -> weekly season
        //            // f = 24(h) * 7(d/w) = 168
        //            return 168;
        //        case TimeIntervalEnum.Month:
        //            //                            return 24*30;
        //            return (24 * 365) / 12; // = 730
        //            throw new NotImplementedException("Don't know yet what to put here...");
        //        case TimeIntervalEnum.Year:
        //            // hourly data -> daily season
        //            // TODO this frequency does not check for leap years yet
        //            // f = 24(h) * 365(d/y) = 8760 (8766 with averaged leap years)
        //            return 8760;
        //        case TimeIntervalEnum.NotSpecified:
        //            throw new InvalidEnumArgumentException("The interval of a window always has to be specified.");
        //        case TimeIntervalEnum.Custom:
        //            return _subseriesLength;
        //        default:
        //            throw new ArgumentOutOfRangeException();
        //    }
        //}


        #region Private fields
        private TimeSpan _aggregationLevel;
        //            private int _numberOfRobustnessIterations = DefaultOuterLoopPasses;
        private bool _robustCalculation;
        //            private TimeIntervalEnum _seasonalWindowInterval;
        private TimeSpan _subseriesFrequency = TimeSpans.Week;
        private int _subseriesLength = 168;
        private int? _seasonalWindow = null; //TODO set from null to 151 for debugging
        private double? _trendWindow; // nt
        private int _seasonalLoessDegree;
        private int _trendLoessDegree;
        //            private TimeIntervalEnum _trendWindowInterval;
        private int RobustInnerLoopPasses = 1;
        private int RobustOuterLoopPasses = 5;
        private double? _lowPassWindow;
        #endregion
    }

}
