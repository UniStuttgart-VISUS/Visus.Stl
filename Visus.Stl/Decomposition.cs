// <copyright file="Decomposition.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;
using System.Collections.Generic;
using Visus.Stl.Maths;


namespace Visus.Stl {

    /// <summary>
    /// Holds the result of an STL decomposition.
    /// </summary>
    public sealed class Decomposition {

        #region Public properties
        /// <summary>
        /// Gets the original data that have been decomposed.
        /// </summary>
        public IList<double> Data {
            get;
        }

        /// <summary>
        /// Gets the residual remaining after removing the seasonality and the
        /// trend from <see cref="Data"/>.
        /// </summary>
        public double[] Residuals {
            get;
        }

        /// <summary>
        /// Gets the seasonal component of the decomposition.
        /// </summary>
        public double[] Seasonal {
            get;
        }

        /// <summary>
        /// Gets the trans component of the decomposition.
        /// </summary>
        public double[] Trend {
            get;
        }

        /// <summary>
        /// Gets the robustness weights used in the calculation.
        /// </summary>
        /// <remarks>
        /// Places where the weights are near zero indicate outliers that were
        /// effectively ignored during the decomposition. Only applicable if
        /// robustness iterations are performed.
        /// </remarks>
        public double[] Weights {
            get;
        }
        #endregion

        #region Internal constructors
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="data">The data to be decomposed.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="data"/>
        /// is <c>null</c>.</exception>
        internal Decomposition(IList<double> data) {
            this.Data = data ?? throw new ArgumentNullException(nameof(data));

            int size = this.Data.Count;
            this.Residuals = new double[size];
            this.Seasonal = new double[size];
            this.Trend = new double[size];
            this.Weights = new double[size];

            Array.Fill(this.Weights, 1);
        }
        #endregion

        #region Internal methods
        /// <summary>
        /// Compute the residual-based weights used in the robustness
        /// iterations.
        /// </summary>
        internal void ComputeResidualWeights() {
            // TODO: There can be problems if "robust" iterations are done but
            // MAD ~= 0. May want to put a floor on c001.

            // The residual-based weights are a "bisquare" weight based on the
            // residual deviation compared to 6 times the median absolute
            // deviation (MAD). First compute 6 * MAD. (The sort could be a
            // selection but this is not critical as the rest of the algorithm 
            // is higher complexity.)
            for (int i = 0; i < this.Data.Count; ++i) {
                this.Weights[i] = Math.Abs(this.Data[i] - this.Seasonal[i]
                    - this.Trend[i]);
            }

            Array.Sort(this.Weights);

            // For an even number of elements, the median is the average of the
            // middle two. With proper indexing this formula works either way at
            // the cost of some superfluous work when the number is odd.
            int mi0 = (this.Data.Count + 1) / 2 - 1;    // n = 5, mi0 = 2; n = 4, mi0 = 1
            int mi1 = this.Data.Count - mi0 - 1;        // n = 5, mi1 = 2; n = 4, mi1 = 2

            double sixMad = 3.0 * (this.Weights[mi0] + this.Weights[mi1]);
            double c999 = 0.999 * sixMad;
            double c001 = 0.001 * sixMad;

            for (int i = 0; i < this.Data.Count; ++i) {
                var r = Math.Abs(this.Data[i] - this.Seasonal[i]
                    - this.Trend[i]);
                if (r <= c001) {
                    this.Weights[i] = 1.0;

                } else if (r <= c999) {
                    var h = r / sixMad;
                    var w = 1.0 - h.Square();
                    this.Weights[i] = w.Square();

                } else {
                    this.Weights[i] = 0.0;
                }
            }
        }

        /// <summary>
        /// Updates the <see cref="Residuals"/> by subtracting the seasonal and
        /// trend components from the data.
        /// </summary>
        internal void UpdateResiduals() {
            for (int i = 0; i < this.Data.Count; ++i)
                this.Residuals[i] = this.Data[i] - this.Seasonal[i]
                    - this.Trend[i];
        }

        /// <summary>
        /// Smooth the STL seasonal component with quadratic LOESS and recompute
        /// the residual.
        /// </summary>
        /// <param name="width">The width of the LOESS smoother.</param>
        /// <param name="restoreEndPoints">Indicates whether the end points
        /// should be restored to their original values. This parameter defaults
        /// to <c>true</c>.</param>
        internal void smoothSeasonal(int width, bool restoreEndPoints = true) {
            // Ensure that LOESS smoother width is odd and >= 3.
            width = Math.Max(3, width);
            if (width % 2 == 0) {
                ++width;
            }

            // Quadratic smoothing of the seasonal component.
            // Do NOT perform linear interpolation between smoothed points - the quadratic spline can accommodate
            // sharp changes and linear interpolation would cut off peaks/valleys.
            var builder = new LoessSmootherBuilder()
                .SetWidth(width)
                .SetDegree(2)
                .SetJump(1);

            var seasonalSmoother = builder.Build(this.Seasonal);
            var smoothedSeasonal = seasonalSmoother.Smooth();

            // TODO: Calculate the variance reduction in smoothing the seasonal.

            // Update the seasonal with the smoothed values.

            // TODO: This is not very good - it causes discontinuities a the endpoints.
            //       Better to transition to linear in the last half-smoother width.

            // Restore the end-point values as the smoother will tend to over-modify these.

            double s0 = this.Seasonal[0];
            double sN = this.Seasonal[this.Seasonal.Length - 1];
            Array.Copy(smoothedSeasonal, this.Seasonal, smoothedSeasonal.Length);

            if (restoreEndPoints) {
                this.Seasonal[0] = s0;
                this.Seasonal[this.Seasonal.Length - 1] = sN;
            }

            for (int i = 0; i < smoothedSeasonal.Length; ++i) {
                this.Residuals[i] = this.Data[i] - this.Trend[i]
                    - this.Seasonal[i];
            }
        }
        #endregion
    }
}
