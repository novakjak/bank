using System;
using System.Threading;

const string CONFIG_FILE = "settings.ini";

var conf = ConfigParser.Parse<Config>(CONFIG_FILE);

var csv = new CsvAccountStorage("data/accounts.csv");
var db  = new MySqlAccountStorage(conf.MySql);
var storage = new HybridAccountStorage(csv, db);
BankStorage.Init(storage);

var metrics = ProgramMetrics.Instance;
var monitoringDb = new MySqlMonitoringStorage(conf.MySql);

var monitoring = new MonitoringService(
    metrics,
    monitoringDb,
    () => ConnectionManager.Get().ActiveConnections,
    () => storage.StrategyName == "MYSQL"
        ? PersistenceStrategy.MYSQL
        : PersistenceStrategy.CSV
);

var tokenSource = new CancellationTokenSource();

int min_port = BankConnection.MIN_PORT;
int max_port = BankConnection.MAX_PORT;
int? def_port = conf.DefaultPort;

if (def_port < min_port || def_port > max_port)
    Logger.Warn($"Warning: Using port ({def_port}) out of the standard range ({min_port} - {max_port}); service might be unreachable.");

using var listener = def_port is not null
    ? NetworkListener.Create((int)def_port)
    : NetworkListener.CreateWithinRange(BankConnection.MIN_PORT, BankConnection.MAX_PORT);
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
