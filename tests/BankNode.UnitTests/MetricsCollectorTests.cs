using Xunit;

public class MetricsCollectorTests
{
    [Fact]
    public void OnCommandSuccess_IncrementsCounters()
    {
        var m = new MetricsCollector();

        m.OnCommandSuccess("AC", 10);
        m.OnCommandSuccess("AC", 20);

        var metric = Assert.Single(m.BuildCommandMetricsAndReset());

        Assert.Equal("AC", metric.CommandName);
        Assert.Equal(2, metric.ExecutionCount);
        Assert.Equal(0, metric.ErrorCount);
        Assert.Equal(15, metric.AvgExecutionMs);
    }

    [Fact]
    public void OnCommandError_IncrementsErrorCount()
    {
        var m = new MetricsCollector();

        m.OnCommandError("BN", 5);

        var metric = Assert.Single(m.BuildCommandMetricsAndReset());

        Assert.Equal(1, metric.ExecutionCount);
        Assert.Equal(1, metric.ErrorCount);
    }
}
