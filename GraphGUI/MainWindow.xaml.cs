using Graph;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Windows;

namespace GraphGUI
{
    public partial class MainWindow : Window
    {
        private DijkstraGraph<string, string, int> graph = new DijkstraGraph<string, string, int>();
        private GridIndex<string> gridIndex = new GridIndex<string>(100, 100);

        public MainWindow()
        {
            InitializeComponent();
            //LoadGraphFromFile("grafy/graphZadani.txt");
            //LoadGraphFromFile("grafy/randomGraph.txt");
            LoadGraphAndGridFromCSVFile("grafy/gridIndexZadani.txt");
            StartVertexComboBox.ItemsSource = graph.GetVertices();
            PointValueComboBox.ItemsSource = graph.GetVertices(); // Naplnění ComboBoxu vrcholy z grafu
            UpdateEdgeComboBox();

            // Načtení CSV dat ze souboru
        }

        private void LoadGraphAndGridFromCSVFile(string filePath)
        {
            try
            {
                string csvData = File.ReadAllText(filePath);
                LoadGraphAndGridFromCSV(csvData);
                MessageTextBox.Text += "Data z CSV byla úspěšně načtena.\n";
            }
            catch (Exception ex)
            {
                MessageTextBox.Text += $"Chyba při načítání CSV dat: {ex.Message}\n";
            }
        }

        private void LoadGraphAndGridFromCSV(string csvData)
        {
            var lines = csvData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length == 4)
                {
                    var id = parts[0];
                    var data = parts[1];
                    if (int.TryParse(parts[2], out int x) && int.TryParse(parts[3], out int y))
                    {
                        AddVertex(graph, gridIndex, id, data, x, y);
                    }
                }
            }
        }

        private void AddVertex(DijkstraGraph<string, string, int> graph, GridIndex<string> gridIndex, string vertexId, string vertexData, int x, int y)
        {
            if (graph.AddVertex(vertexId, vertexData))
            {
                gridIndex.AddPoint(x, y, vertexId);
                MessageTextBox.Text += $"Vrchol {vertexId} přidán na souřadnice ({x}, {y}).\n";
            }
            else
            {
                MessageTextBox.Text += $"Vrchol {vertexId} již existuje.\n";
            }
        }
        private void AddPointToGridButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(PointXTextBox.Text, out int x) && int.TryParse(PointYTextBox.Text, out int y))
            {
                var value = PointValueComboBox.SelectedItem as string;
                if (value != null)
                {
                    gridIndex.AddPoint(x, y, value);
                    MessageTextBox.Text = $"Bod ({x}, {y}) s hodnotou '{value}' byl přidán do gridu.";
                }
                else
                {
                    MessageTextBox.Text = "Prosím vyberte platnou hodnotu z ComboBoxu.";
                }
            }
            else
            {
                MessageTextBox.Text = "Prosím zadejte platné souřadnice X a Y.";
            }
        }

        private void FindPointButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(SearchXTextBox.Text, out int x) && int.TryParse(SearchYTextBox.Text, out int y))
            {
                var point = gridIndex.FindPointBetweenLines(x, y);
                if (point != null)
                {
                    MessageTextBox.Text = $"Bod na souřadnicích ({x}, {y}) má hodnotu '{point}' ({x}, {y}).";
                }
                else
                {
                    MessageTextBox.Text = $"Bod na souřadnicích ({x}, {y}) nebyl nalezen.";
                }
            }
            else
            {
                MessageTextBox.Text = "Prosím zadejte platné souřadnice X a Y.";
            }
        }

        private void FindIntervalButton_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(IntervalX1TextBox.Text, out int x1) && int.TryParse(IntervalY1TextBox.Text, out int y1) &&
                int.TryParse(IntervalX2TextBox.Text, out int x2) && int.TryParse(IntervalY2TextBox.Text, out int y2))
            {
                var points = gridIndex.FindPointsInArea(x1, y1, x2, y2);
                if (points.Any())
                {
                    var result = points.Select(p => $"ID: {p.Id}, Souřadnice: ({p.X}, {p.Y})").ToList();
                    MessageTextBox.Text = $"Body v intervalu ({x1}, {y1}) - ({x2}, {y2}):\n" + string.Join("\n", result);
                }
                else
                {
                    MessageTextBox.Text = $"V intervalu ({x1}, {y1}) - ({x2}, {y2}) nebyly nalezeny žádné body.";
                }
            }
            else
            {
                MessageTextBox.Text = "Prosím zadejte platné souřadnice pro interval.";
            }
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

        private void LoadGraphFromFile(string filePath)
        {
            try
            {
                graph = new DijkstraGraph<string, string, int>();

                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts[0] == "V")
                    {
                        // Načíst vrchol
                        graph.AddVertex(parts[1], parts[2]);
                    }
                    else if (parts[0] == "E")
                    {
                        // Načíst hranu
                        var from = parts[1];
                        var to = parts[2];
                        var weight = int.Parse(parts[3]);
                        var isAccessible = bool.Parse(parts[4]);
                        graph.AddEdge(from, to, weight);
                        if (!isAccessible)
                        {
                            graph.ChangeAccessibility(from, to);
                        }
                    }
                }
                MessageTextBox.Text = "Graf byl úspěšně načten ze souboru.";
            }
            catch (Exception ex)
            {
                MessageTextBox.Text = $"Chyba při načítání grafu: {ex.Message}";
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
                    MessageTextBox.Text = $"Vrchol {vertexId} přidán.";
                    StartVertexComboBox.ItemsSource = graph.GetVertices(); // Aktualizace ComboBoxu
                    UpdateEdgeComboBox(); // Aktualizace ComboBoxu hrany
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
                var weight = int.Parse(addEdgeDialog.Weight);

                if (graph.AddEdge(startVertex, endVertex, weight))
                {
                    MessageTextBox.Text = $"Hrana přidána z {startVertex} do {endVertex} s ohodnocením {weight}.";
                    UpdateEdgeComboBox(); // Aktualizace ComboBoxu hrany
                }
                else
                {
                    MessageTextBox.Text = $"Hranu z {startVertex} do {endVertex} nelze přidat.";
                }
            }
        }

        private void RemoveEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedEdge = EdgeComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedEdge))
            {
                MessageTextBox.Text = "Prosím vyberte hranu.";
                return;
            }

            var vertices = selectedEdge.Split(" <-> ");
            if (vertices.Length == 2 && graph.RemoveEdge(vertices[0], vertices[1]))
            {
                MessageTextBox.Text = $"Hrana mezi {vertices[0]} a {vertices[1]} byla odebrána.";
                UpdateEdgeComboBox(); // Aktualizace ComboBoxu hrany
            }
            else
            {
                MessageTextBox.Text = $"Hranu mezi {vertices[0]} a {vertices[1]} nelze odebrat.";
            }
        }

        private void BlockEdgeButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedEdge = EdgeComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedEdge))
            {
                MessageTextBox.Text = "Prosím vyberte hranu.";
                return;
            }

            var vertices = selectedEdge.Split(" <-> ");
            if (vertices.Length == 2 && graph.ChangeAccessibility(vertices[0], vertices[1]))
            {
                MessageTextBox.Text = $"Hrana mezi {vertices[0]} a {vertices[1]} byla zablokována.";
            }
            else
            {
                MessageTextBox.Text = $"Hranu mezi {vertices[0]} a {vertices[1]} nelze zablokovat.";
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
            graph.FindShortestPaths(startVertex);
            var vector = graph.PrintPredecessors();
            var adjacencyMatrix = graph.PrintShortestPathsTable();

            MessageTextBox.Text = "Matice sousednosti:\n" + adjacencyMatrix + "\n\nVektor nejkratších cest:\n" + vector;
        }

        private void PrintGraphButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text = graph.PrintGraph();
        }

        private void ClearGraphButton_Click(object sender, RoutedEventArgs e)
        {
            graph = new DijkstraGraph<string, string, int>();
            StartVertexComboBox.ItemsSource = graph.GetVertices();
            UpdateEdgeComboBox();
            MessageTextBox.Text = "Graf byl zrušen a program je připraven na nový graf.";
        }

        private void SearchVertexButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedVertex = StartVertexComboBox.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedVertex))
            {
                MessageTextBox.Text = "Prosím vyberte vrchol.";
                return;
            }

            var vertexData = graph.FindVertex(selectedVertex);
            if (vertexData == null)
            {
                MessageTextBox.Text = $"Vrchol {selectedVertex} nebyl nalezen.";
                return;
            }

            var edges = graph.GetEdgesAsString().Where(e => e.StartsWith(selectedVertex) || e.EndsWith(selectedVertex));
            var edgesInfo = string.Join("\n", edges);

            MessageTextBox.Text = $"Vrchol: {selectedVertex}\nData: {vertexData}\nSousedi:\n{edgesInfo}";
        }

        private void PrintLinesButton_Click(object sender, RoutedEventArgs e)
        {
            var xLines = new StringWriter();
            var yLines = new StringWriter();

            Console.SetOut(xLines);
            gridIndex.PrintXLines();
            Console.SetOut(yLines);
            gridIndex.PrintYLines();

            MessageTextBox.Text = $"Čáry na ose X:\n{xLines}\n\nČáry na ose Y:\n{yLines}";
        }
        private void PrintGridButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTextBox.Text = gridIndex.ToCompactString();
        }


        private void UpdateEdgeComboBox()
        {
            EdgeComboBox.ItemsSource = graph.GetEdgesAsString();
        }
    }
}

