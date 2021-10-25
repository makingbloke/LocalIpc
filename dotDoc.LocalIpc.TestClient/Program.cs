using DotDoc.LocalIpc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IPCClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using LocalIpcClient ipcClient = await LocalIpcClient.CreateAsync(args[0], args[1]);
            object value = await ipcClient.ReceiveAsync();
            await ipcClient.SendAsync(value);
        }
    }
}
