using System.Drawing.Printing;
using System.Windows;
using Cocheras.Models;
using Cocheras.Services;

namespace Cocheras.Windows
{
    public partial class InfoEstacionamientoWindow : Window
    {
        private readonly DatabaseService _dbService;

        public InfoEstacionamientoWindow(DatabaseService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
            CargarDatos();
            this.Loaded += InfoEstacionamientoWindow_Loaded;
        }

        private void InfoEstacionamientoWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Activate();
            this.Focus();
            if (TxtNombre != null)
            {
                TxtNombre.Focus();
            }
        }

        private void CargarDatos()
        {
            // Cargar países
            var paises = new List<string>
            {
                "Argentina", "Bolivia", "Brasil", "Chile", "Colombia",
                "Costa Rica", "Cuba", "Ecuador", "El Salvador", "Guatemala",
                "Honduras", "México", "Nicaragua", "Panamá", "Paraguay",
                "Perú", "República Dominicana", "Uruguay", "Venezuela", "España"
            };
            CmbPais.ItemsSource = paises;
            CmbPais.SelectedItem = "Argentina";

            // Cargar impresoras
            var impresoras = new List<string> { "-- SELECCIONAR --" };
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                impresoras.Add(printer);
            }
            CmbImpresora.ItemsSource = impresoras;
            CmbImpresora.SelectedIndex = 0;
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnContinuar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                MessageBox.Show("Por favor ingrese el nombre del estacionamiento.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbPais.SelectedItem == null)
            {
                MessageBox.Show("Por favor seleccione un país.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbImpresora.SelectedItem == null || CmbImpresora.SelectedIndex == 0)
            {
                MessageBox.Show("Por favor seleccione una impresora.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var estacionamiento = new Estacionamiento
                {
                    Nombre = TxtNombre.Text.Trim(),
                    Direccion = TxtDireccion.Text.Trim(),
                    Ciudad = TxtCiudad.Text.Trim(),
                    Pais = CmbPais.SelectedItem.ToString() ?? "Argentina",
                    Telefono = string.IsNullOrWhiteSpace(TxtTelefono.Text) ? null : TxtTelefono.Text.Trim(),
                    Impresora = CmbImpresora.SelectedItem.ToString() ?? string.Empty
                };

                _dbService.GuardarEstacionamiento(estacionamiento);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la información: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

