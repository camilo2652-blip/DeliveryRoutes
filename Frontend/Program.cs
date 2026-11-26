using System.Text;
using Backend;

Console.OutputEncoding = Encoding.UTF8;

string cityPath;
string packagesPath;
string startLocation;

if (args.Length >= 3)
{
    cityPath = args[0].Trim();
    packagesPath = args[1].Trim();
    startLocation = args[2].Trim();
}
else
{
    Console.Write("Ruta del archivo de ciudad (ej. city.txt): ");
    cityPath = (Console.ReadLine() ?? string.Empty).Trim();

    Console.Write("Ruta del archivo de paquetes (ej. packages.txt): ");
    packagesPath = (Console.ReadLine() ?? string.Empty).Trim();

    Console.Write("Ubicación inicial (ej. D): ");
    startLocation = (Console.ReadLine() ?? string.Empty).Trim();
}

try
{
    Graph<string> city = CityFile.Load(cityPath);
    List<Package> packages = PackageFile.Load(packagesPath, city);

    var planner = new RoutePlanner(city, packages, startLocation);
    List<Stop> route = planner.Plan();

    if (!planner.IsValid(route))
    {
        Console.Error.WriteLine("Error: no se pudo generar una ruta válida.");
        return;
    }

    string output = RoutePrinter.FormatRoute(city, packages, startLocation, route, planner);
    Console.WriteLine(output);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error de validación o ejecución: {ex.Message}");
}