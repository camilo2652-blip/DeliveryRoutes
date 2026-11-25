using DeliveryRoutes.Backend;

namespace Backend;

/// <summary>
/// Computes shortest paths on a directed weighted graph using Dijkstra's algorithm
/// and validates reachability across required courier locations.
/// </summary>
public class ShortestPaths
{
    private readonly Graph<string> _graph;
    private readonly Dictionary<string, Dictionary<string, double>> _distances = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string?>> _predecessors = new(StringComparer.Ordinal);

    public ShortestPaths(Graph<string> graph, string start, IEnumerable<string> requiredLocations)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(start);
        ArgumentNullException.ThrowIfNull(requiredLocations);

        _graph = graph;

        if (!_graph.ContainsVertex(start))
        {
            throw new InvalidOperationException($"Start location '{start}' does not exist in the city map.");
        }

        var relevantLocations = new HashSet<string>(StringComparer.Ordinal) { start };
        foreach (string location in requiredLocations)
        {
            if (!_graph.ContainsVertex(location))
            {
                throw new InvalidOperationException($"Location '{location}' does not exist in the city map.");
            }
            relevantLocations.Add(location);
        }

        foreach (string source in relevantLocations)
        {
            RunDijkstraFrom(source);
        }

        ValidateReachability(start, relevantLocations);
    }

    private void RunDijkstraFrom(string source)
    {
        var dist = new Dictionary<string, double>(StringComparer.Ordinal);
        var prev = new Dictionary<string, string?>(StringComparer.Ordinal);
        var queue = new PriorityQueue<string, double>();

        foreach (string vertex in _graph.Vertices)
        {
            dist[vertex] = double.PositiveInfinity;
            prev[vertex] = null;
        }

        dist[source] = 0;
        queue.Enqueue(source, 0);

        while (queue.Count > 0)
        {
            queue.TryDequeue(out string? current, out double currentDist);
            if (current is null || currentDist > dist[current])
            {
                continue;
            }

            foreach (Edge<string> edge in _graph.GetNeighbors(current))
            {
                double nextDist = currentDist + edge.Weight;
                if (nextDist < dist[edge.To])
                {
                    dist[edge.To] = nextDist;
                    prev[edge.To] = current;
                    queue.Enqueue(edge.To, nextDist);
                }
            }
        }

        _distances[source] = dist;
        _predecessors[source] = prev;
    }

    private void ValidateReachability(string start, HashSet<string> relevantLocations)
    {
        var startDistances = _distances[start];
        foreach (string target in relevantLocations)
        {
            if (double.IsInfinity(startDistances[target]))
            {
                throw new InvalidOperationException(
                    $"Location '{target}' is not reachable from start location '{start}'.");
            }
        }

        foreach (string from in relevantLocations)
        {
            foreach (string to in relevantLocations)
            {
                if (string.Equals(from, to, StringComparison.Ordinal))
                {
                    continue;
                }

                if (double.IsInfinity(_distances[from][to]))
                {
                    throw new InvalidOperationException(
                        $"Cannot travel from '{from}' to '{to}' respecting one-way streets.");
                }
            }
        }
    }

    public double GetDistance(string from, string to)
    {
        if (!_distances.TryGetValue(from, out var fromMap) || !fromMap.TryGetValue(to, out double distance))
        {
            throw new KeyNotFoundException($"Shortest distance from '{from}' to '{to}' was not precomputed.");
        }

        return distance;
    }

    public List<string> GetPath(string from, string to)
    {
        if (!_predecessors.TryGetValue(from, out var prevMap) || !prevMap.ContainsKey(to))
        {
            throw new KeyNotFoundException($"Path from '{from}' to '{to}' was not precomputed.");
        }

        if (double.IsInfinity(_distances[from][to]))
        {
            return new List<string>();
        }

        var path = new List<string>();
        string? step = to;

        while (step is not null)
        {
            path.Add(step);
            if (string.Equals(step, from, StringComparison.Ordinal))
            {
                break;
            }
            step = prevMap[step];
        }

        path.Reverse();
        return path;
    }
}