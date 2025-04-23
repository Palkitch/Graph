using Graph.City;


namespace Graph.Grid
{
    [Serializable]
    public class GridNode<T> // Zůstává generický
    {
        public T Data { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }

        // Konstruktory zůstávají stejné
        public GridNode(T data, double x, double y) { Data = data; X = x; Y = y; }
        private GridNode() { }
        public override string ToString() => Data?.ToString() ?? "[Prázdný uzel]";

        /// <summary>
        /// Vypočítá fixní velikost serializovaného GridNode<T> na základě velikosti T.
        /// </summary>
        public static int GetFixedNodeSize(IFixedSizeSerializer<T> dataSerializer)
        {
            if (dataSerializer == null) throw new ArgumentNullException(nameof(dataSerializer));
            // Velikost: X(8) + Y(8) + Fixní velikost T
            return sizeof(double) + sizeof(double) + dataSerializer.GetFixedSize();
        }

        /// <summary>
        /// Zapíše uzel do binárního streamu s použitím specifikovaného serializátoru pro Data.
        /// </summary>
        public void WriteTo(BinaryWriter writer, IFixedSizeSerializer<T> dataSerializer)
        {
            if (dataSerializer == null) throw new ArgumentNullException(nameof(dataSerializer));
            writer.Write(X); // 8 B
            writer.Write(Y); // 8 B
                             // Zapíšeme Data pomocí serializátoru (ten zajistí padding/ořezání a fixní délku)
            dataSerializer.Write(writer, Data);
        }

        /// <summary>
        /// Načte uzel z binárního streamu s použitím specifikovaného serializátoru pro Data.
        /// </summary>
        public static GridNode<T> ReadFrom(BinaryReader reader, IFixedSizeSerializer<T> dataSerializer)
        {
            if (dataSerializer == null) throw new ArgumentNullException(nameof(dataSerializer));
            var node = new GridNode<T>();
            try
            {
                node.X = reader.ReadDouble();
                node.Y = reader.ReadDouble();
                // Přečteme Data pomocí serializátoru (ten přečte fixní počet bytů)
                node.Data = dataSerializer.Read(reader);
            }
            catch (EndOfStreamException eof) { Console.WriteLine($"Chyba čtení GridNode (EOF): {eof.Message}"); return null; }
            catch (Exception ex) { Console.WriteLine($"Chyba čtení GridNode: {ex.Message}"); return null; }
            return node;
        }
    }
}