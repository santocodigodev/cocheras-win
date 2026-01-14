using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;

namespace Cocheras.Utils
{
    /// <summary>
    /// Utilidades para configurar el control WebBrowser (IE) y WebView2
    /// </summary>
    public static class WebBrowserHelper
    {
        // Valores de emulación de IE
        // IE11 = 11001 (0x2AF9)
        // IE10 = 10001 (0x2711)
        // IE9 = 9999 (0x270F)
        // IE8 = 8888 (0x22B8)
        // IE7 = 7000 (0x1B58)
        private const int IE11_EMULATION = 11001;
        private const string REGISTRY_KEY = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
        private const string REGISTRY_KEY_64 = @"Software\WOW6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

        /// <summary>
        /// Configura la emulación de IE11 para el ejecutable actual
        /// </summary>
        public static void ConfigurarEmulacionIE11()
        {
            try
            {
                string nombreEjecutable = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? "Cocheras.exe");
                
                // Configurar para arquitectura de 64 bits
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY, true) ?? 
                                         Registry.LocalMachine.CreateSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        key.SetValue(nombreEjecutable, IE11_EMULATION, RegistryValueKind.DWord);
                        System.Diagnostics.Debug.WriteLine($"Emulación IE11 configurada para {nombreEjecutable} (64-bit)");
                    }
                }

                // Configurar para arquitectura de 32 bits (WOW64)
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY_64, true) ?? 
                                         Registry.LocalMachine.CreateSubKey(REGISTRY_KEY_64))
                {
                    if (key != null)
                    {
                        key.SetValue(nombreEjecutable, IE11_EMULATION, RegistryValueKind.DWord);
                        System.Diagnostics.Debug.WriteLine($"Emulación IE11 configurada para {nombreEjecutable} (32-bit)");
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine("No se pudo configurar la emulación IE11: Se requieren permisos de administrador");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al configurar emulación IE11: {ex.Message}");
            }
        }

        /// <summary>
        /// Verifica si la emulación IE11 está configurada
        /// </summary>
        public static bool VerificarEmulacionIE11()
        {
            try
            {
                string nombreEjecutable = System.IO.Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? "Cocheras.exe");
                
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        object value = key.GetValue(nombreEjecutable);
                        if (value != null && Convert.ToInt32(value) == IE11_EMULATION)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar emulación IE11: {ex.Message}");
            }
            return false;
        }
    }
}

