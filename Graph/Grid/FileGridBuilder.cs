using Graph;
using System;
using System.Collections.Generic;
using System.IO;
using Graph.Grid;

public class FileGridBuilder<T>
{
    private readonly GridIndex<T> inMemoryGrid; // Paměťový index
    private readonly int BLOCKING_FACTOR;
    private readonly IFixedSizeSerializer<T> dataSerializer; // Přijímá serializer
    private readonly int FixedNodeSize;       // Velikost jednoho uzlu
    private readonly int FixedBlockDataSize;  // Velikost slotu pro buňku

    public FileGridBuilder(int blockingFactor, double xMin, double xMax, double yMin, double yMax, IFixedSizeSerializer<T> serializer)
    {
        if (blockingFactor <= 0) throw new ArgumentOutOfRangeException(nameof(blockingFactor));
        if (serializer == null) throw new ArgumentNullException(nameof(serializer));

        this.BLOCKING_FACTOR = blockingFactor;
        this.dataSerializer = serializer;
        this.FixedNodeSize = GridNode<T>.GetFixedNodeSize(serializer); // Získá velikost uzlu
        this.FixedBlockDataSize = this.BLOCKING_FACTOR * this.FixedNodeSize; // Velikost slotu buňky
        this.inMemoryGrid = new GridIndex<T>(blockingFactor, xMin, xMax, yMin, yMax);
        Console.WriteLine($"BUILDER: BF={blockingFactor}, FixedNodeSize={FixedNodeSize}, FixedBlockDataSize={FixedBlockDataSize}");
    }

    public void AddPoint(T data, int x, int y) => inMemoryGrid.AddPoint(data, x, y);

    public bool BuildFiles(string indexFilePath, string dataFilePath)
    {
        Console.WriteLine("BUILDER: Sestavuji soubory (fixní bloky)...");
        // ... (Získání finalXLines, finalYLines, internalGridStruct jako dříve) ...
        var finalXLines = inMemoryGrid.GetXLines();
        var finalYLines = inMemoryGrid.GetYLines();
        var internalGridStruct = inMemoryGrid.GetInternalGridStructure();
        if (finalXLines == null || finalYLines == null || internalGridStruct == null) return false;
        int xDim = finalXLines.Count - 1; int yDim = finalYLines.Count - 1;
        if (xDim <= 0 || yDim <= 0) return false;


        var cellIndexData = new List<List<CellInfo>>(xDim);
        for (int i = 0; i < xDim; i++) { /* ... inicializace cellIndexData ... */ }

        byte[] paddingBuffer = new byte[FixedBlockDataSize]; // Pro zápis paddingu

        try
        {
            EnsureDirectoryExists(indexFilePath); EnsureDirectoryExists(dataFilePath);
            using (var dataStream = new FileStream(dataFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var dataWriter = new BinaryWriter(dataStream)) // Necháme UTF8 default
            using (var indexStream = new FileStream(indexFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var indexWriter = new BinaryWriter(indexStream))
            {
                // Zápis hlavičky indexu (verze, čáry)
                indexWriter.Write(1); // Verze 1
                indexWriter.Write(finalXLines.Count); foreach (var x in finalXLines) indexWriter.Write(x);
                indexWriter.Write(finalYLines.Count); foreach (var y in finalYLines) indexWriter.Write(y);

                Console.WriteLine("BUILDER: Zapisuji data a sbírám metadata...");
                long totalPointsWritten = 0;

                for (int i = 0; i < xDim; i++)
                {
                    // ... (inicializace sloupce v cellIndexData) ...
                    var column = new List<CellInfo>(yDim);
                    for (int j = 0; j < yDim; j++) column.Add(CellInfo.Empty);
                    cellIndexData.Add(column);

                    if (i >= internalGridStruct.Count || internalGridStruct[i] == null) continue;

                    for (int j = 0; j < yDim; j++)
                    {
                        if (j >= internalGridStruct[i].Count || internalGridStruct[i][j] == null) continue;

                        List<GridNode<T>> cellNodes = internalGridStruct[i][j];
                        int currentPointCount = cellNodes?.Count ?? 0;
                        long blockStartOffset = dataWriter.BaseStream.Position;

                        // Zapíšeme skutečné body (max BLOCKING_FACTOR)
                        int pointsActuallyWritten = 0;
                        long actualBytesWritten = 0;
                        if (currentPointCount > 0)
                        {
                            foreach (var node in cellNodes)
                            {
                                if (pointsActuallyWritten < BLOCKING_FACTOR)
                                {
                                    // Předáme serializer metodě WriteTo
                                    node.WriteTo(dataWriter, dataSerializer);
                                    pointsActuallyWritten++; totalPointsWritten++;
                                }
                                else { break; } // Nepřekročíme BF
                            }
                            actualBytesWritten = dataWriter.BaseStream.Position - blockStartOffset;
                        }

                        // Vypočítáme a zapíšeme padding
                        long paddingBytesNeeded = FixedBlockDataSize - actualBytesWritten;
                        if (paddingBytesNeeded < 0)
                        {
                            // Toto by nemělo nastat, pokud FixedNodeSize je správně
                            Console.WriteLine($" !!! BUILDER CHYBA: Buňka [{i}][{j}] překročila FixBlockSize !!!");
                            paddingBytesNeeded = 0;
                        }
                        if (paddingBytesNeeded > 0)
                        {
                            dataWriter.Write(paddingBuffer, 0, (int)paddingBytesNeeded);
                        }

                        // Uložíme metadata (offset + fixní délka bloku + skutečný počet bodů)
                        cellIndexData[i][j] = new CellInfo
                        {
                            Offset = blockStartOffset,
                            Length = FixedBlockDataSize, // Vždy fixní délka
                            PointCount = pointsActuallyWritten
                        };
                    }
                }
                Console.WriteLine($"BUILDER: Zapsáno bodů: {totalPointsWritten}");

                // Zapíšeme metadata do indexu
                Console.WriteLine("BUILDER: Zapisuji metadata...");
                for (int i = 0; i < xDim; i++)
                {
                    for (int j = 0; j < yDim; j++)
                    {
                        cellIndexData[i][j].WriteTo(indexWriter);
                    }
                }
                Console.WriteLine("BUILDER: Sestavení dokončeno.");
            }
            return true;
        }
        catch (Exception ex) { /* ... ošetření ... */ Console.WriteLine($"BUILDER Chyba: {ex}"); return false; }
    }
    private void EnsureDirectoryExists(string filePath) { /* ... */ }
}