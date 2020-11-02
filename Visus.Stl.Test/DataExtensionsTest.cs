// <copyright file="DataExtensionsTest.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Dominik Herr, Christoph Müller</author>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using Visus.Stl.Data;


namespace Visus.Stl.Test {

    /// <summary>
    /// Tests for <see cref="Visus.Stl.Data.Extensions"/>.
    /// </summary>
    [TestClass]
    public sealed class DataExtensionsTest {

        [TestMethod]
        public void TestSpaceEvenly() {
            var input = new[] {
                new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 0), 0),
                new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 1), 1),
                new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 2), 2),
                new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 3), 2),
                new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 2, 3), 1),
                new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 5), 5),
            };

            var output = input.SpaceEvenly(TimeSpan.FromSeconds(1), (l, r) => l + r, 0).ToArray();

            Assert.AreEqual(6, output.Length);
            Assert.AreEqual(0, output[0]);
            Assert.AreEqual(1, output[1]);
            Assert.AreEqual(2, output[2]);
            Assert.AreEqual(3, output[3]);
            Assert.AreEqual(0, output[4]);
            Assert.AreEqual(5, output[5]);
        }

        [TestMethod]
        public void TestSpaceEvenlyEmpty() {
            {
                var output = Array.Empty<DateTimePoint<int>>().SpaceEvenly(TimeSpan.FromSeconds(1), (l, r) => l + r, 0).ToArray();
                Assert.AreEqual(0, output.Length);
            }

            {
                var output = Extensions.SpaceEvenly(null, TimeSpan.FromSeconds(1), (l, r) => l + r, 0).ToArray();
                Assert.AreEqual(0, output.Length);
            }
        }

        [TestMethod]
        public void TestSpaceEvenlySingle() {
            var input = new[] { new DateTimePoint<int>(new DateTime(2020, 1, 1, 0, 0, 0), 42) };
            var output = input.SpaceEvenly(TimeSpan.FromSeconds(1), (l, r) => l + r, 0).ToArray();

            Assert.AreEqual(1, output.Length);
            Assert.AreEqual(42, output[0]);
        }
    }
}
