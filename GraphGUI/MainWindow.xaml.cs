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
        private Program p = new Program();

        public MainWindow()
        {
            graph = p.GrafZadani();
            InitializeComponent();
            //SaveGraphToFile("grafy/graf.txt");
            //LoadGraphFromFile("grafy/graf.txt");
            StartVertexComboBox.ItemsSource = graph.GetVertices();
            UpdateEdgeComboBox();
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

        private void UpdateEdgeComboBox()
        {
            EdgeComboBox.ItemsSource = graph.GetEdgesAsString();
        }
    }
}

