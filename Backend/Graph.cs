namespace DeliveryRoutes.Backend;

/// <summary>
/// Weighted graph backed by adjacency lists.
/// </summary>
public class Graph<T> where T : notnull
{
    private readonly Dictionary<T, List<Edge<T>>> _adjacency = new();
    public bool IsDirected { get; }
    public int VertexCount => _adjacency.Count;
    public IReadOnlyCollection<T> Vertices => _adjacency.Keys;
    public Graph(bool directed = true)
    {
        IsDirected = directed;
    }

    public bool ContainsVertex(T vertex) => _adjacency.ContainsKey(vertex);
    public void AddVertex(T vertex)
    {
        if (!_adjacency.ContainsKey(vertex))
        {
            _adjacency[vertex] = new List<Edge<T>>();
        }
    }
    public bool HasEdge(T from, T to)
    {
        return _adjacency.TryGetValue(from, out var edges) &&
               edges.Any(e => EqualityComparer<T>.Default.Equals(e.To, to));
    }
    public void AddEdge(T from, T to, double weight)
    {
        if (weight <= 0)
        {
            throw new ArgumentException("Edge weight must be greater than zero.", nameof(weight));
        }

        AddVertex(from);
        AddVertex(to);

        if (!HasEdge(from, to))
        {
            _adjacency[from].Add(new Edge<T>(to, weight));
        }

        if (!IsDirected && !HasEdge(to, from))
        {
            _adjacency[to].Add(new Edge<T>(from, weight));
        }
    }
    public IReadOnlyList<Edge<T>> GetNeighbors(T vertex)
    {
        if (!_adjacency.TryGetValue(vertex, out var edges))
        {
            throw new KeyNotFoundException($"Vertex '{vertex}' does not exist in the graph.");
        }
        return edges;
    }
}
