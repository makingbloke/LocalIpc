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
        private readonly AnonymousPipeServerStream _sendPipe;
        private readonly AnonymousPipeServerStream _receivePipe;

        public static LocalIpcServer Create(ISerializer serializer = null)
        {
            AnonymousPipeServerStream sendPipe = new (PipeDirection.Out, HandleInheritability.Inheritable);
            AnonymousPipeServerStream receivePipe = new (PipeDirection.In, HandleInheritability.Inheritable);
            return new (sendPipe, receivePipe, serializer);
        }

        private LocalIpcServer(AnonymousPipeServerStream sendPipe, AnonymousPipeServerStream receivePipe, ISerializer serializer)
            : base(sendPipe, receivePipe, serializer)
        {
            _sendPipe = sendPipe;
            SendPipeHandle = sendPipe.GetClientHandleAsString();

            _receivePipe = receivePipe;
            ReceivePipeHandle = receivePipe.GetClientHandleAsString();
        }

        public string SendPipeHandle { get; }
        public string ReceivePipeHandle { get; }


        // TODO: check initialize is completed - make sure EnableReceiveEvents is not set until it is?.
        public async Task InitialiseAsync(CancellationToken cancellationToken = default)
        {
            // Wait for the client to send back its process id. 
            // If it is running on a different process to the server then dispose of the local copy of the client handles.
            // If the handles are not disposed then the server cannot track if the client has closed the pipe unexpectedly.
            int clientProcessId = await ReceiveAsync<int>(cancellationToken).ConfigureAwait(false);

            if (clientProcessId != Environment.ProcessId)
            {
                _sendPipe.DisposeLocalCopyOfClientHandle();
                _receivePipe.DisposeLocalCopyOfClientHandle();
            }
        }
    }
}
