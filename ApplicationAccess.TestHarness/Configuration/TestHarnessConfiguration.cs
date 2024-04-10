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

namespace ApplicationAccess.TestHarness.Configuration
{
#pragma warning disable 0649

    /// <summary>
    /// Model/container class holding <see cref="TestHarness{TUser, TGroup, TComponent, TAccess}"/> configuration.
    /// </summary>
    class TestHarnessConfiguration
    {
        public String TestProfile;
        public Boolean LoadExistingData;
        public Int32 ThreadCount;
        public Double TargetOperationsPerSecond;
        public Int32 OperationsPerSecondPrintFrequency;
        public Int32 PreviousOperationInitiationTimeWindowSize;
        public Double ExceptionsPerSecondThreshold;
        public Int32 PreviousExceptionOccurenceTimeWindowSize;
        public Boolean IgnoreKnownAccessManagerExceptions;
        public Int64 OperationLimit;
        public Int32 AddOperationDelayTime;
        public Boolean GeneratePrimaryAddOperations;
    }

    #pragma warning restore 0649
}
