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

        }
}