// <copyright file="LoessSettings.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;


namespace Visus.Stl.Maths {

    /// <summary>
    /// Configuration data for the LOESS smoothers.
    /// </summary>
    public sealed class LoessSettings {

        #region Public constructors
        /// <summary>
        /// Initialises a new instance.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="degree"></param>
        /// <param name="jump"></param>
        public LoessSettings(int width, int degree, int jump) {
            AdjustWidth(ref width);
            this.Degree = degree.Clamp(0, 2);
            this.Jump = Math.Max(1, jump);
            this.Width = width;
        }

        /// <summary>
        /// Initialises a new instance with a jump of 10% of the smoothing
        /// width.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="degree"></param>
        public LoessSettings(int width, int degree) {
            AdjustWidth(ref width);
            this.Degree = degree.Clamp(0, 2);
            this.Jump = Math.Max(1, (int) (0.1 * width + 0.9));
            this.Width = width;
        }

        /// <summary>
        /// Create a new instance with linear degree and a jump of 10% of the
        /// smoothing width.
        /// </summary>
        /// <param name="width"></param>
        public LoessSettings(int width) : this(width, 1) { }
        #endregion

        #region Public properties
        /// <summary>
        /// Gets the degree of the LOESS smoother.
        /// </summary>
        public int Degree {
            get;
        }

        /// <summary>
        /// Gets the width of the jumps used between LOESS interpolations.
        /// </summary>
        public int Jump {
            get;
        }

        /// <summary>
        /// Gets the width of the LOESS smoother.
        /// </summary>
        public int Width {
            get;
        }
        #endregion

        #region Private class methods
        private static void AdjustWidth(ref int width) {
            width = Math.Max(3, width);
            if (width % 2 == 0) {
                ++width;
            }
        }
        #endregion
    }
}
