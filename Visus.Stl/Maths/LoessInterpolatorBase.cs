using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Superclass for LOESS smoothing implementations of different degreee.
    /// </summary>
    /// <remarks>
    /// Roughly ported from the Java implementation at
    /// https://github.com/ServiceNow/stl-decomp-4j/blob/master/stl-decomp-4j/src/main/java/com/github/servicenow/ds/stats/stl/LoessInterpolator.java
    /// </remarks>
    public abstract class LoessInterpolatorBase {

        #region Public properties
        /// <summary>
        /// Gets the data to perform the interpolation on.
        /// </summary>
        public IList<double> Data {
            get;
        }

        /// <summary>
        /// Gets the degree of the LOESS smoother.
        /// </summary>
        public abstract int Degree {
            get;
            //set {
            //    if ((value < 0) || (value > 2)) {
            //        throw new ArgumentException($"{nameof(this.Degree)} must "
            //            + $"be within [0, 2], but is {value}.");
            //    }
            //    this._degree = value;
            //}
        }

        /// <summary>
        /// Gets the external weights.
        /// </summary>
        /// <remarks>
        /// This property is optional and all weight will be assumed to be 1 if
        /// it is <c>null</c>.
        /// </remarks>
        public IList<double> ExternalWeights {
            get;
        }

        /// <summary>
        /// Gets the smoothing width of the LOESS smoother.
        /// </summary>
        public int Width {
            get;
        }

        /// <summary>
        /// Gets the interpolation weights.
        /// </summary>
        public IList<double> Weights {
            get;
        }
        #endregion

        /**
         * Given a set of data on the regular grid {left, left+1, ..., right-1, right}, computed the LOESS-smoothed value at
         * the position x and return it. If the value can't be computed, return null.
         *
         * @param x
         *            double x-coordinate at which we want to compute an estimate of y
         * @param left
         *            int leftmost coordinate to use from the input data
         * @param right
         *            int rightmost coordinate to use from the input data
         * @return Double interpolated value, or null if interpolation could not be done
         */
        public double? Smooth(double x, int left, int right) {

            // Ordinarily, one doesn't do linear regression one x-value at a time, but LOESS does since
            // each x-value will typically have a different window. As a result, the weighted linear regression
            // is recast as a linear operation on the input data, weighted by this.fWeights.

            State state = this.ComputeNeighbourhoodWeights(x, left, right);

            if (state == State.WeightFailed) {
                return null;
            }

            if (state == State.LinearOK) {
                this.UpdateWeights(x, left, right);
            }

            double ys = 0.0;
            for (int i = left; i <= right; ++i) {
                ys += this.Weights[i] * this.Data[i];
            }

            return ys;
        }

        #region Protected constructors
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="width">The smoothing width.</param>
        /// <param name="data">The underlying data to be smoothed.</param>
        /// <param name="externalWeights">Optional external weights to be
        /// applied while smoothing.</param>
        protected LoessInterpolatorBase(int width, IList<double> data,
                IList<double> externalWeights) {
            this.Data = data ?? throw new ArgumentNullException(nameof(data));
            this.ExternalWeights = externalWeights;
            this.Width = width;
            this.Weights = new double[this.Data.Count];

            if ((this.ExternalWeights != null)
                    && (this.ExternalWeights.Count < this.Weights.Count)) {
                throw new ArgumentException($"{this.Data.Count} data points "
                    + $"have been provided, but the {nameof(externalWeights)} "
                    + "are less than that.", nameof(externalWeights));
            }
        }
        #endregion

        #region Protected methods
        /// <summary>
        /// Update the weights for the appropriate least-squares interpolation.
        /// </summary>
        /// <param name="x">The x-coordinate where to compute the estimate for
        /// y.</param>
        /// <param name="left">The leftmost coordinate to use from the input
        /// data.</param>
        /// <param name="right">The rightmode coordinate to use from the input
        /// data.</param>
        protected abstract void UpdateWeights(double x, int left, int right);
        #endregion

        private enum State {
            WeightFailed,
            LinearFailed,
            LinearOK
        }

        /// <summary>
        /// Compute the neighbourhood weights.
        /// </summary>
        /// <param name="x">The x-coordinate where to compute the estimate for
        /// y.</param>
        /// <param name="left">The leftmost coordinate to use from the input
        /// data.</param>
        /// <param name="right">The rightmode coordinate to use from the input
        /// data.</param>
        /// <returns>A state indicating whether we can do linear, moving average
        /// or nothing.</returns>
        private State ComputeNeighbourhoodWeights(double x, int left, int right) {
            double lambda = Math.Max(x - left, right - x);

            // Ordinarily, lambda ~ width / 2.
            //
            // If width > n, then we will only be computing with n points (i.e. left and right will always be in the
            // domain of 1..n) and the above calculation will give lambda ~ n / 2. We want the shape of the neighbourhood
            // weight function to be driven by width, not by the size of the domain, so we adjust lambda to be ~ width / 2.
            // (The paper does this by multiplying the above lambda by (width / n). Not sure why the code is different.)
            if (this.Width > this.Data.Count) {
                lambda += (double) ((this.Width - this.Data.Count) / 2);
            }

            // "Neighbourhood" is computed somewhat fuzzily.
            double l999 = 0.999 * lambda;
            double l001 = 0.001 * lambda;

            // Compute neighbourhood weights.
            double totalWeight = 0.0;
            for (int i = left; i <= right; ++i) {
                double delta = Math.Abs(x - i);

                // Compute the tri-cube neighbourhood weight.
                double weight = 0.0;
                if (delta <= l999) {
                    if (delta <= l001) {
                        weight = 1.0;
                    } else {
                        double fraction = delta / lambda;
                        weight = fraction.Tricube();
                    }

                    if (this.ExternalWeights != null) {
                        // If external weights are provided, apply them.
                        weight *= this.ExternalWeights[i];
                    }

                    totalWeight += weight;
                }

                this.Weights[i] = weight;
            }

            if (totalWeight <= 0.0) {
                // If the total weight is 0, we cannot proceed.
                return State.WeightFailed;
            }

            // Normalise the weights.
#if PARALLEL_LOESS
            Parallel.For(left, right, (i) => {
                this.Weights[i] /= totalWeight;
            });
#else // PARALLEL_LOESS
            for (int i = left; i <= right; ++i) {
                this.Weights[i] /= totalWeight;
            }
#endif // PARALLEL_LOESS

            return (lambda > 0) ? State.LinearOK : State.LinearFailed;
        }
    }
}
