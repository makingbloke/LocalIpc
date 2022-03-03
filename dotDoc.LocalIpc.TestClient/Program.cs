// Copyright ©2021-2022 Mike King.
// This file is licensed to you under the MIT license.
// See the License.txt file in the solution root for more information.

using DotDoc.LocalIpc;

namespace IPCClient;

/// <summary>
/// Local Ipc External Test Client.
/// </summary>
public static class Program
{
    /// <summary>
    /// Receives an object, echoes it back to the server then exits.
    /// </summary>
    /// <param name="args">Arguments.</param>
    /// <returns><see cref="Task"/>.</returns>
    public static async Task Main(string[] args)
    {
        using LocalIpcClient localIpcClient = LocalIpcClient.Create(args[0], args[1]);
        await localIpcClient.InitializeAsync().ConfigureAwait(false);

        object value = await localIpcClient.ReceiveAsync().ConfigureAwait(false);
        await localIpcClient.SendAsync(value).ConfigureAwait(false);
    }
}
