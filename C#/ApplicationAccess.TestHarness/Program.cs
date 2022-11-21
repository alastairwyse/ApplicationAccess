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
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Microsoft.Data.SqlClient;
using ApplicationAccess.Hosting;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.SqlServer;
using ApplicationLogging;
using ApplicationLogging.Adapters;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;
using log4net;
using log4net.Config;

namespace ApplicationAccess.TestHarness
{
    class Program
    {
        protected static ManualResetEvent stopNotifySignal;

        static void Main(string[] args)
        {
            /*
            var targetStorateStructureCountsTemp = new Dictionary<StorageStructure, Int32>()
                    {
                        { StorageStructure.Users, 100 },
                        { StorageStructure.Groups, 30 },
                        { StorageStructure.UserToGroupMap, 500 },
                        { StorageStructure.GroupToGroupMap, 12 },
                        { StorageStructure.UserToComponentMap, 800 },
                        { StorageStructure.GroupToComponentMap, 240 },
                        { StorageStructure.EntityTypes, 5 },
                        { StorageStructure.Entities, 80 },
                        { StorageStructure.UserToEntityMap, 500 },
                        { StorageStructure.GroupToEntityMap, 180 }
                    };
            var opGen = new DefaultOperationGenerator<String, String, TestApplicationComponent, TestAccessLevel>(null, targetStorateStructureCountsTemp, null, null, 1.0);
            foreach (Int32 currentCount in System.Linq.Enumerable.Range(0, 11))
            {
                Double orig = Convert.ToDouble(currentCount / 10.0);
                Double prob = opGen.CalculateBetaScaledBaseRemoveProbability(currentCount, 5, 1.0, 4.0);
                Console.WriteLine($"Remove for current count {currentCount}, original was {orig}... remove prob would be {prob}");
                Double addOrig = 1.0 - orig;
                Double addProb = opGen.CalculateBetaScaledBaseRemoveProbability(10 - currentCount, 5, 1.0, 4.0);
                Console.WriteLine($"Add for current count {currentCount}, original was {addOrig}... add prob would be {addProb}");
            }

            return;

            */

            stopNotifySignal = new ManualResetEvent(false);

            // Setup SQL Server connection strings
            var persisterConnectionStringBuilder = new SqlConnectionStringBuilder();
            persisterConnectionStringBuilder.DataSource = "";
            persisterConnectionStringBuilder.InitialCatalog = "ApplicationAccess";
            persisterConnectionStringBuilder.Encrypt = false;
            persisterConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            persisterConnectionStringBuilder.UserID = "";
            persisterConnectionStringBuilder.Password = "";
            var metricsConnectionStringBuilder = new SqlConnectionStringBuilder();
            metricsConnectionStringBuilder.DataSource = "";
            metricsConnectionStringBuilder.InitialCatalog = "ApplicationMetrics";
            metricsConnectionStringBuilder.Encrypt = false;
            metricsConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            metricsConnectionStringBuilder.UserID = "";
            metricsConnectionStringBuilder.Password = "";

            // Setup the log4net logger for the SQL Server persister
            const String log4netConfigFileName = "log4net.config";
            XmlConfigurator.Configure(new FileInfo(log4netConfigFileName));
            ILog log4netPersisterLogger = LogManager.GetLogger(typeof(SqlServerAccessManagerTemporalPersister<String, String, TestApplicationComponent, TestAccessLevel>));
            var persisterLogger = new ApplicationLoggingLog4NetAdapter(log4netPersisterLogger);

            // Setup the log4net logger for the test harness
            ILog log4netTestHarnessExceptionLogger = LogManager.GetLogger(typeof(TestHarness<String, String, TestApplicationComponent, TestAccessLevel>));
            var testHarnessExceptionLogger = new ApplicationLoggingLog4NetAdapter(log4netTestHarnessExceptionLogger);

            // Setup the metric logger
            const String metricLoggerCategory = "ApplicationAccessTestHarness";
            String sqlServerMetricsConnectionString = metricsConnectionStringBuilder.ConnectionString;
            Int32 sqlServerRetryCount = 10;
            Int32 sqlServerRetryInterval = 10;
            Int32 metricLoggerBufferSizeLimit = 100;

            // Setup the SQL Server persister
            Int32 persisterBufferFlushStrategyFlushInterval = 60000;
            String sqlServerConnectionString = persisterConnectionStringBuilder.ConnectionString;
            using (var metricLoggerBufferProcessingStrategy = new SizeLimitedBufferProcessor(metricLoggerBufferSizeLimit))
            using (var metricLogger = new SqlServerMetricLogger(metricLoggerCategory, sqlServerMetricsConnectionString, sqlServerRetryCount, sqlServerRetryInterval, metricLoggerBufferProcessingStrategy, false))
            using (var persisterBufferFlushStrategy = new LoopingWorkerThreadBufferFlushStrategy(persisterBufferFlushStrategyFlushInterval, metricLogger))
            {
                var persister = new SqlServerAccessManagerTemporalPersister<String, String, TestApplicationComponent, TestAccessLevel>
                (
                    sqlServerConnectionString,
                    sqlServerRetryCount,
                    sqlServerRetryInterval,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<TestApplicationComponent>(),
                    new EnumUniqueStringifier<TestAccessLevel>(),
                    persisterLogger,
                    metricLogger
                );

                // Setup the test AccessManager
                using
                (
                    var testAccessManager = new ReaderWriterNode<String, String, TestApplicationComponent, TestAccessLevel>
                    (
                        persisterBufferFlushStrategy,
                        persister,
                        persister,
                        metricLogger
                    )
                )
                {
                    // Setup the test harness
                    Int32 workerThreadCount = 1;
                    Boolean loadExistingData = true;
                    var targetStorateStructureCounts = new Dictionary<StorageStructure, Int32>()
                    {
                        { StorageStructure.Users, 100 },
                        { StorageStructure.Groups, 30 },
                        { StorageStructure.UserToGroupMap, 500 },
                        { StorageStructure.GroupToGroupMap, 12 },
                        { StorageStructure.UserToComponentMap, 800 },
                        { StorageStructure.GroupToComponentMap, 240 },
                        { StorageStructure.EntityTypes, 5 },
                        { StorageStructure.Entities, 80 },
                        { StorageStructure.UserToEntityMap, 500 },
                        { StorageStructure.GroupToEntityMap, 180 }
                    };
                    var dataElementStorer = new DataElementStorer<String, String, TestApplicationComponent, TestAccessLevel>();
                    if (loadExistingData == true)
                    {
                        var dataElementStorerLoader = new DataElementStorerLoader<String, String, TestApplicationComponent, TestAccessLevel>();
                        dataElementStorerLoader.Load(persister, dataElementStorer);
                        testAccessManager.Load();
                    }
                    var operationGenerator = new DefaultOperationGenerator<String, String, TestApplicationComponent, TestAccessLevel>
                    (
                        dataElementStorer,
                        targetStorateStructureCounts,
                        new EnumAvailableDataElementCounter<TestApplicationComponent>(),
                        new EnumAvailableDataElementCounter<TestAccessLevel>(),
                        2.0
                    );
                    var parameterGenerator = new DefaultOperationParameterGenerator<String, String, TestApplicationComponent, TestAccessLevel>
                    (
                        dataElementStorer,
                        new StringifiedGuidGenerator(),
                        new StringifiedGuidGenerator(),
                        new NewTestApplicationComponentGenerator(),
                        new NewTestAccessLevelGenerator(),
                        new StringifiedGuidGenerator(),
                        new StringifiedGuidGenerator()
                    );

                    Double targetOperationsPerSecond = 1.0;
                    Int32 previousInitiationTimeWindowSize = 20;

                    Double exceptionsPerSecondThreshold = 0.5;
                    Int32 previousExceptionOccurenceTimeWindowSize = 5;
                    Boolean ignoreKnownAccessManagerExceptions = false;
                    using (var operationTriggerer = new DefaultOperationTriggerer(targetOperationsPerSecond, previousInitiationTimeWindowSize))
                    using (var testHarness = new TestHarness<String, String, TestApplicationComponent, TestAccessLevel>
                    (
                        testAccessManager,
                        workerThreadCount,
                        dataElementStorer,
                        new IOperationGenerator[] { operationGenerator },
                        new IOperationParameterGenerator<String, String, TestApplicationComponent, TestAccessLevel>[] { parameterGenerator },
                        new IOperationTriggerer[] { operationTriggerer },
                        new IApplicationLogger[] { testHarnessExceptionLogger },
                        exceptionsPerSecondThreshold,
                        previousExceptionOccurenceTimeWindowSize,
                        ignoreKnownAccessManagerExceptions
                    ))
                    {
                        metricLogger.Start();
                        persisterBufferFlushStrategy.Start();
                        operationTriggerer.Start();

                        try
                        {
                            var stopSignalThread = new Thread(StopThreadTask);
                            stopSignalThread.Start();
                            testHarness.Start();
                            stopNotifySignal.WaitOne();
                            testHarness.Stop();

                            // TODO:
                            // Try different types of buffer processing strategies (for 'persisterBufferFlushStrategy' and 'metricLoggerBufferProcessingStrategy')
                            // Ability to load existing state into DataElementStorer
                            // Should the threads in TestHarness be foreground rather than background
                            // Implementation of ParameterizedOperationSerializer 
                            // Check for and remove any Console.Writeline()
                        }
                        finally
                        {
                            // Don't need to call operationTriggerer.Stop(), as it's called from the TestHarness.Stop() method
                            Console.WriteLine("Stopping 'persisterBufferFlushStrategy'...");
                            persisterBufferFlushStrategy.Stop();
                            Console.WriteLine("Stopping 'metricLogger'...");
                            metricLogger.Stop();
                            Console.WriteLine("Disposing 'stopNotifySignal'...");
                            stopNotifySignal.Dispose();
                            Console.WriteLine("Flushing log4net logs...");
                            LogManager.Flush(10000);
                        }
                    }
                }
            }
 


            /* Testing of DataElementStorer
             
            var testDataElementStorer = new DataElementStorer<String, String, Screen, Access>();

            testDataElementStorer.AddUser("User1");
            testDataElementStorer.AddEntityType("Customers");
            testDataElementStorer.AddEntityType("Products");
            testDataElementStorer.AddEntity("Customers", "Client1");
            testDataElementStorer.AddEntity("Customers", "Client2");
            testDataElementStorer.AddEntity("Products", "Boxes");
            testDataElementStorer.AddEntity("Products", "Forks");
            testDataElementStorer.AddEntity("Products", "Spoons");
            testDataElementStorer.AddUserToApplicationComponentAndAccessLevelMapping("User1", Screen.Screen1, Access.Create);
            testDataElementStorer.AddUserToApplicationComponentAndAccessLevelMapping("User2", Screen.Screen1, Access.Delete);
            testDataElementStorer.AddUserToEntityMapping("User1", "Customers", "Client1");
            testDataElementStorer.AddUserToEntityMapping("User3", "Customers", "Client1");
            testDataElementStorer.AddUserToEntityMapping("User4", "Customers", "Client3");
            testDataElementStorer.AddUserToEntityMapping("User5", "Systems", "Order");
            Console.WriteLine($"UserCount: {testDataElementStorer.UserCount}"); // 5
            Console.WriteLine($"UserToComponentMappingCount: {testDataElementStorer.UserToComponentMappingCount}"); // 2
            Console.WriteLine($"EntityTypeCount: {testDataElementStorer.EntityTypeCount}"); // 3
            Console.WriteLine($"EntityCount: {testDataElementStorer.EntityCount}"); // 7
            Console.WriteLine($"UserToEntityMappingCount: {testDataElementStorer.UserToEntityMappingCount}"); // 4
            testDataElementStorer.RemoveEntityType("Systems");
            testDataElementStorer.RemoveUser("User5");
            testDataElementStorer.RemoveUserToEntityMapping("User1", "Customers", "Client1");
            testDataElementStorer.RemoveUserToApplicationComponentAndAccessLevelMapping("User1", Screen.Screen1, Access.Create);
            testDataElementStorer.RemoveEntity("Customers", "Client1");
            testDataElementStorer.RemoveUser("User1");
            testDataElementStorer.RemoveUser("User2");
            testDataElementStorer.RemoveUser("User3");
            testDataElementStorer.RemoveUser("User4");
            testDataElementStorer.RemoveEntityType("Customers");
            testDataElementStorer.RemoveEntityType("Products");
            Console.WriteLine($"UserCount: {testDataElementStorer.UserCount}"); // 0
            Console.WriteLine($"UserToComponentMappingCount: {testDataElementStorer.UserToComponentMappingCount}"); // 0
            Console.WriteLine($"EntityTypeCount: {testDataElementStorer.EntityTypeCount}"); // 0
            Console.WriteLine($"EntityCount: {testDataElementStorer.EntityCount}"); // 0
            Console.WriteLine($"UserToEntityMappingCount: {testDataElementStorer.UserToEntityMappingCount}"); // 0

            testDataElementStorer.RemoveEntityType("Systems");
            testDataElementStorer.RemoveUser("User5");
            testDataElementStorer.RemoveUserToEntityMapping("User1", "Customers", "Client1");
            testDataElementStorer.RemoveUserToApplicationComponentAndAccessLevelMapping("User1", Screen.Screen1, Access.Create);
            testDataElementStorer.RemoveEntity("Customers", "Client1");
            testDataElementStorer.RemoveUser("User1");
            testDataElementStorer.RemoveUser("User2");
            testDataElementStorer.RemoveUser("User3");
            testDataElementStorer.RemoveUser("User4");
            testDataElementStorer.RemoveEntityType("Customers");
            testDataElementStorer.RemoveEntityType("Products");
            Console.WriteLine($"UserCount: {testDataElementStorer.UserCount}"); // 0
            Console.WriteLine($"UserToComponentMappingCount: {testDataElementStorer.UserToComponentMappingCount}"); // 0
            Console.WriteLine($"EntityTypeCount: {testDataElementStorer.EntityTypeCount}"); // 0
            Console.WriteLine($"EntityCount: {testDataElementStorer.EntityCount}"); // 0
            Console.WriteLine($"UserToEntityMappingCount: {testDataElementStorer.UserToEntityMappingCount}"); // 0

            Console.WriteLine("Testing DefaultOperationTriggerer...");
            Boolean stopRequestRecieved = false;
            var stopSignalThread = new Thread(new ThreadStart(new Action(() =>
            {
                Console.ReadLine();
                stopRequestRecieved = true;
            })));
            stopSignalThread.Start();
            using (var testOperationTriggerer = new DefaultOperationTriggerer(4, 100))
            {
                testOperationTriggerer.Start();
                while (stopRequestRecieved == false)
                {
                    testOperationTriggerer.WaitForNextTrigger();
                    Console.WriteLine($"Got triggered at {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}");
                    testOperationTriggerer.NotifyOperationInitiated();
                }
                testOperationTriggerer.Stop();
            }
            Console.WriteLine("DONE!");

            */
        }

        protected static void StopThreadTask()
        {
            Console.ReadLine();
            stopNotifySignal.Set();
        }

        protected enum Screen
        {
            Screen1,
            Screen2,
            Screen3,
            Screen4,
            Screen5,
            Screen6,
            Screen7,
            Screen8,
            Screen9
        }

        protected enum Access
        {
            Read, 
            Create, 
            Update, 
            Delete
        }
    }
}
