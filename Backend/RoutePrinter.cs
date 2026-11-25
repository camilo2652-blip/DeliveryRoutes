using DeliveryRoutes.Backend;
using System.Globalization;
using System.Text;

namespace Backend;

/// <summary>
/// Formats courier routes into human-readable Spanish console output.
/// </summary>
public static class RoutePrinter
{
    private sealed record GroupedStop(string Location, List<int> Deliveries, List<int> Pickups);

    public static string FormatRoute(
        Graph<string> city,
        IReadOnlyList<Package> packages,
        string start,
        List<Stop> route,
        RoutePlanner planner)
    {
        ArgumentNullException.ThrowIfNull(city);
        ArgumentNullException.ThrowIfNull(packages);
        ArgumentException.ThrowIfNullOrWhiteSpace(start);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(planner);

        int oneWayCount = CountOneWayStreets(city);
        int twoWayCount = CountTwoWayStreets(city);
        int totalStreets = oneWayCount + twoWayCount;

        var groupedStops = GroupConsecutiveStopsByLocation(route);

        var sb = new StringBuilder();
        sb.AppendLine($"Posición inicial: {start}");
        sb.AppendLine($"Paquetes: {packages.Count} Ubicaciones: {city.VertexCount}");
        sb.AppendLine($"Calles: {totalStreets} ({oneWayCount} de un solo sentido)");

        string currentLocation = start;
        double totalMeters = 0.0;
        int stopNumber = 1;

        foreach (GroupedStop group in groupedStops)
        {
            List<string> path = planner.PathBetween(currentLocation, group.Location);
            double legMeters = CalculatePathMeters(city, path);
            totalMeters += legMeters;

            string actionsText = FormatActions(group.Deliveries, group.Pickups);
            string formattedMeters = legMeters.ToString("0.##", CultureInfo.InvariantCulture);

            if (string.Equals(currentLocation, group.Location, StringComparison.Ordinal) && legMeters == 0)
            {
                sb.AppendLine($"{stopNumber}. En {group.Location}: {actionsText}.");
            }
            else
            {
                sb.AppendLine($"{stopNumber}. Ir de {currentLocation} a {group.Location} ({formattedMeters} m): {actionsText}.");
            }

            if (path.Count > 1)
            {
                sb.AppendLine($"Camino: {string.Join(" -> ", path)}");
            }

            currentLocation = group.Location;
            stopNumber++;
        }

        double totalKm = totalMeters / 1000.0;
        sb.AppendLine("Todos los paquetes fueron recogidos y entregados.");
        sb.AppendLine($"Recorrido total: {totalKm.ToString("0.00", CultureInfo.InvariantCulture)} km");

        return sb.ToString();
    }

    private static List<GroupedStop> GroupConsecutiveStopsByLocation(List<Stop> route)
    {
        var result = new List<GroupedStop>();

        foreach (Stop stop in route)
        {
            if (result.Count > 0 && string.Equals(result[^1].Location, stop.Location, StringComparison.Ordinal))
            {
                if (stop.Kind == StopKind.Delivery)
                {
                    result[^1].Deliveries.Add(stop.PackageId);
                }
                else
                {
                    result[^1].Pickups.Add(stop.PackageId);
                }
            }
            else
            {
                var deliveries = new List<int>();
                var pickups = new List<int>();

                if (stop.Kind == StopKind.Delivery)
                {
                    deliveries.Add(stop.PackageId);
                }
                else
                {
                    pickups.Add(stop.PackageId);
                }

                result.Add(new GroupedStop(stop.Location, deliveries, pickups));
            }
        }

        return result;
    }

    private static string FormatActions(List<int> deliveries, List<int> pickups)
    {
        var parts = new List<string>();

        if (deliveries.Count > 0)
        {
            parts.Add($"{GetVerb(deliveries.Count, "entregar")} {FormatPackageList(deliveries)}");
        }

        if (pickups.Count > 0)
        {
            parts.Add($"{GetVerb(pickups.Count, "recoger")} {FormatPackageList(pickups)}");
        }

        return string.Join(" y ", parts);
    }

    private static string GetVerb(int count, string baseVerb)
    {
        return baseVerb;
    }

    private static string FormatPackageList(List<int> ids)
    {
        if (ids.Count == 1)
        {
            return $"paquete {ids[0]}";
        }

        if (ids.Count == 2)
        {
            return $"paquetes {ids[0]} y {ids[1]}";
        }

        string allExceptLast = string.Join(", ", ids.Take(ids.Count - 1));
        return $"paquetes {allExceptLast} y {ids[^1]}";
    }

    private static double CalculatePathMeters(Graph<string> city, List<string> path)
    {
        if (path.Count < 2)
        {
            return 0.0;
        }

        double sum = 0.0;
        for (int i = 1; i < path.Count; i++)
        {
            Edge<string> edge = city.GetNeighbors(path[i - 1])
                .First(e => string.Equals(e.To, path[i], StringComparison.Ordinal));
            sum += edge.Weight;
        }

        return sum;
    }

    private static int CountOneWayStreets(Graph<string> city)
    {
        int oneWay = 0;
        foreach (string from in city.Vertices)
        {
            foreach (Edge<string> edge in city.GetNeighbors(from))
            {
                bool reverseExists = city.ContainsVertex(edge.To) &&
                                     city.GetNeighbors(edge.To).Any(e => string.Equals(e.To, from, StringComparison.Ordinal));
                if (!reverseExists)
                {
                    oneWay++;
                }
            }
        }
        return oneWay;
    }

    private static int CountTwoWayStreets(Graph<string> city)
    {
        int directedPairs = 0;
        foreach (string from in city.Vertices)
        {
            foreach (Edge<string> edge in city.GetNeighbors(from))
            {
                bool reverseExists = city.ContainsVertex(edge.To) &&
                                     city.GetNeighbors(edge.To).Any(e => string.Equals(e.To, from, StringComparison.Ordinal));
                if (reverseExists)
                {
                    directedPairs++;
                }
            }
        }
        return directedPairs / 2;
    }
}