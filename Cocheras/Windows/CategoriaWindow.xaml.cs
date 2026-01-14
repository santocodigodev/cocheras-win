using System.Windows;
using Cocheras.Models;

namespace Cocheras.Windows
{
    public partial class CategoriaWindow : Window
    {
        public string Titulo { get; set; } = "Nueva Categoría";
        public string NombreCategoria { get; set; } = string.Empty;
        public Categoria? CategoriaEditando { get; private set; }

        public CategoriaWindow(Categoria? categoria = null)
        {
            InitializeComponent();
            DataContext = this;

            if (categoria != null)
            {
                CategoriaEditando = categoria;
                Titulo = "Modificar Categoría";
                NombreCategoria = categoria.Nombre;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NombreCategoria))
            {
                MessageBox.Show("El nombre de la categoría no puede estar vacío.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

