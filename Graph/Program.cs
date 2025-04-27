using Graph.Grid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    public class IntFixedSizeSerializer : IFixedSizeSerializer<int>
    {
        private const int IntSize = sizeof(int);
        private const int FlagSize = sizeof(byte);
        private readonly int fixedDataSize = FlagSize + IntSize; // 5 Bytů

        public int GetFixedSize() => fixedDataSize;

        public void Write(BinaryWriter writer, int instance)
        {
            writer.Write((byte)1);     // Flag: data existují
            writer.Write(instance);    // Data (int)
        }

        public int Read(BinaryReader reader)
        {
            byte hasData = reader.ReadByte();
            if (hasData == 0)
            {
                reader.ReadBytes(IntSize); // Přeskočit místo pro int
                return default(int);
            }
            else
            {
                return reader.ReadInt32();
            }
        }
    }
    class Program
    {
        // --- Konfigurace Testu ---
        const string TestDir = "GridIntTestFiles_Output";
        const string IndexFilePath = "int_index.idx";
        const string DataFilePath = "int_data.dat";
        const int BlockingFactor = 30;
        const double MinX = 0, MaxX = 1000, MinY = 0, MaxY = 1000;
        const int PointCountToGenerate = 1000;

        // --- Pomocné Metody pro Výpis ---
        static void Separator(char c = '=', int count = 70) => Console.WriteLine(new string(c, count));
        static void LogStep(string description) { Separator('-'); Console.WriteLine($"--- {description} ---"); Separator('-'); }
        static void PrintNodeInfo(string prefix, GridNode<int> node)
        {
            if (node != null) Console.WriteLine($"{prefix}Nalezeno: Hodnota={node.Data} @ (X={node.X:G3}, Y={node.Y:G3})"); // G3 = obecný formát, 3 platné číslice
            else Console.WriteLine($"{prefix}Bod nenalezen.");
        }
        static void PrintAreaResult(string prefix, AreaSearchResult<int> result)
        {
            Console.WriteLine($"{prefix}Prohledané bloky: {(result.CheckedCellIndices.Any() ? string.Join(", ", result.CheckedCellIndices.Select(ci => $"[{ci.XIndex}][{ci.YIndex}]")) : "Žádné")}");
            Console.WriteLine($"{prefix}Nalezené body ({result.FoundPoints.Count}):");
            if (result.FoundPoints.Any())
            {
                foreach (var item in result.FoundPoints) Console.WriteLine($"{prefix}  - Hodnota: {item.Node.Data} (X={item.Node.X:G3},Y={item.Node.Y:G3}) [Blok:[{item.XIndex}][{item.YIndex}]@Ofs:{item.Offset}]");
            }
            else { Console.WriteLine($"{prefix}  (žádné)"); }
        }


        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            string fullTestDirPath = Path.GetFullPath(TestDir);
            string fullIndexFilePath = Path.Combine(fullTestDirPath, IndexFilePath);
            string fullDataFilePath = Path.Combine(fullTestDirPath, DataFilePath);

            // Příprava adresáře
            try { if (Directory.Exists(fullTestDirPath)) Directory.Delete(fullTestDirPath, true); Directory.CreateDirectory(fullTestDirPath); }
            catch (Exception ex) { Console.WriteLine($"Chyba přípravy adresáře: {ex.Message}"); Console.ReadKey(); return; }

            // --- FÁZE 1: Build ---
            LogStep("FÁZE 1: Build souborů pro <int> (BF=30)");
            bool buildSuccess = BuildIntTestFiles(fullIndexFilePath, fullDataFilePath);
            if (!buildSuccess) { Console.WriteLine("\nBUILD SELHAL! Ukončuji."); Console.ReadKey(); return; }

            // --- FÁZE 2: Testování FileGridIndex<int> ---
            LogStep("FÁZE 2: Testování FileGridIndex<int> (BF=30)");
            FileGridIndex<int> grid = null;
            try
            {
                var intSerializer = new IntFixedSizeSerializer();
                using (grid = new FileGridIndex<int>(fullIndexFilePath, fullDataFilePath, BlockingFactor, MinX, MaxX, MinY, MaxY, intSerializer))
                {
                    if (!grid.IsLoaded) { Console.WriteLine("CHYBA: Načtení selhalo!"); return; }
                    Console.WriteLine($"Index načten. Rozměry: {grid.XLines?.Count - 1}x{grid.YLines?.Count - 1}");

                    // -- Testovací operace --
                    LogStep("Test 1: FindPoint - Náhodný existující (hledáme blízko středu)");
                    Console.WriteLine(" Hledám bod blízko (50,50)...");
                    GridNode<int> found = null;
                    for (double d = 0; d < 5 && found == null; d += 1)
                    { // Hledáme v okolí 5x5
                        for (double angle = 0; angle < 360 && found == null; angle += 45)
                        {
                            double rad = angle * Math.PI / 180.0;
                            found = grid.FindPoint(50 + d * Math.Cos(rad), 50 + d * Math.Sin(rad));
                        }
                    }
                    if (found == null) found = grid.FindPoint(50, 50); // Zkusíme ještě přesně střed
                    PrintNodeInfo("  Výsledek: ", found);


                    LogStep("Test 2: AddPoint - Přidání známého bodu");
                    int newValue = 1234567; double newX = 7.7, newY = 8.8;
                    Console.WriteLine($" Přidávám: {newValue} na [{newX:G3},{newY:G3}]");
                    bool added = false; try { added = grid.AddPoint(newValue, newX, newY); } catch { }
                    Console.WriteLine($"  Výsledek přidání: {(added ? "ÚSPĚCH" : "SELHALO")}");
                    Console.WriteLine("  Ověření hledáním:"); PrintNodeInfo("   -> ", grid.FindPoint(newX, newY));


                    LogStep("Test 3: FindPointsInArea - Malá oblast");
                    Console.WriteLine(" Hledám v oblasti X=[5,15], Y=[5,15]");
                    PrintAreaResult("  Výsledek: ", grid.FindPointsInArea(5, 15, 5, 15));

                    // --- OPRAVENÝ TEST 6 (Nyní Test 4) ---
                    LogStep("Test 4: AddPoint - Zaplnění bloku a pokus o split");
                    // Předpokládáme, že bod (7.7, 8.8) přidaný v kroku 2 spadl do [0][0]
                    // Přidáme dalších (BlockingFactor - 1) bodů do stejné oblasti, abychom ji zaplnili
                    int pointsToAdd = BlockingFactor - 1; // Kolik jich ještě přidat, aby byla plná
                    int startValue = 2000000;
                    Console.WriteLine($" Pokouším se zaplnit blok [0][0] přidáním {pointsToAdd} bodů blízko (7,8)...");
                    bool fillSuccess = true;
                    for (int k = 0; k < pointsToAdd; ++k)
                    {
                        double dx = 0.1 * (k + 1); // Malý posun, aby nebyly na stejném místě
                        try
                        {
                            if (!grid.AddPoint(startValue + k, 7.0 + dx, 8.0 + dx))
                            {
                                Console.WriteLine($"  Chyba: Přidání {k + 1}. bodu pro zaplnění selhalo.");
                                fillSuccess = false; break;
                            }
                        }
                        catch (Exception ex)
                        { // Zachytíme i výjimku ze splitu, pokud by nastala dříve
                            Console.WriteLine($"  Chyba: Přidání {k + 1}. bodu pro zaplnění selhalo ({ex.GetType().Name}).");
                            fillSuccess = false; break;
                        }
                    }

                    if (fillSuccess)
                    {
                        Console.WriteLine($" Blok [0][0] by nyní měl být plný ({BlockingFactor}/{BlockingFactor}).");
                        Console.WriteLine($" Pokouším se přidat {BlockingFactor + 1}. bod (7.0, 8.0), což by mělo spustit split...");
                        int triggerValue = 3000000; double triggerX = 7.0, triggerY = 8.0;
                        try
                        {
                            added = grid.AddPoint(triggerValue, triggerX, triggerY);
                            Console.WriteLine($"  Výsledek přidání {BlockingFactor + 1}. bodu: {(added ? "ÚSPĚCH (Split proběhl)" : "SELHALO")}");
                            Console.WriteLine("  Ověření hledáním přidaného bodu:");
                            PrintNodeInfo("   -> ", grid.FindPoint(triggerX, triggerY));
                        }
                        catch (Exception ex)
                        { // Zachytíme chybu, pokud by nastala
                            Console.WriteLine($"  Výsledek přidání {BlockingFactor + 1}. bodu: CHYBA ({ex.GetType().Name}) - {ex.Message}");
                        }
                        Console.WriteLine($"  Nové rozměry gridu (po pokusu o split): {grid.XLines?.Count - 1}x{grid.YLines?.Count - 1}");
                    }
                    else
                    {
                        Console.WriteLine(" Nepodařilo se zaplnit blok, test splitu se neprovádí.");
                    }
                    // --- KONEC OPRAVENÉHO TESTU ---


                    LogStep("Test 5: FindPointsInArea - Malá oblast po pokusu o split");
                    Console.WriteLine(" Hledám v oblasti X=[5,15], Y=[5,15]");
                    PrintAreaResult("  Výsledek: ", grid.FindPointsInArea(5, 15, 5, 15));


                    LogStep("Test 6: DeletePoint - Smazání původního přidaného bodu");
                    Console.WriteLine($" Mažu bod na [{newX:G3},{newY:G3}] (Hodnota: {newValue})");
                    bool deleted = grid.DeletePoint(newX, newY);
                    Console.WriteLine($"  Výsledek mazání: {(deleted ? "ÚSPĚCH" : "SELHALO")}");
                    Console.WriteLine("  Ověření hledáním:");
                    PrintNodeInfo("   -> ", grid.FindPoint(newX, newY));


                    LogStep("Test 7: DeletePoint - Mazání neexistujícího bodu");
                    Console.WriteLine(" Mažu bod na [1,1]");
                    deleted = grid.DeletePoint(1, 1);
                    Console.WriteLine($"  Výsledek mazání: {(!deleted ? "SELHALO (OK)" : "ÚSPĚCH (CHYBA?)")}");


                    LogStep("Test 8: FindPointsInArea - Finální stav oblasti");
                    Console.WriteLine(" Hledám v oblasti X=[5,15], Y=[5,15]");
                    PrintAreaResult("  Výsledek: ", grid.FindPointsInArea(5, 15, 5, 15));

                } // Konec using grid
                Console.WriteLine("\nFileGridIndex<int> uvolněn.");

            }
            catch (Exception ex) { Console.WriteLine($"\nNEOČEKÁVANÁ CHYBA TESTU: {ex}"); }
            finally { /* Volitelné mazání souborů */ }

            Separator();
            Console.WriteLine("Test dokončen. Stiskněte klávesu...");
            Console.ReadKey();
        }


        /// Pomocná metoda pro vytvoření testovacích souborů pro int.
        static bool BuildIntTestFiles(string idxPath, string dataPath)
        { /* ... implementace jako dříve ... */
            Console.WriteLine(" Builder<int>: Vytvářím instanci a přidávám body..."); var ser = new IntFixedSizeSerializer(); var bld = new FileGridBuilder<int>(BlockingFactor, MinX, MaxX, MinY, MaxY, ser); var rnd = new Random(); Console.WriteLine($" Builder<int>: Generuji {PointCountToGenerate} bodů..."); for (int i = 0; i < PointCountToGenerate; i++) { double x = rnd.NextDouble() * (MaxX - MinX) + MinX; double y = rnd.NextDouble() * (MaxY - MinY) + MinY; int val = rnd.Next(-50000, 50000); bld.AddPoint(val, x, y); }
            Console.WriteLine(" Builder<int>: Přidávání dokončeno."); Console.WriteLine(" Builder<int>: Volám BuildFiles..."); bool res = bld.BuildFiles(idxPath, dataPath); Console.WriteLine($" Builder<int>: BuildFiles výsledek: {(res ? "OK" : "Chyba")}"); return res;
        }

        // --- ZDE MUSÍ BÝT DEFINICE VŠECH POTŘEBNÝCH TŘÍD ---
        // IFixedSizeSerializer<T>, IntFixedSizeSerializer, GridNode<T>,
        // AreaSearchResult<T>, CellInfo, GridIndex<T> (paměťová),
        // FileGridBuilder<T>, FileGridIndex<T>

    } // Konec třídy Program
}
