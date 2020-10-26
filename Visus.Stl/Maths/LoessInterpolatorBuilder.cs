// <copyright file="LoessInterpolatorBuilder.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Fluid API factory for LOESS smoothers.
    /// </summary>
    /// <remarks>
    /// Roughly ported from the Java implementation at
    /// https://github.com/ServiceNow/stl-decomp-4j/blob/master/stl-decomp-4j/src/main/java/com/github/servicenow/ds/stats/stl/LoessInterpolator.java
    /// </remarks>
    public class LoessInterpolatorBuilder {

        /// <summary>
        /// Creates the interpolator for the given data.
        /// </summary>
        /// <remarks>
        /// At least <see cref="SetWidth(int)"/> must have been called before
        /// the interpolator can be built.
        /// </remarks>
        /// <param name="data">The data toe be interpolated. This must not
        /// be <c>null</c>.</param>
        /// <returns>An interpolator for the gvien data.</returns>
        /// <exception cref="InvalidOperationException">If the width has
        /// not been set.</exception>
        public LoessInterpolatorBase Build(IList<double> data) {
            _ = this._width ?? throw new InvalidOperationException(
                $"{nameof(this.SetWidth)} must be called before "
                + $"{nameof(this.Build)} can be called.");

            switch (this._degree) {
                case 0:
                    return new FlatLoessInterpolator(this._width.Value,
                        data, this._externalWeights);

                case 1:
                    return new LinearLoessInterpolator(this._width.Value,
                        data, this._externalWeights);

                case 2:
                    return new QuadraticLoessInterpolator(this._width.Value,
                        data, this._externalWeights);

                default:
                    Debug.Assert(false);
                    return null;
            }
        }

        /// <summary>
        /// Sets the degreee of the interpolator to be built.
        /// </summary>
        /// <param name="degree">The degree, which must be within [0, 2.</param>
        /// <returns><c>this</c>.</returns>
        public LoessInterpolatorBuilder SetDegree(int degree) {
            if ((degree < 0) || (degree > 2)) {
                throw new ArgumentException($"{nameof(degree)} must be within "
                    + $"[0, 2], but is {degree}.", nameof(degree));
            }

            this._degree = degree;
            return this;
        }

        /// <summary>
        /// Sets the optional external weightst to be used.
        /// </summary>
        /// <param name="externalWeights">The external weights. It is safe to
        /// pass <c>null</c>.</param>
        /// <returns><c>this</c>.</returns>
        public LoessInterpolatorBuilder SetExternalWeights(
                IList<double> externalWeights) {
            this._externalWeights = externalWeights;
            return this;
        }

        /// <summary>
        /// Sets the width of the interpolator.
        /// </summary>
        /// <param name="width">The width of the interpolator.</param>
        /// <returns><c>this</c>.</returns>
        public LoessInterpolatorBuilder SetWidth(int width) {
            this._width = width;
            return this;
        }

        #region Private fields
        private int _degree = 1;
        private IList<double> _externalWeights;
        private int? _width;
        #endregion
    }
}
