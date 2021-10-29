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
        /// Test that an exception is thrown if a method is called server side before calling Initialize.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestServerNotInitializedExceptionAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = new ();

            await Assert.ThrowsExceptionAsync<LocalIpcNotInitializedException>(async () =>
            {
                await localIpcServer.SendAsync(sendText).ConfigureAwait(false);

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Test that an exception is thrown if a method is called client side before calling Initialize.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public void TestClientNotInitializedException()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = new ();

            Task.Run(async () =>
            {
                using LocalIpcClient localIpcClient = new (localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);

                await Assert.ThrowsExceptionAsync<LocalIpcNotInitializedException>(async () =>
                {
                    await localIpcClient.SendAsync(sendText).ConfigureAwait(false);

                }).ConfigureAwait(false);

            }).ConfigureAwait(false);
        }


        /// <summary>
        /// Test sending and receiving a string
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveStringAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = new ();
            LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

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

            using LocalIpcServer localIpcServer = new ();
            LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

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
            TaskCompletionSource tcs = new ();

            using LocalIpcServer localIpcServer = new ();
            LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

            localIpcServer.IsReceiveEventsEnabled = true;

            localIpcServer.Received += (s, e) =>
            {
                string receiveText = e.GetValue<string>();
                Assert.AreEqual(sendText, receiveText, "Unexpected text received");
                tcs.SetResult();
            };

            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)).ConfigureAwait(false) != tcs.Task)
            {
                Assert.Fail("Timeout waiting for received event");
            }
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
            TaskCompletionSource tcs = new ();

            using LocalIpcServer localIpcServer = new ();
            LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

            localIpcServer.IsReceiveEventsEnabled = true;

            localIpcServer.Received += (s, e) =>
            {
                TestObject receiveTestClass = e.GetValue<TestObject>();
                Assert.AreEqual(sendTestClass, receiveTestClass, "Unexpected object received");
                tcs.SetResult();
            };

            await localIpcServer.SendAsync(sendTestClass).ConfigureAwait(false);

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)).ConfigureAwait(false) != tcs.Task)
            {
                Assert.Fail("Timeout waiting for received event");
            }
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

            using (LocalIpcServer localIpcServer = new ())
            {
                LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
                await localIpcServer.InitializeAsync().ConfigureAwait(false);

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
        public async Task TestCancelServerInitializeAsync()
        {
            CancellationTokenSource cancellationTokenSource = new ();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                using LocalIpcServer localIpcServer = new ();
                await localIpcServer.InitializeAsync(cancellationTokenSource.Token).ConfigureAwait(false);

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Test cancelling the creation of an Ipc client.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public void TestCancelClientInitialize()
        {
            CancellationTokenSource cancellationTokenSource = new ();
            cancellationTokenSource.Cancel();

            using LocalIpcServer localIpcServer = new ();

            Task.Run(async () =>
            {
                using LocalIpcClient localIpcClient = new (localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);

                await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
                {
                    await localIpcClient.InitializeAsync(cancellationTokenSource.Token).ConfigureAwait(false);

                }).ConfigureAwait(false);

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

            using LocalIpcServer localIpcServer = new ();
            LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

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

            using LocalIpcServer localIpcServer = new ();
            LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

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

            using LocalIpcServer localIpcServer = new ();
            using Process process = LaunchExternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
            string receiveText = await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

            Assert.AreEqual(sendText, receiveText, "Unexpected string received");
        }

        /// <summary>
        /// Test checking that sending to a non existant external client raises an exception.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestBrokenPipeExternalClientAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = new ();
            using Process process = LaunchExternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

            // send an receive to external client - the external client will exit after this
            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
            string receiveText = await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

            // send again, the pipe should be broken
            await Assert.ThrowsExceptionAsync<PipeBrokenException>(async () =>
            {
                await localIpcServer.SendAsync(sendText).ConfigureAwait(false);

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Launch an internal IPC client. 
        /// This is very simple and just echoes back the object sent to it.
        /// </summary>
        /// <param name="sendPipeHandle">Send handle from server.</param>
        /// <param name="receivePipeHandle">Receive handle from server.</param>
        private static void LaunchInternalClient(string sendPipeHandle, string receivePipeHandle)
        {
            Task.Run(async () =>
            {
                using LocalIpcClient localIpcClient = new (sendPipeHandle, receivePipeHandle);
                await localIpcClient.InitializeAsync().ConfigureAwait(false);

                object value = await localIpcClient.ReceiveAsync().ConfigureAwait(false);
                await localIpcClient.SendAsync(value).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Launch an EXE containing an external IPC client. 
        /// This is very simple and just echoes back the object sent to it.
        /// </summary>
        /// <param name="sendPipeHandle">Send handle from server.</param>
        /// <param name="receivePipeHandle">Receive handle from server.</param>
        /// <returns>A <see cref="Process"/> object for the client.</returns>
        private static Process LaunchExternalClient(string sendPipeHandle, string receivePipeHandle)
        {
            return Process.Start(@"..\..\..\..\dotDoc.LocalIpc.TestClient\bin\Debug\net5.0\dotDoc.LocalIpc.TestClient.exe", $"{sendPipeHandle} {receivePipeHandle}");
        }
    }
}
