using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;


public interface IBinarySerializable
{
    void WriteTo(BinaryWriter writer);

}


[Serializable]
public class GridNode<T>
{
    public T Data { get; private set; }
    public double X { get; private set; }
    public double Y { get; private set; }

    public GridNode(T data, double x, double y)
    {
        Data = data;
        X = x;
        Y = y;
    }

    private GridNode() { }

    public override string ToString() => Data?.ToString() ?? "null";


    public void WriteTo(BinaryWriter writer)
    {
        writer.Write(X);
        writer.Write(Y);

        if (Data is string s)
        {
            writer.Write(true);
            writer.Write(s);
        }
        else if (Data is int i)
        {
            writer.Write(true);
            writer.Write(i);
        }
        else if (Data is double d)
        {
            writer.Write(true);
            writer.Write(d);
        }

        else if (Data == null)
        {
            writer.Write(false);
        }
        else
        {

            throw new NotSupportedException($"Typ {typeof(T)} nelze automaticky serializovat touto metodou WriteTo. Upravte ji nebo použijte IBinarySerializable.");

        }
    }
    public static GridNode<T> ReadFrom(BinaryReader reader)
    {
        var node = new GridNode<T>();
        node.X = reader.ReadDouble();
        node.Y = reader.ReadDouble();

        bool hasData = reader.ReadBoolean();

        if (!hasData)
        {
            node.Data = default(T);
            return node;
        }

        if (typeof(T) == typeof(string))
        {
            node.Data = (T)(object)reader.ReadString();
        }
        else if (typeof(T) == typeof(int))
        {
            node.Data = (T)(object)reader.ReadInt32();
        }
        else if (typeof(T) == typeof(double))
        {
            node.Data = (T)(object)reader.ReadDouble();
        }

        else
        {
            throw new NotSupportedException($"Typ {typeof(T)} nelze automaticky deserializovat touto metodou ReadFrom. Upravte ji nebo použijte IBinarySerializable.");

        }
        return node;
    }
}

public class GridIndex<T>
{
    private int BLOCKING_FACTOR = 3;

    private List<double> xLines;
    private List<double> yLines;

    // Vrací kopie seznamů jako ReadOnly pro bezpečnost
    public IReadOnlyList<double> XLines => xLines?.AsReadOnly();
    public IReadOnlyList<double> YLines => yLines?.AsReadOnly();

    private List<List<List<GridNode<T>>>> grid;

    private bool drawVerticalLine = true;
    private string dataFilePath;


    public bool IsLoadedFromFile { get; private set; } = false;

    public GridIndex(int blockingFactor, string filePath, double xMin, double xMax, double yMin, double yMax)
    {
        this.BLOCKING_FACTOR = blockingFactor;

        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("Cesta k souboru nesmí být prázdná.", nameof(filePath));
        if (xMin >= xMax || yMin >= yMax)
            throw new ArgumentException("Neplatné hranice: Min musí být menší než Max.");

        this.dataFilePath = filePath;

        if (!Load()) // Pokusí se načíst data
        {
            // Pokud načtení selže (např. soubor neexistuje nebo je poškozen)
            Console.WriteLine($"Nepodařilo se načíst data z '{filePath}'. Inicializuje se nová mřížka.");
            xLines = new List<double> { xMin, xMax };
            yLines = new List<double> { yMin, yMax };
            InitializeGridStructure(); // Vytvoří počáteční strukturu gridu (1x1)
            this.IsLoadedFromFile = false; // Explicitně nastaveno
        }
        else
        {
            Console.WriteLine($"Mřížka úspěšně načtena z '{filePath}'.");
            // IsLoadedFromFile je již nastaveno na true uvnitř Load()
            // Ověření konzistence po načtení (volitelné)
            if (xLines == null || yLines == null || grid == null || xLines.Count < 2 || yLines.Count < 2)
            {
                Console.WriteLine("Chyba: Načtená data jsou nekonzistentní. Inicializuje se nová mřížka.");
                xLines = new List<double> { xMin, xMax };
                yLines = new List<double> { yMin, yMax };
                InitializeGridStructure();
                this.IsLoadedFromFile = false; // Reset příznaku
            }
        }
    }


    private void InitializeGridStructure()
    {
        grid = new List<List<List<GridNode<T>>>>();
        int xDim = Math.Max(1, xLines.Count - 1); // Počet sloupců
        int yDim = Math.Max(1, yLines.Count - 1); // Počet řádků

        Console.WriteLine($"Inicializace struktury mřížky: {xDim} sloupců x {yDim} řádků.");

        for (int i = 0; i < xDim; i++)
        {
            var column = new List<List<GridNode<T>>>();
            for (int j = 0; j < yDim; j++)
            {
                // Přidá prázdnou buňku s počáteční kapacitou
                column.Add(new List<GridNode<T>>(BLOCKING_FACTOR + 1));
            }
            grid.Add(column);
        }
    }


    public void AddPoint(T data, double x, double y)
    {
        // Kontrola null dat může záviset na typu T
        // if (data == null) throw new ArgumentNullException(nameof(data));

        // Ověření, zda je bod v rámci celkových hranic mřížky
        if (xLines == null || yLines == null || x < xLines[0] || x > xLines[xLines.Count - 1] || y < yLines[0] || y > yLines[yLines.Count - 1])
        {
            Console.WriteLine($"Varování: Bod ({x}, {y}) je mimo definované hranice mřížky. Bod nebude přidán.");
            return;
        }

        int xIndex = FindIndex(xLines, x);
        int yIndex = FindIndex(yLines, y);

        // Ověření platnosti indexů (mělo by být vždy platné, pokud je bod uvnitř hranic)
        if (xIndex < 0 || grid == null || xIndex >= grid.Count || grid[xIndex] == null || yIndex < 0 || yIndex >= grid[xIndex].Count)
        {
            Console.WriteLine($"Chyba: Vypočítaný index [{xIndex}][{yIndex}] pro bod ({x},{y}) je mimo rozsah mřížky. Bod nebude přidán.");
            return;
        }

        var newNode = new GridNode<T>(data, x, y);
        var cell = grid[xIndex][yIndex];

        // Ošetření null buňky (nemělo by nastat při správné inicializaci)
        if (cell == null)
        {
            Console.WriteLine($"Chyba: Buňka [{xIndex}][{yIndex}] je null!");
            cell = new List<GridNode<T>>(BLOCKING_FACTOR + 1);
            grid[xIndex][yIndex] = cell; // Pokus o opravu
        }

        Console.WriteLine($"Pokus o přidání bodu {data}[{x}, {y}] do buňky [{xIndex}][{yIndex}] (Aktuální velikost: {cell.Count})");

        if (cell.Count < BLOCKING_FACTOR)
        {
            cell.Add(newNode);
            Console.WriteLine($"Bod přidán do buňky [{xIndex}][{yIndex}]. Nová velikost: {cell.Count}");
        }
        else
        {
            Console.WriteLine($"Buňka [{xIndex}][{yIndex}] je plná (Velikost: {cell.Count}). Spouští se dělení...");
            var nodesToRedistribute = new List<GridNode<T>>(cell.Count + 1);
            nodesToRedistribute.AddRange(cell);
            nodesToRedistribute.Add(newNode);

            cell.Clear(); // Vyprázdní původní buňku PŘED dělením

            SplitCell(xIndex, yIndex, nodesToRedistribute);
            Console.WriteLine("Dělení dokončeno. Struktura mřížky aktualizována.");
            // Volání Save() by mělo být provedeno externě po operaci AddPoint, pokud je potřeba okamžitá perzistence.
        }
    }


    private void SplitCell(int xIndex, int yIndex, List<GridNode<T>> nodesToRedistribute)
    {
        bool splitX = drawVerticalLine;
        drawVerticalLine = !drawVerticalLine;

        nodesToRedistribute.Sort((a, b) => splitX ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        int medianListIndex = nodesToRedistribute.Count / 2;

        double medianValue;
        double lowerBound = splitX ? xLines[xIndex] : yLines[yIndex];
        double upperBound = splitX ? xLines[xIndex + 1] : yLines[yIndex + 1];

        if (nodesToRedistribute.Count > 1)
        {
            double val1 = splitX ? nodesToRedistribute[medianListIndex - 1].X : nodesToRedistribute[medianListIndex - 1].Y;
            double val2 = splitX ? nodesToRedistribute[medianListIndex].X : nodesToRedistribute[medianListIndex].Y;
            medianValue = (val1 + val2) / 2.0;

            // Zajištění, že medián je striktně mezi hranicemi buňky
            if (medianValue <= lowerBound || medianValue >= upperBound)
            {
                medianValue = lowerBound + (upperBound - lowerBound) / 2.0;
                Console.WriteLine($"Varování: Medián {medianValue} mimo hranice [{lowerBound}, {upperBound}]. Použit střed.");
            }
            // Zajištění, že medián není příliš blízko hranic (prevence numerických chyb)
            double range = upperBound - lowerBound;
            if (Math.Abs(medianValue - lowerBound) < range * 0.01) medianValue = lowerBound + range * 0.01;
            if (Math.Abs(medianValue - upperBound) < range * 0.01) medianValue = upperBound - range * 0.01;

        }
        else
        {
            // V případě jediného bodu (méně pravděpodobné) vezmeme střed buňky
            medianValue = lowerBound + (upperBound - lowerBound) / 2.0;
        }


        // Přidání nové dělící čáry a rozšíření struktury mřížky 'grid'
        if (splitX) // Vertikální dělení
        {
            Console.WriteLine($"Dělení vertikálně (osa X) na X = {medianValue}");
            xLines.Insert(xIndex + 1, medianValue);

            var newColumn = new List<List<GridNode<T>>>();
            int yDim = yLines.Count - 1;
            for (int j = 0; j < yDim; j++)
            {
                newColumn.Add(new List<GridNode<T>>(BLOCKING_FACTOR + 1));
            }
            grid.Insert(xIndex + 1, newColumn);
        }
        else // Horizontální dělení
        {
            Console.WriteLine($"Dělení horizontálně (osa Y) na Y = {medianValue}");
            yLines.Insert(yIndex + 1, medianValue);

            foreach (var column in grid)
            {
                if (column != null)
                {
                    column.Insert(yIndex + 1, new List<GridNode<T>>(BLOCKING_FACTOR + 1));
                }
                else
                {
                    // Logika pro případ chyby - null sloupec
                    Console.WriteLine($"Chyba: Pokus o vložení řádku do null sloupce!");
                }
            }
        }

        // Redistribuce uzlů
        Console.WriteLine($"Redistribuce {nodesToRedistribute.Count} uzlů...");
        foreach (var node in nodesToRedistribute)
        {
            int newXIndex = FindIndex(xLines, node.X);
            int newYIndex = FindIndex(yLines, node.Y);

            if (newXIndex >= 0 && newXIndex < grid.Count && grid[newXIndex] != null &&
                newYIndex >= 0 && newYIndex < grid[newXIndex].Count && grid[newXIndex][newYIndex] != null)
            {
                //Console.WriteLine($"  - Přesouvání uzlu {node.Data} ({node.X}, {node.Y}) do buňky [{newXIndex}][{newYIndex}]");
                grid[newXIndex][newYIndex].Add(node);
            }
            else
            {
                Console.WriteLine($"  - CHYBA: Nelze umístit uzel {node.Data} ({node.X}, {node.Y}). Cílový index [{newXIndex}][{newYIndex}] je mimo rozsah nebo buňka je null.");
            }
        }
        Console.WriteLine("Redistribuce dokončena.");
    }



    private int FindIndex(List<double> scale, double value)
    {
        // Zvláštní případ: hodnota je přesně na poslední čáře -> patří do posledního segmentu
        if (value == scale[scale.Count - 1])
        {
            return scale.Count - 2;
        }

        // Pro většinu případů - hledání intervalu [scale[i], scale[i+1])
        for (int i = 0; i < scale.Count - 1; i++)
        {
            if (value >= scale[i] && value < scale[i + 1])
            {
                return i;
            }
        }

        // Pokud je hodnota menší než první čára
        if (value < scale[0])
        {
            Console.WriteLine($"Varování: Hodnota {value} je menší než minimální hranice {scale[0]}. Vrací se index 0.");
            return 0;
        }

        // Fallback - neměl by nastat, pokud je value v rozsahu [min, max]
        Console.WriteLine($"Varování: Nepodařilo se zařadit hodnotu {value} do škály. Vrací se poslední index {scale.Count - 2}.");
        return scale.Count - 2;
    }


    public T FindPoint(double x, double y) // Návratový typ T? nelze použít bez omezení 'where T: struct' nebo 'where T: class'
    {
        int xIndex = FindIndex(xLines, x);
        int yIndex = FindIndex(yLines, y);

        if (xIndex < 0 || grid == null || xIndex >= grid.Count || grid[xIndex] == null || yIndex < 0 || yIndex >= grid[xIndex].Count || grid[xIndex][yIndex] == null)
        {
            return default(T); // Mimo mřížku nebo chyba struktury
        }

        var cell = grid[xIndex][yIndex];
        double tolerance = 1e-9;

        foreach (var node in cell)
        {
            if (Math.Abs(node.X - x) < tolerance && Math.Abs(node.Y - y) < tolerance)
            {
                return node.Data; // Nalezeno
            }
        }

        return default(T); // Nenalezeno
    }


    // Uvnitř třídy GridIndex<T>

    /// <summary>
    /// Najde všechny body v zadané obdélníkové oblasti a vrátí je
    /// spolu s informací o prohledaných buňkách mřížky.
    /// </summary>
    /// <param name="xMinQuery">Minimální X oblasti dotazu.</param>
    /// <param name="xMaxQuery">Maximální X oblasti dotazu.</param>
    /// <param name="yMinQuery">Minimální Y oblasti dotazu.</param>
    /// <param name="yMaxQuery">Maximální Y oblasti dotazu.</param>
    /// <returns>Objekt AreaSearchResult obsahující nalezené body a indexy prohledaných buněk.</returns>
    public AreaSearchResult<T> FindPointsInArea(double xMinQuery, double xMaxQuery, double yMinQuery, double yMaxQuery)
    {
        // Vytvoříme instanci pro ukládání výsledků
        var result = new AreaSearchResult<T>();

        // Základní kontroly
        if (grid == null || xLines == null || yLines == null || xLines.Count < 2 || yLines.Count < 2)
        {
            Console.WriteLine("FindPointsInArea: Mřížka není správně inicializována.");
            return result; // Vrátíme prázdný výsledek
        }

        // Určení rozsahu buněk k prohledání
        int xStartIndex = FindIndex(xLines, xMinQuery);
        int xEndIndex = FindIndex(xLines, xMaxQuery);
        int yStartIndex = FindIndex(yLines, yMinQuery);
        int yEndIndex = FindIndex(yLines, yMaxQuery);

        Console.WriteLine($"Hledání v oblasti ({xMinQuery}, {yMinQuery}) až ({xMaxQuery}, {yMaxQuery})");
        Console.WriteLine($"Prohledávají se buňky mřížky X:[{xStartIndex}-{xEndIndex}], Y:[{yStartIndex}-{yEndIndex}]");

        // Procházení relevantních buněk mřížky
        for (int i = xStartIndex; i <= xEndIndex; i++)
        {
            // Bezpečnostní kontrola indexu sloupce a null sloupce
            if (i < 0 || i >= grid.Count || grid[i] == null) continue;

            for (int j = yStartIndex; j <= yEndIndex; j++)
            {
                // Bezpečnostní kontrola indexu řádku a null buňky
                if (j < 0 || j >= grid[i].Count || grid[i][j] == null) continue;

                // === Záznam prohledávané buňky ===
                result.CheckedCellIndices.Add((i, j));
                // ===================================

                var cell = grid[i][j];
                // Procházení bodů uvnitř aktuální buňky
                foreach (var node in cell)
                {
                    // Finální ověření, zda bod skutečně leží v *přesné* dotazované oblasti
                    if (node.X >= xMinQuery && node.X <= xMaxQuery &&
                        node.Y >= yMinQuery && node.Y <= yMaxQuery)
                    {
                        result.FoundPoints.Add(node.Data);
                    }
                }
            }
        }
        // Vrátíme objekt s výsledky (nalezené body a prohledané buňky)
        return result;
    }
    public bool Save(string filePath = null)
    {
        string path = filePath ?? this.dataFilePath;
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("Chyba ukládání: Cesta k souboru není specifikována.");
            return false;
        }

        Console.WriteLine($"Ukládání stavu mřížky do '{path}'...");
        try
        {
            // Zajistí vytvoření adresáře, pokud neexistuje
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
            {
                writer.Write(1); // Verze 1

                writer.Write(xLines.Count);
                foreach (double x in xLines) writer.Write(x);

                writer.Write(yLines.Count);
                foreach (double y in yLines) writer.Write(y);

                int totalNodes = grid.Sum(column => column?.Sum(cell => cell?.Count ?? 0) ?? 0);
                writer.Write(totalNodes);

                int savedNodes = 0;
                foreach (var column in grid)
                {
                    if (column == null) continue;
                    foreach (var cell in column)
                    {
                        if (cell == null) continue;
                        foreach (var node in cell)
                        {
                            node.WriteTo(writer);
                            savedNodes++;
                        }
                    }
                }
                Console.WriteLine($"Uložení úspěšné. Struktura: {xLines.Count - 1}x{yLines.Count - 1}, Počet bodů: {savedNodes}/{totalNodes}.");
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při ukládání mřížky do {path}: {ex.ToString()}"); // Vypíše i stack trace
            return false;
        }
    }


    private bool Load()
    {
        this.IsLoadedFromFile = false; // Reset
        if (string.IsNullOrEmpty(dataFilePath)) return false;
        if (!File.Exists(dataFilePath)) return false;

        Console.WriteLine($"Pokus o načtení stavu mřížky z '{dataFilePath}'...");
        try
        {
            using (var stream = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, false))
            {
                int version = reader.ReadInt32();
                if (version != 1) throw new NotSupportedException($"Nepodporovaná verze souboru: {version}");

                int xCount = reader.ReadInt32();
                var loadedXLines = new List<double>(xCount);
                for (int i = 0; i < xCount; i++) loadedXLines.Add(reader.ReadDouble());

                int yCount = reader.ReadInt32();
                var loadedYLines = new List<double>(yCount);
                for (int i = 0; i < yCount; i++) loadedYLines.Add(reader.ReadDouble());

                this.xLines = loadedXLines;
                this.yLines = loadedYLines;

                InitializeGridStructure(); // Vytvoří prázdnou strukturu dle načtených čar

                int totalNodes = reader.ReadInt32();
                Console.WriteLine($"Načítání {totalNodes} bodů...");
                int loadedNodes = 0;
                for (int n = 0; n < totalNodes; n++)
                {
                    if (stream.Position == stream.Length)
                    {
                        throw new EndOfStreamException($"Neočekávaný konec souboru při čtení bodu {n + 1} z {totalNodes}.");
                    }
                    GridNode<T> node = GridNode<T>.ReadFrom(reader);

                    int xIndex = FindIndex(this.xLines, node.X);
                    int yIndex = FindIndex(this.yLines, node.Y);

                    if (xIndex >= 0 && xIndex < grid.Count && grid[xIndex] != null &&
                        yIndex >= 0 && yIndex < grid[xIndex].Count && grid[xIndex][yIndex] != null)
                    {
                        grid[xIndex][yIndex].Add(node);
                        loadedNodes++;
                    }
                    else
                    {
                        Console.WriteLine($"  - Varování: Nelze umístit načtený uzel {node.Data} ({node.X}, {node.Y}). Index [{xIndex}][{yIndex}] mimo rozsah/null.");
                    }
                }
                Console.WriteLine($"Načtení dokončeno. Struktura: {xLines.Count - 1}x{yLines.Count - 1}, Načteno bodů: {loadedNodes}/{totalNodes}.");
                if (loadedNodes != totalNodes) Console.WriteLine($"Varování: Počet úspěšně načtených bodů ({loadedNodes}) neodpovídá očekávanému počtu ({totalNodes}).");

                this.IsLoadedFromFile = true; // Úspěšně načteno
            }
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Chyba při načítání mřížky z {dataFilePath}: {ex.ToString()}");
            // Resetovat stav na prázdný/neplatný
            xLines = null; yLines = null; grid = null;
            this.IsLoadedFromFile = false;
            return false;
        }
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Stav GridIndex (Soubor: {dataFilePath ?? "Nezadán"}, Načteno: {IsLoadedFromFile})");

        if (grid == null || xLines == null || yLines == null || grid.Count == 0 || xLines.Count < 2 || yLines.Count < 2)
        {
            sb.AppendLine("Mřížka není inicializována nebo je nekonzistentní.");
            return sb.ToString();
        }

        int cols = xLines.Count - 1;
        int rows = yLines.Count - 1;
        int actualCols = grid.Count;
        int actualRows = (actualCols > 0 && grid[0] != null) ? grid[0].Count : 0;

        sb.AppendLine($"Rozměry dle čar: {cols} sloupců x {rows} řádků");
        if (cols != actualCols || rows != actualRows)
        {
            sb.AppendLine($"! Skutečná struktura gridu: {actualCols} sloupců x {actualRows} řádků (Nekonzistence!)");
        }
        else
        {
            sb.AppendLine($"Struktura gridu: {actualCols} sloupců x {actualRows} řádků");
        }

        sb.AppendLine("Obsah buněk (počet bodů):");
        for (int j = 0; j < actualRows; j++) // Iteruje přes řádky
        {
            for (int i = 0; i < actualCols; i++) // Iteruje přes sloupce
            {
                if (i < grid.Count && grid[i] != null && j < grid[i].Count && grid[i][j] != null)
                {
                    sb.Append($"[{grid[i][j].Count}] ");
                }
                else
                {
                    sb.Append("[N/A] "); // Buňka neexistuje nebo je null
                }
            }
            sb.Append("\n");
        }

        // Použití CultureInfo.InvariantCulture pro konzistentní formát desetinných čísel
        sb.Append("X Lines (Vertikální): ").AppendLine(string.Join(", ", xLines.Select(l => l.ToString(CultureInfo.InvariantCulture))));
        sb.Append("Y Lines (Horizontální): ").AppendLine(string.Join(", ", yLines.Select(l => l.ToString(CultureInfo.InvariantCulture))));
        return sb.ToString();
    }
}