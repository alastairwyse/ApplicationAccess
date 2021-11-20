/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Threading;
using NUnit.Framework;

namespace ApplicationAccess.Utilities.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Utilities.LockManager class.
    /// </summary>
    public class LockManagerTests
    {
        private LockManager testLockManager;

        [SetUp]
        protected void SetUp()
        {
            testLockManager = new LockManager();
        }

        [Test]
        public void RegisterLockObject_NullLockObjectParameter()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testLockManager.RegisterLockObject(null);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'lockObject' cannot be null."));
            Assert.AreEqual("lockObject", e.ParamName);
        }

        [Test]
        public void RegisterLockObject_CalledAfterAcquireLocksAndInvokeActionHasBeenCalled()
        {
            var testLockObject1 = new Object();
            var testLockObject2 = new Object();
            testLockManager.RegisterLockObject(testLockObject1);
            testLockManager.AcquireLocksAndInvokeAction(testLockObject1, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => { }));

            var e = Assert.Throws<InvalidOperationException>(delegate
            {
                testLockManager.RegisterLockObject(testLockObject2);
            });

            Assert.That(e.Message, Does.StartWith("Cannot register new lock objects after the AcquireLocksAndInvokeAction() method has been called."));
        }

        [Test]
        public void RegisterLockObject_LockObjectParameterAlreadyRegistered()
        {
            var testLockObject = new Object();
            testLockManager.RegisterLockObject(testLockObject);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObject(testLockObject);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'lockObject' has already been registered as a lock object."));
            Assert.AreEqual("lockObject", e.ParamName);
        }

        [Test]
        public void RegisterLockObjectDependency_NullDependencyFromObjectParameter()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(null, new Object());
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'dependencyFromObject' cannot be null."));
            Assert.AreEqual("dependencyFromObject", e.ParamName);
        }

        [Test]
        public void RegisterLockObjectDependency_NullDependencyToObjectParameter()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(new Object(), null);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'dependencyToObject' cannot be null."));
            Assert.AreEqual("dependencyToObject", e.ParamName);
        }

        [Test]
        public void RegisterLockObjectDependency_CalledAfterAcquireLocksAndInvokeActionHasBeenCalled()
        {
            var testLockObject1 = new Object();
            var testLockObject2 = new Object();
            testLockManager.RegisterLockObject(testLockObject1);
            testLockManager.RegisterLockObject(testLockObject2);
            testLockManager.AcquireLocksAndInvokeAction(testLockObject1, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => { }));

            var e = Assert.Throws<InvalidOperationException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(testLockObject1, testLockObject2);
            });

            Assert.That(e.Message, Does.StartWith("Cannot register new lock object dependencies after the AcquireLocksAndInvokeAction() method has been called."));
        }

        [Test]
        public void RegisterLockObjectDependency_ParametersContainSameObject()
        {
            var testLockObject = new Object();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(testLockObject, testLockObject);
            });

            Assert.That(e.Message, Does.StartWith("Parameters 'dependencyFromObject' and 'dependencyToObject' cannot contain the same object."));
            Assert.AreEqual("dependencyToObject", e.ParamName);
        }

        [Test]
        public void RegisterLockObjectDependency_DependencyFromObjectHasNotBeenRegistered()
        {
            var fromObject = new Object();
            var toObject = new Object();
            testLockManager.RegisterLockObject(toObject);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(fromObject, toObject);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'dependencyFromObject' has not been registered."));
            Assert.AreEqual("dependencyFromObject", e.ParamName);
        }

        [Test]
        public void RegisterLockObjectDependency_DependencyToObjectHasNotBeenRegistered()
        {
            var fromObject = new Object();
            var toObject = new Object();
            testLockManager.RegisterLockObject(fromObject);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(fromObject, toObject);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'dependencyToObject' has not been registered."));
            Assert.AreEqual("dependencyToObject", e.ParamName);
        }

        [Test]
        public void RegisterLockObjectDependency_RegistrationWouldCreateCircularReference()
        {
            // Test dependency graph is as below...
            // 
            //        > 3 -> 4
            //       /
            // 1 -> 2
            //       \
            //        > 5
            //
            // 6 -> 7

            var object1 = new Object();
            var object2 = new Object();
            var object3 = new Object();
            var object4 = new Object();
            var object5 = new Object();
            var object6 = new Object();
            var object7 = new Object();
            testLockManager.RegisterLockObject(object1);
            testLockManager.RegisterLockObject(object2);
            testLockManager.RegisterLockObject(object3);
            testLockManager.RegisterLockObject(object4);
            testLockManager.RegisterLockObject(object5);
            testLockManager.RegisterLockObject(object6);
            testLockManager.RegisterLockObject(object7);
            testLockManager.RegisterLockObjectDependency(object1, object2);
            testLockManager.RegisterLockObjectDependency(object2, object3);
            testLockManager.RegisterLockObjectDependency(object2, object5);
            testLockManager.RegisterLockObjectDependency(object3, object4);
            testLockManager.RegisterLockObjectDependency(object6, object7);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(object4, object1);
            });

            Assert.That(e.Message, Does.StartWith("A dependency between the objects in the specified parameters cannot be created as it would cause a circular reference."));
            Assert.AreEqual("dependencyToObject", e.ParamName);


            e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(object3, object1);
            });

            Assert.That(e.Message, Does.StartWith("A dependency between the objects in the specified parameters cannot be created as it would cause a circular reference."));
            Assert.AreEqual("dependencyToObject", e.ParamName);


            e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(object3, object2);
            });

            Assert.That(e.Message, Does.StartWith("A dependency between the objects in the specified parameters cannot be created as it would cause a circular reference."));
            Assert.AreEqual("dependencyToObject", e.ParamName);


            e = Assert.Throws<ArgumentException>(delegate
            {
                testLockManager.RegisterLockObjectDependency(object7, object6);
            });

            Assert.That(e.Message, Does.StartWith("A dependency between the objects in the specified parameters cannot be created as it would cause a circular reference."));
            Assert.AreEqual("dependencyToObject", e.ParamName);
        }

        [Test]
        public void AcquireLocksAndInvokeAction()
        {
            // Test dependency graph is as below...
            //
            //           <-------------j
            //          /             /
            //   g-->--a-<--e        / 
            //             /        /
            //   h-->--b--<---f    /
            //             \      /
            //              i    /
            //             /    /
            //     <--n---<----<   
            //    /      
            //   d
            //
            //    k--<--l--<--m

            var a = new Object();
            var b = new Object();
            var d = new Object();
            var e = new Object();
            var f = new Object();
            var g = new Object();
            var h = new Object();
            var i = new Object();
            var j = new Object();
            var k = new Object();
            var l = new Object();
            var m = new Object();
            var n = new Object();
            testLockManager.RegisterLockObjects(new Object[] { a, b, d, e, f, g, h, i, j, k, l, m, n });
            testLockManager.RegisterLockObjectDependency(g, a);
            testLockManager.RegisterLockObjectDependency(j, a);
            testLockManager.RegisterLockObjectDependency(e, a);
            testLockManager.RegisterLockObjectDependency(e, b);
            testLockManager.RegisterLockObjectDependency(h, b);
            testLockManager.RegisterLockObjectDependency(f, b);
            testLockManager.RegisterLockObjectDependency(i, b);
            testLockManager.RegisterLockObjectDependency(i, n);
            testLockManager.RegisterLockObjectDependency(j, n);
            testLockManager.RegisterLockObjectDependency(n, d);
            testLockManager.RegisterLockObjectDependency(l, k);
            testLockManager.RegisterLockObjectDependency(m, l);

            testLockManager.AcquireLocksAndInvokeAction(a, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsTrue(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(b, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsTrue(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(d, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsTrue(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(e, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsTrue(Monitor.IsEntered(a));
                Assert.IsTrue(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsTrue(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(f, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsTrue(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsTrue(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(g, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsTrue(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsTrue(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(h, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsTrue(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsTrue(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(i, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsTrue(Monitor.IsEntered(b));
                Assert.IsTrue(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsTrue(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsTrue(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(j, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsTrue(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsTrue(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsTrue(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsTrue(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(k, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsTrue(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(l, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsTrue(Monitor.IsEntered(k));
                Assert.IsTrue(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(m, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsTrue(Monitor.IsEntered(k));
                Assert.IsTrue(Monitor.IsEntered(l));
                Assert.IsTrue(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));








            testLockManager.AcquireLocksAndInvokeAction(a, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsTrue(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsTrue(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsTrue(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsTrue(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(b, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsTrue(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsTrue(Monitor.IsEntered(e));
                Assert.IsTrue(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsTrue(Monitor.IsEntered(h));
                Assert.IsTrue(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(d, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsTrue(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsTrue(Monitor.IsEntered(i));
                Assert.IsTrue(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsTrue(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(e, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsTrue(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(f, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsTrue(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(g, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsTrue(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(h, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsTrue(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(i, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsTrue(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(j, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsTrue(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsFalse(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(k, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsTrue(Monitor.IsEntered(k));
                Assert.IsTrue(Monitor.IsEntered(l));
                Assert.IsTrue(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(l, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsTrue(Monitor.IsEntered(l));
                Assert.IsTrue(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));


            testLockManager.AcquireLocksAndInvokeAction(m, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                Assert.IsFalse(Monitor.IsEntered(a));
                Assert.IsFalse(Monitor.IsEntered(b));
                Assert.IsFalse(Monitor.IsEntered(d));
                Assert.IsFalse(Monitor.IsEntered(e));
                Assert.IsFalse(Monitor.IsEntered(f));
                Assert.IsFalse(Monitor.IsEntered(g));
                Assert.IsFalse(Monitor.IsEntered(h));
                Assert.IsFalse(Monitor.IsEntered(i));
                Assert.IsFalse(Monitor.IsEntered(j));
                Assert.IsFalse(Monitor.IsEntered(k));
                Assert.IsFalse(Monitor.IsEntered(l));
                Assert.IsTrue(Monitor.IsEntered(m));
                Assert.IsFalse(Monitor.IsEntered(n));
            }));
        }
    }
}
