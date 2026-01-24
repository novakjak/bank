using System.Threading;

public sealed class MonitoringService : IDisposable
{
    private readonly MetricsCollector _collector;
    private readonly IMonitoringStorage _storage;
    private readonly Func<int> _activeConnections;
    private readonly Func<PersistenceStrategy> _strategy;

    private readonly Timer _timer;

    public MonitoringService(
        MetricsCollector collector,
        IMonitoringStorage storage,
        Func<int> activeConnections,
        Func<PersistenceStrategy> strategy,
        int intervalSeconds = 10
    )
    {
        _collector = collector;
        _storage = storage;
        _activeConnections = activeConnections;
        _strategy = strategy;

        _timer = new Timer(
            _ => Flush(),
            null,
            TimeSpan.FromSeconds(intervalSeconds),
            TimeSpan.FromSeconds(intervalSeconds)
        );
    }

    private void Flush()
    {
        try
        {
            var snap = _collector.BuildSnapshot(
                _activeConnections(),
                _strategy()
            );

            _storage.SaveSnapshot(snap);

            foreach (var m in _collector.BuildCommandMetricsAndReset())
                _storage.UpdateCommandMetric(m);
        }
        catch
        {
            // monitoring must never crash the app
        }
    }

    public void Dispose() => _timer.Dispose();
}
