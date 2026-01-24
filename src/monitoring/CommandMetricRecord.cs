public sealed record CommandMetricRecord(
    string CommandName,
    long ExecutionCount,
    long ErrorCount,
    double AvgExecutionMs
);
