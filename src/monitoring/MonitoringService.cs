public sealed class MonitoringService : IDisposable
{
    private readonly MetricsCollector _collector;
    private readonly IMonitoringStorage _storage;
    private readonly Func<int> _activeConnections;
    private readonly Func<PersistenceStrategy> _strategy;
    private readonly string _conn;

    private readonly Timer _timer;

    public MonitoringService(
        MetricsCollector collector,
        IMonitoringStorage storage,
        Func<int> activeConnections,
        Func<PersistenceStrategy> strategy,
        string mysqlConn,
        int intervalSeconds = 10
    )
    {
        _collector = collector;
        _storage = storage;
        _activeConnections = activeConnections;
        _strategy = strategy;
        _conn = mysqlConn;

        _timer = new Timer(
            _ => Tick(),
            null,
            TimeSpan.FromSeconds(intervalSeconds),
            TimeSpan.FromSeconds(intervalSeconds)
        );
    }

    private void Tick()
    {
        try
        {
            var snapshot = _collector.BuildSnapshot(
                _activeConnections(),
                _strategy()
            );

            _storage.SaveSnapshot(snapshot);

            foreach (var metric in _collector.BuildCommandMetricsAndReset())
            {
                _storage.UpdateCommandMetric(metric);
            }

            using var c = new MySql.Data.MySqlClient.MySqlConnection(_conn);
            c.Open();

            using var tx = c.BeginTransaction();

            using var cmd = c.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                select shutdown_requested
                from node_control
                where id = 1
                for update
            """;

            var shutdown = Convert.ToBoolean(cmd.ExecuteScalar());

            if (shutdown)
            {
                using var reset = c.CreateCommand();
                reset.Transaction = tx;
                reset.CommandText = """
                    update node_control
                    set shutdown_requested = false
                    where id = 1
                """;
                reset.ExecuteNonQuery();

                tx.Commit();

                Logger.Warn("Shutdown requested via DB");
                Environment.Exit(0);
            }

            tx.Commit();
        }
        catch
        {
            // monitoring must never crash the app
        }
    }


    public void Dispose() => _timer.Dispose();
}
