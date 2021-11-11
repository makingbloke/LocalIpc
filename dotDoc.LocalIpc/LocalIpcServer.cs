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

        /// <summary>
        /// Create an instance of the local Ipc Server.
        /// </summary>
        /// <param name="serializer">An optional serializer, if none is specified then <see cref="DefaultSerializer"/> is used.</param>
        /// <returns>An instance of <see cref="LocalIpcServer"/>.</returns>
        public static LocalIpcServer Create(ISerializer serializer = null)
        {
            AnonymousPipeServerStream sendPipe = new (PipeDirection.Out, HandleInheritability.Inheritable);
            AnonymousPipeServerStream receivePipe = new (PipeDirection.In, HandleInheritability.Inheritable);
            return new LocalIpcServer(sendPipe, receivePipe, serializer);
        }

        protected LocalIpcServer(AnonymousPipeServerStream sendPipe, AnonymousPipeServerStream receivePipe, ISerializer serializer)
            : base(sendPipe, receivePipe, serializer)
        {
            _sendPipe = sendPipe;
            SendPipeHandle = sendPipe.GetClientHandleAsString();

            _receivePipe = receivePipe;
            ReceivePipeHandle = receivePipe.GetClientHandleAsString();
        }

        /// <summary>
        /// The send pipe handle that must be passed to a client.
        /// </summary>
        public string SendPipeHandle { get; }

        /// <summary>
        /// The receive pipe handle that must be passed to a client.
        /// </summary>
        public string ReceivePipeHandle { get; }

        /// <summary>
        /// Initialize method. 
        /// This must be called after the class has been created and the client has been launched.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns><see cref="Task"/>.</returns>
        public override async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            await base.InitializeAsync(cancellationToken).ConfigureAwait(false);

            // Wait for the client to send back its process id. 
            // If it is running on a different process to the server then dispose of the local copy of the client handles
            // (If the handles are not disposed then the server cannot track if the client has broken the pipe unexpectedly).
            int clientProcessId = await ReceiveAsync<int>(cancellationToken).ConfigureAwait(false);

            if (clientProcessId != Environment.ProcessId)
            {
                _sendPipe.DisposeLocalCopyOfClientHandle();
                _receivePipe.DisposeLocalCopyOfClientHandle();
            }
        }
    }
}
