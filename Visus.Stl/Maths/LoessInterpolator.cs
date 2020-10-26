// <copyright file="LoessInterpolator.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Dominik Herr, Christoph Müller</author>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Implementation of the Local Regression Algorithm (locally estimated
    /// scatterplot smoothing).
    /// </summary>
    /// <remarks>
    /// <para>This implementation derived from
    /// https://github.com/apache/commons-math/blob/master/src/main/java/org/apache/commons/math4/analysis/interpolation/LoessInterpolator.java
    /// and Dominik Herr's code.</para>
    /// </remarks>
    public sealed class LoessInterpolator {

        #region Public constants
        /// <summary>
        /// The default value for accuracy.
        /// </summary>
        public const double DefaultAccuracy = 1e-12;

        /// <summary>
        /// The default value of the bandwith parameter.
        /// </summary>
        public const double DefaultBandwidth = 0.3;

        /// <summary>
        /// The default value fo the number of robustness iterations.
        /// </summary>
        public const int DefaultRobustnessIterations = 1;
        #endregion

        #region Public constructors
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        public LoessInterpolator() : this(DefaultBandwidth,
            DefaultRobustnessIterations) { }

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="bandwidth"></param>
        /// <param name="robustnessIterations"></param>
        public LoessInterpolator(double? bandwidth, int robustnessIterations) {
            if ((bandwidth < 0) || (bandwidth > 1)) {
                throw new ArgumentException("The bandwidth must be within "
                    + $"[0, 1], but actually is {bandwidth}.", nameof(bandwidth));
            }
            if (robustnessIterations < 0) {
                throw new ArgumentException("The number of robustnessIterations "
                    + "must be positive, but actually is "
                    + $"{robustnessIterations}.", nameof(robustnessIterations));
            }

            this._bandwidth = bandwidth;
            this._robustnessIterations = robustnessIterations;
        }
        #endregion


        /// <summary>
        /// Compute a loess fit on the data at the original abscissae.
        /// </summary>
        /// <param name="xval">the arguments for the interpolation points</param>
        /// <param name="yval">the values for the interpolation points</param>
        /// <param name="q">values of the loess fit at corresponding original abscissae</param>
        /// <param name="jumpsize">TODO NOT YET IMPLEMENTED - using non-optimized solution</param>
        /// <returns></returns>
        public double[] Smooth(long[] xval, double[] yval, double[] robustnessWeights = null, int q = -1, int? jumpsize = 1) {
            if (xval.Length != yval.Length) {
                throw new ArgumentException(
                    $"Loess expects the abscissa and ordinate arrays to be of the same size, but got {xval.Length} abscisssae and {yval.Length} ordinatae");
            }

            int n = xval.Length;

            if (n == 0) {
                throw new ApplicationException("Loess expects at least 1 point");
            }


            //            CheckAllFiniteReal(xval, true);
            CheckAllFiniteReal(yval, false);
            CheckStrictlyIncreasing(xval);

            if (n == 1) {
                return new double[] { yval[0] };
            }


            if (n == 2) {
                return new double[] { yval[0], yval[1] };
            }

            if (q == -1) {
                if (_bandwidth == null) {
                    throw new ArgumentNullException("The loess interpolator was initialized without a bandwidth and no bandwidth in points was given.");
                }
                Debug.WriteLine("Info: Loess was initialized with a bandwidth and not with a static amount of points");
                q = (int) (_bandwidth * n);
            }

            if (q < 2) {
                throw new ArgumentException(
                    $"the bandwidth must be large enough to accomodate at least 2 points. There are {n} " +
                    $" data points, and bandwidth must be at least {2.0 / n} but it is only {_bandwidth}");
            }

            // all jumps are the same, so calculating just one jump value should be sufficient
            //            int ntJump = 1, nsJump = 1, nlJump = 1;
            //            if (jumpsize != 1)
            //            {
            ////                var jump = (int) Math.Ceiling((double)nt/10D);
            ////                ntJump = jump;
            ////                nsJump = jump;
            ////                nlJump = jump;
            //            }

            double[] res = new double[n];

            //            double[] residuals = new double[n];
            //            double[] sortedResiduals = new double[n];

            //            double[] robustnessWeights = new double[n];
            if (robustnessWeights == null) {
                robustnessWeights = new double[n];
                for (int i = 0; i < robustnessWeights.Length; i++)
                    robustnessWeights[i] = 1;
            } else if (robustnessWeights.Length != n)
                throw new ArgumentOutOfRangeException();

            int[] bandwidthInterval = { 0, (q <= xval.Length - 1) ? q - 1 : xval.Length - 1 };
            // At each x, compute a local weighted linear regression
            //            for (int i = 0; i < n; ++i)
            //            {
            Parallel.For(0, n, (i) => {
                var x = xval[i];

                // Find out the interval of source points on which
                // a regression is to be made.
                var localbandwidthInterval = bandwidthInterval;
                if (i > 0) {
                    localbandwidthInterval = UpdateBandwidthInterval(xval, i, bandwidthInterval);
                }

                int ileft = localbandwidthInterval[0];
                int iright = localbandwidthInterval[1];

                // Compute the point of the bandwidth interval that is
                // farthest from x
                //                int edge;
                //                if (xval[i] - xval[ileft] > xval[iright] - xval[i])
                //                {
                //                    edge = ileft;
                //                }
                //                else
                //                {
                //                    edge = iright;
                //                }

                // Compute a least-squares linear fit weighted by
                // the product of robustness weights and the tricube
                // weight function.
                // See http://en.wikipedia.org/wiki/Linear_regression
                // (section "Univariate linear case")
                // and http://en.wikipedia.org/wiki/Weighted_least_squares
                // (section "Weighted least squares")
                double sumWeights = 0;
                double sumX = 0, sumXSquared = 0, sumY = 0, sumXY = 0;
                // denom = lambda_q = (q <= n) ? dist(x_q, x) : lambda_q(n)*q/n
                //                    double lambdaQ = Math.Abs(1.0/(xval[edge] - x));
                double lambdaQ = CalculateLambda(q, xval, i);
                for (int k = ileft; k <= iright; ++k) {
                    // xk = kth farthest element from x
                    double xk = xval[k];
                    double yk = yval[k];
                    double dist;
                    dist = Math.Abs(x - xk);
                    //                    if (k < i)
                    //                    {
                    //                        dist = (x - xk);
                    //                    }
                    //                    else
                    //                    {
                    //                        dist = (xk - x);
                    //                    }
                    // W = tricube function
                    // v_i = W(|x_i - x| / lambda_q)
                    double vi = Tricube(dist / lambdaQ) * robustnessWeights[k];
                    // the following assumes a linear regression of first order and uses simple regression
                    // need: average y, average x, sum of xiyi, sum of x, sum of y, sum of xi^2
                    double xkw = xk * vi; // weighted xi
                    sumWeights += vi;
                    sumX += xkw;    // sum of weighted xi -> used for average xi
                    sumXSquared += xk * xkw;  //sum of weighted xi^2 -> xi*xi*wi
                    sumY += yk * vi;  // weighted yi -> used for average yi
                    sumXY += yk * xkw;
                }

                // simple regression model - see: https://en.wikipedia.org/wiki/Ordinary_least_squares#Simple_regression_model
                // need: average y, average x, sum of xiyi, sum of x, sum of y, sum of xi^2
                // further: average x, average y

                double meanX = sumX / sumWeights;
                double meanY = sumY / sumWeights;
                double meanXY = sumXY / sumWeights;
                double meanXSquared = sumXSquared / sumWeights;

                double beta;
                //                if (meanXSquared == meanX*meanX)
                //                {
                //                    beta = 0;
                //                }
                //                else
                //                {
                var denom = (sumXSquared - sumX * sumX / sumWeights);
                beta = (denom == 0) ? 0 : (sumXY - sumX * sumY / sumWeights) / denom;
                //                    beta = (meanXY - meanX*meanY)/(meanXSquared - meanX*meanX);
                //                }

                double alpha = meanY - beta * meanX;

                res[i] = beta * x + alpha;
                //                residuals[i] = Math.Abs(yval[i] - res[i]);
            }
            );

            return res;
        }

        /// <summary>
        /// 
        /// Calculates lambda_q, which is 
        /// (length of series > q) the distance of the qth farthest point from x
        /// (else) [maxDistance of x to any end of the series]*n/q
        /// 
        /// </summary>
        /// <param name="q"></param>
        /// <param name="xSeries"></param>
        /// <param name="xIndex"></param>
        /// <returns></returns>
        private double CalculateLambda(int q, long[] xSeries, long xIndex) {
            if (q < 0) {
                throw new ArgumentOutOfRangeException(nameof(q), "q must be positive");
            }
            double lambdaQ;
            var q2 = q / 2D;
            if (q <= xSeries.Length - 1) {
                // three cases: (1) we are too far left (no more items to the left), (2) we are too far on the right (no more items to the tight), (3) we are centered
                // case 1: too far left
                if (xIndex < q2) {
                    // how many fit in the short direction?
                    var offset = q - xIndex;
                    // calculate how many points work in both ways; then go the rest to go backwards
                    lambdaQ = Math.Abs(xSeries[xIndex] - xSeries[xIndex + offset]);
                } else {
                    // case 2: too far right
                    if (xIndex > xSeries.Length - 1 - q2) {
                        // how many fit in the short direction?
                        var offset = q - (xSeries.Length - 1 - xIndex);
                        // calculate how many points work in both ways; then go the rest to go backwards
                        lambdaQ = Math.Abs(xSeries[xIndex] - xSeries[xIndex - offset]);
                    }
                    // case 3: centered
                    else {
                        lambdaQ = Math.Abs(xSeries[xIndex] - xSeries[xIndex + (int) Math.Ceiling(q / 2D)]);
                    }
                }

                //                // check if straightforward approach will work...
                //                if (xIndex + (int) Math.Ceiling(q/2D) < xSeries.Length)
                //                {
                //                    lambdaQ = Math.Abs(xSeries[xIndex] - xSeries[xIndex + (int) Math.Ceiling(q/2D)]);
                //                }
                //                else
                //                {
                //                    // ... otherwise calculate how many points to go backwards
                //                    var bothDir = ((xSeries.Length - 1) - xIndex);
                //                    lambdaQ = Math.Abs(xSeries[xIndex] - xSeries[xIndex - bothDir - (q - bothDir*2)]);
                //                }
            } else {
                lambdaQ = CalculateLambda(xSeries.Length - 1, xSeries, xIndex) * ((double) q / (xSeries.Length - 1));
            }
            return lambdaQ;
        }

        /// <summary>
        /// Check that all elements of an array are finite real numbers.
        /// </summary>
        /// <param name="values">the values array</param>
        /// <param name="isAbscissae">isAbscissae if true, elements are abscissae otherwise they are ordinatae</param>
        private static void CheckAllFiniteReal(double[] values, bool isAbscissae) {
            for (int i = 0; i < values.Length; i++) {
                double x = values[i];
                if (double.IsInfinity(x) || double.IsNaN(x)) {
                    string pattern = isAbscissae
                        ? "all abscissae must be finite real numbers, but {0}-th is {1}"
                        : "all ordinatae must be finite real numbers, but {0}-th is {1}";
                    throw new ArgumentException("one of the values is not a finite real number.",
                        new ArgumentException(string.Format(pattern, i, x)));
                }
            }
        }

        /// <summary>
        /// Check that elements of the abscissae array are in a strictly
        /// increasing order.
        /// </summary>
        /// <param name="xval">xval the abscissae array</param>
        private static void CheckStrictlyIncreasing(long[] xval) {
            for (int i = 0; i < xval.Length; ++i) {
                if (i >= 1 && xval[i - 1] >= xval[i]) {
                    throw new ArgumentException("the abscissae array must be sorted in a strictly " +
                                                $"increasing order, but the {i - 1}-th element is {xval[i - 1]} " +
                                                $"whereas {i}-th is {xval[i]}");
                }
            }
        }

        /// <summary>
        /// Compute the 
        /// <a href="http://en.wikipedia.org/wiki/Local_regression#Weight_function">tricube</a>
        /// weight function.
        /// </summary>
        /// <param name="x">the argument</param>
        /// <returns>(1-|x|^3)^3</returns>
        private static double Tricube(double x) {
            if (Math.Abs(x) < 1D)
                return Math.Pow(Math.Pow(1D - x, 3), 3);
            else {
                return 0;
            }
            //            double tmp = 1D - x*x*x;
            //            return tmp*tmp*tmp;
        }

        /// <summary>
        /// Given an index interval into xval that embraces a certain number of
        /// points closest to xval[i-1], update the interval so that it embraces
        /// the same number of points closest to xval[i]
        /// </summary>
        /// <param name="xval">arguments array.</param>
        /// <param name="i">the index around which the new interval should be computed.</param>
        /// <param name="bandwidthInterval">a two-element array {left, right} such that: <para/>
        /// <tt>(left==0 or xval[i] - xval[left-1] > xval[right] - xval[i])</tt>
        /// <para/> and also <para/>
        /// <tt>(right==xval.length-1 or xval[right+1] - xval[i] > xval[i] - xval[left])</tt>.
        /// The array will be updated.</param>
        private int[] UpdateBandwidthInterval(long[] xval, int i, int[] bandwidthInterval) {
            int left = bandwidthInterval[0];
            int right = bandwidthInterval[1];
            // The right edge should be adjusted if the next point to the right
            // is closer to xval[i] than the leftmost point of the current interval
            while (right < xval.Length - 1 &&
                xval[right + 1] - xval[i] < xval[i] - xval[left]) {
                left++;
                right++;
            }
            int[] res = { left, right };
            return res;
        }

        #region Private fields
        /// <summary>
        /// The bandwidth parameter.
        /// </summary>
        /// <remarks>
        /// <para>When computing the Loess fit at a particular point, this
        /// fraction of source points closest to the current point is taken into
        /// account for computing a least-squares regression.</para>
        /// <para>A sensible value is usually 0.25 to 0.5.</para>
        /// </remarks>
        private readonly double? _bandwidth;

        /// <summary>
        /// The number of robustness iterations to be performed.
        /// </summary>
        /// <remarks>
        /// A sensible value is usually 0 (just the initial fit without any
        /// robustness iterations) to 4.
        /// </remarks>
        private readonly int _robustnessIterations;
        #endregion

    }
}
