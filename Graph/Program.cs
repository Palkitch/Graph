using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    class Program
    {
        static void Main(string[] args)
        {
            // Cesta, kam chcete soubor uložit
            string outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "grafy", "test_data_1000.txt");

            // Zavoláme metodu Generate z třídy TestDataGenerator
            TestDataGenerator.Generate(outputPath, 1000, 0, 100);

            // Po dokončení můžete zobrazit zprávu nebo provést další akce
            Console.WriteLine("Testovací data byla vygenerována.");
            Console.WriteLine("Stiskněte klávesu pro ukončení.");
            Console.ReadKey();
        }

    }
}
