using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{

    public static class TestDataGenerator
    {

        public static void Generate(string filePath, int numberOfEntries = 1000, int minCoord = 0, int maxCoord = 100)
        {
            Random random = new Random();
            StringBuilder csvContent = new StringBuilder();

            Console.WriteLine($"Generuji {numberOfEntries} testovacích záznamů...");

            for (int i = 1; i <= numberOfEntries; i++)
            {
                string id = $"V{i}"; // Unikátní ID vrcholu (např. V1, V2, ...)
                string data = $"Data_pro_V{i}"; // Nějaká data asociovaná s vrcholem
                int x = random.Next(minCoord, maxCoord + 1); // Náhodné X (včetně maxCoord)
                int y = random.Next(minCoord, maxCoord + 1); // Náhodné Y (včetně maxCoord)

                // Formát řádku: ID,Data,X,Y
                csvContent.AppendLine($"{id},{data},{x},{y}");
            }

            try
            {
                // Zajistí vytvoření adresáře, pokud neexistuje
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Vytvořen adresář: {directory}");
                }

                File.WriteAllText(filePath, csvContent.ToString());
                Console.WriteLine($"Testovací data úspěšně vygenerována a uložena do souboru: '{filePath}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chyba při ukládání souboru s testovacími daty: {ex.Message}");
            }
        }

        // Příklad použití (můžete zavolat např. z Main metody generátoru nebo dočasně z MainWindow)
        // public static void Main(string[] args)
        // {
        //     // Ujistěte se, že cesta odpovídá struktuře vašeho projektu
        //     string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "grafy", "test_data_1000.txt");
        //     Generate(outputPath, 1000, 0, 100);
        //     Console.WriteLine("Hotovo. Stiskněte klávesu pro ukončení.");
        //     Console.ReadKey();
        // }
    }
}
