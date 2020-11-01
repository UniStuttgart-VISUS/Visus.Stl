// <copyright file="QuadraticLoessInterpolator.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System.Collections.Generic;


namespace Visus.Stl.Maths {

    public sealed class QuadraticLoessInterpolator : LoessInterpolatorBase {

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="width">The smoothing width.</param>
        /// <param name="data">The underlying data to be smoothed.</param>
        /// <param name="externalWeights">Optional external weights to be
        /// applied while smoothing. This parameter defaults to
        /// <c>null</c>.</param>
        public QuadraticLoessInterpolator(int width, IList<double> data,
                IList<double> externalWeights = null)
            : base(width, data, externalWeights) { }

        /// <inheritdoc />
        public override int Degree => 2;

        /// <inheritdoc />
        protected override void UpdateWeights(double x, int left, int right) {
            double x1Mean = 0.0;
            double x2Mean = 0.0;
            double x3Mean = 0.0;
            double x4Mean = 0.0;
            for (int i = left; i <= right; ++i) {
                double w = this.Weights[i];
                double x1w = i * w;
                double x2w = i * x1w;
                double x3w = i * x2w;
                double x4w = i * x3w;
                x1Mean += x1w;
                x2Mean += x2w;
                x3Mean += x3w;
                x4Mean += x4w;
            }

            double m2 = x2Mean - x1Mean * x1Mean;
            double m3 = x3Mean - x2Mean * x1Mean;
            double m4 = x4Mean - x2Mean * x2Mean;

            double denominator = m2 * m4 - m3 * m3;
            double range = this.Data.Count - 1;

            if (denominator > 0.000001 * range * range) {
                // TODO: Are there cases where denominator is too small but m2 is not too small?
                // In that case, it would make sense to fall back to linear regression instead of falling back to just the
                // weighted average.
                double beta2 = m4 / denominator;
                double beta3 = m3 / denominator;
                double beta4 = m2 / denominator;

                double x1 = x - x1Mean;
                double x2 = x * x - x2Mean;

                double a1 = beta2 * x1 - beta3 * x2;
                double a2 = beta4 * x2 - beta3 * x1;

                for (int i = left; i <= right; ++i) {
                    this.Weights[i] *= (1 + a1 * (i - x1Mean) + a2 * (i * i - x2Mean));
                }
            }
        }
    }
}
