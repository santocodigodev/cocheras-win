using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace Cocheras.Helpers
{
    /// <summary>
    /// Helper para configurar la emulación de Internet Explorer en el registro de Windows.
    /// Útil cuando se usa el control WebBrowser (basado en IE) en lugar de WebView2.
    /// </summary>
    public static class InternetExplorerEmulationHelper
    {
        // Valores de emulación IE
        // IE7 = 7000
        // IE8 = 8000
        // IE9 = 9000
        // IE10 = 10000
        // IE11 = 11000
        private const int IE11_EMULATION_VALUE = 11000;
        private const string REGISTRY_KEY_PATH = @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

        /// <summary>
        /// Configura la emulación de IE11 para la aplicación actual.
        /// </summary>
        /// <param name="applicationName">Nombre del ejecutable (ej: "MyApp.exe"). Si es null, usa el nombre del proceso actual.</param>
        /// <returns>True si se configuró correctamente, False en caso contrario.</returns>
        public static bool SetIE11Emulation(string? applicationName = null)
        {
            try
            {
                string appName = applicationName ?? Process.GetCurrentProcess().ProcessName + ".exe";
                
                // Abrir o crear la clave del registro para el usuario actual
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH, true))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, IE11_EMULATION_VALUE, RegistryValueKind.DWord);
                        Debug.WriteLine($"Emulación IE11 configurada para: {appName}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al configurar emulación IE11: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Obtiene el valor de emulación actual para la aplicación.
        /// </summary>
        /// <param name="applicationName">Nombre del ejecutable. Si es null, usa el nombre del proceso actual.</param>
        /// <returns>El valor de emulación o null si no está configurado.</returns>
        public static int? GetEmulationValue(string? applicationName = null)
        {
            try
            {
                string appName = applicationName ?? Process.GetCurrentProcess().ProcessName + ".exe";
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH))
                {
                    if (key != null)
                    {
                        object? value = key.GetValue(appName);
                        if (value is int intValue)
                        {
                            return intValue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al leer emulación IE: {ex.Message}");
            }
            
            return null;
        }

        /// <summary>
        /// Elimina la configuración de emulación para la aplicación.
        /// </summary>
        /// <param name="applicationName">Nombre del ejecutable. Si es null, usa el nombre del proceso actual.</param>
        /// <returns>True si se eliminó correctamente.</returns>
        public static bool RemoveEmulation(string? applicationName = null)
        {
            try
            {
                string appName = applicationName ?? Process.GetCurrentProcess().ProcessName + ".exe";
                
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(appName, false);
                        Debug.WriteLine($"Emulación IE eliminada para: {appName}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al eliminar emulación IE: {ex.Message}");
            }
            
            return false;
        }

        /// <summary>
        /// Genera un archivo .reg para configurar la emulación IE11.
        /// </summary>
        /// <param name="outputPath">Ruta donde guardar el archivo .reg</param>
        /// <param name="applicationName">Nombre del ejecutable. Si es null, usa el nombre del proceso actual.</param>
        /// <returns>True si se generó correctamente.</returns>
        public static bool GenerateRegFile(string outputPath, string? applicationName = null)
        {
            try
            {
                string appName = applicationName ?? Process.GetCurrentProcess().ProcessName + ".exe";
                string regContent = $@"Windows Registry Editor Version 5.00

[HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION]
""{appName}""=dword:00002af8

";
                
                System.IO.File.WriteAllText(outputPath, regContent);
                Debug.WriteLine($"Archivo .reg generado en: {outputPath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al generar archivo .reg: {ex.Message}");
                return false;
            }
        }
    }
}

