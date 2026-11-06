namespace SentinelFlow.Application;

public static class IncidentSummaryGenerator
{
    public static string Generate(string serviceName, string metric, double baseline, double observed, DateTime startedAtUtc, string? rootCause)
    {
        return $"{serviceName} experienced a significant {metric} change beginning at {startedAtUtc:HH:mm:ss} UTC. " +
               $"{metric} moved from {baseline:N1} to {observed:N1}. " +
               (rootCause is null
                   ? "No single dependency could be isolated as the root cause."
                   : $"The strongest correlation points to {rootCause} as the probable root cause.");
    }
}
