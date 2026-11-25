using DeliveryRoutes.Backend;
using System.Globalization;

namespace Backend;
/// <summary>
/// Loads and validates city streets from a plain text file into a directed weighted graph.
/// </summary>
public static class CityFile
{
    public static Graph<string> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new FileNotFoundException($"City file was not found: '{path}'.", path);
        }

        var graph = new Graph<string>(directed: true);
        var seenStreets = new HashSet<string>(StringComparer.Ordinal);
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
            if (parts.Length != 4)
            {
                throw new FormatException($"Line {lineNumber}: expected 4 fields (from,to,distance_in_meters,direction).");
            }
            string from = parts[0].Trim();
            string to = parts[1].Trim();
            string distanceText = parts[2].Trim();
            string directionText = parts[3].Trim();

            if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
            {
                throw new FormatException($"Line {lineNumber}: 'from' and 'to' locations cannot be empty.");
            }

            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                throw new FormatException($"Line {lineNumber}: self-loop street from '{from}' to '{to}' is not allowed.");
            }

            if (!double.TryParse(distanceText, NumberStyles.Float, CultureInfo.InvariantCulture, out double distance) ||
                double.IsNaN(distance) || double.IsInfinity(distance) || distance <= 0)
            {
                throw new FormatException($"Line {lineNumber}: distance '{distanceText}' must be a positive number greater than 0.");
            }

            bool isTwoWay;
            if (string.Equals(directionText, "TwoWay", StringComparison.OrdinalIgnoreCase))
            {
                isTwoWay = true;
            }
            else if (string.Equals(directionText, "OneWay", StringComparison.OrdinalIgnoreCase))
            {
                isTwoWay = false;
            }
            else
            {
                throw new FormatException($"Line {lineNumber}: invalid direction '{directionText}'. Expected TwoWay or OneWay.");
            }

            string canonicalStreetKey = GetCanonicalStreetKey(from, to);
            if (!seenStreets.Add(canonicalStreetKey))
            {
                throw new InvalidOperationException($"Line {lineNumber}: duplicate street between '{from}' and '{to}'.");
            }

            graph.AddEdge(from, to, distance);
            if (isTwoWay)
            {
                graph.AddEdge(to, from, distance);
            }
        }

        return graph;
    }

    private static string GetCanonicalStreetKey(string a, string b)
    {
        return string.Compare(a, b, StringComparison.Ordinal) <= 0
            ? $"{a}\u001F{b}"
            : $"{b}\u001F{a}";
    }

}
