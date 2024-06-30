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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.ApplicationInitializer class.
    /// </summary>
    public class ApplicationInitializerTests
    {
        protected ApplicationInitializer testApplicationInitializer;

        [SetUp]
        protected void SetUp()
        {
            testApplicationInitializer = new ApplicationInitializer();
        }

        [Test]
        public void Constructor_ArgsParameterNull()
        {
            var testParameters = new ApplicationInitializerParameters();

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith("Property 'Args' of parameters object cannot be null."));
            Assert.AreEqual("Args", e.ParamName);
        }

        [Test]
        public void Constructor_SwaggerVersionStringParameterWhiteSpace()
        {
            var testParameters = new ApplicationInitializerParameters()
            {
                Args = new String[] { }
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith("Property 'SwaggerVersionString' of parameters object cannot be null or empty."));
            Assert.AreEqual("SwaggerVersionString", e.ParamName);
        }

        [Test]
        public void Constructor_SwaggerApplicationNameParameterWhiteSpace()
        {
            var testParameters = new ApplicationInitializerParameters()
            {
                Args = new String[] { },
                SwaggerVersionString = "v1"
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith("Property 'SwaggerApplicationName' of parameters object cannot be null or empty."));
            Assert.AreEqual("SwaggerApplicationName", e.ParamName);
        }

        [Test]
        public void Constructor_SwaggerApplicationDescriptionParameterWhiteSpace()
        {
            var testParameters = new ApplicationInitializerParameters()
            {
                Args = new String[] { },
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess"
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith("Property 'SwaggerApplicationDescription' of parameters object cannot be null or empty."));
            Assert.AreEqual("SwaggerApplicationDescription", e.ParamName);
        }

        [Test]
        public void Constructor_ExceptionToHttpStatusCodeMappingsParameterTypeNotException()
        {
            var testParameters = new ApplicationInitializerParameters()
            {
                Args = new String[] { },
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess",
                SwaggerApplicationDescription = "Node in a distributed/scaled deployment of ApplicationAccess which caches events", 
                ExceptionToHttpStatusCodeMappings = new List<Tuple<Type, HttpStatusCode>>()
                {
                    new Tuple<Type, HttpStatusCode>(typeof(StringBuilder), HttpStatusCode.BadRequest)
                }
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith($"Property 'ExceptionToHttpStatusCodeMappings' of parameters object contains type '{typeof(StringBuilder).FullName}' which does not derive from '{typeof(Exception).FullName}'."));
            Assert.AreEqual("ExceptionToHttpStatusCodeMappings", e.ParamName);
        }

        [Test]
        public void Constructor_ExceptionTypesMappedToStandardHttpErrorResponseParameterNotException()
        {
            var testParameters = new ApplicationInitializerParameters()
            {
                Args = new String[] { },
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess",
                SwaggerApplicationDescription = "Node in a distributed/scaled deployment of ApplicationAccess which caches events",
                ExceptionToHttpStatusCodeMappings = new List<Tuple<Type, HttpStatusCode>>()
                {
                    new Tuple<Type, HttpStatusCode>(typeof(NotFoundException), HttpStatusCode.NotFound)
                },
                ExceptionTypesMappedToStandardHttpErrorResponse = new List<Type>() 
                {
                    typeof(StringBuilder)
                }
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith($"Property 'ExceptionTypesMappedToStandardHttpErrorResponse' of parameters object contains type '{typeof(StringBuilder).FullName}' which does not derive from '{typeof(Exception).FullName}'."));
            Assert.AreEqual("ExceptionTypesMappedToStandardHttpErrorResponse", e.ParamName);
        }

        [Test]
        public void Constructor_ExceptionToCustomHttpErrorResponseGeneratorFunctionMappingsParameterTypeNotException()
        {
            var testParameters = new ApplicationInitializerParameters()
            {
                Args = new String[] { },
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess",
                SwaggerApplicationDescription = "Node in a distributed/scaled deployment of ApplicationAccess which caches events",
                ExceptionToHttpStatusCodeMappings = new List<Tuple<Type, HttpStatusCode>>()
                {
                    new Tuple<Type, HttpStatusCode>(typeof(NotFoundException), HttpStatusCode.BadRequest)
                },
                ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings = new List<Tuple<Type, Func<Exception, HttpErrorResponse>>>()
                {
                    new Tuple<Type, Func<Exception, HttpErrorResponse>>
                    (
                        typeof(StringBuilder), 
                        (Exception exception) => { return new HttpErrorResponse("ErrorCode", "ErrorMessage"); }
                    )
                }
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith($"Property 'ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings' of parameters object contains type '{typeof(StringBuilder).FullName}' which does not derive from '{typeof(Exception).FullName}'."));
            Assert.AreEqual("ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings", e.ParamName);
        }

        [Test]
        public void Constructor_FileLoggingEnabledAndLogFileNamePrefixParameterNull()
        {
            var testParameters = new ApplicationInitializerParameters()
            {
                Args = new String[] { },
                SwaggerVersionString = "v1",
                SwaggerApplicationName = "ApplicationAccess",
                SwaggerApplicationDescription = "Node in a distributed/scaled deployment of ApplicationAccess which caches events",
                LogFilePath = @"C:\Temp"
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testApplicationInitializer.Initialize<FakeHostedService>(testParameters);
            });

            Assert.That(e.Message, Does.StartWith("Property 'LogFileNamePrefix' of parameters object cannot be null or empty."));
            Assert.AreEqual("LogFileNamePrefix", e.ParamName);
        }

        #region Nested Classes

        protected class FakeHostedService : IHostedService
        {
            /// <inheritdoc/>
            public Task StartAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            /// <inheritdoc/>
            public Task StopAsync(CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
