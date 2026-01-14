using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Cocheras.Services;

namespace Cocheras
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _dbService;

        public MainWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            LoadBackgroundImage();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            CheckInitialSetup();
        }

        public void VerificarYMostrarCrearAdmin()
        {
            // Método público para verificar y mostrar crear admin si es necesario
            if (!_dbService.ExisteAdmin())
            {
                CheckInitialSetup();
            }
        }

        private void LoadBackgroundImage()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // Buscar imagen en Assets/Images (donde se copia como Content)
            string imagePath = Path.Combine(baseDirectory, "Assets", "Images", "background.jpg");
            
            if (!File.Exists(imagePath))
            {
                // Intentar también en la raíz
                imagePath = Path.Combine(baseDirectory, "background.jpg");
            }

            if (File.Exists(imagePath))
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(imagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    BackgroundImage.Source = bitmap;
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cargando imagen: {ex.Message}");
                }
            }

            // Si no se encuentra la imagen, crear un gradiente oscuro tipo estacionamiento nocturno
            CreateDefaultBackground();
        }

        private void CreateDefaultBackground()
        {
            var drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                // Gradiente oscuro de azul a negro (tipo estacionamiento nocturno)
                var gradient = new LinearGradientBrush(
                    Color.FromRgb(20, 30, 50),
                    Color.FromRgb(10, 15, 25),
                    new Point(0, 0),
                    new Point(0, 1));
                drawingContext.DrawRectangle(gradient, null, new Rect(0, 0, 1920, 1080));
            }

            var rtb = new RenderTargetBitmap(1920, 1080, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(drawingVisual);
            rtb.Freeze();
            BackgroundImage.Source = rtb;
        }

        public void CheckInitialSetup()
        {
            if (!_dbService.ExisteAdmin())
            {
                // Mostrar ventana de creación de administrador
                var crearAdminWindow = new Windows.CrearAdminWindow(_dbService);
                crearAdminWindow.Owner = this;
                crearAdminWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                crearAdminWindow.Topmost = true;
                crearAdminWindow.ShowActivated = true;
                bool? resultadoAdmin = crearAdminWindow.ShowDialog();
                
                // Asegurar que la ventana se cierre completamente
                crearAdminWindow.Close();
                crearAdminWindow = null;
                
                if (resultadoAdmin == true)
                {
                    // Después de crear admin, verificar si necesita info del estacionamiento
                    if (!_dbService.ExisteEstacionamiento())
                    {
                        var infoEstacionamientoWindow = new Windows.InfoEstacionamientoWindow(_dbService);
                        infoEstacionamientoWindow.Owner = this;
                        infoEstacionamientoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        infoEstacionamientoWindow.Topmost = true;
                        infoEstacionamientoWindow.ShowActivated = true;
                        bool? resultadoEstacionamiento = infoEstacionamientoWindow.ShowDialog();
                        
                        // Asegurar que la ventana se cierre completamente
                        infoEstacionamientoWindow.Close();
                        infoEstacionamientoWindow = null;
                        
                        if (resultadoEstacionamiento == true)
                        {
                            // Después de completar info, mostrar login
                            MostrarLogin();
                        }
                    }
                    else
                    {
                        MostrarLogin();
                    }
                }
            }
            else if (!_dbService.ExisteEstacionamiento())
            {
                // Mostrar ventana de información del estacionamiento
                var infoEstacionamientoWindow = new Windows.InfoEstacionamientoWindow(_dbService);
                infoEstacionamientoWindow.Owner = this;
                infoEstacionamientoWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                infoEstacionamientoWindow.Topmost = true;
                infoEstacionamientoWindow.ShowActivated = true;
                bool? resultadoEstacionamiento = infoEstacionamientoWindow.ShowDialog();
                
                // Asegurar que la ventana se cierre completamente
                infoEstacionamientoWindow.Close();
                infoEstacionamientoWindow = null;
                
                if (resultadoEstacionamiento == true)
                {
                    MostrarLogin();
                }
            }
            else
            {
                MostrarLogin();
            }
        }

        private void MostrarLogin()
        {
            var loginWindow = new Windows.LoginWindow(_dbService);
            loginWindow.Owner = this;
            loginWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            bool? resultado = loginWindow.ShowDialog();
            
            if (resultado == true && !string.IsNullOrEmpty(loginWindow.UsernameLogueado))
            {
                // Mostrar ventana principal (Home)
                var homeWindow = new Windows.HomeWindow(loginWindow.UsernameLogueado, _dbService);
                homeWindow.Owner = this;
                homeWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                homeWindow.Show();
                this.Hide();
            }
            else
            {
                // Si se canceló el login (puede ser por reinicio de BD), verificar SIEMPRE si hay admin
                // Si no hay admin, mostrar la creación de admin INMEDIATAMENTE
                System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action(() => 
                    {
                        if (!_dbService.ExisteAdmin())
                        {
                            CheckInitialSetup();
                        }
                    }),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }
    }
}