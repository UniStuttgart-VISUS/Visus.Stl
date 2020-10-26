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
    }
}
