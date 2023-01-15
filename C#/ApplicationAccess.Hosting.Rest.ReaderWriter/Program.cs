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

using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.SqlServer;
using ApplicationAccess.Hosting.Models.Options;
using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using ApplicationLogging;
using ApplicationLogging.Adapters.MicrosoftLoggingExtensions;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;
using Microsoft.Extensions.DependencyInjection;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Validate and populate the configuration
            var accessManagerSqlServerConnectionOptions = new AccessManagerSqlServerConnectionOptions();
            var eventBufferFlushingOptions = new EventBufferFlushingOptions();
            var metricLoggingOptions = new MetricLoggingOptions();
            ValidateAndPopulateConfiguration
            (
                builder,
                accessManagerSqlServerConnectionOptions,
                eventBufferFlushingOptions,
                metricLoggingOptions
            );

            // Create the ReaderWriterNode constructor parameters from the configuration
            //   TODO: Need to figure out how to take this from appsettings.json, as logger setup in there won't be reflected in this logger factory
            ILoggerFactory loggerFactory = LoggerFactory.Create((ILoggingBuilder config) => { config.AddConsole(); });
            ReaderWriterNodeConstructorParameters constructorParameters = CreateReaderWriterNodeConstructorParameters
            (
                accessManagerSqlServerConnectionOptions, 
                eventBufferFlushingOptions, 
                metricLoggingOptions,
                loggerFactory
            );

            // Create the ReaderWriterNode
            ReaderWriterNode<String, String, String, String> readerWriterNode = null;
            if (metricLoggingOptions.MetricLoggingEnabled == false)
            {
                readerWriterNode = new ReaderWriterNode<String, String, String, String>
                (
                    constructorParameters.EventBufferFlushStrategy,
                    constructorParameters.EventPersister,
                    constructorParameters.EventPersister
                );
            }
            else
            {
                readerWriterNode = new ReaderWriterNode<String, String, String, String>
                (
                    constructorParameters.EventBufferFlushStrategy,
                    constructorParameters.EventPersister,
                    constructorParameters.EventPersister, 
                    constructorParameters.MetricLogger
                );
            }

            // Create the ReaderWriterNodeHostedServiceWrapper
            var readerWriterNodeHostedServiceWrapper = new ReaderWriterNodeHostedServiceWrapper
            (
                constructorParameters,
                readerWriterNode,
                loggerFactory
            );

            // Register the hosted service wrapper and the ReaderWriterNode
            builder.Services.AddHostedService<ReaderWriterNodeHostedServiceWrapper>((IServiceProvider serviceProvider) => 
            { 
                return readerWriterNodeHostedServiceWrapper; 
            });
            builder.Services.AddSingleton<ReaderWriterNode<String, String, String, String>>(readerWriterNode);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        /// <summary>
        /// Validates the application configuration (e.g. that stored in 'appsettings.json') and populates / binds to the specified options objects.
        /// </summary>
        /// <param name="builder">The builder for the application.</param>
        protected static void ValidateAndPopulateConfiguration(
            WebApplicationBuilder builder,
            AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions,
            EventBufferFlushingOptions eventBufferFlushingOptions,
            MetricLoggingOptions metricLoggingOptions
        )
        {
            // TODO: Likely some of this validation will be repeated in other REST hosting process, so these checks could be split up and put in their own 'Validation' project

            ValidateConfigurationSection(builder, accessManagerSqlServerConnectionOptions, AccessManagerSqlServerConnectionOptions.AccessManagerSqlServerConnectionOptionsName);
            ValidateConfigurationSection(builder, eventBufferFlushingOptions, EventBufferFlushingOptions.EventBufferFlushingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions, MetricLoggingOptions.MetricLoggingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricBufferProcessing, MetricBufferProcessingOptions.MetricBufferProcessingOptionsName);
            ValidateConfigurationSection(builder, metricLoggingOptions.MetricsSqlServerConnection, MetricsSqlServerConnectionOptions.MetricsSqlServerConnectionOptionsName);
        }

        /// <summary>
        /// Validates a specific section of the application configuration.
        /// </summary>
        /// <param name="builder">The builder for the application.</param>
        /// <param name="optionsInstance">An instance of the class holding the section of the configuration.</param>
        /// <param name="optionsSectionName">The name of the section within the configuration (e.g. in 'appsettings.json').</param>
        protected static void ValidateConfigurationSection(WebApplicationBuilder builder, Object optionsInstance, String optionsSectionName)
        {
            builder.Configuration.GetSection(optionsSectionName).Bind(optionsInstance);
            var context = new ValidationContext(optionsInstance);
            Validator.ValidateObject(optionsInstance, context, true);
        }

        /// <summary>
        /// Creates a set of constructor parameters for a <see cref="ReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> object based on application configuration objects.
        /// </summary>
        protected static ReaderWriterNodeConstructorParameters CreateReaderWriterNodeConstructorParameters
        (
            AccessManagerSqlServerConnectionOptions accessManagerSqlServerConnectionOptions,
            EventBufferFlushingOptions eventBufferFlushingOptions,
            MetricLoggingOptions metricLoggingOptions, 
            ILoggerFactory loggerFactory
        )
        {
            const String sqlServerMetricLoggerCategoryName = "ApplicationAccessReaderWriterNode";

            var eventBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit, 
                eventBufferFlushingOptions.FlushLoopInterval
            );

            var connectionStringBuilder = new SqlConnectionStringBuilder();
            connectionStringBuilder.DataSource = accessManagerSqlServerConnectionOptions.DataSource;
            // TODO: Need to enable this once I find a way to inject cert details etc into
            connectionStringBuilder.Encrypt = false;
            connectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
            connectionStringBuilder.InitialCatalog = accessManagerSqlServerConnectionOptions.InitialCatalog;
            connectionStringBuilder.UserID = accessManagerSqlServerConnectionOptions.UserId;
            connectionStringBuilder.Password = accessManagerSqlServerConnectionOptions.Password;
            String connectionString = connectionStringBuilder.ConnectionString;
            IApplicationLogger eventPersisterLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
            (
                loggerFactory.CreateLogger<SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>>()
            );
            var eventPersister = new SqlServerAccessManagerTemporalBulkPersister<String, String, String, String>
            (
                connectionString,
                accessManagerSqlServerConnectionOptions.RetryCount,
                accessManagerSqlServerConnectionOptions.RetryInterval,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                eventPersisterLogger
            );

            WorkerThreadBufferProcessorBase? metricLoggerBufferProcessingStrategy = null;
            SqlServerMetricLogger? metricLogger = null;
            if (metricLoggingOptions.MetricLoggingEnabled == true)
            {
                MetricBufferProcessingOptions metricBufferProcessingOptions = metricLoggingOptions.MetricBufferProcessing;
                switch (metricBufferProcessingOptions.BufferProcessingStrategy)
                {
                    case MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor:
                        metricLoggerBufferProcessingStrategy = new SizeLimitedBufferProcessor(metricBufferProcessingOptions.BufferSizeLimit);
                        break;
                    case MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor:
                        metricLoggerBufferProcessingStrategy = new LoopingWorkerThreadBufferProcessor(metricBufferProcessingOptions.DequeueOperationLoopInterval);
                        break;
                    default:
                        throw new Exception($"Encountered unhandled {nameof(MetricBufferProcessingStrategyImplementation)} '{metricBufferProcessingOptions.BufferProcessingStrategy}' while attempting to create {nameof(ReaderWriterNode<String, String, String, String>)} constructor parameters.");
                }
                MetricsSqlServerConnectionOptions metricsSqlServerConnectionOptions = metricLoggingOptions.MetricsSqlServerConnection;
                connectionStringBuilder = new SqlConnectionStringBuilder();
                connectionStringBuilder.DataSource = metricsSqlServerConnectionOptions.DataSource;
                // TODO: Need to enable this once I find a way to inject cert details etc into
                connectionStringBuilder.Encrypt = false;
                connectionStringBuilder.Authentication = SqlAuthenticationMethod.SqlPassword;
                connectionStringBuilder.InitialCatalog = metricsSqlServerConnectionOptions.InitialCatalog;
                connectionStringBuilder.UserID = metricsSqlServerConnectionOptions.UserId;
                connectionStringBuilder.Password = metricsSqlServerConnectionOptions.Password;
                connectionString = connectionStringBuilder.ConnectionString;
                IApplicationLogger metricLoggerLogger = new ApplicationLoggingMicrosoftLoggingExtensionsAdapter
                (
                    loggerFactory.CreateLogger<SqlServerMetricLogger>()
                );
                metricLogger = new SqlServerMetricLogger
                (
                    sqlServerMetricLoggerCategoryName,
                    connectionString,
                    metricsSqlServerConnectionOptions.RetryCount,
                    metricsSqlServerConnectionOptions.RetryInterval,
                    metricLoggerBufferProcessingStrategy, 
                    true,
                    metricLoggerLogger
                );
            }

            return new ReaderWriterNodeConstructorParameters
            (
                eventBufferFlushStrategy, 
                eventPersister, 
                metricLoggerBufferProcessingStrategy, 
                metricLogger
            );
        }
    }
}