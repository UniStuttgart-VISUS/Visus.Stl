// <copyright file="CyclicSubSeriesSmootherCompatTest.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Visus.Stl.Maths;


namespace Visus.Stl.Test {

    /// <summary>
    /// Compatibility tests from
    /// https://github.com/ServiceNow/stl-decomp-4j/blob/master/stl-decomp-4j/src/test/java/com/github/servicenow/ds/stats/stl/CyclicSubSeriesSmootherTest.java
    /// </summary>
    [TestClass]
    public sealed class CyclicSubSeriesSmootherCompatTest {
        // Smoothing the cyclic sub-series extends the data one period in each direction. Ensure that when the data is
        // linear, that the extrapolations are linear.

        [TestMethod]
        public void TrendingSinusoidExtrapolationTest() {
            int period = 24;
            double[] data = new double[2 * period];
            double dx = 2 * Math.PI / period;
            for (int i = 0; i < data.Length; ++i) {
                int amplitude = 10 - i / period;
                data[i] = amplitude * Math.Sin(i * dx);
            }

            double[] extendedData = new double[4 * period];

            var sssmoother = new CyclicSubSeriesSmoother(7, 1, 1, data.Length, period, 1, 1);
            sssmoother.Smooth(data, extendedData, null);

            for (int i = 0; i < extendedData.Length; ++i) {
                int amplitude = 11 - i / period; // An extra for the extrapolation before.
                double value = amplitude * Math.Sin(i * dx);
                Assert.AreEqual(extendedData[i], value, 1.0e-11, $"Test point {i}");
            }
        }

        [TestMethod]
        public void ShouldExtrapolateFourPeriodsForwards() {
            int period = 24;
            double[] data = new double[2 * period];
            double dx = 2 * Math.PI / period;
            for (int i = 0; i < data.Length; ++i) {
                int amplitude = 10 - i / period;
                data[i] = amplitude * Math.Sin(i * dx);
            }

            double[] extendedData = new double[6 * period];

            var sssmoother = new CyclicSubSeriesSmoother(7, 1, 1, data.Length, period, 0, 4);
            sssmoother.Smooth(data, extendedData, null);

            for (int i = 0; i < extendedData.Length; ++i) {
                int amplitude = 10 - i / period;
                double value = amplitude * Math.Sin(i * dx);
                Assert.AreEqual(extendedData[i], value, 1.0e-11, $"Test point {i}");
            }
        }

        [TestMethod]
        public void ShouldExtrapolateTwoPeriodsBackwardAndTwoPeriodsForward() {
            int period = 24;
            double[] data = new double[2 * period];
            double dx = 2 * Math.PI / period;
            for (int i = 0; i < data.Length; ++i) {
                int amplitude = 10 - i / period;
                data[i] = amplitude * Math.Sin(i * dx);
            }

            double[] extendedData = new double[6 * period];

            var sssmoother = new CyclicSubSeriesSmoother(7, 1, 1, data.Length, period, 2, 2);
            sssmoother.Smooth(data, extendedData, null);

            for (int i = 0; i < extendedData.Length; ++i) {
                int amplitude = 12 - i / period; // Two extra for the extrapolation before.
                double value = amplitude * Math.Sin(i * dx);
                Assert.AreEqual(extendedData[i], value, 1.0e-11, $"Test point {i}");
            }
        }

        [TestMethod]
        public void DegreeMustBePositive() {
            Assert.ThrowsException<ArgumentException>(() => {
                new CyclicSubSeriesSmoother(1, -1, 0, 10, 1, 0, 0);
            });
        }

        [TestMethod]
        public void DegreeMustBeLessThanThree() {
            Assert.ThrowsException<ArgumentException>(() => {
                new CyclicSubSeriesSmoother(1, 3, 0, 10, 1, 0, 0);
            });
        }

        //       @Test(expected = IllegalArgumentException.class)
        //public void widthMustBeSet() {
        //           Builder builder = new Builder();
        //           builder.setDataLength(100).extrapolateForwardAndBack(1).setPeriodicity(12).build();
        //       }

        //       @Test(expected = IllegalArgumentException.class)
        //public void dataLengthMustBeSet() {
        //           Builder builder = new Builder();
        //           builder.setWidth(3).extrapolateForwardAndBack(1).setPeriodicity(12).build();
        //       }

        //       @Test(expected = IllegalArgumentException.class)
        //public void periodMustBeSet() {
        //           Builder builder = new Builder();
        //           builder.setDataLength(100).extrapolateForwardAndBack(1).setWidth(11).build();
        //       }

        //       @Test(expected = IllegalArgumentException.class)
        //public void backwardExtrapolationMustBeSet() {
        //           Builder builder = new Builder();
        //           builder.setDataLength(100).setNumPeriodsForward(1).setWidth(11).setPeriodicity(12).build();
        //       }

        //       @Test(expected = IllegalArgumentException.class)
        //public void forwardExtrapolationMustBeSet() {
        //           Builder builder = new Builder();
        //           builder.setDataLength(100).setNumPeriodsBackward(1).setWidth(11).setPeriodicity(12).build();
        //       }
        //   }
    }
}