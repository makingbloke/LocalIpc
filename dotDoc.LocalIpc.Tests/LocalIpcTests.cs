// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc;
using DotDoc.LocalIpc.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace dotDoc.LocalIpc.Tests
{
    /// <summary>
    /// LocalIpcClient/Server tests.
    /// </summary>
    [TestClass]
    public class LocalIpcTests
    {
        /// <summary>
        /// Test object used to send & receive.
        /// </summary>
        private record TestObject(string TextValue, int IntValue);

        /// <summary>
        /// Test sending and receiving a string
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveStringAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchInternalClient).ConfigureAwait(false);

            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
            string receiveText = await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

            Assert.AreEqual(sendText, receiveText, "Unexpected string received");
        }

        /// <summary>
        /// Test sending and receiving an object.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveObjectAsync()
        {
            TestObject sendTestObject = new ("Hello", 1);

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchInternalClient).ConfigureAwait(false);

            await localIpcServer.SendAsync(sendTestObject).ConfigureAwait(false);
            TestObject receiveTestObject = await localIpcServer.ReceiveAsync<TestObject>().ConfigureAwait(false);

            Assert.AreEqual(sendTestObject, receiveTestObject, "Unexpected object received");
        }

        /// <summary>
        /// Test sending and receiving a string using a receive event.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveEventStringAsync()
        {
            const int receivedEventTimeout = 2000;
            const string sendText = "Hello";
            string receiveText = null;
            TaskCompletionSource tcs = new ();

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchInternalClient).ConfigureAwait(false);
            localIpcServer.EnableReceiveEvents = true;

            localIpcServer.Received += (s, e) =>
            {
                receiveText = e.GetValue<string>();
                tcs.SetResult();
            };

            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)).ConfigureAwait(false) != tcs.Task)
            {
                Assert.Fail("Timeout waiting for received event");
            }

            Assert.AreEqual(sendText, receiveText, "Unexpected text received");
        }

        /// <summary>
        /// Test sending and receiving an object using a receive event.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveEventObjectAsync()
        {
            const int receivedEventTimeout = 2000;
            TestObject sendTestClass = new ("Hello", 1);
            TestObject receiveTestClass = null;
            TaskCompletionSource tcs = new ();

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchInternalClient).ConfigureAwait(false);
            localIpcServer.EnableReceiveEvents = true;

            localIpcServer.Received += (s, e) =>
            {
                receiveTestClass = e.GetValue<TestObject>();
                tcs.SetResult();
            };

            await localIpcServer.SendAsync(sendTestClass).ConfigureAwait(false);

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)).ConfigureAwait(false) != tcs.Task)
            {
                Assert.Fail("Timeout waiting for received event");
            }

            Assert.AreEqual(sendTestClass, receiveTestClass, "Unexpected object received");
        }

        /// <summary>
        /// Test that a disposed event is raised when an Ipc object is disposed.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestDisposedEventAsync()
        {
            const int receivedEventTimeout = 2000;
            TaskCompletionSource tcs = new ();

            using (LocalIpcServer localIpcServer = LocalIpcServer.Create())
            {
                await localIpcServer.InitialiseAsync(LaunchInternalClient).ConfigureAwait(false);

                localIpcServer.Disposed += (s, e) =>
                {
                    tcs.SetResult();
                };
            }

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)).ConfigureAwait(false) != tcs.Task)
            {
                Assert.Fail("Timeout waiting for disposed event");
            }
        }

        /// <summary>
        /// Test cancelling the creation of an Ipc server.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelServerInitialiseAsync()
        {
            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                using LocalIpcServer localIpcServer = LocalIpcServer.Create();
                await localIpcServer.InitialiseAsync(LaunchInternalClient, cancellationTokenSource.Token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Test cancelling the creation of an Ipc client.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelClientInitialiseAsync()
        {
            CancellationTokenSource cancellationTokenSource = new ();
            cancellationTokenSource.Cancel();

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await localIpcServer.InitialiseAsync(LaunchInternalClient, cancellationTokenSource.Token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Test cancelling a send.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelSendAsync()
        {
            const string sendText = "Hello";
            CancellationTokenSource cancellationTokenSource = new ();
            cancellationTokenSource.Cancel();

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchInternalClient).ConfigureAwait(false);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await localIpcServer.SendAsync(sendText, cancellationTokenSource.Token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Test cancelling a receive.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelReceiveAsync()
        {
            const string sendText = "Hello";
            CancellationTokenSource cancellationTokenSource = new ();
            cancellationTokenSource.Cancel();

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchInternalClient).ConfigureAwait(false);

            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await localIpcServer.ReceiveAsync<string>(cancellationTokenSource.Token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Test sending and receiving a string to an external client.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveStringExternalClientAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchExternalClient).ConfigureAwait(false);

            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
            string receiveText = await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

            Assert.AreEqual(sendText, receiveText, "Unexpected string received");

            DisposeExternalClient(localIpcServer);
        }

        /// <summary>
        /// Test checking that sending to a non existant external client raises an exception.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestBrokenPipeExternalClientAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchExternalClient).ConfigureAwait(false);

            // send an receive to external client - the external client will exit after this
            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
            string receiveText = await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

            // send again, the pipe should be broken
            await Assert.ThrowsExceptionAsync<PipeBrokenException>(async () =>
            {
                await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
            }).ConfigureAwait(false);

            DisposeExternalClient(localIpcServer);
        }

        /// <summary>
        /// Test passing extra arguments to the LaunchClient method when creating a server object.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestLaunchArgsAsync()
        {
            object[] testLaunchArgs = { "hello", 1 };

            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitialiseAsync(LaunchClientInternal, launchArgs: testLaunchArgs).ConfigureAwait(false);

            void LaunchClientInternal(string sendPipeHandle, string receivePipeHandle, params object[] launchArgs)
            {
                Assert.AreEqual(testLaunchArgs.Length, launchArgs?.Length, "Invalid launch argument count");

                for (int i = 0; i < testLaunchArgs.Length; i++)
                {
                    Assert.AreEqual(testLaunchArgs[i], launchArgs[i], $"Invalid launchArg[{i}] value");
                }

                LaunchInternalClient(sendPipeHandle, receivePipeHandle);
            }
        }

        /// <summary>
        /// Launch an internal IPC client. 
        /// This is very simple and just echoes back the object sent to it.
        /// </summary>
        /// <param name="sendPipeHandle"></param>
        /// <param name="receivePipeHandle"></param>
        private static void LaunchInternalClient(string sendPipeHandle, string receivePipeHandle, params object[] launchArgs)
        {
            Task.Run(async () =>
            {
                using LocalIpcClient localIpcClient = LocalIpcClient.Create(sendPipeHandle, receivePipeHandle);
                await localIpcClient.InitializeAsync().ConfigureAwait(false);

                object value = await localIpcClient.ReceiveAsync().ConfigureAwait(false);
                await localIpcClient.SendAsync(value).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Launch an EXE containing an external IPC client. 
        /// This is very simple and just echoes back the object sent to it.
        /// </summary>
        /// <param name="sendPipeHandle"></param>
        /// <param name="receivePipeHandle"></param>
        /// <returns></returns>
        private static object LaunchExternalClient(string sendPipeHandle, string receivePipeHandle, params object[] launchArgs)
        {
            return Process.Start(@"..\..\..\..\dotDoc.LocalIpc.TestClient\bin\Debug\net5.0\dotDoc.LocalIpc.TestClient.exe", $"{sendPipeHandle} {receivePipeHandle}");
        }

        /// <summary>
        /// Dispose of the process handle used by the local IPC server.
        /// </summary>
        /// <param name="localIpcServer">Instance of <see cref="LocalIpcServer"/>.</param>
        private static void DisposeExternalClient(LocalIpcServer localIpcServer)
        {
            if (localIpcServer.ClientHandle is Process process)
            {
                process.Dispose();
            }
        }
    }
}
