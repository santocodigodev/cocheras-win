using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cocheras.Models;
using Cocheras.Services;

namespace Cocheras.Windows
{
    public partial class ConfiguracionWindow : Window
    {
        private readonly DatabaseService _dbService;
        private ObservableCollection<Categoria> _categorias;
        private Categoria? _categoriaArrastrando;
        private Point _puntoInicioArrastre;

        public ConfiguracionWindow(DatabaseService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
            _categorias = new ObservableCollection<Categoria>();
            
            CargarCategorias();
        }

        private void CargarCategorias()
        {
            _categorias.Clear();
            var categorias = _dbService.ObtenerCategorias();
            foreach (var categoria in categorias)
            {
                _categorias.Add(categoria);
            }
            ItemsCategoriasLista.ItemsSource = _categorias;
        }

        private void ResetearBotonesMenu()
        {
            var azul = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            var transparente = System.Windows.Media.Brushes.Transparent;
            var gris = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(51, 65, 85));

            BtnCategorias.Background = transparente;
            BtnCategorias.Foreground = gris;
            BtnTarifas.Background = transparente;
            BtnTarifas.Foreground = gris;
            BtnPagos.Background = transparente;
            BtnPagos.Foreground = gris;
            BtnUsuarios.Background = transparente;
            BtnUsuarios.Foreground = gris;
            BtnMediosPago.Background = transparente;
            BtnMediosPago.Foreground = gris;
            BtnInformes.Background = transparente;
            BtnInformes.Foreground = gris;
            BtnAjustes.Background = transparente;
            BtnAjustes.Foreground = gris;
        }

        private void MostrarPanel(string panelNombre)
        {
            PanelCategorias.Visibility = panelNombre == "Categorias" ? Visibility.Visible : Visibility.Collapsed;
            PanelTarifas.Visibility = panelNombre == "Tarifas" ? Visibility.Visible : Visibility.Collapsed;
            PanelPagos.Visibility = panelNombre == "Pagos" ? Visibility.Visible : Visibility.Collapsed;
            PanelUsuarios.Visibility = panelNombre == "Usuarios" ? Visibility.Visible : Visibility.Collapsed;
            PanelMediosPago.Visibility = panelNombre == "MediosPago" ? Visibility.Visible : Visibility.Collapsed;
            PanelInformes.Visibility = panelNombre == "Informes" ? Visibility.Visible : Visibility.Collapsed;
            PanelAjustes.Visibility = panelNombre == "Ajustes" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void BtnCategorias_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesMenu();
            BtnCategorias.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnCategorias.Foreground = System.Windows.Media.Brushes.White;
            MostrarPanel("Categorias");
        }

        private void BtnTarifas_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesMenu();
            BtnTarifas.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnTarifas.Foreground = System.Windows.Media.Brushes.White;
            MostrarPanel("Tarifas");
        }

        private void BtnPagos_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesMenu();
            BtnPagos.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnPagos.Foreground = System.Windows.Media.Brushes.White;
            MostrarPanel("Pagos");
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesMenu();
            BtnUsuarios.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnUsuarios.Foreground = System.Windows.Media.Brushes.White;
            MostrarPanel("Usuarios");
        }

        private void BtnMediosPago_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesMenu();
            BtnMediosPago.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnMediosPago.Foreground = System.Windows.Media.Brushes.White;
            MostrarPanel("MediosPago");
        }

        private void BtnInformes_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesMenu();
            BtnInformes.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnInformes.Foreground = System.Windows.Media.Brushes.White;
            MostrarPanel("Informes");
        }

        private void BtnAjustes_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesMenu();
            BtnAjustes.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnAjustes.Foreground = System.Windows.Media.Brushes.White;
            MostrarPanel("Ajustes");
        }

        private void BtnNuevaCategoria_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new CategoriaWindow();
            ventana.Owner = this;
            ventana.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            if (ventana.ShowDialog() == true && !string.IsNullOrWhiteSpace(ventana.NombreCategoria))
            {
                try
                {
                    var nuevaCategoria = new Categoria
                    {
                        Nombre = ventana.NombreCategoria.Trim()
                    };
                    _dbService.CrearCategoria(nuevaCategoria);
                    CargarCategorias();
                    NotificarCambioCategorias();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al crear categoría: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnModificarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Categoria categoria)
            {
                var ventana = new CategoriaWindow(categoria);
                ventana.Owner = this;
                ventana.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                if (ventana.ShowDialog() == true && !string.IsNullOrWhiteSpace(ventana.NombreCategoria))
                {
                    try
                    {
                        categoria.Nombre = ventana.NombreCategoria.Trim();
                        _dbService.ActualizarCategoria(categoria);
                        CargarCategorias();
                        NotificarCambioCategorias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al modificar categoría: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnEliminarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Categoria categoria)
            {
                var resultado = MessageBox.Show(
                    $"¿Está seguro que desea eliminar la categoría '{categoria.Nombre}'?",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);

                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dbService.EliminarCategoria(categoria.Id);
                        CargarCategorias();
                        // Notificar al HomeWindow para actualizar el footer
                        NotificarCambioCategorias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar categoría: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // Drag and Drop
        private void CategoriaItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Categoria categoria)
            {
                _categoriaArrastrando = categoria;
                _puntoInicioArrastre = e.GetPosition(border);
                border.CaptureMouse();
            }
        }

        private void CategoriaItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (_categoriaArrastrando != null && e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is Border border)
                {
                    Point currentPosition = e.GetPosition(border);
                    if (Math.Abs(currentPosition.Y - _puntoInicioArrastre.Y) > 5)
                    {
                        DragDrop.DoDragDrop(border, _categoriaArrastrando, DragDropEffects.Move);
                    }
                }
            }
        }

        private void CategoriaItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                border.ReleaseMouseCapture();
                _categoriaArrastrando = null;
            }
        }

        private void CategoriaItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Categoria)))
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }

        private void CategoriaItem_Drop(object sender, DragEventArgs e)
        {
            if (sender is Border border && 
                border.DataContext is Categoria categoriaDestino &&
                e.Data.GetData(typeof(Categoria)) is Categoria categoriaOrigen)
            {
                if (categoriaOrigen.Id != categoriaDestino.Id)
                {
                    // Reordenar en la colección
                    int indiceOrigen = _categorias.IndexOf(categoriaOrigen);
                    int indiceDestino = _categorias.IndexOf(categoriaDestino);
                    
                    _categorias.RemoveAt(indiceOrigen);
                    _categorias.Insert(indiceDestino, categoriaOrigen);
                    
                    // Actualizar orden en la base de datos
                    try
                    {
                        _dbService.ActualizarOrdenCategorias(_categorias.ToList());
                        CargarCategorias(); // Recargar para asegurar sincronización
                        // Notificar al HomeWindow para actualizar el footer
                        NotificarCambioCategorias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al actualizar orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        CargarCategorias(); // Recargar en caso de error
                    }
                }
            }
            _categoriaArrastrando = null;
        }

        private void NotificarCambioCategorias()
        {
            // Notificar al HomeWindow que las categorías han cambiado
            var homeWindow = Application.Current.Windows.OfType<HomeWindow>().FirstOrDefault();
            if (homeWindow != null)
            {
                homeWindow.ActualizarCategorias();
            }
        }
    }
}

