// MainWindow.xaml.cs - Finální verze pro generický FileGridIndex<T> s fixními bloky

using Graph; // Váš namespace pro CityData, FileGridIndex atd.
using Graph.City;
using Graph.Grid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

// --- Třída CityData (musí být definována) ---
// public class CityData { ... }

// --- Rozhraní a Serializer (musí být definovány) ---
// public interface IFixedSizeSerializer<T> { ... }
// public class CityDataFixedSizeSerializer : IFixedSizeSerializer<CityData> { ... }


namespace GraphGUI // Váš namespace
{
    public partial class MainWindow : Window, IDisposable
    {
        // --- Konstanty a konfigurace ---
        private const string GridDataBasePath = "GridIndicesFixed"; // Adresář pro datasety (fixní velikost)
        private const string IndexFileName = "index_fixed.idx"; // Odlišíme názvy souborů
        private const string DataFileName = "data_fixed.dat";
        private const int DefaultBlockingFactor = 3;
        private const double GridMinX = 0, GridMaxX = 100, GridMinY = 0, GridMaxY = 100;

        // --- Serializátor pro CityData (jedna instance pro celou aplikaci) ---
        private readonly IFixedSizeSerializer<CityData> cityDataSerializer = new CityDataFixedSizeSerializer();

        // --- Instance Indexu (generický, ale používán s CityData) ---
        private FileGridIndex<CityData> fileGridIndex;
        private bool isDisposed = false;

        public MainWindow()
        {
            InitializeComponent();
            //OneTimeGenerator.GenerateFiles();
            InitializeApp();
        }

        /// <summary>
        /// Inicializuje aplikaci - načte datasety, nastaví UI.
        /// </summary>
        private void InitializeApp()
        {
            MessageTextBox.Clear();
            PopulateGridIndexSelector(); // Najde existující datasety

            if (GridIndexSelectorComboBox.Items.Count > 0)
            {
                GridIndexSelectorComboBox.SelectedIndex = 0; // Načte první dataset
            }
            else
            {
                MessageTextBox.Text = $"Nebyly nalezeny žádné datasety ve složce:\n'{Path.GetFullPath(GridDataBasePath)}'\n" +
                                      "Vytvořte nový dataset.\n";
                SetGridUIEnabled(false);
            }

        }

        /// <summary>
        /// Naplní ComboBox pro výběr datasetů GridIndexu.
        /// </summary>
        private void PopulateGridIndexSelector()
        {
            try
            {
                string fullPath = Path.GetFullPath(GridDataBasePath);
                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
                var directories = Directory.GetDirectories(fullPath);
                var datasetNames = directories.Select(Path.GetFileName).OrderBy(name => name).ToList();
                object selectedItem = GridIndexSelectorComboBox.SelectedItem;
                GridIndexSelectorComboBox.ItemsSource = datasetNames;
                if (selectedItem != null && datasetNames.Contains(selectedItem.ToString())) { GridIndexSelectorComboBox.SelectedItem = selectedItem; }
                else if (datasetNames.Count == 0) { fileGridIndex?.Dispose(); fileGridIndex = null; SetGridUIEnabled(false); }
            }
            catch (Exception ex) { MessageTextBox.Text += $"Chyba hledání datasetů: {ex.Message}\n"; GridIndexSelectorComboBox.ItemsSource = new List<string>(); }
        }

        /// <summary>
        /// Zpracuje změnu výběru datasetu v ComboBoxu. Načte nový index.
        /// </summary>
        private void GridIndexSelectorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GridIndexSelectorComboBox.SelectedItem == null) { SetGridUIEnabled(false); return; }
            string selectedDatasetName = GridIndexSelectorComboBox.SelectedItem.ToString();
            string datasetPath = Path.Combine(GridDataBasePath, selectedDatasetName);
            string idxPath = Path.Combine(datasetPath, IndexFileName);
            string datPath = Path.Combine(datasetPath, DataFileName);
            MessageTextBox.Text = $"Načítám dataset: '{selectedDatasetName}'...";
            fileGridIndex?.Dispose(); fileGridIndex = null; // Uvolníme starý
            try
            {
                // === Vytvoření instance s PŘEDANÝM SERIALIZÁTOREM ===
                fileGridIndex = new FileGridIndex<CityData>(idxPath, datPath, DefaultBlockingFactor,
                                                           GridMinX, GridMaxX, GridMinY, GridMaxY,
                                                           cityDataSerializer);
                if (!fileGridIndex.IsLoaded)
                {
                    MessageTextBox.Text += $"\nCHYBA načtení/vytvoření souborů."; SetGridUIEnabled(false);
                }
                else
                {
                    MessageTextBox.Text += $" OK.\nRozměry: {fileGridIndex.XLines?.Count - 1}x{fileGridIndex.YLines?.Count - 1}\n-------\n"; SetGridUIEnabled(true);
                }
            }
            catch (Exception ex) { MessageTextBox.Text += $"\nCHYBA '{selectedDatasetName}': {ex.Message}"; SetGridUIEnabled(false); fileGridIndex = null; }
        }

        /// <summary>
        /// Obsluha tlačítka pro vytvoření nového prázdného datasetu.
        /// </summary>
        private void CreateDatasetButton_Click(object sender, RoutedEventArgs e)
        {
            string newName = NewDatasetNameTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(newName) || newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || newName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                MessageBox.Show("Zadejte platný název datasetu (adresáře)."); return;
            }
            string targetDirPath = Path.Combine(GridDataBasePath, newName);
            string targetIdxPath = Path.Combine(targetDirPath, IndexFileName);
            string targetDatPath = Path.Combine(targetDirPath, DataFileName);
            if (Directory.Exists(targetDirPath) || File.Exists(targetIdxPath) || File.Exists(targetDatPath)) { MessageBox.Show($"Dataset '{newName}' již existuje."); return; }

            MessageTextBox.Text = $"Vytvářím '{newName}'...";
            try
            {
                Directory.CreateDirectory(targetDirPath);
                // Vytvoříme prázdný index (konstruktor alokuje .dat a uloží .idx)
                // Musíme mu předat serializer!
                using (var tempIndex = new FileGridIndex<CityData>(targetIdxPath, targetDatPath, DefaultBlockingFactor,
                                                                  GridMinX, GridMaxX, GridMinY, GridMaxY,
                                                                  cityDataSerializer)) // <<< Zde také
                { if (!tempIndex.IsLoaded) throw new IOException("Nelze init index."); }

                MessageTextBox.Text += " Hotovo.\n"; NewDatasetNameTextBox.Clear(); PopulateGridIndexSelector(); GridIndexSelectorComboBox.SelectedItem = newName;
            }
            catch (Exception ex) { MessageTextBox.Text += $"\nChyba vytváření: {ex.Message}\n"; try { if (Directory.Exists(targetDirPath)) Directory.Delete(targetDirPath, true); } catch { } }
        }

        // --- Pomocné metody UI a Kontroly ---
        private void SetGridUIEnabled(bool isEnabled)
        { /* ... jako dříve ... */
            if (this.Content == null || AddCityButton == null) return;
            AddCityButton.IsEnabled = isEnabled; FindPointButton.IsEnabled = isEnabled; FindIntervalButton.IsEnabled = isEnabled; PrintLinesButton.IsEnabled = isEnabled; PrintGridButton.IsEnabled = isEnabled; DeletePointButton.IsEnabled = isEnabled;
            CityNameTextBox.IsEnabled = isEnabled; PopulationTextBox.IsEnabled = isEnabled; PointXTextBox.IsEnabled = isEnabled; PointYTextBox.IsEnabled = isEnabled; SearchXTextBox.IsEnabled = isEnabled; SearchYTextBox.IsEnabled = isEnabled; IntervalX1TextBox.IsEnabled = isEnabled; IntervalX2TextBox.IsEnabled = isEnabled; IntervalY1TextBox.IsEnabled = isEnabled; IntervalY2TextBox.IsEnabled = isEnabled; DeleteXTextBox.IsEnabled = isEnabled; DeleteYTextBox.IsEnabled = isEnabled;
            string tip = isEnabled ? null : "Vyberte/vytvořte dataset."; AddCityButton.ToolTip = FindPointButton.ToolTip = FindIntervalButton.ToolTip = PrintLinesButton.ToolTip = PrintGridButton.ToolTip = DeletePointButton.ToolTip = tip;
        }
        private bool CheckGridIndexReady() { if (fileGridIndex == null || !fileGridIndex.IsLoaded) { MessageBox.Show("Není načten dataset!", "Chyba", MessageBoxButton.OK, MessageBoxImage.Warning); return false; } return true; }
        private void Swap(ref int a, ref int b) { int t = a; a = b; b = t; }


        // --- Obslužné metody pro tlačítka Gridu ---

        private void AddCityButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckGridIndexReady()) return;
            string cityName = CityNameTextBox.Text; // Oříznutí řeší CityData nebo Serializer
            if (!int.TryParse(PopulationTextBox.Text, out int population) || population < 0)
            {
                MessageBox.Show("Zadejte kladný počet obyv.");
                return;
            }
            if (!int.TryParse(PointXTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int x) ||
                !int.TryParse(PointYTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int y))
            {
                MessageBox.Show("Zadejte platné X, Y.");
                return;
            }

            // Vytvoříme CityData (setter Name by měl oříznout dle konstanty MaxNameLength v CityData)
            var city = new CityData(cityName, population);
            // Můžeme explicitně zkontrolovat/upozornit na oříznutí ZDE, pokud chceme
            if (cityName.Length > CityData.MaxNameLength)
            {
                MessageTextBox.Text = $"Varování: Název '{cityName}' byl oříznut na {CityData.MaxNameLength} znaků.\n";
            }
            else
            {
                MessageTextBox.Clear(); // Vyčistit předchozí zprávu
            }

            try
            {
                bool added = fileGridIndex.AddPoint(city, x, y); // Přidá CityData
                MessageTextBox.Text += added ? $"Město '{city}' přidáno." : $"Město NELZE přidat.";
                if (added) { CityNameTextBox.Clear(); PopulationTextBox.Clear(); PointXTextBox.Clear(); PointYTextBox.Clear(); }
            }
            catch (NotSupportedException nse)
            { // Zachycení výjimky z plného bloku
                MessageTextBox.Text += $"\nMěsto NELZE přidat: {nse.Message}";
                MessageBox.Show(nse.Message, "Blok je plný", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex) { MessageTextBox.Text += $"\nChyba přidání: {ex.Message}"; }
        }

        private void DeletePointButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckGridIndexReady()) return;
            if (int.TryParse(DeleteXTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int x) &&
                int.TryParse(DeleteYTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int y))
            {
                try
                {
                    bool deleted = fileGridIndex.DeletePoint(x, y);
                    MessageTextBox.Text = deleted ? $"Bod(y) na ({x:G},{y:G}) smazán(y)." : $"Bod na ({x:G},{y:G}) nenalezen.";
                }
                catch (Exception ex) { MessageTextBox.Text = $"Chyba mazání: {ex.Message}"; }
            }
            else { MessageTextBox.Text = "Zadejte platné X, Y pro smazání."; }
        }

        private void FindPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckGridIndexReady()) return;
            if (int.TryParse(SearchXTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int searchX) &&
                int.TryParse(SearchYTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int searchY))
            {
                try
                {
                    // FindPoint vrací GridNode<CityData> nebo null
                    GridNode<CityData> foundNode = fileGridIndex.FindPoint(searchX, searchY);
                    if (foundNode != null)
                    {
                        MessageTextBox.Text = $"Bod blízko ({searchX:G}, {searchY:G}) nalezen:\n" +
                                            $"Data: {foundNode.Data}\n" + // Použije CityData.ToString()
                                            $"Souřadnice: (X={foundNode.X:G}, Y={foundNode.Y:G})";
                    }
                    else { MessageTextBox.Text = $"Bod na ({searchX:G}, {searchY:G}) nenalezen."; }
                }
                catch (Exception ex) { MessageTextBox.Text = $"Chyba hledání bodu: {ex.Message}"; }
            }
            else { MessageTextBox.Text = "Zadejte platné X, Y."; }
        }

        private void FindIntervalButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckGridIndexReady()) return;
            if (int.TryParse(IntervalX1TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int x1) &&
                int.TryParse(IntervalY1TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int y1) &&
                int.TryParse(IntervalX2TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int x2) &&
                int.TryParse(IntervalY2TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out int y2))
            {
                if (x1 > x2) Swap(ref x1, ref x2); if (y1 > y2) Swap(ref y1, ref y2);
                try
                {
                    // FindPointsInArea vrací AreaSearchResult<CityData> s tuplem
                    AreaSearchResult<CityData> searchResult = fileGridIndex.FindPointsInArea(x1, x2, y1, y2);
                    var mb = new StringBuilder();
                    mb.AppendLine($"Výsledky X:[{x1:G}, {x2:G}], Y:[{y1:G}, {y2:G}]:"); mb.AppendLine("---");
                    if (searchResult.CheckedCellIndices.Any()) mb.AppendLine($"Prohledané bloky: {string.Join(", ", searchResult.CheckedCellIndices.Select(ci => $"[{ci.XIndex}][{ci.YIndex}]"))}"); else mb.AppendLine("Žádné."); mb.AppendLine("---");
                    // FoundPoints je List<(GridNode<CityData> Node, int XIndex, int YIndex, long Offset)>
                    if (searchResult.FoundPoints.Any())
                    {
                        mb.AppendLine($"Nalezeno {searchResult.FoundPoints.Count} bodů:");
                        foreach (var item in searchResult.FoundPoints)
                        { // Iterujeme přes tuple
                            mb.AppendLine($"- {item.Node.Data} (X={item.Node.X:G}, Y={item.Node.Y:G}) [Blok [{item.XIndex}][{item.YIndex}]@Ofs:{item.Offset}]");
                        }
                    }
                    else { mb.AppendLine("Žádné body nenalezeny."); }
                    MessageTextBox.Text = mb.ToString();
                }
                catch (Exception ex) { MessageTextBox.Text = $"Chyba hledání intervalu: {ex.Message}"; }
            }
            else { MessageTextBox.Text = "Zadejte platný interval."; }
        }

        private void PrintLinesButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckGridIndexReady()) return;
            try
            {
                var xL = fileGridIndex.XLines; var yL = fileGridIndex.YLines; if (xL == null || yL == null) return;
                MessageTextBox.Text = $"X Lines:\n{string.Join("\n", xL.Select(l => $"x = {l:G}"))}\n\nY Lines:\n{string.Join("\n", yL.Select(l => $"y = {l:G}"))}";
            }
            catch (Exception ex) { MessageTextBox.Text = $"Chyba čar: {ex.Message}"; }
        }

        private void PrintGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckGridIndexReady()) return;
            try
            {
                MessageTextBox.Text = $"FileGridIndex Info:\nIndex: {fileGridIndex.IndexFilePath}\nData: {fileGridIndex.DataFilePath}\nLoaded: {fileGridIndex.IsLoaded}\nRozměry: {fileGridIndex.XLines?.Count - 1}x{fileGridIndex.YLines?.Count - 1}";
            }
            catch (Exception ex) { MessageTextBox.Text = $"Chyba info: {ex.Message}"; }
        }

        // --- Grafové metody odstraněny ---

        // --- IDisposable ---
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing) { if (!isDisposed) { if (disposing) { fileGridIndex?.Dispose(); } isDisposed = true; } }
        ~MainWindow() { Dispose(false); }

    } // Konec MainWindow

    // --- Placeholder pro Dialog (nahraďte vlastním) ---
    public class AddVertexWithCoordsDialog { /* ... jako dříve ... */ public string CityName; public int Population; public double CoordX; public double CoordY; }

} // Konec namespace