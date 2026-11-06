namespace SentinelFlow.Application;

public record AnomalyResult(bool IsAnomaly, string Severity, double Confidence, string Metric, double Baseline, double Observed);

public static class AnomalyDetector
{
    /// <summary>
    /// Statistical anomaly detection using rolling mean, standard deviation and percentage change.
    /// </summary>
    public static AnomalyResult Evaluate(string metric, IReadOnlyList<double> history, double observed, double deviationThreshold = 3.0, double pctChangeThreshold = 2.0)
    {
        if (history.Count == 0)
            return new AnomalyResult(false, "Info", 0, metric, observed, observed);

        double mean = history.Average();
        double variance = history.Average(v => Math.Pow(v - mean, 2));
        double stdDev = Math.Sqrt(variance);

        double zScore = stdDev > 0 ? Math.Abs(observed - mean) / stdDev : (observed > mean ? double.MaxValue : 0);
        double pctChange = mean > 0 ? Math.Abs(observed - mean) / mean : 0;

        bool isAnomaly = zScore >= deviationThreshold || pctChange >= pctChangeThreshold;
        double severityScore = Math.Min(1.0, zScore / 6.0);
        string severity = !isAnomaly ? "Info" : severityScore > 0.66 ? "Critical" : severityScore > 0.33 ? "High" : "Warning";
        double confidence = Math.Min(0.99, Math.Max(0.5, severityScore));

        return new AnomalyResult(isAnomaly, severity, confidence, metric, mean, observed);
    }
}
