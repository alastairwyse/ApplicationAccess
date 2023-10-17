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
using System.Reflection;
using NUnit.Framework;

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// Contains static methods which implement assertions on the types and values of non-public fields of objects under test.
    /// </summary>
    public static class NonPublicFieldAssert
    {
        /// <summary>
        /// Verifies whether the non-public field at the end of a sequence of fields is of the specified type.
        /// </summary>
        /// <typeparam name="TField">The expected type of the field.</typeparam>
        /// <param name="fieldNamePath">The 'path' of field names required to arrive at the destination field (i.e. the last element).  These are processed in order, with subsequent names being of fields of child (composed) objects, fields of child's child object, and so forth.</param>
        /// <param name="testObject">The object holding the field with the first field name in parameter <paramref name="fieldNamePath"/>.</param>
        public static void IsOfType<TField>(IList<String> fieldNamePath, Object testObject)
        {
            AssertionResult result = NonPublicFieldIsOfType<TField>(fieldNamePath, testObject);
            if (result.Result == false)
            {
                Assert.Fail(result.FailureDetails);
            }
        }

        /// <summary>
        /// Verifies whether the non-public field at the end of a sequence of fields has the specified value.
        /// </summary>
        /// <typeparam name="TField">The expected type of the field.</typeparam>
        /// <param name="fieldNamePath">The 'path' of field names required to arrive at the destination field (i.e. the last element).  These are processed in order, with subsequent names being of fields of child (composed) objects, fields of child's child object, and so forth.</param>
        /// <param name="expectedValue">The expected value of the field.</param>
        /// <param name="testObject">The object holding the field with the first field name in parameter <paramref name="fieldNamePath"/>.</param>
        public static void HasValue<TField>(IList<String> fieldNamePath, TField expectedValue, Object testObject)
        {
            AssertionResult result = NonPublicFieldHasValue<TField>(fieldNamePath, expectedValue, testObject);
            if (result.Result == false)
            {
                Assert.Fail(result.FailureDetails);
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Checks whether the non-public field at the end of a sequence of fields is of the specified type.
        /// </summary>
        /// <typeparam name="TField">The expected type of the field.</typeparam>
        /// <param name="fieldNamePath">The 'path' of field names required to arrive at the destination field (i.e. the last element).  These are processed in order, with subsequent names being of fields of child (composed) objects, fields of child's child object, and so forth.</param>
        /// <param name="testObject">The object holding the field with the first field name in parameter <paramref name="fieldNamePath"/>.</param>
        /// <returns>An <see cref="AssertionResult"/> containing the result.</returns>
        private static AssertionResult NonPublicFieldIsOfType<TField>(IList<String> fieldNamePath, Object testObject)
        {
            try
            {
                TField actualFieldValue = GetNonPublicFieldValueRecurse<TField>(fieldNamePath, 0, testObject);
            }
            catch (Exception e)
            {
                return new AssertionResult(e.Message);
            }

            return new AssertionResult();
        }

        /// <summary>
        /// Checks whether the non-public field at the end of a sequence of fields has the specified value.
        /// </summary>
        /// <typeparam name="TField">The expected type of the field.</typeparam>
        /// <param name="fieldNamePath">The 'path' of field names required to arrive at the destination field (i.e. the last element).  These are processed in order, with subsequent names being of fields of child (composed) objects, fields of child's child object, and so forth.</param>
        /// <param name="expectedValue">The expected value of the field.</param>
        /// <param name="testObject">The object holding the field with the first field name in parameter <paramref name="fieldNamePath"/>.</param>
        /// <returns>An <see cref="AssertionResult"/> containing the result.</returns>
        private static AssertionResult NonPublicFieldHasValue<TField>(IList<String> fieldNamePath, TField expectedValue, Object testObject)
        {
            try
            {
                TField actualFieldValue = GetNonPublicFieldValueRecurse<TField>(fieldNamePath, 0, testObject);
                if (expectedValue.Equals(actualFieldValue) == false)
                    throw new Exception($"Expected value of field '{fieldNamePath[fieldNamePath.Count - 1]}' is '{expectedValue.ToString()}' but actual value was '{actualFieldValue.ToString()}'.");
            }
            catch (Exception e)
            {
                return new AssertionResult(e.Message);
            }

            return new AssertionResult();
        }

        /// <summary>
        /// Recursively attempts to retrieve a non-public field from an object via a sequence of names of fields.
        /// </summary>
        /// <typeparam name="TField">The expected type of the field.</typeparam>
        /// <param name="fieldNamePath">The 'path' of field names required to arrive at the destination field (i.e. the last element).  These are processed in order, with subsequent names being of fields of child (composed) objects, fields of child's child object, and so forth.</param>
        /// <param name="fieldNamePathIndex">The index of the current field name within the <paramref name="fieldNamePath"/> parameter.</param>
        /// <param name="currentObject">The current child (composed) object.</param>
        /// <returns>The value of the specified field.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Parameter <paramref name="fieldNamePathIndex"/> was out of range.</exception>
        /// <exception cref="ArgumentException">The field was not of the expected type <typeparamref name="TField"/>.</exception>
        private static TField GetNonPublicFieldValueRecurse<TField>(IList<String> fieldNamePath, Int32 fieldNamePathIndex, Object currentObject)
        {
            if (fieldNamePathIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(fieldNamePathIndex), $"Parameter '{nameof(fieldNamePathIndex)}' with value {fieldNamePathIndex} cannot be less than 0.");
            if (fieldNamePathIndex >= fieldNamePath.Count)
                throw new ArgumentOutOfRangeException(nameof(fieldNamePathIndex), $"Parameter '{nameof(fieldNamePathIndex)}' with value {fieldNamePathIndex} cannot greater than or equal to the length of parameter '{nameof(fieldNamePath)}' with value {fieldNamePath.Count}.");
            if (fieldNamePathIndex < (fieldNamePath.Count - 1))
            {
                // Continue recursing
                Object nextObject = GetNonPublicField(currentObject, fieldNamePath[fieldNamePathIndex]);
                return GetNonPublicFieldValueRecurse<TField>(fieldNamePath, fieldNamePathIndex + 1, nextObject);
            }
            else
            {
                // Attempt to return the field value
                Object fieldValue = GetNonPublicField(currentObject, fieldNamePath[fieldNamePathIndex]);
                if (fieldValue.GetType() != typeof(TField))
                    throw new ArgumentException($"Field with name '{fieldNamePath[fieldNamePathIndex]}' was expected to be of type '{typeof(TField).FullName}' but was of type '{fieldValue.GetType().FullName}'.");

                return (TField)fieldValue;
            }
        }

        /// <summary>
        /// Attempts to retrieve a non-public field from the specified object.
        /// </summary>
        /// <param name="currentObject">The object to retrieve the field from.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The value of the field.</returns>
        /// <exception cref="ArgumentException">The object did contain a non-public field with the specified name.</exception>
        private static Object GetNonPublicField(Object currentObject, String fieldName)
        {
            FieldInfo fieldInfo = currentObject.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null)
            {
                throw new ArgumentException($"The specified object did not contain a non-public field with name '{fieldName}'.");
            }

            return fieldInfo.GetValue(currentObject);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Container class holding the result of an assertion.
        /// </summary>
        private class AssertionResult
        {
            /// <summary>The result of the assertion.</summary>
            public Boolean Result { get; private set; }

            /// <summary>Details of the failure of the assertion if it was found to be false.  Null if it was found to be true.</summary>
            public String FailureDetails { get; private set; }

            /// <summary>
            /// Initialises a new instance of theApplicationAccess.UnitTests.NonPublicFieldAssert+AssertionResult class, where the result of the assertion is true.
            /// </summary>
            public AssertionResult()
            {
                Result = true;
                FailureDetails = null;
            }

            /// <summary>
            /// Initialises a new instance of theApplicationAccess.UnitTests.NonPublicFieldAssert+AssertionResult class, where the result of the assertion is false.
            /// </summary>
            /// <param name="failureDetails">Details of why the assertion was found to be false.</param>
            public AssertionResult(String failureDetails)
            {
                if (String.IsNullOrWhiteSpace(failureDetails) == true)
                    throw new ArgumentException($"Parameter '{nameof(failureDetails)}' cannot be null or blank.", nameof(failureDetails));

                Result = false;
                FailureDetails = failureDetails;
            }
        }

        #endregion
    }
}
