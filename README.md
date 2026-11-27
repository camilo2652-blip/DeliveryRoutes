DeliveryRoutes — Solution to the Pickup and Delivery Problem (PDP)
A C# (.NET) console application structured into two projects:

Backend: class library containing graph logic, shortest paths (Dijkstra), file loading (city and packages), route planning, and output formatting.

Frontend: console application (DeliveryRoutes.csproj) that takes command-line arguments or prompts for input interactively and displays the final itinerary.

How to Build and Run the Program
From the root directory of the repository (DeliveryRoutes), first build the solution with:

Bash
dotnet build
Then you can run the program in the following ways:

Full case (from the root directory, specifying the file paths):

Bash
dotnet run -c Release --project Frontend/DeliveryRoutes.csproj -- Frontend/city.txt Frontend/packages.txt D
Small test example:

Bash
dotnet run -c Release --project Frontend\DeliveryRoutes.csproj -- Frontend\city_mini.txt Frontend\packages_mini.txt A
Interactive mode (without arguments, prompts for input in the console):

Bash
dotnet run -c Release --project Frontend/DeliveryRoutes.csproj
Note: If the .txt files are in the current working directory, you can pass their filenames directly (city.txt packages.txt D).

Stop-Ordering Strategy
To build the route without wasting distance or breaking the rule of picking up a package before delivering it, the planner combines real distance calculations over the graph (using Dijkstra's algorithm to respect one-way streets and shortest paths) with an insertion heuristic and local search (reordering / rerouting):

Shortest-path calculation: Instead of measuring straight-line distance, it computes the minimum distance and node-by-node path between locations using Dijkstra's algorithm over the city graph.

Initial construction by insertion: Pickup and delivery pairs are added one by one by evaluating which position in the current route produces the smallest distance increase, always respecting that each package's pickup occurs before its delivery (and grouping actions when they share the same stop).

Improvement via local search (rerouting): Once a valid initial route is built, local adjustments (such as relocating or reordering stops within precedence constraints) are applied to reduce the total route distance whenever a swap improves it.

Why Not Try All Combinations?
Because the number of possible visit orders grows combinatorially / factorially as the number of packages and stops increases. Even after filtering only valid sequences where each pickup comes before its delivery, testing all possible permutations on a moderate or large set becomes computationally infeasible (combinatorial explosion). That is why building a strong solution with an insertion heuristic and refining it with local search is more practical within a reasonable runtime.