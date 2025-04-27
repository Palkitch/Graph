using System;
using System.IO;
using System.Globalization; // Pro CultureInfo, pokud by bylo třeba
using Graph;
using Graph.City; // Nebo váš namespace, kde máte třídy Gridu a CityData
namespace Graph
{
    public static class OneTimeGenerator
    {
        // --- Nastavení Generátoru ---
        // Adresář a název datasetu, který chcete vygenerovat
        private const string DatasetName = "Random1000_BF30";
        // Cesta k adresáři, kde budou datasety (stejná jako v MainWindow)
        private const string GridDataBasePath = "GridIndicesFixed";
        // Názvy souborů (stejné jako v MainWindow)
        private const string IndexFileName = "index_fixed.idx";
        private const string DataFileName = "data_fixed.dat";
        // Parametry gridu
        private const int PointCountToGenerate = 1000;
        private const int BlockingFactor = 30; // <--- Důležité!
        private const double MinX = 0, MaxX = 1000, MinY = 0, MaxY = 1000;
        // ---------------------------


        /// <summary>
        /// Tuto metodu zavolejte jednou pro vygenerování souborů.
        /// </summary>
        public static void GenerateFiles()
        {
            Console.WriteLine($"--- Generátor Datasetu: {DatasetName} ---");

            string dirPath = Path.Combine(GridDataBasePath, DatasetName);
            string idxPath = Path.Combine(dirPath, IndexFileName);
            string datPath = Path.Combine(dirPath, DataFileName);

            Console.WriteLine($" Cílový adresář: {Path.GetFullPath(dirPath)}");

            // Smazání starých souborů/adresáře, pokud existují (pro čistý start)
            try
            {
                if (Directory.Exists(dirPath))
                {
                    Console.WriteLine(" Mažu existující adresář...");
                    Directory.Delete(dirPath, true);
                }
                Directory.CreateDirectory(dirPath); // Vytvoříme adresář
            }
            catch (Exception ex)
            {
                Console.WriteLine($"! Chyba při přípravě adresáře: {ex.Message}");
                return;
            }

            try
            {
                // 1. Vytvoření serializeru a builderu
                var serializer = new CityDataFixedSizeSerializer();
                var builder = new FileGridBuilder<CityData>(BlockingFactor, MinX, MaxX, MinY, MaxY, serializer);

                // 2. Generování a přidávání bodů
                var random = new Random();
                Console.WriteLine($" Generuji {PointCountToGenerate} náhodných bodů...");
                for (int i = 0; i < PointCountToGenerate; i++)
                {
                    double x = random.NextDouble() * (MaxX - MinX) + MinX;
                    double y = random.NextDouble() * (MaxY - MinY) + MinY;
                    // Jméno omezíme už zde, i když to řeší i CityData/Serializer
                    string name = $"Point_{i + 1}".PadRight(CityData.MaxNameLength).Substring(0, CityData.MaxNameLength);
                    int population = random.Next(100, 500000);
                    builder.AddPoint(new CityData(name, population), x, y);
                }
                Console.WriteLine(" Přidávání bodů do builderu dokončeno.");

                // 3. Sestavení souborů
                Console.WriteLine(" Sestavuji soubory (.idx a .dat)...");
                bool success = builder.BuildFiles(idxPath, datPath);

                if (success)
                {
                    Console.WriteLine($" HOTOVO: Dataset '{DatasetName}' úspěšně vytvořen.");
                }
                else
                {
                    Console.WriteLine($"! CHYBA: Nepodařilo se sestavit soubory pro dataset '{DatasetName}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n!!! NEOČEKÁVANÁ CHYBA BĚHEM GENERÁTORU !!!");
                Console.WriteLine(ex.ToString());
            }
        }
    }

    // --- Příklad, jak to zavolat (např. v samostatném projektu nebo dočasně) ---
    /*
    class MainProgramForGenerator
    {
        static void Main(string[] args)
        {
            OneTimeGenerator.GenerateFiles();

            Console.WriteLine("\nGenerování dokončeno. Stiskněte Enter pro konec.");
            Console.ReadLine();
        }
    }
    */

}