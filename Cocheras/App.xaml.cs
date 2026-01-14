using System.Configuration;
using System.Data;
using System.Windows;
using Cocheras.Utils;
using Cocheras.Helpers;

namespace Cocheras
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Configurar emulación IE11 para WebBrowser (si se usa como fallback)
            // Nota: No es necesario con WebView2, pero útil si se vuelve a usar WebBrowser
            try
            {
                InternetExplorerEmulationHelper.SetIE11Emulation();
                System.Diagnostics.Debug.WriteLine("Emulación IE11 configurada correctamente");
            }
            catch (Exception ex)
            {
                // Si falla, no es crítico - WebView2 es la solución principal
                System.Diagnostics.Debug.WriteLine($"No se pudo configurar emulación IE11: {ex.Message}");
            }
        }
    }

}
