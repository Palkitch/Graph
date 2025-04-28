// GridNode.cs - Upraveno pro int X, Y a IFixedSizeSerializer<T>
using Graph;
using System;
using System.IO;

[Serializable]
public class GridNode<T>
{
    public T Data { get; private set; }
    public int X { get; private set; } // <<< ZMĚNA NA INT
    public int Y { get; private set; } // <<< ZMĚNA NA INT

    // Konstruktor přijímá int
    public GridNode(T data, int x, int y) { Data = data; X = x; Y = y; }
    private GridNode() { }
    public override string ToString() => Data?.ToString() ?? "[Prázdný uzel]";

    /// <summary>
    /// Vypočítá fixní velikost serializovaného GridNode<T>.
    /// </summary>
    public static int GetFixedNodeSize(IFixedSizeSerializer<T> dataSerializer)
    {
        if (dataSerializer == null) throw new ArgumentNullException(nameof(dataSerializer));
        // Velikost: X(4) + Y(4) + Fixní velikost T
        return sizeof(int) + sizeof(int) + dataSerializer.GetFixedSize(); // <<< ZMĚNA sizeof(double) na sizeof(int)
    }

    /// <summary>
    /// Zapíše uzel pomocí předaného serializeru pro data T.
    /// </summary>
    public void WriteTo(BinaryWriter writer, IFixedSizeSerializer<T> dataSerializer)
    {
        if (dataSerializer == null) throw new ArgumentNullException(nameof(dataSerializer));
        writer.Write(X); // <<< Zapisuje INT (4 B)
        writer.Write(Y); // <<< Zapisuje INT (4 B)
        dataSerializer.Write(writer, Data); // Zavolá serializer pro T
    }

    /// <summary>
    /// Načte uzel pomocí předaného serializeru pro data T.
    /// </summary>
    public static GridNode<T> ReadFrom(BinaryReader reader, IFixedSizeSerializer<T> dataSerializer)
    {
        if (dataSerializer == null) throw new ArgumentNullException(nameof(dataSerializer));
        var node = new GridNode<T>();
        try
        {
            node.X = reader.ReadInt32(); // <<< Čte INT (4 B)
            node.Y = reader.ReadInt32(); // <<< Čte INT (4 B)
            node.Data = dataSerializer.Read(reader); // Zavolá serializer pro T
            if (node.Data == null && default(T) != null) { /* Varování pro value typy */ }
        }
        catch (Exception ex) { Console.WriteLine($"Chyba čtení GridNode: {ex.Message}"); return null; }
        return node;
    }
}