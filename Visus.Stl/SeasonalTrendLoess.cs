using System;
using System.Collections.Generic;
using System.Text;
using Visus.Stl.Maths;


namespace Visus.Stl {

    /// <summary>
    /// Implements the Seasonal Trend Loess algorithm for evenly spaced data.
    /// </summary>
    /// <remarks>
    /// This implementation is a direct port of the Java implemenation at
    /// https://github.com/ServiceNow/stl-decomp-4j/blob/master/stl-decomp-4j/src/main/java/com/github/servicenow/ds/stats/stl/SeasonalTrendLoess.java
    /// </remarks>
    public sealed class SeasonalTrendLoess {

#if false
        /**
		 * Factory method to perform a non-robust STL decomposition enforcing strict periodicity.
		 * <p>
		 * Meant for diagnostic purposes only.
		 *
		 * @param data        the data to analyze
		 * @param periodicity the (suspected) periodicity of the data
		 * @return SeasonalTrendLoess object with the decomposition already performed.
		 */
        public static Decomposition performPeriodicDecomposition(
                double[] data,
                int periodicity
        ) {
            // The LOESS interpolator with degree 0 and a very long window (arbitrarily chosen to be 100 times the length of
            // the array) will interpolate all points as the average value of the series. This particular setting is used
            // for smoothing the seasonal sub-cycles, so the end result is that the seasonal component of the decomposition
            // is exactly periodic.

            // This fit is for diagnostic purposes, so we just do a single inner iteration.

            SeasonalTrendLoess stl = new SeasonalTrendLoess.Builder()
                    .setPeriodLength(periodicity) //
                    .setSeasonalWidth(100 * data.length) //
                    .setSeasonalDegree(0) //
                    .setInnerIterations(1) //
                    .setRobustnessIterations(0) //
                    .buildSmoother(data);

            return stl.decompose();
        }

        /**
		 * Factory method to perform a (somewhat) robust STL decomposition enforcing strict periodicity.
		 * <p>
		 * Meant for diagnostic purposes only.
		 *
		 * @param data        the data to analyze
		 * @param periodicity the (suspected) periodicity of the data
		 * @return SeasonalTrendLoess object with the decomposition already performed.
		 */
        public static Decomposition performRobustPeriodicDecomposition(
                double[] data,
                int periodicity
        ) {
            // The LOESS interpolator with degree 0 and a very long window (arbitrarily chosen to be 100 times the length of
            // the array) will interpolate all points as the average value of the series. This particular setting is used
            // for smoothing the seasonal sub-cycles, so the end result is that the seasonal component of the decomposition
            // is exactly periodic.

            // This fit is for diagnostic purposes, so we just do a single inner and outer iteration.

            SeasonalTrendLoess stl = new SeasonalTrendLoess.Builder()
                    .setPeriodLength(periodicity) //
                    .setSeasonalWidth(100 * data.length) //
                    .setSeasonalDegree(0) //
                    .setInnerIterations(1) //
                    .setRobustnessIterations(1) //
                    .buildSmoother(data);

            return stl.decompose();
        }
#endif

        #region Public constructors
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="data">The data to be decomposed.</param>
        /// <param name="periodicity">The periodicity of the data.</param>
        /// <param name="cntInner">The number of inner iterations.</param>
        /// <param name="cntOuter">The number of outer &quot;robustness&quot;
        /// iterations.</param>
        /// <param name="seasonalSettings">The settings for the LOESS smoother
        /// for the cyclic sub-series.</param>
        /// <param name="trendSettings">The settings for the LOESS smoother for
        /// the trend component.</param>
        /// <param name="lowpassSettings">The settings for the LOESS smoother
        /// used in de-seasonalising.</param>
        public SeasonalTrendLoess(IList<double> data, int periodicity, int cntInner,
                int cntOuter, LoessSettings seasonalSettings,
                LoessSettings trendSettings, LoessSettings lowpassSettings) {
            fData = data;
            int size = data.Count;
            fPeriodLength = periodicity;
            fSeasonalSettings = seasonalSettings;
            fTrendSettings = trendSettings;
            fLowpassSettings = lowpassSettings;
            fInnerIterations = cntInner;
            fRobustIterations = cntOuter;
            fLoessSmootherFactory = new LoessSmootherBuilder()
                .SetSettings(trendSettings);
            fLowpassLoessFactory = new LoessSmootherBuilder()
                .SetSettings(lowpassSettings);
            fCyclicSubSeriesSmoother = new CyclicSubSeriesSmoother(
                seasonalSettings, size, periodicity, 1, 1);
            fDetrend = new double[size];
            fExtendedSeasonal = new double[size + 2 * fPeriodLength];
        }
        #endregion

        #region Public methods
        /// <summary>
        /// Decompose the input into seasonal, trend and residual components.
        /// </summary>
        /// <returns></returns>
        public Decomposition Decompose() {
            // TODO: Pass input data to decompose and reallocate buffers based on that size.
            fDecomposition = new Decomposition(fData);

            int outerIteration = 0;
            while (true) {
                var useResidualWeights = (outerIteration > 0);

                for (int i = 0; i < this.fInnerIterations; ++i) {
                    this.SmoothSeasonalSubCycles(useResidualWeights);
                    this.RemoveSeasonality();
                    this.UpdateSeasonalAndTrend(useResidualWeights);
                }

                if (++outerIteration > fRobustIterations) {
                    break;
                }

                fDecomposition.ComputeResidualWeights();
            }

            fDecomposition.UpdateResiduals();

            Decomposition result = fDecomposition;

            fDecomposition = null;

            return result;
        }
        #endregion

        #region Private methods
        /// <summary>
        /// Compute the seasonal component by smoothing on the cyclic sub-series
        /// after removing the trend.
        /// </summary>
        /// <remarks>
        /// The current estimate of the trend is removed, then the detrended
        /// data is separated into sub-series (e.g. all the Januaries, all the
        /// Februaries, etc., for yearly data), and these sub-series are
        /// smoothed and extrapolated into <see cref="fExtendedSeasonal"/>.
        /// </remarks>
        /// <param name="useResidualWeights"></param>
        private void SmoothSeasonalSubCycles(bool useResidualWeights) {
            var data = this.fDecomposition.Data;
            var trend = this.fDecomposition.Trend;
            var weights = this.fDecomposition.Weights;

            for (int i = 0; i < data.Count; ++i) {
                this.fDetrend[i] = data[i] - trend[i];
            }

            var residualWeights = useResidualWeights ? weights : null;
            this.fCyclicSubSeriesSmoother.Smooth(fDetrend, fExtendedSeasonal,
                residualWeights);
        }

        /// <summary>
        /// The lowpass calculation takes the extended seasonal results and
        /// smoothes them with three moving averages and a  LOESS smoother to
        /// remove the seasonality.
        /// </summary>
        private void RemoveSeasonality() {
            // TODO: This creates some garbage - see if its a problem. If so we could preallocate these work arrays and
            // change the code to reuse them.

            // The moving average "erodes" data from the boundaries. We start with:
            //
            // extendedSeasonal.length == data.length + 2 * periodicity
            //
            // and the length after each pass is.................................
            double[] pass1 = this.fExtendedSeasonal.SimpleMovingAverage(
                this.fPeriodLength);    // data.length + periodLength + 1
            double[] pass2 = pass1.SimpleMovingAverage(this.fPeriodLength);// data.length + 2
            double[] pass3 = pass2.SimpleMovingAverage(3);  // data.length

            // assert pass3.length == fData.length; // testing sanity check.

            var lowPassLoess = this.fLowpassLoessFactory.Build(pass3);
            this.fDeSeasonalized = lowPassLoess.Smooth();
        }

        /// <summary>
        /// Computes the new seasonal component by removing the low
        /// pass-smoothed seasonality from the extended seasonality and the
        /// trend by subtracting this new seasonality from the data.
        /// </summary>
        /// <param name="useResidualWeights"></param>
        private void UpdateSeasonalAndTrend(bool useResidualWeights) {
            var data = this.fDecomposition.Data;
            var trend = this.fDecomposition.Trend;
            var weights = this.fDecomposition.Weights;
            var seasonal = this.fDecomposition.Seasonal;

            for (int i = 0; i < data.Count; ++i) {
                seasonal[i] = this.fExtendedSeasonal[fPeriodLength + i]
                    - this.fDeSeasonalized[i];
                trend[i] = data[i] - seasonal[i];
            }

            // dumpDebugData("seasonal", seasonal);
            // dumpDebugData("trend0", trend);

            var residualWeights = useResidualWeights ? weights : null;
            var trendSmoother = this.fLoessSmootherFactory
                .SetExternalWeights(residualWeights)
                .Build(trend);
            Array.Copy(trendSmoother.Smooth(), trend, trend.Length);
        }
        #endregion

        #region Private fields
        private IList<double> fData;

        private Decomposition fDecomposition;

        private int fPeriodLength;
        private LoessSettings fSeasonalSettings;
        private LoessSettings fTrendSettings;
        private LoessSettings fLowpassSettings;
        private int fInnerIterations;
        private int fRobustIterations;
        private double[] fDetrend;
        private double[] fExtendedSeasonal;
        private double[] fDeSeasonalized; // TODO: Garbage - can this be made in-place?
        private CyclicSubSeriesSmoother fCyclicSubSeriesSmoother;
        private LoessSmootherBuilder fLoessSmootherFactory;
        private LoessSmootherBuilder fLowpassLoessFactory;
        #endregion
    }
}
