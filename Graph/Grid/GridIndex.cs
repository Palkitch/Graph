using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Graph.Grid
{


    /// <summary>
    /// Původní, paměťově orientovaná implementace Grid Indexu.
    /// Používá se interně třídou FileGridBuilder pro dynamické vytváření
    /// struktury mřížky včetně dělení buněk před zápisem do souborů.
    /// NEPRACUJE přímo se soubory idx/dat.
    /// </summary>
    /// <typeparam name="T">Typ dat uložených v uzlech.</typeparam>
    public class GridIndex<T> // Ponecháme název, nebo přejmenujte např. na InMemoryGridIndex<T>
    {
        private int BLOCKING_FACTOR = 3; // Výchozí, lze přepsat konstruktorem

        private List<double> xLines; // Vertikální dělící čáry
        private List<double> yLines; // Horizontální dělící čáry
        private List<List<List<GridNode<T>>>> grid; // Vnořená struktura buněk v paměti
        private bool drawVerticalLine = true; // Pro střídání směru dělení

        /// <summary>
        /// Konstruktor pro použití v Builderu nebo pro čistě paměťové použití.
        /// </summary>
        /// <param name="blockingFactor">Maximální počet prvků v buňce před dělením.</param>
        /// <param name="xMin">Minimální X souřadnice.</param>
        /// <param name="xMax">Maximální X souřadnice.</param>
        /// <param name="yMin">Minimální Y souřadnice.</param>
        /// <param name="yMax">Maximální Y souřadnice.</param>
        public GridIndex(int blockingFactor, double xMin, double xMax, double yMin, double yMax)
        {
            if (blockingFactor <= 0) throw new ArgumentOutOfRangeException(nameof(blockingFactor), "Blocking factor musí být kladný.");
            if (xMin >= xMax || yMin >= yMax) throw new ArgumentException("Neplatné hranice: Min musí být menší než Max.");

            BLOCKING_FACTOR = blockingFactor;
            xLines = new List<double> { xMin, xMax };
            yLines = new List<double> { yMin, yMax };
            InitializeGridStructure();
        }

        /// <summary>
        /// Inicializuje prázdnou strukturu vnitřní mřížky v paměti.
        /// </summary>
        private void InitializeGridStructure()
        {
            grid = new List<List<List<GridNode<T>>>>();
            int xDim = Math.Max(1, xLines.Count - 1);
            int yDim = Math.Max(1, yLines.Count - 1);
            //Console.WriteLine($"MEMORY GridIndex: Inicializace {xDim}x{yDim}");
            for (int i = 0; i < xDim; i++)
            {
                var column = new List<List<GridNode<T>>>(yDim);
                for (int j = 0; j < yDim; j++)
                {
                    // Přidá prázdnou buňku s adekvátní počáteční kapacitou
                    column.Add(new List<GridNode<T>>(BLOCKING_FACTOR + 1));
                }
                grid.Add(column);
            }
        }

        /// <summary>
        /// Přidá bod do paměťové mřížky. Pokud je buňka plná, zavolá SplitCell.
        /// </summary>
        public void AddPoint(T data, double x, double y)
        {
            // Kontrola hranic
            if (xLines == null || yLines == null || x < xLines[0] || x > xLines[xLines.Count - 1] || y < yLines[0] || y > yLines[yLines.Count - 1])
            {
                return;
            }

            int xIndex = FindIndex(xLines, x);
            int yIndex = FindIndex(yLines, y);

            // Kontrola platnosti indexů a struktury
            if (xIndex < 0 || yIndex < 0 || grid == null || xIndex >= grid.Count || grid[xIndex] == null || yIndex >= grid[xIndex].Count)
            {
                Console.WriteLine($"MEMORY AddPoint: Chyba - Neplatný index [{xIndex}][{yIndex}] pro ({x},{y}). Rozměry gridu: {grid?.Count}x{(grid?.Count > 0 ? grid[0]?.Count ?? -1 : -1)}");
                return;
            }

            var newNode = new GridNode<T>(data, x, y);
            var cell = grid[xIndex][yIndex];

            // Pojistka pro případ null buňky (nemělo by nastat)
            if (cell == null)
            {
                Console.WriteLine($"MEMORY AddPoint: Varování - Buňka [{xIndex}][{yIndex}] byla null. Vytvářím novou.");
                cell = new List<GridNode<T>>(BLOCKING_FACTOR + 1);
                grid[xIndex][yIndex] = cell;
            }

            // Přidání nebo dělení
            if (cell.Count < BLOCKING_FACTOR)
            {
                cell.Add(newNode);
            }
            else
            {
                // Shromáždíme body pro dělení
                var nodesToRedistribute = new List<GridNode<T>>(cell.Count + 1);
                nodesToRedistribute.AddRange(cell);
                nodesToRedistribute.Add(newNode);
                cell.Clear(); // Vyčistíme původní buňku

                // Zavoláme dělení (to upraví xLines, yLines a grid)
                SplitCell(xIndex, yIndex, nodesToRedistribute);
            }
        }

        /// <summary>
        /// Rozdělí buňku v paměti, upraví čáry a strukturu gridu, redistribuuje body.
        /// </summary>
        private void SplitCell(int xIndex, int yIndex, List<GridNode<T>> nodesToRedistribute)
        {
            bool splitX = drawVerticalLine;
            drawVerticalLine = !drawVerticalLine; // Střídáme směr

            // Seřadíme podle relevantní osy
            nodesToRedistribute.Sort((a, b) => splitX ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            int medianListIndex = nodesToRedistribute.Count / 2;

            // Vypočítáme medián (pozici nové čáry)
            double medianValue;
            double lowerBound = splitX ? xLines[xIndex] : yLines[yIndex];
            double upperBound = splitX ? xLines[xIndex + 1] : yLines[yIndex + 1];

            // (Stejná logika výpočtu medianValue jako v předchozích verzích)
            if (nodesToRedistribute.Count > 1)
            {
                double val1 = splitX ? nodesToRedistribute[medianListIndex - 1].X : nodesToRedistribute[medianListIndex - 1].Y;
                double val2 = splitX ? nodesToRedistribute[medianListIndex].X : nodesToRedistribute[medianListIndex].Y;
                medianValue = (val1 + val2) / 2.0;
                // Korekce, aby medián byl platný a ne příliš blízko hranic
                if (medianValue <= lowerBound || medianValue >= upperBound) { medianValue = lowerBound + (upperBound - lowerBound) / 2.0; }
                double range = upperBound - lowerBound;
                if (range > 1e-9)
                { // Check for very small range
                    double tolerance = range * 0.001; // Use a small tolerance relative to range
                    if (medianValue - lowerBound < tolerance) medianValue = lowerBound + tolerance;
                    if (upperBound - medianValue < tolerance) medianValue = upperBound - tolerance;
                }
                else { medianValue = lowerBound + range / 2.0; } // Avoid issues if range is zero/tiny
            }
            else { medianValue = lowerBound + (upperBound - lowerBound) / 2.0; } // Fallback pro 1 bod


            // Aktualizace čar a struktury gridu v RAM
            if (splitX)
            {
                //Console.WriteLine($"MEMORY Split X na {medianValue}");
                xLines.Insert(xIndex + 1, medianValue);
                var newColumn = new List<List<GridNode<T>>>(yLines.Count - 1);
                for (int j = 0; j < yLines.Count - 1; j++) newColumn.Add(new List<GridNode<T>>(BLOCKING_FACTOR + 1));
                grid.Insert(xIndex + 1, newColumn); // Vložíme nový sloupec do gridu
            }
            else // splitY
            {
                //Console.WriteLine($"MEMORY Split Y na {medianValue}");
                yLines.Insert(yIndex + 1, medianValue);
                // Vložíme nový řádek (buňku) do každého sloupce
                foreach (var column in grid)
                {
                    if (column != null)
                    { // Safety check
                      // Zajistíme, že vkládáme na správné místo, i když sloupec už mohl být delší
                        while (column.Count <= yIndex + 1)
                        {
                            column.Add(new List<GridNode<T>>(BLOCKING_FACTOR + 1)); // Případně doplníme chybějící buňky
                        }
                        column.Insert(yIndex + 1, new List<GridNode<T>>(BLOCKING_FACTOR + 1));
                    }
                }
            }

            // Redistribuce bodů do (nyní větší) struktury gridu v RAM
            foreach (var node in nodesToRedistribute)
            {
                int newXIndex = FindIndex(xLines, node.X); // Najdeme nové indexy
                int newYIndex = FindIndex(yLines, node.Y);

                // Ověříme platnost nových indexů a existence buňky
                if (newXIndex >= 0 && newXIndex < grid.Count && grid[newXIndex] != null &&
                    newYIndex >= 0 && newYIndex < grid[newXIndex].Count)
                {
                    if (grid[newXIndex][newYIndex] == null) // Pojistka pro null buňku
                    {
                        grid[newXIndex][newYIndex] = new List<GridNode<T>>(BLOCKING_FACTOR + 1);
                    }
                    grid[newXIndex][newYIndex].Add(node); // Přidáme bod do správné buňky
                }
                else
                {
                    Console.WriteLine($"MEMORY Split: Chyba redistribuce bodu {node.Data} ({node.X},{node.Y}). Cílový index [{newXIndex}][{newYIndex}] mimo rozsah.");
                }
            }
        }

        /// <summary>
        /// Najde index segmentu (buňky) pro danou hodnotu (souřadnici).
        /// </summary>
        private int FindIndex(IReadOnlyList<double> scale, double value)
        {
            if (scale == null || scale.Count < 2) return -1;

            // Ošetření přesné shody s horní hranicí
            if (value == scale[scale.Count - 1])
            {
                return scale.Count - 2; // Patří do posledního intervalu
            }

            // Hledání intervalu [scale[i], scale[i+1])
            for (int i = 0; i < scale.Count - 1; i++)
            {
                if (value >= scale[i] && value < scale[i + 1])
                {
                    return i;
                }
            }

            // Hodnota je mimo definované hranice
            if (value < scale[0]) return 0; // Menší než minimum, zařadíme do první buňky
                                            // Větší než maximum (případ == max je ošetřen výše)
            return scale.Count - 2; // Větší než maximum, zařadíme do poslední buňky
        }

        // --- Metody pro přístup k finální struktuře (pro Builder) ---

        /// <summary>
        /// (Pro použití Builderem) Vrací referenci na vnitřní strukturu mřížky.
        /// </summary>
        internal List<List<List<GridNode<T>>>> GetInternalGridStructure()
        {
            return grid;
        }

        /// <summary>
        /// (Pro použití Builderem) Vrací finální seznam X-ových čar.
        /// </summary>
        internal IReadOnlyList<double> GetXLines() => xLines?.AsReadOnly();

        /// <summary>
        /// (Pro použití Builderem) Vrací finální seznam Y-ových čar.
        /// </summary>
        internal IReadOnlyList<double> GetYLines() => yLines?.AsReadOnly();


        // --- Volitelné: Metody pro vyhledávání a výpis (pro ladění builderu) ---

        // Uvnitř třídy GridIndex<T> (paměťové verze)

        /// <summary>
        /// Najde konkrétní uzel (GridNode) v paměťové mřížce na přesných souřadnicích (s tolerancí).
        /// (Primárně pro ladění builderu nebo paměťové použití).
        /// </summary>
        /// <returns>Nalezený GridNode<T> nebo null, pokud nebyl nalezen nebo indexy jsou neplatné.</returns>
        public GridNode<T> FindPoint(double x, double y)
        {
            // Nejprve základní kontrola existence struktury
            if (xLines == null || yLines == null || grid == null)
            {
                return null; // Mřížka není inicializována
            }

            // Najdeme indexy
            int xIndex = FindIndex(xLines, x);
            int yIndex = FindIndex(yLines, y);

            // --- Zde je detailní podmínka ---
            if (xIndex < 0 || xIndex >= grid.Count || // Je xIndex platný pro počet sloupců?
                grid[xIndex] == null ||               // Existuje sloupec na tomto indexu?
                yIndex < 0 || yIndex >= grid[xIndex].Count || // Je yIndex platný pro počet řádků v daném sloupci?
                grid[xIndex][yIndex] == null)         // Existuje buňka (seznam) na této pozici?
            {
                // Pokud některá z podmínek neplatí, index je neplatný nebo část struktury chybí
                return null; // Bod nemůže být nalezen
            }
            // --- Konec podmínky ---

            // Pokud jsme se dostali sem, grid[xIndex][yIndex] je platný a můžeme k němu přistoupit
            var cell = grid[xIndex][yIndex];
            double tolerance = 1e-9;

            // Prohledáme body v nalezené buňce
            foreach (var node in cell)
            {
                if (Math.Abs(node.X - x) < tolerance && Math.Abs(node.Y - y) < tolerance)
                {
                    return node; // Nalezeno - vracíme celý uzel
                }
            }

            return null; // Nenalezeno v buňce
        }

        // Uvnitř třídy GridIndex<T> (PŮVODNÍ PAMĚŤOVÁ VERZE)

        /// <summary>
        /// Najde všechny body v oblasti v paměťové mřížce.
        /// Upraveno, aby vracelo AreaSearchResult<T> kompatibilní s FileGridIndex,
        /// ale s fiktivním offsetem -1.
        /// </summary>
        /// <returns>AreaSearchResult obsahující nalezené uzly a indexy prohledaných buněk.</returns>
        public AreaSearchResult<T> FindPointsInArea(double xMinQuery, double xMaxQuery, double yMinQuery, double yMaxQuery)
        {
            // Použije definici AreaSearchResult s tuplem: List<(Node, X, Y, Offset)>
            var result = new AreaSearchResult<T>();

            if (grid == null || xLines == null || yLines == null || xLines.Count < 2 || yLines.Count < 2)
            {
                // Console.WriteLine("InMemory GridIndex: FindPointsInArea - Mřížka není inicializována.");
                return result;
            }

            // Výpočet indexů buněk k prohledání
            int xStartIndex = FindIndex(xLines, xMinQuery);
            int xEndIndex = FindIndex(xLines, xMaxQuery);
            int yStartIndex = FindIndex(yLines, yMinQuery);
            int yEndIndex = FindIndex(yLines, yMaxQuery);

            // Ošetření neplatných indexů (pokud FindIndex vrátí -1)
            if (xStartIndex < 0 || xEndIndex < 0 || yStartIndex < 0 || yEndIndex < 0) return result;


            // Procházení relevantních buněk v paměti
            for (int i = xStartIndex; i <= xEndIndex; i++)
            {
                // Kontrola mezí a null (robustnější)
                if (i >= grid.Count || grid[i] == null) continue;

                for (int j = yStartIndex; j <= yEndIndex; j++)
                {
                    // Kontrola mezí a null (robustnější)
                    if (j >= grid[i].Count || grid[i][j] == null) continue;

                    result.CheckedCellIndices.Add((i, j)); // Zaznamenáme kontrolu buňky
                    var cell = grid[i][j]; // Získáme List<GridNode<T>>

                    // Procházení bodů uvnitř buňky
                    foreach (var node in cell)
                    {
                        // Přesná kontrola souřadnic vůči hledané oblasti
                        if (node.X >= xMinQuery && node.X <= xMaxQuery &&
                            node.Y >= yMinQuery && node.Y <= yMaxQuery)
                        {
                            // --- !!! ZDE JE OPRAVA !!! ---
                            // Přidáme tuple (Node, XIndex, YIndex, Offset)
                            // Protože jsme v paměti a nemáme Offset, použijeme -1L.
                            result.FoundPoints.Add((node, i, j, -1L));
                            // -----------------------------
                        }
                    }
                } // Konec vnitřní smyčky j
            } // Konec vnější smyčky i
            return result;
        }

        public override string ToString()
        {
            // ... (Implementace jako dříve, vypisuje stav paměťové mřížky) ...
            var sb = new StringBuilder();
            sb.AppendLine($"Stav InMemory GridIndex (BF={BLOCKING_FACTOR})");
            if (grid == null || xLines == null || yLines == null) { /*...*/ return sb.ToString(); }
            int cols = xLines.Count - 1; int rows = yLines.Count - 1;
            int actualCols = grid.Count; int actualRows = actualCols > 0 && grid[0] != null ? grid[0].Count : 0;
            sb.AppendLine($"Rozměry: {cols}x{rows} (dle čar), {actualCols}x{actualRows} (skutečné)");
            // ... (výpis počtu bodů v buňkách) ...
            for (int j = 0; j < actualRows; j++)
            {
                for (int i = 0; i < actualCols; i++)
                {
                    sb.Append($"[{(grid[i] != null && j < grid[i].Count && grid[i][j] != null ? grid[i][j].Count : "N/A")}] ");
                }
                sb.AppendLine();
            }
            sb.Append("X Lines: ").AppendLine(string.Join(", ", xLines.Select(l => l.ToString(CultureInfo.InvariantCulture))));
            sb.Append("Y Lines: ").AppendLine(string.Join(", ", yLines.Select(l => l.ToString(CultureInfo.InvariantCulture))));
            return sb.ToString();
        }

    } // Konec třídy GridIndex<T> (paměťové)
}