// <copyright file="MathsTest.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Visus.Stl.Maths;


namespace Visus.Stl.Test {

    /// <summary>
    /// Performs test for the underlying maths operations.
    /// </summary>
    [TestClass]
    public class MathsTest {

        [TestMethod]
        public void TestCube() {
            Assert.AreEqual(-1.0, (-1.0).Cube());
            Assert.AreEqual(1.0, 1.0.Cube());
            Assert.AreEqual(-8.0, (-2.0).Cube());
            Assert.AreEqual(8.0, 2.0.Cube());
        }

        [TestMethod]
        public void TestMedian() {
            {
                var list = new List<int> { 1, 2, 3 };
                Assert.AreEqual(2, list.Median());
            }

            {
                var list = new List<int> { 2, 1, 3 };
                Assert.AreEqual(2, list.Median());
            }

            {
                var list = new List<int> { 1, 3, 2 };
                Assert.AreEqual(2, list.Median());
            }

            {
                var list = new List<int> { 1 };
                Assert.AreEqual(1, list.Median());
            }
        }

        [TestMethod]
        public void TestSquare() {
            Assert.AreEqual(1.0, (-1.0).Square());
            Assert.AreEqual(1.0, 1.0.Square());
            Assert.AreEqual(4.0, (-2.0).Square());
            Assert.AreEqual(4.0, 2.0.Square());
        }

        [TestMethod]
        public void TestSwap() {
            {
                var list = new List<int> { 1, 2, 3 };
                list.Swap(0, 1);
                Assert.AreEqual(2, list[0]);
                Assert.AreEqual(1, list[1]);
                Assert.AreEqual(3, list[2]);
            }

            {
                var list = new List<int> { 1, 2, 3 };
                list.Swap(0, 2);
                Assert.AreEqual(3, list[0]);
                Assert.AreEqual(2, list[1]);
                Assert.AreEqual(1, list[2]);
            }


            {
                var list = new List<int> { 1, 2, 3 };
                list.Swap(0, 0);
                Assert.AreEqual(1, list[0]);
                Assert.AreEqual(2, list[1]);
                Assert.AreEqual(3, list[2]);
            }
        }

        [TestMethod]
        public void TestTricube() {
            Assert.AreEqual(0.0, (-1.0).Tricube());
            Assert.AreEqual(1.0, 0.0.Tricube());
            Assert.AreEqual(0.0, 1.0.Tricube());
            Assert.AreEqual(0.6699, 0.5.Tricube(), 0.0001);
        }
    }
}
