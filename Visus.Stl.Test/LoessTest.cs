// <copyright file="LoessTest.cs" company="Universität Stuttgart">
// Copyright © 2020 Visualisierungsinstitut der Universität Stuttgart. All rights reserved.
// </copyright>
// <author>Christoph Müller</author>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Visus.Stl.Maths;


namespace Visus.Stl.Test {

    [TestClass]
    public sealed class LoessTest {

        [TestMethod]
        public void TestDegreeBuilt() {
            var data = new[] { 1.0, 2.0, 3.0 };

            {
                var interpolator = new LoessInterpolatorBuilder()
                    .SetDegree(0)
                    .SetWidth(1)
                    .Build(data);
                Assert.AreEqual(typeof(FlatLoessInterpolator), interpolator.GetType());
            }

            {
                var interpolator = new LoessInterpolatorBuilder()
                    .SetDegree(1)
                    .SetWidth(1)
                    .Build(data);
                Assert.AreEqual(typeof(LinearLoessInterpolator), interpolator.GetType());
            }

            {
                var interpolator = new LoessInterpolatorBuilder()
                    .SetDegree(2)
                    .SetWidth(1)
                    .Build(data);
                Assert.AreEqual(typeof(QuadraticLoessInterpolator), interpolator.GetType());
            }

            Assert.ThrowsException<ArgumentException>(() => {
                var interpolator = new LoessInterpolatorBuilder()
                    .SetDegree(3)
                    .SetWidth(1)
                    .Build(data);
            });

        }
    }
}
