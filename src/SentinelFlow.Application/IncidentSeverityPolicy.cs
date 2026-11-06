using SentinelFlow.Domain;

namespace SentinelFlow.Application;

public static class IncidentSeverityPolicy
{
    public static IncidentSeverity FromMetric(MetricType type, double pctChange, Criticality criticality)
    {
        double score = pctChange;
        if (criticality == Criticality.Critical) score *= 1.5;
        else if (criticality == Criticality.High) score *= 1.25;

        return score switch
        {
            >= 10 => IncidentSeverity.Critical,
            >= 5 => IncidentSeverity.High,
            >= 2 => IncidentSeverity.Warning,
            _ => IncidentSeverity.Info
        };
    }
}
