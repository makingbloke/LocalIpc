// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc.Serializers;
using System.IO.Pipes;

namespace DotDoc.LocalIpc;

/// <summary>
/// Local Ipc Client.
/// </summary>
public class LocalIpcClient : LocalIpcBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalIpcClient"/> class.
    /// </summary>
    /// <param name="sendPipeStream">The send <see cref="AnonymousPipeClientStream"/>.</param>
    /// <param name="receivePipeStream">The receive <see cref="AnonymousPipeClientStream"/>.</param>
    /// <param name="serializer">An optional serializer, if none is specified then <see cref="DefaultSerializer"/> is used.</param>
    protected LocalIpcClient(AnonymousPipeClientStream sendPipeStream, AnonymousPipeClientStream receivePipeStream, ISerializer serializer)
        : base(sendPipeStream, receivePipeStream, serializer)
    {
    }

    /// <summary>
    /// Create an instance of the Local Ipc Client.
    /// </summary>
    /// <param name="sendPipeHandle">The send pipe handle from the server.</param>
    /// <param name="receivePipeHandle">The receive pipe handle from the server.</param>
    /// <param name="serializer">An optional serializer, if none is specified then <see cref="DefaultSerializer"/> is used.</param>
    /// <returns>An instance of <see cref="LocalIpcClient"/>.</returns>
    public static LocalIpcClient Create(string sendPipeHandle, string receivePipeHandle, ISerializer serializer = null)
    {
        AnonymousPipeClientStream sendPipeStream = new (PipeDirection.Out, receivePipeHandle);
        AnonymousPipeClientStream receivePipeStream = new (PipeDirection.In, sendPipeHandle);
        return new LocalIpcClient(sendPipeStream, receivePipeStream, serializer);
    }

    /// <summary>
    /// Initialize method.
    /// This must be called after the class is created.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns><see cref="Task"/>.</returns>
    public override async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await base.InitializeAsync(cancellationToken).ConfigureAwait(false);

        // Send back the process of the client so the server knows if it is running in a different process.
        await this.SendAsync(Environment.ProcessId, cancellationToken).ConfigureAwait(false);
    }
}
