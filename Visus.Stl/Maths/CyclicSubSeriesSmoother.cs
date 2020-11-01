// <copyright file="CyclicSubSeriesSmoother.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Encapsulates the complexity of smoothing the cyclic sub-series.
    /// </summary>
    /// <remarks>
    /// Based on the Java implementation at
    /// https://github.com/ServiceNow/stl-decomp-4j/blob/master/stl-decomp-4j/src/main/java/com/github/servicenow/ds/stats/stl/CyclicSubSeriesSmoother.java
    /// </remarks>
    internal sealed class CyclicSubSeriesSmoother {

       //     /**
        //* Use Builder to simplify complex construction patterns.
        //*/
        //     public static class Builder {
        //         private Integer fWidth = null;
        //         private Integer fDataLength = null;
        //         private Integer fPeriodicity = null;
        //         private Integer fNumPeriodsBackward = null;
        //         private Integer fNumPeriodsForward = null;
        //         private int fDegree = 1;
        //         private int fJump = 1;

        //         /**
        // * Set the width of the LOESS smoother used to smooth each seasonal sub-series.
        // *
        // * @param width width of the LOESS smoother
        // * @return this
        // */
        //         public Builder setWidth(int width) {
        //             fWidth = width;
        //             return this;
        //         }

        //         /**
        // * Set the degree of the LOESS smoother used to smooth each seasonal sub-series.
        // *
        // * @param degree degree of the LOESS smoother
        // * @return this
        // */
        //         public Builder setDegree(int degree) {
        //             if (degree < 0 || degree > 2)
        //                 throw new IllegalArgumentException("Degree must be 0, 1 or 2");

        //             fDegree = degree;
        //             return this;
        //         }

        //         /**
        // * Set the jump (number of points to skip) between LOESS interpolations when smoothing the seasonal sub-series.
        // * <p>
        // * Defaults to 1 (computes LOESS interpolation at each point).
        // *
        // * @param jump jump (number of points to skip) in the LOESS smoother
        // * @return this
        // */
        //         public Builder setJump(int jump) {
        //             fJump = jump;
        //             return this;
        //         }

        //         /**
        // * Set the total length of the data that will be deconstructed into cyclic sub-series.
        // *
        // * @param dataLength total length of the data
        // * @return this
        // */
        //         public Builder setDataLength(int dataLength) {
        //             fDataLength = dataLength;
        //             return this;
        //         }

        //         /**
        // * Set the period of the data's seasonality.
        // *
        // * @param periodicity number of data points in each season or period
        // * @return this
        // */
        //         public Builder setPeriodicity(int periodicity) {
        //             fPeriodicity = periodicity;
        //             return this;
        //         }

        //         /**
        // * Construct a smoother that will extrapolate forward only by the specified number of periods.
        // *
        // * @param periods number of periods to extrapolate
        // * @return this
        // */
        //         public Builder extrapolateForwardOnly(int periods) {
        //             fNumPeriodsForward = periods;
        //             fNumPeriodsBackward = 0;
        //             return this;
        //         }

        //         /**
        // * Construct a smoother that extrapolates forward and backward by the specified number of periods.
        // *
        // * @param periods number of periods to extrapolate
        // * @return this
        // */
        //         public Builder extrapolateForwardAndBack(int periods) {
        //             fNumPeriodsForward = periods;
        //             fNumPeriodsBackward = periods;
        //             return this;
        //         }

        //         /**
        // * Set the number of periods to extrapolate forward.
        // * <p>
        // * Defaults to 1.
        // *
        // * @param periods number of periods to extrapolate
        // * @return this
        // */
        //         public Builder setNumPeriodsForward(int periods) {
        //             fNumPeriodsForward = periods;
        //             return this;
        //         }

        //         /**
        // * Set the number of periods to extrapolate backward.
        // * <p>
        // * Defaults to 1.
        // *
        // * @param periods number of periods to extrapolate
        // * @return this
        // */
        //         public Builder setNumPeriodsBackward(int periods) {
        //             fNumPeriodsBackward = periods;
        //             return this;
        //         }

        //         /**
        // * Build the sub-series smoother.
        // *
        // * @return new CyclicSubSeriesSmoother
        // */
        //         public CyclicSubSeriesSmoother build() {
        //             checkSanity();

        //             return new CyclicSubSeriesSmoother(fWidth, fDegree, fJump, fDataLength, fPeriodicity,
        //                     fNumPeriodsBackward, fNumPeriodsForward);
        //         }

        //         private void checkSanity() {
        //             if (fWidth == null)
        //                 throw new IllegalArgumentException(
        //                         "CyclicSubSeriesSmoother.Builder: setWidth must be called before building the smoother.");

        //             if (fPeriodicity == null)
        //                 throw new IllegalArgumentException(
        //                         "CyclicSubSeriesSmoother.Builder: setPeriodicity must be called before building the smoother.");

        //             if (fDataLength == null)
        //                 throw new IllegalArgumentException(
        //                         "CyclicSubSeriesSmoother.Builder: setDataLength must be called before building the smoother.");

        //             if (fNumPeriodsBackward == null || fNumPeriodsForward == null)
        //                 throw new IllegalArgumentException(
        //                         "CyclicSubSeriesSmoother.Builder: Extrapolation settings must be provided.");
        //         }
        //     }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="width">The width of the LOESS smoother.</param>
        /// <param name="degree">The degree of the LOESS smoother.</param>
        /// <param name="jump">Jump width to use in LOESS smoothing.</param>
        /// <param name="dataLength">Length of the input data.</param>
        /// <param name="periodicity">Length of the cycle.</param>
        /// <param name="backwardPeriods">Number of periods to extrapolate
        /// backward.</param>
        /// <param name="forwardPeriods">Number of peroids to extrapolate
        /// forward.</param>
        public CyclicSubSeriesSmoother(int width, int degree, int jump,
                int dataLength, int periodicity, int backwardPeriods,
                int forwardPeriods) {
            Contract.Assert(dataLength >= 0);
            Contract.Assert(periodicity >= 0);
            Contract.Assert(backwardPeriods >= 0);
            Contract.Assert(forwardPeriods >= 0);

            this._backwardPeriods = backwardPeriods;
            this._cntPeriods = dataLength / periodicity;
            this._forwardPeriods = forwardPeriods;
            this._periodicity = periodicity;
            this._rawCyclicSubSeries = new double[periodicity][];
            this._remainder = dataLength % periodicity;
            this._smoothedCyclicSubSeries = new double[periodicity][];
            this._smootherFactory = new LoessSmootherBuilder()
                .SetWidth(width)
                .SetJump(jump)
                .SetDegree(degree);
            this._width = width;
            this._subSeriesWeights = new double[periodicity][];

            // Bookkeeping: Write the data length as
            //
            // n = m * periodicity + r
            //
            // where r < periodicity. The first r sub-series will have length
            // m + 1 and the remaining will have length m. Another way to look
            // at this is that the cycle length is
            //
            // cycleLength = (n - p - 1) / periodicity + 1
            //
            // where p is the index of the cycle that we're currently in.
            for (int p = 0; p < this._periodicity; ++p) {
                int seriesLength = (p < this._remainder)
                    ? (this._cntPeriods + 1)
                    : this._cntPeriods;
                this._rawCyclicSubSeries[p] = new double[seriesLength];
                this._smoothedCyclicSubSeries[p] = new double[_backwardPeriods
                    + seriesLength + _forwardPeriods];
                this._subSeriesWeights[p] = new double[seriesLength];
            }
        }

        /// <summary>
        ///  Run the cyclic sub-series smoother on the specified data, with the
        ///  specified weights (ignored if <c>null</c>). The  sub-series are
        ///  reconstructed into a single series in <paramref name="smoothedData"/>.
        /// </summary>
        /// <param name="rawData">The input data.</param>
        /// <param name="smoothedData">Receives the output data.</param>
        /// <param name="weights">Weights to use in the underlying interpolator;
        /// ignored if null.</param>
        public void Smooth(double[] rawData, double[] smoothedData,
                double[] weights) {
            this.ExtractRawSubSeriesAndWeights(rawData, weights);
            this.ComputeSmoothedSubSeries(weights != null);
            this.ReconstructExtendedDataFromSubSeries(smoothedData);
        }

        #region Private methods
        private void ComputeSmoothedSubSeries(bool useResiduals) {
            for (int p = 0; p < this._periodicity; ++p) {
                var weights = useResiduals ? this._subSeriesWeights[p] : null;
                var rawData = this._rawCyclicSubSeries[p];
                var smoothedData = this._smoothedCyclicSubSeries[p];
                this.SmoothSubSeries(weights, rawData, smoothedData);
            }
        }

        private void ExtractRawSubSeriesAndWeights(IList<double> data,
                IList<double> weights) {
            Contract.Assert(data != null);

            for (int p = 0; p < this._periodicity; ++p) {
                var cycleLength = (p < this._remainder)
                    ? (_cntPeriods + 1)
                    : _cntPeriods;

                for (int i = 0; i < cycleLength; ++i) {
                    this._rawCyclicSubSeries[p][i]
                        = data[i * this._periodicity + p];

                    if (weights != null) {
                        this._subSeriesWeights[p][i]
                            = weights[i * this._periodicity + p];
                    }
                }
            }
        }

        /// <summary>
        /// Copy smoothed cyclic sub-series to the extendedSeasonal work array.
        /// </summary>
        /// <param name="data"></param>
        private void ReconstructExtendedDataFromSubSeries(IList<double> data) {
            Contract.Assert(data != null);

            for (int p = 0; p < this._periodicity; ++p) {
                var cycleLength = (p < this._remainder)
                    ? (this._cntPeriods + 1)
                    : this._cntPeriods;

                for (int i = 0; i < this._backwardPeriods + cycleLength
                        + this._forwardPeriods; ++i) {
                    data[i * _periodicity + p]
                        = this._smoothedCyclicSubSeries[p][i];
                }
            }
        }

        /// <summary>
        /// Use LOESS interpolation on each of the cyclic sub-series (e.g. in
        /// monthly data, smooth the Januaries, Februaries,  etc.).
        /// </summary>
        /// <param name="weights">External weights for interpolation.</param>
        /// <param name="rawData">Input data to be smoothed.</param>
        /// <param name="smoothedData">Receives the smoothed data.</param>
        private void SmoothSubSeries(double[] weights,
                double[] rawData, double[] smoothedData) {
            Contract.Assert(rawData != null);
            Contract.Assert(smoothedData != null);
            var cycleLength = rawData.Length;

            // Smooth the cyclic sub-series with LOESS and then extrapolate one
            // place beyond each end.
            var smoother = this._smootherFactory
                .SetExternalWeights(weights)
                .Build(rawData);

            // Copy, shifting by 1 to leave room for the extrapolated point at
            // the beginning.
            Array.Copy(smoother.Smooth(), 0, smoothedData,
                this._backwardPeriods, cycleLength);

            var interpolator = smoother.Interpolator;

            // Extrapolate from the leftmost "width" points to the "-1" position
            int left = 0;
            int right = left + _width - 1;
            right = Math.Min(right, cycleLength - 1);
            int leftValue = this._backwardPeriods;

            for (int i = 1; i <= this._backwardPeriods; i++) {
                var ys = interpolator.Smooth(-i, left, right);
                smoothedData[leftValue - i] = ys ?? smoothedData[leftValue];
            }

            // Extrapolate from the rightmost "width" points to the "length"
            // position (one past the array end).
            right = cycleLength - 1;
            left = right - _width + 1;
            left = Math.Max(0, left);
            int rightValue = _backwardPeriods + right;

            for (int i = 1; i <= _forwardPeriods; i++) {
                var ys = interpolator.Smooth(right + i, left, right);
                smoothedData[rightValue + i] = ys ?? smoothedData[rightValue];
            }
        }
        #endregion

        #region private fields
        private readonly int _backwardPeriods;
        private readonly int _cntPeriods;
        private readonly int _forwardPeriods;
        private readonly int _periodicity;
        private readonly double[][] _rawCyclicSubSeries;
        private readonly int _remainder;
        private readonly double[][] _smoothedCyclicSubSeries;
        private readonly LoessSmootherBuilder _smootherFactory;
        private readonly double[][] _subSeriesWeights;
        private readonly int _width;
        #endregion
    }
}
