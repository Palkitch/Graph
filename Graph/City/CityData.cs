namespace Graph.City
{
    // CityData.cs
    using System;
    using System.IO;
    using System.Text; // Pro Encoding

    public class CityData
    {
        // --- Konstanty pro fixní velikost ---
        public const int MaxNameLength = 15; // Max délka názvu
                                             // Velikost bufferu pro název v bytech (UTF8 max 4B/znak + rezerva)
                                             // Zvolíme např. 64 bytů pro jistotu (15*4 = 60)
        public const int FixedNameByteSize = 64;
        // ------------------------------------

        private string name = "";
        public string Name
        {
            get => name;
            set => name = TruncatePadName(value); // Oříznutí při nastavení
        }
        public int Population { get; set; }

        public CityData() { }

        public CityData(string name, int population)
        {
            Name = name; // Setter se postará o oříznutí/padding
            Population = population;
        }

        public override string ToString()
        {
            return $"{Name.TrimEnd()} ({Population} obyv.)"; // Trimneme mezery/null znaky pro výpis
        }

        /// <summary>
        /// Ořízne nebo doplní název na MaxNameLength (pro interní účely).
        /// Nyní raději řešíme padding až při zápisu do bufferu.
        /// </summary>
        private static string TruncatePadName(string inputName)
        {
            string name = inputName ?? "";
            if (name.Length > MaxNameLength)
                return name.Substring(0, MaxNameLength);
            // Padding explicitním znakem není nutný, pokud nulujeme buffer
            // return name.PadRight(MaxNameLength, '\0'); // Příklad paddingu nulovým znakem
            return name; // Vrátíme oříznutý nebo původní
        }

        /// <summary>
        /// Zapíše data do pevného byte bufferu pro jméno a int pro populaci.
        /// </summary>
        public void WriteBinaryFixed(BinaryWriter writer, Encoding encoding)
        {
            writer.Write(Population); // int (4 byty)

            byte[] nameBuffer = new byte[FixedNameByteSize]; // Vytvoří buffer plný nul
                                                             // Získá byty z potenciálně oříznutého jména
            byte[] nameBytes = encoding.GetBytes(TruncatePadName(Name));
            // Zkopíruje do bufferu jen tolik, kolik se vejde
            int bytesToCopy = Math.Min(nameBytes.Length, FixedNameByteSize);
            Buffer.BlockCopy(nameBytes, 0, nameBuffer, 0, bytesToCopy);
            // Zapíše celý buffer (včetně padding nul)
            writer.Write(nameBuffer);
        }

        /// <summary>
        /// Načte data z binárního streamu (očekává fixní formát).
        /// </summary>
        public static CityData ReadBinaryFixed(BinaryReader reader, Encoding encoding)
        {
            try
            {
                int population = reader.ReadInt32();
                byte[] nameBuffer = reader.ReadBytes(FixedNameByteSize); // Přečte fixní počet bytů

                // Převede byty na string, odstraní koncové nuly (padding)
                string name = encoding.GetString(nameBuffer).TrimEnd('\0');

                return new CityData(name, population);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba čtení Fix CityData: {ex.Message}");
                return new CityData("Chyba", -1);
            }
        }
    }
}