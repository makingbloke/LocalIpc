// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc;
using DotDoc.LocalIpc.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace dotDoc.LocalIpc.Tests;

/// <summary>
/// LocalIpc tests.
/// </summary>
[TestClass]
public class LocalIpcTests
{
    /// <summary>
    /// Test object used to send and receive.
    /// </summary>
    private record TestObject(string textValue, int intValue);

    /// <summary>
    /// Test that an exception is thrown if an attempt is made to initialize Local Ipc twice.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestAlreadyInitializedException()
    {
        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        await Assert.ThrowsExceptionAsync<LocalIpcAlreadyInitializedException>(async () =>
        {
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Test that an exception is thrown if a send is attempted before calling Initialize.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestSendNotInitializedExceptionAsync()
    {
        const string sendText = "Hello";

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();

        await Assert.ThrowsExceptionAsync<LocalIpcNotInitializedException>(async () =>
        {
            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);

        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Test that an exception is thrown if a receive is attempted before calling Initialize.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestReceiveNotInitializedExceptionAsync()
    {
        using LocalIpcServer localIpcServer = LocalIpcServer.Create();

        await Assert.ThrowsExceptionAsync<LocalIpcNotInitializedException>(async () =>
        {
            await localIpcServer.ReceiveAsync().ConfigureAwait(false);

        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Test that an exception is thrown if a method if the receive event property is changed before calling Initialize.
    /// </summary>
    [TestMethod]
    public void TestReceiveEventNotInitializedException()
    {
        using LocalIpcServer localIpcServer = LocalIpcServer.Create();

        Assert.ThrowsException<LocalIpcNotInitializedException>(() =>
        {
            localIpcServer.IsReceiveEventsEnabled = true;

        });
    }

    /// <summary>
    /// Test an exception is thrown if a manual receive is attempted while receive events are enabled.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestReceiveWhenReceiveEventsEnabledExceptionAsync()
    {
        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        localIpcServer.IsReceiveEventsEnabled = true;

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
        {
            await localIpcServer.ReceiveAsync().ConfigureAwait(false);

        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Test sending and receiving a string.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestSendReceiveStringAsync()
    {
        const string sendText = "Hello";

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
        string receiveText = await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

        Assert.AreEqual(sendText, receiveText, "Unexpected object received");
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
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        await localIpcServer.SendAsync(sendTestObject).ConfigureAwait(false);
        TestObject receiveTestObject = await localIpcServer.ReceiveAsync<TestObject>().ConfigureAwait(false);

        Assert.AreEqual(sendTestObject, receiveTestObject, "Unexpected object received");
    }

    /// <summary>
    /// Test sending and receiving a null.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestSendReceiveNullAsync()
    {
        const object sendNull = null;

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        await localIpcServer.SendAsync(sendNull).ConfigureAwait(false);
        object receiveNull = await localIpcServer.ReceiveAsync<object>().ConfigureAwait(false);

        Assert.AreEqual(sendNull, receiveNull, "Unexpected object received");
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
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        localIpcServer.IsReceiveEventsEnabled = true;

        localIpcServer.Received += (s, e) =>
        {
            receiveText = e.Value as string;
            tcs.SetResult();
        };

        await localIpcServer.SendAsync(sendText).ConfigureAwait(false);

        if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)).ConfigureAwait(false) != tcs.Task)
        {
            Assert.Fail("Timeout waiting for received event");
        }

        Assert.AreEqual(sendText, receiveText, "Unexpected object received");
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
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        localIpcServer.IsReceiveEventsEnabled = true;

        localIpcServer.Received += (s, e) =>
        {
            receiveTestClass = e.Value as TestObject;
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
    /// Test sending and receiving a string using a receive event.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestSendReceiveEventNullAsync()
    {
        const int receivedEventTimeout = 2000;
        const object sendNull = null;
        object receiveNull = null;
        TaskCompletionSource tcs = new ();

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
        LaunchInternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        localIpcServer.IsReceiveEventsEnabled = true;

        localIpcServer.Received += (s, e) =>
        {
            receiveNull = e.Value;
            tcs.SetResult();
        };

        await localIpcServer.SendAsync(sendNull).ConfigureAwait(false);

        if (await Task.WhenAny(tcs.Task, Task.Delay(receivedEventTimeout)).ConfigureAwait(false) != tcs.Task)
        {
            Assert.Fail("Timeout waiting for received event");
        }

        Assert.AreEqual(sendNull, receiveNull, "Unexpected object received");
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
            using LocalIpcServer localIpcServer = LocalIpcServer.Create();
            await localIpcServer.InitializeAsync(cancellationTokenSource.Token).ConfigureAwait(false);

        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Test cancelling the creation of an Ipc client.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestCancelClientInitializeAsync()
    {
        CancellationTokenSource cancellationTokenSource = new ();
        cancellationTokenSource.Cancel();

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
        using LocalIpcClient localIpcClient = LocalIpcClient.Create(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(async () =>
        {
            await localIpcClient.InitializeAsync(cancellationTokenSource.Token).ConfigureAwait(false);

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

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
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

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();
        using Process process = LaunchExternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle);
        await localIpcServer.InitializeAsync().ConfigureAwait(false);

        await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
        string receiveText = await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

        Assert.AreEqual(sendText, receiveText, "Unexpected string received");
    }

    /// <summary>
    /// Test checking that sending to a non existant external client raises a pipe broken exception.
    /// </summary>
    /// <returns><see cref="Task"/>.</returns>
    [TestMethod]
    public async Task TestSendExternalClientBrokenPipeAsync()
    {
        const string sendText = "Hello";

        using LocalIpcServer localIpcServer = LocalIpcServer.Create();

        using (Process process = LaunchExternalClient(localIpcServer.SendPipeHandle, localIpcServer.ReceivePipeHandle))
        {
            await localIpcServer.InitializeAsync().ConfigureAwait(false);

            // send an receive to external client - the external client will exit after this
            await localIpcServer.SendAsync(sendText).ConfigureAwait(false);
            await localIpcServer.ReceiveAsync<string>().ConfigureAwait(false);

            await process.WaitForExitAsync().ConfigureAwait(false);
        }

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
    /// <param name="sendPipeHandle">Send handle from server.</param>
    /// <param name="receivePipeHandle">Receive handle from server.</param>
    /// <returns>A <see cref="Process"/> object for the client.</returns>
    private static Process LaunchExternalClient(string sendPipeHandle, string receivePipeHandle)
    {
        return Process.Start(@"..\..\..\..\dotDoc.LocalIpc.TestClient\bin\Debug\net6.0\dotDoc.LocalIpc.TestClient.exe", $"{sendPipeHandle} {receivePipeHandle}");
    }
}
