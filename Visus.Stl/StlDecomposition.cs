// <copyright file="StlDecomposition.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Dominik Herr, Christoph Müller</author>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Visus.Stl.Data;
using Visus.Stl.Maths;


namespace Visus.Stl {


    public sealed class StlDecomposition {

        private const bool OutputDetailedStlDebugInformation = false;

        #region Public contructors
        /// <summary>
        /// Constructs an STL function that can de-trend data.
        /// <para>
        /// n.b. The Java Loess implementation only does linear local polynomial
        /// regression, but R supports linear (degree=1), quadratic (degree=2), and a
        /// strange degree=0 option.
        /// </para>
        /// <para>
        /// Also, the Java Loess implementation accepts "bandwidth", the fraction of
        /// source points closest to the current point, as opposed to integral values.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration object (also contains numberOfObservations)</param>
        public StlDecomposition(MetaData configuration) {
            this.Configuration = configuration
                ?? throw new ArgumentNullException(nameof(configuration));
        }
        #endregion

        #region Public properties
        /// <summary>
        /// Gets the configuration specifying the parameters of the
        /// decomposition.
        /// </summary>
        public MetaData Configuration { get; }
        #endregion

        /// <summary>
        /// A convenience method to use objects.
        /// </summary>
        /// <param name="times">A sequence of time values.</param>
        /// <param name="series">A dependent variable on times.</param>
        /// <returns>The STL decomposition of the time series.</returns>
        public Decomposition<double> Decompose(List<long> times, List<double> series, BandwidthMode bandwidthMode = BandwidthMode.Discrete) {
            long[] timesArray = times.ToArray();
            double[] seriesArray = series.ToArray();

            //            int idx = 0;
            //            foreach (long time in times)
            //            {
            //                timesArray[idx++] = time;
            //            }
            //
            //            idx = 0;
            //            foreach (double value in series)
            //            {
            //                seriesArray[idx++] = value;
            //            }

            return Decompose(timesArray, seriesArray, bandwidthMode);
        }

        public Decomposition<double> Decompose(
                IList<DateTimePoint<double>> dateTimePoints,
                BandwidthMode bandwidthMode = BandwidthMode.Discrete) {
            var timesArray = new long[dateTimePoints.Count];
            var seriesArray = new double[dateTimePoints.Count];

            int idx = 0;
            foreach (var dateTimePoint in dateTimePoints) {
                timesArray[idx] = dateTimePoint.Time.Ticks;
                seriesArray[idx++] = dateTimePoint.Value;
            }

            return this.Decompose(timesArray, seriesArray, bandwidthMode);
        }

        /// <summary>
        /// Computes the STL decomposition of a times series.
        /// </summary>
        /// <param name="times">A sequence of time values.</param>
        /// <param name="series">A dependent variable on times.</param>
        /// <returns>The STL decomposition of the time series.</returns>
        public Decomposition<double> Decompose(long[] times, double[] series, BandwidthMode bandwidthMode, bool postTrendSmoothing = true, bool postSeasonSmoothing = false) {
            Debug.WriteLineIf(OutputDetailedStlDebugInformation, "STL decomposition: started");
            if (times.Length != series.Length) {
                throw new ArgumentException("Times (" + times.Length +
                                            ") and series (" + series.Length + ") must be same size");
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int numberOfDataPoints = series.Length;

            //            if (bandwidthMode == BandwidthMode.Discrete)
            //            {
            //                Config.SeasonalComponentBandwidth = Config.SubseriesLength/(double)times.Length * 2;
            ////                // set seasonal component bandwidth
            ////                switch (Config.SeasonalWindowInterval)
            ////                {
            ////                    case StlR.StlMetaData.TimeIntervalEnum.Hour:
            ////                        Config.SeasonalComponentBandwidth = Config.SubseriesLength/(double)times.Length * 2;
            ////                        break;
            ////                    case StlR.StlMetaData.TimeIntervalEnum.Shift:
            ////                        break;
            ////                    case StlR.StlMetaData.TimeIntervalEnum.Day:
            ////                        Config.SeasonalComponentBandwidth = Config.SubseriesLength/(double)times.Length * 2;
            ////                        break;
            ////                    case StlR.StlMetaData.TimeIntervalEnum.Week:
            ////                        break;
            ////                    case StlR.StlMetaData.TimeIntervalEnum.Month:
            ////                        break;
            ////                    case StlR.StlMetaData.TimeIntervalEnum.Year:
            ////                        break;
            ////                    case StlR.StlMetaData.TimeIntervalEnum.NotSpecified:
            ////                        break;
            ////                    case StlR.StlMetaData.TimeIntervalEnum.Custom:
            ////                        break;
            ////                    default:
            ////                        throw new ArgumentOutOfRangeException();
            ////                }
            //            }

            Configuration.Check(numberOfDataPoints);

            double[] trend = new double[numberOfDataPoints];
            double[] seasonal = new double[numberOfDataPoints];
            double[] remainder = new double[numberOfDataPoints];
            double[] robustness = null;
            double[] detrend = new double[numberOfDataPoints];
            double[] combinedSmoothed = new double[numberOfDataPoints + 2 * Configuration.SubseriesLength];

            long[] combinedSmoothedTimes = new long[numberOfDataPoints + 2 * Configuration.SubseriesLength];
            for (int i = 0; i < combinedSmoothedTimes.Length; i++) {
                combinedSmoothedTimes[i] = i;
            }

            // outer loop
            for (int l = 0; l < Configuration.NumberOfOuterLoopPasses; l++) {
                Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"STL decomposition: outer iteration #{l}");
                // inner loop
                for (int k = 0; k < Configuration.NumberOfInnerLoopPasses; k++) {
                    Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"STL decomposition: outer iteration #{l}; inner iteration #{k}");
#if(DEBUG)
                    var swSteps = new Stopwatch();
                    swSteps.Start();
#endif
                    // Step 1: De-trending
                    for (int i = 0; i < numberOfDataPoints; i++) {
                        detrend[i] = series[i] - trend[i];
                    }
                    //                    Parallel.For(0, numberOfDataPoints, i => detrend[i] = series[i] - trend[i]);

                    // Get cycle sub-series
                    int numberOfObservations = Configuration.SubseriesLength;
                    //#if(DEBUG)
                    //                    var swCycle = new Stopwatch();
                    //                    swCycle.Start();
                    //#endif
                    CycleSubSeries cycle = new CycleSubSeries(times, series, robustness, detrend, numberOfObservations);
                    cycle.Compute();
                    //#if(DEBUG)
                    //                    swCycle.Stop();
                    //                    Debug.WriteLine($"subcycle computation took {swCycle.ElapsedMilliseconds}ms.");
                    //#endif
                    List<double[]> cycleSubseries = cycle.SubSeries;
                    List<long[]> cycleTimes = cycle.CycleTimes;
                    List<double[]> cycleRobustnessWeights = cycle.CycleRobustnessWeights;

#if (DEBUG)
                    swSteps.Stop();
                    Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"Preprocessing & Step 1 took {swSteps.ElapsedMilliseconds}ms.");
                    swSteps.Restart();
#endif
                    // Step 2: Cycle-subseries Smoothing
                    for (int i = 0; i < cycleSubseries.Count; i++) {
                        // Pad times
                        long[] paddedTimes = new long[cycleTimes[i].Length + 2];
                        for (int j = 0; j < paddedTimes.Length; j++) {
                            paddedTimes[j] = j;
                        }

                        // Pad series
                        double[] paddedSeries = new double[cycleSubseries[i].Length + 2];
                        Array.Copy(cycleSubseries[i], 0, paddedSeries, 1, cycleSubseries[i].Length);

                        // Pad weights
                        double[] weights = cycleRobustnessWeights[i];
                        double[] paddedWeights = null;
                        if (weights != null) {
                            paddedWeights = new double[weights.Length + 2];
                            Array.Copy(weights, 0, paddedWeights, 1, weights.Length);
                        }

                        // Loess smoothing
                        double[] smoothed;

                        switch (bandwidthMode) {
                            case BandwidthMode.Percentage:
                                throw new NotImplementedException();
                            //                                smoothed = LoessSmooth(paddedTimes, paddedSeries, Config.SeasonalComponentBandwidth, paddedWeights);
                            //                                break;
                            case BandwidthMode.Discrete:
                                //                                var jump = (int)Math.Ceiling(numberOfObservations*1.5);
                                //                                jump = (jump%2 == 1) ? jump : jump + 1;
                                var jump = 1;
                                smoothed = LoessSmooth(paddedTimes, paddedSeries, (int) Configuration.SeasonalWindow, paddedWeights, jump);
                                //                                smoothed = LoessSmooth(paddedTimes, paddedSeries, Config.SeasonalComponentBandwidth, paddedWeights);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(bandwidthMode), bandwidthMode, null);
                        }
                        //                        = LoessSmooth(
                        //                            paddedTimes,
                        //                            paddedSeries,
                        //                            Config.SeasonalComponentBandwidth,
                        //                            paddedWeights);

                        cycleSubseries[i] = smoothed;
                    }
                    //                    Parallel.For(0, cycleSubseries.Count, i =>
                    //                    {
                    //                        // Pad times
                    //                        long[] paddedTimes = new long[cycleTimes[i].Length + 2];
                    //                        for (int j = 0; j < paddedTimes.Length; j++)
                    //                        {
                    //                            paddedTimes[j] = j;
                    //                        }
                    //
                    //                        // Pad series
                    //                        double[] paddedSeries = new double[cycleSubseries[i].Length + 2];
                    //                        Array.Copy(cycleSubseries[i], 0, paddedSeries, 1, cycleSubseries[i].Length);
                    //
                    //                        // Pad weights
                    //                        double[] weights = cycleRobustnessWeights[i];
                    //                        double[] paddedWeights = null;
                    //                        if (weights != null)
                    //                        {
                    //                            paddedWeights = new double[weights.Length + 2];
                    //                            Array.Copy(weights, 0, paddedWeights, 1, weights.Length);
                    //                        }
                    //                        
                    //                        // Loess smoothing
                    //                        double[] smoothed;
                    //                        switch (bandwidthMode)
                    //                        {
                    //                            case BandwidthMode.Percentage:
                    //                                throw new NotImplementedException();
                    ////                                smoothed = LoessSmooth(paddedTimes, paddedSeries, Config.SeasonalComponentBandwidth, paddedWeights);
                    ////                                break;
                    //                            case BandwidthMode.Discrete:
                    ////                                var jump = (int)Math.Ceiling(numberOfObservations*1.5);
                    ////                                jump = (jump%2 == 1) ? jump : jump + 1;
                    //                                var jump = 1;
                    //                                smoothed = LoessSmooth(paddedTimes, paddedSeries, (int) Config.SeasonalWindow, paddedWeights, jump);
                    ////                                smoothed = LoessSmooth(paddedTimes, paddedSeries, Config.SeasonalComponentBandwidth, paddedWeights);
                    //                                break;
                    //                            default:
                    //                                throw new ArgumentOutOfRangeException(nameof(bandwidthMode), bandwidthMode, null);
                    //                        }
                    //
                    //                        cycleSubseries[i] = smoothed;
                    //                    });

                    // Combine smoothed series into one
                    for (int i = 0; i < cycleSubseries.Count; i++) {
                        double[] subseriesValues = cycleSubseries[i];
                        for (int cycleIdx = 0; cycleIdx < subseriesValues.Length; cycleIdx++) {
                            combinedSmoothed[numberOfObservations * cycleIdx + i] = subseriesValues[cycleIdx];
                        }
                    }

#if (DEBUG)
                    swSteps.Stop();
                    Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"Step 2 took {swSteps.ElapsedMilliseconds}ms.");
                    swSteps.Restart();
#endif
                    // Step 3: Low-Pass Filtering of Smoothed Cycle-Subseries
                    double[] filtered = LowPassFilter(combinedSmoothedTimes, combinedSmoothed, null);

#if (DEBUG)
                    swSteps.Stop();
                    Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"Step 3 took {swSteps.ElapsedMilliseconds}ms.");
                    swSteps.Restart();
#endif
                    // Step 4: Detrending of Smoothed Cycle-Subseries
                    int offset = Configuration.SubseriesLength;
                    for (int i = 0; i < seasonal.Length; i++) {
                        seasonal[i] = combinedSmoothed[i + offset] - filtered[i + offset];
                    }

#if (DEBUG)
                    swSteps.Stop();
                    Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"Step 4 took {swSteps.ElapsedMilliseconds}ms.");
                    swSteps.Restart();
#endif
                    // Step 5: Deseasonalizing
                    for (int i = 0; i < numberOfDataPoints; i++) {
                        trend[i] = series[i] - seasonal[i];
                    }

#if (DEBUG)
                    swSteps.Stop();
                    Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"Step 5 took {swSteps.ElapsedMilliseconds}ms.");
                    swSteps.Restart();
#endif
                    // Step 6: Trend Smoothing
                    // reconstruct trend size according to paper
                    // 1.5*n_p <= n_t <= 2*n_p; n_t = smallest odd integer satisfying inequality
                    //                    var nt = (int)Math.Ceiling(numberOfObservations*1.5);
                    //                    var nt = (int)Math.Ceiling((1.5*numberOfObservations)/(1D - (1.5/(double)Config.SeasonalWindow)));
                    //                    nt = (nt%2 == 1) ? nt : nt + 1;
                    var nt = Configuration.TrendWindow;
                    trend = LoessSmooth(times, trend, nt, robustness);
                    //                    switch (bandwidthMode)
                    //                    {
                    //                        case BandwidthMode.Percentage:
                    //                            trend = LoessSmooth(times, trend, Config.TrendComponentBandwidth, robustness);
                    //                            break;
                    //                        case BandwidthMode.Discrete:
                    //                            if (Config.TrendWindow != null)
                    //                                trend = LoessSmooth(times, trend, (int) Config.TrendWindow, robustness);
                    ////                                trend = LoessSmooth(times, trend, Config.TrendComponentBandwidth, robustness);
                    //                            else
                    //                                throw new ArgumentNullException("Trend window cannot be null!");
                    //                            break;
                    //                        default:
                    //                            throw new ArgumentOutOfRangeException(nameof(bandwidthMode), bandwidthMode, null);
                    //                    }
#if (DEBUG)
                    swSteps.Stop();
                    Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"Step 6 took {swSteps.ElapsedMilliseconds}ms.");
#endif
                }

                // --- Now in outer loop ---

                // Calculate remainder
                for (int i = 0; i < numberOfDataPoints; i++) {
                    remainder[i] = series[i] - trend[i] - seasonal[i];
                }

                if (l < Configuration.NumberOfOuterLoopPasses - 1)
                    // Calculate robustness weights using remainder
                    robustness = RobustnessWeights(remainder);
            }

            if (postTrendSmoothing) {
                var tmp = LoessSmooth(times, trend, (int) (1.5 * Configuration.TrendWindow + 1), robustness);
                for (int i = 0; i < tmp.Length; i++) {
                    var diff = trend[i] - tmp[i];
                    trend[i] = trend[i] - diff;
                    remainder[i] = remainder[i] + diff;
                }
            }

            if (postSeasonSmoothing) {
                var tmp = LoessSmooth(times, trend, (int) (1.5 * (int) Configuration.SeasonalWindow + 1), robustness);
                for (int i = 0; i < tmp.Length; i++) {
                    var diff = seasonal[i] - tmp[i];
                    seasonal[i] = seasonal[i] - diff;
                    remainder[i] = remainder[i] + diff;
                }
            }


            if (Configuration.IsPeriodic) {
                for (int i = 0; i < Configuration.SubseriesLength; i++) {
                    // Compute weighted mean for one season
                    double sum = 0.0;
                    int count = 0;
                    for (int j = i; j < numberOfDataPoints; j += Configuration.SubseriesLength) {
                        sum += seasonal[j];
                        count++;
                    }
                    double mean = sum / count;

                    // Copy this to rest of seasons
                    for (int j = i; j < numberOfDataPoints; j += Configuration.SubseriesLength) {
                        seasonal[j] = mean;
                    }
                }

                // Recalculate remainder
                for (int i = 0; i < series.Length; i++) {
                    remainder[i] = series[i] - trend[i] - seasonal[i];
                }
            }

            var trendDtp = new DateTimePoint<double>[times.Length];
            var seasonDtp = new DateTimePoint<double>[times.Length];
            var remainderDtp = new DateTimePoint<double>[times.Length];

            Parallel.For(0, times.Length, i => {
                trendDtp[i] = new DateTimePoint<double>(times[i], trend[i]);
                seasonDtp[i] = new DateTimePoint<double>(times[i], seasonal[i]);
                remainderDtp[i] = new DateTimePoint<double>(times[i], remainder[i]);
            });

            sw.Stop();
            Debug.WriteLineIf(OutputDetailedStlDebugInformation, $"Thread{Thread.CurrentThread.Name} ({Thread.CurrentThread.ManagedThreadId} - decomposition) took {sw.ElapsedMilliseconds} ms.");

            return new Decomposition<double>(trendDtp, seasonDtp, remainderDtp,
                new MetaData(this.Configuration));
        }

        /// <summary>
        /// The bisquare weight function.
        /// </summary>
        /// <param name="value">Any real number.</param>
        /// <returns>
        /// <pre>
        ///     (1 - value^2)^2 for 0 &lt;= value &lt; 1
        ///     0 for value > 1
        /// </pre>
        /// </returns>
        private double biSquareWeight(double value) {
            if (value < 0) {
                throw new ArgumentException("Invalid value, must be >= 0: " + value);
            } else if (value < 1) {
                return (1D - (value * value)) * (1D - (value * value));
            } else {
                return 0;
            }
        }

        //        /// <summary>
        //        /// Performs weighted Loess smoothing on a series.
        //        /// <para>
        //        ///     Does not assume contiguous time.
        //        /// </para>
        //        /// </summary>
        //        /// <param name="times">The times.</param>
        //        /// <param name="series">The time series data.</param>
        //        /// <param name="bandwidth">The amount of neighbor points to consider for each point in Loess.</param>
        //        /// <param name="weights">The weights to use for smoothing, if null, equal weights are assumed.</param>
        //        /// <returns>Loess-smoothed series.</returns>
        //        private double[] LoessSmooth(long[] times, double[] series, double bandwidth, double[] weights)
        //        {
        //            if (weights == null)
        //            {
        //                return new LoessInterpolator(bandwidth, Config.LoessRobustnessIterations).Smooth(times, series);
        //            }
        //            else
        //            {
        ////                throw new NotImplementedException();
        //                return new LoessInterpolator(
        //                    bandwidth,
        //                    Config.LoessRobustnessIterations).Smooth(times, series, weights);
        //            }
        //        }

        private double[] LoessSmooth(long[] times, double[] series, int bandwidthInPoints, double[] weights, int jumpsize = 1) {
            return new LoessInterpolator().Smooth(times, series, weights, bandwidthInPoints, jumpsize);
        }

        /// <summary>
        /// A low pass filter used on combined smoothed cycle subseries.
        /// <para>
        ///     The filter consists of the following steps:
        ///     <ol>
        ///         <li>Moving average of length n_p, seasonal size</li>
        ///         <li>Moving average of length 3, (magic number from paper)</li>
        ///         <li>Loess smoothing</li>
        ///     </ol>
        /// </para>
        /// </summary>
        /// <param name="times">The times.</param>
        /// <param name="series">The time series data.</param>
        /// <param name="weights">Weights to use in Loess stage.</param>
        /// <returns>A smoother, less noisy series.</returns>
        private double[] LowPassFilter(long[] times, double[] series, double[] weights) {
            // Find the next odd integer >= n_p (see: section 3.4)
            //            int nextOdd = Config.SubseriesLength%2 == 1 ? Config.SubseriesLength : Config.SubseriesLength + 1;
            var nl = Configuration.LowPassWindow;
            // Determine bandwidth as a percentage of points
            //            double lowPassBandwidth = nextOdd/series.Length;

            // Apply moving average of length n_p, twice
            series = movingAverage(series, Configuration.SubseriesLength);
            series = movingAverage(series, Configuration.SubseriesLength);
            // Apply moving average of length 3
            series = movingAverage(series, 3);
            // Loess smoothing with d = 1, q = n_l
            series = LoessSmooth(times, series, nl, weights);
            return series;
        }

        /// <summary>
        /// Computes the moving average.
        /// <para>
        ///     The first "window" values are meaningless in the return value.
        /// </para>
        /// </summary>
        /// <param name="series">An input series of data.</param>
        /// <param name="window">The moving average sliding window.</param>
        /// <returns>A new series that contains moving average of series.</returns>
        public double[] movingAverage(double[] series, int window) {
            double[] movingAverage = new double[series.Length];

            //            // centered average
            double average = 0;

            double averageRight = 0;
            var windowEdge = (int) Math.Floor(window / 2D);
            //            // left+right side
            //            for (int i = 0; i < windowEdge; i++)
            //            {
            //                average += series[i] / (i + 1);
            //                movingAverage[i] =+ average;
            //                averageRight += series[series.Length - 1 - i] / (i + 1);
            //                movingAverage[movingAverage.Length - 1 - i] = averageRight;
            //            }
            //            // center
            //            for (int i = windowEdge; i < series.Length - 1 - windowEdge + 1 ; i++)
            //            {
            //                if (i != windowEdge)
            //                    average -= series[i - windowEdge - 1]/window;
            //                average += series[i]/window;
            //                movingAverage[i - windowEdge] =+ average;
            //            }



            // Initialize
            //            double average = 0;
            for (int i = 0; i < window; i++) {
                average += series[i] / window;
                movingAverage[i] = average;
            }

            for (int i = window; i < series.Length; i++) {
                average -= series[i - window] / (double) window;
                average += series[i] / (double) window;
                movingAverage[i] = average;
            }

            // FIXME new: shift
            for (int i = 0; i < series.Length - windowEdge; i++) {
                movingAverage[i] = movingAverage[i + windowEdge];
            }
            // FIXME fill leftover values
            for (int i = 0; i < windowEdge; i++) {
                average -= series[series.Length - 1 - i - windowEdge] / (windowEdge - (i));
                movingAverage[movingAverage.Length - 1 - i] = average;
            }
            return movingAverage;
        }

        /// <summary>
        /// Computes robustness weights using bisquare weight function.
        /// </summary>
        /// <param name="remainder">The remainder, series - trend - seasonal.</param>
        /// <returns>A new array containing the robustness weights.</returns>
        private double[] RobustnessWeights(double[] remainder) {
            // Compute "h" = 6 median(|R_v|)
            double[] absRemainder = new double[remainder.Length];
            //            double[] stats = new double[remainder.Length];
            for (int i = 0; i < remainder.Length; i++) {
                absRemainder[i] = Math.Abs(remainder[i]);
                //                stats[i] = remainder[i];
            }
            var outlierThreshold = 6 * absRemainder.Median();
            //            stats.Sort();
            //            double outlierThreshold = 6*absRemainder[Convert.ToInt32(Math.Floor(stats.Count/2D))];

            // Compute robustness weights
            double[] robustness = new double[remainder.Length];
            //            for (int i = 0; i < remainder.Length; i++)
            //            {
            //                robustness[i] = biSquareWeight(absRemainder[i]/outlierThreshold);
            //            }
            Parallel.For(0, remainder.Length, i => { robustness[i] = biSquareWeight(absRemainder[i] / outlierThreshold); });

            return robustness;
        }
    }

    /// <summary>
    /// The cycle subseries of a time series.
    /// <para>
    ///     The cycle subseries is a set of series whose members are of length
    ///     N, where N is the number of observations in a season.
    /// </para>
    /// 
    /// <para>
    ///     For example, if we have monthly data from 1990 to 2000, the cycle
    ///     subseries would be [[Jan_1990, Jan_1991, ...], ..., [Dec_1990, Dec_1991]].
    /// </para>
    /// </summary>
    internal class CycleSubSeries {
        /** Input: The de-trended series, from STL. */
        private readonly double[] _detrend;

        /// <summary>
        /// Constructs a cycle subseries computation.
        /// </summary>
        /// <param name="times">The input times.</param>
        /// <param name="series">A dependent variable on times.</param>
        /// <param name="robustness">The robustness weights from STL loop.</param>
        /// <param name="detrend">The de-trended data.</param>
        /// <param name="numberOfObservations">The number of observations in a season.</param>
        internal CycleSubSeries(long[] times, double[] series, double[] robustness, double[] detrend, int numberOfObservations) {
            Times = times;
            Series = series;
            Robustness = robustness;
            _detrend = detrend;
            NumberOfObservations = numberOfObservations;
        }

        /// <summary>
        /// Input: The number of observations in a season.
        /// </summary>
        private int NumberOfObservations { get; }

        /// <summary>
        /// Input: The robustness weights, from STL.
        /// </summary>
        private double[] Robustness { get; }

        /// <summary>
        /// Input: The input series data.
        /// </summary>
        private double[] Series { get; }

        /// <summary>
        /// Input: The input times.
        /// </summary>
        private long[] Times { get; }


        /// <summary>
        /// Output: The list of cycle subseries robustness weights.
        /// </summary>
        public List<double[]> CycleRobustnessWeights { get; } = new List<double[]>();

        /// <summary>
        /// Output: The list of cycle subseries series data.
        /// </summary>
        public List<double[]> SubSeries { get; } = new List<double[]>();

        /// <summary>
        /// Output: The list of cycle subseries times.
        /// </summary>
        public List<long[]> CycleTimes { get; } = new List<long[]>();

        /// <summary>
        /// Computes the cycle subseries of the input.
        /// <para>
        ///     Must call this before getters return anything meaningful.
        /// </para>
        /// </summary>
        internal void Compute() {
            for (int i = 0; i < NumberOfObservations; i++) {
                int subseriesLength = Series.Length / NumberOfObservations;
                subseriesLength += (i < Series.Length % NumberOfObservations) ? 1 : 0;

                double[] subseriesValues = new double[subseriesLength];
                long[] subseriesTimes = new long[subseriesLength];
                double[] subseriesRobustnessWeights = null;

                if (Robustness != null) {
                    subseriesRobustnessWeights = new double[subseriesLength];
                }

                for (int cycleIdx = 0; cycleIdx < subseriesLength; cycleIdx++) {
                    subseriesValues[cycleIdx] = _detrend[cycleIdx * NumberOfObservations + i];
                    subseriesTimes[cycleIdx] = Times[cycleIdx * NumberOfObservations + i];
                    if (subseriesRobustnessWeights != null) {
                        subseriesRobustnessWeights[cycleIdx] = Robustness[cycleIdx * NumberOfObservations + i];

                        // TODO: Hack to ensure no divide by zero
                        if (subseriesRobustnessWeights[cycleIdx] < 0.001) {
                            subseriesRobustnessWeights[cycleIdx] = 0.001;
                        }
                    }
                }

                //                Parallel.For(0, subseriesLength, cycleIdx =>
                //                {
                //                    subseriesValues[cycleIdx] = _detrend[cycleIdx*NumberOfObservations + i];
                //                    subseriesTimes[cycleIdx] = Times[cycleIdx*NumberOfObservations + i];
                //                    if (subseriesRobustnessWeights != null)
                //                    {
                //                        subseriesRobustnessWeights[cycleIdx] = Robustness[cycleIdx*NumberOfObservations + i];
                //                        
                //                        if (subseriesRobustnessWeights[cycleIdx] < 0.001)
                //                        {
                //                            subseriesRobustnessWeights[cycleIdx] = 0.001;
                //                        }
                ////                        if (Math.Abs(subseriesRobustnessWeights[cycleIdx]) < double.Epsilon)
                ////                        {
                ////                            subseriesRobustnessWeights[cycleIdx] = double.Epsilon;
                ////                        }
                //                    }
                //                });

                SubSeries.Add(subseriesValues);
                CycleTimes.Add(subseriesTimes);
                CycleRobustnessWeights.Add(subseriesRobustnessWeights);
            }
        }
    }
}
