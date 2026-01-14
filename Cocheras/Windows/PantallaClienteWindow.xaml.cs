using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Web.WebView2.Core;

namespace Cocheras.Windows
{
    public partial class PantallaClienteWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private struct MonitorInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const uint SWP_SHOWWINDOW = 0x0040;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const int SW_MAXIMIZE = 3;
        private const int SW_SHOWMAXIMIZED = 3;

        private class MonitorEnumData
        {
            public List<MonitorInfo> Monitores { get; set; } = new List<MonitorInfo>();
        }

        public PantallaClienteWindow()
        {
            InitializeComponent();
            this.Loaded += PantallaClienteWindow_Loaded;
            
            // Configurar WebView2
            if (WebViewPantalla != null)
            {
                WebViewPantalla.NavigationCompleted += WebViewPantalla_NavigationCompleted;
                WebViewPantalla.CoreWebView2InitializationCompleted += WebViewPantalla_CoreWebView2InitializationCompleted;
            }
        }

        private void WebViewPantalla_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess && WebViewPantalla?.CoreWebView2 != null)
            {
                // Configurar WebView2 para mejor rendimiento y compatibilidad
                var settings = WebViewPantalla.CoreWebView2.Settings;
                settings.AreDefaultScriptDialogsEnabled = false; // Suprimir diálogos de script
                settings.IsScriptEnabled = true;
                settings.AreHostObjectsAllowed = false;
                settings.IsWebMessageEnabled = true;
                settings.AreBrowserAcceleratorKeysEnabled = false;
                
                // Suprimir errores de script completamente
                // AreDefaultScriptDialogsEnabled = false ya suprime los diálogos por defecto
                // Interceptar ScriptDialogOpening para cancelar cualquier diálogo que aparezca
                WebViewPantalla.CoreWebView2.ScriptDialogOpening += (s, args) =>
                {
                    // Cancelar todos los diálogos de script (alertas, confirmaciones, etc.)
                    try
                    {
                        // Usar deferral para manejar el diálogo de forma asíncrona
                        var deferral = args.GetDeferral();
                        try
                        {
                            // Cerrar el diálogo inmediatamente sin mostrar nada
                            // Esto suprime alertas, confirmaciones y prompts
                        }
                        finally
                        {
                            deferral.Complete();
                        }
                    }
                    catch
                    {
                        // Si falla, simplemente ignorar el diálogo
                    }
                };
                
                System.Diagnostics.Debug.WriteLine("WebView2 inicializado correctamente con supresión de errores");
            }
            else if (!e.IsSuccess)
            {
                System.Diagnostics.Debug.WriteLine($"Error al inicializar WebView2: {e.InitializationException?.Message}");
            }
        }

        private void PantallaClienteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Posicionar en segundo monitor después de que la ventana esté cargada
            // Usar un pequeño delay para asegurar que el handle esté disponible
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PosicionarEnSegundoMonitor();
            }), DispatcherPriority.Loaded);
        }

        private async void WebViewPantalla_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            // Configurar zoom al 100% y dimensiones después de cargar
            try
            {
                if (WebViewPantalla?.CoreWebView2 != null)
                {
                    string script = @"
                        document.body.style.zoom = '1.0';
                        document.body.style.transform = 'scale(1)';
                        document.body.style.margin = '0';
                        document.body.style.padding = '0';
                        document.documentElement.style.width = '100%';
                        document.documentElement.style.height = '100%';
                        document.body.style.width = '100%';
                        document.body.style.height = '100%';
                    ";
                    await WebViewPantalla.CoreWebView2.ExecuteScriptAsync(script);
                    System.Diagnostics.Debug.WriteLine("Zoom y dimensiones configurados correctamente en WebView2");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al configurar zoom del WebView2: {ex.Message}");
            }
        }

        public async void CargarURL(string url)
        {
            if (WebViewPantalla == null || string.IsNullOrEmpty(url))
            {
                return;
            }

            try
            {
                // Asegurar que WebView2 esté inicializado
                if (WebViewPantalla.CoreWebView2 == null)
                {
                    await WebViewPantalla.EnsureCoreWebView2Async();
                }
                
                if (WebViewPantalla.CoreWebView2 == null)
                {
                    System.Diagnostics.Debug.WriteLine("No se pudo inicializar CoreWebView2");
                    return;
                }
                
                // Convertir IP local a localhost para evitar problemas de zona de seguridad de Intranet
                string urlFinal = url;
                try
                {
                    var uri = new Uri(url);
                    string? host = uri.Host;
                    
                    if (!string.IsNullOrEmpty(host))
                    {
                        // Si es una IP local, convertir a localhost para mejor compatibilidad
                        if (System.Net.IPAddress.TryParse(host, out var ipAddress))
                        {
                            bool esIPLocal = false;
                            
                            // Verificar si es loopback
                            if (ipAddress.Equals(System.Net.IPAddress.Loopback) || host == "127.0.0.1")
                            {
                                esIPLocal = true;
                            }
                            // Verificar rangos privados comunes
                            else if (host.StartsWith("192.168.", StringComparison.Ordinal) || 
                                     host.StartsWith("10.", StringComparison.Ordinal))
                            {
                                esIPLocal = true;
                            }
                            // Verificar rango 172.16-31.x.x
                            else if (host.StartsWith("172.", StringComparison.Ordinal) && host.Length > 4)
                            {
                                int dotIndex = host.IndexOf('.', 4);
                                if (dotIndex > 4 && dotIndex < host.Length)
                                {
                                    string secondOctetStr = host.Substring(4, dotIndex - 4);
                                    if (int.TryParse(secondOctetStr, out int secondOctet))
                                    {
                                        if (secondOctet >= 16 && secondOctet <= 31)
                                        {
                                            esIPLocal = true;
                                        }
                                    }
                                }
                            }
                            
                            if (esIPLocal && !string.IsNullOrEmpty(host))
                            {
                                // Usar localhost para evitar problemas de zona de seguridad
                                urlFinal = url.Replace(host, "localhost");
                                System.Diagnostics.Debug.WriteLine($"URL convertida de {host} a localhost: {urlFinal}");
                            }
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    // Si falla el parsing, usar la URL original
                    System.Diagnostics.Debug.WriteLine($"No se pudo parsear la URL: {parseEx.Message}, usando original");
                }
                
                WebViewPantalla.CoreWebView2.Navigate(urlFinal);
                System.Diagnostics.Debug.WriteLine($"URL cargada en WebView2: {urlFinal}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar URL en WebView2: {ex.Message}");
                // Fallback: intentar con Source si falla Navigate
                try
                {
                    if (WebViewPantalla != null)
                    {
                        WebViewPantalla.Source = new Uri(url);
                    }
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar URL con Source: {ex2.Message}");
                }
            }
        }

        public void PosicionarEnSegundoMonitor()
        {
            try
            {
                var monitores = ObtenerMonitores();
                if (monitores != null && monitores.Count >= 2)
                {
                    var segundoMonitor = monitores[1];
                    System.Diagnostics.Debug.WriteLine($"Segundo monitor detectado: X={segundoMonitor.X}, Y={segundoMonitor.Y}, Width={segundoMonitor.Width}, Height={segundoMonitor.Height}");
                    
                    // Obtener el handle de la ventana
                    var windowHandle = new WindowInteropHelper(this).Handle;
                    if (windowHandle != IntPtr.Zero)
                    {
                        // Mover la ventana al segundo monitor usando Windows API
                        SetWindowPos(windowHandle, IntPtr.Zero, segundoMonitor.X, segundoMonitor.Y, segundoMonitor.Width, segundoMonitor.Height, SWP_SHOWWINDOW);
                        System.Diagnostics.Debug.WriteLine($"Ventana movida al segundo monitor usando SetWindowPos");
                        
                        // Maximizar después de mover
                        ShowWindow(windowHandle, SW_SHOWMAXIMIZED);
                        System.Diagnostics.Debug.WriteLine("Ventana maximizada");
                    }
                    else
                    {
                        // Fallback: usar propiedades WPF
                        this.Left = segundoMonitor.X;
                        this.Top = segundoMonitor.Y;
                        this.Width = segundoMonitor.Width;
                        this.Height = segundoMonitor.Height;
                        this.WindowState = WindowState.Maximized;
                        System.Diagnostics.Debug.WriteLine($"Ventana posicionada usando propiedades WPF");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("No se detectó segundo monitor, la ventana no se posicionará");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al posicionar ventana en segundo monitor: {ex.Message}");
            }
        }

        private static List<MonitorInfo>? ObtenerMonitores()
        {
            try
            {
                var data = new MonitorEnumData();
                MonitorEnumProc proc = (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
                {
                    try
                    {
                        if (lprcMonitor == IntPtr.Zero) return true;
                        
                        RECT rect = Marshal.PtrToStructure<RECT>(lprcMonitor);
                        var dataLocal = GCHandle.FromIntPtr(dwData).Target as MonitorEnumData;
                        if (dataLocal != null)
                        {
                            dataLocal.Monitores.Add(new MonitorInfo
                            {
                                X = rect.Left,
                                Y = rect.Top,
                                Width = rect.Right - rect.Left,
                                Height = rect.Bottom - rect.Top
                            });
                        }
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                };
                
                GCHandle handle = GCHandle.Alloc(data);
                try
                {
                    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, GCHandle.ToIntPtr(handle));
                }
                finally
                {
                    handle.Free();
                }
                return data.Monitores;
            }
            catch
            {
                return null;
            }
        }
    }
}

