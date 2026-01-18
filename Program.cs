using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

const int START_PORT = 65525;
const int MAX_PORT = 65535;


var tokenSource = new CancellationTokenSource();

using var listener = NetworkListener.CreateWithinRange(START_PORT, MAX_PORT);
if (listener is null)
{
    Logger.Error("No port available");
    return 1;
}

ConnectionManager.Init(listener, tokenSource);
await ConnectionManager.Get().Run();

return 0;
