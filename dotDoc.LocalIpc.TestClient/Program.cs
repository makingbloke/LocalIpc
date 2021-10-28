using DotDoc.LocalIpc;
using System.Threading.Tasks;

namespace IPCClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using LocalIpcClient ipcClient = await LocalIpcClient.CreateAsync(args[0], args[1]).ConfigureAwait(false);
            object value = await ipcClient.ReceiveAsync().ConfigureAwait(false);
            await ipcClient.SendAsync(value).ConfigureAwait(false);
        }
    }
}
