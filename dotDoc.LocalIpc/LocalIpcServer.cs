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
        public LocalIpcServer(ISerializer serializer = null)
            : base(serializer)
        {
            AnonymousPipeServerStream sendPipe = new (PipeDirection.Out, HandleInheritability.Inheritable);
            SendPipeStream = sendPipe;
            SendPipeHandle = sendPipe.GetClientHandleAsString();

            AnonymousPipeServerStream receivePipe = new (PipeDirection.In, HandleInheritability.Inheritable);
            ReceivePipeStream = receivePipe;
            ReceivePipeHandle = receivePipe.GetClientHandleAsString();
        }

        public string SendPipeHandle { get; }
        public string ReceivePipeHandle { get; }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            IsInitialized = true;

            // Wait for the client to send back its process id. 
            // If it is running on a different process to the server then dispose of the local copy of the client handles.
            // If the handles are not disposed then the server cannot track if the client has closed the pipe unexpectedly.
            int clientProcessId = await ReceiveAsync<int>(cancellationToken).ConfigureAwait(false);

            if (clientProcessId != Environment.ProcessId)
            {
                ((AnonymousPipeServerStream)SendPipeStream).DisposeLocalCopyOfClientHandle();
                ((AnonymousPipeServerStream)ReceivePipeStream).DisposeLocalCopyOfClientHandle();
            }
        }
    }
}
