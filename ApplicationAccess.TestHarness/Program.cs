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
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.IO;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ApplicationAccess.TestHarness.Configuration;
using ApplicationAccess.Hosting;
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Sql.SqlServer;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationLogging.Adapters;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.Filters;
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
        protected static String accessManagerRestClientConfigurationFileProperty = "AccessManagerRestClientConfiguration";
        protected static String operationGeneratorConfigurationFileProperty = "OperationGeneratorConfiguration";
        protected static String testHarnessConfigurationFileProperty = "TestHarnessConfiguration";

        protected static HashSet<String> validTestProfiles;
        protected static ManualResetEvent stopNotifySignal;
        protected static volatile Boolean stopped;

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
            catch (Exception e)
            {
                throw new Exception($"Failed to load configuration from file '{args[0]}'.", e);
            }
            validTestProfiles = new HashSet<String>()
            {
                "LocalReaderWriterNode",
                "RemoteRestAccessManager"
            };
            stopped = false;

            TestHarnessConfiguration testHarnessConfiguration = new TestHarnessConfigurationReader().Read(configurationRoot.GetSection(testHarnessConfigurationFileProperty));
            if (validTestProfiles.Contains(testHarnessConfiguration.TestProfile) == false)
                throw new Exception($"TestHarness configuration contains invalid '{nameof(testHarnessConfiguration.TestProfile)}' value '{testHarnessConfiguration.TestProfile}'.");
            if (testHarnessConfiguration.TestProfile == "LocalReaderWriterNode")
            {
                RunWithLocalReaderWriterNode(configurationRoot);
            }
            else if (testHarnessConfiguration.TestProfile == "RemoteRestAccessManager")
            {
                RunWithRemoteRestAccessManager(configurationRoot);
            }
            else
            {
                throw new Exception($"TestHarness configuration contains unhandled '{nameof(testHarnessConfiguration.TestProfile)}' value '{testHarnessConfiguration.TestProfile}'.");
            }
        }

        protected static void StopThreadTask()
        {
            Console.ReadLine();
            if (stopped == false)
            {
                stopNotifySignal.Set();
            }
        }

        /// <summary>
        /// Runs a local <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> access manager instance.
        /// </summary>
        protected static void RunWithLocalReaderWriterNode(IConfigurationRoot configurationRoot)
        {
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
            Int32 sqlServerOperationTimeout = metricsDatabaseConnectionConfiguration.OperationTimeout;
            Int32 metricLoggerBufferSizeLimit = metricsBufferConfiguration.BufferSizeLimit;

            // Setup the SQL Server persister
            String sqlServerConnectionString = persisterConnectionStringBuilder.ConnectionString;
            var metricsBufferProcessingStrategyFactory = new MetricsBufferProcessingStrategyFactory();
            var accessManagerEventBufferFlushStrategyFactory = new AccessManagerEventBufferFlushStrategyFactory();
            var metricLoggerBufferProcessingStrategyAndActions = metricsBufferProcessingStrategyFactory.MakeProcessingStrategy(metricsBufferConfiguration);
            try
            {
                IBufferProcessingStrategy metricLoggerBufferProcessingStrategy = metricLoggerBufferProcessingStrategyAndActions.BufferFlushStrategy;
                using (var metricLogger = new SqlServerMetricLogger(metricLoggerCategory, sqlServerMetricsConnectionString, sqlServerRetryCount, sqlServerRetryInterval, sqlServerOperationTimeout, metricLoggerBufferProcessingStrategy, IntervalMetricBaseTimeUnit.Nanosecond, false, metricLoggerLogger))
                {
                    var accessManagerEventBufferFlushStrategyAndActions = accessManagerEventBufferFlushStrategyFactory.MakeFlushStrategy
                    (
                        persisterBufferFlushStrategyConfiguration,
                        (BufferFlushingException bufferFlushingException) =>
                        {
                            persisterLogger.Log(ApplicationLogging.LogLevel.Critical, "Exception occurred when flushing event buffer.", bufferFlushingException);
                            persisterLogger.Log(ApplicationLogging.LogLevel.Critical, "Tripswitch has been actuated due to an unrecoverable error whilst flushing the event buffer.");
                        }, 
                        metricLogger
                    ); ;
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
                                    appAccessDatabaseConnectionConfiguration.RetryCount,
                                    appAccessDatabaseConnectionConfiguration.RetryInterval,
                                    appAccessDatabaseConnectionConfiguration.OperationTimeout, 
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
                                // Setup DataElementStorer
                                IDataElementStorer<String, String, TestApplicationComponent, TestAccessLevel> dataElementStorer = null;
                                dataElementStorer = new DataElementStorer<String, String, TestApplicationComponent, TestAccessLevel>();
                                if (loadExistingData == true)
                                {
                                    var dataElementStorerLoader = new DataElementStorerLoader<String, String, TestApplicationComponent, TestAccessLevel>();
                                    dataElementStorerLoader.Load(persister, dataElementStorer, false);
                                    testAccessManager.Load(false);
                                }
                                if (testHarnessConfiguration.AddOperationDelayTime > 0)
                                {
                                    dataElementStorer = new DataElementStorerDelayBuffer<String, String, TestApplicationComponent, TestAccessLevel>(testHarnessConfiguration.AddOperationDelayTime, dataElementStorer);
                                }
                                // Setup TestHarness array parameters
                                Double targetOperationsPerSecond = testHarnessConfiguration.TargetOperationsPerSecond;
                                Int32 operationsPerSecondPrintFrequency = testHarnessConfiguration.OperationsPerSecondPrintFrequency;
                                Int32 previousInitiationTimeWindowSize = testHarnessConfiguration.PreviousOperationInitiationTimeWindowSize;
                                var testAccessManagerQueryProcessors = new List<IAccessManagerQueryProcessor<String, String, TestApplicationComponent, TestAccessLevel>>();
                                var testAccessManagerEventProcessors = new List<IAccessManagerEventProcessor<String, String, TestApplicationComponent, TestAccessLevel>>();
                                var operationGenerators = new List<IOperationGenerator>();
                                var parameterGenerators = new List<IOperationParameterGenerator<String, String, TestApplicationComponent, TestAccessLevel>>();
                                var exceptionLoggers = new List<IApplicationLogger>();
                                for (Int32 i = 0; i < workerThreadCount; i++)
                                {
                                    // Set the reader/writer node as the test access manager on each thread
                                    testAccessManagerQueryProcessors.Add(testAccessManager);
                                    testAccessManagerEventProcessors.Add(testAccessManager);

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
                                        new StringifiedGuidGenerator(), 
                                        operationGeneratorConfiguration.ContainsMethodInvalidParameterGenerationFrequency
                                    );
                                    parameterGenerators.Add(parameterGenerator);
                                    exceptionLoggers.Add(testHarnessExceptionLogger);
                                }

                                Double exceptionsPerSecondThreshold = testHarnessConfiguration.ExceptionsPerSecondThreshold;
                                Int32 previousExceptionOccurenceTimeWindowSize = testHarnessConfiguration.PreviousExceptionOccurenceTimeWindowSize;
                                Boolean ignoreKnownAccessManagerExceptions = testHarnessConfiguration.IgnoreKnownAccessManagerExceptions;
                                Boolean generatePrimaryAddOperations = testHarnessConfiguration.GeneratePrimaryAddOperations;
                                Int64 operationLimit = testHarnessConfiguration.OperationLimit;
                                using (var testHarness = new TestHarness<String, String, TestApplicationComponent, TestAccessLevel>
                                (
                                    workerThreadCount,
                                    dataElementStorer,
                                    testAccessManagerQueryProcessors,
                                    testAccessManagerEventProcessors, 
                                    operationGenerators,
                                    parameterGenerators,
                                    exceptionLoggers,
                                    stopNotifySignal, 
                                    targetOperationsPerSecond,
                                    operationsPerSecondPrintFrequency,
                                    previousInitiationTimeWindowSize, 
                                    exceptionsPerSecondThreshold,
                                    previousExceptionOccurenceTimeWindowSize,
                                    operationLimit, 
                                    ignoreKnownAccessManagerExceptions,
                                    generatePrimaryAddOperations
                                ))
                                {
                                    metricLogger.Start();
                                    accessManagerEventBufferFlushStrategyAndActions.StartAction.Invoke();
                                    try
                                    {
                                        var stopSignalThread = new Thread(StopThreadTask);
                                        stopSignalThread.Start();
                                        if (testHarnessConfiguration.AddUrlReservedCharacterElements == true)
                                        {
                                            AddUrlReservedCharacterElements(testAccessManagerEventProcessors[0], dataElementStorer);
                                        }
                                        testHarness.Start();
                                        stopNotifySignal.WaitOne();
                                        stopped = true;
                                        testHarness.Stop();
                                    }
                                    finally
                                    {
                                        Console.WriteLine("Stopping 'persisterBufferFlushStrategy'...");
                                        accessManagerEventBufferFlushStrategyAndActions.StopAction.Invoke();
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

        /// <summary>
        /// Runs the test harness locally, connecting to a remote <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance via REST.
        /// </summary>
        protected static void RunWithRemoteRestAccessManager(IConfigurationRoot configurationRoot)
        {
            foreach (String currentConfigurationSection in new String[]
            {
                metricsDatabaseConnectionConfigurationFileProperty,
                applicationAccessDatabaseConnectionConfigurationFileProperty,
                metricsBufferConfigurationFileProperty,
                accessManagerRestClientConfigurationFileProperty, 
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
            AccessManagerRestClientConfiguration accessManagerRestClientConfiguration = new AccessManagerRestClientConfigurationReader().Read(configurationRoot.GetSection(accessManagerRestClientConfigurationFileProperty));
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

            // Setup the log4net loggers
            const String log4netConfigFileName = "log4net.config";
            XmlConfigurator.Configure(new FileInfo(log4netConfigFileName));
            ILog log4netMetricLoggerLogger = LogManager.GetLogger(typeof(SqlServerMetricLogger));
            var metricLoggerLogger = new ApplicationLoggingLog4NetAdapter(log4netMetricLoggerLogger);
            ILog log4netRestClientLogger = LogManager.GetLogger(typeof(AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>));
            var restClientLogger = new ApplicationLoggingLog4NetAdapter(log4netRestClientLogger);

            // Setup the log4net logger for the test harness
            ILog log4netTestHarnessExceptionLogger = LogManager.GetLogger(typeof(TestHarness<String, String, TestApplicationComponent, TestAccessLevel>));
            var testHarnessExceptionLogger = new ApplicationLoggingLog4NetAdapter(log4netTestHarnessExceptionLogger);

            // Setup the metric loggers
            const String clientMetricLoggerCategory = "ApplicationAccessTestHarnessRestClient"; 
            String sqlServerMetricsConnectionString = metricsConnectionStringBuilder.ConnectionString;
            Int32 sqlServerRetryCount = metricsDatabaseConnectionConfiguration.RetryCount;
            Int32 sqlServerRetryInterval = metricsDatabaseConnectionConfiguration.RetryInterval;
            Int32 sqlServerOperationTimeout = metricsDatabaseConnectionConfiguration.OperationTimeout;
            Int32 metricLoggerBufferSizeLimit = metricsBufferConfiguration.BufferSizeLimit;

            String sqlServerConnectionString = metricsConnectionStringBuilder.ConnectionString;
            var metricsBufferProcessingStrategyFactory = new MetricsBufferProcessingStrategyFactory();

            using (var httpClient = new HttpClient())
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
                IDataElementStorer<String, String, TestApplicationComponent, TestAccessLevel> dataElementStorer = null;
                dataElementStorer = new DataElementStorer<String, String, TestApplicationComponent, TestAccessLevel>();
                if (loadExistingData == true)
                {
                    var dataElementStorerLoader = new DataElementStorerLoader<String, String, TestApplicationComponent, TestAccessLevel>();
                    using
                    (
                        var persister = new SqlServerAccessManagerTemporalBulkPersister<String, String, TestApplicationComponent, TestAccessLevel>
                        (
                            persisterConnectionStringBuilder.ConnectionString,
                            appAccessDatabaseConnectionConfiguration.RetryCount,
                            appAccessDatabaseConnectionConfiguration.RetryInterval,
                            appAccessDatabaseConnectionConfiguration.OperationTimeout, 
                            new StringUniqueStringifier(),
                            new StringUniqueStringifier(),
                            new EnumUniqueStringifier<TestApplicationComponent>(),
                            new EnumUniqueStringifier<TestAccessLevel>(),
                            new NullLogger(),
                            new NullMetricLogger()
                        )
                    )
                    {
                        dataElementStorerLoader.Load(persister, dataElementStorer, false);
                    }
                }
                if (testHarnessConfiguration.AddOperationDelayTime > 0)
                {
                    dataElementStorer = new DataElementStorerDelayBuffer<String, String, TestApplicationComponent, TestAccessLevel>(testHarnessConfiguration.AddOperationDelayTime, dataElementStorer);
                }

                // Setup TestHarness array parameters
                Double targetOperationsPerSecond = testHarnessConfiguration.TargetOperationsPerSecond;
                Int32 operationsPerSecondPrintFrequency = testHarnessConfiguration.OperationsPerSecondPrintFrequency;
                Int32 previousInitiationTimeWindowSize = testHarnessConfiguration.PreviousOperationInitiationTimeWindowSize;
                var metricsBufferFlushStrategies = new List<BufferFlushStrategyFactoryResult<IBufferProcessingStrategy>>();
                var metricsLoggers = new List<SqlServerMetricLogger>();
                var testAccessManagerQueryProcessors = new List<IAccessManagerQueryProcessor<String, String, TestApplicationComponent, TestAccessLevel>>();
                var testAccessManagerEventProcessors = new List<IAccessManagerEventProcessor<String, String, TestApplicationComponent, TestAccessLevel>>();
                var operationGenerators = new List<IOperationGenerator>();
                var parameterGenerators = new List<IOperationParameterGenerator<String, String, TestApplicationComponent, TestAccessLevel>>();
                var exceptionLoggers = new List<IApplicationLogger>();
                for (Int32 i = 0; i < workerThreadCount; i++)
                {
                    // Setup a metric logger buffer flush strategy for the current worker thread
                    metricsBufferFlushStrategies.Add(metricsBufferProcessingStrategyFactory.MakeProcessingStrategy(metricsBufferConfiguration));
                    // Setup a metric logger for the current worker thread
                    MetricLoggerTypeFilter filteredMetricLogger = null;
                    if (accessManagerRestClientConfiguration.LogMetrics == true)
                    {
                        metricsLoggers.Add(new SqlServerMetricLogger
                        (
                            clientMetricLoggerCategory, sqlServerMetricsConnectionString, sqlServerRetryCount, sqlServerRetryInterval, sqlServerOperationTimeout, metricsBufferFlushStrategies.Last().BufferFlushStrategy, IntervalMetricBaseTimeUnit.Nanosecond, false, metricLoggerLogger
                        ));
                        // Create a filter for the metric logger so that it only logs interval metrics
                        filteredMetricLogger = new MetricLoggerTypeFilter(metricsLoggers.Last(), false, false, false, true);
                    }
                    // Setup AccessManager query clients for the current worker thread
                    AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel> queryClient;
                    if (accessManagerRestClientConfiguration.LogMetrics == true)
                    {
                        queryClient = new AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>
                        (
                            new Uri(accessManagerRestClientConfiguration.AccessManagerQueryUrl),
                            httpClient,
                            new StringUniqueStringifier(),
                            new StringUniqueStringifier(),
                            new EnumUniqueStringifier<TestApplicationComponent>(),
                            new EnumUniqueStringifier<TestAccessLevel>(),
                            accessManagerRestClientConfiguration.RetryCount,
                            accessManagerRestClientConfiguration.RetryInterval,
                            restClientLogger,
                            // We pass the 'unfiltered' metric logger here, as it's only used for logging metrics when retries are required
                            metricsLoggers.Last()
                        );
                    }
                    else
                    {
                        queryClient = new AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>
                        (
                            new Uri(accessManagerRestClientConfiguration.AccessManagerQueryUrl),
                            httpClient,
                            new StringUniqueStringifier(),
                            new StringUniqueStringifier(),
                            new EnumUniqueStringifier<TestApplicationComponent>(),
                            new EnumUniqueStringifier<TestAccessLevel>(),
                            accessManagerRestClientConfiguration.RetryCount,
                            accessManagerRestClientConfiguration.RetryInterval
                        );
                    }

                    if (accessManagerRestClientConfiguration.LogIntervalMetrics == true)
                    {
                        // Setup a decorator metric logger around the client
                        var metricLoggingQueryClient = new AccessManagerMetricLogger<String, String, TestApplicationComponent, TestAccessLevel>
                        (
                            queryClient,
                            filteredMetricLogger
                        );
                        // This is the list of IAccessManager which gets passed to the TestHarness
                        testAccessManagerQueryProcessors.Add(metricLoggingQueryClient);
                    }
                    else
                    {
                        testAccessManagerQueryProcessors.Add(queryClient);
                    }
                    // Setup AccessManager event clients for the current worker thread
                    IAccessManager<String, String, TestApplicationComponent, TestAccessLevel> eventClient;
                    if (accessManagerRestClientConfiguration.LogMetrics == true)
                    {
                        // We pass the 'unfiltered' metric logger here, as it's only used for logging metrics when retries are required
                        eventClient = BuildEventClients(accessManagerRestClientConfiguration, httpClient, restClientLogger, metricsLoggers.Last());
                    }
                    else
                    {
                        eventClient = BuildEventClients(accessManagerRestClientConfiguration, httpClient);
                    }

                    if (accessManagerRestClientConfiguration.LogIntervalMetrics == true)
                    {
                        // Setup a decorator metric logger around the client
                        var metricLoggingEventClient = new AccessManagerMetricLogger<String, String, TestApplicationComponent, TestAccessLevel>
                        (
                            eventClient,
                            filteredMetricLogger
                        );
                        // This is the list of IAccessManager which gets passed to the TestHarness
                        testAccessManagerEventProcessors.Add(metricLoggingEventClient);
                    }
                    else
                    {
                        testAccessManagerEventProcessors.Add(eventClient);
                    }

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
                        new StringifiedGuidGenerator(),
                        operationGeneratorConfiguration.ContainsMethodInvalidParameterGenerationFrequency
                    );
                    parameterGenerators.Add(parameterGenerator);
                    exceptionLoggers.Add(testHarnessExceptionLogger);
                }

                Double exceptionsPerSecondThreshold = testHarnessConfiguration.ExceptionsPerSecondThreshold;
                Int32 previousExceptionOccurenceTimeWindowSize = testHarnessConfiguration.PreviousExceptionOccurenceTimeWindowSize;
                Boolean ignoreKnownAccessManagerExceptions = testHarnessConfiguration.IgnoreKnownAccessManagerExceptions;
                Boolean generatePrimaryAddOperations = testHarnessConfiguration.GeneratePrimaryAddOperations;
                Int64 operationLimit = testHarnessConfiguration.OperationLimit;
                using (var testHarness = new TestHarness<String, String, TestApplicationComponent, TestAccessLevel>
                (
                    workerThreadCount,
                    dataElementStorer,
                    testAccessManagerQueryProcessors, 
                    testAccessManagerEventProcessors, 
                    operationGenerators,
                    parameterGenerators,
                    exceptionLoggers,
                    stopNotifySignal,
                    targetOperationsPerSecond,
                    operationsPerSecondPrintFrequency,
                    previousInitiationTimeWindowSize,
                    exceptionsPerSecondThreshold,
                    previousExceptionOccurenceTimeWindowSize,
                    operationLimit,
                    ignoreKnownAccessManagerExceptions,
                    generatePrimaryAddOperations
                ))
                {
                    foreach (SqlServerMetricLogger currentmetricLogger in metricsLoggers)
                    {
                        // If 'LogMetrics' is false we won't get here, as 'metricsLoggers' will be an empty List
                        currentmetricLogger.Start();
                    }
                    try
                    {
                        var stopSignalThread = new Thread(StopThreadTask);
                        stopSignalThread.Start();
                        if (testHarnessConfiguration.AddUrlReservedCharacterElements == true)
                        {
                            AddUrlReservedCharacterElements(testAccessManagerEventProcessors[0], dataElementStorer);
                        }
                        testHarness.Start();
                        stopNotifySignal.WaitOne();
                        stopped = true;
                        testHarness.Stop();
                    }
                    finally
                    {
                        Console.WriteLine("Disposing AccessManager clients...");
                        foreach (IAccessManagerQueryProcessor<String, String, TestApplicationComponent, TestAccessLevel> currentQueryProcessor in testAccessManagerQueryProcessors)
                        {
                            if (currentQueryProcessor is IDisposable disposableQueryProcessor)
                            {
                                disposableQueryProcessor.Dispose();
                            }
                        }
                        foreach (IAccessManagerEventProcessor<String, String, TestApplicationComponent, TestAccessLevel> currentEventProcessor in testAccessManagerEventProcessors)
                        {
                            if (currentEventProcessor is IDisposable disposableEventProcessor)
                            {
                                disposableEventProcessor.Dispose();
                            }
                        }
                        Console.WriteLine("Stopping and disposing 'metricsLoggers'...");
                        foreach (SqlServerMetricLogger currentmetricLogger in metricsLoggers)
                        {
                            // If 'LogMetrics' is false we won't get here, as 'metricsLoggers' will be an empty List
                            currentmetricLogger.Stop();
                            currentmetricLogger.Dispose();
                        }
                        Console.WriteLine("Disposing 'metricsBufferFlushStrategies'...");
                        foreach (BufferFlushStrategyFactoryResult<IBufferProcessingStrategy> currentBufferFlushStrategy in metricsBufferFlushStrategies)
                        {
                            currentBufferFlushStrategy.DisposeAction.Invoke();
                        }
                        Console.WriteLine("Disposing 'stopNotifySignal'...");
                        stopNotifySignal.Dispose();
                        Console.WriteLine("Flushing log4net logs...");
                        LogManager.Flush(10000);
                        if (testHarnessConfiguration.GeneratePrimaryAddOperations == false)
                        {
                            PrintUnmappedPrimaryElementCounts(dataElementStorer);
                        }

                        Console.WriteLine("Testing Complete");
                    }
                }
            }
        }

        protected static IAccessManager<String, String, TestApplicationComponent, TestAccessLevel> BuildEventClients
        (
            AccessManagerRestClientConfiguration accessManagerRestClientConfiguration, 
            HttpClient httpClient
        )
        {
            if (accessManagerRestClientConfiguration.AccessManagerEventUrls.Count == 1)
            {
                return new AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>
                (
                    new Uri(accessManagerRestClientConfiguration.AccessManagerEventUrls[0]),
                    httpClient,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<TestApplicationComponent>(),
                    new EnumUniqueStringifier<TestAccessLevel>(),
                    accessManagerRestClientConfiguration.RetryCount,
                    accessManagerRestClientConfiguration.RetryInterval
                );
            }
            else
            {
                var eventProcessors = new List<AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>>();
                foreach (String currentEventProcessorUrl in accessManagerRestClientConfiguration.AccessManagerEventUrls)
                {
                    var currentEventProcessor = new AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>
                    (
                        new Uri(currentEventProcessorUrl),
                        httpClient,
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new EnumUniqueStringifier<TestApplicationComponent>(),
                        new EnumUniqueStringifier<TestAccessLevel>(),
                        accessManagerRestClientConfiguration.RetryCount,
                        accessManagerRestClientConfiguration.RetryInterval
                    );
                    eventProcessors.Add(currentEventProcessor);
                }

                return new AccessManagerClientDistributor<String, String, TestApplicationComponent, TestAccessLevel>(eventProcessors);
            }
        }

        protected static IAccessManager<String, String, TestApplicationComponent, TestAccessLevel> BuildEventClients
        (
            AccessManagerRestClientConfiguration accessManagerRestClientConfiguration,
            HttpClient httpClient,
            ApplicationLoggingLog4NetAdapter restClientLogger, 
            SqlServerMetricLogger metricLogger
        )
        {
            if (accessManagerRestClientConfiguration.AccessManagerEventUrls.Count == 1)
            {
                return new AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>
                (
                    new Uri(accessManagerRestClientConfiguration.AccessManagerEventUrls[0]),
                    httpClient,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<TestApplicationComponent>(),
                    new EnumUniqueStringifier<TestAccessLevel>(),
                    accessManagerRestClientConfiguration.RetryCount,
                    accessManagerRestClientConfiguration.RetryInterval,
                    restClientLogger, 
                    metricLogger
                );
            }
            else
            {
                var eventProcessors = new List<AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>>();
                foreach (String currentEventProcessorUrl in accessManagerRestClientConfiguration.AccessManagerEventUrls)
                {
                    var currentEventProcessor = new AccessManagerClient<String, String, TestApplicationComponent, TestAccessLevel>
                    (
                        new Uri(currentEventProcessorUrl),
                        httpClient,
                        new StringUniqueStringifier(),
                        new StringUniqueStringifier(),
                        new EnumUniqueStringifier<TestApplicationComponent>(),
                        new EnumUniqueStringifier<TestAccessLevel>(),
                        accessManagerRestClientConfiguration.RetryCount,
                        accessManagerRestClientConfiguration.RetryInterval, 
                        restClientLogger,
                        metricLogger
                    );
                    eventProcessors.Add(currentEventProcessor);
                }

                return new AccessManagerClientDistributor<String, String, TestApplicationComponent, TestAccessLevel>(eventProcessors);
            }
        }

        protected static void PrintUnmappedPrimaryElementCounts(IDataElementStorer<String, String, TestApplicationComponent, TestAccessLevel> dataElementStorer)
        {
            Console.WriteLine("User count: " + dataElementStorer.UserCount);
            Console.WriteLine("Unmapped user count: " + dataElementStorer.GetUnmappedUsers().Count());
            foreach (String currentUnmappedUser in dataElementStorer.GetUnmappedUsers().OrderBy((user) => { return user; }))
            {
                Console.WriteLine(currentUnmappedUser);
            }
            Console.WriteLine();

            Console.WriteLine("Group count: " + dataElementStorer.GroupCount);
            Console.WriteLine("Unmapped group count: " + dataElementStorer.GetUnmappedGroups().Count());
            foreach (String currentUnmappedGroup in dataElementStorer.GetUnmappedGroups().OrderBy((group) => { return group; }))
            {
                Console.WriteLine(currentUnmappedGroup);
            }
            Console.WriteLine();

            Console.WriteLine("EntityType count: " + dataElementStorer.EntityTypeCount);
            Console.WriteLine("Entity count: " + dataElementStorer.EntityCount);
            Console.WriteLine("Unmapped entity count: " + dataElementStorer.GetUnmappedEntities().Count());
            foreach (Tuple<String, String> currentUnmappedEntity in dataElementStorer.GetUnmappedEntities().OrderBy((entityAndType) => { return entityAndType.Item1 + entityAndType.Item2; }))
            {
                Console.WriteLine($"{currentUnmappedEntity.Item1}, {currentUnmappedEntity.Item2}");
            }
            Console.WriteLine();
        }

        protected static void AddUrlReservedCharacterElements
        (
            IAccessManagerEventProcessor<String, String, TestApplicationComponent, TestAccessLevel> eventProcessor,
            IDataElementStorer<String, String, TestApplicationComponent, TestAccessLevel> dataElementStorer
        )
        {
            const String urlReservedCharcters = "! * ' ( ) ; : @ & = + $ , / ? % # [ ]";
            try
            {
                eventProcessor.AddUser(urlReservedCharcters);
                dataElementStorer.AddUser(urlReservedCharcters);
                eventProcessor.AddGroup(urlReservedCharcters);
                dataElementStorer.AddGroup(urlReservedCharcters);
                eventProcessor.AddEntityType(urlReservedCharcters);
                dataElementStorer.AddEntityType(urlReservedCharcters);
                eventProcessor.AddEntity(urlReservedCharcters, urlReservedCharcters);
                dataElementStorer.AddEntity(urlReservedCharcters, urlReservedCharcters);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to add URL reserved character elements.", e);
            }
        }
    }
}
