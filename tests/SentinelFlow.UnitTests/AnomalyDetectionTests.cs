using SentinelFlow.Application;
using Xunit;

namespace SentinelFlow.UnitTests;

public class AnomalyDetectionTests
{
    private static readonly double[] Normal = Enumerable.Range(0, 30).Select(i => 200.0 + (i % 5) * 10).ToArray(); // mean 220, std ~14

    [Fact]
    public void NormalValue_IsNotAnomaly()
    {
        var r = AnomalyDetector.Evaluate("latency", Normal, 220);
        Assert.False(r.IsAnomaly);
    }

    [Fact]
    public void LatencySpike_IsAnomaly_WithHighSeverity()
    {
        var r = AnomalyDetector.Evaluate("latency", Normal, 2400);
        Assert.True(r.IsAnomaly);
        Assert.Equal("Critical", r.Severity);
        Assert.True(r.Confidence > 0.5);
        Assert.True(r.Observed > r.Baseline);
    }

    [Fact]
    public void EmptyHistory_ReturnsNoAnomaly()
    {
        var r = AnomalyDetector.Evaluate("latency", Array.Empty<double>(), 500);
        Assert.False(r.IsAnomaly);
    }
}

public class IncidentSeverityTests
{
    [Fact]
    public void CriticalService_LargeChange_IsCritical()
    {
        Assert.Equal(SentinelFlow.Domain.IncidentSeverity.Critical,
            IncidentSeverityPolicy.FromMetric(SentinelFlow.Domain.MetricType.Latency, 12, SentinelFlow.Domain.Criticality.Critical));
    }

    [Fact]
    public void LowCriticality_SmallChange_IsInfo()
    {
        Assert.Equal(SentinelFlow.Domain.IncidentSeverity.Info,
            IncidentSeverityPolicy.FromMetric(SentinelFlow.Domain.MetricType.Latency, 0.5, SentinelFlow.Domain.Criticality.Low));
    }
}

public class SlaCalculatorTests
{
    [Fact]
    public void Uptime_CalculatesPercentage()
    {
        var checks = new[] { true, true, true, false, true };
        Assert.Equal(80, SlaCalculator.UptimePercentage(checks));
    }

    [Fact]
    public void EmptyChecks_Returns100()
    {
        Assert.Equal(100, SlaCalculator.UptimePercentage(Array.Empty<bool>()));
    }
}

public class DependencyAnalyzerTests
{
    [Fact]
    public void UnhealthyDatabase_IsIdentifiedAsRootCause()
    {
        var checkout = Guid.NewGuid(); var payment = Guid.NewGuid(); var paymentDb = Guid.NewGuid();
        var deps = new[]
        {
            new SentinelFlow.Domain.Dependency { ServiceId = checkout, DependsOnServiceId = payment },
            new SentinelFlow.Domain.Dependency { ServiceId = payment, DependsOnServiceId = paymentDb },
        };
        var service = new SentinelFlow.Domain.MonitoredService { Id = checkout, Name = "Checkout API" };
        var health = new Dictionary<Guid, SentinelFlow.Domain.ServiceHealth>
        {
            [checkout] = SentinelFlow.Domain.ServiceHealth.Unhealthy,
            [payment] = SentinelFlow.Domain.ServiceHealth.Healthy,
            [paymentDb] = SentinelFlow.Domain.ServiceHealth.Unhealthy,
        };
        var names = new Dictionary<Guid, string> { [checkout] = "Checkout API", [payment] = "Payment API", [paymentDb] = "Payment Database" };

        var result = DependencyAnalyzer.Analyze(service, deps, health, names);

        Assert.Contains(result.ProbableRootCause, new[] { "Payment Database", "Checkout API" });
        Assert.True(result.Confidence > 0);
    }
}

public class IncidentSummaryTests
{
    [Fact]
    public void Summary_ContainsBaselineAndObserved()
    {
        var text = IncidentSummaryGenerator.Generate("Payment API", "latency", 220, 2400, DateTime.UtcNow, "Payment Database");
        Assert.Contains("220", text);
        Assert.Contains("2,400", text);
        Assert.Contains("Payment Database", text);
    }
}
