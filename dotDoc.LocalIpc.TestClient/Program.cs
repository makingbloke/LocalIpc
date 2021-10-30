using DotDoc.LocalIpc;
using System.Threading.Tasks;

namespace IPCClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using LocalIpcClient localIpcClient = LocalIpcClient.Create(args[0], args[1]);
            await localIpcClient.InitializeAsync().ConfigureAwait(false);

            object value = await localIpcClient.ReceiveAsync().ConfigureAwait(false);
            await localIpcClient.SendAsync(value).ConfigureAwait(false);
        }
    }
}
