using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;


namespace Visus.Stl.Maths {

    public sealed class LinearLoessInterpolator : LoessInterpolatorBase {

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="width">The smoothing width.</param>
        /// <param name="data">The underlying data to be smoothed.</param>
        /// <param name="externalWeights">Optional external weights to be
        /// applied while smoothing.</param>
        public LinearLoessInterpolator(int width, IList<double> data,
                IList<double> externalWeights)
            : base(width, data, externalWeights) { }

        public override int Degree => 1;

        protected override void UpdateWeights(double x, int left, int right) {
            double xMean = 0.0;
            for (int i = left; i <= right; ++i) {
                xMean += i * this.Weights[i];
            }

            double x2Mean = 0.0;
            for (int i = left; i <= right; ++i) {
                double delta = i - xMean;
                x2Mean += this.Weights[i] * delta.Square();
            }

            // Finding y(x) from the least-squares fit can be cast as a linear operation on the input data.
            // This is implemented by updating the weights to include the least-squares weighting of the points.
            // Note that this is only done if the points are spread out enough (variance > (0.001 * range)^2)
            // to compute a slope. If not, we leave the weights alone and essentially fall back to a moving
            // average of the data based on the neighborhood and external weights.

            double range = this.Data.Count - 1;
            if (x2Mean > 0.000001 * range.Square()) {
                double beta = (x - xMean) / x2Mean;
#if PARALLEL_LOESS
                Parallel.For(left, right, (i) => {
                    this.Weights[i] *= (1.0 + beta * (i - xMean));
                });
#else // PARALLEL_LOESS
                for (int i = left; i <= right; ++i) {
                    this.Weights[i] *= (1.0 + beta * (i - xMean));
                }
#endif // PARALLEL_LOESS
            }
        }

    }
}
