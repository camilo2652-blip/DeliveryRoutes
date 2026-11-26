
# DeliveryRoutes — Solución al Pickup and Delivery Problem (PDP)

Aplicación de consola en C# (.NET) estructurada en dos proyectos:
- **`Backend`**: biblioteca de clases con la lógica de grafos, rutas más cortas (Dijkstra), carga de archivos (`city` y `packages`),
 planificación de rutas y formato de salida.
- **`Frontend`**: aplicación de consola (`DeliveryRoutes.csproj`) que recibe los argumentos o solicita datos de forma interactiva 
y muestra el itinerario final.

---

## Cómo compilar y ejecutar el programa

Desde la carpeta raíz del repositorio (`DeliveryRoutes`), primero compila la solución con:

```bash
dotnet build
Luego puedes ejecutar el programa de las siguientes maneras:

Caso completo (desde la raíz indicando la ruta de los archivos):

Bash
dotnet run -c Release --project Frontend/DeliveryRoutes.csproj -- Frontend/city.txt Frontend/packages.txt D
Ejemplo pequeño de prueba:

Bash
dotnet run -c Release --project Frontend/DeliveryRoutes.csproj -- Frontend/city_mini.txt Frontend/packages_mini.txt A
Modo interactivo (sin argumentos, pide los datos por consola):

Bash
dotnet run -c Release --project Frontend/DeliveryRoutes.csproj
Nota: Si los archivos .txt están en el directorio de trabajo actual, puedes pasar directamente sus nombres (city.txt packages.txt D).

Estrategia para ordenar las paradas
Para armar el recorrido sin que el repartidor pierda distancia innecesaria ni rompa la regla de recoger un paquete antes de entregarlo, 
el planificador combina el cálculo de distancias reales sobre el grafo (usando Dijkstra para respetar calles de un solo sentido y 
caminos mínimos) con una heurística de inserción y búsqueda local (reordenamiento / rerouting):

Cálculo de rutas más cortas: en lugar de medir distancia en línea recta, se obtiene la distancia mínima y el camino nodo a nodo entre 
ubicaciones utilizando Dijkstra sobre el grafo de la ciudad.

Construcción inicial por inserción: se van incorporando los pares de recogida y entrega evaluando en qué posición de la ruta actual 
generan el menor incremento de distancia, respetando siempre que la recogida de cada paquete ocurra antes que su entrega 
(y agrupando acciones cuando coinciden en la misma parada).

Mejora mediante búsqueda local (rerouting): una vez construida una ruta inicial válida, se aplican ajustes locales 
(como reubicar o reordenar paradas dentro de las restricciones de precedencia) para reducir el recorrido total cuando el intercambio mejora la distancia.

¿Por qué no se prueban todas las combinaciones?
Porque el número de posibles órdenes de visitas crece de forma combinatoria / factorial a medida que aumentan los paquetes 
y las paradas. Incluso filtrando solo las secuencias válidas donde cada recogida va antes que su entrega, probar todas las 
permutaciones posibles en un conjunto de tamaño moderado o grande se vuelve inviable en tiempo de cómputo (explosión combinatoria). 
Por eso es más práctico construir una buena solución con heurística de inserción y refinarla con búsqueda local en un tiempo razonable.