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
        public LocalIpcClient(string sendPipeHandle, string receivePipeHandle, ISerializer serializer = null)
            : base(serializer)
        {
            SendPipeStream = new AnonymousPipeClientStream(PipeDirection.Out, receivePipeHandle);
            ReceivePipeStream = new AnonymousPipeClientStream(PipeDirection.In, sendPipeHandle);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            // Send back the process of the client so the server knows if it is running in a different process.
            await SendAsync(Environment.ProcessId, cancellationToken).ConfigureAwait(false);
        }
    }
}
