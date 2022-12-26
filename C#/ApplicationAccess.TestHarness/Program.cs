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
using Microsoft.Extensions.Configuration;
using ApplicationAccess.TestHarness.Configuration;
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
        protected static String metricsDatabaseConnectionConfigurationFileProperty = "MetricsDatabaseConnectionConfiguration";
        protected static String applicationAccessDatabaseConnectionConfigurationFileProperty = "ApplicationAccessDatabaseConnectionConfiguration";
        protected static String metricsBufferConfigurationFileProperty = "MetricsBufferConfiguration";
        protected static String persisterBufferFlushStrategyConfigurationFileProperty = "PersisterBufferFlushStrategyConfiguration";
        protected static String operationGeneratorConfigurationFileProperty = "OperationGeneratorConfiguration";
        protected static String testHarnessConfigurationFileProperty = "TestHarnessConfiguration";
        
        protected static ManualResetEvent stopNotifySignal;

        static void Main(string[] args)
        {
            // Get configuration file path from command line parameters
            if (args.Length != 1)
                throw new ArgumentException($"Expected 1 command line parameter, but recevied {args.Length}.");
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile(args[0], false, false);
            IConfigurationRoot configurationRoot = null;
            try
            {
                configurationRoot = configurationBuilder.Build();
            }
            catch(Exception e)
            {
                throw new Exception($"Failed to load configuration from file '{args[0]}'.", e);
            }
            foreach (String currentConfigurationSection in new String[] 
            { 
                metricsDatabaseConnectionConfigurationFileProperty, 
                applicationAccessDatabaseConnectionConfigurationFileProperty,
                metricsBufferConfigurationFileProperty,
                persisterBufferFlushStrategyConfigurationFileProperty,
                operationGeneratorConfigurationFileProperty, 
                testHarnessConfigurationFileProperty 
            })
            {
                if (configurationRoot.GetSection(currentConfigurationSection).Exists() == false)
                    throw new Exception($"Could not find section '{currentConfigurationSection}' in configuration file.");
            }
            DatabaseConnectionConfiguration metricsDatabaseConnectionConfiguration = new DatabaseConnectionConfigurationReader().Read(configurationRoot.GetSection(metricsDatabaseConnectionConfigurationFileProperty));
            DatabaseConnectionConfiguration appAccessDatabaseConnectionConfiguration = new DatabaseConnectionConfigurationReader().Read(configurationRoot.GetSection(applicationAccessDatabaseConnectionConfigurationFileProperty));
            MetricsBufferConfiguration metricsBufferConfiguration = new MetricsBufferConfigurationReader().Read(configurationRoot.GetSection(metricsBufferConfigurationFileProperty));
            PersisterBufferFlushStrategyConfiguration persisterBufferFlushStrategyConfiguration = new PersisterBufferFlushStrategyConfigurationReader().Read(configurationRoot.GetSection(persisterBufferFlushStrategyConfigurationFileProperty));
            OperationGeneratorConfiguration operationGeneratorConfiguration = new OperationGeneratorConfigurationReader().Read(configurationRoot.GetSection(operationGeneratorConfigurationFileProperty));
            TestHarnessConfiguration testHarnessConfiguration = new TestHarnessConfigurationReader().Read(configurationRoot.GetSection(testHarnessConfigurationFileProperty));

            // Setup the test harness
            stopNotifySignal = new ManualResetEvent(false);

            // Setup SQL Server connection strings
            var persisterConnectionStringBuilder = new SqlConnectionStringBuilder();
            persisterConnectionStringBuilder.DataSource = appAccessDatabaseConnectionConfiguration.DataSource;
            persisterConnectionStringBuilder.InitialCatalog = appAccessDatabaseConnectionConfiguration.InitialCatalogue;
            persisterConnectionStringBuilder.Encrypt = false;
            persisterConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            persisterConnectionStringBuilder.UserID = appAccessDatabaseConnectionConfiguration.UserId;
            persisterConnectionStringBuilder.Password = appAccessDatabaseConnectionConfiguration.Password;
            var metricsConnectionStringBuilder = new SqlConnectionStringBuilder();
            metricsConnectionStringBuilder.DataSource = metricsDatabaseConnectionConfiguration.DataSource;
            metricsConnectionStringBuilder.InitialCatalog = metricsDatabaseConnectionConfiguration.InitialCatalogue;
            metricsConnectionStringBuilder.Encrypt = false;
            metricsConnectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            metricsConnectionStringBuilder.UserID = metricsDatabaseConnectionConfiguration.UserId;
            metricsConnectionStringBuilder.Password = metricsDatabaseConnectionConfiguration.Password;

            // Setup the log4net logger for the SQL Server persister
            const String log4netConfigFileName = "log4net.config";
            XmlConfigurator.Configure(new FileInfo(log4netConfigFileName));
            ILog log4netPersisterLogger = LogManager.GetLogger(typeof(SqlServerAccessManagerTemporalPersister<String, String, TestApplicationComponent, TestAccessLevel>));
            var persisterLogger = new ApplicationLoggingLog4NetAdapter(log4netPersisterLogger);
            ILog log4netMetricLoggerLogger = LogManager.GetLogger(typeof(SqlServerMetricLogger));
            var metricLoggerLogger = new ApplicationLoggingLog4NetAdapter(log4netMetricLoggerLogger);

            // Setup the log4net logger for the test harness
            ILog log4netTestHarnessExceptionLogger = LogManager.GetLogger(typeof(TestHarness<String, String, TestApplicationComponent, TestAccessLevel>));
            var testHarnessExceptionLogger = new ApplicationLoggingLog4NetAdapter(log4netTestHarnessExceptionLogger);

            // Setup the metric logger
            const String metricLoggerCategory = "ApplicationAccessTestHarness";
            String sqlServerMetricsConnectionString = metricsConnectionStringBuilder.ConnectionString;
            Int32 sqlServerRetryCount = metricsDatabaseConnectionConfiguration.RetryCount;
            Int32 sqlServerRetryInterval = metricsDatabaseConnectionConfiguration.RetryInterval;
            Int32 metricLoggerBufferSizeLimit = metricsBufferConfiguration.BufferSizeLimit;

            // Setup the SQL Server persister
            String sqlServerConnectionString = persisterConnectionStringBuilder.ConnectionString;
            var metricsBufferProcessingStrategyFactory = new MetricsBufferProcessingStrategyFactory();
            var accessManagerEventBufferFlushStrategyFactory = new AccessManagerEventBufferFlushStrategyFactory();
            var metricLoggerBufferProcessingStrategyAndActions = metricsBufferProcessingStrategyFactory.MakeProcessingStrategy(metricsBufferConfiguration);
            try
            {
                IBufferProcessingStrategy metricLoggerBufferProcessingStrategy = metricLoggerBufferProcessingStrategyAndActions.BufferFlushStrategy;
                using (var metricLogger = new SqlServerMetricLogger(metricLoggerCategory, sqlServerMetricsConnectionString, sqlServerRetryCount, sqlServerRetryInterval, metricLoggerBufferProcessingStrategy, false, metricLoggerLogger))
                {
                    var accessManagerEventBufferFlushStrategyAndActions = accessManagerEventBufferFlushStrategyFactory.MakeFlushStrategy(persisterBufferFlushStrategyConfiguration, metricLogger);
                    try
                    {
                        IAccessManagerEventBufferFlushStrategy persisterBufferFlushStrategy = accessManagerEventBufferFlushStrategyAndActions.BufferFlushStrategy;
                        {
                            // Setup the test AccessManager
                            //   Can comment-swap between the two versions of 'persister' to specify either serialized or bulk persistence to SQL server
                            /*
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
                            */
                            using
                            (
                                var persister = new SqlServerAccessManagerTemporalBulkPersister<String, String, TestApplicationComponent, TestAccessLevel>
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
                                )
                            )
                            
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
                                Int32 workerThreadCount = testHarnessConfiguration.ThreadCount;
                                Boolean loadExistingData = testHarnessConfiguration.LoadExistingData;
                                var targetStorateStructureCounts = new Dictionary<StorageStructure, Int32>()
                                {
                                    { StorageStructure.Users, operationGeneratorConfiguration.ElementTargetStorageCounts.Users },
                                    { StorageStructure.Groups, operationGeneratorConfiguration.ElementTargetStorageCounts.Groups },
                                    { StorageStructure.UserToGroupMap, operationGeneratorConfiguration.ElementTargetStorageCounts.UserToGroupMap },
                                    { StorageStructure.GroupToGroupMap, operationGeneratorConfiguration.ElementTargetStorageCounts.GroupToGroupMap },
                                    { StorageStructure.UserToComponentMap, operationGeneratorConfiguration.ElementTargetStorageCounts.UserToComponentMap },
                                    { StorageStructure.GroupToComponentMap, operationGeneratorConfiguration.ElementTargetStorageCounts.GroupToComponentMap },
                                    { StorageStructure.EntityTypes, operationGeneratorConfiguration.ElementTargetStorageCounts.EntityTypes },
                                    { StorageStructure.Entities, operationGeneratorConfiguration.ElementTargetStorageCounts.Entities },
                                    { StorageStructure.UserToEntityMap, operationGeneratorConfiguration.ElementTargetStorageCounts.UserToEntityMap },
                                    { StorageStructure.GroupToEntityMap, operationGeneratorConfiguration.ElementTargetStorageCounts.GroupToEntityMap }
                                };
                                var dataElementStorer = new DataElementStorer<String, String, TestApplicationComponent, TestAccessLevel>();
                                if (loadExistingData == true)
                                {
                                    var dataElementStorerLoader = new DataElementStorerLoader<String, String, TestApplicationComponent, TestAccessLevel>();
                                    dataElementStorerLoader.Load(persister, dataElementStorer);
                                    testAccessManager.Load();
                                }

                                // Setup TestHarness array parameters
                                Double targetOperationsPerSecond = testHarnessConfiguration.TargetOperationsPerSecond;
                                Int32 previousInitiationTimeWindowSize = testHarnessConfiguration.PreviousOperationInitiationTimeWindowSize;
                                var operationGenerators = new List<IOperationGenerator>();
                                var parameterGenerators = new List<IOperationParameterGenerator<String, String, TestApplicationComponent, TestAccessLevel>>();
                                var operationTriggerers = new List<IOperationTriggerer>();
                                var exceptionLoggers = new List<IApplicationLogger>();
                                for (Int32 i = 0; i < workerThreadCount; i++)
                                {
                                    var operationGenerator = new DefaultOperationGenerator<String, String, TestApplicationComponent, TestAccessLevel>
                                    (
                                        dataElementStorer,
                                        targetStorateStructureCounts,
                                        new EnumAvailableDataElementCounter<TestApplicationComponent>(),
                                        new EnumAvailableDataElementCounter<TestAccessLevel>(),
                                        operationGeneratorConfiguration.QueryToEventOperationRatio,
                                        operationGeneratorConfiguration.DataElementStorerCountPrintFrequency
                                    );
                                    operationGenerators.Add(operationGenerator);

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
                                    parameterGenerators.Add(parameterGenerator);

                                    var operationTriggerer = new DefaultOperationTriggerer(targetOperationsPerSecond, previousInitiationTimeWindowSize);
                                    operationTriggerers.Add(operationTriggerer);

                                    exceptionLoggers.Add(testHarnessExceptionLogger);
                                }

                                Double exceptionsPerSecondThreshold = testHarnessConfiguration.ExceptionsPerSecondThreshold;
                                Int32 previousExceptionOccurenceTimeWindowSize = testHarnessConfiguration.PreviousExceptionOccurenceTimeWindowSize;
                                Boolean ignoreKnownAccessManagerExceptions = testHarnessConfiguration.IgnoreKnownAccessManagerExceptions;
                                using (var testHarness = new TestHarness<String, String, TestApplicationComponent, TestAccessLevel>
                                (
                                    testAccessManager,
                                    workerThreadCount,
                                    dataElementStorer,
                                    operationGenerators,
                                    parameterGenerators,
                                    operationTriggerers,
                                    exceptionLoggers,
                                    exceptionsPerSecondThreshold,
                                    previousExceptionOccurenceTimeWindowSize,
                                    ignoreKnownAccessManagerExceptions
                                ))
                                {
                                    metricLogger.Start();
                                    accessManagerEventBufferFlushStrategyAndActions.StartAction.Invoke();
                                    foreach (IOperationTriggerer currentOperationTriggerer in operationTriggerers)
                                    {
                                        currentOperationTriggerer.Start();
                                    }

                                    try
                                    {
                                        var stopSignalThread = new Thread(StopThreadTask);
                                        stopSignalThread.Start();
                                        testHarness.Start();
                                        stopNotifySignal.WaitOne();
                                        testHarness.Stop();
                                    }
                                    finally
                                    {
                                        // Don't need to call operationTriggerer.Stop(), as it's called from the TestHarness.Stop() method
                                        Console.WriteLine("Stopping 'persisterBufferFlushStrategy'...");
                                        accessManagerEventBufferFlushStrategyAndActions.StopAction.Invoke();
                                        Console.WriteLine("Stopping 'metricLogger'...");
                                        metricLogger.Stop();
                                        Console.WriteLine("Disposing 'stopNotifySignal'...");
                                        stopNotifySignal.Dispose();
                                        Console.WriteLine("Flushing log4net logs...");
                                        LogManager.Flush(10000);
                                        foreach (IOperationTriggerer currentOperationTriggerer in operationTriggerers)
                                        {
                                            currentOperationTriggerer.Stop();
                                            currentOperationTriggerer.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        accessManagerEventBufferFlushStrategyAndActions.DisposeAction.Invoke();
                    }
                }
            }
            finally
            {
                metricLoggerBufferProcessingStrategyAndActions.DisposeAction.Invoke();
            }
        }

        protected static void StopThreadTask()
        {
            Console.ReadLine();
            stopNotifySignal.Set();
        }
    }
}
