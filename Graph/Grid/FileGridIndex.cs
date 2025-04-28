
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;

// Namespace, kde máte ostatní definice (GridNode, Serializer atd.)
// Upravte podle potřeby
using Graph;

// Předpokládá existenci v namespace Graph (nebo jiném dostupném):
// - interface IFixedSizeSerializer<T>
// - třídy GridNode<T> s upravenými WriteTo/ReadFrom přijímajícími IFixedSizeSerializer<T>
// - třídy AreaSearchResult<T> s upraveným List<(Node, X, Y, Offset)> FoundPoints
// - struct CellInfo

/// <summary>
/// Blokově orientovaný Grid Index s bloky FIXNÍ velikosti (pomocí paddingu).
/// Používá IFixedSizeSerializer<T> pro zajištění fixní velikosti dat T.
/// Podporuje Add (s hybridním splitem), Delete, Find*.
/// Update/Delete přepisují na místě, Split přidává nové bloky na konec.
/// Vyžaduje PŘEDEM VYTVOŘENÉ soubory .idx a .dat pomocí FileGridBuilder<T>.
/// </summary>
public class FileGridIndex<T> : IDisposable
{
    // --- Konstanty a Fields ---
    private readonly string indexFilePath;
    private readonly string dataFilePath;
    private readonly int BLOCKING_FACTOR;
    private readonly IFixedSizeSerializer<T> dataSerializer;
    private readonly int FixedNodeSize;
    private readonly int FixedBlockDataSize;
    private readonly object fileLock = new object(); // Zámek pro synchronizaci I/O

    private List<double> xLines;
    private List<double> yLines;
    private List<List<CellInfo>> cellIndex; // Metadata v RAM

    private bool isIndexDirty = false;
    private bool drawVerticalLine = true; // Pro střídání směru splitu

    // --- Properties ---
    public bool IsLoaded { get; private set; } = false;
    public string IndexFilePath => indexFilePath;
    public string DataFilePath => dataFilePath;
    public IReadOnlyList<double> XLines => xLines?.AsReadOnly();
    public IReadOnlyList<double> YLines => yLines?.AsReadOnly();

    // --- Konstruktor ---
    public FileGridIndex(string indexFilePath, string dataFilePath, int blockingFactor,
                         double xMin, double xMax, double yMin, double yMax,
                         IFixedSizeSerializer<T> serializer)
    {
        if (serializer == null) throw new ArgumentNullException(nameof(serializer));
        if (blockingFactor <= 0) throw new ArgumentOutOfRangeException(nameof(blockingFactor));
        this.indexFilePath = indexFilePath ?? throw new ArgumentNullException(nameof(indexFilePath));
        this.dataFilePath = dataFilePath ?? throw new ArgumentNullException(nameof(dataFilePath));

        this.dataSerializer = serializer;
        this.BLOCKING_FACTOR = blockingFactor;
        try { this.FixedNodeSize = GridNode<T>.GetFixedNodeSize(serializer); }
        catch (Exception ex) { throw new InvalidOperationException("Chyba získání velikosti nodu.", ex); }
        this.FixedBlockDataSize = Math.Max(1, this.BLOCKING_FACTOR) * this.FixedNodeSize;
        if (this.FixedBlockDataSize <= 0) throw new InvalidOperationException("Vypočtená velikost bloku dat musí být kladná.");

        if (!LoadIndex())
        {
            Console.WriteLine($"Index nenalezen/neplatný. Vytvářím prázdnou strukturu: {indexFilePath}");
            if (xMin >= xMax || yMin >= yMax) throw new ArgumentException("Neplatné hranice.");
            xLines = new List<double> { xMin, xMax }; yLines = new List<double> { yMin, yMax };
            InitializeCellIndexAndDataFile();
            isIndexDirty = true; IsLoaded = true;
            if (!SaveIndex()) { throw new IOException($"Nepodařilo se uložit nový index {indexFilePath}"); }
        }
        else if (!File.Exists(dataFilePath))
        {
            Console.WriteLine($"Varování: Datový soubor '{dataFilePath}' chybí. Vytvářím prázdný.");
            if (!CreateEmptyDataFileWithSlots()) { IsLoaded = false; }
        }
        else { CheckDataFileSize(); }
    }

    // --- Interní Metody (Inicializace, Load/Save Index, Load/Write/Append Blok, Split) ---

    private void InitializeCellIndex()
    {
        int xDim = Math.Max(1, xLines.Count - 1); int yDim = Math.Max(1, yLines.Count - 1);
        cellIndex = new List<List<CellInfo>>(xDim);
        for (int i = 0; i < xDim; i++)
        {
            var col = new List<CellInfo>(yDim);
            for (int j = 0; j < yDim; j++)
            {
                long offset = CalculateExpectedOffset(i, j, yDim);
                col.Add(new CellInfo { Offset = offset, Length = FixedBlockDataSize, PointCount = 0 });
            }
            cellIndex.Add(col);
        }
        Console.WriteLine($"Interní index {xDim}x{yDim} inicializován v RAM.");
    }

    private long CalculateExpectedOffset(int i, int j, int yDimension)
    {
        // Ukládání po sloupcích
        return (long)i * yDimension * FixedBlockDataSize + (long)j * FixedBlockDataSize;
    }

    private bool CreateEmptyDataFileWithSlots()
    {
        if (xLines == null || yLines == null || cellIndex == null) return false;
        long totalSlots = (long)(xLines.Count - 1) * (yLines.Count - 1); if (totalSlots <= 0) return true;
        long requiredSize = totalSlots * FixedBlockDataSize; Console.WriteLine($"Alokuji '{dataFilePath}' ({requiredSize} B)...");
        try { EnsureDirectoryExists(dataFilePath); lock (fileLock) { using (var fs = new FileStream(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.None)) { fs.SetLength(requiredSize); } } return true; }
        catch (Exception ex) { Console.WriteLine($"Chyba alokace .dat: {ex.Message}"); return false; }
    }

    private void InitializeCellIndexAndDataFile()
    {
        InitializeCellIndex();
        if (!CreateEmptyDataFileWithSlots())
        {
            throw new IOException($"Nepodařilo se alokovat {dataFilePath}");
        }
    }

    private void CheckDataFileSize()
    {
        if (xLines == null || yLines == null || cellIndex == null) return;
        try
        {
            long expectedSize = (long)(xLines.Count - 1) * (yLines.Count - 1) * FixedBlockDataSize; if (expectedSize < 0) expectedSize = 0;
            long actualSize; lock (fileLock) { if (!File.Exists(dataFilePath)) return; using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) { actualSize = fs.Length; } }
            if (actualSize < expectedSize) { Console.WriteLine($"Varování: Velikost .dat ({actualSize} B) < očekávaná ({expectedSize} B)!"); }
        }
        catch (Exception ex) { Console.WriteLine($"Chyba kontroly velikosti .dat: {ex.Message}"); }
    }

    private bool LoadIndex()
    {
        IsLoaded = false; if (!File.Exists(indexFilePath)) return false;
        Console.WriteLine($"Načítám index '{indexFilePath}'...");
        try
        {
            lock (fileLock)
            {
                using (var s = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) using (var r = new BinaryReader(s, Encoding.UTF8, false))
                {
                    int v = r.ReadInt32(); if (v != 1) throw new NotSupportedException($"Verze {v}");
                    int xc = r.ReadInt32(); if (xc < 2) throw new InvalidDataException("Min 2 X"); xLines = new List<double>(xc); for (int i = 0; i < xc; i++) xLines.Add(r.ReadDouble());
                    int yc = r.ReadInt32(); if (yc < 2) throw new InvalidDataException("Min 2 Y"); yLines = new List<double>(yc); for (int i = 0; i < yc; i++) yLines.Add(r.ReadDouble());
                    int xd = xc - 1, yd = yc - 1; long eb = (long)xd * yd * CellInfo.SizeInBytes; long rb = r.BaseStream.Length - r.BaseStream.Position; if (rb < eb) throw new EndOfStreamException($"Málo dat pro {xd}x{yd} cell info.");
                    cellIndex = new List<List<CellInfo>>(xd); for (int i = 0; i < xd; i++) { var col = new List<CellInfo>(yd); for (int j = 0; j < yd; j++) col.Add(CellInfo.ReadFrom(r)); cellIndex.Add(col); }
                }
            }
            IsLoaded = true; isIndexDirty = false; Console.WriteLine($"Index načten. Rozměry: {xLines.Count - 1}x{yLines.Count - 1}"); return true;
        }
        catch (Exception ex) { Console.WriteLine($"Chyba načítání indexu: {ex}"); xLines = null; yLines = null; cellIndex = null; IsLoaded = false; return false; }
    }

    public bool SaveIndex()
    {
        if (!IsLoaded || !isIndexDirty) return true;
        if (xLines == null || yLines == null || cellIndex == null) { Console.WriteLine("Chyba SaveIndex: Není co uložit."); return false; }
        Console.WriteLine($"Ukládám index '{indexFilePath}'..."); string temp = indexFilePath + ".tmp";
        try
        {
            EnsureDirectoryExists(indexFilePath); lock (fileLock)
            {
                using (var s = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) using (var w = new BinaryWriter(s, Encoding.UTF8, false))
                {
                    w.Write(1); w.Write(xLines.Count); foreach (var x in xLines) w.Write(x); w.Write(yLines.Count); foreach (var y in yLines) w.Write(y);
                    int xd = xLines.Count - 1; int yd = yLines.Count - 1; if (cellIndex.Count != xd) throw new InvalidOperationException($"X mismatch {cellIndex.Count} vs {xd}");
                    for (int i = 0; i < xd; i++) { if (cellIndex[i] == null || cellIndex[i].Count != yd) throw new InvalidOperationException($"Y mismatch col {i}: {cellIndex[i]?.Count} vs {yd}"); for (int j = 0; j < yd; j++) cellIndex[i][j].WriteTo(w); }
                }
                if (File.Exists(indexFilePath)) File.Replace(temp, indexFilePath, indexFilePath + ".bak", true); else File.Move(temp, indexFilePath); if (File.Exists(indexFilePath + ".bak")) try { File.Delete(indexFilePath + ".bak"); } catch { }
            }
            isIndexDirty = false; Console.WriteLine("Index uložen."); return true;
        }
        catch (Exception ex) { Console.WriteLine($"Chyba uložení indexu: {ex}"); try { if (File.Exists(temp)) File.Delete(temp); } catch { } return false; }
    }

    private List<GridNode<T>> LoadBlock(int i, int j)
    {
        if (!IsLoaded || cellIndex == null || i < 0 || i >= cellIndex.Count || cellIndex[i] == null || j < 0 || j >= cellIndex[i].Count) return new List<GridNode<T>>();
        CellInfo info = cellIndex[i][j]; if (info.PointCount <= 0 || info.Offset < 0) return new List<GridNode<T>>();
        var blockData = new List<GridNode<T>>(info.PointCount);
        try
        {
            lock (fileLock)
            {
                if (!File.Exists(dataFilePath)) return blockData;
                using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)) using (var reader = new BinaryReader(fs, Encoding.UTF8, false))
                {
                    long expectedEnd = info.Offset + (long)info.PointCount * FixedNodeSize;
                    if (info.Offset >= fs.Length || expectedEnd > fs.Length) { Console.WriteLine($"Chyba LoadBlock Offset/Length [{i}][{j}]"); return blockData; }
                    fs.Seek(info.Offset, SeekOrigin.Begin);
                    for (int k = 0; k < info.PointCount; k++)
                    {
                        GridNode<T> node = GridNode<T>.ReadFrom(reader, dataSerializer);
                        if (node != null) blockData.Add(node); else { Console.WriteLine($"Chyba čtení uzlu {k + 1} v [{i}][{j}]"); break; }
                    }
                }
            }
        }
        catch (Exception ex) { Console.WriteLine($"Chyba čtení bloku [{i}][{j}]: {ex.Message}"); blockData.Clear(); }
        return blockData;
    }

    private bool WriteBlockInPlace(int i, int j, List<GridNode<T>> blockData)
    {
        if (!IsLoaded || i < 0 || j < 0 || i >= cellIndex.Count || j >= cellIndex[i].Count) return false;
        CellInfo info = cellIndex[i][j]; if (info.Offset < 0) { Console.WriteLine($"Chyba WriteInPlace: Neplatný offset [{i}][{j}]"); return false; }
        int newPointCount = blockData?.Count ?? 0; if (newPointCount > BLOCKING_FACTOR) { Console.WriteLine($"Chyba WriteInPlace: Přetečení BF [{i}][{j}] ({newPointCount})"); return false; }

        byte[] blockBuffer = new byte[FixedBlockDataSize]; // Padding buffer
        try
        {
            using (MemoryStream ms = new MemoryStream(blockBuffer)) using (BinaryWriter tempWriter = new BinaryWriter(ms))
            {
                if (newPointCount > 0) foreach (var node in blockData) node.WriteTo(tempWriter, dataSerializer);
            } // blockBuffer je nyní naplněn daty a paddingem

            lock (fileLock)
            {
                if (!File.Exists(dataFilePath)) return false;
                using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    if (info.Offset + FixedBlockDataSize > fs.Length) { Console.WriteLine($"Chyba WriteInPlace: Offset+Délka [{i}][{j}] mimo soubor."); return false; }
                    fs.Seek(info.Offset, SeekOrigin.Begin);
                    fs.Write(blockBuffer, 0, FixedBlockDataSize); // Přepíše celý slot
                }
            }
            cellIndex[i][j] = new CellInfo { Offset = info.Offset, Length = FixedBlockDataSize, PointCount = newPointCount };
            isIndexDirty = true;
            return true;
        }
        catch (Exception ex) { Console.WriteLine($"Chyba PŘEPISU bloku [{i}][{j}]: {ex}"); return false; }
    }

    private CellInfo AppendBlock(List<GridNode<T>> blockData)
    {
        int pointCount = blockData?.Count ?? 0; if (pointCount > BLOCKING_FACTOR) { Console.WriteLine($"Chyba AppendBlock: Příliš mnoho bodů ({pointCount})"); return CellInfo.Empty; }
        byte[] blockBuffer = new byte[FixedBlockDataSize]; long newOffset = -1;
        try
        {
            using (MemoryStream ms = new MemoryStream(blockBuffer)) using (BinaryWriter tempWriter = new BinaryWriter(ms))
            {
                if (pointCount > 0) foreach (var node in blockData) node.WriteTo(tempWriter, dataSerializer);
            }
            lock (fileLock)
            {
                EnsureDirectoryExists(dataFilePath);
                using (var fs = new FileStream(dataFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    newOffset = fs.Position; // Pozice před zápisem = nový offset
                    fs.Write(blockBuffer, 0, FixedBlockDataSize); // Zapíše celý blok
                }
            }
            Console.WriteLine($" -> Blok APPEND na Offset={newOffset}, Počet={pointCount}");
            return new CellInfo { Offset = newOffset, Length = FixedBlockDataSize, PointCount = pointCount };
        }
        catch (Exception ex) { Console.WriteLine($"Chyba APPEND bloku: {ex}"); return CellInfo.Empty; }
    }

    private int FindIndex(IReadOnlyList<double> scale, double value)
    {
        if (scale == null || scale.Count < 2) return -1; if (value == scale[scale.Count - 1]) return scale.Count - 2;
        for (int i = 0; i < scale.Count - 1; i++) if (value >= scale[i] && value < scale[i + 1]) return i;
        if (value < scale[0]) return 0; return scale.Count - 2;
    }

    private bool SplitCellAndAppend(int xIndex, int yIndex, List<GridNode<T>> nodesToRedistribute)
    {
        // --- Kritická sekce - celá operace splitu musí být atomická z pohledu RAM struktur ---
        // fileLock chrání I/O, ale potřebujeme konzistenci i v RAM

        bool splitX = drawVerticalLine; drawVerticalLine = !drawVerticalLine;
        nodesToRedistribute.Sort((a, b) => splitX ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
        double medianValue;
        // --- Výpočet medianValue ---
        { double l = splitX ? xLines[xIndex] : yLines[yIndex]; double u = splitX ? xLines[xIndex + 1] : yLines[yIndex + 1]; if (nodesToRedistribute.Count > 1) { double v1 = splitX ? nodesToRedistribute[nodesToRedistribute.Count / 2 - 1].X : nodesToRedistribute[nodesToRedistribute.Count / 2 - 1].Y; double v2 = splitX ? nodesToRedistribute[nodesToRedistribute.Count / 2].X : nodesToRedistribute[nodesToRedistribute.Count / 2].Y; medianValue = (v1 + v2) / 2.0; double r = u - l; if (medianValue <= l || medianValue >= u) medianValue = l + r / 2.0; if (r > 1e-9) { double tol = r * 0.001; if (medianValue - l < tol) medianValue = l + tol; if (u - medianValue < tol) medianValue = u - tol; } else { medianValue = l + r / 2.0; } } else { medianValue = l + (u - l) / 2.0; } }

        isIndexDirty = true;
        int oldYDim = yLines.Count - 1;

        // 1. Aktualizace čar a struktury cellIndex v RAM
        if (splitX)
        {
            Console.WriteLine($"Split X na {medianValue}"); xLines.Insert(xIndex + 1, medianValue);
            var newColumnCells = new List<CellInfo>(yLines.Count - 1); for (int j = 0; j < yLines.Count - 1; j++) newColumnCells.Add(CellInfo.Empty);
            cellIndex.Insert(xIndex + 1, newColumnCells);
        }
        else
        {
            Console.WriteLine($"Split Y na {medianValue}"); yLines.Insert(yIndex + 1, medianValue);
            foreach (var column in cellIndex) column.Insert(yIndex + 1, CellInfo.Empty);
        }

        // 2. Redistribuce bodů do dočasné mapy
        Dictionary<(int, int), List<GridNode<T>>> newBlocksData = new Dictionary<(int, int), List<GridNode<T>>>();
        foreach (var node in nodesToRedistribute)
        {
            int ni = FindIndex(xLines, (double)node.X); int nj = FindIndex(yLines, (double)node.Y); // Použijeme int souřadnice
            if (ni < 0 || nj < 0 || ni >= cellIndex.Count || nj >= cellIndex[ni].Count) { Console.WriteLine($"Chyba Split-Redistr: Bod {node.Data} mimo grid?"); continue; }
            var key = (ni, nj);
            if (!newBlocksData.TryGetValue(key, out var list)) { list = new List<GridNode<T>>(); newBlocksData[key] = list; }
            if (list.Count >= BLOCKING_FACTOR) { Console.WriteLine($"!!! Chyba Split: Buňka [{ni}][{nj}] by přetekla při redistribuci !!!"); continue; } // Kontrola BF
            list.Add(node);
        }

        // 3. Zápis NOVÝCH bloků na KONEC .dat a aktualizace cellIndex v RAM
        bool writeOk = true;
        // Projdeme všechny buňky ovlivněné splitem (klíče v mapě)
        foreach (var kvp in newBlocksData)
        {
            var cellCoords = kvp.Key; var nodes = kvp.Value;
            CellInfo newInfo = AppendBlock(nodes); // Přidá na konec .dat
            if (newInfo.Offset != -1)
            {
                if (cellCoords.Item1 < cellIndex.Count && cellCoords.Item2 < cellIndex[cellCoords.Item1].Count)
                {
                    cellIndex[cellCoords.Item1][cellCoords.Item2] = newInfo; // Aktualizujeme RAM index
                }
                else { Console.WriteLine($"Chyba Split: Index [{cellCoords.Item1}][{cellCoords.Item2}] mimo rozsah!"); writeOk = false; break; }
            }
            else { Console.WriteLine($"Chyba Split: Zápis bloku pro [{cellCoords.Item1}][{cellCoords.Item2}] selhal!"); writeOk = false; break; }
        }

        // 4. Označit původní buňku jako prázdnou V RAM indexu, pokud nebyla přepsána
        //    (append by měl vždy vytvořit nový záznam pro dané souřadnice)
        if (writeOk && xIndex < cellIndex.Count && yIndex < cellIndex[xIndex].Count && !newBlocksData.ContainsKey((xIndex, yIndex)))
        {
            cellIndex[xIndex][yIndex] = CellInfo.Empty;
            Console.WriteLine($" -> Split: Původní buňka [{xIndex}][{yIndex}] označena jako prázdná.");
        }


        Console.WriteLine($"Split dokončen {(writeOk ? "úspěšně" : "s chybami")}. Nové rozměry: {xLines.Count - 1}x{yLines.Count - 1}");
        return writeOk;
    }

    // --- Veřejné Metody ---

    public bool AddPoint(T data, int x, int y)
    {
        if (!IsLoaded) { Console.WriteLine("AddPoint: Index není načten."); return false; }
        if (data == null) throw new ArgumentNullException(nameof(data));
        // VALIDACE/OŘÍZNUTÍ DAT 'data'

        int i = FindIndex(xLines, (double)x); int j = FindIndex(yLines, (double)y);
        if (i < 0 || j < 0 || i >= cellIndex.Count || j >= cellIndex[i].Count) { Console.WriteLine($"AddPoint: Bod ({x},{y}) mimo hranice."); return false; }

        bool success = false;
        lock (fileLock)
        {
            CellInfo info = cellIndex[i][j];
            if (info.Offset < 0 || info.PointCount == 0) // Prázdná/nealokovaná buňka
            {
                Console.WriteLine($"AddPoint: Buňka [{i}][{j}] je prázdná. Přidávám na konec .dat...");
                List<GridNode<T>> blockData = new List<GridNode<T>>();
                blockData.Add(new GridNode<T>(data, x, y));
                CellInfo newInfo = AppendBlock(blockData);
                if (newInfo.Offset != -1) { cellIndex[i][j] = newInfo; isIndexDirty = true; success = SaveIndex(); }
                else { success = false; }
            }
            else if (info.PointCount < BLOCKING_FACTOR)
            {
                // Buňka existuje a je v ní místo - načíst, přidat, přepsat na místě
                Console.WriteLine($"AddPoint: Přidávám bod do buňky [{i}][{j}] (obsazeno {info.PointCount}/{BLOCKING_FACTOR})");
                List<GridNode<T>> blockData = LoadBlock(i, j);
                if (blockData == null) { Console.WriteLine($"Chyba AddPoint: Načtení bloku [{i}][{j}] selhalo."); return false; }
                blockData.Add(new GridNode<T>(data, x, y));
                if (WriteBlockInPlace(i, j, blockData)) { success = SaveIndex(); }
                else { success = false; }
            }
            else
            {
                // Blok je plný -> SPLIT (hybridní přístup)
                Console.WriteLine($"AddPoint: Buňka [{i}][{j}] plná ({info.PointCount}/{BLOCKING_FACTOR}). Provádím split...");
                List<GridNode<T>> blockData = LoadBlock(i, j);
                if (blockData == null) { Console.WriteLine($"Chyba AddPoint: Načtení bloku [{i}][{j}] pro split selhalo."); return false; }
                blockData.Add(new GridNode<T>(data, x, y));

                if (SplitCellAndAppend(i, j, blockData)) { success = SaveIndex(); }
                else { success = false; }
            }
        }
        return success;
    }

    public bool DeletePoint(int x, int y)
    {
        if (!IsLoaded) return false; int i = FindIndex(xLines, (double)x); int j = FindIndex(yLines, (double)y); if (i < 0 || j < 0 || i >= cellIndex.Count || j >= cellIndex[i].Count) return false;
        bool success = false; bool deleted = false; lock (fileLock)
        {
            CellInfo info = cellIndex[i][j]; if (info.PointCount == 0) return false; List<GridNode<T>> block = LoadBlock(i, j); if (block == null) return false;
            int removed = block.RemoveAll(n => n != null && n.X == x && n.Y == y); // Přesné porovnání int
            if (removed > 0) { deleted = true; Console.WriteLine($"Mažu {removed} bodů z [{i}][{j}]"); if (WriteBlockInPlace(i, j, block)) success = SaveIndex(); else success = false; } else { success = false; }
        }
        return success && deleted;
    }

    public GridNode<T> FindPoint(int x, int y)
    {
        if (!IsLoaded) return null; int i = FindIndex(xLines, (double)x); int j = FindIndex(yLines, (double)y); if (i < 0 || j < 0 || i >= cellIndex.Count || j >= cellIndex[i].Count) return null;
        List<GridNode<T>> block = LoadBlock(i, j); if (block == null) return null;
        foreach (var n in block) if (n != null && n.X == x && n.Y == y) return n; // Přesné porovnání int
        return null;
    }

    public AreaSearchResult<T> FindPointsInArea(int xMinQuery, int xMaxQuery, int yMinQuery, int yMaxQuery)
    {
        var result = new AreaSearchResult<T>(); if (!IsLoaded) return result;
        int iS = FindIndex(xLines, (double)xMinQuery), iE = FindIndex(xLines, (double)xMaxQuery); int jS = FindIndex(yLines, (double)yMinQuery), jE = FindIndex(yLines, (double)yMaxQuery); if (iS < 0 || iE < 0 || jS < 0 || jE < 0) return result;
        for (int i = iS; i <= iE; i++) { if (i >= cellIndex.Count) continue; for (int j = jS; j <= jE; j++) { if (j >= cellIndex[i].Count) continue; result.CheckedCellIndices.Add((i, j)); CellInfo info = cellIndex[i][j]; if (info.PointCount > 0 && info.Offset >= 0) { List<GridNode<T>> block = LoadBlock(i, j); if (block != null) foreach (var node in block) if (node != null && node.X >= xMinQuery && node.X <= xMaxQuery && node.Y >= yMinQuery && node.Y <= yMaxQuery) result.FoundPoints.Add((node, i, j, info.Offset)); } } }
        return result;
    }

    // --- Dispose a pomocné metody ---
    public void Dispose() { SaveIndex(); GC.SuppressFinalize(this); }
    ~FileGridIndex() { SaveIndex(); }
    private void EnsureDirectoryExists(string filePath) { try { string d = Path.GetDirectoryName(filePath); if (!string.IsNullOrEmpty(d) && !Directory.Exists(d)) Directory.CreateDirectory(d); } catch { } }

} // Konec FileGridIndex<T>