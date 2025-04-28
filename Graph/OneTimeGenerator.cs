using Graph.City;


namespace Graph.Grid
{
    public static class OneTimeGenerator
    {
        // --- Nastavení Generátoru ---
        private const string DatasetName = "Random1000_BF30_IntCoords"; // Nový název pro odlišení
                                                                        // Cesta k adresáři (stejná jako v MainWindow)
        private const string GridDataBasePath = "GridIndicesFixed";
        // Názvy souborů (stejné jako v MainWindow)
        private const string IndexFileName = "index_fixed.idx";
        private const string DataFileName = "data_fixed.dat";
        // Parametry gridu
        private const int PointCountToGenerate = 1000;
        private const int BlockingFactor = 30;
        // Hranice nyní jako INT
        private const int MinX = 0, MaxX = 1000, MinY = 0, MaxY = 1000;
        // ---------------------------


        /// <summary>
        /// Tuto metodu zavolejte jednou pro vygenerování souborů s INT souřadnicemi.
        /// </summary>
        public static void GenerateFiles()
        {
            Console.WriteLine($"--- Generátor Datasetu: {DatasetName} (INT Coords) ---");

            string dirPath = Path.Combine(GridDataBasePath, DatasetName);
            string idxPath = Path.Combine(dirPath, IndexFileName);
            string datPath = Path.Combine(dirPath, DataFileName);

            Console.WriteLine($" Cílový adresář: {Path.GetFullPath(dirPath)}");

            // Příprava adresáře
            try
            {
                if (Directory.Exists(dirPath))
                {
                    Console.WriteLine(" Mažu existující adresář...");
                    Directory.Delete(dirPath, true);
                }
                Directory.CreateDirectory(dirPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"! Chyba při přípravě adresáře: {ex.Message}");
                return;
            }

            try
            {
                // 1. Vytvoření serializeru a builderu
                var serializer = new CityDataFixedSizeSerializer(); // Stále potřebujeme pro CityData
                                                                    // Builder nyní používá int souřadnice v AddPoint
                var builder = new FileGridBuilder<CityData>(BlockingFactor, MinX, MaxX, MinY, MaxY, serializer);

                // 2. Generování a přidávání bodů s INT souřadnicemi
                var random = new Random();
                Console.WriteLine($" Generuji {PointCountToGenerate} náhodných bodů (INT souřadnice)...");
                for (int i = 0; i < PointCountToGenerate; i++)
                {
                    // Generování INT souřadnic
                    // Random.Next(min, max) generuje od min (včetně) do max (VYJMA)! Proto +1 u MaxX/MaxY.
                    int x = random.Next(MinX, MaxX + 1);
                    int y = random.Next(MinY, MaxY + 1);

                    // Jméno a populace
                    string name = $"Point_{i + 1}";
                    // Oříznutí jména (řeší CityData/Serializer, ale pro jistotu)
                    // if (name.Length > CityData.MaxNameLength) name = name.Substring(0, CityData.MaxNameLength);
                    int population = random.Next(100, 500000);

                    // Přidáme bod s INT souřadnicemi
                    builder.AddPoint(new CityData(name, population), x, y);

                    if ((i + 1) % 100 == 0) Console.Write(".");
                }
                Console.WriteLine("\n Přidávání bodů do builderu dokončeno.");

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
}