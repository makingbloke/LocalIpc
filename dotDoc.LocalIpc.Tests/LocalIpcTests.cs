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
        /// Test sending and receiving a string
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveStringAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient);

            await localIpcServer.SendAsync(sendText);
            string receiveText = await localIpcServer.ReceiveAsync<string>();

            Assert.AreEqual(sendText, receiveText, "Unexpected string received");
        }

        /// <summary>
        /// Test sending and receiving an object.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveObjectAsync()
        {
            TestClass sendTestClass = new() { TextValue = "Hello", IntValue = 1 };

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient);

            await localIpcServer.SendAsync(sendTestClass);
            TestClass receiveTestClass = await localIpcServer.ReceiveAsync<TestClass>();

            Assert.AreEqual(sendTestClass, receiveTestClass, "Unexpected object received");
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
            TaskCompletionSource tcs = new();

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient);
            localIpcServer.EnableReceiveEvents = true;

            localIpcServer.Received += (s, e) =>
            {
                receiveText = e.GetValue<string>();
                tcs.SetResult();
            };

            await localIpcServer.SendAsync(sendText);

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)) != tcs.Task)
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
            TestClass sendTestClass = new() { TextValue = "Hello", IntValue = 1 };
            TestClass receiveTestClass = null;
            TaskCompletionSource tcs = new();

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient);
            localIpcServer.EnableReceiveEvents = true;

            localIpcServer.Received += (s, e) =>
            {
                receiveTestClass = e.GetValue<TestClass>();
                tcs.SetResult();
            };

            await localIpcServer.SendAsync(sendTestClass);

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)) != tcs.Task)
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
            TaskCompletionSource tcs = new();

            using (LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient))
            {
                localIpcServer.Disposed += (s, e) =>
                {
                    tcs.SetResult();
                };
            }

            if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)) != tcs.Task)
            {
                Assert.Fail("Timeout waiting for disposed event");
            }
        }

        /// <summary>
        /// Test cancelling the creation of an Ipc client.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelCreateClientAsync()
        {
            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            // first create a server so we can pass valid pipe handles to the fake client.
            // when the client is cancelled the server throws a pipe broken exception because it is waiting for the client to send a response.
            await Assert.ThrowsExceptionAsync<PipeBrokenException>(async () =>
            {
                using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync((sendPipeHandle, receivePipeHandle) =>
                {
                    return Task.Run(async () =>
                    {
                        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
                        {
                            using LocalIpcClient ipcClient = await LocalIpcClient.CreateAsync(sendPipeHandle, receivePipeHandle, cancellationToken: cancellationTokenSource.Token);
                        });
                    });
                });
            });
        }

        /// <summary>
        /// Test cancelling the creation of an Ipc server.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelCreateServerAsync()
        {
            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient, cancellationToken: cancellationTokenSource.Token);
            });
        }

        /// <summary>
        /// Test cancelling a send.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelSendAsync()
        {
            const string sendText = "Hello";
            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await localIpcServer.SendAsync(sendText, cancellationTokenSource.Token);
            });
        }

        /// <summary>
        /// Test cancelling a receive.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestCancelReceiveAsync()
        {
            const string sendText = "Hello";
            CancellationTokenSource cancellationTokenSource = new();
            cancellationTokenSource.Cancel();

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchInternalClient);

            await localIpcServer.SendAsync(sendText);

            await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
            {
                await localIpcServer.ReceiveAsync<string>(cancellationTokenSource.Token);
            });
        }

        /// <summary>
        /// Test sending and receiving a string to an external client.
        /// </summary>
        /// <returns><see cref="Task"/>.</returns>
        [TestMethod]
        public async Task TestSendReceiveStringExternalClientAsync()
        {
            const string sendText = "Hello";

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchExternalClient);

            await localIpcServer.SendAsync(sendText);
            string receiveText = await localIpcServer.ReceiveAsync<string>();

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

            using LocalIpcServer localIpcServer = await LocalIpcServer.CreateAsync(LaunchExternalClient);

            // send an receive to external client - the external client will exit after this
            await localIpcServer.SendAsync(sendText);
            string receiveText = await localIpcServer.ReceiveAsync<string>();

            // send again, the pipe should be broken
            await Assert.ThrowsExceptionAsync<PipeBrokenException>(async () =>
            {
                await localIpcServer.SendAsync(sendText);
            });

            DisposeExternalClient(localIpcServer);
        }

        /// <summary>
        /// Launch an internal IPC client. 
        /// This is very simple and just echoes back the object sent to it.
        /// </summary>
        /// <param name="sendPipeHandle"></param>
        /// <param name="receivePipeHandle"></param>
        /// <returns></returns>
        private static object LaunchInternalClient(string sendPipeHandle, string receivePipeHandle)
        {
            return Task.Run(async () =>
            {
                using LocalIpcClient ipcClient = await LocalIpcClient.CreateAsync(sendPipeHandle, receivePipeHandle);

                object value = await ipcClient.ReceiveAsync();
                await ipcClient.SendAsync(value);
            });
        }

        /// <summary>
        /// Launch an EXE containing an external IPC client. 
        /// This is very simple and just echoes back the object sent to it.
        /// </summary>
        /// <param name="sendPipeHandle"></param>
        /// <param name="receivePipeHandle"></param>
        /// <returns></returns>
        private static object LaunchExternalClient(string sendPipeHandle, string receivePipeHandle) =>
            Process.Start(@"..\..\..\..\dotDoc.LocalIpc.TestClient\bin\Debug\net5.0\dotDoc.LocalIpc.TestClient.exe", $"{sendPipeHandle} {receivePipeHandle}");

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
