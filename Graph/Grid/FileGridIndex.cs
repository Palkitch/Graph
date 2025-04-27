// FileGridIndex.cs - KOMPLETNÍ KÓD - Generický, fixní bloky, hybridní split

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;

// Předpokládá existenci:
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
namespace Graph.Grid
{


    public class FileGridIndex<T> : IDisposable
    {
        // --- Konstanty a Fields ---
        private readonly string indexFilePath;
        private readonly string dataFilePath;
        private readonly int BLOCKING_FACTOR;
        private readonly IFixedSizeSerializer<T> dataSerializer; // Přijatý serializer
        private readonly int FixedNodeSize;                      // Velikost 1 uzlu v B
        private readonly int FixedBlockDataSize;                 // Velikost 1 bloku (slotu) v B
        private readonly object fileLock = new object();         // Zámek pro I/O operace

        private List<double> xLines;                             // Dělící čáry v RAM
        private List<double> yLines;                             // Dělící čáry v RAM
        private List<List<CellInfo>> cellIndex;                  // Metadata buněk v RAM

        private bool isIndexDirty = false;                       // Zda byl index v RAM změněn
        private bool drawVerticalLine = true;                   // Pro střídání směru splitu

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
            // Validace vstupů
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (blockingFactor <= 0) throw new ArgumentOutOfRangeException(nameof(blockingFactor));
            this.indexFilePath = indexFilePath ?? throw new ArgumentNullException(nameof(indexFilePath));
            this.dataFilePath = dataFilePath ?? throw new ArgumentNullException(nameof(dataFilePath));

            // Nastavení interních proměnných
            dataSerializer = serializer;
            BLOCKING_FACTOR = blockingFactor;
            try { FixedNodeSize = GridNode<T>.GetFixedNodeSize(serializer); }
            catch (Exception ex) { throw new InvalidOperationException("Chyba získání velikosti nodu.", ex); }
            // Zajistíme, aby velikost bloku byla alespoň velikost jednoho nodu
            FixedBlockDataSize = Math.Max(1, BLOCKING_FACTOR) * FixedNodeSize;
            if (FixedBlockDataSize <= 0) throw new InvalidOperationException("Vypočtená velikost bloku dat musí být kladná.");


            // Pokus o načtení existujícího indexu
            if (!LoadIndex())
            {
                // Pokud neexistuje nebo je chyba, vytvoří prázdnou strukturu
                Console.WriteLine($"Index nenalezen/neplatný. Vytvářím prázdnou strukturu: {indexFilePath}");
                if (xMin >= xMax || yMin >= yMax) throw new ArgumentException("Neplatné hranice pro inicializaci.");

                xLines = new List<double> { xMin, xMax };
                yLines = new List<double> { yMin, yMax };

                // Vytvoří prázdný index v RAM a alokuje místo v datovém souboru
                InitializeCellIndexAndDataFile();

                isIndexDirty = true; // Nová struktura, je třeba ji uložit
                IsLoaded = true; // Je "načtena" jako prázdná a připravena
                SaveIndex(); // Uloží .idx (s offsety pro alokované sloty)
            }
            // Po úspěšném LoadIndex nebo inicializaci zkontrolujeme datový soubor
            else if (!File.Exists(dataFilePath))
            {
                Console.WriteLine($"Varování: Datový soubor '{dataFilePath}' chybí. Vytvářím prázdný alokovaný soubor.");
                if (!CreateEmptyDataFileWithSlots())
                {
                    Console.WriteLine("Chyba: Nepodařilo se vytvořit datový soubor. Index může být nekonzistentní.");
                    IsLoaded = false; // Pokud selže vytvoření .dat, označíme jako nenačteno
                }
            }
            else
            { /* Volitelná kontrola velikosti .dat souboru */
                CheckDataFileSize();
            }
        }

        // --- Interní Metody (Inicializace, Load/Save Index, Load/Write/Append Blok) ---

        /// <summary>
        /// Inicializuje prázdnou mapu metadat buněk v RAM s vypočítanými offsety.
        /// </summary>
        private void InitializeCellIndex()
        {
            int xDim = Math.Max(1, xLines.Count - 1);
            int yDim = Math.Max(1, yLines.Count - 1);
            cellIndex = new List<List<CellInfo>>(xDim);
            for (int i = 0; i < xDim; i++)
            {
                var col = new List<CellInfo>(yDim);
                for (int j = 0; j < yDim; j++)
                {
                    // Vypočítáme offset pro každý slot v souboru
                    long offset = CalculateExpectedOffset(i, j, yDim);
                    // Uložíme metadata s vypočítaným offsetem a fixní délkou
                    col.Add(new CellInfo { Offset = offset, Length = FixedBlockDataSize, PointCount = 0 });
                }
                cellIndex.Add(col);
            }
            Console.WriteLine($"Interní index {xDim}x{yDim} inicializován v RAM.");
        }

        /// <summary>
        /// Vypočítá očekávaný offset pro buňku [i][j] v souvislém poli bloků.
        /// </summary>
        private long CalculateExpectedOffset(int i, int j, int yDimension)
        {
            // Pořadí ukládání: nejprve všechny řádky pro první sloupec, pak pro druhý atd.
            // nebo obráceně? Zvolme řádky uvnitř sloupců.
            // return (long)(i * yDimension + j) * FixedBlockDataSize; // Pokud jdeme po sloupcích
            // Nebo:
            return (long)(j * (xLines.Count - 1) + i) * FixedBlockDataSize; // Pokud jdeme po řádcích (běžnější)
                                                                            // Musí být konzistentní s builderem a případnou kontrolou!
                                                                            // Pro jednoduchost nyní necháme první variantu:
                                                                            //return (long)(i * yDimension + j) * FixedBlockDataSize;
        }


        /// <summary>
        /// Vytvoří (nebo přepíše) datový soubor a alokuje v něm místo pro všechny buňky.
        /// </summary>
        private bool CreateEmptyDataFileWithSlots()
        {
            if (xLines == null || yLines == null || cellIndex == null)
            {
                Console.WriteLine("Chyba CreateEmptyData: Index není inicializován v RAM."); return false;
            }
            long totalSlots = (long)(xLines.Count - 1) * (yLines.Count - 1);
            if (totalSlots <= 0) return true; // Není co alokovat

            long requiredFileSize = totalSlots * FixedBlockDataSize;
            Console.WriteLine($"Alokuji datový soubor '{dataFilePath}' o velikosti {requiredFileSize} B pro {totalSlots} slotů...");
            try
            {
                EnsureDirectoryExists(dataFilePath);
                lock (fileLock)
                {
                    using (var fs = new FileStream(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fs.SetLength(requiredFileSize); // Alokuje místo
                    }
                }
                Console.WriteLine("Datový soubor alokován.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba alokace datového souboru: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pomocná metoda pro kombinovanou inicializaci indexu v RAM a alokaci .dat souboru.
        /// </summary>
        private void InitializeCellIndexAndDataFile()
        {
            InitializeCellIndex();
            if (!CreateEmptyDataFileWithSlots())
            {
                throw new IOException($"Nepodařilo se vytvořit/alokovat prázdný datový soubor {dataFilePath}");
            }
        }

        /// <summary>
        /// Zkontroluje, zda má datový soubor očekávanou velikost.
        /// </summary>
        private void CheckDataFileSize()
        {
            if (xLines == null || yLines == null || cellIndex == null) return;
            try
            {
                long expectedSize = 0;
                int yDim = yLines.Count - 1;
                // Najdeme nejvyšší offset + velikost bloku v indexu
                // (pro případ, že by index nebyl plně souvislý kvůli chybám/appendům)
                long maxOffsetFound = -1;
                foreach (var col in cellIndex)
                {
                    foreach (var info in col)
                    {
                        if (info.Offset > maxOffsetFound) maxOffsetFound = info.Offset;
                    }
                }
                if (maxOffsetFound >= 0)
                {
                    expectedSize = maxOffsetFound + FixedBlockDataSize;
                }
                else
                {
                    // Pokud jsou všechny offsety -1 (prázdný index), očekávaná velikost je 0
                    expectedSize = (long)(xLines.Count - 1) * (yLines.Count - 1) * FixedBlockDataSize; // Nebo spočítat z rozměrů
                    if (expectedSize < 0) expectedSize = 0;
                }


                long actualSize;
                lock (fileLock)
                {
                    if (!File.Exists(dataFilePath))
                    {
                        Console.WriteLine($"Varování: Datový soubor '{dataFilePath}' neexistuje pro kontrolu velikosti.");
                        return;
                    }
                    using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        actualSize = fs.Length;
                    }
                }
                // Použijeme >= protože soubor může obsahovat "mrtvá" data po appendu
                if (actualSize < expectedSize)
                {
                    Console.WriteLine($"Varování: Velikost datového souboru ({actualSize} B) je menší než očekávaná ({expectedSize} B) dle nejvyššího offsetu v indexu! Může chybět místo pro některé bloky.");
                }
                else if (actualSize > expectedSize)
                {
                    Console.WriteLine($"Info: Velikost datového souboru ({actualSize} B) je větší než minimální očekávaná ({expectedSize} B). Může obsahovat stará data.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při kontrole velikosti datového souboru: {ex.Message}");
            }
        }


        /// <summary>
        /// Načte indexový soubor (.idx) do paměti.
        /// </summary>
        private bool LoadIndex()
        {
            IsLoaded = false;
            if (!File.Exists(indexFilePath)) return false;
            Console.WriteLine($"Načítám index '{indexFilePath}'...");
            try
            {
                lock (fileLock)
                {
                    using (var s = new FileStream(indexFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var r = new BinaryReader(s, Encoding.UTF8, false))
                    {
                        int v = r.ReadInt32(); if (v != 1) throw new NotSupportedException($"Verze {v}");
                        int xc = r.ReadInt32(); if (xc < 2) throw new InvalidDataException("Min 2 X"); xLines = new List<double>(xc); for (int i = 0; i < xc; i++) xLines.Add(r.ReadDouble());
                        int yc = r.ReadInt32(); if (yc < 2) throw new InvalidDataException("Min 2 Y"); yLines = new List<double>(yc); for (int i = 0; i < yc; i++) yLines.Add(r.ReadDouble());
                        int xd = xc - 1, yd = yc - 1; long eb = (long)xd * yd * CellInfo.SizeInBytes; long rb = r.BaseStream.Length - r.BaseStream.Position; if (rb < eb) throw new EndOfStreamException($"Málo dat pro {xd}x{yd} cell info.");
                        cellIndex = new List<List<CellInfo>>(xd);
                        for (int i = 0; i < xd; i++) { var col = new List<CellInfo>(yd); for (int j = 0; j < yd; j++) col.Add(CellInfo.ReadFrom(r)); cellIndex.Add(col); }
                    }
                }
                IsLoaded = true; isIndexDirty = false; Console.WriteLine($"Index načten. Rozměry: {xLines.Count - 1}x{yLines.Count - 1}"); return true;
            }
            catch (Exception ex) { Console.WriteLine($"Chyba načítání indexu: {ex}"); xLines = null; yLines = null; cellIndex = null; IsLoaded = false; return false; }
        }

        /// <summary>
        /// Uloží aktuální stav indexu (xLines, yLines, cellIndex) do .idx souboru, pokud byl změněn.
        /// </summary>
        public bool SaveIndex()
        {
            if (!IsLoaded || !isIndexDirty) return true;
            if (xLines == null || yLines == null || cellIndex == null) return false;
            Console.WriteLine($"Ukládám index '{indexFilePath}'..."); string temp = indexFilePath + ".tmp";
            try
            {
                EnsureDirectoryExists(indexFilePath);
                lock (fileLock)
                {
                    using (var s = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None)) using (var w = new BinaryWriter(s))
                    {
                        w.Write(1); w.Write(xLines.Count); foreach (var x in xLines) w.Write(x); w.Write(yLines.Count); foreach (var y in yLines) w.Write(y);
                        int xd = xLines.Count - 1; int yd = yLines.Count - 1; if (cellIndex.Count != xd) throw new InvalidDataException("X mismatch");
                        for (int i = 0; i < xd; i++) { if (cellIndex[i] == null || cellIndex[i].Count != yd) throw new InvalidDataException($"Y mismatch col {i}"); for (int j = 0; j < yd; j++) cellIndex[i][j].WriteTo(w); }
                    }
                    if (File.Exists(indexFilePath)) File.Replace(temp, indexFilePath, indexFilePath + ".bak", true); else File.Move(temp, indexFilePath);
                    if (File.Exists(indexFilePath + ".bak")) try { File.Delete(indexFilePath + ".bak"); } catch { }
                }
                isIndexDirty = false; Console.WriteLine("Index uložen."); return true;
            }
            catch (Exception ex) { Console.WriteLine($"Chyba uložení indexu: {ex}"); try { if (File.Exists(temp)) File.Delete(temp); } catch { } return false; }
        }

        /// <summary>
        /// Načte všechny uzly pro danou buňku z jejího fixního bloku v datovém souboru.
        /// </summary>
        private List<GridNode<T>> LoadBlock(int i, int j)
        {
            // Kontrola základní platnosti indexů a stavu
            if (!IsLoaded || cellIndex == null || i < 0 || i >= cellIndex.Count || cellIndex[i] == null || j < 0 || j >= cellIndex[i].Count)
            {
                Console.WriteLine($"LoadBlock: Neplatný index [{i}][{j}] nebo index není načten.");
                return new List<GridNode<T>>(); // Vrací prázdný seznam
            }

            CellInfo info = cellIndex[i][j];
            if (info.PointCount <= 0 || info.Offset < 0)
            {
                // Console.WriteLine($"LoadBlock: Buňka [{i}][{j}] je prázdná nebo má neplatný offset.");
                return new List<GridNode<T>>(); // Prázdná nebo neplatná buňka
            }

            var blockData = new List<GridNode<T>>(info.PointCount);
            try
            {
                lock (fileLock)
                {
                    if (!File.Exists(dataFilePath)) { Console.WriteLine($"Chyba LoadBlock: Datový soubor '{dataFilePath}' neexistuje."); return blockData; }

                    using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var reader = new BinaryReader(fs, Encoding.UTF8, false))
                    {
                        // Kontrola offsetu vůči velikosti souboru
                        if (info.Offset >= fs.Length || info.Offset + (long)info.PointCount * FixedNodeSize > fs.Length)
                        {
                            Console.WriteLine($"Chyba LoadBlock: Offset {info.Offset} nebo očekávaný konec dat pro [{i}][{j}] je mimo soubor (délka {fs.Length}). Čtu {info.PointCount} bodů.");
                            // Můžeme se pokusit přečíst, co jde, nebo vrátit prázdný seznam
                            return blockData; // Bezpečnější vrátit prázdný
                        }

                        fs.Seek(info.Offset, SeekOrigin.Begin);
                        for (int k = 0; k < info.PointCount; k++)
                        {
                            // Předáme serializer metodě ReadFrom
                            GridNode<T> node = GridNode<T>.ReadFrom(reader, dataSerializer);
                            if (node != null)
                            {
                                blockData.Add(node);
                            }
                            else
                            {
                                Console.WriteLine($"Chyba čtení uzlu {k + 1} v bloku [{i}][{j}]. Přerušuji čtení bloku.");
                                break; // Přerušíme čtení, pokud deserializace selže
                            }
                        }
                    } // Konec using reader/fs
                } // Konec lock
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba čtení bloku [{i}][{j}] z '{dataFilePath}': {ex.Message}");
                blockData.Clear(); // Při chybě vrátíme prázdný seznam
            }
            return blockData;
        }

        /// <summary>
        /// PŘEPÍŠE data bloku na jeho PŮVODNÍM místě v datovém souboru.
        /// </summary>
        private bool WriteBlockInPlace(int i, int j, List<GridNode<T>> blockData)
        {
            if (!IsLoaded || i < 0 || j < 0 || i >= cellIndex.Count || j >= cellIndex[i].Count) return false;
            CellInfo info = cellIndex[i][j]; if (info.Offset < 0) { Console.WriteLine($"Chyba WriteInPlace: Neplatný offset pro [{i}][{j}]"); return false; }
            int newPointCount = blockData?.Count ?? 0; if (newPointCount > BLOCKING_FACTOR) { Console.WriteLine($"Chyba WriteInPlace: Přetečení BF v [{i}][{j}] ({newPointCount})"); return false; }

            byte[] blockBuffer = new byte[FixedBlockDataSize]; // Připravíme buffer
            try
            {
                // 1. Serializujeme do bufferu v paměti
                using (MemoryStream ms = new MemoryStream(blockBuffer)) // Zapíše přímo do bufferu
                using (BinaryWriter tempWriter = new BinaryWriter(ms))
                {
                    if (newPointCount > 0) foreach (var node in blockData) node.WriteTo(tempWriter, dataSerializer);
                } // Data jsou nyní v blockBuffer, zbytek jsou nuly

                // 2. Zapíšeme celý buffer na místo do souboru
                lock (fileLock)
                {
                    if (!File.Exists(dataFilePath)) return false;
                    using (var fs = new FileStream(dataFilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                    {
                        if (info.Offset + FixedBlockDataSize > fs.Length) { Console.WriteLine($"Chyba WriteInPlace: Offset+Délka pro [{i}][{j}] mimo soubor."); return false; }
                        fs.Seek(info.Offset, SeekOrigin.Begin);
                        fs.Write(blockBuffer, 0, FixedBlockDataSize); // Přepíše celý slot
                    }
                }
                // 3. Aktualizujeme metadata v RAM
                cellIndex[i][j] = new CellInfo { Offset = info.Offset, Length = FixedBlockDataSize, PointCount = newPointCount };
                isIndexDirty = true;
                // Console.WriteLine($"Blok [{i}][{j}] PŘEPSÁN, Nový Počet={newPointCount}");
                return true;
            }
            catch (Exception ex) { Console.WriteLine($"Chyba PŘEPISU bloku [{i}][{j}]: {ex}"); return false; }
        }

        /// <summary>
        /// PŘIDÁ data bloku na KONEC datového souboru.
        /// </summary>
        /// <returns>Metadata (CellInfo) pro nově zapsaný blok, nebo CellInfo.Empty při chybě.</returns>
        private CellInfo AppendBlock(List<GridNode<T>> blockData)
        {
            int pointCount = blockData?.Count ?? 0;
            if (pointCount > BLOCKING_FACTOR) { Console.WriteLine($"Chyba AppendBlock: Příliš mnoho bodů ({pointCount})"); return CellInfo.Empty; }

            byte[] blockBuffer = new byte[FixedBlockDataSize];
            long newOffset = -1;
            try
            {
                // 1. Serializujeme do bufferu
                using (MemoryStream ms = new MemoryStream(blockBuffer)) using (BinaryWriter tempWriter = new BinaryWriter(ms))
                {
                    if (pointCount > 0) foreach (var node in blockData) node.WriteTo(tempWriter, dataSerializer);
                }
                // 2. Přidáme celý buffer na konec souboru
                lock (fileLock)
                {
                    EnsureDirectoryExists(dataFilePath);
                    using (var fs = new FileStream(dataFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
                    {
                        newOffset = fs.Position; // Pozice před zápisem = nový offset
                        fs.Write(blockBuffer, 0, FixedBlockDataSize); // Zapíšeme celý blok
                    }
                }
                // Vrátíme metadata nově zapsaného bloku
                return new CellInfo { Offset = newOffset, Length = FixedBlockDataSize, PointCount = pointCount };
            }
            catch (Exception ex) { Console.WriteLine($"Chyba APPEND bloku: {ex}"); return CellInfo.Empty; }
        }


        /// <summary>
        /// Najde index buňky pro danou souřadnici.
        /// </summary>
        private int FindIndex(IReadOnlyList<double> scale, double value)
        {
            if (scale == null || scale.Count < 2) return -1;
            if (value == scale[scale.Count - 1]) return scale.Count - 2;
            for (int i = 0; i < scale.Count - 1; i++)
            {
                if (value >= scale[i] && value < scale[i + 1]) return i;
            }
            if (value < scale[0]) return 0;
            return scale.Count - 2;
        }


        /// <summary>
        /// Implementace dělení buňky pro souborový systém (hybridní přístup).
        /// Upraví struktury v RAM, zapíše NOVÉ bloky na konec .dat.
        /// </summary>
        /// <param name="xIndex">Původní index sloupce buňky ke splitu.</param>
        /// <param name="yIndex">Původní index řádku buňky ke splitu.</param>
        /// <param name="nodesToRedistribute">VŠECHNY body (staré + nový), které mají být rozděleny.</param>
        /// <returns>True při úspěchu zápisu všech nových bloků.</returns>
        private bool SplitCellAndAppend(int xIndex, int yIndex, List<GridNode<T>> nodesToRedistribute)
        {
            // POZNÁMKA: Metoda musí být volána uvnitř 'lock(fileLock)'

            bool splitX = drawVerticalLine;
            drawVerticalLine = !drawVerticalLine; // Přepneme pro příští split

            nodesToRedistribute.Sort((a, b) => splitX ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            int medianListIndex = nodesToRedistribute.Count / 2;
            double medianValue;
            // --- Výpočet medianValue ---
            { double l = splitX ? xLines[xIndex] : yLines[yIndex]; double u = splitX ? xLines[xIndex + 1] : yLines[yIndex + 1]; if (nodesToRedistribute.Count > 1) { double v1 = splitX ? nodesToRedistribute[medianListIndex - 1].X : nodesToRedistribute[medianListIndex - 1].Y; double v2 = splitX ? nodesToRedistribute[medianListIndex].X : nodesToRedistribute[medianListIndex].Y; medianValue = (v1 + v2) / 2.0; double r = u - l; if (medianValue <= l || medianValue >= u) medianValue = l + r / 2.0; if (r > 1e-9) { double tol = r * 0.001; if (medianValue - l < tol) medianValue = l + tol; if (u - medianValue < tol) medianValue = u - tol; } else { medianValue = l + r / 2.0; } } else { medianValue = l + (u - l) / 2.0; } }
            // --- Konec výpočtu medianValue ---


            // --- Kritická sekce úpravy struktur v RAM ---
            isIndexDirty = true; // Budeme měnit index

            // 1. Aktualizace dělících čar a struktury cellIndex v RAM
            int yDimPreSplit = yLines.Count - 1; // Počet řádků před splitem Y
            if (splitX)
            {
                Console.WriteLine($"Split X na {medianValue}");
                xLines.Insert(xIndex + 1, medianValue);
                var newColumnCells = new List<CellInfo>(yDimPreSplit); // Velikost dle původních Y čar
                for (int j = 0; j < yDimPreSplit; j++) newColumnCells.Add(CellInfo.Empty);
                cellIndex.Insert(xIndex + 1, newColumnCells); // Vložíme nový sloupec metadat
            }
            else
            { // splitY
                Console.WriteLine($"Split Y na {medianValue}");
                yLines.Insert(yIndex + 1, medianValue);
                // Vložíme nový řádek (prázdné CellInfo) do každého sloupce cellIndex
                foreach (var column in cellIndex)
                {
                    // Vložíme na správné místo
                    column.Insert(yIndex + 1, CellInfo.Empty);
                }
            }
            // Nyní mají xLines, yLines a struktura cellIndex nové rozměry a čáry

            // 2. Redistribuce bodů do dočasné mapy podle NOVÝCH indexů
            Dictionary<(int, int), List<GridNode<T>>> newBlocksData = new Dictionary<(int, int), List<GridNode<T>>>();
            foreach (var node in nodesToRedistribute)
            {
                int ni = FindIndex(xLines, node.X); // Najdeme indexy v NOVÉ struktuře
                int nj = FindIndex(yLines, node.Y);
                // Ověření, zda nové indexy jsou platné pro rozšířenou mřížku
                if (ni < 0 || ni >= cellIndex.Count || nj < 0 || nj >= cellIndex[ni].Count)
                {
                    Console.WriteLine($"Chyba Split-Redistribuce: Neplatný nový index [{ni}][{nj}] pro bod {node.Data}");
                    continue; // Přeskočíme bod, který se nevešel (nemělo by nastat)
                }
                var key = (ni, nj);
                if (!newBlocksData.TryGetValue(key, out var list)) { list = new List<GridNode<T>>(); newBlocksData[key] = list; }
                list.Add(node);
            }
            bool originalCellOverwritten = false;

            bool writeOk = true;
            foreach (var kvp in newBlocksData)
            {
                var cellCoords = kvp.Key;
                var nodes = kvp.Value;

                if (nodes.Count > BLOCKING_FACTOR)
                {
                    Console.WriteLine($" !!! Chyba Split: Buňka [{cellCoords.Item1}][{cellCoords.Item2}] má po redistribuci {nodes.Count} > {BLOCKING_FACTOR} bodů!");

                    writeOk = false; break;
                }

                CellInfo newInfo = AppendBlock(nodes);
                if (newInfo.Offset != -1)
                {
                    if (cellCoords.Item1 < cellIndex.Count && cellCoords.Item2 < cellIndex[cellCoords.Item1].Count)
                    {
                        cellIndex[cellCoords.Item1][cellCoords.Item2] = newInfo;
                        if (cellCoords.Item1 == xIndex && cellCoords.Item2 == yIndex)
                        {
                            originalCellOverwritten = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Chyba Split: Index [{cellCoords.Item1}][{cellCoords.Item2}] mimo rozsah RAM indexu po resize!"); writeOk = false; break;
                    }
                }
                else { Console.WriteLine($"Chyba Split: Zápis bloku pro [{cellCoords.Item1}][{cellCoords.Item2}] selhal!"); writeOk = false; break; }
            }
            Console.WriteLine($"Split dokončen {(writeOk ? "úspěšně" : "s chybami")}. Nové rozměry: {xLines.Count - 1}x{yLines.Count - 1}");
            return writeOk;
        }



        public GridNode<T> FindPoint(double x, double y)
        {
            if (!IsLoaded) return null; int i = FindIndex(xLines, x); int j = FindIndex(yLines, y); if (i < 0 || j < 0 || i >= cellIndex.Count || j >= cellIndex[i].Count) return null; List<GridNode<T>> block = LoadBlock(i, j); if (block == null) return null; double tol = 1e-9; foreach (var n in block) if (n != null && Math.Abs(n.X - x) < tol && Math.Abs(n.Y - y) < tol) return n; return null;
        }

        public AreaSearchResult<T> FindPointsInArea(double xMinQuery, double xMaxQuery, double yMinQuery, double yMaxQuery)
        {
            var result = new AreaSearchResult<T>();
            if (!IsLoaded)
                return result;
            int iS = FindIndex(xLines, xMinQuery), iE = FindIndex(xLines, xMaxQuery);
            int jS = FindIndex(yLines, yMinQuery), jE = FindIndex(yLines, yMaxQuery);
            if (iS < 0 || iE < 0 || jS < 0 || jE < 0)
                return result;
            for (int i = iS; i <= iE; i++)
            {
                if (i >= cellIndex.Count) continue;
                for (int j = jS; j <= jE; j++)
                {
                    if (j >= cellIndex[i].Count)
                        continue;
                    result.CheckedCellIndices.Add((i, j));
                    CellInfo info = cellIndex[i][j];
                    if (info.PointCount > 0 && info.Offset >= 0)
                    {
                        List<GridNode<T>> block = LoadBlock(i, j);
                        if (block != null)
                            foreach (var node in block)
                                if (node != null && node.X >= xMinQuery && node.X <= xMaxQuery && node.Y >= yMinQuery && node.Y <= yMaxQuery)
                                    result.FoundPoints.Add((node, i, j, info.Offset));
                    }
                }
            }
            return result;
        }

        public bool DeletePoint(double x, double y)
        { /* ... Implementace s LoadBlock a WriteBlockInPlace jako dříve ... */
            if (!IsLoaded) return false; int i = FindIndex(xLines, x); int j = FindIndex(yLines, y); if (i < 0 || j < 0 || i >= cellIndex.Count || j >= cellIndex[i].Count) return false;
            bool success = false; bool deleted = false; lock (fileLock)
            {
                CellInfo info = cellIndex[i][j]; if (info.PointCount == 0) return false; List<GridNode<T>> block = LoadBlock(i, j); if (block == null) return false;
                double tol = 1e-9; int removed = block.RemoveAll(n => n != null && Math.Abs(n.X - x) < tol && Math.Abs(n.Y - y) < tol);
                if (removed > 0) { deleted = true; if (WriteBlockInPlace(i, j, block)) success = SaveIndex(); else success = false; } else { success = false; }
            }
            return success && deleted;
        }
        public bool AddPoint(T data, double x, double y)
        {
            if (!IsLoaded)
            {
                Console.WriteLine("AddPoint: Index není načten.");
                return false;
            }
            if (data == null) throw new ArgumentNullException(nameof(data));

            // ZDE můžete přidat validaci/ořezání dat 'data' podle limitů definovaných v IFixedSizeSerializer<T>,
            // aby nedošlo k chybě při zápisu WriteBlockInPlace nebo AppendBlock, pokud by data byla příliš velká.
            // Např. if (data is CityData cd) cd.Name = CityData.TruncatePadName(cd.Name);

            int i = FindIndex(xLines, x);
            int j = FindIndex(yLines, y);

            // Kontrola, zda jsou vypočtené indexy v platných mezích RAM indexu
            if (i < 0 || cellIndex == null || i >= cellIndex.Count || cellIndex[i] == null || j < 0 || j >= cellIndex[i].Count)
            {
                Console.WriteLine($"AddPoint: Bod ({x},{y}) je mimo definované hranice mřížky nebo chyba indexu (Indexy: [{i}][{j}]).");
                return false;
            }

            bool success = false;
            lock (fileLock) // Zamkneme celou operaci (čtení info, čtení bloku, zápis bloku, potenciální split, uložení indexu)
            {
                CellInfo info = cellIndex[i][j]; // Získáme metadata buňky

                // Zkontrolujeme kapacitu pomocí PointCount z metadat
                if (info.PointCount < BLOCKING_FACTOR)
                {
                    // Je místo: Načíst blok, přidat bod, přepsat blok na místě, uložit index
                    Console.WriteLine($"AddPoint: Přidávám bod do buňky [{i}][{j}] (obsazeno {info.PointCount}/{BLOCKING_FACTOR})");
                    List<GridNode<T>> blockData = LoadBlock(i, j);
                    if (blockData == null)
                    { // Kontrola chyby při načítání
                        Console.WriteLine($"Chyba AddPoint: Nepodařilo se načíst blok [{i}][{j}] pro přidání.");
                        return false; // return explicitně uvnitř lock není ideální, ale zde pro jednoduchost
                    }
                    blockData.Add(new GridNode<T>(data, x, y)); // Přidáme nový bod do načteného seznamu
                    if (WriteBlockInPlace(i, j, blockData))
                    { // Přepíšeme blok na disku na původním místě
                        success = SaveIndex(); // Uložíme aktualizovaný index (.idx)
                    }
                    else
                    {
                        Console.WriteLine($"Chyba AddPoint: Nepodařilo se zapsat přepsaný blok [{i}][{j}].");
                        success = false; // Chyba zápisu bloku
                    }
                }
                else
                {
                    // Blok je plný -> SPLIT (hybridní přístup)
                    Console.WriteLine($"AddPoint: Buňka [{i}][{j}] plná ({info.PointCount}/{BLOCKING_FACTOR}). Provádím split...");
                    List<GridNode<T>> blockData = LoadBlock(i, j); // Načteme plný blok
                    if (blockData == null)
                    { // Kontrola chyby při načítání
                        Console.WriteLine($"Chyba AddPoint: Nepodařilo se načíst blok [{i}][{j}] pro split.");
                        return false;
                    }
                    blockData.Add(new GridNode<T>(data, x, y)); // Přidáme nový bod do paměti pro redistribuci

                    // Zavoláme metodu, která provede split v RAM, zapíše NOVÉ bloky na konec .dat
                    // a aktualizuje index v RAM.
                    if (SplitCellAndAppend(i, j, blockData))
                    {
                        success = SaveIndex(); // Uložíme změněný index a čáry
                    }
                    else
                    {
                        Console.WriteLine($"Chyba AddPoint: Operace SplitCellAndAppend selhala pro buňku [{i}][{j}].");
                        success = false; // Split nebo zápis nových bloků selhal
                    }
                }
            } // Konec lock
            return success;
        }

        // --- Dispose a pomocné metody ---
        public void Dispose() { SaveIndex(); GC.SuppressFinalize(this); }
        ~FileGridIndex() { SaveIndex(); }
        private void EnsureDirectoryExists(string filePath) { try { string d = Path.GetDirectoryName(filePath); if (!string.IsNullOrEmpty(d) && !Directory.Exists(d)) Directory.CreateDirectory(d); } catch { } }

    } // Konec FileGridIndex<T>

}