/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationAccess.UnitTests;
using ApplicationMetrics.Filters;
using ApplicationMetrics;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggerBaseTypeInclusionFilter class.
    /// </summary>
    public class MetricLoggerBaseTypeInclusionFilterTests
    {
        private IMetricLogger mockFilteredMetricLogger;
        private MetricLoggerBaseTypeInclusionFilter testMetricLoggerBaseTypeInclusionFilter;

        [SetUp]
        protected void SetUp()
        {
            mockFilteredMetricLogger = Substitute.For<IMetricLogger>();
            // TODO: Need to setup here for tests
            // testMetricLoggerBaseTypeInclusionFilter = 
        }

        [Test]
        public void Constructor_CountMetricBaseTypesParameterContainsNonCountMetric()
        {
            var countMetricBaseTypes = new List<Type>() { typeof(String) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger, 
                    countMetricBaseTypes, 
                    Enumerable.Empty<Type>(), 
                    Enumerable.Empty<Type>(), 
                    Enumerable.Empty<Type>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'countMetricBaseTypes' contains type 'System.String' which is not assignable to 'CountMetric'."));
            Assert.AreEqual("countMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Constructor_CountMetricBaseTypesParameterContainsDuplicates()
        {
            var countMetricBaseTypes = new List<Type>() { typeof(UserAdded), typeof(UserAdded) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger,
                    countMetricBaseTypes,
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'countMetricBaseTypes' contains duplicate base count metrics of type '{typeof(UserAdded).FullName}'."));
            Assert.AreEqual("countMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Constructor_AmountMetricBaseTypesParameterContainsNonAmountMetric()
        {
            var amountMetricBaseTypes = new List<Type>() { typeof(String) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger,
                    Enumerable.Empty<Type>(),
                    amountMetricBaseTypes,
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'amountMetricBaseTypes' contains type 'System.String' which is not assignable to 'AmountMetric'."));
            Assert.AreEqual("amountMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Constructor_AmountMetricBaseTypesParameterContainsDuplicates()
        {
            var amountMetricBaseTypes = new List<Type>() { typeof(DiskBytesRead), typeof(DiskBytesRead) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger,
                    Enumerable.Empty<Type>(),
                    amountMetricBaseTypes,
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'amountMetricBaseTypes' contains duplicate base amount metrics of type '{typeof(DiskBytesRead).FullName}'."));
            Assert.AreEqual("amountMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Constructor_StatusMetricBaseTypesParameterContainsNonStatusMetric()
        {
            var statusMetricBaseTypes = new List<Type>() { typeof(String) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger,
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>(),
                    statusMetricBaseTypes,
                    Enumerable.Empty<Type>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'statusMetricBaseTypes' contains type 'System.String' which is not assignable to 'StatusMetric'."));
            Assert.AreEqual("statusMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Constructor_StatusMetricBaseTypesParameterContainsDuplicates()
        {
            var statusMetricBaseTypes = new List<Type>() { typeof(UsersStored), typeof(UsersStored) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger,
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>(),
                    statusMetricBaseTypes,
                    Enumerable.Empty<Type>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'statusMetricBaseTypes' contains duplicate base status metrics of type '{typeof(UsersStored).FullName}'."));
            Assert.AreEqual("statusMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Constructor_IntervalMetricBaseTypesParameterContainsNonIntervalMetric()
        {
            var intervalMetricBaseTypes = new List<Type>() { typeof(String) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger,
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>(),
                    intervalMetricBaseTypes
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'intervalMetricBaseTypes' contains type 'System.String' which is not assignable to 'IntervalMetric'."));
            Assert.AreEqual("intervalMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Constructor_IntervalMetricBaseTypesParameterContainsDuplicates()
        {
            var intervalMetricBaseTypes = new List<Type>() { typeof(UsersPropertyQueryTime), typeof(UsersPropertyQueryTime) };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
                (
                    mockFilteredMetricLogger,
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>(),
                    Enumerable.Empty<Type>(),
                    intervalMetricBaseTypes
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'intervalMetricBaseTypes' contains duplicate base interval metrics of type '{typeof(UsersPropertyQueryTime).FullName}'."));
            Assert.AreEqual("intervalMetricBaseTypes", e.ParamName);
        }

        [Test]
        public void Increment_IncludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                new List<Type>() { typeof(CountMetricBase) }, 
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>()
            );

            testMetricLoggerBaseTypeInclusionFilter.Increment(new CountMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new CountMetricTwiceDerived());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new CountMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new CountMetricTwiceDerived());

            // Arg.Any matches on base T types, so need to check for 4 calls here
            mockFilteredMetricLogger.Received(4).Increment(Arg.Any<CountMetricDerived>());
            mockFilteredMetricLogger.Received(2).Increment(Arg.Any<CountMetricTwiceDerived>());
        }

        [Test]
        public void Increment_ExcludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                new List<Type>() { typeof(CountMetricBase) },
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>()
            );

            testMetricLoggerBaseTypeInclusionFilter.Increment(new UserRemoved());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new UserRemoved());

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void Add_IncludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(AmountMetricBase) },
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>()
            );

            testMetricLoggerBaseTypeInclusionFilter.Add(new AmountMetricDerived(), 1);
            testMetricLoggerBaseTypeInclusionFilter.Add(new AmountMetricTwiceDerived(), 2);
            testMetricLoggerBaseTypeInclusionFilter.Add(new AmountMetricDerived(), 3);
            testMetricLoggerBaseTypeInclusionFilter.Add(new AmountMetricTwiceDerived(), 4);

            mockFilteredMetricLogger.Received(1).Add(Arg.Any<AmountMetricDerived>(), 1);
            mockFilteredMetricLogger.Received(1).Add(Arg.Any<AmountMetricTwiceDerived>(), 2);
            mockFilteredMetricLogger.Received(1).Add(Arg.Any<AmountMetricDerived>(), 3);
            mockFilteredMetricLogger.Received(1).Add(Arg.Any<AmountMetricTwiceDerived>(), 4);
        }

        [Test]
        public void Add_ExcludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(AmountMetricBase) },
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>()
            );

            testMetricLoggerBaseTypeInclusionFilter.Add(new DiskBytesRead(), 1);
            testMetricLoggerBaseTypeInclusionFilter.Add(new DiskBytesRead(), 2);

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void Set_IncludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(StatusMetricBase) },
                Enumerable.Empty<Type>()
            );

            testMetricLoggerBaseTypeInclusionFilter.Set(new StatusMetricDerived(), 1);
            testMetricLoggerBaseTypeInclusionFilter.Set(new StatusMetricTwiceDerived(), 2);
            testMetricLoggerBaseTypeInclusionFilter.Set(new StatusMetricDerived(), 3);
            testMetricLoggerBaseTypeInclusionFilter.Set(new StatusMetricTwiceDerived(), 4);

            mockFilteredMetricLogger.Received(1).Set(Arg.Any<StatusMetricDerived>(), 1);
            mockFilteredMetricLogger.Received(1).Set(Arg.Any<StatusMetricTwiceDerived>(), 2);
            mockFilteredMetricLogger.Received(1).Set(Arg.Any<StatusMetricDerived>(), 3);
            mockFilteredMetricLogger.Received(1).Set(Arg.Any<StatusMetricTwiceDerived>(), 4);
        }

        [Test]
        public void Set_ExcludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(StatusMetricBase) },
                Enumerable.Empty<Type>()
            );

            testMetricLoggerBaseTypeInclusionFilter.Set(new UsersStored(), 1);
            testMetricLoggerBaseTypeInclusionFilter.Set(new UsersStored(), 2);

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void Begin_IncludedMetric()
        {
            mockFilteredMetricLogger.Begin(Arg.Any<IntervalMetricDerived>()).Returns<Guid>
            (
                Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Guid.Parse("00000000-0000-0000-0000-000000000003")
            );
            mockFilteredMetricLogger.Begin(Arg.Any<IntervalMetricTwiceDerived>()).Returns<Guid>
            (
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Guid.Parse("00000000-0000-0000-0000-000000000004")
            );
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            Guid beginId1 = testMetricLoggerBaseTypeInclusionFilter.Begin(new IntervalMetricDerived());
            Guid beginId2 = testMetricLoggerBaseTypeInclusionFilter.Begin(new IntervalMetricTwiceDerived());
            Guid beginId3 = testMetricLoggerBaseTypeInclusionFilter.Begin(new IntervalMetricDerived());
            Guid beginId4 = testMetricLoggerBaseTypeInclusionFilter.Begin(new IntervalMetricTwiceDerived());

            // Arg.Any matches on base T types, so need to check for 4 calls here
            mockFilteredMetricLogger.Received(4).Begin(Arg.Any<IntervalMetricDerived>());
            mockFilteredMetricLogger.Received(2).Begin(Arg.Any<IntervalMetricTwiceDerived>());
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000001"), beginId1);
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000002"), beginId2);
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000003"), beginId3);
            Assert.AreEqual(Guid.Parse("00000000-0000-0000-0000-000000000004"), beginId4);
        }

        [Test]
        public void Begin_ExcludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.Begin(new UserAddTime());
            testMetricLoggerBaseTypeInclusionFilter.Begin(new UserAddTime());

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void End_InterleavedModeIncludedMetric()
        {
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.End(guid1, new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.End(guid2, new IntervalMetricTwiceDerived());
            testMetricLoggerBaseTypeInclusionFilter.End(guid3, new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.End(guid4, new IntervalMetricTwiceDerived());

            mockFilteredMetricLogger.Received(1).End(guid1, Arg.Any<IntervalMetricDerived>());
            mockFilteredMetricLogger.Received(1).End(guid2, Arg.Any<IntervalMetricTwiceDerived>());
            mockFilteredMetricLogger.Received(1).End(guid3, Arg.Any<IntervalMetricDerived>());
            mockFilteredMetricLogger.Received(1).End(guid4, Arg.Any<IntervalMetricTwiceDerived>());
        }

        [Test]
        public void End_InterleavedModeExcludedMetric()
        {
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.End(guid1, new UserAddTime());
            testMetricLoggerBaseTypeInclusionFilter.End(guid2, new UserAddTime());

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void End_NonInterleavedModeIncludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.End(new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.End(new IntervalMetricTwiceDerived());
            testMetricLoggerBaseTypeInclusionFilter.End(new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.End(new IntervalMetricTwiceDerived());

            // Arg.Any matches on base T types, so need to check for 4 calls here
            mockFilteredMetricLogger.Received(4).End(Arg.Any<IntervalMetricDerived>());
            mockFilteredMetricLogger.Received(2).End(Arg.Any<IntervalMetricTwiceDerived>());
        }

        [Test]
        public void End_NonInterleavedModeExcludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.End(new UserAddTime());
            testMetricLoggerBaseTypeInclusionFilter.End(new UserAddTime());

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void CancelBegin_InterleavedModeIncludedMetric()
        {
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(guid1, new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(guid2, new IntervalMetricTwiceDerived());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(guid3, new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(guid4, new IntervalMetricTwiceDerived());

            mockFilteredMetricLogger.Received(1).CancelBegin(guid1, Arg.Any<IntervalMetricDerived>());
            mockFilteredMetricLogger.Received(1).CancelBegin(guid2, Arg.Any<IntervalMetricTwiceDerived>());
            mockFilteredMetricLogger.Received(1).CancelBegin(guid3, Arg.Any<IntervalMetricDerived>());
            mockFilteredMetricLogger.Received(1).CancelBegin(guid4, Arg.Any<IntervalMetricTwiceDerived>());
        }

        [Test]
        public void CancelBegin_InterleavedModeExcludedMetric()
        {
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(guid1, new UserAddTime());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(guid2, new UserAddTime());

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void CancelBegin_NonInterleavedModeIncludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(new IntervalMetricTwiceDerived());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(new IntervalMetricDerived());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(new IntervalMetricTwiceDerived());

            // Arg.Any matches on base T types, so need to check for 4 calls here
            mockFilteredMetricLogger.Received(4).CancelBegin(Arg.Any<IntervalMetricDerived>());
            mockFilteredMetricLogger.Received(2).CancelBegin(Arg.Any<IntervalMetricTwiceDerived>());
        }

        [Test]
        public void CancelBegin_NonInterleavedModeExcludedMetric()
        {
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(IntervalMetricBase) }
            );

            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(new UserAddTime());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(new UserAddTime());

            Assert.AreEqual(0, mockFilteredMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void AllMethods()
        {
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var guid5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var guid6 = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var guid7 = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var guid8 = Guid.Parse("00000000-0000-0000-0000-000000000008");
            mockFilteredMetricLogger.Begin(Arg.Any<QueryIntervalMetric>()).Returns<Guid>
            (
                guid1,
                guid2,
                guid3,
                guid4,
                guid5,
                guid6,
                guid7,
                guid8
            );
            testMetricLoggerBaseTypeInclusionFilter = new MetricLoggerBaseTypeInclusionFilter
            (
                mockFilteredMetricLogger,
                new List<Type>() { typeof(QueryCountMetric) },
                Enumerable.Empty<Type>(),
                Enumerable.Empty<Type>(),
                new List<Type>() { typeof(QueryIntervalMetric) }
            );

            testMetricLoggerBaseTypeInclusionFilter.Increment(new UserRemoved());
            testMetricLoggerBaseTypeInclusionFilter.Set(new GroupsStored(), 2);
            Guid beginId1 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UsersPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.End(beginId1, new UsersPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.Add(new DiskBytesRead(), 3);
            Guid beginId2 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UsersPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.Set(new UsersStored(), 1);
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(beginId2, new UsersPropertyQueryTime());
            Guid beginId3 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UsersPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new UsersPropertyQuery());
            testMetricLoggerBaseTypeInclusionFilter.End(beginId3, new UsersPropertyQueryTime());
            Guid beginId4 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UsersPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new UsersPropertyQuery());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(beginId4, new UsersPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new GroupsPropertyQuery());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new UserAdded());
            Guid beginId5 = testMetricLoggerBaseTypeInclusionFilter.Begin(new GroupsPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(beginId5, new GroupsPropertyQueryTime());
            Guid beginId6 = testMetricLoggerBaseTypeInclusionFilter.Begin(new GroupsPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.Increment(new GroupsPropertyQuery());
            testMetricLoggerBaseTypeInclusionFilter.End(beginId6, new GroupsPropertyQueryTime());
            Guid beginId7 = testMetricLoggerBaseTypeInclusionFilter.Begin(new GroupsPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(beginId7, new GroupsPropertyQueryTime());
            Guid beginId8 = testMetricLoggerBaseTypeInclusionFilter.Begin(new GroupsPropertyQueryTime());
            testMetricLoggerBaseTypeInclusionFilter.End(beginId8, new GroupsPropertyQueryTime());
            Guid beginId9 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UserAddTime());
            testMetricLoggerBaseTypeInclusionFilter.End(beginId5, new UserAddTime());
            Guid beginId10 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UserAddTime());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(beginId6, new UserAddTime());
            Guid beginId11 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UserRemoveTime());
            testMetricLoggerBaseTypeInclusionFilter.CancelBegin(beginId6, new UserRemoveTime());
            Guid beginId12 = testMetricLoggerBaseTypeInclusionFilter.Begin(new UserRemoveTime());
            testMetricLoggerBaseTypeInclusionFilter.End(beginId5, new UserRemoveTime());

            mockFilteredMetricLogger.Received(2).Increment(Arg.Any<UsersPropertyQuery>());
            mockFilteredMetricLogger.Received(2).Increment(Arg.Any<GroupsPropertyQuery>());
            mockFilteredMetricLogger.Received(4).Begin(Arg.Any<UsersPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).End(beginId1, Arg.Any<UsersPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).CancelBegin(beginId2, Arg.Any<UsersPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).End(beginId3, Arg.Any<UsersPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).CancelBegin(beginId4, Arg.Any<UsersPropertyQueryTime>());
            mockFilteredMetricLogger.Received(4).Begin(Arg.Any<GroupsPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).CancelBegin(beginId5, Arg.Any<GroupsPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).End(beginId6, Arg.Any<GroupsPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).CancelBegin(beginId7, Arg.Any<GroupsPropertyQueryTime>());
            mockFilteredMetricLogger.Received(1).End(beginId8, Arg.Any<GroupsPropertyQueryTime>());
            mockFilteredMetricLogger.DidNotReceive().Increment(Arg.Any<UserAdded>());
            mockFilteredMetricLogger.DidNotReceive().Increment(Arg.Any<UserRemoved>());
            mockFilteredMetricLogger.DidNotReceive().Set(Arg.Any<UsersStored>(), Arg.Any<Int64>());
            mockFilteredMetricLogger.DidNotReceive().Add(Arg.Any<DiskBytesRead>(), Arg.Any<Int64>());
            mockFilteredMetricLogger.DidNotReceive().Begin(Arg.Any<UserAddTime>());
            mockFilteredMetricLogger.DidNotReceive().End(Arg.Any<Guid>(), Arg.Any<UserAddTime>());
            mockFilteredMetricLogger.DidNotReceive().CancelBegin(Arg.Any<Guid>(), Arg.Any<UserAddTime>());
            mockFilteredMetricLogger.DidNotReceive().Begin(Arg.Any<UserRemoveTime>());
            mockFilteredMetricLogger.DidNotReceive().End(Arg.Any<Guid>(), Arg.Any<UserRemoveTime>());
            mockFilteredMetricLogger.DidNotReceive().CancelBegin(Arg.Any<Guid>(), Arg.Any<UserRemoveTime>());
            Assert.AreEqual(guid1, beginId1);
            Assert.AreEqual(guid2, beginId2);
            Assert.AreEqual(guid3, beginId3);
            Assert.AreEqual(guid4, beginId4);
            Assert.AreEqual(guid5, beginId5);
            Assert.AreEqual(guid6, beginId6);
            Assert.AreEqual(guid7, beginId7);
            Assert.AreEqual(guid8, beginId8);
            Assert.AreEqual(Guid.Empty, beginId9);
            Assert.AreEqual(Guid.Empty, beginId10);
            Assert.AreEqual(Guid.Empty, beginId11);
            Assert.AreEqual(Guid.Empty, beginId12);
        }

        #region Nested Classes

        protected abstract class CountMetricBase : CountMetric
        {
        }

        protected class CountMetricDerived : CountMetricBase
        {
            public CountMetricDerived()
            {
                name = "CountMetricDerived";
                description = "CountMetricDerived";
            }
        }

        protected class CountMetricTwiceDerived : CountMetricDerived
        {
            public CountMetricTwiceDerived()
            {
                name = "CountMetricTwiceDerived";
                description = "CountMetricTwiceDerived";
            }
        }

        protected abstract class AmountMetricBase : AmountMetric
        {
        }

        protected class AmountMetricDerived : AmountMetricBase
        {
            public AmountMetricDerived()
            {
                name = "AmountMetricDerived";
                description = "AmountMetricDerived";
            }
        }

        protected class AmountMetricTwiceDerived : AmountMetricDerived
        {
            public AmountMetricTwiceDerived()
            {
                name = "AmountMetricTwiceDerived";
                description = "AmountMetricTwiceDerived";
            }
        }

        protected abstract class StatusMetricBase : StatusMetric
        {
        }

        protected class StatusMetricDerived : StatusMetricBase
        {
            public StatusMetricDerived()
            {
                name = "StatusMetricDerived";
                description = "StatusMetricDerived";
            }
        }

        protected class StatusMetricTwiceDerived : StatusMetricDerived
        {
            public StatusMetricTwiceDerived()
            {
                name = "StatusMetricTwiceDerived";
                description = "StatusMetricTwiceDerived";
            }
        }

        protected abstract class IntervalMetricBase : IntervalMetric
        {
        }

        protected class IntervalMetricDerived : IntervalMetricBase
        {
            public IntervalMetricDerived()
            {
                name = "IntervalMetricDerived";
                description = "IntervalMetricDerived";
            }
        }

        protected class IntervalMetricTwiceDerived : IntervalMetricDerived
        {
            public IntervalMetricTwiceDerived()
            {
                name = "IntervalMetricTwiceDerived";
                description = "IntervalMetricTwiceDerived";
            }
        }

        protected class DiskBytesRead : AmountMetric
        {
            protected static String staticName = "DiskBytesRead";
            protected static String staticDescription = "Represents the number of bytes read during a disk read operation.";

            public DiskBytesRead()
            {
                base.name = staticName;
                base.description = staticDescription;
            }
        }
 
        #endregion
    }
}
