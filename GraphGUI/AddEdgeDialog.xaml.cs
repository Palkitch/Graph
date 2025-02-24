using Graph;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GraphGUI
{
    public partial class AddEdgeDialog : UserControl
    {
        public string StartVertex => StartVertexComboBox.SelectedItem as string;
        public string EndVertex => EndVertexComboBox.SelectedItem as string;
        public string Weight => WeightTextBox.Text;

        public AddEdgeDialog(DijkstraGraph<string, string, int> graph)
        {
            InitializeComponent();
            StartVertexComboBox.ItemsSource = graph.GetVertices();
            EndVertexComboBox.ItemsSource = graph.GetVertices();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(StartVertex) || string.IsNullOrEmpty(EndVertex) || string.IsNullOrEmpty(Weight))
            {
                MessageBox.Show("Prosím vyplňte všechna pole.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (!int.TryParse(Weight, out _) && !double.TryParse(Weight, out _))
            {
                MessageBox.Show("Ohodnocení hrany musí být číslo.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Window.GetWindow(this).DialogResult = true;
                Window.GetWindow(this).Close();
            }
        }
    }
}
