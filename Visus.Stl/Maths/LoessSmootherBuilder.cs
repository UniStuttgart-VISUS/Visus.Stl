// <copyright file="LoessSmootherBuilder.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;
using System.Collections.Generic;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Fluid API factory for LOESS smoothers.
    /// </summary>
    public sealed class LoessSmootherBuilder {

        #region Public methods
        /// <summary>
        /// Build the <see cref="LoessSmoother"/>.
        /// </summary>
        /// <returns>A new <see cref="LoessSmoother"/> for the given
        /// data.</returns>
        public LoessSmoother Build(IList<double> data) {
            _ = this._width ?? throw new InvalidOperationException(
                $"{nameof(this.SetWidth)} must be called before "
                + $"{nameof(this.Build)} can be called.");
            return new LoessSmoother(this._width.Value, this._jump,
                this._degree, data, this._externalWeights);
        }

        /// <summary>
        /// Sets the degreee of the interpolator to be built.
        /// </summary>
        /// <param name="degree">The degree, which must be within
        /// [0, 2].</param>
        /// <returns><c>this</c>.</returns>
        public LoessSmootherBuilder SetDegree(int degree) {
            if ((degree < 0) || (degree > 2)) {
                throw new ArgumentException($"{nameof(degree)} must be within "
                    + $"[0, 2], but is {degree}.", nameof(degree));
            }

            this._degree = degree;
            return this;
        }

        /// <summary>
        /// Sets the optional external weights to be used.
        /// </summary>
        /// <param name="externalWeights">The external weights. It is safe to
        /// pass <c>null</c>.</param>
        /// <returns><c>this</c>.</returns>
        public LoessSmootherBuilder SetExternalWeights(
                IList<double> externalWeights) {
            this._externalWeights = externalWeights;
            return this;
        }

        /// <summary>
        /// Set the number of points to skip between LOESS interpolations.
        /// </summary>
        /// <param name="jump">The number of points to skip.</param>
        /// <returns><c>this</c>.</returns>
        public LoessSmootherBuilder SetJump(int jump) {
            this._jump = jump;
            return this;
        }

        /// <summary>
        /// Applies <see cref="LoessSettings.Degree"/> and
        /// <see cref="LoessSettings.Width"/> from <paramref name="settings"/>.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public LoessSmootherBuilder SetSettings(LoessSettings settings) {
            _ = settings ?? throw new ArgumentNullException(nameof(settings));
            this._degree = settings.Degree;
            this._width = settings.Width;
            return this;
        }

        /// <summary>
        /// Sets the width of the interpolator.
        /// </summary>
        /// <param name="width">The width of the interpolator.</param>
        /// <returns><c>this</c>.</returns>
        public LoessSmootherBuilder SetWidth(int width) {
            this._width = width;
            return this;
        }
        #endregion

        #region Private fields
        private int _degree = 1;
        private IList<double>_externalWeights;
        private int _jump = 1;
        private int? _width;
        #endregion
    }
}
