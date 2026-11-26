namespace Backend;

/// <summary>
/// Type of task performed at a stop.
/// </summary>
public enum StopKind
{
    Pickup,
    Delivery
}

/// <summary>
/// Represents a single task stop in the courier route.
/// </summary>
public readonly record struct Stop(string Location, StopKind Kind, int PackageId);