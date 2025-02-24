using System.Windows;
using System.Windows.Controls;

namespace GraphGUI
{
    public partial class AddVertexDialog : UserControl
    {
        public string VertexId => IdTextBox.Text;
        public string VertexData => DataTextBox.Text;

        public AddVertexDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(VertexId) || string.IsNullOrEmpty(VertexData))
            {
                MessageBox.Show("Prosím vyplňte všechna pole.", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Window.GetWindow(this).DialogResult = true;
                Window.GetWindow(this).Close();
            }
        }
    }
}
