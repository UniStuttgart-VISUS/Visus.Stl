// <copyright file="FlatLoessInterpolator.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System.Collections.Generic;


namespace Visus.Stl.Maths {

    public sealed class FlatLoessInterpolator : LoessInterpolatorBase {

        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="width">The smoothing width.</param>
        /// <param name="data">The underlying data to be smoothed.</param>
        /// <param name="externalWeights">Optional external weights to be
        /// applied while smoothing.</param>
        public FlatLoessInterpolator(int width, IList<double> data,
                IList<double> externalWeights)
            : base(width, data, externalWeights) { }

        /// <inheritdoc />
        public override int Degree => 0;

        protected override void UpdateWeights(double x, int left, int right) {
            // Nothing to do.
        }
    }
}
