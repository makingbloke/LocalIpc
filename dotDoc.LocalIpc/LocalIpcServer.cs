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
        public static async Task<LocalIpcServer> CreateAsync(Func<string, string, object> launchClient, ISerializer serializer = null, CancellationToken cancellationToken = default)
        {
            AnonymousPipeServerStream sendPipe = new (PipeDirection.Out, HandleInheritability.Inheritable);
            AnonymousPipeServerStream receivePipe = new (PipeDirection.In, HandleInheritability.Inheritable);

            // Create an instance of LocalIpcServer and call the method to launch the client. 
            // This returns a piece of data which can be used as a handle to the client.
            LocalIpcServer localIpcServer = new(sendPipe, receivePipe, serializer)
            {
                ClientHandle = launchClient(sendPipe.GetClientHandleAsString(), receivePipe.GetClientHandleAsString())
            };

            // Wait for the client to send back its process id. 
            // If it is running on a different process to the server then dispose of the local copy of the client handles.
            // We need to do this, as if the handles are left open then the server cannot tell if the client has closed the pipe unexpectedly.
            int clientProcessId = await localIpcServer.ReceiveAsync<int>(cancellationToken);

            if (clientProcessId != Environment.ProcessId)
            {
                sendPipe.DisposeLocalCopyOfClientHandle();
                receivePipe.DisposeLocalCopyOfClientHandle();
            }

            return localIpcServer;
        }

        public static Task<LocalIpcServer> CreateAsync(Action<string, string> launchClient, ISerializer serializer = null, CancellationToken cancellationToken = default)
        {
            return CreateAsync((c, s) =>
            {
                launchClient(c, s);
                return null;
            }, serializer, cancellationToken);
        }

        private LocalIpcServer(AnonymousPipeServerStream sendPipe, AnonymousPipeServerStream receivePipe, ISerializer serializer)
            : base(sendPipe, receivePipe, serializer)
        {
        }

        public object ClientHandle { get; private set; }
    }
}
