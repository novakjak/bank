public enum HealthState
{
    OK,
    DEGRADED,
    ERROR
}

public enum PersistenceStrategy
{
    MYSQL,
    CSV
}

public sealed record SnapshotRecord(
    DateTime CreatedAt,
    long UptimeSeconds,
    HealthState HealthState,
    int ActiveConnections,
    long TotalCommands,
    long ProxyCommands,
    long ErrorCount,
    PersistenceStrategy PersistenceStrategy
);
