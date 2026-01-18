using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

const int START_PORT = 65525;
const int MAX_PORT = 65535;
const string CONFIG_FILE = "settings.ini";

var conf = ConfigParser.Parse<Config>(CONFIG_FILE);


var tokenSource = new CancellationTokenSource();

using var listener = conf.DefaultPort is not null
    ? NetworkListener.Create((int)conf.DefaultPort)
    : NetworkListener.CreateWithinRange(START_PORT, MAX_PORT);
if (listener is null)
{
    Logger.Error("No port available");
    return 1;
}

try
{
    ConnectionManager.Init(listener, tokenSource);
    await ConnectionManager.Get().Run();
}
catch (Exception e)
{
    Logger.Error(e.Message);
    return 1;
}

return 0;
