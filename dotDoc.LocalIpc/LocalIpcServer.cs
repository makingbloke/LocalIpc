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

        public delegate object LaunchClientFuncDelegate(string sendPipeHandle, string receivePipeHandle, params object[] launchArgs);
        public delegate void LaunchClientActionDelegate(string sendPipeHandle, string receivePipeHandle, params object[] launchArgs);

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
            _receivePipe = receivePipe;
        }

        public object ClientHandle { get; private set; }

        // TODO: check initialize is completed - make sure EnableReceiveEvents is not set until it is?.
        public async Task InitialiseAsync(LaunchClientFuncDelegate launchClient, CancellationToken cancellationToken = default, params object[] launchArgs)
        {
            ClientHandle = launchClient(_sendPipe.GetClientHandleAsString(), _receivePipe.GetClientHandleAsString(), launchArgs);

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

        public async Task InitialiseAsync(LaunchClientActionDelegate launchClient, CancellationToken cancellationToken = default, params object[] launchArgs)
        {
            await InitialiseAsync(LaunchClientInternal, cancellationToken, launchArgs).ConfigureAwait(false);

            object LaunchClientInternal(string sendPipeHandle, string receivePipeHandle, object[] launchArgs)
            {
                launchClient(sendPipeHandle, receivePipeHandle, launchArgs);
                return null;
            }
        }
    }
}
