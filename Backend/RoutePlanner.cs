using Backend;

namespace Backend;

/// <summary>
/// Plans a valid pickup-and-delivery courier route using constructive insertion
/// and local search improvements over shortest paths.
/// </summary>
public class RoutePlanner
{
    private const int DefaultSeedCount = 40;
    private const int BaseSeed = 20260511;

    private readonly Graph<string> _city;
    private readonly List<Package> _packages;
    private readonly string _start;
    private readonly ShortestPaths _shortestPaths;
    private readonly int _seedCount;

    public RoutePlanner(
        Graph<string> city,
        IReadOnlyList<Package> packages,
        string start,
        int seedCount = DefaultSeedCount)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentException.ThrowIfNullOrWhiteSpace(start);

        if (seedCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seedCount), "Seed count must be greater than zero.");
        }

        _city = city;
        _packages = packages.ToList();
        _start = start;
        _seedCount = seedCount;

        var locations = new List<string>(_packages.Count * 2);
        foreach (Package pkg in _packages)
        {
            locations.Add(pkg.Pickup);
            locations.Add(pkg.Delivery);
        }

        _shortestPaths = new ShortestPaths(_city, _start, locations);
    }

    public List<Stop> Plan()
    {
        if (_packages.Count == 0)
        {
            return new List<Stop>();
        }

        List<Stop>? bestRoute = null;
        double bestDistance = double.PositiveInfinity;

        for (int attempt = 0; attempt < _seedCount; attempt++)
        {
            var rng = new Random(BaseSeed + attempt * 7919);
            var order = _packages.OrderBy(_ => rng.Next()).ToList();

            List<Stop> route = BuildInsertionRoute(order);
            route = ImproveWithRelocatePackage(route);
            route = ImproveWithMoveSingleStop(route);
            route = ImproveWithRelocatePackage(route);

            if (!IsValid(route))
            {
                continue;
            }

            double distance = TotalDistance(route);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestRoute = route;
            }
        }

        return bestRoute ?? new List<Stop>();
    }

    private List<Stop> BuildInsertionRoute(List<Package> orderedPackages)
    {
        var route = new List<Stop>(orderedPackages.Count * 2);

        foreach (Package pkg in orderedPackages)
        {
            var pickupStop = new Stop(pkg.Pickup, StopKind.Pickup, pkg.Id);
            var deliveryStop = new Stop(pkg.Delivery, StopKind.Delivery, pkg.Id);

            if (route.Count == 0)
            {
                route.Add(pickupStop);
                route.Add(deliveryStop);
                continue;
            }

            double bestDelta = double.PositiveInfinity;
            int bestPickupIdx = 0;
            int bestDeliveryIdx = 1;

            for (int pIdx = 0; pIdx <= route.Count; pIdx++)
            {
                for (int dIdx = pIdx + 1; dIdx <= route.Count + 1; dIdx++)
                {
                    double delta = CalculatePackageInsertionDelta(route, pickupStop, pIdx, deliveryStop, dIdx);
                    if (delta < bestDelta)
                    {
                        bestDelta = delta;
                        bestPickupIdx = pIdx;
                        bestDeliveryIdx = dIdx;
                    }
                }
            }

            route.Insert(bestPickupIdx, pickupStop);
            route.Insert(bestDeliveryIdx, deliveryStop);
        }

        return route;
    }

    private double CalculatePackageInsertionDelta(
        List<Stop> currentRoute,
        Stop pickupStop,
        int pickupIndex,
        Stop deliveryStop,
        int deliveryIndex)
    {
        var candidate = new List<Stop>(currentRoute.Count + 2);
        candidate.AddRange(currentRoute);
        candidate.Insert(pickupIndex, pickupStop);
        candidate.Insert(deliveryIndex, deliveryStop);

        return TotalDistance(candidate) - TotalDistance(currentRoute);
    }

    private List<Stop> ImproveWithRelocatePackage(List<Stop> initialRoute)
    {
        var route = new List<Stop>(initialRoute);
        double currentDistance = TotalDistance(route);
        bool improved = true;

        while (improved)
        {
            improved = false;

            foreach (Package pkg in _packages)
            {
                int oldPickupIdx = route.FindIndex(s => s.PackageId == pkg.Id && s.Kind == StopKind.Pickup);
                int oldDeliveryIdx = route.FindIndex(s => s.PackageId == pkg.Id && s.Kind == StopKind.Delivery);

                if (oldPickupIdx < 0 || oldDeliveryIdx < 0 || oldPickupIdx >= oldDeliveryIdx)
                {
                    continue;
                }

                Stop pickupStop = route[oldPickupIdx];
                Stop deliveryStop = route[oldDeliveryIdx];

                var withoutPair = new List<Stop>(route);
                withoutPair.RemoveAt(oldDeliveryIdx);
                withoutPair.RemoveAt(oldPickupIdx);

                double bestCandidateDistance = currentDistance;
                List<Stop>? bestCandidate = null;

                for (int pIdx = 0; pIdx <= withoutPair.Count; pIdx++)
                {
                    for (int dIdx = pIdx + 1; dIdx <= withoutPair.Count + 1; dIdx++)
                    {
                        var candidate = new List<Stop>(withoutPair.Count + 2);
                        candidate.AddRange(withoutPair);
                        candidate.Insert(pIdx, pickupStop);
                        candidate.Insert(dIdx, deliveryStop);

                        double candidateDistance = TotalDistance(candidate);
                        if (candidateDistance + 1e-6 < bestCandidateDistance && IsValid(candidate))
                        {
                            bestCandidateDistance = candidateDistance;
                            bestCandidate = candidate;
                        }
                    }
                }

                if (bestCandidate is not null)
                {
                    route = bestCandidate;
                    currentDistance = bestCandidateDistance;
                    improved = true;
                }
            }
        }

        return route;
    }

    private List<Stop> ImproveWithMoveSingleStop(List<Stop> initialRoute)
    {
        var route = new List<Stop>(initialRoute);
        double currentDistance = TotalDistance(route);
        bool improved = true;

        while (improved)
        {
            improved = false;

            for (int fromIdx = 0; fromIdx < route.Count; fromIdx++)
            {
                Stop movingStop = route[fromIdx];
                var baseWithoutStop = new List<Stop>(route);
                baseWithoutStop.RemoveAt(fromIdx);

                for (int toIdx = 0; toIdx <= baseWithoutStop.Count; toIdx++)
                {
                    if (toIdx == fromIdx)
                    {
                        continue;
                    }

                    var candidate = new List<Stop>(baseWithoutStop.Count + 1);
                    candidate.AddRange(baseWithoutStop);
                    candidate.Insert(toIdx, movingStop);

                    if (!IsValid(candidate))
                    {
                        continue;
                    }

                    double candidateDistance = TotalDistance(candidate);
                    if (candidateDistance + 1e-6 < currentDistance)
                    {
                        currentDistance = candidateDistance;
                        route = candidate;
                        improved = true;
                        break;
                    }
                }

                if (improved)
                {
                    break;
                }
            }
        }

        return route;
    }

    public double TotalDistance(List<Stop> route)
    {
        ArgumentNullException.ThrowIfNull(route);

        if (route.Count == 0)
        {
            return 0.0;
        }

        double total = _shortestPaths.GetDistance(_start, route[0].Location);
        for (int i = 1; i < route.Count; i++)
        {
            total += _shortestPaths.GetDistance(route[i - 1].Location, route[i].Location);
        }

        return total;
    }

    public bool IsValid(List<Stop> route)
    {
        if (route is null || route.Count != _packages.Count * 2)
        {
            return false;
        }

        var pickedUp = new HashSet<int>();
        var delivered = new HashSet<int>();
        var expectedIds = _packages.Select(p => p.Id).ToHashSet();

        string currentLocation = _start;

        foreach (Stop stop in route)
        {
            if (!expectedIds.Contains(stop.PackageId))
            {
                return false;
            }

            double legDistance = _shortestPaths.GetDistance(currentLocation, stop.Location);
            if (double.IsInfinity(legDistance))
            {
                return false;
            }

            if (stop.Kind == StopKind.Pickup)
            {
                if (!pickedUp.Add(stop.PackageId))
                {
                    return false;
                }
            }
            else if (stop.Kind == StopKind.Delivery)
            {
                if (!pickedUp.Contains(stop.PackageId) || !delivered.Add(stop.PackageId))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            currentLocation = stop.Location;
        }

        return pickedUp.Count == _packages.Count && delivered.Count == _packages.Count;
    }

    public List<string> PathBetween(string from, string to)
    {
        return _shortestPaths.GetPath(from, to);
    }
}