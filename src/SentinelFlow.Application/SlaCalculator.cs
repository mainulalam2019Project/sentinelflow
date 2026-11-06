namespace SentinelFlow.Application;

public static class SlaCalculator
{
    public static double UptimePercentage(IEnumerable<bool> checks)
    {
        var list = checks.ToList();
        if (list.Count == 0) return 100;
        return Math.Round(100.0 * list.Count(c => c) / list.Count, 4);
    }
}
