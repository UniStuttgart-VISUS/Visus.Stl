// <copyright file="SeasonalTrendLoessBuilder.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Visus.Stl.Maths;


namespace Visus.Stl {

    /// <summary>
    /// Fluent API for creating <see cref="SeasonalTrendLoess"/>.
    /// </summary>
    public sealed class SeasonalTrendLoessBuilder {

        /// <summary>
        /// Construct the smoother.
        /// </summary>
        /// <param name="data">The data to be smoothed.</param>
        /// <returns>A new smoother.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="data"/>
        /// is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">If <paramref name="data"/> does
        /// not fit the configured periodicity.</exception>
        /// <exception cref="InvalidOperationException">If any of the required
        /// parameters have not been set or an invalid value.</exception>
        public SeasonalTrendLoess Build(IList<double> data) {
            // Sanity checks.
            _ = data ?? throw new ArgumentNullException(nameof(data));
            _ = this._periodLength ?? throw new InvalidOperationException(
                $"{nameof(this.SetPeriodic)} must be set before calling "
                + $"{nameof(this.Build)}.");

            if (data.Count < 2 * this._periodLength) {
                throw new ArgumentException($"{nameof(data)} must hold at "
                    + $"least {2 * this._periodLength} elements.",
                    nameof(data));
            }

            if (this._isPeriodic) {
                var massiveWidth = 100 * data.Count;
                var isConsistent = (this._seasonalDegree != null)
                    && (this._seasonalWidth != null)
                    && (this._seasonalWidth == massiveWidth)
                    && (this._seasonalDegree == 0);

                if ((this._seasonalDegree != null) && !isConsistent) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetSeasonalDegree)} and "
                        + $"{nameof(this.SetPeriodic)} cannot be both called.");
                }

                if (this._seasonalJump != null) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetSeasonalJump)} and "
                        + $"{nameof(this.SetPeriodic)} cannot be both called.");
                }

                if ((this._seasonalWidth != null) && !isConsistent) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetSeasonalWidth)} and "
                        + $"{nameof(this.SetPeriodic)} cannot be both called.");
                }

            } else {
                if (this._seasonalWidth == null) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetSeasonalWidth)} must be called if "
                        + $"{nameof(this.SetPeriodic)} was not called.");
                }
            } /* end if (this._isPeriodic) */

            if (_isFlatTrend) {
                var massiveWidth = 100 * this._periodLength * data.Count;
                var isConsistent = (this._trendWidth != null)
                    && (this._trendDegree != null)
                    && (this._trendWidth == massiveWidth)
                    && (this._trendDegree == 0);

                if ((this._trendDegree != null) && !isConsistent) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetTrendDegree)} and "
                        + $"{nameof(this.SetFlatTrend)} cannot be both called.");
                }

                if (this._trendJump != null) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetTrendJump)} and "
                        + $"{nameof(this.SetFlatTrend)} cannot be both called.");
                }

                if ((this._trendWidth != null) && !isConsistent) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetTrendWidth)} and "
                        + $"{nameof(this.SetFlatTrend)} cannot be both called.");
                }
            }

            if (this._isLinearTrend) {
                var massiveWidth = 100 * this._periodLength * data.Count;
                var isConsistent = (this._trendWidth != null)
                    && (this._trendDegree != null)
                    && (this._trendWidth == massiveWidth)
                    && (this._trendDegree == 1);

                if ((this._trendDegree != null) && !isConsistent) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetTrendDegree)} and "
                        + $"{nameof(this.SetLinearTrend)} cannot be both called.");
                }

                if (this._trendJump != null) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetTrendJump)} and "
                        + $"{nameof(this.SetLinearTrend)} cannot be both called.");
                }

                if ((this._trendWidth != null) && !isConsistent) {
                    throw new InvalidOperationException(
                        $"{nameof(this.SetTrendWidth)} and "
                        + $"{nameof(this.SetLinearTrend)} cannot be both called.");
                }
            }

            // Apply defaults
            if (this._isPeriodic) {
                this._seasonalWidth = 100 * data.Count;
                this._seasonalDegree = 0;

            } else if (this._seasonalDegree == null) {
                this._seasonalDegree = 1;
            }

            Debug.Assert(this._seasonalWidth != null);
            Debug.Assert(this._seasonalDegree != null);
            var seasonalSettings = BuildSettings(this._seasonalWidth.Value,
                this._seasonalDegree.Value, this._seasonalJump);

            if (this._isFlatTrend) {
                this._trendWidth = 100 * this._periodLength * data.Count;
                this._trendDegree = 0;

            } else if (this._isLinearTrend) {
                this._trendWidth = 100 * this._periodLength * data.Count;
                this._trendDegree = 1;

            } else if (this._trendDegree == null) {
                this._trendDegree = 1;
            }

            if (this._trendWidth == null) {
                Debug.Assert(this._periodLength != null);
                Debug.Assert(this._seasonalWidth != null);
                this._trendWidth = CalcDefaultTrendWidth(
                    this._periodLength.Value, this._seasonalWidth.Value);
            }

            var trendSettings = BuildSettings(this._trendWidth.Value,
                this._trendDegree.Value, this._trendJump);

            if (this._lowpassWidth == null) {
                this._lowpassWidth = this._periodLength;
            }

            var lowpassSettings = BuildSettings(this._lowpassWidth.Value,
                this._lowpassDegree, this._lowpassJump);

            // Create the STL instance.
            Debug.Assert(this._periodLength != null);
            return new SeasonalTrendLoess(data, this._periodLength.Value,
                this._innerIterations, this._robustIterations, seasonalSettings,
                trendSettings, lowpassSettings);
        }

        /// <summary>
        /// Force a float trend when smoothing.
        /// </summary>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetFlatTrend() {
            this._isLinearTrend = false;
            this._isFlatTrend = true;
            return this;
        }

        /// <summary>
        /// Set the number of inner iterations.
        /// </summary>
        /// <remarks>
        /// The number of inner iterations defaults to 2.
        /// </remarks>
        /// <param name="innerIterations">The number of inner iterations.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetInnerIterations(
                int innerIterations) {
            this._innerIterations = innerIterations;
            return this;
        }

        /// <summary>
        /// Enables or disabes robustness.
        /// </summary>
        /// <param name="robust"><c>true</c> to set the parameters to default
        /// robust STL, <c>false</c> to set it to non-robust parameters.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetIsRobust(bool isRobust) {
            return isRobust ? this.SetRobust() : this.SetNonRobust();
        }

        /// <summary>
        /// Force a linear trend when smoothing.
        /// </summary>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetLinearTrend() {
            this._isFlatTrend = false;
            this._isLinearTrend = true;
            return this;
        }

        /// <summary>
        /// Sets the LOESS degree used for the low pass step.
        /// </summary>
        /// <remarks>
        /// This value defaults to 1.
        /// </remarks>
        /// <param name="degree">The degree of the low pass step.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetLowpassDegree(int degree) {
            this._lowpassDegree = degree;
            return this;
        }

        /// <summary>
        /// Sets the number of data points to skip between LOESS interpolations
        /// used by the low pass step.
        /// </summary>
        /// <remarks>
        /// If not set, the value will be set to 10% of the smoother width.
        /// </remarks>
        /// <param name="jump">The number of data points to be skipped.</param>
        /// <returns></returns>
        public SeasonalTrendLoessBuilder SetLowpassJump(int jump) {
            this._lowpassJump = jump;
            return this;
        }

        /// <summary>
        /// Sets the LOESS width (in data points) used for the low pass step.
        /// </summary>
        /// <remarks>
        /// If not set, the period length is used.
        /// </remarks>
        /// <param name="width">The width in data points.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetLowpassWidth(int width) {
            this._lowpassWidth = width;
            return this;
        }

        /// <summary>
        /// Set the iteration count for non-robust STL.
        /// </summary>
        /// <remarks>
        /// The outer iterations will be set to zero, the inner iterationts to
        /// 2.
        /// </remarks>
        /// <returns><c>this</c></returns>
        public SeasonalTrendLoessBuilder SetNonRobust() {
            this._innerIterations = 2;
            this._robustIterations = 0;
            return this;
        }

        /// <summary>
        /// Sets the number of STL robustness (outer) iterations.
        /// </summary>
        /// <param name="robustIterations">The number of outer iterations.
        /// </param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetOuterIterations(
                int robustIterations) {
            this._robustIterations = robustIterations;
            return this;
        }

        /// <summary>
        /// Constrain the seasonal component to be exactly periodic.
        /// </summary>
        /// <returns></returns>
        public SeasonalTrendLoessBuilder SetPeriodic() {
            this._isPeriodic = true;
            return this;
        }

        /// <summary>
        /// Set the period length for the STL seasonal decomposition.
        /// </summary>
        /// <param name="period">The number of data points in seach season.
        /// </param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetPeriodLength(int period) {
            if (period < 2) {
                throw new ArgumentException($"{nameof(period)} must be at "
                    + $"least 2, but is {period}.", nameof(period));
            }

            this._periodLength = period;
            return this;
        }

        /// <summary>
        /// Set the default robust STL iteration counts.
        /// </summary>
        /// <remarks>
        /// This method sets 15 outer iterations and 1 inner iteration.
        /// </remarks>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetRobust() {
            this._innerIterations = 1;
            this._robustIterations = 15;
            return this;
        }

        /// <summary>
        /// Sets the number of STL robustness (outer) iterations.
        /// </summary>
        /// <param name="robustIterations">The number of outer iterations.
        /// </param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetRobustnessIterations(
                int robustIterations) {
            this._robustIterations = robustIterations;
            return this;
        }

        /// <summary>
        /// Sets the LOESS degree used for seasonal sub-series.
        /// </summary>
        /// <remarks>
        /// This value defaults to 1.
        /// </remarks>
        /// <param name="degree">The degree of seasonal sub-series.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetSeasonalDegree(int degree) {
            this._seasonalDegree = degree;
            return this;
        }

        /// <summary>
        /// Sets the number of data points to skip between LOESS interpolations
        /// used for seasonal sub-series.
        /// </summary>
        /// <remarks>
        /// If not set, the value will be set to 10% of the smoother width.
        /// </remarks>
        /// <param name="jump">The number of data points to be skipped.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetSeasonalJump(int jump) {
            this._seasonalJump = jump;
            return this;
        }

        /// <summary>
        /// Sets the LOESS width (in data points) used for the seasonal
        /// sub-series.
        /// </summary>
        /// <remarks>
        /// This value is required unless <see cref="SetPeriodic"/> is set.
        /// </remarks>
        /// <param name="width">The width in data points.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetSeasonalWidth(int width) {
            this._seasonalWidth = width;
            return this;
        }

        /// <summary>
        /// Set the LOESS degree used to smooth the trend.
        /// </summary>
        /// <remarks>
        /// This value defaults to 1.
        /// </remarks>
        /// <param name="degree">The degree for smoothing the trend.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetTrendDegree(int degree) {
            this._trendDegree = degree;
            return this;
        }

        /// <summary>
        /// Sets the number of data points to skip between LOESS interpolations
        /// used to smooth the tren.
        /// </summary>
        /// <remarks>
        /// If not set, the value will be set to 10% of the smoother width.
        /// </remarks>
        /// <param name="jump">The number of data points to be skipped.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetTrendJump(int jump) {
            this._trendJump = jump;
            return this;
        }

        /// <summary>
        /// Set the LOESS width (in data points) used to smooth the trend.
        /// </summary>
        /// <remarks>
        /// Defaults to (1.5 * periodLength / (1 - 1.5 / seasonalWidth) + 0.5).
        /// </remarks>
        /// <param name="width">LOESS width for the trend component.</param>
        /// <returns><c>this</c>.</returns>
        public SeasonalTrendLoessBuilder SetTrendWidth(int width) {
            this._trendWidth = width;
            return this;
        }

        #region Private class methods
        private static LoessSettings BuildSettings(int width, int degree,
                int? jump) {
            if (jump == null) {
                return new LoessSettings(width, degree);
            } else {
                return new LoessSettings(width, degree, jump.Value);
            }
        }

        private static int CalcDefaultTrendWidth(int periodicity,
                int seasonalWidth) {
            // This formula is based on a numerical stability analysis in the
            // original paper.
            return (int) (1.5f * periodicity / (1.0f - 1.5f / seasonalWidth)
                + 0.5f);
        }
        #endregion

        #region Private fields
        private int? _periodLength;

        private int? _seasonalWidth;
        private int? _seasonalJump;
        private int? _seasonalDegree;

        private int? _trendWidth;
        private int? _trendJump;
        private int? _trendDegree;

        private int? _lowpassWidth;
        private int? _lowpassJump;
        private int _lowpassDegree = 1;

        // Following the R interface, we default to "non-robust"

        private int _innerIterations = 2;
        private int _robustIterations = 0;

        // Following the R interface, we implement a "periodic" flag that defaults to false.

        private bool _isPeriodic = false;
        private bool _isFlatTrend = false;
        private bool _isLinearTrend = false;
        #endregion
    }
}
