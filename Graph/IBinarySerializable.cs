using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;


namespace Graph
{
    // Rozhraní (pokud ho chcete používat pro složitější T)
    public interface IBinarySerializable
    {
        void WriteTo(BinaryWriter writer);
        // ReadFrom by muselo být řešeno jinak (např. statická tovární metoda)
    }

    // Metadata buňky pro indexový soubor
    internal struct CellInfo
    {
        public long Offset;       // Pozice v .dat
        public int Length;        // Délka v .dat (informativní)
        public int PointCount;    // Počet bodů

        public static readonly CellInfo Empty = new CellInfo { Offset = -1, Length = 0, PointCount = 0 };
        public static readonly int SizeInBytes = sizeof(long) + sizeof(int) + sizeof(int);

        public void WriteTo(BinaryWriter writer) { writer.Write(Offset); writer.Write(Length); writer.Write(PointCount); }
        public static CellInfo ReadFrom(BinaryReader reader) { return new CellInfo { Offset = reader.ReadInt64(), Length = reader.ReadInt32(), PointCount = reader.ReadInt32() }; }
    }
}