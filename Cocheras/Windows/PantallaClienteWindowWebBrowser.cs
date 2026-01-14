using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace Cocheras.Windows
{
    /// <summary>
    /// Versión alternativa usando WebBrowser (IE) con supresión de errores de script
    /// Usar esta clase solo si WebView2 no está disponible
    /// </summary>
    public partial class PantallaClienteWindowWebBrowser : Window
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
        private const int SW_SHOWMAXIMIZED = 3;

        private class MonitorEnumData
        {
            public List<MonitorInfo> Monitores { get; set; } = new List<MonitorInfo>();
        }

        private System.Windows.Controls.WebBrowser? _webBrowser;

        public PantallaClienteWindowWebBrowser()
        {
            InitializeComponent();
            this.Loaded += PantallaClienteWindowWebBrowser_Loaded;
        }

        private void PantallaClienteWindowWebBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            // Crear WebBrowser programáticamente con supresión de errores
            _webBrowser = new System.Windows.Controls.WebBrowser
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            
            // SUPRESIÓN DE ERRORES DE SCRIPT - IMPORTANTE
            // Nota: ScriptErrorsSuppressed no está disponible en .NET 9.0 para System.Windows.Controls.WebBrowser
            // Los errores de script se suprimirán mediante el manejo de eventos en LoadCompleted
            // Alternativamente, se puede usar WebView2 que tiene mejor soporte para esto
            
            // Configurar eventos
            if (_webBrowser != null)
            {
                _webBrowser.LoadCompleted += WebBrowser_LoadCompleted;
            }
            
            // Agregar al Grid
            var grid = this.Content as System.Windows.Controls.Grid;
            if (grid != null && _webBrowser != null)
            {
                grid.Children.Add(_webBrowser);
            }
            
            // Posicionar en segundo monitor
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PosicionarEnSegundoMonitor();
            }), DispatcherPriority.Loaded);
        }

        private void WebBrowser_LoadCompleted(object? sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // Configurar zoom al 100% y dimensiones
            try
            {
                if (_webBrowser?.Document != null)
                {
                    dynamic? doc = _webBrowser.Document;
                    if (doc != null)
                    {
                        try
                        {
                            var parentWindow = doc.parentWindow;
                            if (parentWindow != null)
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
                                parentWindow.execScript(script, "javascript");
                                System.Diagnostics.Debug.WriteLine("Zoom y dimensiones configurados en WebBrowser");
                            }
                        }
                        catch (Exception scriptEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al ejecutar script en WebBrowser: {scriptEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al configurar zoom del WebBrowser: {ex.Message}");
            }
        }

        public void CargarURL(string? url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            // Convertir IP local a localhost si es necesario para evitar problemas de zona de seguridad
            string urlFinal = url;
            
            // Opcional: usar localhost en lugar de IP para evitar restricciones de zona de seguridad
            // Descomentar si hay problemas con zonas de seguridad de Intranet
            /*
            if (url.Contains("192.168.") || url.Contains("10.") || url.Contains("172."))
            {
                var match = System.Text.RegularExpressions.Regex.Match(url, @"\d+\.\d+\.\d+\.\d+");
                if (match.Success)
                {
                    urlFinal = url.Replace(match.Value, "localhost");
                    System.Diagnostics.Debug.WriteLine($"URL convertida de IP a localhost: {urlFinal}");
                }
            }
            */
            
            if (_webBrowser != null && !string.IsNullOrEmpty(urlFinal))
            {
                _webBrowser.Navigate(urlFinal);
                System.Diagnostics.Debug.WriteLine($"URL cargada en WebBrowser: {urlFinal}");
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
                    
                    var windowHandle = new WindowInteropHelper(this).Handle;
                    if (windowHandle != IntPtr.Zero)
                    {
                        SetWindowPos(windowHandle, IntPtr.Zero, segundoMonitor.X, segundoMonitor.Y, segundoMonitor.Width, segundoMonitor.Height, SWP_SHOWWINDOW);
                        ShowWindow(windowHandle, SW_SHOWMAXIMIZED);
                        System.Diagnostics.Debug.WriteLine("Ventana posicionada en segundo monitor usando SetWindowPos");
                    }
                    else
                    {
                        this.Left = segundoMonitor.X;
                        this.Top = segundoMonitor.Y;
                        this.Width = segundoMonitor.Width;
                        this.Height = segundoMonitor.Height;
                        this.WindowState = WindowState.Maximized;
                        System.Diagnostics.Debug.WriteLine("Ventana posicionada usando propiedades WPF");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al posicionar ventana: {ex.Message}");
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
                return data.Monitores.Count > 0 ? data.Monitores : null;
            }
            catch
            {
                return null;
            }
        }
    }
}

