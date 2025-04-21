using Graph; 
using System;
using System.Collections.Generic;
using System.Globalization;     
using System.IO;
using System.Linq;              
using System.Text;
using System.Windows;
using System.Windows.Controls;


namespace GraphGUI
{
    public partial class MainWindow : Window
    {
        private const string GridDataFilePath = "grid_data.bin";
        private const double GridMinX = 0, GridMaxX = 100, GridMinY = 0, GridMaxY = 100;

        private DijkstraGraph<string, string, int> graph = new DijkstraGraph<string, string, int>();
     
        private GridIndex<string> gridIndex;

        public MainWindow()
        {
            InitializeComponent();

            gridIndex = new GridIndex<string>(3, GridDataFilePath, GridMinX, GridMaxX, GridMinY, GridMaxY);
            MessageTextBox.Text = $"GridIndex inicializován. Načteno ze souboru: {gridIndex.IsLoadedFromFile}\n";
            MessageTextBox.Text += gridIndex.ToString() + "\n-------\n";

            string graphDataSourcePath = "grafy/gridIndexZadaniC.txt"; 
            LoadGraphStructureFromCSV(graphDataSourcePath);

            if (!gridIndex.IsLoadedFromFile)
            {
                MessageTextBox.Text += $"Binární soubor '{GridDataFilePath}' nebyl nalezen nebo je neplatný.\n";
                MessageTextBox.Text += $"Pokouším se naplnit GridIndex daty z CSV: '{graphDataSourcePath}'...\n";
                bool populated = PopulateGridFromCSV(graphDataSourcePath);
                if (populated)
                {
                    MessageTextBox.Text += "GridIndex úspěšně naplněn z CSV.\n";
                    bool saved = gridIndex.Save();
                    MessageTextBox.Text += $"Pokus o uložení do '{GridDataFilePath}': {(saved ? "Úspěšný" : "Neúspěšný")}\n";
                    MessageTextBox.Text += gridIndex.ToString() + "\n-------\n";
                }
                else
                {
                    MessageTextBox.Text += "Naplnění GridIndex z CSV se nezdařilo.\n";
                }
            }
            else 
            {
                MessageTextBox.Text += $"GridIndex úspěšně načten z '{GridDataFilePath}'. Načítání z CSV bylo přeskočeno.\n";

            }
            UpdateUIComboBoxes();
        }

        private void LoadGraphStructureFromCSV(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageTextBox.Text += $"Chyba: Soubor pro načtení struktury grafu nebyl nalezen: {filePath}\n";
                    return;
                }

                string[] lines = File.ReadAllLines(filePath);
                graph = new DijkstraGraph<string, string, int>(); 
                int verticesLoaded = 0;
                foreach (var line in lines.Skip(0)) 
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 2)
                    {
                        var id = parts[0].Trim();
                        var data = parts[1].Trim();
                        if (!string.IsNullOrEmpty(id))
                        {
                            if (graph.AddVertex(id, data))
                            {
                                verticesLoaded++;
                            }
                            else
                            {
                                MessageTextBox.Text += $"Varování: Duplicitní vrchol '{id}' v CSV pro graf.\n";
                            }
                        }
                    }
                    else
                    {
                        MessageTextBox.Text += $"Varování: Neplatný řádek v CSV pro graf: {line}\n";
                    }
                }
                MessageTextBox.Text += $"Struktura grafu načtena z '{filePath}'. Počet vrcholů: {verticesLoaded}.\n";
            }
            catch (Exception ex)
            {
                MessageTextBox.Text += $"Kritická chyba při načítání struktury grafu z CSV: {ex.Message}\n";

                graph = new DijkstraGraph<string, string, int>();
            }
        }


        private bool PopulateGridFromCSV(string filePath)
        {
            bool dataAdded = false;
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageTextBox.Text += $"Chyba: Soubor pro naplnění gridu nebyl nalezen: {filePath}\n";
                    return false;
                }

                string[] lines = File.ReadAllLines(filePath);
                int pointsAdded = 0;
                foreach (var line in lines.Skip(0)) 
                {
                    var parts = line.Split(',');
                    if (parts.Length == 4)
                    {
                        var id = parts[0].Trim();
                        if (!string.IsNullOrEmpty(id) &&
                            double.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                            double.TryParse(parts[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
                        {
                            gridIndex.AddPoint(id, x, y);
                            pointsAdded++;
                            dataAdded = true;
                        }
                        else
                        {
                            MessageTextBox.Text += $"Varování: Neplatný řádek v CSV pro grid: {line}\n";
                        }
                    }
                    else
                    {
                        MessageTextBox.Text += $"Varování: Nesprávný počet polí v CSV pro grid: {line}\n";
                    }
                }
                MessageTextBox.Text += $"Grid naplněn {pointsAdded} body z '{filePath}'.\n";
                return dataAdded;
            }
            catch (Exception ex)
            {
                MessageTextBox.Text += $"Chyba při plnění gridu z CSV: {ex.Message}\n";
                return false;
            }
        }


        private void AddVertex(string vertexId, string vertexData, double x, double y)
        {
            if (graph.AddVertex(vertexId, vertexData))
            {
                gridIndex.AddPoint(vertexId, x, y); 
                gridIndex.Save();
                MessageTextBox.Text += $"Vrchol {vertexId} přidán na souřadnice ({x}, {y}) a uložen.\n";
                UpdateUIComboBoxes(); 
            }
            else
            {
                MessageTextBox.Text += $"Vrchol {vertexId} již existuje.\n";
            }
        }

        private void AddPointToGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(PointXTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(PointYTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
            {
                var value = PointValueComboBox.SelectedItem as string; 
                if (!string.IsNullOrEmpty(value))
                {
                    if (graph.FindVertex(value) != null)
                    {
                        gridIndex.AddPoint(value, x, y);
                        bool saved = gridIndex.Save();     
                        MessageTextBox.Text = $"Bod ({x}, {y}) s ID '{value}' byl přidán do gridu. Uložení: {(saved ? "OK" : "Chyba")}";
                       
                    }
                    else
                    {
                        MessageTextBox.Text = $"Chyba: Vrchol s ID '{value}' neexistuje v grafu.";
                    }
                }
                else
                {
                    MessageTextBox.Text = "Prosím vyberte platné ID vrcholu z ComboBoxu.";
                }
            }
            else
            {
                MessageTextBox.Text = "Prosím zadejte platné číselné souřadnice X a Y.";
            }
        }

        private void FindPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(SearchXTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(SearchYTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double y))
            {
                var pointId = gridIndex.FindPoint(x, y); 
                if (pointId != null)
                {
                    var vertexData = graph.FindVertex(pointId);
                    MessageTextBox.Text = $"Bod na souřadnicích ({x}, {y}) nalezen.\nID: '{pointId}'\nData vrcholu: '{vertexData ?? "N/A"}'";
                }
                else
                {
                    MessageTextBox.Text = $"Bod na přesných souřadnicích ({x}, {y}) nebyl v gridu nalezen.";
                }
            }
            else
            {
                MessageTextBox.Text = "Prosím zadejte platné číselné souřadnice X a Y pro hledání.";
            }
        }


        private void FindIntervalButton_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(IntervalX1TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double x1) &&
                double.TryParse(IntervalY1TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double y1) &&
                double.TryParse(IntervalX2TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double x2) &&
                double.TryParse(IntervalY2TextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double y2))
            {
                if (x1 > x2) Swap(ref x1, ref x2);
                if (y1 > y2) Swap(ref y1, ref y2);

                AreaSearchResult<string> searchResult = gridIndex.FindPointsInArea(x1, x2, y1, y2);

                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"Výsledky hledání v intervalu X:[{x1}, {x2}], Y:[{y1}, {y2}]:");
                messageBuilder.AppendLine("---");

                if (searchResult.CheckedCellIndices.Any())
                {
                    string checkedCellsString = string.Join(", ",
                        searchResult.CheckedCellIndices.Select(cellIndex => $"[{cellIndex.XIndex}][{cellIndex.YIndex}]"));
                    messageBuilder.AppendLine($"Prohledané bloky (indexy [sloupec][řádek]): {checkedCellsString}");
                }
                else
                {
                    messageBuilder.AppendLine("Nebyly prohledány žádné bloky (oblast mimo mřížku?).");
                }
                messageBuilder.AppendLine("---");

                if (searchResult.FoundPoints.Any())
                {
                    messageBuilder.AppendLine($"Nalezeno {searchResult.FoundPoints.Count} bodů:");
                    foreach (string pointId in searchResult.FoundPoints)
                    {
                        messageBuilder.AppendLine($"- ID: {pointId}");
                    }
                }
                else
                {
                    messageBuilder.AppendLine("V zadané oblasti nebyly nalezeny žádné body.");
                }

                MessageTextBox.Text = messageBuilder.ToString();
            }
            else
            {
                MessageTextBox.Text = "Prosím zadejte platné číselné souřadnice pro interval.";
            }
        }

        private void Swap(ref double a, ref double b)
        {
            double temp = a;
            a = b;
            b = temp;
        }

        private void PrintLinesButton_Click(object sender, RoutedEventArgs e)
        {
            var xLines = gridIndex.XLines;
            var yLines = gridIndex.YLines;

            if (xLines == null || yLines == null)
            {
                MessageTextBox.Text = "Chyba: Seznamy čar nebyly inicializovány.";
                return;
            }

            var verticalLinesText = string.Join("\n", xLines.Select(line => $"Vertikální čára na x = {line.ToString(CultureInfo.InvariantCulture)}"));
            var horizontalLinesText = string.Join("\n", yLines.Select(line => $"Horizontální čára na y = {line.ToString(CultureInfo.InvariantCulture)}"));

            MessageTextBox.Text = $"Vertikální dělící čáry (X Lines):\n{verticalLinesText}\n\n"
                                + $"Horizontální dělící čáry (Y Lines):\n{horizontalLinesText}";
        }

        private void PrintGridButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text = gridIndex?.ToString() ?? "GridIndex není inicializován.";
        }



        private void SaveGraphToFile(string filePath) 
        {
            try
            {
                string graphData = graph.PrintRawGraph(); 
                File.WriteAllText(filePath, graphData);
                MessageTextBox.Text = "Graf byl úspěšně uložen do souboru.";
            }
            catch (Exception ex)
            {
                MessageTextBox.Text = $"Chyba při ukládání grafu: {ex.Message}";
            }
        }

        private void AddVertexButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Přidat vrchol",
                Content = new AddVertexDialog(),
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                var addVertexDialog = (AddVertexDialog)dialog.Content;
                var vertexId = addVertexDialog.VertexId;
                var vertexData = addVertexDialog.VertexData;


                if (graph.AddVertex(vertexId, vertexData))
                {
                    MessageTextBox.Text = $"Vrchol {vertexId} přidán do grafu. POZOR: Nebyl přidán do gridu (chybí souřadnice).";
                    UpdateUIComboBoxes();
                }
                else
                {
                    MessageTextBox.Text = $"Vrchol {vertexId} již existuje.";
                }
            }
        }


        private void AddEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Window
            {
                Title = "Přidat hranu",
                Content = new AddEdgeDialog(graph), 
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            if (dialog.ShowDialog() == true)
            {
                var addEdgeDialog = (AddEdgeDialog)dialog.Content;
                var startVertex = addEdgeDialog.StartVertex;
                var endVertex = addEdgeDialog.EndVertex;
                if (int.TryParse(addEdgeDialog.Weight, NumberStyles.Integer, CultureInfo.InvariantCulture, out int weight))
                {
                    if (graph.AddEdge(startVertex, endVertex, weight))
                    {
                        MessageTextBox.Text = $"Hrana přidána z {startVertex} do {endVertex} s váhou {weight}.";
                        UpdateUIComboBoxes(); 
                    }
                    else
                    {
                        MessageTextBox.Text = $"Hranu z {startVertex} do {endVertex} nelze přidat (možná již existuje?).";
                    }
                }
                else
                {
                    MessageTextBox.Text = "Neplatná váha hrany.";
                }
            }
        }


        private void RemoveEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedEdge = EdgeComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedEdge))
            {
                MessageTextBox.Text = "Prosím vyberte hranu k odebrání.";
                return;
            }

            string[] parts = selectedEdge.Split(new[] { " <-> ", " (" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2 && graph.RemoveEdge(parts[0], parts[1]))
            {
                MessageTextBox.Text = $"Hrana mezi {parts[0]} a {parts[1]} byla odebrána.";
                UpdateUIComboBoxes(); 
            }
            else if (parts.Length >= 2)
            {
                MessageTextBox.Text = $"Hranu mezi {parts[0]} a {parts[1]} nelze odebrat (možná neexistuje?).";
            }
            else
            {
                MessageTextBox.Text = "Nelze zpracovat vybranou hranu.";
            }
        }

        private void BlockEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedEdge = EdgeComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedEdge))
            {
                MessageTextBox.Text = "Prosím vyberte hranu pro změnu dostupnosti.";
                return;
            }

            string[] parts = selectedEdge.Split(new[] { " <-> ", " (" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 2)
            {
                bool currentState = graph.ChangeAccessibility(parts[0], parts[1]); 
                MessageTextBox.Text = $"Dostupnost hrany mezi {parts[0]} a {parts[1]} změněna. Nový stav: {(currentState ? "Dostupná" : "Nedostupná")}.";

                UpdateUIComboBoxes();
            }
            else
            {
                MessageTextBox.Text = "Nelze zpracovat vybranou hranu.";
            }
        }

        private void DijkstraToButton_Click(object sender, RoutedEventArgs e)
        {
            var startVertex = StartVertexComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(startVertex))
            {
                MessageTextBox.Text = "Prosím vyberte počáteční vrchol.";
                return;
            }
            try
            {
                graph.FindShortestPaths(startVertex);
                var vector = graph.PrintPredecessors();
                var pathsTable = graph.PrintShortestPathsTable(); 

                MessageTextBox.Text = "Výsledky Dijkstrova algoritmu:\n"
                                    + "Tabulka nejkratších cest:\n" + pathsTable
                                    + "\n\nVektor předchůdců:\n" + vector;
            }
            catch (Exception ex)
            {
                MessageTextBox.Text = $"Chyba při provádění Dijkstrova algoritmu: {ex.Message}";
            }
        }

        private void PrintGraphButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text = graph?.PrintGraph() ?? "Graf není inicializován."; 
        }

        private void ClearGraphButton_Click(object sender, RoutedEventArgs e)
        {
            graph = new DijkstraGraph<string, string, int>();
            gridIndex = new GridIndex<string>(30, GridDataFilePath, GridMinX, GridMaxX, GridMinY, GridMaxY);

            try
            {
                if (File.Exists(GridDataFilePath)) File.Delete(GridDataFilePath);
            }
            catch (Exception ex)
            {
                MessageTextBox.Text = $"Varování: Nepodařilo se smazat soubor gridu '{GridDataFilePath}': {ex.Message}\n";
            }

            UpdateUIComboBoxes();
            MessageTextBox.Text = "Graf a GridIndex byly resetovány. Program je připraven na nový graf.";
        }

        private void SearchVertexButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedVertexId = StartVertexComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedVertexId))
            {
                MessageTextBox.Text = "Prosím vyberte vrchol pro vyhledání.";
                return;
            }

            var vertexData = graph.FindVertex(selectedVertexId);
            if (vertexData == null)
            {
                MessageTextBox.Text = $"Vrchol '{selectedVertexId}' nebyl v grafu nalezen.";
                return;
            }

            var edges = graph.GetEdgesAsString()
                             .Where(edgeStr => edgeStr.Contains($" {selectedVertexId} ") || edgeStr.StartsWith($"{selectedVertexId} ") || edgeStr.EndsWith($" {selectedVertexId}(")) // Hrubý odhad filtru
                             .ToList();

            var edgesInfo = edges.Any() ? string.Join("\n", edges) : "Žádné připojené hrany.";

            MessageTextBox.Text = $"Informace o vrcholu:\n"
                                + $"ID: {selectedVertexId}\n"
                                + $"Data: {vertexData}\n"
                                + $"Připojené hrany:\n{edgesInfo}";
        }

        private void UpdateUIComboBoxes()
        {
            var vertices = graph?.GetVertices() ?? new List<string>();
            StartVertexComboBox.ItemsSource = vertices;
            PointValueComboBox.ItemsSource = vertices;
            EdgeComboBox.ItemsSource = graph?.GetEdgesAsString() ?? new List<string>(); 
        }
    }
}