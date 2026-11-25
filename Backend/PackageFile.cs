using DeliveryRoutes.Backend;
using System.Globalization;

namespace Backend;

/// <summary>
/// Loads, validates, and groups package pickup and delivery tasks from a plain text file.
/// </summary>
public static class PackageFile
{
    public static List<Package> Load(string path, Graph<string> city)
    {
        ArgumentNullException.ThrowIfNull(city);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException($"Packages file was not found: '{path}'.", path);
        }

        var pickups = new Dictionary<int, (string Location, int LineNumber)>();
        var deliveries = new Dictionary<int, (string Location, int LineNumber)>();
        var lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int lineNumber = i + 1;
            string rawLine = lines[i].Trim();

            if (string.IsNullOrEmpty(rawLine) || rawLine.StartsWith('#'))
            {
                continue;
            }

            string[] parts = rawLine.Split(',');
            if (parts.Length != 3)
            {
                throw new FormatException($"Line {lineNumber}: expected 3 fields (location,action,package).");
            }

            string location = parts[0].Trim();
            string actionText = parts[1].Trim();
            string packageIdText = parts[2].Trim();

            if (string.IsNullOrEmpty(location) || !city.ContainsVertex(location))
            {
                throw new InvalidOperationException($"Line {lineNumber}: location '{location}' does not exist in the city map.");
            }

            StopKind kind;
            if (string.Equals(actionText, "Pickup", StringComparison.OrdinalIgnoreCase))
            {
                kind = StopKind.Pickup;
            }
            else if (string.Equals(actionText, "Delivery", StringComparison.OrdinalIgnoreCase))
            {
                kind = StopKind.Delivery;
            }
            else
            {
                throw new FormatException($"Line {lineNumber}: invalid action '{actionText}'. Expected Pickup or Delivery.");
            }

            if (!int.TryParse(packageIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int packageId) ||
                packageId <= 0)
            {
                throw new FormatException($"Line {lineNumber}: package id '{packageIdText}' must be an integer greater than 0.");
            }

            if (kind == StopKind.Pickup)
            {
                if (!pickups.TryAdd(packageId, (location, lineNumber)))
                {
                    throw new InvalidOperationException(
                        $"Line {lineNumber}: duplicate Pickup for package {packageId} (first seen on line {pickups[packageId].LineNumber}).");
                }
            }
            else
            {
                if (!deliveries.TryAdd(packageId, (location, lineNumber)))
                {
                    throw new InvalidOperationException(
                        $"Line {lineNumber}: duplicate Delivery for package {packageId} (first seen on line {deliveries[packageId].LineNumber}).");
                }
            }
        }

        var allIds = new SortedSet<int>(pickups.Keys);
        allIds.UnionWith(deliveries.Keys);

        if (allIds.Count == 0)
        {
            throw new InvalidOperationException("Packages file contains no valid package tasks.");
        }

        var result = new List<Package>(allIds.Count);
        foreach (int id in allIds)
        {
            if (!pickups.TryGetValue(id, out var pickupInfo))
            {
                throw new InvalidOperationException($"Package {id} is missing its Pickup task.");
            }

            if (!deliveries.TryGetValue(id, out var deliveryInfo))
            {
                throw new InvalidOperationException($"Package {id} is missing its Delivery task.");
            }

            result.Add(new Package(id, pickupInfo.Location, deliveryInfo.Location));
        }

        return result;
    }
}