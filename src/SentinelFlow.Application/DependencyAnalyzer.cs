using SentinelFlow.Domain;

namespace SentinelFlow.Application;

public record RootCauseAnalysis(string ProbableRootCause, double Confidence, IReadOnlyList<string> AffectedServices);

public static class DependencyAnalyzer
{
    public static RootCauseAnalysis Analyze(MonitoredService service, IEnumerable<Dependency> dependencies, IReadOnlyDictionary<Guid, ServiceHealth> healthById, IReadOnlyDictionary<Guid, string> namesById)
    {
        var affected = new List<string>();
        var queue = new Queue<Guid>();
        queue.Enqueue(service.Id);
        var visited = new HashSet<Guid> { service.Id };

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var dep in dependencies.Where(d => d.ServiceId == current))
            {
                if (visited.Add(dep.DependsOnServiceId))
                    queue.Enqueue(dep.DependsOnServiceId);
            }
        }

        var unhealthy = visited
            .Where(id => healthById.TryGetValue(id, out var h) && h == ServiceHealth.Unhealthy)
            .ToList();

        if (unhealthy.Count == 0)
            return new RootCauseAnalysis("No unhealthy dependency found", 0, Array.Empty<string>());

        var root = unhealthy.First();
        var impacted = visited
            .Where(id => id != root)
            .Select(id => namesById.GetValueOrDefault(id, id.ToString()))
            .ToList();

        double confidence = Math.Min(0.99, 0.6 + 0.1 * unhealthy.Count);
        return new RootCauseAnalysis(namesById.GetValueOrDefault(root, root.ToString()), confidence, impacted);
    }
}
