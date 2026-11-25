namespace DeliveryRoutes.Backend;

/// <summary>
/// Represents a weighted directed edge to a neighboring vertex.
/// </summary>
public readonly record struct Edge<T>(T To, double Weight) where T : notnull;