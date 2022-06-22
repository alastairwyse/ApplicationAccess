/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;

namespace ApplicationAccess.Utilities.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Utilities.MappingMetricLogger class.
    /// </summary>
    public class MappingMetricLoggerTests
    {
        private IMetricLogger mockDownstreamMetricLogger;
        private MappingMetricLogger testMappingMetricLogger;

        [SetUp]
        protected void SetUp()
        {
            mockDownstreamMetricLogger = Substitute.For<IMetricLogger>();
            testMappingMetricLogger = new MappingMetricLogger(mockDownstreamMetricLogger);
        }

        [Test]
        public void AddCountMetricMapping_CountMetricTypeParameterNotCountMetric()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.AddCountMetricMapping(typeof(MappedToAmountMetric), new MappedToCountMetric());
            });

            Assert.That(e.Message, Does.StartWith($"Type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToAmountMetric' in parameter 'countMetricType' is not assignable to 'ApplicationMetrics.CountMetric'."));
            Assert.AreEqual("countMetricType", e.ParamName);
        }

        [Test]
        public void AddAmountMetricMapping_AmountMetricTypeParameterNotAmountMetric()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.AddAmountMetricMapping(typeof(MappedToStatusMetric), new MappedToAmountMetric());
            });

            Assert.That(e.Message, Does.StartWith($"Type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToStatusMetric' in parameter 'amountMetricType' is not assignable to 'ApplicationMetrics.AmountMetric'."));
            Assert.AreEqual("amountMetricType", e.ParamName);
        }

        [Test]
        public void AddStatusMetricMapping_StatuMetricTypeParameterNotStatusMetric()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.AddStatusMetricMapping(typeof(MappedToIntervalMetric), new MappedToStatusMetric());
            });

            Assert.That(e.Message, Does.StartWith($"Type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToIntervalMetric' in parameter 'statusMetricType' is not assignable to 'ApplicationMetrics.StatusMetric'."));
            Assert.AreEqual("statusMetricType", e.ParamName);
        }

        [Test]
        public void AddIntervalMetricMapping_IntervalMetricTypeParameterNotIntervalMetric()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.AddIntervalMetricMapping(typeof(MappedToCountMetric), new MappedToIntervalMetric());
            });

            Assert.That(e.Message, Does.StartWith($"Type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToCountMetric' in parameter 'intervalMetricType' is not assignable to 'ApplicationMetrics.IntervalMetric'."));
            Assert.AreEqual("intervalMetricType", e.ParamName);
        }

        [Test]
        public void Increment_MappingDoesntExist()
        {
            testMappingMetricLogger.AddCountMetricMapping(typeof(MappedFromCountMetric), new MappedToCountMetric());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.Increment(new MappedToCountMetric());
            });

            Assert.That(e.Message, Does.StartWith($"No mapping exists for metric of type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToCountMetric' in parameter 'countMetric'."));
            Assert.AreEqual("countMetric", e.ParamName);
        }

        [Test]
        public void Increment()
        {
            testMappingMetricLogger.AddCountMetricMapping(typeof(MappedFromCountMetric), new MappedToCountMetric());

            testMappingMetricLogger.Increment(new MappedFromCountMetric());

            mockDownstreamMetricLogger.Received(1).Increment(Arg.Any<MappedToCountMetric>());
        }

        [Test]
        public void Add_MappingDoesntExist()
        {
            testMappingMetricLogger.AddAmountMetricMapping(typeof(MappedFromAmountMetric), new MappedToAmountMetric());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.Add(new MappedToAmountMetric(), 123);
            });

            Assert.That(e.Message, Does.StartWith($"No mapping exists for metric of type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToAmountMetric' in parameter 'amountMetric'."));
            Assert.AreEqual("amountMetric", e.ParamName);
        }

        [Test]
        public void Add()
        {
            testMappingMetricLogger.AddAmountMetricMapping(typeof(MappedFromAmountMetric), new MappedToAmountMetric());

            testMappingMetricLogger.Add(new MappedFromAmountMetric(), 123);

            mockDownstreamMetricLogger.Received(1).Add(Arg.Any<MappedToAmountMetric>(), 123);
        }

        [Test]
        public void Set_MappingDoesntExist()
        {
            testMappingMetricLogger.AddStatusMetricMapping(typeof(MappedFromStatusMetric), new MappedToStatusMetric());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.Set(new MappedToStatusMetric(), 456);
            });

            Assert.That(e.Message, Does.StartWith($"No mapping exists for metric of type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToStatusMetric' in parameter 'statusMetric'."));
            Assert.AreEqual("statusMetric", e.ParamName);
        }

        [Test]
        public void Set()
        {
            testMappingMetricLogger.AddStatusMetricMapping(typeof(MappedFromStatusMetric), new MappedToStatusMetric());

            testMappingMetricLogger.Set(new MappedFromStatusMetric(), 456);

            mockDownstreamMetricLogger.Received(1).Set(Arg.Any<MappedToStatusMetric>(), 456);
        }

        [Test]
        public void Begin_MappingDoesntExist()
        {
            testMappingMetricLogger.AddIntervalMetricMapping(typeof(MappedFromIntervalMetric), new MappedToIntervalMetric());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.Begin(new MappedToIntervalMetric());
            });

            Assert.That(e.Message, Does.StartWith($"No mapping exists for metric of type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToIntervalMetric' in parameter 'intervalMetric'."));
            Assert.AreEqual("intervalMetric", e.ParamName);
        }

        [Test]
        public void Begin()
        {
            testMappingMetricLogger.AddIntervalMetricMapping(typeof(MappedFromIntervalMetric), new MappedToIntervalMetric());

            testMappingMetricLogger.Begin(new MappedFromIntervalMetric());

            mockDownstreamMetricLogger.Received(1).Begin(Arg.Any<MappedToIntervalMetric>());
        }

        [Test]
        public void End_MappingDoesntExist()
        {
            testMappingMetricLogger.AddIntervalMetricMapping(typeof(MappedFromIntervalMetric), new MappedToIntervalMetric());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.End(new MappedToIntervalMetric());
            });

            Assert.That(e.Message, Does.StartWith($"No mapping exists for metric of type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToIntervalMetric' in parameter 'intervalMetric'."));
            Assert.AreEqual("intervalMetric", e.ParamName);
        }

        [Test]
        public void End()
        {
            testMappingMetricLogger.AddIntervalMetricMapping(typeof(MappedFromIntervalMetric), new MappedToIntervalMetric());

            testMappingMetricLogger.End(new MappedFromIntervalMetric());

            mockDownstreamMetricLogger.Received(1).End(Arg.Any<MappedToIntervalMetric>());
        }

        [Test]
        public void CancelBegin_MappingDoesntExist()
        {
            testMappingMetricLogger.AddIntervalMetricMapping(typeof(MappedFromIntervalMetric), new MappedToIntervalMetric());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMappingMetricLogger.CancelBegin(new MappedToIntervalMetric());
            });

            Assert.That(e.Message, Does.StartWith($"No mapping exists for metric of type 'ApplicationAccess.Utilities.UnitTests.MappingMetricLoggerTests+MappedToIntervalMetric' in parameter 'intervalMetric'."));
            Assert.AreEqual("intervalMetric", e.ParamName);
        }

        [Test]
        public void CancelBegin()
        {
            testMappingMetricLogger.AddIntervalMetricMapping(typeof(MappedFromIntervalMetric), new MappedToIntervalMetric());

            testMappingMetricLogger.CancelBegin(new MappedFromIntervalMetric());

            mockDownstreamMetricLogger.Received(1).CancelBegin(Arg.Any<MappedToIntervalMetric>());
        }

        #region Test Metric Classes

        public class MappedFromCountMetric : CountMetric
        {
            protected static String staticName = "MappedFromCountMetric";
            protected static String staticDescription = "Test mapped 'from' CountMetric";

            public MappedFromCountMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }

        public class MappedToCountMetric : CountMetric
        {
            protected static String staticName = "MappedToCountMetric";
            protected static String staticDescription = "Test mapped 'to' CountMetric";

            public MappedToCountMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }

        public class MappedFromAmountMetric : AmountMetric
        {
            protected static String staticName = "MappedFromAmountMetric";
            protected static String staticDescription = "Test mapped 'from' AmountMetric";

            public MappedFromAmountMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }

        public class MappedToAmountMetric : AmountMetric
        {
            protected static String staticName = "MappedToAmountMetric";
            protected static String staticDescription = "Test mapped 'to' AmountMetric";

            public MappedToAmountMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }

        public class MappedFromStatusMetric : StatusMetric
        {
            protected static String staticName = "MappedFromStatusMetric";
            protected static String staticDescription = "Test mapped 'from' StatusMetric";

            public MappedFromStatusMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }

        public class MappedToStatusMetric : StatusMetric
        {
            protected static String staticName = "MappedToStatusMetric";
            protected static String staticDescription = "Test mapped 'to' StatusMetric";

            public MappedToStatusMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }

        public class MappedFromIntervalMetric : IntervalMetric
        {
            protected static String staticName = "MappedFromIntervalMetric";
            protected static String staticDescription = "Test mapped 'from' IntervalMetric";

            public MappedFromIntervalMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }

        public class MappedToIntervalMetric : IntervalMetric
        {
            protected static String staticName = "MappedToIntervalMetric";
            protected static String staticDescription = "Test mapped 'to' IntervalMetric";

            public MappedToIntervalMetric()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }


        #endregion
    }
}
