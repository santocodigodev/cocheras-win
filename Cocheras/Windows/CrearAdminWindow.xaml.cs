using System.Windows;
using System.Windows.Input;
using Cocheras.Models;
using Cocheras.Services;

namespace Cocheras.Windows
{
    public partial class CrearAdminWindow : Window
    {
        private readonly DatabaseService _dbService;

        public CrearAdminWindow(DatabaseService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
            this.Loaded += CrearAdminWindow_Loaded;
        }

        private void CrearAdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Activate();
            this.Focus();
            if (TxtNombre != null)
            {
                TxtNombre.Focus();
            }
        }

        private void BtnCerrar_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnCrear_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtNombre.Text))
            {
                MessageBox.Show("Por favor ingrese el nombre.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtApellido.Text))
            {
                MessageBox.Show("Por favor ingrese el apellido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtUsername.Text) || TxtUsername.Text.Length < 4)
            {
                MessageBox.Show("El nombre de usuario debe contener al menos 4 caracteres.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TxtPassword.Password.Length < 6)
            {
                MessageBox.Show("La contraseña debe contener al menos 6 caracteres.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TxtPassword.Password != TxtPasswordRepeat.Password)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtEmail.Text) || !TxtEmail.Text.Contains("@"))
            {
                MessageBox.Show("Por favor ingrese un email válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var admin = new Admin
                {
                    Nombre = TxtNombre.Text.Trim(),
                    Apellido = TxtApellido.Text.Trim(),
                    Username = TxtUsername.Text.Trim(),
                    PasswordHash = TxtPassword.Password,
                    Email = TxtEmail.Text.Trim()
                };

                _dbService.CrearAdmin(admin);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear el administrador: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

