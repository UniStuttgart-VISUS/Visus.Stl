// <copyright file="SimpleMovingAverageCompatTest.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Visus.Stl.Maths;


namespace Visus.Stl.Test {

    /// <summary>
    /// Test for simple moving average being compatible with
    /// https://github.com/ServiceNow/stl-decomp-4j/blob/master/stl-decomp-4j/src/test/java/com/github/servicenow/ds/stats/TimeSeriesUtilitiesTest.java
    /// </summary>
    [TestClass]
    public sealed class SimpleMovingAverageCompatTest {

        [TestMethod]
        public void SmaWithWindowEqualLengthIsJustAverage() {
            var rng = new Random();
            var length = rng.Next(1, 1000);

            double[] data = CreateRandomArray(length);

            double sum = 0.0;
            for (int i = 0; i < data.Length; ++i) {
                sum += data[i];
            }

            double mean = sum / data.Length;

            double[] average = data.SimpleMovingAverage(data.Length);

            Assert.AreEqual(1, average.Length);
            Assert.AreEqual(mean, average[0], 1.0e-10);
        }

        [TestMethod]
        public void SmaWithWindowEqualOneIsJustData() {
            double[] data = CreateRandomArray(10);

            double[] average = data.SimpleMovingAverage(1);

            Assert.AreEqual(data.Length, average.Length);

            for (int i = 0; i < data.Length; ++i) {
                Assert.AreEqual(data[i], average[i], 1.0e-10);
            }
        }

        [TestMethod]
        public void SmaRandomDataTest() {
            var rng = new Random();
            var length = rng.Next(1, 1000);

            double[] data = CreateRandomArray(length);

            int window = rng.Next(length);
            window = Math.Max(window, 2);
            window = Math.Min(window, length);

            double[] average = data.SimpleMovingAverage(window);

            Assert.AreEqual(data.Length - window + 1, average.Length);

            for (int i = 0; i < average.Length; ++i) {
                double sum = 0.0;
                for (int j = 0; j < window; ++j) {
                    sum += data[i + j];
                }
                double mean = sum / window;

                Assert.AreEqual(mean, average[i], 1.0e-10);
            }
        }

        private static double[] CreateRandomArray(int length) {
            var retval = new double[length];
            var rng = new Random();

            for (int i = 0; i < retval.Length; ++i) {
                retval[i] = rng.NextDouble() * 100 - 50; // [-50..50];
            }

            return retval;
        }
    }
}
