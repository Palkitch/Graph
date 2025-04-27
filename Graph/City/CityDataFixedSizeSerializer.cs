using System;
using System.IO;
using System.Text;

// Předpokládá existenci třídy CityData
// public class CityData { public string Name; public int Population; ... }

/// <summary>
/// Serializátor pro CityData zajišťující fixní velikost.
/// Name je omezeno a použije se padding.
/// </summary>
namespace Graph.City
{
    public class CityDataFixedSizeSerializer : IFixedSizeSerializer<CityData>
    {
        private readonly Encoding encoding = Encoding.UTF8;
        // Konstanty pro výpočet velikosti
        private const int MaxNameLength = 15;
        private const int FixedNameByteSize = 64; // Buffer pro jméno
        private const int PopulationSize = sizeof(int); // 4
        private const int NullFlagSize = sizeof(byte); // 1 byte pro příznak null

        // Celková fixní velikost pro serializovaná data CityData (bez X, Y)
        private readonly int fixedDataSize = NullFlagSize + PopulationSize + FixedNameByteSize; // 1 + 4 + 64 = 69

        public int GetFixedSize() => fixedDataSize;

        public void Write(BinaryWriter writer, CityData instance)
        {
            if (instance == null)
            {
                writer.Write((byte)0); // Flag: 0 = null
                                       // Zapíšeme padding pro zbytek místa
                writer.Write(new byte[PopulationSize + FixedNameByteSize]);
            }
            else
            {
                writer.Write((byte)1); // Flag: 1 = not null

                // Zapíšeme Population
                writer.Write(instance.Population);

                // Připravíme a zapíšeme Name s paddingem
                byte[] nameBuffer = new byte[FixedNameByteSize]; // Vynulovaný buffer
                string nameToWrite = instance.Name ?? "";
                if (nameToWrite.Length > MaxNameLength)
                    nameToWrite = nameToWrite.Substring(0, MaxNameLength); // Ořízneme

                byte[] nameBytes = encoding.GetBytes(nameToWrite);
                int bytesToCopy = Math.Min(nameBytes.Length, FixedNameByteSize);
                Buffer.BlockCopy(nameBytes, 0, nameBuffer, 0, bytesToCopy); // Zkopírujeme do bufferu

                writer.Write(nameBuffer); // Zapíšeme celý buffer (64 bytů)
            }
        }

        public CityData Read(BinaryReader reader)
        {
            byte nullFlag = reader.ReadByte();
            if (nullFlag == 0)
            {
                // Přečteme (přeskočíme) padding
                reader.ReadBytes(PopulationSize + FixedNameByteSize);
                return null; // Vrátíme null
            }
            else
            {
                try
                {
                    int population = reader.ReadInt32();
                    byte[] nameBuffer = reader.ReadBytes(FixedNameByteSize);
                    string name = encoding.GetString(nameBuffer).TrimEnd('\0'); // Převedeme a ořízneme nuly
                    return new CityData(name, population);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Chyba čtení CityData (fixed): {ex.Message}");
                    // I při chybě musíme zajistit, že se přečetl správný počet bytů,
                    // což ReadBytes zajistilo, pokud nevyhodilo výjimku dříve.
                    return new CityData("Chyba čtení", -1);
                }
            }
        }
    }
}