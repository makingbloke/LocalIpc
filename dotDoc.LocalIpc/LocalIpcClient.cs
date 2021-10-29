// Copyright ©2021 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc.Serializers;
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace DotDoc.LocalIpc
{
    public class LocalIpcClient: LocalIpcBase
    {
        public static LocalIpcClient Create(string sendPipeHandle, string receivePipeHandle, ISerializer serializer = null)
        {
            AnonymousPipeClientStream sendPipe = new (PipeDirection.Out, receivePipeHandle);
            AnonymousPipeClientStream receivePipe = new (PipeDirection.In, sendPipeHandle);
            return new (sendPipe, receivePipe, serializer);
        }

        private LocalIpcClient(AnonymousPipeClientStream sendPipe, AnonymousPipeClientStream receivePipe, ISerializer serializer)
            : base(sendPipe, receivePipe, serializer)
        {
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // Send back the process of the client so the server knows if it is running in a different process.
            await SendAsync(Environment.ProcessId, cancellationToken).ConfigureAwait(false);
        }
    }
}
