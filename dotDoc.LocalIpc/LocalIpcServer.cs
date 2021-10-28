// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc.Serializers;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace DotDoc.LocalIpc
{
    public class LocalIpcServer : LocalIpcBase
    {
        public delegate object LaunchClientFuncDelegate(string sendPipeHandle, string receivePipeHandle, params object[] launchArgs);
        public delegate void LaunchClientActionDelegate(string sendPipeHandle, string receivePipeHandle, params object[] launchArgs);

        public static async Task<LocalIpcServer> CreateAsync(LaunchClientFuncDelegate launchClient, ISerializer serializer = null, CancellationToken cancellationToken = default, params object[] launchArgs)
        {
            AnonymousPipeServerStream sendPipe = new (PipeDirection.Out, HandleInheritability.Inheritable);
            AnonymousPipeServerStream receivePipe = new (PipeDirection.In, HandleInheritability.Inheritable);

            // Create an instance of LocalIpcServer and then call the method to launch the client. 
            // This returns a value which can be used as a handle to the client.
            LocalIpcServer localIpcServer = new (sendPipe, receivePipe, serializer)
            {
                ClientHandle = launchClient(sendPipe.GetClientHandleAsString(), receivePipe.GetClientHandleAsString(), launchArgs)
            };

            // Wait for the client to send back its process id. 
            // If it is running on a different process to the server then dispose of the local copy of the client handles.
            // If the handles are not disposed then the server cannot track if the client has closed the pipe unexpectedly.
            int clientProcessId = await localIpcServer.ReceiveAsync<int>(cancellationToken).ConfigureAwait(false);

            if (clientProcessId != Environment.ProcessId)
            {
                sendPipe.DisposeLocalCopyOfClientHandle();
                receivePipe.DisposeLocalCopyOfClientHandle();
            }

            return localIpcServer;
        }

        public static Task<LocalIpcServer> CreateAsync(LaunchClientActionDelegate launchClient, ISerializer serializer = null, CancellationToken cancellationToken = default, params object[] launchArgs) =>
            CreateAsync((sendPipeHandle, receivePipeHandle, launchArgs) =>
            {
                launchClient(sendPipeHandle, receivePipeHandle, launchArgs);
                return null;
            }, serializer, cancellationToken, launchArgs);

        private LocalIpcServer(AnonymousPipeServerStream sendPipe, AnonymousPipeServerStream receivePipe, ISerializer serializer)
            : base(sendPipe, receivePipe, serializer)
        {
        }

        public object ClientHandle { get; private set; }
    }
}
