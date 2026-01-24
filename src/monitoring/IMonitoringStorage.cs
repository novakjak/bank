public interface IMonitoringStorage
{
    void SaveSnapshot(SnapshotRecord snapshot);
    void UpdateCommandMetric(CommandMetricRecord metric);

    IEnumerable<SnapshotRecord> GetSnapshots(int limit);
    IEnumerable<CommandMetricRecord> GetCommandMetrics();
}
