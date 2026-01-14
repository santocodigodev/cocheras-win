using System.Windows;
using System.Windows.Input;
using Cocheras.Services;
using Cocheras.Models;

namespace Cocheras.Windows
{
    public partial class LoginWindow : Window
    {
        private readonly DatabaseService _dbService;
        public string? UsernameLogueado { get; private set; }

        public LoginWindow(DatabaseService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
            TxtUsuario.Focus();

            // Cargar nombre de empresa para mostrar debajo de "Parking"
            try
            {
                var est = _dbService.ObtenerEstacionamiento();
                if (est != null && !string.IsNullOrWhiteSpace(est.Nombre) && TxtNombreEmpresaLogin != null)
                {
                    TxtNombreEmpresaLogin.Text = est.Nombre;
                }
            }
            catch
            {
                // ignorar errores de lectura; dejar texto por defecto
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Shift+R para reiniciar la base de datos
            if (e.Key == Key.R && 
                Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                BtnReiniciarBD_Click(sender, e);
                e.Handled = true;
            }
        }

        private void BtnReiniciarBD_Click(object sender, RoutedEventArgs e)
        {
            // Mostrar confirmación
            var resultado = MessageBox.Show(
                "¿Está seguro que desea reiniciar la base de datos?\n\n" +
                "Esta acción eliminará TODOS los datos:\n" +
                "- Administradores\n" +
                "- Información del estacionamiento\n" +
                "- Todos los tickets\n" +
                "- Todas las categorías\n\n" +
                "Esta acción NO se puede deshacer.",
                "Confirmar Reinicio de Base de Datos",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    // Reiniciar la base de datos
                    _dbService.ReiniciarBaseDatos();
                    
                    MessageBox.Show(
                        "La base de datos ha sido reiniciada correctamente.\n" +
                        "Será redirigido a la pantalla de creación de administrador.",
                        "Base de Datos Reiniciada",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Cerrar esta ventana
                    DialogResult = false;
                    Close();
                    
                    // Notificar al MainWindow que debe mostrar crear admin
                    if (this.Owner is MainWindow mainWindow)
                    {
                        // Esperar un momento para que la ventana se cierre completamente
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                            new Action(() => 
                            {
                                mainWindow.VerificarYMostrarCrearAdmin();
                            }),
                            System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error al reiniciar la base de datos:\n{ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnIngresar_Click(object sender, RoutedEventArgs e)
        {
            ValidarLogin();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ValidarLogin();
            }
        }

        private void ValidarLogin()
        {
            if (string.IsNullOrWhiteSpace(TxtUsuario.Text))
            {
                MessageBox.Show("Por favor ingrese su usuario.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUsuario.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtPassword.Password))
            {
                MessageBox.Show("Por favor ingrese su contraseña.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtPassword.Focus();
                return;
            }

            try
            {
                if (_dbService.ValidarLogin(TxtUsuario.Text, TxtPassword.Password))
                {
                    UsernameLogueado = TxtUsuario.Text;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    TxtPassword.Clear();
                    TxtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al validar el login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LnkRecuperar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Funcionalidad de recuperación de contraseña próximamente disponible
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(30, 58, 138));
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.TextBox textBox)
            {
                textBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(209, 213, 219));
            }
        }

        private void PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                passwordBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(30, 58, 138));
            }
        }

        private void PasswordBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.PasswordBox passwordBox)
            {
                passwordBox.BorderBrush = new System.Windows.Media.SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(209, 213, 219));
            }
        }
    }
}

