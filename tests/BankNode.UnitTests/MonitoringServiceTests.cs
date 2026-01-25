using Xunit;

public class MonitoringServiceTests
{
    [Fact]
    public void MonitoringService_CanBeCreated()
    {
        var svc = new MonitoringService(
            new MetricsCollector(),
            new DummyMonitoringStorage(),
            () => 0,
            () => PersistenceStrategy.CSV,
            "Server=localhost;"
        );

        Assert.NotNull(svc);
    }
}

public class DummyMonitoringStorage : IMonitoringStorage
{
    public void SaveSnapshot(SnapshotRecord snapshot) { }
    public void UpdateCommandMetric(CommandMetricRecord metric) { }
    public IEnumerable<SnapshotRecord> GetSnapshots(int limit) => [];
    public IEnumerable<CommandMetricRecord> GetCommandMetrics() => [];
}
