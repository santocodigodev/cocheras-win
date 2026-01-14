using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Cocheras.Models;
using Cocheras.Services;
using Cocheras.Helpers;

namespace Cocheras.Windows
{
    // Clase para mostrar usuarios en la lista
    public class UsuarioDisplay
    {
        public int Id { get; set; }
        public int Numero { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public string RolDisplay => Rol.ToUpper();
        public DateTime? UltimoAcceso { get; set; }
        public string UltimoAccesoDisplay => UltimoAcceso?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca";
        public bool PuedeEliminar { get; set; }
    }

    public class MovimientoTicketDisplay
    {
        public bool EsEntrada { get; set; }
        public string Icono { get; set; } = string.Empty;
        public SolidColorBrush IconoColor { get; set; } = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
        public DateTime FechaHora { get; set; }
        public string Fecha => FechaHora.ToString("dd/MM/yy");
        public string Hora => FechaHora.ToString("HH:mm");
        public string Matricula { get; set; } = string.Empty;
        public string? ImagenPath { get; set; }
        public string Categoria { get; set; } = string.Empty;
        public string TicketId { get; set; } = string.Empty;
        public string MensualId { get; set; } = "-";
        public string Operador { get; set; } = string.Empty;
    }

    public partial class HomeWindow : Window
    {
        private readonly DatabaseService _dbService;
        private readonly string _username;
        private int? _currentAdminId;
        private DispatcherTimer? _timer;
        private DispatcherTimer? _timerEstadisticas;
        private DispatcherTimer? _timerTiempoTickets;
        private ObservableCollection<Ticket> _tickets;
        private ObservableCollection<Ticket> _ticketsFiltrados;
        private ObservableCollection<Ticket> _ticketsCerrados;
        private ObservableCollection<Ticket> _ticketsCerradosFiltrados;
        private ObservableCollection<Categoria> _categorias;
        private ObservableCollection<Tarifa> _tarifas = new ObservableCollection<Tarifa>();
        private ObservableCollection<FormaPago> _formasPago = new ObservableCollection<FormaPago>();
        private ObservableCollection<ClienteMensual> _clientesMensuales = new ObservableCollection<ClienteMensual>();
        private ObservableCollection<ClienteMensual> _clientesMensualesFiltrados = new ObservableCollection<ClienteMensual>();
        private ObservableCollection<VehiculoMensual> _vehiculosMensuales = new ObservableCollection<VehiculoMensual>();
        private ObservableCollection<VehiculoMensual> _vehiculosMensualesFiltrados = new ObservableCollection<VehiculoMensual>();
        private ObservableCollection<MovimientoMensual> _movimientosMensuales = new ObservableCollection<MovimientoMensual>();
        private ObservableCollection<UsuarioDisplay> _usuarios;
        private ObservableCollection<MovimientoTicketDisplay> _movimientosEntradasSalidas = new ObservableCollection<MovimientoTicketDisplay>();
        private ObservableCollection<MovimientoTicketDisplay> _movimientosEntradasSalidasFiltrados = new ObservableCollection<MovimientoTicketDisplay>();
        private ObservableCollection<CamaraANPR> _camarasANPR = new ObservableCollection<CamaraANPR>();
        private bool _modoOscuro = false;
        private Dictionary<string, Button> _botonesModulos = new Dictionary<string, Button>();
        private Dictionary<string, Grid> _panelesModulos = new Dictionary<string, Grid>();
        private HttpListener? _httpListener;
        private int _puertoPantallaCliente = 0;
        private string _urlPantallaCliente = "";
        private string _estadoPantallaCliente = "bienvenida"; // bienvenida, cobro, agradecimiento
        private string _matriculaPantallaCliente = "";
        private decimal _importePantallaCliente = 0m;
        private string _metodoPagoPantallaCliente = "";
        private string _qrMercadoPago = "";
        private bool _cargandoConfiguracionPantallaCliente = false;
        
        // APIs de Windows para manejar ventanas y monitores
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
        
        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
        
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
        
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int Size;
            public RECT Monitor;
            public RECT WorkArea;
            public uint Flags;
        }
        
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);
        
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_MAXIMIZE = 3;
        private const int SM_CMONITORS = 80;
        
        private PantallaClienteWindow? _ventanaPantallaCliente = null;
        
        private struct MonitorInfo
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }
        
        private class MonitorEnumData
        {
            public List<MonitorInfo> Monitores { get; set; } = new List<MonitorInfo>();
        }
        
        private static List<MonitorInfo>? ObtenerMonitores()
        {
            try
            {
                var data = new MonitorEnumData();
                MonitorEnumProc proc = MonitorEnumCallback;
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
        
        private static bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
        {
            try
            {
                if (lprcMonitor == IntPtr.Zero) return true;
                
                RECT rect = Marshal.PtrToStructure<RECT>(lprcMonitor);
                var data = GCHandle.FromIntPtr(dwData).Target as MonitorEnumData;
                if (data != null)
                {
                    data.Monitores.Add(new MonitorInfo
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
        }
        private Admin? _usuarioEditando;
        private Ticket? _ticketEditando;
        private Ticket? _ticketCancelando;
        private Ticket? _ticketCobrar;
        private List<string> _itemsCobrarDetalle = new List<string>();
        private decimal _importeCobrar = 0m;
        private readonly StringBuilder _scannerBuffer = new StringBuilder();
        private DateTime _lastScanTime = DateTime.MinValue;
        private const int ScannerTimeoutMs = 800;
        private ClienteMensual? _clienteMensualSeleccionado;
        private string _tipoMovimientoMensual = "Cargo";
        private string? _mesPagoAdelantado;
        private decimal _importePagoAdelantado = 0m;

        // Colores modo oscuro
        private static readonly SolidColorBrush ColorFondoOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
        private static readonly SolidColorBrush ColorFondoSecundarioOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55));
        private static readonly SolidColorBrush ColorTextoOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
        private static readonly SolidColorBrush ColorTextoSecundarioOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));
        private static readonly SolidColorBrush ColorBordeOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(55, 65, 81));
        private static readonly SolidColorBrush ColorInputOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55));
        private static readonly SolidColorBrush ColorTicketOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55));

        // Colores modo claro
        private static readonly SolidColorBrush ColorFondoClaro = System.Windows.Media.Brushes.White;
        private static readonly SolidColorBrush ColorFondoSecundarioClaro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252));
        private static readonly SolidColorBrush ColorTextoClaro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39));
        private static readonly SolidColorBrush ColorTextoSecundarioClaro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
        private static readonly SolidColorBrush ColorBordeClaro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));
        private static readonly SolidColorBrush ColorInputClaro = System.Windows.Media.Brushes.White;
        private static readonly SolidColorBrush ColorTicketClaro = System.Windows.Media.Brushes.White;

        // Estado popup crear ticket
        private Tarifa? _tarifaSeleccionada;
        private Categoria? _categoriaSeleccionada;
        private Estacionamiento? _estacionamientoCache;

        public HomeWindow(string username, DatabaseService dbService)
        {
            InitializeComponent();
            _dbService = dbService;
            _username = username;
            _tickets = new ObservableCollection<Ticket>();
            _ticketsFiltrados = new ObservableCollection<Ticket>();
            _ticketsCerrados = new ObservableCollection<Ticket>();
            _ticketsCerradosFiltrados = new ObservableCollection<Ticket>();
            _categorias = new ObservableCollection<Categoria>();
            _usuarios = new ObservableCollection<UsuarioDisplay>();
            
            InicializarTimer();
            CargarCategorias();
            CargarTarifas();
            CargarFormasPago();
            CargarFiltroTarifasAbiertos();
            CargarDatos();
            CargarEstadisticas();
            InicializarAtajosTeclado();
            this.PreviewTextInput += HomeWindow_PreviewTextInput;
            
            // Cargar mensuales de forma asíncrona para no bloquear la inicialización
            this.Loaded += (s, e) => 
            {
                try
                {
                    ActualizarPestanasModulos();
                    
                    // Si el módulo Pantalla Cliente está activo, iniciar servidor
                    var modulos = _dbService.ObtenerModulos();
                    if (modulos.ContainsKey("MONITOR") && modulos["MONITOR"])
                    {
                        IniciarServidorPantallaCliente();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al cargar pestañas de módulos: {ex.Message}");
                }
                try
                {
                    CargarMensuales();
                }
                catch (Exception ex)
                {
                    // Log del error pero no bloquear la aplicación
                    System.Diagnostics.Debug.WriteLine($"Error al cargar mensuales: {ex.Message}");
                }
            };
        }

        private void CargarDatos()
        {
            // Cargar nombre del usuario
            string? nombre = _dbService.ObtenerNombreAdmin(_username);
            TxtNombreUsuario.Text = nombre ?? _username;

            var adminActual = _dbService.ObtenerAdminPorUsername(_username);
            _currentAdminId = adminActual?.Id;

            // Cachear estacionamiento para usar impresora y nombre en ticket
            _estacionamientoCache = _dbService.ObtenerEstacionamiento();

            // Cargar tickets abiertos
            CargarTicketsAbiertos();
            CargarTicketsCerrados();
            CargarFiltrosCerrados();
            CargarMensuales();
        }

        private string ObtenerNombreAdminPorId(int? adminId)
        {
            if (adminId == null) return string.Empty;
            var admin = _dbService.ObtenerAdminPorId(adminId.Value);
            if (admin == null) return string.Empty;
            var nombreCompleto = $"{admin.Nombre} {admin.Apellido}".Trim();
            return string.IsNullOrWhiteSpace(nombreCompleto) ? admin.Username : nombreCompleto;
        }

        private void CargarEntradasSalidas()
        {
            if (DataGridEntradasSalidas == null) return;

            var verdeEntrada = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
            var rojoSalida = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68));
            var categoriaDict = _categorias.ToDictionary(c => c.Id, c => c.Nombre);
            var cacheTarifas = new Dictionary<int, Tarifa>();
            var cacheAdmins = new Dictionary<int, string>();

            string GetCategoriaNombre(int? id)
            {
                if (id == null) return string.Empty;
                return categoriaDict.TryGetValue(id.Value, out var nombreCat) ? nombreCat : string.Empty;
            }

            Tarifa? GetTarifa(int? id)
            {
                if (id == null) return null;
                if (cacheTarifas.TryGetValue(id.Value, out var t)) return t;
                var tarifa = _dbService.ObtenerTarifaPorId(id.Value);
                if (tarifa != null) cacheTarifas[id.Value] = tarifa;
                return tarifa;
            }

            string GetAdminNombre(int? id)
            {
                if (id == null) return string.Empty;
                if (cacheAdmins.TryGetValue(id.Value, out var nombre)) return nombre;
                var nombreDb = ObtenerNombreAdminPorId(id);
                cacheAdmins[id.Value] = nombreDb;
                return nombreDb;
            }

            var movimientos = new List<MovimientoTicketDisplay>();

            // Entradas de tickets abiertos
            foreach (var t in _dbService.ObtenerTicketsAbiertos())
            {
                var tarifa = GetTarifa(t.TarifaId);
                movimientos.Add(new MovimientoTicketDisplay
                {
                    EsEntrada = true,
                    Icono = "\uE72B",
                    IconoColor = verdeEntrada,
                    FechaHora = t.FechaEntrada,
                    Matricula = t.Matricula,
                    ImagenPath = t.ImagenPath,
                    Categoria = GetCategoriaNombre(t.CategoriaId),
                    TicketId = t.Id.ToString(),
                    MensualId = tarifa != null && tarifa.Tipo == TipoTarifa.Mensual ? t.Id.ToString() : "-",
                    Operador = GetAdminNombre(t.AdminCreadorId)
                });
            }

            // Entradas y salidas de tickets cerrados (incluye cancelados para entrada; salidas solo no cancelados)
            foreach (var t in _dbService.ObtenerTicketsCerrados())
            {
                var tarifa = GetTarifa(t.TarifaId);

                // Entrada
                movimientos.Add(new MovimientoTicketDisplay
                {
                    EsEntrada = true,
                    Icono = "\uE72B",
                    IconoColor = verdeEntrada,
                    FechaHora = t.FechaEntrada,
                    Matricula = t.Matricula,
                    ImagenPath = t.ImagenPath,
                    Categoria = GetCategoriaNombre(t.CategoriaId),
                    TicketId = t.Id.ToString(),
                    MensualId = tarifa != null && tarifa.Tipo == TipoTarifa.Mensual ? t.Id.ToString() : "-",
                    Operador = GetAdminNombre(t.AdminCreadorId)
                });

                // Salida (solo si no está cancelado y tiene fecha de salida)
                if (!t.EstaCancelado && t.FechaSalida.HasValue)
                {
                    movimientos.Add(new MovimientoTicketDisplay
                    {
                        EsEntrada = false,
                        Icono = "\uE72C",
                        IconoColor = rojoSalida,
                        FechaHora = t.FechaSalida.Value,
                        Matricula = t.Matricula,
                        ImagenPath = t.ImagenPath,
                        Categoria = GetCategoriaNombre(t.CategoriaId),
                        TicketId = t.Id.ToString(),
                        MensualId = tarifa != null && tarifa.Tipo == TipoTarifa.Mensual ? t.Id.ToString() : "-",
                        Operador = GetAdminNombre(t.AdminCerradorId)
                    });
                }
            }

            var ordenados = movimientos.OrderByDescending(m => m.FechaHora).ToList();
            _movimientosEntradasSalidas.Clear();
            foreach (var m in ordenados)
            {
                _movimientosEntradasSalidas.Add(m);
            }
            AplicarFiltrosEntradasSalidas();
        }

        private void AplicarFiltrosEntradasSalidas()
        {
            if (DataGridEntradasSalidas == null) return;

            var fechaSeleccionada = CalendarEntradasSalidas?.SelectedDate ?? DateTime.Today;
            if (CalendarEntradasSalidas != null && CalendarEntradasSalidas.SelectedDate == null)
            {
                CalendarEntradasSalidas.SelectedDate = fechaSeleccionada;
            }

            IEnumerable<MovimientoTicketDisplay> consulta = _movimientosEntradasSalidas
                .Where(m => m.FechaHora.Date == fechaSeleccionada.Date);

            _movimientosEntradasSalidasFiltrados.Clear();
            foreach (var mov in consulta)
            {
                _movimientosEntradasSalidasFiltrados.Add(mov);
            }

            DataGridEntradasSalidas.ItemsSource = _movimientosEntradasSalidasFiltrados;
        }

        // -------------------- Mensuales --------------------
        private void CargarMensuales()
        {
            try
            {
                _clientesMensuales.Clear();
                var clientes = _dbService.ObtenerClientesMensuales();
                foreach (var c in clientes)
                {
                    _clientesMensuales.Add(c);
                }
                _clientesMensualesFiltrados = new ObservableCollection<ClienteMensual>(_clientesMensuales);

                _vehiculosMensuales.Clear();
                var vehiculos = _dbService.ObtenerVehiculosMensuales();
                foreach (var v in vehiculos)
                {
                    _vehiculosMensuales.Add(v);
                }
                _vehiculosMensualesFiltrados = new ObservableCollection<VehiculoMensual>(_vehiculosMensuales);

                // Solo aplicar filtros si los controles están inicializados
                if (CmbMensualesEstado != null)
                {
                    AplicarFiltrosMensuales();
                    ActualizarConteosMensuales();
                }
            }
            catch (Exception ex)
            {
                // Log del error pero no bloquear
                System.Diagnostics.Debug.WriteLine($"Error en CargarMensuales: {ex.Message}");
                // Inicializar colecciones vacías para evitar errores
                if (_clientesMensuales == null) _clientesMensuales = new ObservableCollection<ClienteMensual>();
                if (_clientesMensualesFiltrados == null) _clientesMensualesFiltrados = new ObservableCollection<ClienteMensual>();
                if (_vehiculosMensuales == null) _vehiculosMensuales = new ObservableCollection<VehiculoMensual>();
                if (_vehiculosMensualesFiltrados == null) _vehiculosMensualesFiltrados = new ObservableCollection<VehiculoMensual>();
            }
        }

        private void ActualizarConteosMensuales()
        {
            int countClientes = _clientesMensualesFiltrados.Count;
            // Contar vehículos de clientes activos (no filtrados por búsqueda)
            int countVehiculos = _vehiculosMensuales.Count(v => 
            {
                var cliente = _clientesMensuales.FirstOrDefault(c => c.Id == v.ClienteId);
                return cliente != null && cliente.EstaActivo;
            });
            if (BtnMensualesClientesTab != null) BtnMensualesClientesTab.Content = $"Clientes Activos ({countClientes})";
            if (BtnMensualesVehiculosTab != null) BtnMensualesVehiculosTab.Content = $"Vehículos ({countVehiculos})";
        }

        private void AplicarFiltrosMensuales()
        {
            string estado = (CmbMensualesEstado?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Activos";

            string filtroMat = (TxtMensualesMatricula?.Text ?? string.Empty).Trim().ToUpperInvariant();

            string filtroCli = (TxtMensualesCliente?.Text ?? string.Empty).Trim().ToUpperInvariant();

            bool conDeuda = ChkMensualesConDeuda?.IsChecked == true;

            var clientes = _clientesMensuales.Where(c =>
            {
                if (estado == "Activos" && !c.Activo) return false;
                if (estado == "Archivados" && c.Activo) return false;
                if (!string.IsNullOrWhiteSpace(filtroCli) && !c.NombreCompleto.ToUpperInvariant().Contains(filtroCli)) return false;
                if (!string.IsNullOrWhiteSpace(filtroMat) && !c.MatriculasConcat.ToUpperInvariant().Contains(filtroMat)) return false;
                if (conDeuda && c.Balance >= 0) return false;
                return true;
            }).ToList();

            _clientesMensualesFiltrados = new ObservableCollection<ClienteMensual>(clientes);
            if (DataGridMensualesClientes != null) DataGridMensualesClientes.ItemsSource = _clientesMensualesFiltrados;

            var vehiculos = _vehiculosMensuales.Where(v =>
            {
                var cliente = _clientesMensuales.FirstOrDefault(c => c.Id == v.ClienteId);
                if (estado == "Activos" && cliente != null && !cliente.Activo) return false;
                if (estado == "Archivados" && cliente != null && cliente.Activo) return false;
                if (!string.IsNullOrWhiteSpace(filtroCli) && cliente != null && !cliente.NombreCompleto.ToUpperInvariant().Contains(filtroCli)) return false;
                if (!string.IsNullOrWhiteSpace(filtroMat) && !v.Matricula.ToUpperInvariant().Contains(filtroMat)) return false;
                return true;
            }).ToList();
            _vehiculosMensualesFiltrados = new ObservableCollection<VehiculoMensual>(vehiculos);
            if (DataGridMensualesVehiculos != null) DataGridMensualesVehiculos.ItemsSource = _vehiculosMensualesFiltrados;

            ActualizarConteosMensuales();
        }

        private void MostrarTabMensualesClientes()
        {
            if (GridMensualesClientes != null) GridMensualesClientes.Visibility = Visibility.Visible;
            if (GridMensualesVehiculos != null) GridMensualesVehiculos.Visibility = Visibility.Collapsed;
            if (GridMensualesDetalle != null) GridMensualesDetalle.Visibility = Visibility.Collapsed;
            if (BtnMensualesClientesTab != null)
            {
                BtnMensualesClientesTab.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnMensualesClientesTab.Foreground = System.Windows.Media.Brushes.White;
            }
            if (BtnMensualesVehiculosTab != null)
            {
                BtnMensualesVehiculosTab.Background = System.Windows.Media.Brushes.Transparent;
                BtnMensualesVehiculosTab.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));
            }
            if (BtnMensualesAgregarCliente != null) BtnMensualesAgregarCliente.Visibility = Visibility.Visible;
        }

        private void MostrarTabMensualesVehiculos()
        {
            if (GridMensualesClientes != null) GridMensualesClientes.Visibility = Visibility.Collapsed;
            if (GridMensualesVehiculos != null) GridMensualesVehiculos.Visibility = Visibility.Visible;
            if (GridMensualesDetalle != null) GridMensualesDetalle.Visibility = Visibility.Collapsed;
            if (BtnMensualesClientesTab != null)
            {
                BtnMensualesClientesTab.Background = System.Windows.Media.Brushes.Transparent;
                BtnMensualesClientesTab.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));
            }
            if (BtnMensualesVehiculosTab != null)
            {
                BtnMensualesVehiculosTab.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnMensualesVehiculosTab.Foreground = System.Windows.Media.Brushes.White;
            }
            if (BtnMensualesAgregarCliente != null) BtnMensualesAgregarCliente.Visibility = Visibility.Visible;
        }

        private void MostrarDetalleMensual(ClienteMensual cliente)
        {
            _clienteMensualSeleccionado = cliente;
            if (GridMensualesClientes != null) GridMensualesClientes.Visibility = Visibility.Collapsed;
            if (GridMensualesVehiculos != null) GridMensualesVehiculos.Visibility = Visibility.Collapsed;
            if (GridMensualesDetalle != null) GridMensualesDetalle.Visibility = Visibility.Visible;
            
            // Ocultar filtros y tabs cuando se muestra el detalle
            if (StackPanelMensualesFiltros != null) StackPanelMensualesFiltros.Visibility = Visibility.Collapsed;
            if (BtnMensualesClientesTab != null) BtnMensualesClientesTab.Visibility = Visibility.Collapsed;
            if (BtnMensualesVehiculosTab != null) BtnMensualesVehiculosTab.Visibility = Visibility.Collapsed;
            
            // Cargar datos del cliente
            if (TxtMensualNombreDetalle != null) TxtMensualNombreDetalle.Text = cliente.NombreCompleto;
            if (TxtDetalleClienteNombre != null) TxtDetalleClienteNombre.Text = cliente.NombreCompleto;
            if (TxtDetalleClienteWhatsapp != null) TxtDetalleClienteWhatsapp.Text = cliente.Whatsapp ?? "";
            if (TxtDetalleClienteEmail != null) TxtDetalleClienteEmail.Text = cliente.Email ?? "";
            if (TxtDetalleClienteDNI != null) TxtDetalleClienteDNI.Text = cliente.DNI ?? "";
            if (TxtDetalleClienteCUIT != null) TxtDetalleClienteCUIT.Text = cliente.CUIT ?? "";
            if (TxtDetalleClienteDireccion != null) TxtDetalleClienteDireccion.Text = cliente.Direccion ?? "";
            if (TxtDetalleClienteNota != null) TxtDetalleClienteNota.Text = cliente.Nota ?? "";

            // Cargar balance
            var balance = _dbService.ObtenerBalanceClienteMensual(cliente.Id);
            if (TxtMensualBalance != null)
            {
                TxtMensualBalance.Text = balance.ToString("$#,0.00");
                // Aplicar color según el balance
                if (balance < 0)
                    TxtMensualBalance.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)); // Rojo
                else if (balance == 0)
                    TxtMensualBalance.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0)); // Negro
                else
                    TxtMensualBalance.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74)); // Verde
            }

            // Cargar movimientos
            _movimientosMensuales = new ObservableCollection<MovimientoMensual>(_dbService.ObtenerMovimientosMensuales(cliente.Id));
            if (DataGridMensualesMovimientos != null)
            {
                DataGridMensualesMovimientos.ItemsSource = _movimientosMensuales;
            }

            // Cargar vehículos del cliente y poblar nombres y valores
            var vehiculosCliente = _dbService.ObtenerVehiculosMensuales(cliente.Id);
            foreach (var v in vehiculosCliente)
            {
                if (v.CategoriaId.HasValue)
                {
                    var cat = _categorias.FirstOrDefault(c => c.Id == v.CategoriaId.Value);
                    v.CategoriaNombre = cat?.Nombre ?? "";
                }
                if (v.TarifaId.HasValue)
                {
                    var tar = _tarifas.FirstOrDefault(t => t.Id == v.TarifaId.Value);
                    v.TarifaNombre = tar?.Nombre ?? "";
                    
                    // Calcular valor mensual
                    if (v.TienePrecioDiferenciado && v.PrecioPersonalizado.HasValue)
                    {
                        v.ValorMensual = v.PrecioPersonalizado.Value;
                    }
                    else if (v.CategoriaId.HasValue && v.TarifaId.HasValue)
                    {
                        var precio = _dbService.ObtenerPrecio(v.TarifaId.Value, v.CategoriaId.Value);
                        v.ValorMensual = precio?.Monto ?? 0m;
                    }
                    else
                    {
                        v.ValorMensual = 0m;
                    }
                }
                else
                {
                    v.ValorMensual = 0m;
                }
            }
            if (DataGridMensualDetalleVehiculos != null) DataGridMensualDetalleVehiculos.ItemsSource = vehiculosCliente;
            if (BtnMensualDetalleTabVehiculos != null) BtnMensualDetalleTabVehiculos.Content = $"Vehículos ({vehiculosCliente.Count})";

            // Calcular próximo cargo: usar el ProximoCargo más próximo de todos los vehículos
            if (vehiculosCliente.Count > 0)
            {
                // Buscar el ProximoCargo más próximo de todos los vehículos
                DateTime? proximoCargoMinimo = null;
                foreach (var vehiculo in vehiculosCliente)
                {
                    if (vehiculo.ProximoCargo.HasValue)
                    {
                        if (!proximoCargoMinimo.HasValue || vehiculo.ProximoCargo.Value < proximoCargoMinimo.Value)
                        {
                            proximoCargoMinimo = vehiculo.ProximoCargo.Value;
                        }
                    }
                }
                
                // Si no hay ningún ProximoCargo definido, usar el día 1 del siguiente mes
                if (!proximoCargoMinimo.HasValue)
                {
                    proximoCargoMinimo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                }
                
                if (TxtMensualProximoCargo != null)
                {
                    TxtMensualProximoCargo.Text = proximoCargoMinimo.Value.ToString("dd/MM/yyyy");
                }
            }
            else
            {
                if (TxtMensualProximoCargo != null)
                {
                    TxtMensualProximoCargo.Text = "N/A";
                }
            }
            
            // Recargar los datos del cliente para actualizar el balance
            var clienteActualizado = _dbService.ObtenerClienteMensualPorId(cliente.Id);
            if (clienteActualizado != null)
            {
                cliente.Balance = clienteActualizado.Balance;
            }

            // Deshabilitar botón de pago adelantado si hay deuda (balance negativo)
            var btnPagoAdelantado = this.FindName("BtnPagoAdelantadoMensual") as Button;
            if (btnPagoAdelantado != null)
            {
                btnPagoAdelantado.IsEnabled = balance >= 0;
                if (balance < 0)
                {
                    btnPagoAdelantado.Opacity = 0.5;
                    btnPagoAdelantado.Cursor = Cursors.No;
                }
                else
                {
                    btnPagoAdelantado.Opacity = 1.0;
                    btnPagoAdelantado.Cursor = Cursors.Hand;
                }
            }

            // Mostrar pestaña de cuenta corriente por defecto
            MostrarTabDetalleCuenta();
        }

        private void DataGridMensualesClientes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridMensualesClientes.SelectedItem is ClienteMensual cliente)
            {
                MostrarDetalleMensual(cliente);
            }
        }

        private void BtnMensualesVolver_Click(object sender, RoutedEventArgs e)
        {
            if (GridMensualesDetalle != null) GridMensualesDetalle.Visibility = Visibility.Collapsed;
            
            // Mostrar filtros y tabs cuando se vuelve a la lista
            if (StackPanelMensualesFiltros != null) StackPanelMensualesFiltros.Visibility = Visibility.Visible;
            if (BtnMensualesClientesTab != null) BtnMensualesClientesTab.Visibility = Visibility.Visible;
            if (BtnMensualesVehiculosTab != null) BtnMensualesVehiculosTab.Visibility = Visibility.Visible;
            
            // Limpiar selección del DataGrid
            if (DataGridMensualesClientes != null) DataGridMensualesClientes.SelectedItem = null;
            
            MostrarTabMensualesClientes();
        }

        private void BtnMensualDetalleTabCuenta_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabDetalleCuenta();
        }

        private void BtnMensualDetalleTabVehiculos_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabDetalleVehiculos();
        }

        private void MostrarTabDetalleCuenta()
        {
            if (GridMensualDetalleCuenta != null) GridMensualDetalleCuenta.Visibility = Visibility.Visible;
            if (GridMensualDetalleVehiculos != null) GridMensualDetalleVehiculos.Visibility = Visibility.Collapsed;
            if (ScrollViewerDetalleCliente != null) ScrollViewerDetalleCliente.Visibility = Visibility.Visible;
            var columnaDetalleCliente = this.FindName("ColumnaDetalleCliente") as ColumnDefinition;
            if (columnaDetalleCliente != null) columnaDetalleCliente.Width = new GridLength(350);
            if (BtnMensualDetalleTabCuenta != null)
            {
                BtnMensualDetalleTabCuenta.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnMensualDetalleTabCuenta.Foreground = System.Windows.Media.Brushes.White;
            }
            if (BtnMensualDetalleTabVehiculos != null)
            {
                BtnMensualDetalleTabVehiculos.Background = System.Windows.Media.Brushes.Transparent;
                BtnMensualDetalleTabVehiculos.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));
            }
        }

        private void MostrarTabDetalleVehiculos()
        {
            if (GridMensualDetalleCuenta != null) GridMensualDetalleCuenta.Visibility = Visibility.Collapsed;
            if (GridMensualDetalleVehiculos != null) GridMensualDetalleVehiculos.Visibility = Visibility.Visible;
            if (ScrollViewerDetalleCliente != null) ScrollViewerDetalleCliente.Visibility = Visibility.Collapsed;
            var columnaDetalleCliente = this.FindName("ColumnaDetalleCliente") as ColumnDefinition;
            if (columnaDetalleCliente != null) columnaDetalleCliente.Width = new GridLength(0);
            if (BtnMensualDetalleTabCuenta != null)
            {
                BtnMensualDetalleTabCuenta.Background = System.Windows.Media.Brushes.Transparent;
                BtnMensualDetalleTabCuenta.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));
            }
            if (BtnMensualDetalleTabVehiculos != null)
            {
                BtnMensualDetalleTabVehiculos.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnMensualDetalleTabVehiculos.Foreground = System.Windows.Media.Brushes.White;
            }
            
            // Recargar vehículos del cliente si hay un cliente seleccionado
            if (_clienteMensualSeleccionado != null)
            {
                var vehiculosCliente = _dbService.ObtenerVehiculosMensuales(_clienteMensualSeleccionado.Id);
                foreach (var v in vehiculosCliente)
                {
                    if (v.CategoriaId.HasValue)
                    {
                        var cat = _categorias.FirstOrDefault(c => c.Id == v.CategoriaId.Value);
                        v.CategoriaNombre = cat?.Nombre ?? "";
                    }
                    if (v.TarifaId.HasValue)
                    {
                        var tar = _tarifas.FirstOrDefault(t => t.Id == v.TarifaId.Value);
                        v.TarifaNombre = tar?.Nombre ?? "";
                        
                        // Calcular valor mensual
                        if (v.TienePrecioDiferenciado && v.PrecioPersonalizado.HasValue)
                        {
                            v.ValorMensual = v.PrecioPersonalizado.Value;
                        }
                        else if (v.CategoriaId.HasValue && v.TarifaId.HasValue)
                        {
                            var precio = _dbService.ObtenerPrecio(v.TarifaId.Value, v.CategoriaId.Value);
                            v.ValorMensual = precio?.Monto ?? 0m;
                        }
                        else
                        {
                            v.ValorMensual = 0m;
                        }
                    }
                    else
                    {
                        v.ValorMensual = 0m;
                    }
                }
                
                if (DataGridMensualDetalleVehiculos != null)
                {
                    DataGridMensualDetalleVehiculos.ItemsSource = null;
                    DataGridMensualDetalleVehiculos.ItemsSource = vehiculosCliente;
                }
                
                if (BtnMensualDetalleTabVehiculos != null)
                {
                    BtnMensualDetalleTabVehiculos.Content = $"Vehículos ({vehiculosCliente.Count})";
                }
            }
            
            // Seleccionar el primer vehículo automáticamente si hay vehículos
            if (DataGridMensualDetalleVehiculos != null && DataGridMensualDetalleVehiculos.Items.Count > 0)
            {
                DataGridMensualDetalleVehiculos.SelectedIndex = 0;
                if (DataGridMensualDetalleVehiculos.SelectedItem is VehiculoMensual vehiculo)
                {
                    MostrarDetalleVehiculo(vehiculo);
                }
            }
            else
            {
                // Limpiar el detalle si no hay vehículos
                LimpiarDetalleVehiculo();
            }
        }
        
        private void DataGridMensualDetalleVehiculos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataGridMensualDetalleVehiculos?.SelectedItem is VehiculoMensual vehiculo)
            {
                MostrarDetalleVehiculo(vehiculo);
            }
            else
            {
                LimpiarDetalleVehiculo();
            }
        }
        
        private void MostrarDetalleVehiculo(VehiculoMensual vehiculo)
        {
            if (TxtDetalleVehiculoMatricula != null) TxtDetalleVehiculoMatricula.Text = $"#{vehiculo.Matricula}";
            if (TxtDetalleVehiculoMarcaModelo != null) TxtDetalleVehiculoMarcaModelo.Text = string.IsNullOrEmpty(vehiculo.MarcaModelo) ? "" : vehiculo.MarcaModelo;
            if (TxtDetalleVehiculoCategoria != null) TxtDetalleVehiculoCategoria.Text = vehiculo.CategoriaNombre ?? "";
            if (TxtDetalleVehiculoTarifa != null) TxtDetalleVehiculoTarifa.Text = vehiculo.TarifaNombre ?? "";
            if (TxtDetalleVehiculoUbicacion != null) TxtDetalleVehiculoUbicacion.Text = string.IsNullOrEmpty(vehiculo.Ubicacion) ? "Sin ubicación" : vehiculo.Ubicacion;
            if (TxtDetalleVehiculoNota != null) TxtDetalleVehiculoNota.Text = vehiculo.Nota ?? "Escriba aqui cualquier nota...";
        }
        
        private void LimpiarDetalleVehiculo()
        {
            if (TxtDetalleVehiculoMatricula != null) TxtDetalleVehiculoMatricula.Text = "";
            if (TxtDetalleVehiculoMarcaModelo != null) TxtDetalleVehiculoMarcaModelo.Text = "";
            if (TxtDetalleVehiculoCategoria != null) TxtDetalleVehiculoCategoria.Text = "";
            if (TxtDetalleVehiculoTarifa != null) TxtDetalleVehiculoTarifa.Text = "";
            if (TxtDetalleVehiculoUbicacion != null) TxtDetalleVehiculoUbicacion.Text = "Sin ubicación";
            if (TxtDetalleVehiculoNota != null) TxtDetalleVehiculoNota.Text = "Escriba aqui cualquier nota...";
        }
        
        private void BtnDetalleVehiculoEditar_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridMensualDetalleVehiculos?.SelectedItem is VehiculoMensual vehiculo)
            {
                // TODO: Implementar edición de vehículo
                MessageBox.Show("Funcionalidad de edición en desarrollo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void BtnDetalleVehiculoSeleccionarUbicacion_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridMensualDetalleVehiculos?.SelectedItem is VehiculoMensual vehiculo)
            {
                // TODO: Implementar selección de ubicación
                MessageBox.Show("Funcionalidad de selección de ubicación en desarrollo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        private void BtnDetalleVehiculoEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridMensualDetalleVehiculos?.SelectedItem is VehiculoMensual vehiculo)
            {
                var resultado = MessageBox.Show($"¿Está seguro de eliminar el vehículo {vehiculo.Matricula}? Esta acción no se puede deshacer.", 
                    "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (resultado == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dbService.EliminarVehiculoMensual(vehiculo.Id);
                        CargarMensuales();
                        if (_clienteMensualSeleccionado != null)
                        {
                            var cliente = _clientesMensuales.FirstOrDefault(c => c.Id == _clienteMensualSeleccionado.Id);
                            if (cliente != null)
                            {
                                MostrarDetalleMensual(cliente);
                                MostrarTabDetalleVehiculos();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar vehículo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        private void BtnDetalleVehiculoImprimirCodigo_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridMensualDetalleVehiculos?.SelectedItem is VehiculoMensual vehiculo)
            {
                // TODO: Implementar impresión de código de presencia
                MessageBox.Show("Funcionalidad de impresión en desarrollo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CalendarEntradasSalidas_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            AplicarFiltrosEntradasSalidas();
        }

        private void ImprimirReciboCobro(Ticket ticket, Tarifa tarifa, Categoria categoria, string formaPago, DateTime fechaCobro, string descripcion, List<string> items, decimal total)
        {
            if (_estacionamientoCache == null)
            {
                _estacionamientoCache = _dbService.ObtenerEstacionamiento();
            }
            if (_estacionamientoCache == null) return;

            var printerName = _estacionamientoCache.Impresora;
            if (string.IsNullOrWhiteSpace(printerName)) return;

            string placa = FormatearMatricula(ticket.Matricula);
            string estacionamientoNombre = _estacionamientoCache.Nombre;
            string direccion = _estacionamientoCache.Direccion ?? string.Empty;
            string ciudad = _estacionamientoCache.Ciudad ?? string.Empty;
            string cajero = _username;

            try
            {
                var printer = new EscPosPrinter(printerName);

                // Header
                printer.PrintEmptyLine(1);
                printer.PrintCenter("### Detalle ###", bold: true, fontSize: 0);
                printer.PrintCenter(estacionamientoNombre, bold: true, fontSize: 0);
                
                if (!string.IsNullOrWhiteSpace(direccion))
                    printer.PrintCenter(direccion, bold: false, fontSize: 0);
                if (!string.IsNullOrWhiteSpace(ciudad))
                    printer.PrintCenter(ciudad, bold: false, fontSize: 0);

                printer.PrintEmptyLine(1);
                printer.PrintCenter(fechaCobro.ToString("dd/MM/yy • HH:mm"), bold: false, fontSize: 0);
                printer.PrintCenter($"Cajero: {cajero}", bold: false, fontSize: 0);
                printer.PrintCenter($"Forma de Pago: {formaPago}", bold: false, fontSize: 0);
                printer.PrintEmptyLine(1);

                // Datos del ticket
                printer.PrintCenter($"MATRICULA {placa}", bold: true, fontSize: 0);
                printer.PrintCenter($"Categoría: {categoria.Nombre}", bold: false, fontSize: 0);
                printer.PrintCenter($"Tarifa: {tarifa.Nombre}", bold: false, fontSize: 0);
                printer.PrintCenter($"Entrada: {ticket.FechaEntrada:dd/MM HH:mm}", bold: false, fontSize: 0);
                
                string salidaTxt = ticket.FechaSalida.HasValue ? ticket.FechaSalida.Value.ToString("dd/MM HH:mm") : "--";
                printer.PrintCenter($"Salida: {salidaTxt}", bold: false, fontSize: 0);

                var diff = (ticket.FechaSalida ?? fechaCobro) - ticket.FechaEntrada;
                if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;
                printer.PrintCenter($"Permanencia: {FormatearDuracion(diff)}", bold: false, fontSize: 0);
                printer.PrintEmptyLine(1);

                // Descripción
                printer.PrintCenter("DESCRIPCIÓN", bold: true, fontSize: 0);
                printer.PrintCenter(string.IsNullOrWhiteSpace(descripcion) ? $"TICKET #{ticket.Id}" : descripcion, bold: false, fontSize: 0);
                printer.PrintEmptyLine(1);

                // Items
                printer.PrintCenter("ITEMS", bold: true, fontSize: 0);
                foreach (var it in items)
                {
                    printer.PrintCenter(it, bold: false, fontSize: 0);
                }
                printer.PrintEmptyLine(1);

                // Total
                printer.PrintCenter($"TOTAL: {total.ToString("$#,0.00")}", bold: true, fontSize: 1);
                printer.PrintEmptyLine(1);

                // Footer
                printer.PrintCenter("* NO VALIDO COMO FACTURA *", bold: false, fontSize: 0);
                printer.PrintEmptyLine(2);

                // Corte de papel
                printer.CutPaper(fullCut: true);

                // Enviar a impresora
                bool impreso = printer.Print();
                if (!impreso)
                {
                    MessageBox.Show($"Error al imprimir el recibo. Verifique que la impresora '{printerName}' esté conectada y configurada correctamente.", 
                        "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir recibo de cobro: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error al imprimir recibo de cobro: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ImprimirReciboMensual(MovimientoMensual movimiento, string formaPago)
        {
            if (_estacionamientoCache == null)
            {
                _estacionamientoCache = _dbService.ObtenerEstacionamiento();
            }
            if (_estacionamientoCache == null) return;

            var printerName = _estacionamientoCache.Impresora;
            if (string.IsNullOrWhiteSpace(printerName)) return;

            var cliente = _clienteMensualSeleccionado;
            if (cliente == null) return;

            string estacionamientoNombre = _estacionamientoCache.Nombre;
            string direccion = _estacionamientoCache.Direccion ?? string.Empty;
            string ciudad = _estacionamientoCache.Ciudad ?? string.Empty;
            string cajero = _username;
            string clienteNombre = cliente.NombreCompleto;
            string matriculaReferencia = movimiento.MatriculaReferencia ?? cliente.MatriculasConcat?.Split('•').FirstOrDefault()?.Trim() ?? "";

            try
            {
                var printer = new EscPosPrinter(printerName);

                // Header
                printer.PrintEmptyLine(1);
                printer.PrintCenter("### RECIBO ###", bold: true, fontSize: 0);
                printer.PrintCenter(estacionamientoNombre, bold: true, fontSize: 0);
                
                if (!string.IsNullOrWhiteSpace(direccion))
                    printer.PrintCenter(direccion, bold: false, fontSize: 0);
                if (!string.IsNullOrWhiteSpace(ciudad))
                    printer.PrintCenter(ciudad, bold: false, fontSize: 0);

                printer.PrintEmptyLine(1);
                printer.PrintCenter(movimiento.Fecha.ToString("dd/MM/yy • HH:mm"), bold: false, fontSize: 0);
                printer.PrintCenter($"Cajero: {cajero}", bold: false, fontSize: 0);
                printer.PrintCenter($"Forma de Pago: {formaPago}", bold: false, fontSize: 0);
                printer.PrintEmptyLine(1);

                // Cliente
                printer.PrintCenter($"CLIENTE: {clienteNombre}", bold: true, fontSize: 0);
                if (!string.IsNullOrWhiteSpace(matriculaReferencia))
                {
                    printer.PrintCenter($"MATRÍCULA: {FormatearMatricula(matriculaReferencia)}", bold: false, fontSize: 0);
                }
                printer.PrintEmptyLine(1);

                // Descripción
                printer.PrintCenter("DESCRIPCIÓN", bold: true, fontSize: 0);
                printer.PrintCenter(string.IsNullOrWhiteSpace(movimiento.Descripcion) ? $"MOVIMIENTO #{movimiento.Id}" : movimiento.Descripcion, bold: false, fontSize: 0);
                printer.PrintEmptyLine(1);

                // Total
                printer.PrintCenter($"TOTAL: {movimiento.Importe.ToString("$#,0.00")}", bold: true, fontSize: 1);
                printer.PrintEmptyLine(1);

                // Footer
                printer.PrintCenter("* NO VALIDO COMO FACTURA *", bold: false, fontSize: 0);
                printer.PrintEmptyLine(2);

                // Corte de papel
                printer.CutPaper(fullCut: true);

                // Enviar a impresora
                bool impreso = printer.Print();
                if (!impreso)
                {
                    MessageBox.Show($"Error al imprimir el recibo mensual. Verifique que la impresora '{printerName}' esté conectada y configurada correctamente.", 
                        "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir recibo mensual: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error al imprimir recibo mensual: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ImprimirTicket(int ticketId, Ticket ticket, Tarifa? tarifa, Categoria? categoria)
        {
            // Obtener estacionamiento (cache)
            if (_estacionamientoCache == null)
            {
                _estacionamientoCache = _dbService.ObtenerEstacionamiento();
            }
            if (_estacionamientoCache == null) return;

            var printerName = _estacionamientoCache.Impresora;
            if (string.IsNullOrWhiteSpace(printerName)) return; // no hay impresora configurada

            // Formatear placa
            string placa = ticket.Matricula ?? string.Empty;
            if (placa.Length == 7)
                placa = $"{placa.Substring(0, 2)}·{placa.Substring(2, 3)}·{placa.Substring(5, 2)}";
            else if (placa.Length == 6)
                placa = $"{placa.Substring(0, 3)}·{placa.Substring(3, 3)}";

            // Datos
            string nombreEmpresa = _estacionamientoCache.Nombre;
            string tarifaNombre = tarifa?.Nombre?.ToUpperInvariant() ?? "TARIFA";
            string categoriaNombre = categoria?.Nombre?.ToUpperInvariant() ?? "CATEGORIA";
            DateTime fechaEntrada = ticket.FechaEntrada;

            try
            {
                var printer = new EscPosPrinter(printerName);

                // Espacio inicial
                printer.PrintEmptyLine(1);

                // Logo/Header - Círculo con E (simulado con texto grande)
                printer.PrintCenter("E", bold: true, fontSize: 3);
                printer.PrintEmptyLine(1);

                // parking (negrita, tamaño doble ancho)
                printer.PrintCenter("parking", bold: true, fontSize: 1);
                printer.PrintEmptyLine(1);

                // Nombre del estacionamiento
                printer.PrintCenter(nombreEmpresa, bold: false, fontSize: 0);
                
                // Tarifa (en la misma línea o siguiente)
                printer.PrintCenter(tarifaNombre, bold: false, fontSize: 0);
                printer.PrintEmptyLine(1);

                // Código de barras con ID (centrado)
                printer.PrintBarcode(ticketId.ToString(), height: 80, width: 2, position: 2);
                printer.PrintEmptyLine(1);

                // Placa destacada (texto grande y negrita)
                printer.PrintCenter(placa, bold: true, fontSize: 2);
                printer.PrintEmptyLine(1);

                // Fecha/hora entrada
                string fechaStr = fechaEntrada.ToString("dd/MM/yy • HH:mm");
                printer.PrintCenter(fechaStr, bold: false, fontSize: 0);
                printer.PrintEmptyLine(1);

                // Footer
                printer.PrintCenter("Ticket emitido por Parking Co.", bold: false, fontSize: 0);
                printer.PrintEmptyLine(2);

                // Corte de papel
                printer.CutPaper(fullCut: true);

                // Enviar a impresora
                bool impreso = printer.Print();
                if (!impreso)
                {
                    MessageBox.Show($"Error al imprimir el ticket. Verifique que la impresora '{printerName}' esté conectada y configurada correctamente.", 
                        "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"Error al imprimir ticket: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private Bitmap? GenerarCode128(string data, int width, int height)
        {
            // Implementación mínima Code128-B
            if (string.IsNullOrEmpty(data)) return null;
            // Código 128 tabla de patrones (11 módulos cada uno)
            int[][] patterns = {
                new[]{2,1,2,2,2,2}, new[]{2,2,2,1,2,2}, new[]{2,2,2,2,2,1}, new[]{1,2,1,2,2,3},
                new[]{1,2,1,3,2,2}, new[]{1,3,1,2,2,2}, new[]{1,2,2,2,1,3}, new[]{1,2,2,3,1,2},
                new[]{1,3,2,2,1,2}, new[]{2,2,1,2,1,3}, new[]{2,2,1,3,1,2}, new[]{2,3,1,2,1,2},
                new[]{1,1,2,2,3,2}, new[]{1,2,2,1,3,2}, new[]{1,2,2,2,3,1}, new[]{1,1,3,2,2,2},
                new[]{1,2,3,1,2,2}, new[]{1,2,3,2,2,1}, new[]{2,2,3,2,1,1}, new[]{2,2,1,1,3,2},
                new[]{2,2,1,2,3,1}, new[]{2,1,3,2,1,2}, new[]{2,2,3,1,1,2}, new[]{3,1,2,1,3,1},
                new[]{3,1,1,2,2,2}, new[]{3,2,1,1,2,2}, new[]{3,2,1,2,2,1}, new[]{3,1,2,2,1,2},
                new[]{3,2,2,1,1,2}, new[]{3,2,2,2,1,1}, new[]{2,1,2,1,2,3}, new[]{2,1,2,3,2,1},
                new[]{2,3,2,1,2,1}, new[]{1,1,1,3,2,3}, new[]{1,3,1,1,2,3}, new[]{1,3,1,3,2,1},
                new[]{1,1,2,3,1,3}, new[]{1,3,2,1,1,3}, new[]{1,3,2,3,1,1}, new[]{2,1,1,3,1,3},
                new[]{2,3,1,1,1,3}, new[]{2,3,1,3,1,1}, new[]{1,1,2,1,3,3}, new[]{1,1,2,3,3,1},
                new[]{1,3,2,1,3,1}, new[]{1,1,3,1,2,3}, new[]{1,1,3,3,2,1}, new[]{1,3,3,1,2,1},
                new[]{3,1,3,1,2,1}, new[]{2,1,1,3,3,1}, new[]{2,3,1,1,3,1}, new[]{2,1,3,1,1,3},
                new[]{2,1,3,3,1,1}, new[]{2,1,3,1,3,1}, new[]{3,1,1,1,2,3}, new[]{3,1,1,3,2,1},
                new[]{3,3,1,1,2,1}, new[]{3,1,2,1,1,3}, new[]{3,1,2,3,1,1}, new[]{3,3,2,1,1,1},
                new[]{3,1,4,1,1,1}, new[]{2,2,1,4,1,1}, new[]{4,3,1,1,1,1}, new[]{1,1,1,2,2,4},
                new[]{1,1,1,4,2,2}, new[]{1,2,1,1,2,4}, new[]{1,2,1,4,2,1}, new[]{1,4,1,1,2,2},
                new[]{1,4,1,2,2,1}, new[]{1,1,2,2,1,4}, new[]{1,1,2,4,1,2}, new[]{1,2,2,1,1,4},
                new[]{1,2,2,4,1,1}, new[]{1,4,2,1,1,2}, new[]{1,4,2,2,1,1}, new[]{2,4,1,2,1,1},
                new[]{2,2,1,1,1,4}, new[]{4,1,3,1,1,1}, new[]{2,4,1,1,1,2}, new[]{1,3,4,1,1,1},
                new[]{1,1,1,2,4,2}, new[]{1,2,1,1,4,2}, new[]{1,2,1,2,4,1}, new[]{1,1,4,2,1,2},
                new[]{1,2,4,1,1,2}, new[]{1,2,4,2,1,1}, new[]{4,1,1,2,1,2}, new[]{4,2,1,1,1,2},
                new[]{4,2,1,2,1,1}, new[]{2,1,2,1,4,1}, new[]{2,1,4,1,2,1}, new[]{4,1,2,1,2,1},
                new[]{1,1,1,1,4,3}, new[]{1,1,1,3,4,1}, new[]{1,3,1,1,4,1}, new[]{1,1,4,1,1,3},
                new[]{1,1,4,3,1,1}, new[]{4,1,1,1,1,3}, new[]{4,1,1,3,1,1}, new[]{1,1,3,1,4,1},
                new[]{1,1,4,1,3,1}, new[]{3,1,1,1,4,1}, new[]{4,1,1,1,3,1}, new[]{2,1,1,4,1,2},
                new[]{2,1,1,2,1,4}, new[]{2,1,1,2,3,2}, new[]{2,3,3,1,1,1,2} // stop
            };

            // Start code B = 104, stop = 106
            var encoded = new System.Collections.Generic.List<int> { 104 };
            foreach (char c in data)
            {
                int code = c;
                if (code < 32 || code > 127) code = 32; // fallback
                code -= 32;
                encoded.Add(code);
            }
            // checksum
            int checksum = 104;
            for (int i = 0; i < encoded.Count - 1; i++)
            {
                checksum += encoded[i + 1] * (i + 1);
            }
            checksum %= 103;
            encoded.Add(checksum);
            encoded.Add(106); // stop

            // Construir barras
            var bars = new System.Collections.Generic.List<int>();
            foreach (int code in encoded)
            {
                var pat = patterns[code];
                bars.AddRange(pat);
            }
            // stop has 7 elements, already in patterns

            int totalModules = bars.Sum();
            double moduleWidth = (double)width / totalModules;

            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(System.Drawing.Color.White);
                double x = 0;
                bool black = true;
                foreach (int m in bars)
                {
                    double w = m * moduleWidth;
                    if (black)
                    {
                        g.FillRectangle(System.Drawing.Brushes.Black, (float)x, 0, (float)w, height);
                    }
                    x += w;
                    black = !black;
                }
            }
            return bmp;
        }

        private void CargarCategorias()
        {
            _categorias.Clear();
            var categorias = _dbService.ObtenerCategorias();
            foreach (var categoria in categorias)
            {
                _categorias.Add(categoria);
            }
            CargarFooter();
        }

        private void CargarTarifas()
        {
            _tarifas.Clear();
            var tarifas = _dbService.ObtenerTarifas();
            foreach (var tarifa in tarifas)
            {
                _tarifas.Add(tarifa);
            }
            CargarFiltroTarifasAbiertos();
        }

        private void CargarFormasPago()
        {
            _formasPago.Clear();
            var formas = _dbService.ObtenerFormasPago();
            foreach (var f in formas)
            {
                _formasPago.Add(f);
            }
        }

        private void CargarFiltroTarifasAbiertos()
        {
            if (CmbTodos == null) return;
            CmbTodos.Items.Clear();
            var itemTodos = new ComboBoxItem { Content = "Todos", Tag = null, IsSelected = true };
            CmbTodos.Items.Add(itemTodos);
            foreach (var t in _tarifas.Where(t => t.Tipo != TipoTarifa.Mensual))
            {
                CmbTodos.Items.Add(new ComboBoxItem { Content = t.Nombre, Tag = t.Id });
            }
            CmbTodos.SelectedIndex = 0;
        }

        private void CargarEstadisticas()
        {
            CargarFooter();
            CargarContadoresInicio();
        }

        private void CargarFooter()
        {
            if (PanelBotonesTarifas == null) return;

            // Limpiar panel de botones
            PanelBotonesTarifas.Children.Clear();

            // Obtener tarifas - EXCLUIR MENSUALES COMPLETAMENTE (solo tipos 1, 2, 3)
            var tarifas = _dbService.ObtenerTarifas();
            // Filtro estricto: excluir mensuales primero, luego solo incluir tipos válidos
            var tarifasNoMensuales = tarifas
                .Where(t => t.Tipo != TipoTarifa.Mensual) // Excluir mensuales
                .Where(t => (int)t.Tipo != 4) // Verificación adicional por valor numérico
                .Where(t => t.Tipo == TipoTarifa.PorHora || 
                           t.Tipo == TipoTarifa.PorTurno || 
                           t.Tipo == TipoTarifa.PorEstadia) // Solo tipos válidos
                .OrderBy(t => t.Id)
                .ToList();

            // Generar botones por tarifa
            GenerarBotonesPorTarifa(tarifasNoMensuales);
        }

        private void CargarContadoresInicio()
        {
            // Solo mostrar contadores en el inicio (cuando PanelTicketsAbiertos está visible)
            if (PanelContadoresInicio == null || PanelTicketsAbiertos == null) return;
            if (PanelTicketsAbiertos.Visibility != Visibility.Visible) return;

            // Limpiar contadores
            PanelContadoresInicio.Children.Clear();

            // Obtener tarifas
            var tarifas = _dbService.ObtenerTarifas();
            var tarifasNoMensuales = tarifas
                .Where(t => t.Tipo != TipoTarifa.Mensual)
                .Where(t => (int)t.Tipo != 4)
                .Where(t => t.Tipo == TipoTarifa.PorHora || 
                           t.Tipo == TipoTarifa.PorTurno || 
                           t.Tipo == TipoTarifa.PorEstadia)
                .OrderBy(t => t.Id)
                .ToList();
            var tarifaMensual = tarifas.FirstOrDefault(t => t.Tipo == TipoTarifa.Mensual);

            // Generar contadores en el panel de inicio
            GenerarContadores(tarifasNoMensuales, tarifaMensual);
        }

        private void GenerarContadores(List<Tarifa> tarifasNoMensuales, Tarifa? tarifaMensual)
        {
            if (PanelContadoresInicio == null) return;

            // Colores según el tema
            var colorLabel = _modoOscuro ? ColorTextoSecundarioOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            var colorValor = _modoOscuro ? ColorTextoOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));

            // xHora (primera tarifa no mensual)
            if (tarifasNoMensuales.Count > 0)
            {
                var primeraTarifa = tarifasNoMensuales[0];
                var stackPanel = new StackPanel { Margin = new Thickness(0, 0, 40, 0) };
                stackPanel.Children.Add(new TextBlock
                {
                    Text = primeraTarifa.Nombre,
                    FontSize = 12,
                    Foreground = colorLabel,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                var txtContador = new TextBlock
                {
                    Name = $"TxtContador{primeraTarifa.Id}",
                    Text = "0",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = colorValor,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stackPanel.Children.Add(txtContador);
                PanelContadoresInicio.Children.Add(stackPanel);
            }

            // Mensuales
            if (tarifaMensual != null)
            {
                var stackPanel = new StackPanel { Margin = new Thickness(0, 0, 40, 0) };
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Mensuales",
                    FontSize = 12,
                    Foreground = colorLabel,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                var txtContador = new TextBlock
                {
                    Name = "TxtContadorMensuales",
                    Text = "0",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = colorValor,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stackPanel.Children.Add(txtContador);
                PanelContadoresInicio.Children.Add(stackPanel);
            }

            // Total x Tickets
            var stackTotal = new StackPanel { Margin = new Thickness(0, 0, 40, 0) };
            stackTotal.Children.Add(new TextBlock
            {
                Text = "Total x Tickets",
                FontSize = 12,
                Foreground = colorLabel,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            var txtTotal = new TextBlock
            {
                Name = "TxtEstadisticaTotalTickets",
                Text = _dbService.ObtenerCantidadTicketsAbiertos().ToString(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = colorValor,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackTotal.Children.Add(txtTotal);
            PanelContadoresInicio.Children.Add(stackTotal);

            // Entradas ult. 24 hs
            var stackEntradas = new StackPanel { Margin = new Thickness(0, 0, 40, 0) };
            stackEntradas.Children.Add(new TextBlock
            {
                Text = "Entradas ult. 24 hs",
                FontSize = 12,
                Foreground = colorLabel,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            var txtEntradas = new TextBlock
            {
                Name = "TxtEstadisticaEntradas24",
                Text = _dbService.ObtenerEntradasUltimas24Horas().ToString(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = colorValor,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackEntradas.Children.Add(txtEntradas);
            PanelContadoresInicio.Children.Add(stackEntradas);

            // Salidas ult. 24 hs
            var stackSalidas = new StackPanel();
            stackSalidas.Children.Add(new TextBlock
            {
                Text = "Salidas ult. 24 hs",
                FontSize = 12,
                Foreground = colorLabel,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            var txtSalidas = new TextBlock
            {
                Name = "TxtEstadisticaSalidas24",
                Text = _dbService.ObtenerSalidasUltimas24Horas().ToString(),
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = colorValor,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stackSalidas.Children.Add(txtSalidas);
            PanelContadoresInicio.Children.Add(stackSalidas);

            // xTurno (si existe segunda tarifa no mensual)
            if (tarifasNoMensuales.Count > 1)
            {
                var segundaTarifa = tarifasNoMensuales[1];
                var stackTurno = new StackPanel { Margin = new Thickness(40, 0, 0, 0) };
                stackTurno.Children.Add(new TextBlock
                {
                    Text = segundaTarifa.Nombre,
                    FontSize = 12,
                    Foreground = colorLabel,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                var txtTurno = new TextBlock
                {
                    Name = $"TxtContador{segundaTarifa.Id}",
                    Text = "0",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = colorValor,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                stackTurno.Children.Add(txtTurno);
                PanelContadoresInicio.Children.Add(stackTurno);
            }
        }

        private void GenerarBotonesPorTarifa(List<Tarifa> tarifasNoMensuales)
        {
            // Filtrar SOLO tipos 1, 2 y 3 - EXCLUIR COMPLETAMENTE tipo 4 (Mensual)
            // Filtro explícito y estricto: SOLO PorHora, PorTurno, PorEstadia
            var tarifasFiltradas = tarifasNoMensuales
                .Where(t => t.Tipo != TipoTarifa.Mensual) // Primero excluir mensuales
                .Where(t => t.Tipo == TipoTarifa.PorHora || 
                           t.Tipo == TipoTarifa.PorTurno || 
                           t.Tipo == TipoTarifa.PorEstadia) // Luego solo incluir tipos válidos
                .OrderBy(t => t.Id)
                .ToList();
            
            // Verificación final: asegurar que NO haya ninguna tarifa mensual
            tarifasFiltradas = tarifasFiltradas
                .Where(t => (int)t.Tipo != 4) // Verificación adicional por valor numérico
                .Where(t => t.Tipo != TipoTarifa.Mensual) // Verificación por enum
                .ToList();

            // Crear una sola fila horizontal con todas las tarifas
            var filaPrincipal = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Colores diferentes para cada tarifa
            var coloresTarifas = new[]
            {
                System.Windows.Media.Color.FromRgb(59, 130, 246),   // Azul
                System.Windows.Media.Color.FromRgb(16, 185, 129),   // Verde
                System.Windows.Media.Color.FromRgb(245, 158, 11),   // Naranja
                System.Windows.Media.Color.FromRgb(239, 68, 68),    // Rojo
                System.Windows.Media.Color.FromRgb(139, 92, 246),   // Púrpura
                System.Windows.Media.Color.FromRgb(236, 72, 153)    // Rosa
            };

            int indiceTarifa = 0;
            foreach (var tarifa in tarifasFiltradas)
            {
                var colorTarifa = coloresTarifas[indiceTarifa % coloresTarifas.Length];
                var colorBorde = new SolidColorBrush(colorTarifa);
                var colorFondo = new SolidColorBrush(System.Windows.Media.Color.FromArgb(30, colorTarifa.R, colorTarifa.G, colorTarifa.B));
                var colorTexto = colorBorde;

                // Agregar nombre de la tarifa
                var txtTarifa = new TextBlock
                {
                    Text = tarifa.Nombre,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = colorTexto,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                filaPrincipal.Children.Add(txtTarifa);

                // Agregar botones para cada categoría
                int indiceCategoria = 0;
                foreach (var categoria in _categorias.OrderBy(c => c.Orden))
                {
                    string atajo = GenerarAtajoTeclado(indiceTarifa, indiceCategoria);
                    
                    var border = new Border
                    {
                        CornerRadius = new CornerRadius(5),
                        BorderBrush = colorBorde,
                        BorderThickness = new Thickness(1.5, 1.5, 1.5, 1.5),
                        Background = colorFondo,
                        Margin = new Thickness(0, 0, 4, 0),
                        Padding = new Thickness(6, 2, 6, 2)
                    };

                    var button = new Button
                    {
                        Height = 24,
                        MinWidth = 65,
                        FontSize = 9,
                        FontWeight = FontWeights.SemiBold,
                        Background = System.Windows.Media.Brushes.Transparent,
                        Foreground = colorTexto,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        Cursor = Cursors.Hand,
                        Content = $"[{atajo}] {categoria.Nombre.ToUpper()}",
                        Tag = new { Tarifa = tarifa, Categoria = categoria },
                        Padding = new Thickness(0, 0, 0, 0)
                    };
                    button.Click += BtnTarifaCategoria_Click;

                    border.Child = button;
                    filaPrincipal.Children.Add(border);
                    indiceCategoria++;
                }

                // Separador entre tarifas (excepto la última)
                if (indiceTarifa < tarifasFiltradas.Count - 1)
                {
                    var colorSeparador = _modoOscuro ? ColorBordeOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));
                    var separador = new Border
                    {
                        Width = 1,
                        Height = 25,
                        Background = colorSeparador,
                        Margin = new Thickness(12, 0, 12, 0)
                    };
                    filaPrincipal.Children.Add(separador);
                }

                indiceTarifa++;
            }

            // Separador antes de botones finales
            if (tarifasFiltradas.Count > 0)
            {
                var colorSeparador = _modoOscuro ? ColorBordeOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));
                var separadorFinal = new Border
                {
                    Width = 1,
                    Height = 30,
                    Background = colorSeparador,
                    Margin = new Thickness(10, 0, 10, 0)
                };
                filaPrincipal.Children.Add(separadorFinal);
            }

            // Cerrar Ticket
            var colorBotonFondo = _modoOscuro ? ColorFondoSecundarioOscuro : System.Windows.Media.Brushes.White;
            var colorBotonTexto = _modoOscuro ? ColorTextoOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            var colorBotonBorde = _modoOscuro ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)) : new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));

            var borderCerrar = new Border
            {
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 10, 0)
            };
            var btnCerrar = new Button
            {
                Name = "BtnCerrarTicket",
                Content = "[ESPACIO] CERRAR TICKET",
                Height = 45,
                MinWidth = 180,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Background = colorBotonFondo,
                Foreground = colorBotonTexto,
                BorderBrush = colorBotonBorde,
                BorderThickness = new Thickness(1, 1, 1, 1),
                Cursor = Cursors.Hand
            };
            btnCerrar.Click += BtnCerrarTicket_Click;
            borderCerrar.Child = btnCerrar;
            filaPrincipal.Children.Add(borderCerrar);

            // Cerrar Sesión
            var borderSesion = new Border { CornerRadius = new CornerRadius(4) };
            var btnSesion = new Button
            {
                Name = "BtnCerrarSesionFooter",
                Content = "[CTRL+0] CERRAR SESIÓN",
                Height = 45,
                MinWidth = 200,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Background = colorBotonFondo,
                Foreground = colorBotonTexto,
                BorderBrush = colorBotonBorde,
                BorderThickness = new Thickness(1, 1, 1, 1),
                Cursor = Cursors.Hand
            };
            btnSesion.Click += BtnCerrarSesionFooter_Click;
            borderSesion.Child = btnSesion;
            filaPrincipal.Children.Add(borderSesion);

            PanelBotonesTarifas.Children.Add(filaPrincipal);
        }

        private string GenerarAtajoTeclado(int indiceTarifa, int indiceCategoria)
        {
            if (indiceTarifa == 0)
            {
                // Primera tarifa: F1, F2, F3, etc.
                return $"F{indiceCategoria + 1}";
            }
            else if (indiceTarifa == 1)
            {
                // Segunda tarifa: Shift+F1, Shift+F2, Shift+F3, etc.
                return $"SHIFT+F{indiceCategoria + 1}";
            }
            else
            {
                // Tercera tarifa y siguientes: Ctrl+F1, Ctrl+F2, etc. o combinaciones más complejas
                return $"CTRL+F{indiceCategoria + 1}";
            }
        }

        private void BtnTarifaCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                dynamic tag = btn.Tag;
                var tarifa = tag.Tarifa as Tarifa;
                var categoria = tag.Categoria as Categoria;
                
                if (tarifa != null && categoria != null)
                {
                    MostrarPopupCrearTicket(tarifa, categoria);
                }
            }
        }

        private void MostrarPopupCrearTicket(Tarifa tarifa, Categoria categoria)
        {
            _tarifaSeleccionada = tarifa;
            _categoriaSeleccionada = categoria;

            if (TxtCrearTicketTitulo != null)
                TxtCrearTicketTitulo.Text = $"Ingreso {tarifa.Nombre}";
            if (TxtCrearTicketCategoria != null)
                TxtCrearTicketCategoria.Text = categoria.Nombre;
            if (BtnCrearTicketConfirmar != null)
                BtnCrearTicketConfirmar.Content = $"Ingresar {categoria.Nombre}";

            if (TxtCrearTicketMatricula != null)
            {
                TxtCrearTicketMatricula.Text = string.Empty;
                TxtCrearTicketMatricula.Foreground = System.Windows.Media.Brushes.Black;
            }
            if (TxtCrearTicketDescripcion != null)
                TxtCrearTicketDescripcion.Text = string.Empty;

            // ingreso previo por defecto oculto
            if (ChkIngresoPrevio != null) ChkIngresoPrevio.IsChecked = false;
            if (PanelIngresoPrevio != null) PanelIngresoPrevio.Visibility = Visibility.Collapsed;

            // set fecha actual
            var ahora = DateTime.Now;
            if (DpIngresoPrevio != null) DpIngresoPrevio.SelectedDate = ahora.Date;
            if (CmbHoraIngreso != null)
            {
                CmbHoraIngreso.Items.Clear();
                for (int i = 0; i < 24; i++)
                    CmbHoraIngreso.Items.Add(i.ToString("00"));
                CmbHoraIngreso.SelectedIndex = ahora.Hour;
            }
            if (CmbMinutoIngreso != null)
            {
                CmbMinutoIngreso.Items.Clear();
                for (int i = 0; i < 60; i++)
                    CmbMinutoIngreso.Items.Add(i.ToString("00"));
                CmbMinutoIngreso.SelectedIndex = ahora.Minute;
            }

            if (PopupCrearTicket != null)
                PopupCrearTicket.IsOpen = true;

            // Enfocar matrícula al abrir
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtCrearTicketMatricula?.Focus();
                TxtCrearTicketMatricula?.SelectAll();
            }), DispatcherPriority.Input);
        }

        private void InicializarAtajosTeclado()
        {
            this.KeyDown += HomeWindow_KeyDown;
            this.PreviewKeyDown += HomeWindow_PreviewKeyDown;
        }

        private void HomeWindow_KeyDown(object sender, KeyEventArgs e)
        {
            var tarifas = _dbService.ObtenerTarifas();
            var tarifasNoMensuales = tarifas.Where(t => t.Tipo != TipoTarifa.Mensual).OrderBy(t => t.Id).ToList();
            var categorias = _categorias.OrderBy(c => c.Orden).ToList();

            // Manejar F1-F12 para primera tarifa
            if (e.Key >= Key.F1 && e.Key <= Key.F12 && Keyboard.Modifiers == ModifierKeys.None)
            {
                int numeroF = (int)e.Key - (int)Key.F1;
                if (tarifasNoMensuales.Count > 0 && numeroF < categorias.Count)
                {
                    var tarifa = tarifasNoMensuales[0];
                    var categoria = categorias[numeroF];
                    MostrarPopupCrearTicket(tarifa, categoria);
                    e.Handled = true;
                }
            }
            // Manejar Shift+F1-F12 para segunda tarifa
            else if (e.Key >= Key.F1 && e.Key <= Key.F12 && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                int numeroF = (int)e.Key - (int)Key.F1;
                if (tarifasNoMensuales.Count > 1 && numeroF < categorias.Count)
                {
                    var tarifa = tarifasNoMensuales[1];
                    var categoria = categorias[numeroF];
                    MostrarPopupCrearTicket(tarifa, categoria);
                    e.Handled = true;
                }
            }
            // Espacio para cerrar ticket
            else if (e.Key == Key.Space && Keyboard.Modifiers == ModifierKeys.None)
            {
                MostrarPopupCerrarPorMatricula();
                e.Handled = true;
            }
            // Ctrl+0 para cerrar sesión
            else if (e.Key == Key.D0 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BtnCerrarSesionFooter_Click(sender, e);
                e.Handled = true;
            }
        }

        private void HomeWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // CTRL+0 para cerrar sesión
            if (e.Key == Key.D0 && Keyboard.Modifiers == ModifierKeys.Control)
            {
                BtnCerrarSesionFooter_Click(sender, e);
                e.Handled = true;
            }
        }

        private void HomeWindow_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Acumular input de pistola láser (solo dígitos)
            if (e.Text.All(char.IsDigit))
            {
                var now = DateTime.Now;
                if ((now - _lastScanTime).TotalMilliseconds > ScannerTimeoutMs)
                {
                    _scannerBuffer.Clear();
                }
                _scannerBuffer.Append(e.Text);
                _lastScanTime = now;
            }
            else
            {
                // Si llega algo no dígito, resetear para no mezclar
                _scannerBuffer.Clear();
            }
        }

        private bool TryProcesarScan()
        {
            if (_scannerBuffer.Length == 0) return false;
            string texto = _scannerBuffer.ToString();
            _scannerBuffer.Clear();

            if (!int.TryParse(texto, out int ticketId))
            {
                return false;
            }

            var ticket = _tickets.FirstOrDefault(t => t.Id == ticketId && t.EstaAbierto && !t.EstaCancelado);
            if (ticket == null)
            {
                MessageBox.Show("No se encontró un ticket abierto con ese ID.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }

            MostrarPopupCobrarTicket(ticket);
            return true;
        }

        private void BtnCerrarTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Ticket ticket)
            {
                MostrarPopupCobrarTicket(ticket);
            }
        }

        private void BtnCerrarSesionFooter_Click(object sender, RoutedEventArgs e)
        {
            BtnCerrarSesion_Click(sender, e);
        }

        private void CargarTicketsAbiertos()
        {
            _tickets.Clear();
            if (_categorias.Count == 0) CargarCategorias();
            if (_tarifas.Count == 0) CargarTarifas();

            var tickets = _dbService.ObtenerTicketsAbiertos();
            foreach (var ticket in tickets)
            {
                if (ticket.CategoriaId.HasValue)
                {
                    var cat = _categorias.FirstOrDefault(c => c.Id == ticket.CategoriaId.Value);
                    if (cat != null) ticket.CategoriaNombre = cat.Nombre;
                }
                if (ticket.TarifaId.HasValue)
                {
                    var t = _tarifas.FirstOrDefault(x => x.Id == ticket.TarifaId.Value);
                    if (t != null) ticket.TarifaNombre = t.Nombre;
                }
                if (string.IsNullOrWhiteSpace(ticket.TarifaNombre) && _tarifas.Count > 0)
                {
                    var t = _tarifas.FirstOrDefault(x => x.Id == ticket.TarifaId);
                    if (t != null) ticket.TarifaNombre = t.Nombre;
                }
                if (string.IsNullOrWhiteSpace(ticket.CategoriaNombre) && _categorias.Count > 0)
                {
                    var c = _categorias.FirstOrDefault(x => x.Id == ticket.CategoriaId);
                    if (c != null) ticket.CategoriaNombre = c.Nombre;
                }
                _tickets.Add(ticket);
            }
            AplicarFiltros();
            CargarEstadisticas();
        }

        private void CargarTicketsCerrados()
        {
            _ticketsCerrados.Clear();
            if (_categorias.Count == 0) CargarCategorias();
            if (_tarifas.Count == 0) CargarTarifas();
            var admins = _dbService.ObtenerTodosLosAdmins();

            var tickets = _dbService.ObtenerTicketsCerrados();
            foreach (var ticket in tickets)
            {
                if (ticket.CategoriaId.HasValue)
                {
                    var cat = _categorias.FirstOrDefault(c => c.Id == ticket.CategoriaId.Value);
                    if (cat != null) ticket.CategoriaNombre = cat.Nombre;
                }
                if (ticket.TarifaId.HasValue)
                {
                    var t = _tarifas.FirstOrDefault(x => x.Id == ticket.TarifaId.Value);
                    if (t != null) ticket.TarifaNombre = t.Nombre;
                }
                if (ticket.AdminCreadorId.HasValue)
                {
                    var admin = admins.FirstOrDefault(a => a.Id == ticket.AdminCreadorId.Value);
                    ticket.AdminCreadorNombre = admin != null ? admin.Username : string.Empty;
                }
                if (ticket.AdminCerradorId.HasValue)
                {
                    var admin = admins.FirstOrDefault(a => a.Id == ticket.AdminCerradorId.Value);
                    ticket.AdminCerradorNombre = admin != null ? admin.Username : string.Empty;
                }
                // Tiempo total display
                if (ticket.FechaSalida.HasValue)
                {
                    var diff = ticket.FechaSalida.Value - ticket.FechaEntrada;
                    if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;
                    ticket.TiempoTotalDisplay = $"{(int)diff.TotalHours:00}h{diff.Minutes:00}mins";
                }
                else if (ticket.FechaCancelacion.HasValue)
                {
                    var diff = ticket.FechaCancelacion.Value - ticket.FechaEntrada;
                    if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;
                    ticket.TiempoTotalDisplay = $"{(int)diff.TotalHours:00}h{diff.Minutes:00}mins";
                }
                else
                {
                    ticket.TiempoTotalDisplay = "--";
                }
                ticket.MontoDisplay = (ticket.Monto ?? CalcularImporteActual(ticket)).ToString("$#,0.00");
                ticket.ObservacionDisplay = ticket.EstaCancelado ? ticket.MotivoCancelacion : ticket.NotaAdicional;
                if (string.IsNullOrWhiteSpace(ticket.TarifaNombre) && _tarifas.Count > 0)
                {
                    var t = _tarifas.FirstOrDefault(x => x.Id == ticket.TarifaId);
                    if (t != null) ticket.TarifaNombre = t.Nombre;
                }
                if (string.IsNullOrWhiteSpace(ticket.CategoriaNombre) && _categorias.Count > 0)
                {
                    var c = _categorias.FirstOrDefault(x => x.Id == ticket.CategoriaId);
                    if (c != null) ticket.CategoriaNombre = c.Nombre;
                }
                _ticketsCerrados.Add(ticket);
            }
        }

        private void AplicarFiltros()
        {
            // Asegurar que _ticketsFiltrados esté inicializado
            if (_ticketsFiltrados == null)
            {
                _ticketsFiltrados = new ObservableCollection<Ticket>();
            }
            
            // Verificar que _tickets esté inicializado
            if (_tickets == null)
            {
                return;
            }
            
            _ticketsFiltrados.Clear();
            string filtroMatricula = TxtFiltroMatricula?.Text?.ToLower() ?? string.Empty;
            int? tarifaSeleccionada = null;
            if (CmbTodos != null && CmbTodos.SelectedItem is ComboBoxItem item && item.Tag is int idTarifa)
            {
                tarifaSeleccionada = idTarifa;
            }

            var ticketsFiltrados = new List<Ticket>();

            foreach (var ticket in _tickets)
            {
                bool cumpleFiltro = true;
                if (tarifaSeleccionada.HasValue && ticket.TarifaId != tarifaSeleccionada.Value)
                    cumpleFiltro = false;
                
                // Filtro por matrícula
                if (!string.IsNullOrWhiteSpace(filtroMatricula) && 
                    !ticket.Matricula.ToLower().Contains(filtroMatricula))
                {
                    cumpleFiltro = false;
                }
                
                if (cumpleFiltro)
                {
                    ticketsFiltrados.Add(ticket);
                }
            }

            // Aplicar ordenamiento
            if (CmbOrden.SelectedItem is ComboBoxItem selectedItem && CmbOrden.SelectedIndex >= 0)
            {
                string orden = selectedItem.Content?.ToString() ?? "";
                
                switch (orden)
                {
                    case "Últimos ingresados":
                        ticketsFiltrados = ticketsFiltrados.OrderByDescending(t => t.FechaEntrada).ToList();
                        break;
                    case "Primeros ingresados":
                        ticketsFiltrados = ticketsFiltrados.OrderBy(t => t.FechaEntrada).ToList();
                        break;
                    case "Matrícula A-Z":
                        ticketsFiltrados = ticketsFiltrados.OrderBy(t => t.Matricula, StringComparer.OrdinalIgnoreCase).ToList();
                        break;
                    case "Matrícula Z-A":
                        ticketsFiltrados = ticketsFiltrados.OrderByDescending(t => t.Matricula, StringComparer.OrdinalIgnoreCase).ToList();
                        break;
                    default:
                        // Orden por defecto (últimos ingresados)
                        ticketsFiltrados = ticketsFiltrados.OrderByDescending(t => t.FechaEntrada).ToList();
                        break;
                }
            }
            else
            {
                // Orden por defecto (últimos ingresados)
                ticketsFiltrados = ticketsFiltrados.OrderByDescending(t => t.FechaEntrada).ToList();
            }

            foreach (var ticket in ticketsFiltrados)
            {
                _ticketsFiltrados.Add(ticket);
            }

            if (ItemsTickets != null)
            {
                ItemsTickets.ItemsSource = _ticketsFiltrados;
                
                // Aplicar tema a tickets después de asignar ItemsSource
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AplicarTemaATicketsRenderizados();
                }), DispatcherPriority.Loaded);
            }
            
            // Mostrar mensaje si no hay tickets
            if (TxtNoHayTickets != null)
            {
                TxtNoHayTickets.Visibility = _ticketsFiltrados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void InicializarTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => TxtHora.Text = DateTime.Now.ToString("HH:mm:ss");
            _timer.Start();
            TxtHora.Text = DateTime.Now.ToString("HH:mm:ss");

            // Timer para actualizar tiempo en las cards cada minuto
            _timerTiempoTickets = new DispatcherTimer();
            _timerTiempoTickets.Interval = TimeSpan.FromMinutes(1);
            _timerTiempoTickets.Tick += (s, e) => ActualizarTiempoTicketsRenderizados();
            _timerTiempoTickets.Start();
            
            // Timer para actualizar estadísticas cada 30 segundos
            _timerEstadisticas = new DispatcherTimer();
            _timerEstadisticas.Interval = TimeSpan.FromSeconds(30);
            _timerEstadisticas.Tick += (s, e) => CargarEstadisticas();
            _timerEstadisticas.Start();
            
            // Inicializar estado por defecto
            PanelSubmenu.Visibility = Visibility.Visible;
            ResetearBotonesSubmenu();
            BtnTicketsAbiertos.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnTicketsAbiertos.Foreground = System.Windows.Media.Brushes.White;
            
        }

        // Eventos del menú principal
        private void BtnInicio_Click(object sender, RoutedEventArgs e)
        {
            PanelSubmenu.Visibility = Visibility.Visible;
            PanelSubmenuConfiguracion.Visibility = Visibility.Collapsed;
            if (BarraFiltrosAbiertos != null)
                BarraFiltrosAbiertos.Visibility = Visibility.Visible;
            PanelTicketsAbiertos.Visibility = Visibility.Visible;
            if (PanelEntradasSalidas != null) PanelEntradasSalidas.Visibility = Visibility.Collapsed;
            PanelConfiguracion.Visibility = Visibility.Collapsed;
            if (PanelMensuales != null) PanelMensuales.Visibility = Visibility.Collapsed;
            ResetearBotonesMenu();
            BtnInicio.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            MostrarTicketsAbiertos();
            CargarContadoresInicio(); // Cargar contadores cuando se muestra el inicio
        }

        private void BtnMensuales_Click(object sender, RoutedEventArgs e)
        {
            PanelSubmenu.Visibility = Visibility.Collapsed;
            ResetearBotonesMenu();
            BtnMensuales.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            if (PanelTicketsAbiertos != null) PanelTicketsAbiertos.Visibility = Visibility.Collapsed;
            if (PanelTicketsCerrados != null) PanelTicketsCerrados.Visibility = Visibility.Collapsed;
            if (PanelEntradasSalidas != null) PanelEntradasSalidas.Visibility = Visibility.Collapsed;
            if (PanelConfiguracion != null) PanelConfiguracion.Visibility = Visibility.Collapsed;
            if (BarraFiltrosAbiertos != null) BarraFiltrosAbiertos.Visibility = Visibility.Collapsed;
            if (PanelMensuales != null) PanelMensuales.Visibility = Visibility.Visible;
            if (PanelContadoresInicio != null) PanelContadoresInicio.Visibility = Visibility.Collapsed;
            CargarMensuales();
            MostrarTabMensualesClientes();
        }

        private void BtnCaja_Click(object sender, RoutedEventArgs e)
        {
            PanelSubmenu.Visibility = Visibility.Collapsed;
            if (PanelMensuales != null) PanelMensuales.Visibility = Visibility.Collapsed;
            ResetearBotonesMenu();
            BtnCaja.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            // TODO: Mostrar contenido de Caja
        }

        private void BtnConfiguracion_Click(object sender, RoutedEventArgs e)
        {
            PanelSubmenu.Visibility = Visibility.Collapsed;
            if (PanelSubmenuConfiguracion != null)
                PanelSubmenuConfiguracion.Visibility = Visibility.Visible;
            if (BarraFiltrosAbiertos != null)
                BarraFiltrosAbiertos.Visibility = Visibility.Collapsed;
            if (PanelTicketsAbiertos != null)
                PanelTicketsAbiertos.Visibility = Visibility.Collapsed;
            if (PanelEntradasSalidas != null)
                PanelEntradasSalidas.Visibility = Visibility.Collapsed;
            if (PanelMensuales != null) PanelMensuales.Visibility = Visibility.Collapsed;
            if (PanelConfiguracion != null)
                PanelConfiguracion.Visibility = Visibility.Visible;
            ResetearBotonesMenu();
            BtnConfiguracion.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            
            // Mostrar Categorías por defecto
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigCategorias != null)
            {
                BtnConfigCategorias.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigCategorias.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("Categorias");
            CargarCategoriasLista();
            ActualizarPestanasModulos();
        }
        
        public void ActualizarCategorias()
        {
            CargarCategorias();
            if (ItemsCategoriasLista != null)
            {
                CargarCategoriasLista();
            }
        }
        
        private ObservableCollection<Categoria> _categoriasLista = new ObservableCollection<Categoria>();
        private Categoria? _categoriaArrastrando;
        private System.Windows.Point _puntoInicioArrastre;
        private Border? _borderArrastrando;
        private System.Windows.Point _offsetDrag;
        private Window? _dragPreviewWindow;
        private bool _isDragging = false;
        
        private void CargarCategoriasLista()
        {
            _categoriasLista.Clear();
            var categorias = _dbService.ObtenerCategorias();
            foreach (var categoria in categorias)
            {
                _categoriasLista.Add(categoria);
            }
            if (ItemsCategoriasLista != null)
            {
                ItemsCategoriasLista.ItemsSource = _categoriasLista;
            }
        }
        
        private void ResetearBotonesSubmenuConfiguracion()
        {
            var transparente = System.Windows.Media.Brushes.Transparent;
            var gris = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));

            if (BtnConfigCategorias != null)
            {
                BtnConfigCategorias.Background = transparente;
                BtnConfigCategorias.Foreground = gris;
            }
            if (BtnConfigTarifas != null)
            {
                BtnConfigTarifas.Background = transparente;
                BtnConfigTarifas.Foreground = gris;
            }
            if (BtnConfigPrecios != null)
            {
                BtnConfigPrecios.Background = transparente;
                BtnConfigPrecios.Foreground = gris;
            }
            if (BtnConfigUsuarios != null)
            {
                BtnConfigUsuarios.Background = transparente;
                BtnConfigUsuarios.Foreground = gris;
            }
            if (BtnConfigMediosPago != null)
            {
                BtnConfigMediosPago.Background = transparente;
                BtnConfigMediosPago.Foreground = gris;
            }
            if (BtnConfigInformes != null)
            {
                BtnConfigInformes.Background = transparente;
                BtnConfigInformes.Foreground = gris;
            }
            if (BtnConfigAjustes != null)
            {
                BtnConfigAjustes.Background = transparente;
                BtnConfigAjustes.Foreground = gris;
            }
            
            // Resetear botones de módulos
            foreach (var btn in _botonesModulos.Values)
            {
                btn.Background = transparente;
                btn.Foreground = gris;
            }
        }
        
        private void MostrarPanelConfiguracion(string panelNombre)
        {
            if (PanelConfigCategorias != null)
                PanelConfigCategorias.Visibility = panelNombre == "Categorias" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelConfigTarifas != null)
                PanelConfigTarifas.Visibility = panelNombre == "Tarifas" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelConfigPrecios != null)
                PanelConfigPrecios.Visibility = panelNombre == "Precios" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelConfigUsuarios != null)
                PanelConfigUsuarios.Visibility = panelNombre == "Usuarios" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelConfigMediosPago != null)
                PanelConfigMediosPago.Visibility = panelNombre == "MediosPago" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelConfigInformes != null)
                PanelConfigInformes.Visibility = panelNombre == "Informes" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelConfigAjustes != null)
                PanelConfigAjustes.Visibility = panelNombre == "Ajustes" ? Visibility.Visible : Visibility.Collapsed;
            
            // Ocultar todos los paneles de módulos
            foreach (var panel in _panelesModulos.Values)
            {
                panel.Visibility = Visibility.Collapsed;
            }
        }
        
        private void BtnConfigCategorias_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigCategorias != null)
            {
                BtnConfigCategorias.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigCategorias.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("Categorias");
        }
        
        private void BtnConfigTarifas_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigTarifas != null)
            {
                BtnConfigTarifas.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigTarifas.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("Tarifas");
        }
        
        private void BtnConfigPrecios_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigPrecios != null)
            {
                BtnConfigPrecios.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigPrecios.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("Precios");
            CargarTablaPrecios();
        }
        
        private void CargarTablaPrecios()
        {
            if (GridPrecios == null) return;
            
            GridPrecios.Children.Clear();
            GridPrecios.ColumnDefinitions.Clear();
            GridPrecios.RowDefinitions.Clear();
            
            // Obtener tarifas y categorías
            var tarifas = _dbService.ObtenerTarifas();
            var categorias = _dbService.ObtenerCategorias();
            
            if (tarifas.Count == 0 || categorias.Count == 0) return;
            
            // Crear columnas: una para el header de tarifa y una por cada categoría
            GridPrecios.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Columna de headers de tarifa
            foreach (var categoria in categorias)
            {
                GridPrecios.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            }
            
            // Crear filas: una por cada tarifa
            foreach (var tarifa in tarifas)
            {
                GridPrecios.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            
            // Agregar headers de categorías en la primera fila (fila -1 para headers)
            GridPrecios.RowDefinitions.Insert(0, new RowDefinition { Height = GridLength.Auto });
            
            // Header de la primera columna (vacío)
            var headerVacio = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Padding = new Thickness(15, 15, 15, 15),
                Height = 50
            };
            Grid.SetRow(headerVacio, 0);
            Grid.SetColumn(headerVacio, 0);
            GridPrecios.Children.Add(headerVacio);
            
            // Headers de categorías
            for (int i = 0; i < categorias.Count; i++)
            {
                var headerCategoria = new Border
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1, 1, 1, 1),
                    Padding = new Thickness(15, 15, 15, 15),
                    Height = 50
                };
                var txtHeader = new TextBlock
                {
                    Text = categorias[i].Nombre,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                headerCategoria.Child = txtHeader;
                Grid.SetRow(headerCategoria, 0);
                Grid.SetColumn(headerCategoria, i + 1);
                GridPrecios.Children.Add(headerCategoria);
            }
            
            // Crear celdas de precios
            for (int i = 0; i < tarifas.Count; i++)
            {
                var tarifa = tarifas[i];
                int rowIndex = i + 1; // +1 porque la fila 0 es para headers
                
                // Header de tarifa (primera columna)
                var headerTarifa = new Border
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(1, 1, 1, 1),
                    Padding = new Thickness(15, 15, 15, 15),
                    Height = 60
                };
                var txtTarifa = new TextBlock
                {
                    Text = tarifa.Nombre,
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39)),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                headerTarifa.Child = txtTarifa;
                Grid.SetRow(headerTarifa, rowIndex);
                Grid.SetColumn(headerTarifa, 0);
                GridPrecios.Children.Add(headerTarifa);
                
                // Celdas de precio para cada categoría
                for (int j = 0; j < categorias.Count; j++)
                {
                    var categoria = categorias[j];
                    int colIndex = j + 1;
                    
                    // Obtener precio actual
                    var precio = _dbService.ObtenerPrecio(tarifa.Id, categoria.Id);
                    decimal monto = precio?.Monto ?? 100; // Valor por defecto: 100
                    
                    // Si no existe precio, crear uno con valor 100
                    if (precio == null)
                    {
                        _dbService.GuardarPrecio(tarifa.Id, categoria.Id, 100);
                        monto = 100;
                    }
                    
                    // Crear celda editable
                    var celda = new Border
                    {
                        Background = System.Windows.Media.Brushes.White,
                        BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                        BorderThickness = new Thickness(1, 1, 1, 1),
                        Padding = new Thickness(10, 10, 10, 10),
                        Height = 60
                    };
                    
                    var txtPrecio = new TextBox
                    {
                        Text = monto.ToString("F2", CultureInfo.InvariantCulture),
                        FontSize = 14,
                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(17, 24, 39)),
                        Background = System.Windows.Media.Brushes.White,
                        BorderThickness = new Thickness(0, 0, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Center,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        IsReadOnly = false,
                        Tag = new { TarifaId = tarifa.Id, CategoriaId = categoria.Id }
                    };
                    
                    // Evento para guardar cuando se pierde el foco
                    txtPrecio.LostFocus += (s, e) =>
                    {
                        if (s is TextBox txt && txt.Tag != null)
                        {
                            var tag = (dynamic)txt.Tag;
                            if (decimal.TryParse(txt.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal nuevoMonto))
                            {
                                if (nuevoMonto < 0) nuevoMonto = 0;
                                _dbService.GuardarPrecio(tag.TarifaId, tag.CategoriaId, nuevoMonto);
                                txt.Text = nuevoMonto.ToString("F2", CultureInfo.InvariantCulture);
                            }
                            else if (string.IsNullOrWhiteSpace(txt.Text))
                            {
                                // Si está vacío, poner 100 por defecto
                                _dbService.GuardarPrecio(tag.TarifaId, tag.CategoriaId, 100);
                                txt.Text = "100.00";
                            }
                            else
                            {
                                // Si no es válido, restaurar el valor anterior
                                var precioActual = _dbService.ObtenerPrecio(tag.TarifaId, tag.CategoriaId);
                                decimal valorActual = precioActual?.Monto ?? 100;
                                txt.Text = valorActual.ToString("F2", CultureInfo.InvariantCulture);
                            }
                        }
                    };
                    
                    // Solo permitir números y punto decimal
                    txtPrecio.PreviewTextInput += (s, e) =>
                    {
                        if (s is TextBox txt)
                        {
                            // Permitir solo números, punto y coma (para formato decimal)
                            char c = e.Text.Length > 0 ? e.Text[0] : ' ';
                            if (!char.IsDigit(c) && c != '.' && c != ',')
                            {
                                e.Handled = true;
                                return;
                            }
                            
                            // Reemplazar coma por punto
                            if (c == ',')
                            {
                                e.Handled = true;
                                txt.Text = txt.Text.Insert(txt.SelectionStart, ".");
                                txt.SelectionStart = txt.Text.Length;
                                return;
                            }
                            
                            // Verificar que solo haya un punto decimal
                            string newText = txt.Text.Insert(txt.SelectionStart, e.Text);
                            int puntoCount = newText.ToCharArray().Count(ch => ch == '.');
                            if (puntoCount > 1)
                            {
                                e.Handled = true;
                                return;
                            }
                        }
                    };
                    
                    // Permitir pegar solo números
                    txtPrecio.PreviewKeyDown += (s, e) =>
                    {
                        if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
                        {
                            // Permitir pegar, pero validar después
                            return;
                        }
                    };
                    
                    celda.Child = txtPrecio;
                    Grid.SetRow(celda, rowIndex);
                    Grid.SetColumn(celda, colIndex);
                    GridPrecios.Children.Add(celda);
                }
            }
        }
        
        private void BtnConfigUsuarios_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigUsuarios != null)
            {
                BtnConfigUsuarios.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigUsuarios.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("Usuarios");
            CargarUsuarios();
        }
        
        private void BtnConfigMediosPago_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigMediosPago != null)
            {
                BtnConfigMediosPago.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigMediosPago.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("MediosPago");
        }
        
        private void BtnConfigInformes_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigInformes != null)
            {
                BtnConfigInformes.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigInformes.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("Informes");
        }
        
        private void BtnConfigAjustes_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenuConfiguracion();
            if (BtnConfigAjustes != null)
            {
                BtnConfigAjustes.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                BtnConfigAjustes.Foreground = System.Windows.Media.Brushes.White;
            }
            MostrarPanelConfiguracion("Ajustes");
            CargarAjustes();
        }

        private void CargarAjustes()
        {
            // Cargar datos del estacionamiento
            var estacionamiento = _dbService.ObtenerEstacionamiento();
            if (estacionamiento != null)
            {
                if (TxtAjustesNombre != null) TxtAjustesNombre.Text = estacionamiento.Nombre;
                if (TxtAjustesDireccion != null) TxtAjustesDireccion.Text = estacionamiento.Direccion;
                if (TxtAjustesCiudad != null) TxtAjustesCiudad.Text = estacionamiento.Ciudad;
                if (TxtAjustesTelefono != null) TxtAjustesTelefono.Text = estacionamiento.Telefono ?? string.Empty;
                if (TxtAjustesSlogan != null) TxtAjustesSlogan.Text = estacionamiento.Slogan ?? string.Empty;

                // Seleccionar país
                if (CmbAjustesPais != null)
                {
                    for (int i = 0; i < CmbAjustesPais.Items.Count; i++)
                    {
                        if (CmbAjustesPais.Items[i] is ComboBoxItem item && item.Tag?.ToString() == estacionamiento.Pais)
                        {
                            CmbAjustesPais.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Seleccionar impresora
                CargarImpresoras();
                if (CmbAjustesImpresora != null && !string.IsNullOrEmpty(estacionamiento.Impresora))
                {
                    for (int i = 0; i < CmbAjustesImpresora.Items.Count; i++)
                    {
                        if (CmbAjustesImpresora.Items[i] is ComboBoxItem item && item.Content?.ToString() == estacionamiento.Impresora)
                        {
                            CmbAjustesImpresora.SelectedIndex = i;
                            break;
                        }
                    }
                }

                // Tema: se mantiene en claro; ya no hay selector
            }

            // Mostrar tab Perfil por defecto
            MostrarTabAjustes("Perfil");
        }

        private void CargarImpresoras()
        {
            if (CmbAjustesImpresora == null) return;

            CmbAjustesImpresora.Items.Clear();
            
            try
            {
                // Obtener impresoras del sistema
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    var item = new ComboBoxItem
                    {
                        Content = printer,
                        Tag = printer
                    };
                    CmbAjustesImpresora.Items.Add(item);
                }
            }
            catch
            {
                // Si falla, agregar una opción por defecto
                CmbAjustesImpresora.Items.Add(new ComboBoxItem { Content = "Microsoft Print to PDF", Tag = "Microsoft Print to PDF" });
            }
        }

        private void MostrarTabAjustes(string tabNombre)
        {
            if (PanelTabPerfil != null)
                PanelTabPerfil.Visibility = tabNombre == "Perfil" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelTabTickets != null)
                PanelTabTickets.Visibility = tabNombre == "Tickets" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelTabPermisos != null)
                PanelTabPermisos.Visibility = tabNombre == "Permisos" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelTabMonitor != null)
                PanelTabMonitor.Visibility = tabNombre == "Pantalla Cliente" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelTabModulos != null)
                PanelTabModulos.Visibility = tabNombre == "Modulos" ? Visibility.Visible : Visibility.Collapsed;
            if (PanelTabBaseDatos != null)
                PanelTabBaseDatos.Visibility = tabNombre == "BaseDatos" ? Visibility.Visible : Visibility.Collapsed;

            // Actualizar estilos de botones
            var azul = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            var gris = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            var transparente = System.Windows.Media.Brushes.Transparent;

            if (BtnTabPerfil != null)
            {
                BtnTabPerfil.Background = tabNombre == "Perfil" ? azul : transparente;
                BtnTabPerfil.Foreground = tabNombre == "Perfil" ? System.Windows.Media.Brushes.White : gris;
            }
            if (BtnTabTickets != null)
            {
                BtnTabTickets.Background = tabNombre == "Tickets" ? azul : transparente;
                BtnTabTickets.Foreground = tabNombre == "Tickets" ? System.Windows.Media.Brushes.White : gris;
            }
            if (BtnTabPermisos != null)
            {
                BtnTabPermisos.Background = tabNombre == "Permisos" ? azul : transparente;
                BtnTabPermisos.Foreground = tabNombre == "Permisos" ? System.Windows.Media.Brushes.White : gris;
            }
            if (BtnTabMonitor != null)
            {
                BtnTabMonitor.Background = tabNombre == "Pantalla Cliente" ? azul : transparente;
                BtnTabMonitor.Foreground = tabNombre == "Pantalla Cliente" ? System.Windows.Media.Brushes.White : gris;
            }
            if (BtnTabModulos != null)
            {
                BtnTabModulos.Background = tabNombre == "Modulos" ? azul : transparente;
                BtnTabModulos.Foreground = tabNombre == "Modulos" ? System.Windows.Media.Brushes.White : gris;
            }
            if (BtnTabBaseDatos != null)
            {
                BtnTabBaseDatos.Background = tabNombre == "BaseDatos" ? azul : transparente;
                BtnTabBaseDatos.Foreground = tabNombre == "BaseDatos" ? System.Windows.Media.Brushes.White : gris;
            }
        }

        private void BtnTabPerfil_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabAjustes("Perfil");
        }

        private void BtnTabTickets_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabAjustes("Tickets");
        }

        private void BtnTabPermisos_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabAjustes("Permisos");
        }

        private void BtnTabMonitor_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabAjustes("Pantalla Cliente");
        }

        private void BtnTabModulos_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabAjustes("Modulos");
            CargarEstadoModulos();
        }

        private void CargarEstadoModulos()
        {
            try
            {
                var modulos = _dbService.ObtenerModulos();
                if (ChkModuloANPR != null) ChkModuloANPR.IsChecked = modulos.ContainsKey("ANPR") && modulos["ANPR"];
                if (ChkModuloTERMINAL != null) ChkModuloTERMINAL.IsChecked = modulos.ContainsKey("TERMINAL") && modulos["TERMINAL"];
                if (ChkModuloEMAIL != null) ChkModuloEMAIL.IsChecked = modulos.ContainsKey("EMAIL") && modulos["EMAIL"];
                if (ChkModuloMONITOR != null) ChkModuloMONITOR.IsChecked = modulos.ContainsKey("MONITOR") && modulos["MONITOR"];
                if (ChkModuloMERCADOPAGO != null) ChkModuloMERCADOPAGO.IsChecked = modulos.ContainsKey("MERCADOPAGO") && modulos["MERCADOPAGO"];
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar módulos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChkModulo_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                string nombreModulo = "";
                if (checkBox == ChkModuloANPR) nombreModulo = "ANPR";
                else if (checkBox == ChkModuloTERMINAL) nombreModulo = "TERMINAL";
                else if (checkBox == ChkModuloEMAIL) nombreModulo = "EMAIL";
                else if (checkBox == ChkModuloMONITOR) nombreModulo = "MONITOR";
                else if (checkBox == ChkModuloMERCADOPAGO) nombreModulo = "MERCADOPAGO";

                if (!string.IsNullOrEmpty(nombreModulo))
                {
                    try
                    {
                        _dbService.ActualizarModulo(nombreModulo, true);
                        ActualizarPestanasModulos();
                        
                        // Si es Pantalla Cliente, iniciar servidor
                        if (nombreModulo == "MONITOR")
                        {
                            IniciarServidorPantallaCliente();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al guardar módulo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        checkBox.IsChecked = false;
                    }
                }
            }
        }

        private void ChkModulo_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                string nombreModulo = "";
                if (checkBox == ChkModuloANPR) nombreModulo = "ANPR";
                else if (checkBox == ChkModuloTERMINAL) nombreModulo = "TERMINAL";
                else if (checkBox == ChkModuloEMAIL) nombreModulo = "EMAIL";
                else if (checkBox == ChkModuloMONITOR) nombreModulo = "MONITOR";
                else if (checkBox == ChkModuloMERCADOPAGO) nombreModulo = "MERCADOPAGO";

                if (!string.IsNullOrEmpty(nombreModulo))
                {
                    try
                    {
                        _dbService.ActualizarModulo(nombreModulo, false);
                        ActualizarPestanasModulos();
                        
                        // Si es Pantalla Cliente, detener servidor
                        if (nombreModulo == "MONITOR")
                        {
                            DetenerServidorPantallaCliente();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al guardar módulo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        checkBox.IsChecked = true;
                    }
                }
            }
        }

        private void ActualizarPestanasModulos()
        {
            try
            {
                var modulos = _dbService.ObtenerModulos();
                
                // Obtener el StackPanel de la barra de navegación de configuración
                if (PanelSubmenuConfiguracion == null) return;
                var stackPanel = PanelSubmenuConfiguracion.Child as StackPanel;
                if (stackPanel == null) return;

                // Eliminar botones de módulos existentes
                var botonesAEliminar = stackPanel.Children.OfType<Button>()
                    .Where(b => _botonesModulos.ContainsValue(b))
                    .ToList();
                foreach (var btn in botonesAEliminar)
                {
                    stackPanel.Children.Remove(btn);
                }
                _botonesModulos.Clear();

                // Encontrar el botón "Ajustes" para insertar antes de él
                Button? btnAjustes = this.FindName("BtnConfigAjustes") as Button;
                int indiceInsercion = btnAjustes != null && stackPanel.Children.Contains(btnAjustes) 
                    ? stackPanel.Children.IndexOf(btnAjustes) 
                    : stackPanel.Children.Count;

                // Crear botones para módulos activos
                string[] nombresModulos = { "ANPR", "TERMINAL", "EMAIL", "MONITOR", "MERCADOPAGO" };
                string[] textosModulos = { "ANPR", "Terminal", "Email", "Pantalla Cliente", "MercadoPago" };

                for (int i = 0; i < nombresModulos.Length; i++)
                {
                    if (modulos.ContainsKey(nombresModulos[i]) && modulos[nombresModulos[i]])
                    {
                        // Capturar el valor de i en una variable local para evitar problemas de captura en el closure
                        string nombreModulo = nombresModulos[i];
                        string textoModulo = textosModulos[i];
                        
                        var btnModulo = new Button
                        {
                            Name = $"BtnConfig{nombreModulo}",
                            Content = textoModulo,
                            Height = 42,
                            Width = 120,
                            FontSize = 13,
                            FontWeight = FontWeights.SemiBold,
                            Background = System.Windows.Media.Brushes.Transparent,
                            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85)),
                            BorderThickness = new Thickness(0, 0, 0, 0),
                            Margin = new Thickness(0, 0, 10, 0),
                            Cursor = Cursors.Hand
                        };
                        btnModulo.Click += (s, e) => BtnConfigModulo_Click(s, e, nombreModulo);
                        
                        stackPanel.Children.Insert(indiceInsercion, btnModulo);
                        _botonesModulos[nombreModulo] = btnModulo;
                        indiceInsercion++;
                    }
                }

                // Actualizar visibilidad de paneles de módulos
                ActualizarVisibilidadPanelesModulos(modulos);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar pestañas de módulos: {ex.Message}");
            }
        }

        private void ActualizarVisibilidadPanelesModulos(Dictionary<string, bool> modulos)
        {
            // Crear o actualizar paneles de módulos si no existen
            foreach (var kvp in modulos)
            {
                if (kvp.Value)
                {
                    string nombreModulo = kvp.Key;
                    if (!_panelesModulos.ContainsKey(nombreModulo))
                    {
                        Grid panel;
                        if (nombreModulo == "MONITOR")
                        {
                            panel = CrearPanelMonitor();
                        }
                        else if (nombreModulo == "ANPR")
                        {
                            panel = CrearPanelANPR();
                        }
                        else if (nombreModulo == "MERCADOPAGO")
                        {
                            panel = CrearPanelMercadoPago();
                        }
                        else
                        {
                            panel = new Grid
                            {
                                Name = $"PanelConfig{nombreModulo}",
                                Visibility = Visibility.Collapsed
                            };
                            var textBlock = new TextBlock
                            {
                                Text = "En desarrollo",
                                FontSize = 18,
                                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)),
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            panel.Children.Add(textBlock);
                        }
                        
                        panel.Name = $"PanelConfig{nombreModulo}";
                        panel.Visibility = Visibility.Collapsed;
                        
                        if (PanelConfiguracion != null)
                        {
                            PanelConfiguracion.Children.Add(panel);
                        }
                        _panelesModulos[nombreModulo] = panel;
                    }
                }
            }
        }

        private Grid CrearPanelMonitor()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Título
            var titulo = new TextBlock
            {
                Text = "Pantalla Cliente",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(20, 20, 20, 10)
            };
            Grid.SetRow(titulo, 0);
            grid.Children.Add(titulo);

            var subtitulo = new TextBlock
            {
                Text = "Configure los parámetros para la pantalla exclusiva para el cliente",
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(20, 0, 20, 20)
            };
            Grid.SetRow(subtitulo, 0);
            grid.Children.Add(subtitulo);

            // ScrollViewer con contenido
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var stackPanel = new StackPanel { Margin = new Thickness(20, 0, 20, 20) };

            // Panel izquierdo: Configuración
            var gridPrincipal = new Grid();
            gridPrincipal.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridPrincipal.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridPrincipal.ColumnDefinitions[0].MinWidth = 400;
            gridPrincipal.ColumnDefinitions[1].MinWidth = 400;

            // Columna izquierda: Configuración
            var stackPanelIzq = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };

            // Pantalla de Bienvenida
            var borderBienvenida = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };
            var stackBienvenida = new StackPanel();
            var txtBienvenidaTitulo = new TextBlock
            {
                Text = "Pantalla de Bienvenida",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackBienvenida.Children.Add(txtBienvenidaTitulo);

            var lblBienvenida1 = new TextBlock { Text = "Línea 1:", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 5) };
            var txtBienvenida1 = new TextBox
            {
                Name = "TxtPantallaBienvenida1",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            txtBienvenida1.TextChanged += TxtPantallaCliente_TextChanged;
            stackBienvenida.Children.Add(lblBienvenida1);
            stackBienvenida.Children.Add(txtBienvenida1);

            var lblBienvenida2 = new TextBlock { Text = "Línea 2:", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 5) };
            var txtBienvenida2 = new TextBox
            {
                Name = "TxtPantallaBienvenida2",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(0, 0, 0, 0)
            };
            txtBienvenida2.TextChanged += TxtPantallaCliente_TextChanged;
            stackBienvenida.Children.Add(lblBienvenida2);
            stackBienvenida.Children.Add(txtBienvenida2);
            borderBienvenida.Child = stackBienvenida;
            stackPanelIzq.Children.Add(borderBienvenida);

            // Pantalla de Agradecimiento
            var borderAgradecimiento = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };
            var stackAgradecimiento = new StackPanel();
            var txtAgradecimientoTitulo = new TextBlock
            {
                Text = "Pantalla de Agradecimiento",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackAgradecimiento.Children.Add(txtAgradecimientoTitulo);

            var lblAgradecimiento1 = new TextBlock { Text = "Línea 1:", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 5) };
            var txtAgradecimiento1 = new TextBox
            {
                Name = "TxtPantallaAgradecimiento1",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(0, 0, 0, 10)
            };
            txtAgradecimiento1.TextChanged += TxtPantallaCliente_TextChanged;
            stackAgradecimiento.Children.Add(lblAgradecimiento1);
            stackAgradecimiento.Children.Add(txtAgradecimiento1);

            var lblAgradecimiento2 = new TextBlock { Text = "Línea 2:", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 5) };
            var txtAgradecimiento2 = new TextBox
            {
                Name = "TxtPantallaAgradecimiento2",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(0, 0, 0, 0)
            };
            txtAgradecimiento2.TextChanged += TxtPantallaCliente_TextChanged;
            stackAgradecimiento.Children.Add(lblAgradecimiento2);
            stackAgradecimiento.Children.Add(txtAgradecimiento2);
            borderAgradecimiento.Child = stackAgradecimiento;
            stackPanelIzq.Children.Add(borderAgradecimiento);

            // Pantalla de Cobro
            var borderCobro = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 0)
            };
            var stackCobro = new StackPanel();
            var txtCobroTitulo = new TextBlock
            {
                Text = "Pantalla de Cobro",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackCobro.Children.Add(txtCobroTitulo);

            var lblCobro = new TextBlock { Text = "Aclaración (opcional):", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), Margin = new Thickness(0, 0, 0, 5) };
            var txtCobro = new TextBox
            {
                Name = "TxtPantallaCobro",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(0, 0, 0, 0)
            };
            txtCobro.TextChanged += TxtPantallaCliente_TextChanged;
            stackCobro.Children.Add(lblCobro);
            stackCobro.Children.Add(txtCobro);
            borderCobro.Child = stackCobro;
            stackPanelIzq.Children.Add(borderCobro);

            Grid.SetColumn(stackPanelIzq, 0);
            gridPrincipal.Children.Add(stackPanelIzq);

            // Columna derecha: Instrucciones
            var stackPanelDer = new StackPanel { Margin = new Thickness(10, 0, 0, 0) };
            var borderInstrucciones = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20)
            };
            var stackInstrucciones = new StackPanel();

            var txtInstruccionesTitulo = new TextBlock
            {
                Text = "Instrucciones de uso",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackInstrucciones.Children.Add(txtInstruccionesTitulo);

            var icono = new TextBlock
            {
                Text = "Pantalla Cliente",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackInstrucciones.Children.Add(icono);

            var txtInstruccion1 = new TextBlock
            {
                Text = "Utilice cualquier dispositivo de su preferencia, Smart Tv, Smartphone o preferentemente Tablet y conéctelo por red o wifi a la misma red en la que se encuentra esta computadora.",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackInstrucciones.Children.Add(txtInstruccion1);

            var txtInstruccion2 = new TextBlock
            {
                Text = "Escanee con la cámara el siguiente código QR o Acceda con su navegador web a la dirección web.",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackInstrucciones.Children.Add(txtInstruccion2);

            // QR Code (placeholder - se generará dinámicamente)
            var imgQR = new System.Windows.Controls.Image
            {
                Name = "ImgPantallaQR",
                Width = 200,
                Height = 200,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            stackInstrucciones.Children.Add(imgQR);

            // URL y botón copiar
            var stackURL = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 15) };
            var txtURL = new TextBlock
            {
                Name = "TxtPantallaURL",
                Text = "http://192.168.10.169:3555",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var btnCopiar = new Button
            {
                Name = "BtnPantallaCopiar",
                Content = "Copiar en Portapapeles",
                Padding = new Thickness(12, 6, 12, 6),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Cursor = Cursors.Hand,
                FontSize = 12
            };
            btnCopiar.Click += BtnPantallaCopiar_Click;
            stackURL.Children.Add(txtURL);
            stackURL.Children.Add(btnCopiar);
            stackInstrucciones.Children.Add(stackURL);

            var txtInstruccion3 = new TextBlock
            {
                Text = "En este punto ya podrá ver la pantalla. El sistema está preparado para que pueda convertir el sitio en una aplicación para su teléfono o tablet. Revise la configuración del navegador para encontrar esta opción como \"Agregar a Inicio\" o \"Instalar\" (Altamente recomendable)",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackInstrucciones.Children.Add(txtInstruccion3);

            // Instrucciones sobre segundo monitor (se mostrará si hay múltiples monitores)
            var txtInstruccionMonitor = new TextBlock
            {
                Name = "TxtInstruccionMonitor",
                Text = "",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed
            };
            stackInstrucciones.Children.Add(txtInstruccionMonitor);

            // Checkbox para mostrar automáticamente en segundo monitor
            var chkMostrarEnSegundoMonitor = new CheckBox
            {
                Name = "ChkMostrarEnSegundoMonitor",
                Content = "Mostrar automáticamente en el segundo monitor",
                FontSize = 13,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(0, 0, 0, 0),
                Visibility = Visibility.Collapsed
            };
            chkMostrarEnSegundoMonitor.Checked += ChkMostrarEnSegundoMonitor_Changed;
            chkMostrarEnSegundoMonitor.Unchecked += ChkMostrarEnSegundoMonitor_Changed;
            stackInstrucciones.Children.Add(chkMostrarEnSegundoMonitor);

            borderInstrucciones.Child = stackInstrucciones;
            stackPanelDer.Children.Add(borderInstrucciones);

            Grid.SetColumn(stackPanelDer, 1);
            gridPrincipal.Children.Add(stackPanelDer);

            stackPanel.Children.Add(gridPrincipal);
            scrollViewer.Content = stackPanel;
            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

            return grid;
        }

        private Grid CrearPanelMercadoPago()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Título
            var titulo = new TextBlock
            {
                Text = "QR MercadoPago",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(20, 20, 20, 10)
            };
            Grid.SetRow(titulo, 0);
            grid.Children.Add(titulo);

            var subtitulo = new TextBlock
            {
                Text = "Configure las credenciales de Mercado Pago para generar códigos QR de pago",
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(20, 0, 20, 20)
            };
            Grid.SetRow(subtitulo, 0);
            grid.Children.Add(subtitulo);

            // ScrollViewer con contenido
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var stackPanel = new StackPanel { Margin = new Thickness(20, 0, 20, 20) };

            // Panel de credenciales
            var borderCredenciales = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };
            var stackCredenciales = new StackPanel();
            
            var txtCredencialesTitulo = new TextBlock
            {
                Text = "Credenciales de Mercado Pago",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackCredenciales.Children.Add(txtCredencialesTitulo);

            var txtInfo = new TextBlock
            {
                Text = "Ingrese las credenciales de su aplicación de Mercado Pago. Puede obtenerlas desde el panel de desarrolladores de Mercado Pago.",
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackCredenciales.Children.Add(txtInfo);

            var lblAccessToken = new TextBlock 
            { 
                Text = "Access Token:", 
                FontSize = 12, 
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), 
                Margin = new Thickness(0, 0, 0, 5) 
            };
            var txtAccessToken = new PasswordBox
            {
                Name = "TxtMercadoPagoAccessToken",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackCredenciales.Children.Add(lblAccessToken);
            stackCredenciales.Children.Add(txtAccessToken);

            var lblPublicKey = new TextBlock 
            { 
                Text = "Public Key:", 
                FontSize = 12, 
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)), 
                Margin = new Thickness(0, 0, 0, 5) 
            };
            var txtPublicKey = new PasswordBox
            {
                Name = "TxtMercadoPagoPublicKey",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackCredenciales.Children.Add(lblPublicKey);
            stackCredenciales.Children.Add(txtPublicKey);

            var btnGuardar = new Button
            {
                Content = "Guardar Credenciales",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };
            btnGuardar.Click += BtnMercadoPagoGuardar_Click;
            stackCredenciales.Children.Add(btnGuardar);

            var btnProbar = new Button
            {
                Content = "Probar Credenciales",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                Cursor = Cursors.Hand
            };
            btnProbar.Click += BtnMercadoPagoProbar_Click;
            stackCredenciales.Children.Add(btnProbar);

            borderCredenciales.Child = stackCredenciales;
            stackPanel.Children.Add(borderCredenciales);

            scrollViewer.Content = stackPanel;
            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

            return grid;
        }

        private Grid CrearPanelANPR()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Título
            var titulo = new TextBlock
            {
                Text = "Cámaras ANPR",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(20, 20, 20, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(titulo, 0);
            grid.Children.Add(titulo);

            var subtitulo = new TextBlock
            {
                Text = "Configura tus cámaras con o sin barreras vehiculares.",
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                Margin = new Thickness(20, 0, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(subtitulo, 0);
            grid.Children.Add(subtitulo);

            // Pestañas
            var stackPanelTabs = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 0, 20, 0)
            };

            var btnTabCamaras = new Button
            {
                Name = "BtnTabANPRCamaras",
                Content = "Cámaras",
                Height = 40,
                MinWidth = 120,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Tag = "Camaras"
            };
            btnTabCamaras.Click += (s, e) => MostrarTabANPR("Camaras");
            stackPanelTabs.Children.Add(btnTabCamaras);

            var btnTabCategorias = new Button
            {
                Name = "BtnTabANPRCategorias",
                Content = "Categorías",
                Height = 40,
                MinWidth = 120,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Tag = "Categorias"
            };
            btnTabCategorias.Click += (s, e) => MostrarTabANPR("Categorias");
            stackPanelTabs.Children.Add(btnTabCategorias);

            var btnTabListaBlanca = new Button
            {
                Name = "BtnTabANPRListaBlanca",
                Content = "Lista Blanca",
                Height = 40,
                MinWidth = 120,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Tag = "ListaBlanca"
            };
            btnTabListaBlanca.Click += (s, e) => MostrarTabANPR("ListaBlanca");
            stackPanelTabs.Children.Add(btnTabListaBlanca);

            var btnTabListaNegra = new Button
            {
                Name = "BtnTabANPRListaNegra",
                Content = "Lista Negra",
                Height = 40,
                MinWidth = 120,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
                BorderThickness = new Thickness(0, 0, 0, 0),
                Margin = new Thickness(0, 0, 5, 0),
                Cursor = Cursors.Hand,
                Tag = "ListaNegra"
            };
            btnTabListaNegra.Click += (s, e) => MostrarTabANPR("ListaNegra");
            stackPanelTabs.Children.Add(btnTabListaNegra);

            Grid.SetRow(stackPanelTabs, 1);
            grid.Children.Add(stackPanelTabs);

            // Panel de contenido de pestañas
            var panelContenido = new Grid
            {
                Name = "PanelContenidoANPR",
                Margin = new Thickness(20, 20, 20, 20)
            };

            // Pestaña Cámaras
            var panelCamaras = CrearPanelANPRCamaras();
            panelCamaras.Name = "PanelANPRCamaras";
            panelCamaras.Visibility = Visibility.Visible;
            panelContenido.Children.Add(panelCamaras);

            // Pestaña Categorías
            var panelCategorias = CrearPanelANPRCategorias();
            panelCategorias.Name = "PanelANPRCategorias";
            panelCategorias.Visibility = Visibility.Collapsed;
            panelContenido.Children.Add(panelCategorias);

            // Pestaña Lista Blanca (placeholder)
            // Pestaña Lista Blanca
            var panelListaBlanca = CrearPanelListaBlanca();
            panelListaBlanca.Name = "PanelANPRListaBlanca";
            panelListaBlanca.Visibility = Visibility.Collapsed;
            panelContenido.Children.Add(panelListaBlanca);

            // Pestaña Lista Negra
            var panelListaNegra = CrearPanelListaNegra();
            panelListaNegra.Name = "PanelANPRListaNegra";
            panelListaNegra.Visibility = Visibility.Collapsed;
            panelContenido.Children.Add(panelListaNegra);

            Grid.SetRow(panelContenido, 2);
            grid.Children.Add(panelContenido);

            // Cargar cámaras al crear el panel
            CargarCamarasANPR();

            return grid;
        }

        private Grid CrearPanelANPRCategorias()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header con título y descripción
            var borderHeader = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackHeader = new StackPanel();
            
            var txtTitulo = new TextBlock
            {
                Text = "Categorías",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            var txtSubtitulo = new TextBlock
            {
                Text = "Asigne sus categorías a las categorías definidas por las cámaras Dahua",
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap
            };

            stackHeader.Children.Add(txtTitulo);
            stackHeader.Children.Add(txtSubtitulo);
            borderHeader.Child = stackHeader;

            Grid.SetRow(borderHeader, 0);
            grid.Children.Add(borderHeader);

            // ScrollViewer para la tabla
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var borderTabla = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0)
            };

            var gridTabla = new Grid();
            gridTabla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180, GridUnitType.Pixel) }); // Categoría Dahua
            gridTabla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Descripción
            gridTabla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250, GridUnitType.Pixel) }); // Categoría Real

            // Cargar categorías Dahua primero para saber cuántas filas necesitamos
            var categoriasDahua = _dbService.ObtenerCategoriasDahua();
            var categoriasInternas = _dbService.ObtenerCategorias();

            // Agregar fila para headers
            gridTabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            // Agregar filas para cada categoría Dahua
            for (int i = 0; i < categoriasDahua.Count; i++)
            {
                gridTabla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // Headers de la tabla
            var header1 = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(15, 12, 15, 12)
            };
            var txtHeader1 = new TextBlock
            {
                Text = "Categoría Dahua",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55))
            };
            header1.Child = txtHeader1;
            Grid.SetColumn(header1, 0);
            Grid.SetRow(header1, 0);
            gridTabla.Children.Add(header1);

            var header2 = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(15, 12, 15, 12)
            };
            var txtHeader2 = new TextBlock
            {
                Text = "Descripción",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55))
            };
            header2.Child = txtHeader2;
            Grid.SetColumn(header2, 1);
            Grid.SetRow(header2, 0);
            gridTabla.Children.Add(header2);

            var header3 = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(15, 12, 15, 12)
            };
            var txtHeader3 = new TextBlock
            {
                Text = "Categoría Real",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55))
            };
            header3.Child = txtHeader3;
            Grid.SetColumn(header3, 2);
            Grid.SetRow(header3, 0);
            gridTabla.Children.Add(header3);

            // Crear filas para cada categoría Dahua
            for (int i = 0; i < categoriasDahua.Count; i++)
            {
                var categoriaDahua = categoriasDahua[i];
                int rowIndex = i + 1;

                // Columna 1: Categoría Dahua
                var cell1 = new Border
                {
                    Background = i % 2 == 0 ? System.Windows.Media.Brushes.White : new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(15, 12, 15, 12)
                };
                var txtCodigo = new TextBlock
                {
                    Text = categoriaDahua.Codigo,
                    FontSize = 13,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55))
                };
                cell1.Child = txtCodigo;
                Grid.SetColumn(cell1, 0);
                Grid.SetRow(cell1, rowIndex);
                gridTabla.Children.Add(cell1);

                // Columna 2: Descripción
                var cell2 = new Border
                {
                    Background = i % 2 == 0 ? System.Windows.Media.Brushes.White : new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(15, 12, 15, 12)
                };
                var txtDescripcion = new TextBlock
                {
                    Text = categoriaDahua.Descripcion,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99))
                };
                cell2.Child = txtDescripcion;
                Grid.SetColumn(cell2, 1);
                Grid.SetRow(cell2, rowIndex);
                gridTabla.Children.Add(cell2);

                // Columna 3: Categoría Real (ComboBox)
                var cell3 = new Border
                {
                    Background = i % 2 == 0 ? System.Windows.Media.Brushes.White : new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Padding = new Thickness(15, 8, 15, 8)
                };
                var cmbCategoria = new ComboBox
                {
                    Name = $"CmbCategoriaDahua_{categoriaDahua.Id}",
                    Height = 32,
                    FontSize = 13,
                    MinWidth = 200,
                    Tag = categoriaDahua.Id
                };

                // Agregar opción "-- Seleccionar --"
                cmbCategoria.Items.Add(new ComboBoxItem { Content = "-- Seleccionar --", Tag = null });

                // Agregar categorías internas
                foreach (var cat in categoriasInternas)
                {
                    var item = new ComboBoxItem
                    {
                        Content = cat.Nombre,
                        Tag = cat.Id
                    };
                    cmbCategoria.Items.Add(item);

                    // Seleccionar la categoría si está mapeada
                    if (categoriaDahua.CategoriaId == cat.Id)
                    {
                        cmbCategoria.SelectedItem = item;
                    }
                }

                // Si no hay categoría seleccionada, seleccionar "-- Seleccionar --"
                if (cmbCategoria.SelectedItem == null && cmbCategoria.Items.Count > 0)
                {
                    cmbCategoria.SelectedIndex = 0;
                }

                // Evento para guardar automáticamente cuando cambia
                cmbCategoria.SelectionChanged += (s, e) =>
                {
                    if (s is ComboBox cmb && cmb.Tag is int categoriaDahuaId)
                    {
                        var selectedItem = cmb.SelectedItem as ComboBoxItem;
                        int? categoriaId = selectedItem?.Tag as int?;
                        _dbService.ActualizarMapeoCategoriaDahua(categoriaDahuaId, categoriaId);
                    }
                };

                cell3.Child = cmbCategoria;
                Grid.SetColumn(cell3, 2);
                Grid.SetRow(cell3, rowIndex);
                gridTabla.Children.Add(cell3);
            }

            borderTabla.Child = gridTabla;
            scrollViewer.Content = borderTabla;

            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

            return grid;
        }

        private Grid CrearPanelANPRCamaras()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header con botón Agregar Cámara
            var borderHeader = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var gridHeader = new Grid();
            gridHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var stackPanelIzq = new StackPanel { Orientation = Orientation.Horizontal };
            // Header sin emoji ni texto específico - solo espacio para el botón
            Grid.SetColumn(stackPanelIzq, 0);
            gridHeader.Children.Add(stackPanelIzq);

            var btnAgregarCamara = new Button
            {
                Name = "BtnAgregarCamaraANPR",
                Content = "Agregar Cámara",
                Height = 40,
                MinWidth = 150,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Cursor = Cursors.Hand,
                Padding = new Thickness(15, 0, 15, 0)
            };
            btnAgregarCamara.Click += BtnAgregarCamaraANPR_Click;
            Grid.SetColumn(btnAgregarCamara, 1);
            gridHeader.Children.Add(btnAgregarCamara);

            borderHeader.Child = gridHeader;
            Grid.SetRow(borderHeader, 0);
            grid.Children.Add(borderHeader);

            // Lista de cámaras
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var itemsControl = new ItemsControl
            {
                Name = "ItemsControlCamarasANPR",
                ItemsSource = _camarasANPR
            };
            itemsControl.ItemTemplate = CrearDataTemplateCamaraANPR();

            scrollViewer.Content = itemsControl;
            Grid.SetRow(scrollViewer, 1);
            grid.Children.Add(scrollViewer);

            return grid;
        }

        private Grid CrearPanelListaBlanca()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header
            var borderHeader = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackHeader = new StackPanel();
            var txtTitulo = new TextBlock
            {
                Text = "Lista Blanca",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var txtSubtitulo = new TextBlock
            {
                Text = "Las placas en esta lista tendrán acceso gratuito (dueños). Se permite el paso automático sin generar ticket.",
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap
            };
            stackHeader.Children.Add(txtTitulo);
            stackHeader.Children.Add(txtSubtitulo);
            borderHeader.Child = stackHeader;
            Grid.SetRow(borderHeader, 0);
            grid.Children.Add(borderHeader);

            // Formulario para agregar placa
            var borderForm = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var gridForm = new Grid();
            gridForm.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridForm.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridForm.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            gridForm.ColumnDefinitions[0].MinWidth = 200;
            gridForm.ColumnDefinitions[1].MinWidth = 200;

            var stackForm = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 10, 0) };
            var lblMatricula = new TextBlock
            {
                Text = "Matrícula:",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var txtMatricula = new TextBox
            {
                Name = "TxtListaBlancaMatricula",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                MinWidth = 150
            };
            stackForm.Children.Add(lblMatricula);
            stackForm.Children.Add(txtMatricula);
            Grid.SetColumn(stackForm, 0);
            gridForm.Children.Add(stackForm);

            var stackDesc = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 10, 0) };
            var lblDescripcion = new TextBlock
            {
                Text = "Descripción (opcional):",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var txtDescripcion = new TextBox
            {
                Name = "TxtListaBlancaDescripcion",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                MinWidth = 200
            };
            stackDesc.Children.Add(lblDescripcion);
            stackDesc.Children.Add(txtDescripcion);
            Grid.SetColumn(stackDesc, 1);
            gridForm.Children.Add(stackDesc);

            var btnAgregar = new Button
            {
                Content = "Agregar",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                Cursor = Cursors.Hand,
                Margin = new Thickness(10, 0, 0, 0)
            };
            btnAgregar.Click += (s, e) => BtnAgregarListaBlanca_Click(txtMatricula, txtDescripcion);
            Grid.SetColumn(btnAgregar, 2);
            gridForm.Children.Add(btnAgregar);

            borderForm.Child = gridForm;
            Grid.SetRow(borderForm, 1);
            grid.Children.Add(borderForm);

            // Lista de placas
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var borderLista = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0)
            };

            var itemsControl = new ItemsControl
            {
                Name = "ItemsControlListaBlanca"
            };
            CargarListaBlanca(itemsControl);
            borderLista.Child = itemsControl;
            scrollViewer.Content = borderLista;
            Grid.SetRow(scrollViewer, 2);
            grid.Children.Add(scrollViewer);

            return grid;
        }

        private Grid CrearPanelListaNegra()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // Header
            var borderHeader = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var stackHeader = new StackPanel();
            var txtTitulo = new TextBlock
            {
                Text = "Lista Negra",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            var txtSubtitulo = new TextBlock
            {
                Text = "Las placas en esta lista están prohibidas. Se denegará el acceso de cualquier forma.",
                FontSize = 14,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap
            };
            stackHeader.Children.Add(txtTitulo);
            stackHeader.Children.Add(txtSubtitulo);
            borderHeader.Child = stackHeader;
            Grid.SetRow(borderHeader, 0);
            grid.Children.Add(borderHeader);

            // Formulario para agregar placa
            var borderForm = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20, 20, 20, 20),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var gridForm = new Grid();
            gridForm.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridForm.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridForm.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            gridForm.ColumnDefinitions[0].MinWidth = 200;
            gridForm.ColumnDefinitions[1].MinWidth = 200;

            var stackForm = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 10, 0) };
            var lblMatricula = new TextBlock
            {
                Text = "Matrícula:",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var txtMatricula = new TextBox
            {
                Name = "TxtListaNegraMatricula",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                MinWidth = 150
            };
            stackForm.Children.Add(lblMatricula);
            stackForm.Children.Add(txtMatricula);
            Grid.SetColumn(stackForm, 0);
            gridForm.Children.Add(stackForm);

            var stackDesc = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 10, 0) };
            var lblDescripcion = new TextBlock
            {
                Text = "Descripción (opcional):",
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            var txtDescripcion = new TextBox
            {
                Name = "TxtListaNegraDescripcion",
                FontSize = 14,
                Padding = new Thickness(8, 8, 8, 8),
                MinWidth = 200
            };
            stackDesc.Children.Add(lblDescripcion);
            stackDesc.Children.Add(txtDescripcion);
            Grid.SetColumn(stackDesc, 1);
            gridForm.Children.Add(stackDesc);

            var btnAgregar = new Button
            {
                Content = "Agregar",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                Foreground = System.Windows.Media.Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                Cursor = Cursors.Hand,
                Margin = new Thickness(10, 0, 0, 0)
            };
            btnAgregar.Click += (s, e) => BtnAgregarListaNegra_Click(txtMatricula, txtDescripcion);
            Grid.SetColumn(btnAgregar, 2);
            gridForm.Children.Add(btnAgregar);

            borderForm.Child = gridForm;
            Grid.SetRow(borderForm, 1);
            grid.Children.Add(borderForm);

            // Lista de placas
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var borderLista = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(0)
            };

            var itemsControl = new ItemsControl
            {
                Name = "ItemsControlListaNegra"
            };
            CargarListaNegra(itemsControl);
            borderLista.Child = itemsControl;
            scrollViewer.Content = borderLista;
            Grid.SetRow(scrollViewer, 2);
            grid.Children.Add(scrollViewer);

            return grid;
        }

        private DataTemplate CrearDataTemplateCamaraANPR()
        {
            var dataTemplate = new DataTemplate();

            // Border principal del item
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, System.Windows.Media.Brushes.White);
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1, 1, 1, 1));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetValue(Border.PaddingProperty, new Thickness(15, 15, 15, 15));
            border.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 15));

            // Grid principal con 3 columnas: icono, contenido, info
            var grid = new FrameworkElementFactory(typeof(Grid));
            var colDef1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef1.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto); // Icono
            var colDef2 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef2.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star)); // Contenido
            var colDef3 = new FrameworkElementFactory(typeof(ColumnDefinition));
            colDef3.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto); // Info
            grid.AppendChild(colDef1);
            grid.AppendChild(colDef2);
            grid.AppendChild(colDef3);

            // Columna izquierda: Icono de cámara en fondo azul
            var borderIcono = new FrameworkElementFactory(typeof(Border));
            borderIcono.SetValue(Border.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)));
            borderIcono.SetValue(Border.WidthProperty, 60.0);
            borderIcono.SetValue(Border.HeightProperty, 60.0);
            borderIcono.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            borderIcono.SetValue(Border.MarginProperty, new Thickness(0, 0, 15, 0));

            var iconoCamara = new FrameworkElementFactory(typeof(TextBlock));
            iconoCamara.SetValue(TextBlock.TextProperty, "📹");
            iconoCamara.SetValue(TextBlock.FontSizeProperty, 32.0);
            iconoCamara.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            iconoCamara.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderIcono.AppendChild(iconoCamara);
            borderIcono.SetValue(Grid.ColumnProperty, 0);
            grid.AppendChild(borderIcono);

            // Columna central: Contenido editable
            var stackPanelCentral = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelCentral.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            // Fila superior: Nombre editable y checkbox Cámara Activa
            var gridFilaSuperior = new FrameworkElementFactory(typeof(Grid));
            var colNombre = new FrameworkElementFactory(typeof(ColumnDefinition));
            colNombre.SetValue(ColumnDefinition.WidthProperty, new GridLength(1, GridUnitType.Star));
            var colCheckbox = new FrameworkElementFactory(typeof(ColumnDefinition));
            colCheckbox.SetValue(ColumnDefinition.WidthProperty, GridLength.Auto);
            gridFilaSuperior.AppendChild(colNombre);
            gridFilaSuperior.AppendChild(colCheckbox);

            // TextBox para nombre editable
            var txtNombre = new FrameworkElementFactory(typeof(TextBox));
            txtNombre.SetBinding(TextBox.TextProperty, new System.Windows.Data.Binding("Nombre"));
            txtNombre.SetValue(TextBox.FontSizeProperty, 14.0);
            txtNombre.SetValue(TextBox.FontWeightProperty, FontWeights.SemiBold);
            txtNombre.SetValue(TextBox.BorderThicknessProperty, new Thickness(1, 1, 1, 1));
            txtNombre.SetValue(TextBox.BorderBrushProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)));
            txtNombre.SetValue(TextBox.PaddingProperty, new Thickness(8, 6, 8, 6));
            txtNombre.SetValue(TextBox.MarginProperty, new Thickness(0, 0, 10, 0));
            txtNombre.AddHandler(TextBox.LostFocusEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is TextBox txt && txt.DataContext is CamaraANPR camara)
                {
                    camara.Nombre = txt.Text;
                    _dbService.ActualizarCamaraANPR(camara);
                }
            }));
            txtNombre.SetValue(Grid.ColumnProperty, 0);
            gridFilaSuperior.AppendChild(txtNombre);

            // StackPanel para checkbox "Cámara Activa"
            var stackCheckbox = new FrameworkElementFactory(typeof(StackPanel));
            stackCheckbox.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackCheckbox.SetValue(StackPanel.VerticalAlignmentProperty, VerticalAlignment.Center);

            var txtActiva = new FrameworkElementFactory(typeof(TextBlock));
            txtActiva.SetValue(TextBlock.TextProperty, "Cámara Activa");
            txtActiva.SetValue(TextBlock.FontSizeProperty, 14.0);
            txtActiva.SetValue(TextBlock.MarginProperty, new Thickness(0, 0, 5, 0));
            txtActiva.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

            var chkActiva = new FrameworkElementFactory(typeof(CheckBox));
            chkActiva.SetBinding(CheckBox.IsCheckedProperty, new System.Windows.Data.Binding("Activa"));
            chkActiva.SetValue(CheckBox.WidthProperty, 20.0);
            chkActiva.SetValue(CheckBox.HeightProperty, 20.0);
            chkActiva.AddHandler(CheckBox.CheckedEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is CheckBox chk && chk.DataContext is CamaraANPR camara)
                {
                    camara.Activa = chk.IsChecked == true;
                    _dbService.ActualizarCamaraANPR(camara);
                }
            }));
            chkActiva.AddHandler(CheckBox.UncheckedEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is CheckBox chk && chk.DataContext is CamaraANPR camara)
                {
                    camara.Activa = false;
                    _dbService.ActualizarCamaraANPR(camara);
                }
            }));

            stackCheckbox.AppendChild(txtActiva);
            stackCheckbox.AppendChild(chkActiva);
            stackCheckbox.SetValue(Grid.ColumnProperty, 1);
            gridFilaSuperior.AppendChild(stackCheckbox);

            stackPanelCentral.AppendChild(gridFilaSuperior);

            // Campo ENDPOINT
            var lblEndpoint = new FrameworkElementFactory(typeof(TextBlock));
            lblEndpoint.SetValue(TextBlock.TextProperty, "Defina el ENDPOINT de ITSAPI:");
            lblEndpoint.SetValue(TextBlock.FontSizeProperty, 12.0);
            lblEndpoint.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)));
            lblEndpoint.SetValue(TextBlock.MarginProperty, new Thickness(0, 8, 0, 4));
            stackPanelCentral.AppendChild(lblEndpoint);

            // Campo ENDPOINT (solo lectura) - Envuelto en Border para tener borde
            var borderEndpoint = new FrameworkElementFactory(typeof(Border));
            borderEndpoint.SetValue(Border.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(249, 250, 251)));
            borderEndpoint.SetValue(Border.BorderThicknessProperty, new Thickness(1, 1, 1, 1));
            borderEndpoint.SetValue(Border.BorderBrushProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)));
            borderEndpoint.SetValue(Border.PaddingProperty, new Thickness(8, 6, 8, 6));
            borderEndpoint.SetValue(Border.MarginProperty, new Thickness(0, 0, 0, 8));

            var txtEndpoint = new FrameworkElementFactory(typeof(TextBlock));
            txtEndpoint.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("EndpointDisplay"));
            txtEndpoint.SetValue(TextBlock.FontSizeProperty, 13.0);
            txtEndpoint.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)));

            borderEndpoint.AppendChild(txtEndpoint);
            stackPanelCentral.AppendChild(borderEndpoint);

            // Botones Eliminar y Editar
            var stackPanelBotones = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelBotones.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackPanelBotones.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Left);

            var btnEliminar = new FrameworkElementFactory(typeof(Button));
            btnEliminar.SetValue(Button.ContentProperty, "Eliminar");
            btnEliminar.SetValue(Button.HeightProperty, 35.0);
            btnEliminar.SetValue(Button.MinWidthProperty, 100.0);
            btnEliminar.SetValue(Button.FontSizeProperty, 14.0);
            btnEliminar.SetValue(Button.BackgroundProperty, System.Windows.Media.Brushes.White);
            btnEliminar.SetValue(Button.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)));
            btnEliminar.SetValue(Button.BorderBrushProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)));
            btnEliminar.SetValue(Button.BorderThicknessProperty, new Thickness(1, 1, 1, 1));
            btnEliminar.SetValue(Button.MarginProperty, new Thickness(0, 0, 10, 0));
            btnEliminar.SetValue(Button.CursorProperty, Cursors.Hand);
            btnEliminar.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is Button btn && btn.DataContext is CamaraANPR camara)
                {
                    EliminarCamaraANPR(camara.Id);
                }
            }));

            var btnEditar = new FrameworkElementFactory(typeof(Button));
            btnEditar.SetValue(Button.ContentProperty, "Editar");
            btnEditar.SetValue(Button.HeightProperty, 35.0);
            btnEditar.SetValue(Button.MinWidthProperty, 100.0);
            btnEditar.SetValue(Button.FontSizeProperty, 14.0);
            btnEditar.SetValue(Button.BackgroundProperty, System.Windows.Media.Brushes.White);
            btnEditar.SetValue(Button.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)));
            btnEditar.SetValue(Button.BorderBrushProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)));
            btnEditar.SetValue(Button.BorderThicknessProperty, new Thickness(1, 1, 1, 1));
            btnEditar.SetValue(Button.CursorProperty, Cursors.Hand);
            btnEditar.AddHandler(Button.ClickEvent, new RoutedEventHandler((s, e) =>
            {
                if (s is Button btn && btn.DataContext is CamaraANPR camara)
                {
                    MostrarModalAgregarEditarCamara(camara);
                }
            }));

            stackPanelBotones.AppendChild(btnEliminar);
            stackPanelBotones.AppendChild(btnEditar);
            stackPanelCentral.AppendChild(stackPanelBotones);

            stackPanelCentral.SetValue(Grid.ColumnProperty, 1);
            grid.AppendChild(stackPanelCentral);

            // Columna derecha: Información estática
            var stackPanelInfo = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelInfo.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);
            stackPanelInfo.SetValue(StackPanel.MarginProperty, new Thickness(15, 0, 0, 0));

            // Host
            var txtHostLabel = new FrameworkElementFactory(typeof(TextBlock));
            txtHostLabel.SetValue(TextBlock.TextProperty, "Host: ");
            txtHostLabel.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtHostLabel.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)));
            var txtHost = new FrameworkElementFactory(typeof(TextBlock));
            txtHost.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("HostIP"));
            txtHost.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtHost.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)));
            var stackHost = new FrameworkElementFactory(typeof(StackPanel));
            stackHost.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackHost.AppendChild(txtHostLabel);
            stackHost.AppendChild(txtHost);
            stackPanelInfo.AppendChild(stackHost);

            // Barreras vehiculares
            var txtBarrerasLabel = new FrameworkElementFactory(typeof(TextBlock));
            txtBarrerasLabel.SetValue(TextBlock.TextProperty, "Barreras vehiculares: ");
            txtBarrerasLabel.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtBarrerasLabel.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)));
            var txtBarreras = new FrameworkElementFactory(typeof(TextBlock));
            txtBarreras.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("BarrerasVehicularesDisplay"));
            txtBarreras.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtBarreras.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)));
            var stackBarreras = new FrameworkElementFactory(typeof(StackPanel));
            stackBarreras.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackBarreras.AppendChild(txtBarrerasLabel);
            stackBarreras.AppendChild(txtBarreras);
            stackPanelInfo.AppendChild(stackBarreras);

            // Tipo
            var txtTipoLabel = new FrameworkElementFactory(typeof(TextBlock));
            txtTipoLabel.SetValue(TextBlock.TextProperty, "Tipo: ");
            txtTipoLabel.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtTipoLabel.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)));
            var txtTipo = new FrameworkElementFactory(typeof(TextBlock));
            txtTipo.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("Tipo"));
            txtTipo.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtTipo.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)));
            var stackTipo = new FrameworkElementFactory(typeof(StackPanel));
            stackTipo.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackTipo.AppendChild(txtTipoLabel);
            stackTipo.AppendChild(txtTipo);
            stackPanelInfo.AppendChild(stackTipo);

            // Pre-Ingreso
            var txtPreIngresoLabel = new FrameworkElementFactory(typeof(TextBlock));
            txtPreIngresoLabel.SetValue(TextBlock.TextProperty, "Pre-Ingreso: ");
            txtPreIngresoLabel.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtPreIngresoLabel.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)));
            var txtPreIngreso = new FrameworkElementFactory(typeof(TextBlock));
            txtPreIngreso.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("PreIngresoDisplay"));
            txtPreIngreso.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtPreIngreso.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)));
            var stackPreIngreso = new FrameworkElementFactory(typeof(StackPanel));
            stackPreIngreso.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackPreIngreso.AppendChild(txtPreIngresoLabel);
            stackPreIngreso.AppendChild(txtPreIngreso);
            stackPanelInfo.AppendChild(stackPreIngreso);

            // Impresora
            var txtImpresoraLabel = new FrameworkElementFactory(typeof(TextBlock));
            txtImpresoraLabel.SetValue(TextBlock.TextProperty, "");
            txtImpresoraLabel.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtImpresoraLabel.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)));
            var txtImpresora = new FrameworkElementFactory(typeof(TextBlock));
            txtImpresora.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("ImpresoraNombre"));
            txtImpresora.SetValue(TextBlock.FontSizeProperty, 12.0);
            txtImpresora.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)));
            var stackImpresora = new FrameworkElementFactory(typeof(StackPanel));
            stackImpresora.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackImpresora.AppendChild(txtImpresoraLabel);
            stackImpresora.AppendChild(txtImpresora);
            stackPanelInfo.AppendChild(stackImpresora);

            stackPanelInfo.SetValue(Grid.ColumnProperty, 2);
            grid.AppendChild(stackPanelInfo);

            border.AppendChild(grid);
            dataTemplate.VisualTree = border;

            return dataTemplate;
        }

        private void MostrarTabANPR(string tabNombre)
        {
            // Actualizar botones de pestañas
            var azul = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            var gris = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
            var transparente = System.Windows.Media.Brushes.Transparent;

            var btnCamaras = LogicalTreeHelper.FindLogicalNode(this, "BtnTabANPRCamaras") as Button;
            var btnCategorias = LogicalTreeHelper.FindLogicalNode(this, "BtnTabANPRCategorias") as Button;
            var btnListaBlanca = LogicalTreeHelper.FindLogicalNode(this, "BtnTabANPRListaBlanca") as Button;
            var btnListaNegra = LogicalTreeHelper.FindLogicalNode(this, "BtnTabANPRListaNegra") as Button;

            if (btnCamaras != null)
            {
                btnCamaras.Background = tabNombre == "Camaras" ? azul : transparente;
                btnCamaras.Foreground = tabNombre == "Camaras" ? System.Windows.Media.Brushes.White : gris;
            }
            if (btnCategorias != null)
            {
                btnCategorias.Background = tabNombre == "Categorias" ? azul : transparente;
                btnCategorias.Foreground = tabNombre == "Categorias" ? System.Windows.Media.Brushes.White : gris;
            }
            if (btnListaBlanca != null)
            {
                btnListaBlanca.Background = tabNombre == "ListaBlanca" ? azul : transparente;
                btnListaBlanca.Foreground = tabNombre == "ListaBlanca" ? System.Windows.Media.Brushes.White : gris;
            }
            if (btnListaNegra != null)
            {
                btnListaNegra.Background = tabNombre == "ListaNegra" ? azul : transparente;
                btnListaNegra.Foreground = tabNombre == "ListaNegra" ? System.Windows.Media.Brushes.White : gris;
            }

            // Mostrar panel correspondiente
            var panelContenido = LogicalTreeHelper.FindLogicalNode(this, "PanelContenidoANPR") as Grid;
            if (panelContenido != null)
            {
                var panelCamaras = LogicalTreeHelper.FindLogicalNode(panelContenido, "PanelANPRCamaras") as Grid;
                var panelCategorias = LogicalTreeHelper.FindLogicalNode(panelContenido, "PanelANPRCategorias") as Grid;
                var panelListaBlanca = LogicalTreeHelper.FindLogicalNode(panelContenido, "PanelANPRListaBlanca") as Grid;
                var panelListaNegra = LogicalTreeHelper.FindLogicalNode(panelContenido, "PanelANPRListaNegra") as Grid;

                if (panelCamaras != null) panelCamaras.Visibility = tabNombre == "Camaras" ? Visibility.Visible : Visibility.Collapsed;
                
                // Recargar panel de categorías cuando se muestre para obtener datos actualizados
                if (tabNombre == "Categorias" && panelCategorias != null)
                {
                    panelContenido.Children.Remove(panelCategorias);
                    var nuevoPanelCategorias = CrearPanelANPRCategorias();
                    nuevoPanelCategorias.Name = "PanelANPRCategorias";
                    nuevoPanelCategorias.Visibility = Visibility.Visible;
                    Grid.SetRow(nuevoPanelCategorias, 2);
                    panelContenido.Children.Add(nuevoPanelCategorias);
                }
                else if (panelCategorias != null)
                {
                    panelCategorias.Visibility = Visibility.Collapsed;
                }

                // Recargar panel de lista blanca cuando se muestre
                if (tabNombre == "ListaBlanca" && panelListaBlanca != null)
                {
                    panelContenido.Children.Remove(panelListaBlanca);
                    var nuevoPanelListaBlanca = CrearPanelListaBlanca();
                    nuevoPanelListaBlanca.Name = "PanelANPRListaBlanca";
                    nuevoPanelListaBlanca.Visibility = Visibility.Visible;
                    Grid.SetRow(nuevoPanelListaBlanca, 2);
                    panelContenido.Children.Add(nuevoPanelListaBlanca);
                }
                else if (panelListaBlanca != null)
                {
                    panelListaBlanca.Visibility = Visibility.Collapsed;
                }

                // Recargar panel de lista negra cuando se muestre
                if (tabNombre == "ListaNegra" && panelListaNegra != null)
                {
                    panelContenido.Children.Remove(panelListaNegra);
                    var nuevoPanelListaNegra = CrearPanelListaNegra();
                    nuevoPanelListaNegra.Name = "PanelANPRListaNegra";
                    nuevoPanelListaNegra.Visibility = Visibility.Visible;
                    Grid.SetRow(nuevoPanelListaNegra, 2);
                    panelContenido.Children.Add(nuevoPanelListaNegra);
                }
                else if (panelListaNegra != null)
                {
                    panelListaNegra.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CargarCamarasANPR()
        {
            try
            {
                var camaras = _dbService.ObtenerCamarasANPR();
                _camarasANPR.Clear();
                foreach (var camara in camaras)
                {
                    // Asignar el nombre de la impresora si existe
                    camara.ImpresoraNombre = camara.ImpresoraId ?? string.Empty;
                    _camarasANPR.Add(camara);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar cámaras: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarListaBlanca(ItemsControl itemsControl)
        {
            try
            {
                var lista = _dbService.ObtenerListaBlancaANPR();
                itemsControl.Items.Clear();
                
                foreach (var item in lista)
                {
                    var border = new Border
                    {
                        Background = System.Windows.Media.Brushes.White,
                        BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(15, 12, 15, 12)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var txtMatricula = new TextBlock
                    {
                        Text = item.Matricula,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(txtMatricula, 0);
                    grid.Children.Add(txtMatricula);

                    var txtDescripcion = new TextBlock
                    {
                        Text = item.Descripcion ?? "",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(txtDescripcion, 1);
                    grid.Children.Add(txtDescripcion);

                    var btnEliminar = new Button
                    {
                        Content = "Eliminar",
                        FontSize = 12,
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                        Foreground = System.Windows.Media.Brushes.White,
                        Padding = new Thickness(10, 5, 10, 5),
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    btnEliminar.Click += (s, e) => BtnEliminarListaBlanca_Click(item.Id, itemsControl);
                    Grid.SetColumn(btnEliminar, 2);
                    grid.Children.Add(btnEliminar);

                    border.Child = grid;
                    itemsControl.Items.Add(border);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar lista blanca: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarListaNegra(ItemsControl itemsControl)
        {
            try
            {
                var lista = _dbService.ObtenerListaNegraANPR();
                itemsControl.Items.Clear();
                
                foreach (var item in lista)
                {
                    var border = new Border
                    {
                        Background = System.Windows.Media.Brushes.White,
                        BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        Padding = new Thickness(15, 12, 15, 12)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200, GridUnitType.Pixel) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var txtMatricula = new TextBlock
                    {
                        Text = item.Matricula,
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(txtMatricula, 0);
                    grid.Children.Add(txtMatricula);

                    var txtDescripcion = new TextBlock
                    {
                        Text = item.Descripcion ?? "",
                        FontSize = 13,
                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(txtDescripcion, 1);
                    grid.Children.Add(txtDescripcion);

                    var btnEliminar = new Button
                    {
                        Content = "Eliminar",
                        FontSize = 12,
                        Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                        Foreground = System.Windows.Media.Brushes.White,
                        Padding = new Thickness(10, 5, 10, 5),
                        Cursor = Cursors.Hand,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    btnEliminar.Click += (s, e) => BtnEliminarListaNegra_Click(item.Id, itemsControl);
                    Grid.SetColumn(btnEliminar, 2);
                    grid.Children.Add(btnEliminar);

                    border.Child = grid;
                    itemsControl.Items.Add(border);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar lista negra: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAgregarListaBlanca_Click(TextBox txtMatricula, TextBox txtDescripcion)
        {
            try
            {
                string matricula = txtMatricula.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(matricula))
                {
                    MessageBox.Show("Por favor, ingrese una matrícula.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string descripcion = txtDescripcion.Text?.Trim();
                _dbService.AgregarAListaBlanca(matricula, string.IsNullOrWhiteSpace(descripcion) ? null : descripcion);
                
                txtMatricula.Text = "";
                txtDescripcion.Text = "";

                // Recargar la lista
                var panelContenido = LogicalTreeHelper.FindLogicalNode(this, "PanelContenidoANPR") as Grid;
                if (panelContenido != null)
                {
                    var panelListaBlanca = LogicalTreeHelper.FindLogicalNode(panelContenido, "PanelANPRListaBlanca") as Grid;
                    if (panelListaBlanca != null)
                    {
                        var itemsControl = LogicalTreeHelper.FindLogicalNode(panelListaBlanca, "ItemsControlListaBlanca") as ItemsControl;
                        if (itemsControl != null)
                        {
                            CargarListaBlanca(itemsControl);
                        }
                    }
                }

                MessageBox.Show("Placa agregada a la lista blanca correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UNIQUE"))
                {
                    MessageBox.Show("Esta matrícula ya está en la lista blanca.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Error al agregar placa: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEliminarListaBlanca_Click(int id, ItemsControl itemsControl)
        {
            try
            {
                var resultado = MessageBox.Show("¿Está seguro de que desea eliminar esta placa de la lista blanca?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resultado == MessageBoxResult.Yes)
                {
                    _dbService.EliminarDeListaBlanca(id);
                    CargarListaBlanca(itemsControl);
                    MessageBox.Show("Placa eliminada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar placa: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAgregarListaNegra_Click(TextBox txtMatricula, TextBox txtDescripcion)
        {
            try
            {
                string matricula = txtMatricula.Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(matricula))
                {
                    MessageBox.Show("Por favor, ingrese una matrícula.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string descripcion = txtDescripcion.Text?.Trim();
                _dbService.AgregarAListaNegra(matricula, string.IsNullOrWhiteSpace(descripcion) ? null : descripcion);
                
                txtMatricula.Text = "";
                txtDescripcion.Text = "";

                // Recargar la lista
                var panelContenido = LogicalTreeHelper.FindLogicalNode(this, "PanelContenidoANPR") as Grid;
                if (panelContenido != null)
                {
                    var panelListaNegra = LogicalTreeHelper.FindLogicalNode(panelContenido, "PanelANPRListaNegra") as Grid;
                    if (panelListaNegra != null)
                    {
                        var itemsControl = LogicalTreeHelper.FindLogicalNode(panelListaNegra, "ItemsControlListaNegra") as ItemsControl;
                        if (itemsControl != null)
                        {
                            CargarListaNegra(itemsControl);
                        }
                    }
                }

                MessageBox.Show("Placa agregada a la lista negra correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UNIQUE"))
                {
                    MessageBox.Show("Esta matrícula ya está en la lista negra.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show($"Error al agregar placa: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEliminarListaNegra_Click(int id, ItemsControl itemsControl)
        {
            try
            {
                var resultado = MessageBox.Show("¿Está seguro de que desea eliminar esta placa de la lista negra?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (resultado == MessageBoxResult.Yes)
                {
                    _dbService.EliminarDeListaNegra(id);
                    CargarListaNegra(itemsControl);
                    MessageBox.Show("Placa eliminada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar placa: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Verifica si una matrícula está en lista blanca o negra.
        /// Retorna: "blanca" si está en lista blanca (acceso gratuito), "negra" si está en lista negra (acceso denegado), null si no está en ninguna lista.
        /// </summary>
        public string? VerificarListasANPR(string matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula))
                return null;

            try
            {
                // Primero verificar lista negra (tiene prioridad)
                if (_dbService.EstaEnListaNegra(matricula))
                {
                    return "negra";
                }

                // Luego verificar lista blanca
                if (_dbService.EstaEnListaBlanca(matricula))
                {
                    return "blanca";
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar listas ANPR: {ex.Message}");
                return null;
            }
        }

        private void BtnAgregarCamaraANPR_Click(object sender, RoutedEventArgs e)
        {
            MostrarModalAgregarEditarCamara(null);
        }

        private void EliminarCamaraANPR(int camaraId)
        {
            var resultado = MessageBox.Show("¿Está seguro de que desea eliminar esta cámara?", "Confirmar eliminación", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    _dbService.EliminarCamaraANPR(camaraId);
                    CargarCamarasANPR();
                    MessageBox.Show("Cámara eliminada correctamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar cámara: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private Popup? _popupAgregarCamaraANPR = null;
        private CamaraANPR? _camaraEditando = null;

        private void MostrarModalAgregarEditarCamara(CamaraANPR? camara)
        {
            _camaraEditando = camara;
            bool esEdicion = camara != null;

            // Crear o reutilizar popup
            if (_popupAgregarCamaraANPR == null)
            {
                _popupAgregarCamaraANPR = new Popup
                {
                    AllowsTransparency = true,
                    PopupAnimation = PopupAnimation.Fade,
                    Placement = PlacementMode.Center,
                    StaysOpen = false
                };
            }

            // Contenedor principal con fondo semitransparente
            var borderBackdrop = new Border
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(153, 0, 0, 0))
            };
            borderBackdrop.MouseLeftButtonDown += (s, e) => { if (_popupAgregarCamaraANPR != null) _popupAgregarCamaraANPR.IsOpen = false; };

            // Contenedor del modal con Grid para estructura vertical
            var gridModal = new Grid
            {
                MaxWidth = 700,
                MaxHeight = 800
            };
            gridModal.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            gridModal.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Contenido scrollable
            gridModal.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botones

            var borderModal = new Border
            {
                Background = System.Windows.Media.Brushes.White,
                CornerRadius = new CornerRadius(8),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(2, 2, 2, 2)
            };
            borderModal.MouseLeftButtonDown += (s, e) => e.Handled = true;

            // Header
            var gridHeader = new Grid
            {
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Height = 50
            };
            gridHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var txtTitulo = new TextBlock
            {
                Text = esEdicion ? "Editar Cámara" : "Agregar Cámara",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0)
            };
            Grid.SetColumn(txtTitulo, 0);
            gridHeader.Children.Add(txtTitulo);

            var btnCerrar = new Button
            {
                Content = "✕",
                Width = 30,
                Height = 30,
                FontSize = 18,
                Background = System.Windows.Media.Brushes.Transparent,
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnCerrar.Click += (s, e) => { if (_popupAgregarCamaraANPR != null) _popupAgregarCamaraANPR.IsOpen = false; };
            Grid.SetColumn(btnCerrar, 1);
            gridHeader.Children.Add(btnCerrar);

            Grid.SetRow(gridHeader, 0);
            gridModal.Children.Add(gridHeader);

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Contenido del formulario
            var stackPanelContenido = new StackPanel
            {
                Margin = new Thickness(20, 20, 20, 20)
            };

            // Nombre de la Cámara
            var lblNombre = new TextBlock { Text = "Nombre de la Cámara", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var txtNombre = new TextBox
            {
                Name = "TxtCamaraNombre",
                Height = 34,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14,
                Text = camara?.Nombre ?? ""
            };
            stackPanelContenido.Children.Add(lblNombre);
            stackPanelContenido.Children.Add(txtNombre);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Marca
            var lblMarca = new TextBlock { Text = "Marca", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var cmbMarca = new ComboBox
            {
                Name = "CmbCamaraMarca",
                Height = 34,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            cmbMarca.Items.Add(new ComboBoxItem { Content = "Dahua", Tag = "Dahua", IsSelected = true });
            // Por ahora solo Dahua está disponible
            if (camara != null && !string.IsNullOrEmpty(camara.Marca))
            {
                foreach (ComboBoxItem item in cmbMarca.Items)
                {
                    if (item.Tag?.ToString() == camara.Marca)
                    {
                        cmbMarca.SelectedItem = item;
                        break;
                    }
                }
            }
            stackPanelContenido.Children.Add(lblMarca);
            stackPanelContenido.Children.Add(cmbMarca);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Tipo
            var lblTipo = new TextBlock { Text = "Tipo", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var stackPanelTipo = new StackPanel { Orientation = Orientation.Vertical };
            var rbEntrada = new RadioButton { Content = "Entrada", Name = "RbTipoEntrada", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), IsChecked = camara == null || camara.Tipo == "Entrada" };
            var rbSalida = new RadioButton { Content = "Salida", Name = "RbTipoSalida", FontSize = 14, Margin = new Thickness(0, 0, 0, 5), IsChecked = camara?.Tipo == "Salida" };
            var rbEntradaSalida = new RadioButton { Content = "Entrada y Salida", Name = "RbTipoEntradaSalida", FontSize = 14, IsChecked = camara?.Tipo == "Entrada y Salida" };
            stackPanelTipo.Children.Add(rbEntrada);
            stackPanelTipo.Children.Add(rbSalida);
            stackPanelTipo.Children.Add(rbEntradaSalida);
            stackPanelContenido.Children.Add(lblTipo);
            stackPanelContenido.Children.Add(stackPanelTipo);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Sentido de Circulación
            var lblSentido = new TextBlock
            {
                Text = "Sentido de Circulación",
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var txtDescripcionSentido = new TextBlock
            {
                Text = "Para definir el correcto sentido de circulación en el estacionamiento respecto a su cámara, marque la opción correcta.",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var stackPanelSentido = new StackPanel { Orientation = Orientation.Vertical };
            var rbSeAcerca = new RadioButton
            {
                Content = "El vehículo ingresa cuando se acerca a la cámara",
                Name = "RbSentidoSeAcerca",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 5),
                IsChecked = camara == null || camara.SentidoCirculacion == "Se acerca"
            };
            var rbSeAleja = new RadioButton
            {
                Content = "El vehículo ingresa cuando se aleja de la cámara",
                Name = "RbSentidoSeAleja",
                FontSize = 14,
                IsChecked = camara?.SentidoCirculacion == "Se aleja"
            };
            stackPanelSentido.Children.Add(rbSeAcerca);
            stackPanelSentido.Children.Add(rbSeAleja);
            stackPanelContenido.Children.Add(lblSentido);
            stackPanelContenido.Children.Add(txtDescripcionSentido);
            stackPanelContenido.Children.Add(stackPanelSentido);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Checkboxes
            var chkCapturaSinMatricula = new CheckBox
            {
                Name = "ChkCapturaSinMatricula",
                Content = "Captura vehículos sin Matrícula",
                FontSize = 14,
                IsChecked = camara?.CapturaSinMatricula ?? false,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var chkEncuadreVehiculo = new CheckBox
            {
                Name = "ChkEncuadreVehiculo",
                Content = "Encuadre del Vehículo (la imagen se recortará ajustándose al vehículo)",
                FontSize = 14,
                IsChecked = camara?.EncuadreVehiculo ?? false,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var chkConBarreras = new CheckBox
            {
                Name = "ChkConBarreras",
                Content = "Con Barreras vehiculares",
                FontSize = 14,
                IsChecked = camara?.ConBarrerasVehiculares ?? false,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanelContenido.Children.Add(chkCapturaSinMatricula);
            stackPanelContenido.Children.Add(chkEncuadreVehiculo);
            stackPanelContenido.Children.Add(chkConBarreras);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Panel de Barreras (visible solo si está marcado)
            var panelBarreras = new StackPanel { Name = "PanelBarreras", Visibility = chkConBarreras.IsChecked == true ? Visibility.Visible : Visibility.Collapsed };
            chkConBarreras.Checked += (s, e) => panelBarreras.Visibility = Visibility.Visible;
            chkConBarreras.Unchecked += (s, e) => panelBarreras.Visibility = Visibility.Collapsed;

            var lblRetardo = new TextBlock
            {
                Text = "Sólo en el caso de requerir, establezca el tiempo de retardo para apertura y cierre de barrera",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var gridRetardos = new Grid();
            gridRetardos.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridRetardos.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var stackRetardoApertura = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            var lblRetardoApertura = new TextBlock { Text = "Retardo para Apertura", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var txtRetardoApertura = new TextBox
            {
                Name = "TxtRetardoApertura",
                Height = 34,
                Text = (camara?.RetardoAperturaSegundos ?? 0).ToString(),
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            stackRetardoApertura.Children.Add(lblRetardoApertura);
            stackRetardoApertura.Children.Add(txtRetardoApertura);

            var stackRetardoCierre = new StackPanel();
            var lblRetardoCierre = new TextBlock { Text = "Retardo para Cierre", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var txtRetardoCierre = new TextBox
            {
                Name = "TxtRetardoCierre",
                Height = 34,
                Text = (camara?.RetardoCierreSegundos ?? 0).ToString(),
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            stackRetardoCierre.Children.Add(lblRetardoCierre);
            stackRetardoCierre.Children.Add(txtRetardoCierre);

            Grid.SetColumn(stackRetardoApertura, 0);
            Grid.SetColumn(stackRetardoCierre, 1);
            gridRetardos.Children.Add(stackRetardoApertura);
            gridRetardos.Children.Add(stackRetardoCierre);

            var lblAperturaManual = new TextBlock
            {
                Text = "Con la apertura manual activada, permitirá abrir la barrera vehicular con un simple click.",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 8)
            };
            var chkAperturaManual = new CheckBox
            {
                Name = "ChkAperturaManual",
                Content = "Apertura Manual de Barrera Vehicular",
                FontSize = 14,
                IsChecked = camara?.AperturaManual ?? false,
                Margin = new Thickness(0, 0, 0, 8)
            };

            // Campo condicional: Solicitar motivo de apertura (solo visible cuando Apertura Manual está activado)
            var chkSolicitarMotivo = new CheckBox
            {
                Name = "ChkSolicitarMotivoApertura",
                Content = "Solicitar motivo de la apertura (se solicitará al operador el motivo)",
                FontSize = 14,
                IsChecked = camara?.SolicitarMotivoApertura ?? false,
                Margin = new Thickness(0, 0, 0, 8),
                Visibility = (camara?.AperturaManual ?? false) ? Visibility.Visible : Visibility.Collapsed
            };

            // Actualizar visibilidad cuando cambia el checkbox de Apertura Manual
            chkAperturaManual.Checked += (s, e) =>
            {
                chkSolicitarMotivo.Visibility = Visibility.Visible;
            };
            chkAperturaManual.Unchecked += (s, e) =>
            {
                chkSolicitarMotivo.Visibility = Visibility.Collapsed;
                chkSolicitarMotivo.IsChecked = false;
            };

            panelBarreras.Children.Add(lblRetardo);
            panelBarreras.Children.Add(gridRetardos);
            panelBarreras.Children.Add(lblAperturaManual);
            panelBarreras.Children.Add(chkAperturaManual);
            panelBarreras.Children.Add(chkSolicitarMotivo);
            stackPanelContenido.Children.Add(panelBarreras);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Tolerancia de Salida
            var lblTolerancia = new TextBlock
            {
                Text = "Tolerancia de Salida",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var txtDescripcionTolerancia = new TextBlock
            {
                Text = "Indica el tiempo máximo de tolerancia, para que la barrera vehicular de salida se abra luego de realizar el pago del ticket. Por ejemplo, si fueran 10 minutos, el cliente tiene hasta 10 minutos para salir del estacionamiento, si se excede, la barrera de salida no abrirá. Para dejarlo desactivado, el valor debe ser 0 minutos.",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var txtToleranciaSalida = new TextBox
            {
                Name = "TxtToleranciaSalida",
                Height = 34,
                Text = (camara?.ToleranciaSalidaMinutos ?? 0).ToString(),
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            stackPanelContenido.Children.Add(lblTolerancia);
            stackPanelContenido.Children.Add(txtDescripcionTolerancia);
            stackPanelContenido.Children.Add(txtToleranciaSalida);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Pre-Ingreso
            var lblPreIngreso = new TextBlock
            {
                Text = "Pre-Ingreso",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var txtDescripcionPreIngreso = new TextBlock
            {
                Text = "Al activar esta opción el operador tendrá la opción de dar ingreso o rechazarlo. Al darle ingreso tendrá la opción definir tipo de vehículo, tarifa, etc. Si el pre-ingreso está desactivado, el ticket se creará automáticamente y los detalles podrán ser editados posteriormente.",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var chkPreIngreso = new CheckBox
            {
                Name = "ChkPreIngreso",
                Content = "Activar Pre-Ingreso",
                FontSize = 14,
                IsChecked = camara?.PreIngresoActivo ?? false,
                Margin = new Thickness(0, 0, 0, 8)
            };
            stackPanelContenido.Children.Add(lblPreIngreso);
            stackPanelContenido.Children.Add(txtDescripcionPreIngreso);
            stackPanelContenido.Children.Add(chkPreIngreso);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Impresora
            var lblImpresora = new TextBlock { Text = "Impresora (tickets de entrada)", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var cmbImpresora = new ComboBox
            {
                Name = "CmbImpresora",
                Height = 34,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            cmbImpresora.Items.Add(new ComboBoxItem { Content = "-- SIN IMPRESORA --", Tag = null });
            
            // Cargar impresoras del sistema
            try
            {
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    var item = new ComboBoxItem
                    {
                        Content = printer,
                        Tag = printer
                    };
                    cmbImpresora.Items.Add(item);
                    
                    // Seleccionar la impresora si la cámara ya tiene una asignada
                    if (camara != null && camara.ImpresoraId == printer)
                    {
                        cmbImpresora.SelectedItem = item;
                    }
                }
            }
            catch
            {
                // Si falla, agregar una opción por defecto
                cmbImpresora.Items.Add(new ComboBoxItem { Content = "Microsoft Print to PDF", Tag = "Microsoft Print to PDF" });
            }
            
            // Si no se seleccionó ninguna impresora, seleccionar "-- SIN IMPRESORA --"
            if (cmbImpresora.SelectedItem == null && cmbImpresora.Items.Count > 0)
            {
                cmbImpresora.SelectedIndex = 0;
            }
            
            stackPanelContenido.Children.Add(lblImpresora);
            stackPanelContenido.Children.Add(cmbImpresora);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Categoría predeterminada
            var lblCategoria = new TextBlock
            {
                Text = "Categoría de Vehículo predeterminada",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)),
                Margin = new Thickness(0, 0, 0, 4)
            };
            var txtDescripcionCategoria = new TextBlock
            {
                Text = "Al crear el ticket de forma automática se aplicará la siguiente categoría. Luego podrá editarla de ser necesario.",
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(107, 114, 128)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            };
            var cmbCategoria = new ComboBox
            {
                Name = "CmbCategoriaPredeterminada",
                Height = 34,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            cmbCategoria.Items.Add(new ComboBoxItem { Content = "-- Sin categoría --", Tag = null });
            foreach (var categoria in _categorias)
            {
                var item = new ComboBoxItem { Content = categoria.Nombre, Tag = categoria.Id };
                cmbCategoria.Items.Add(item);
                if (camara?.CategoriaPredeterminadaId == categoria.Id)
                {
                    cmbCategoria.SelectedItem = item;
                }
            }
            if (cmbCategoria.SelectedItem == null && cmbCategoria.Items.Count > 0)
            {
                cmbCategoria.SelectedIndex = 0;
            }
            stackPanelContenido.Children.Add(lblCategoria);
            stackPanelContenido.Children.Add(txtDescripcionCategoria);
            stackPanelContenido.Children.Add(cmbCategoria);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Host de la cámara (IP)
            var lblHost = new TextBlock { Text = "Host de la cámara (IP)", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var txtHost = new TextBox
            {
                Name = "TxtHostIP",
                Height = 34,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14,
                Text = camara?.HostIP ?? ""
            };
            if (string.IsNullOrEmpty(txtHost.Text))
            {
                var watermark = new TextBlock
                {
                    Text = "ej. 192.168.1.100",
                    Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)),
                    FontSize = 14,
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                txtHost.GotFocus += (s, e) => { if (txtHost.Parent is Grid grid) grid.Children.Remove(watermark); };
                txtHost.LostFocus += (s, e) => { if (string.IsNullOrEmpty(txtHost.Text) && txtHost.Parent is Grid grid && !grid.Children.Contains(watermark)) grid.Children.Add(watermark); };
            }
            stackPanelContenido.Children.Add(lblHost);
            stackPanelContenido.Children.Add(txtHost);
            stackPanelContenido.Children.Add(new TextBlock { Height = 12 });

            // Credenciales
            var lblCredenciales = new TextBlock { Text = "Credenciales de acceso a la cámara", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var gridCredenciales = new Grid();
            gridCredenciales.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridCredenciales.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            // Nota: ColumnDefinition no tiene propiedad Margin. Se debe aplicar margin a los elementos dentro de la columna.

            var stackUsuario = new StackPanel();
            var lblUsuario = new TextBlock { Text = "Usuario", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var txtUsuario = new TextBox
            {
                Name = "TxtUsuario",
                Height = 34,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14,
                Text = camara?.Usuario ?? ""
            };
            stackUsuario.Children.Add(lblUsuario);
            stackUsuario.Children.Add(txtUsuario);

            var stackClave = new StackPanel();
            var lblClave = new TextBlock { Text = "Clave", FontSize = 12, Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(75, 85, 99)), Margin = new Thickness(0, 0, 0, 4) };
            var txtClave = new PasswordBox
            {
                Name = "TxtClave",
                Height = 34,
                Padding = new Thickness(8, 4, 8, 4),
                FontSize = 14
            };
            if (camara != null && !string.IsNullOrEmpty(camara.Clave))
            {
                txtClave.Password = camara.Clave;
            }
            stackClave.Children.Add(lblClave);
            stackClave.Children.Add(txtClave);

            Grid.SetColumn(stackUsuario, 0);
            Grid.SetColumn(stackClave, 1);
            gridCredenciales.Children.Add(stackUsuario);
            gridCredenciales.Children.Add(stackClave);

            stackPanelContenido.Children.Add(lblCredenciales);
            stackPanelContenido.Children.Add(gridCredenciales);

            scrollViewer.Content = stackPanelContenido;
            Grid.SetRow(scrollViewer, 1);
            gridModal.Children.Add(scrollViewer);

            // Botones
            var stackPanelBotones = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 15, 20, 20)
            };

            var btnCancelar = new Button
            {
                Content = "Cancelar",
                Height = 40,
                MinWidth = 120,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = System.Windows.Media.Brushes.White,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                BorderThickness = new Thickness(1, 1, 1, 1),
                Cursor = Cursors.Hand,
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnCancelar.Click += (s, e) => { if (_popupAgregarCamaraANPR != null) _popupAgregarCamaraANPR.IsOpen = false; };

            var btnGuardar = new Button
            {
                Content = esEdicion ? "Guardar Cambios" : "Agregar Cámara",
                Name = "BtnGuardarCamara",
                Height = 40,
                MinWidth = 150,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnGuardar.Click += (s, e) => GuardarCamaraANPR(txtNombre, cmbMarca, rbEntrada, rbSalida, rbEntradaSalida, rbSeAcerca, rbSeAleja,
                chkCapturaSinMatricula, chkEncuadreVehiculo, chkConBarreras, txtRetardoApertura, txtRetardoCierre, chkAperturaManual, chkSolicitarMotivo,
                txtToleranciaSalida, chkPreIngreso, cmbImpresora, cmbCategoria, txtHost, txtUsuario, txtClave);

            stackPanelBotones.Children.Add(btnCancelar);
            stackPanelBotones.Children.Add(btnGuardar);
            Grid.SetRow(stackPanelBotones, 2);
            gridModal.Children.Add(stackPanelBotones);

            borderModal.Child = gridModal;
            borderBackdrop.Child = borderModal;
            _popupAgregarCamaraANPR.Child = borderBackdrop;

            // Centrar el popup en la pantalla
            _popupAgregarCamaraANPR.PlacementTarget = this;
            _popupAgregarCamaraANPR.Placement = PlacementMode.Center;
            _popupAgregarCamaraANPR.HorizontalOffset = 0;
            _popupAgregarCamaraANPR.VerticalOffset = 0;

            _popupAgregarCamaraANPR.IsOpen = true;
        }

        private void GuardarCamaraANPR(TextBox txtNombre, ComboBox cmbMarca, RadioButton rbEntrada, RadioButton rbSalida, RadioButton rbEntradaSalida,
            RadioButton rbSeAcerca, RadioButton rbSeAleja, CheckBox chkCapturaSinMatricula, CheckBox chkEncuadreVehiculo, CheckBox chkConBarreras,
            TextBox txtRetardoApertura, TextBox txtRetardoCierre, CheckBox chkAperturaManual, CheckBox chkSolicitarMotivo, TextBox txtToleranciaSalida, CheckBox chkPreIngreso,
            ComboBox cmbImpresora, ComboBox cmbCategoria, TextBox txtHost, TextBox txtUsuario, PasswordBox txtClave)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre de la cámara es obligatorio", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtHost.Text))
                {
                    MessageBox.Show("El host (IP) de la cámara es obligatorio", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var camara = _camaraEditando ?? new CamaraANPR();
                camara.Nombre = txtNombre.Text.Trim();
                camara.Marca = (cmbMarca.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Dahua";
                
                if (rbEntrada.IsChecked == true) camara.Tipo = "Entrada";
                else if (rbSalida.IsChecked == true) camara.Tipo = "Salida";
                else if (rbEntradaSalida.IsChecked == true) camara.Tipo = "Entrada y Salida";
                
                camara.SentidoCirculacion = rbSeAcerca.IsChecked == true ? "Se acerca" : "Se aleja";
                camara.CapturaSinMatricula = chkCapturaSinMatricula.IsChecked == true;
                camara.EncuadreVehiculo = chkEncuadreVehiculo.IsChecked == true;
                camara.ConBarrerasVehiculares = chkConBarreras.IsChecked == true;
                
                if (int.TryParse(txtRetardoApertura.Text, out int retardoApertura))
                    camara.RetardoAperturaSegundos = retardoApertura;
                if (int.TryParse(txtRetardoCierre.Text, out int retardoCierre))
                    camara.RetardoCierreSegundos = retardoCierre;
                    
                camara.AperturaManual = chkAperturaManual.IsChecked == true;
                camara.SolicitarMotivoApertura = chkSolicitarMotivo.IsChecked == true;
                
                if (int.TryParse(txtToleranciaSalida.Text, out int tolerancia))
                    camara.ToleranciaSalidaMinutos = tolerancia;
                    
                camara.PreIngresoActivo = chkPreIngreso.IsChecked == true;
                
                var impresoraItem = cmbImpresora.SelectedItem as ComboBoxItem;
                camara.ImpresoraId = impresoraItem?.Tag?.ToString();
                
                var categoriaItem = cmbCategoria.SelectedItem as ComboBoxItem;
                camara.CategoriaPredeterminadaId = categoriaItem?.Tag as int?;
                
                camara.HostIP = txtHost.Text.Trim();
                camara.Usuario = txtUsuario.Text.Trim();
                camara.Clave = txtClave.Password;

                if (_camaraEditando == null)
                {
                    _dbService.CrearCamaraANPR(camara);
                    MessageBox.Show("Cámara agregada correctamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    _dbService.ActualizarCamaraANPR(camara);
                    MessageBox.Show("Cámara actualizada correctamente", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                CargarCamarasANPR();
                if (_popupAgregarCamaraANPR != null)
                    _popupAgregarCamaraANPR.IsOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cámara: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnConfigModulo_Click(object sender, RoutedEventArgs e, string nombreModulo)
        {
            ResetearBotonesSubmenuConfiguracion();
            
            // Resetear todos los botones de módulos
            foreach (var btn in _botonesModulos.Values)
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));
            }
            
            if (_botonesModulos.ContainsKey(nombreModulo))
            {
                _botonesModulos[nombreModulo].Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                _botonesModulos[nombreModulo].Foreground = System.Windows.Media.Brushes.White;
            }
            
            // Ocultar todos los paneles estándar
            MostrarPanelConfiguracion("Ninguno");
            
            // Asegurar que el panel del módulo existe
            if (!_panelesModulos.ContainsKey(nombreModulo))
            {
                Grid panel;
                if (nombreModulo == "MONITOR")
                {
                    panel = CrearPanelMonitor();
                }
                else
                {
                    panel = new Grid
                    {
                        Name = $"PanelConfig{nombreModulo}",
                        Visibility = Visibility.Collapsed
                    };
                    var textBlock = new TextBlock
                    {
                        Text = "En desarrollo",
                        FontSize = 18,
                        Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    panel.Children.Add(textBlock);
                }
                
                panel.Name = $"PanelConfig{nombreModulo}";
                panel.Visibility = Visibility.Collapsed;
                
                if (PanelConfiguracion != null)
                {
                    PanelConfiguracion.Children.Add(panel);
                }
                _panelesModulos[nombreModulo] = panel;
            }
            
            // Ocultar todos los paneles de módulos
            foreach (var panel in _panelesModulos.Values)
            {
                panel.Visibility = Visibility.Collapsed;
            }
            
            // Mostrar el panel del módulo
            if (_panelesModulos.ContainsKey(nombreModulo))
            {
                _panelesModulos[nombreModulo].Visibility = Visibility.Visible;
                
                // Si es Pantalla Cliente, cargar configuración e iniciar servidor
                if (nombreModulo == "MONITOR")
                {
                    CargarConfiguracionPantallaCliente();
                    // Iniciar servidor si no está iniciado
                    if (_httpListener == null || !_httpListener.IsListening)
                    {
                        IniciarServidorPantallaCliente();
                    }
                    else
                    {
                        // Si ya está iniciado, solo actualizar UI
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var panel = _panelesModulos["MONITOR"];
                            var txtURL = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaURL") as TextBlock;
                            if (txtURL != null && !string.IsNullOrEmpty(_urlPantallaCliente))
                            {
                                txtURL.Text = _urlPantallaCliente;
                            }
                            GenerarQRCode();
                            ActualizarInstruccionesSegundoMonitor(panel);
                        }), DispatcherPriority.Loaded);
                    }
                }
                // Si es MercadoPago, cargar credenciales
                else if (nombreModulo == "MERCADOPAGO")
                {
                    CargarCredencialesMercadoPago();
                }
            }
        }

        private void BtnTabBaseDatos_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabAjustes("BaseDatos");
        }

        private void BtnAjustesGuardar_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (TxtAjustesNombre == null || string.IsNullOrWhiteSpace(TxtAjustesNombre.Text))
            {
                MessageBox.Show("El nombre de la empresa es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbAjustesPais == null || CmbAjustesPais.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un país.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var estacionamiento = new Estacionamiento
                {
                    Nombre = TxtAjustesNombre.Text.Trim(),
                    Direccion = TxtAjustesDireccion?.Text?.Trim() ?? string.Empty,
                    Ciudad = TxtAjustesCiudad?.Text?.Trim() ?? string.Empty,
                    Pais = (CmbAjustesPais.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Argentina",
                    Telefono = string.IsNullOrWhiteSpace(TxtAjustesTelefono?.Text) ? null : TxtAjustesTelefono.Text.Trim(),
                    Slogan = string.IsNullOrWhiteSpace(TxtAjustesSlogan?.Text) ? null : TxtAjustesSlogan.Text.Trim(),
                    Impresora = (CmbAjustesImpresora?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty,
                    Tema = "light" // tema fijo claro
                };

                _dbService.GuardarEstacionamiento(estacionamiento);

                // Actualizar el cache del estacionamiento para que los cambios se reflejen inmediatamente
                _estacionamientoCache = _dbService.ObtenerEstacionamiento();

                // Forzar tema claro
                if (_modoOscuro)
                {
                    _modoOscuro = false;
                    AplicarTema(false);
                }

                MessageBox.Show("Configuración guardada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private Categoria? _categoriaEditando;
        
        private void BtnNuevaCategoria_Click(object sender, RoutedEventArgs e)
        {
            _categoriaEditando = null;
            if (TxtTituloPopupCategoria != null)
                TxtTituloPopupCategoria.Text = "Nueva Categoría";
            if (TxtNombreCategoria != null)
                TxtNombreCategoria.Text = string.Empty;
            if (PopupCategoria != null)
                PopupCategoria.Visibility = Visibility.Visible;
            if (TxtNombreCategoria != null)
            {
                TxtNombreCategoria.Focus();
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    Keyboard.Focus(TxtNombreCategoria);
                }), DispatcherPriority.Loaded);
            }
        }
        
        private void BtnModificarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Categoria categoria)
            {
                _categoriaEditando = categoria;
                if (TxtTituloPopupCategoria != null)
                    TxtTituloPopupCategoria.Text = "Modificar Categoría";
                if (TxtNombreCategoria != null)
                    TxtNombreCategoria.Text = categoria.Nombre;
                if (PopupCategoria != null)
                    PopupCategoria.Visibility = Visibility.Visible;
                if (TxtNombreCategoria != null)
                {
                    TxtNombreCategoria.Focus();
                    Dispatcher.BeginInvoke(new Action(() => 
                    {
                        Keyboard.Focus(TxtNombreCategoria);
                    }), DispatcherPriority.Loaded);
                }
            }
        }
        
        private void BtnGuardarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (TxtNombreCategoria == null) return;
            
            string nombre = TxtNombreCategoria.Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre de la categoría no puede estar vacío.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (_categoriaEditando == null)
                {
                    // Crear nueva categoría
                    var nuevaCategoria = new Categoria
                    {
                        Nombre = nombre
                    };
                    _dbService.CrearCategoria(nuevaCategoria);
                }
                else
                {
                    // Modificar categoría existente
                    _categoriaEditando.Nombre = nombre;
                    _dbService.ActualizarCategoria(_categoriaEditando);
                }
                
                CargarCategoriasLista();
                ActualizarCategorias();
                
                // Cerrar popup
                if (PopupCategoria != null)
                    PopupCategoria.Visibility = Visibility.Collapsed;
                _categoriaEditando = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar categoría: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void BtnCancelarCategoria_Click(object sender, RoutedEventArgs e)
        {
            if (PopupCategoria != null)
                PopupCategoria.Visibility = Visibility.Collapsed;
            _categoriaEditando = null;
            if (TxtNombreCategoria != null)
                TxtNombreCategoria.Text = string.Empty;
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
                        CargarCategoriasLista();
                        ActualizarCategorias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar categoría: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        // Drag and Drop para categorías
        private void CategoriaItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is Categoria categoria)
            {
                _categoriaArrastrando = categoria;
                _borderArrastrando = border;
                _puntoInicioArrastre = e.GetPosition(border);
                // Calcular offset relativo al punto donde se hizo click en el elemento
                _offsetDrag = e.GetPosition(border);
                border.CaptureMouse();
            }
        }

        private void CategoriaItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (_categoriaArrastrando != null && e.LeftButton == MouseButtonState.Pressed)
            {
                if (sender is Border border)
                {
                    System.Windows.Point currentPosition = e.GetPosition(border);
                    if (Math.Abs(currentPosition.Y - _puntoInicioArrastre.Y) > 5)
                    {
                        // Crear ventana flotante para el preview
                        if (_borderArrastrando != null)
                        {
                            _dragPreviewWindow = new Window
                            {
                                WindowStyle = WindowStyle.None,
                                AllowsTransparency = true,
                                Background = System.Windows.Media.Brushes.Transparent,
                                ShowInTaskbar = false,
                                Topmost = true,
                                SizeToContent = SizeToContent.WidthAndHeight,
                                ResizeMode = ResizeMode.NoResize
                            };
                            
                            // Crear contenido del preview
                            var previewBorder = new Border
                            {
                                Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
                                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235)),
                                BorderThickness = new Thickness(1, 1, 1, 1),
                                CornerRadius = new CornerRadius(6),
                                Padding = new Thickness(15, 15, 15, 15),
                                Opacity = 0.95
                            };
                            
                            if (_borderArrastrando.ActualWidth > 0)
                            {
                                previewBorder.Width = _borderArrastrando.ActualWidth;
                            }
                            
                            var previewGrid = new Grid();
                            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                            previewGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                            
                            var dragIcon = new TextBlock
                            {
                                Text = "☰",
                                FontSize = 18,
                                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175)),
                                VerticalAlignment = VerticalAlignment.Center,
                                Margin = new Thickness(0, 0, 15, 0)
                            };
                            Grid.SetColumn(dragIcon, 0);
                            previewGrid.Children.Add(dragIcon);
                            
                            var nameText = new TextBlock
                            {
                                Text = _categoriaArrastrando.Nombre,
                                FontSize = 16,
                                FontWeight = FontWeights.SemiBold,
                                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 41, 55)),
                                VerticalAlignment = VerticalAlignment.Center
                            };
                            Grid.SetColumn(nameText, 1);
                            previewGrid.Children.Add(nameText);
                            
                            previewBorder.Child = previewGrid;
                            _dragPreviewWindow.Content = previewBorder;
                            
                            // Posicionar la ventana cerca del cursor
                            System.Windows.Point mousePos = e.GetPosition(this);
                            System.Windows.Point screenPos = this.PointToScreen(mousePos);
                            _dragPreviewWindow.Left = screenPos.X - _offsetDrag.X;
                            _dragPreviewWindow.Top = screenPos.Y - _offsetDrag.Y;
                            
                            _dragPreviewWindow.Show();
                        }
                        
                        // Cambiar opacidad del elemento original
                        border.Opacity = 0.3;
                        _isDragging = true;
                        
                        // Iniciar drag and drop - el preview se mantendrá visible durante el drag
                        DragDrop.DoDragDrop(border, _categoriaArrastrando, DragDropEffects.Move);
                        
                        // Limpiar DESPUÉS de que termine el drag
                        _isDragging = false;
                        if (_dragPreviewWindow != null)
                        {
                            _dragPreviewWindow.Close();
                            _dragPreviewWindow = null;
                        }
                        if (border != null)
                        {
                            border.Opacity = 1.0;
                        }
                    }
                }
            }
        }
        

        private void CategoriaItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                border.ReleaseMouseCapture();
                border.Opacity = 1.0;
                
                // Cerrar ventana flotante si existe
                _isDragging = false;
                if (_dragPreviewWindow != null)
                {
                    _dragPreviewWindow.Close();
                    _dragPreviewWindow = null;
                }
                
                _categoriaArrastrando = null;
                _borderArrastrando = null;
            }
        }

        private void CategoriaItem_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Categoria)))
            {
                e.Effects = DragDropEffects.Move;
                
                // Resaltar el elemento sobre el que se está pasando
                if (sender is Border border)
                {
                    border.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 59, 130, 246));
                    border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                }
            }
            e.Handled = true;
        }

        private void CategoriaItem_DragLeave(object sender, DragEventArgs e)
        {
            // Restaurar el estilo cuando se sale del elemento
            if (sender is Border border)
            {
                border.Background = System.Windows.Media.Brushes.White;
                border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));
            }
        }

        private void CategoriaItem_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            // Solo actualizar si estamos en proceso de drag
            if (!_isDragging)
            {
                return;
            }
            
            // Actualizar posición de la ventana flotante durante el drag para que siga el cursor
            if (_categoriaArrastrando != null && _dragPreviewWindow != null)
            {
                // Obtener posición del mouse en coordenadas de pantalla
                System.Windows.Point mousePos = Mouse.GetPosition(this);
                System.Windows.Point screenPos = this.PointToScreen(mousePos);
                
                // Actualizar posición de la ventana
                _dragPreviewWindow.Left = screenPos.X - _offsetDrag.X;
                _dragPreviewWindow.Top = screenPos.Y - _offsetDrag.Y;
            }
            
            // Cambiar el cursor durante el drag
            e.UseDefaultCursors = false;
            Mouse.SetCursor(Cursors.Hand);
            e.Handled = true;
        }
        
        private void CategoriaItem_QueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            // Detectar cuando se suelta el mouse durante el drag
            if (e.EscapePressed || Mouse.LeftButton == MouseButtonState.Released)
            {
                _isDragging = false;
                if (_dragPreviewWindow != null)
                {
                    _dragPreviewWindow.Close();
                    _dragPreviewWindow = null;
                }
            }
        }

        private void CategoriaItem_Drop(object sender, DragEventArgs e)
        {
            // Cerrar ventana flotante SOLO cuando se suelta
            _isDragging = false;
            if (_dragPreviewWindow != null)
            {
                _dragPreviewWindow.Close();
                _dragPreviewWindow = null;
            }
            
            // Limpiar eventos
            
            // Restaurar estilos
            if (sender is Border border)
            {
                border.Background = System.Windows.Media.Brushes.White;
                border.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));
            }
            
            if (_borderArrastrando != null)
            {
                _borderArrastrando.Opacity = 1.0;
            }
            
            if (e.Data.GetData(typeof(Categoria)) is Categoria categoriaOrigen)
            {
                Categoria? categoriaDestino = null;
                bool colocarAlFinal = false;
                
                // Si se soltó sobre un elemento específico
                if (sender is FrameworkElement element && element.DataContext is Categoria catDest)
                {
                    categoriaDestino = catDest;
                }
                else
                {
                    // Si se soltó fuera de los elementos, verificar si está por debajo del último
                    var scrollViewer = FindVisualChild<ScrollViewer>(PanelConfigCategorias);
                    if (scrollViewer != null)
                    {
                        System.Windows.Point dropPosition = e.GetPosition(scrollViewer);
                        var itemsControl = FindVisualChild<ItemsControl>(scrollViewer);
                        if (itemsControl != null)
                        {
                            // Obtener el último elemento visual
                            var ultimoElemento = GetUltimoElementoVisual(itemsControl);
                            if (ultimoElemento != null)
                            {
                                System.Windows.Point ultimoPos = ultimoElemento.TransformToAncestor(scrollViewer).Transform(new System.Windows.Point(0, 0));
                                double ultimoBottom = ultimoPos.Y + ultimoElemento.RenderSize.Height;
                                
                                // Si el drop está por debajo del último elemento
                                if (dropPosition.Y > ultimoBottom)
                                {
                                    colocarAlFinal = true;
                                    if (_categoriasLista.Count > 0)
                                    {
                                        categoriaDestino = _categoriasLista.Last();
                                    }
                                }
                            }
                        }
                    }
                }
                
                if (categoriaDestino != null && categoriaOrigen.Id != categoriaDestino.Id)
                {
                    // Reordenar en la colección
                    int indiceOrigen = _categoriasLista.IndexOf(categoriaOrigen);
                    int indiceDestino = _categoriasLista.IndexOf(categoriaDestino);
                    
                    if (indiceOrigen >= 0 && indiceDestino >= 0)
                    {
                        _categoriasLista.RemoveAt(indiceOrigen);
                        
                        if (colocarAlFinal)
                        {
                            // Colocar al final
                            _categoriasLista.Add(categoriaOrigen);
                        }
                        else
                        {
                            // Insertar en la posición de destino
                            if (indiceOrigen < indiceDestino)
                            {
                                // Si se movió hacia abajo, ajustar el índice
                                _categoriasLista.Insert(indiceDestino, categoriaOrigen);
                            }
                            else
                            {
                                // Si se movió hacia arriba
                                _categoriasLista.Insert(indiceDestino, categoriaOrigen);
                            }
                        }
                        
                        // Actualizar orden en la base de datos
                        try
                        {
                            _dbService.ActualizarOrdenCategorias(_categoriasLista.ToList());
                            CargarCategoriasLista(); // Recargar para asegurar sincronización
                            ActualizarCategorias(); // Actualizar footer
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error al actualizar orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            CargarCategoriasLista(); // Recargar en caso de error
                        }
                    }
                }
            }
            
            _categoriaArrastrando = null;
            _borderArrastrando = null;
        }
        
        private FrameworkElement? GetUltimoElementoVisual(DependencyObject parent)
        {
            FrameworkElement? ultimo = null;
            double maxY = double.MinValue;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement fe)
                {
                    System.Windows.Point pos = fe.TransformToAncestor(parent as Visual ?? this).Transform(new System.Windows.Point(0, 0));
                    double bottom = pos.Y + fe.RenderSize.Height;
                    
                    if (bottom > maxY)
                    {
                        maxY = bottom;
                        ultimo = fe;
                    }
                }
                
                var childUltimo = GetUltimoElementoVisual(child);
                if (childUltimo != null)
                {
                    System.Windows.Point pos = childUltimo.TransformToAncestor(parent as Visual ?? this).Transform(new System.Windows.Point(0, 0));
                    double bottom = pos.Y + childUltimo.RenderSize.Height;
                    
                    if (bottom > maxY)
                    {
                        maxY = bottom;
                        ultimo = childUltimo;
                    }
                }
            }
            
            return ultimo;
        }
        
        private void ItemsCategoriasLista_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(Categoria)))
            {
                e.Effects = DragDropEffects.Move;
            }
            e.Handled = true;
        }
        
        private void ItemsCategoriasLista_DragLeave(object sender, DragEventArgs e)
        {
            // No hacer nada, pero mantener el handler
        }
        
        private void ItemsCategoriasLista_Drop(object sender, DragEventArgs e)
        {
            // Cerrar ventana flotante SOLO cuando se suelta
            _isDragging = false;
            if (_dragPreviewWindow != null)
            {
                _dragPreviewWindow.Close();
                _dragPreviewWindow = null;
            }
            
            // Si se soltó directamente en el ItemsControl (fuera de los elementos o por debajo del último)
            if (e.Data.GetData(typeof(Categoria)) is Categoria categoriaOrigen)
            {
                // Verificar si se soltó por debajo del último elemento
                var scrollViewer = FindVisualChild<ScrollViewer>(PanelConfigCategorias);
                bool colocarAlFinal = false;
                
                if (scrollViewer != null)
                {
                    System.Windows.Point dropPosition = e.GetPosition(scrollViewer);
                    var itemsControl = FindVisualChild<ItemsControl>(scrollViewer);
                    if (itemsControl != null)
                    {
                        // Buscar el último Border (categoría) en el ItemsControl
                        Border? ultimoBorder = null;
                        double maxY = double.MinValue;
                        
                        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(itemsControl); i++)
                        {
                            var child = VisualTreeHelper.GetChild(itemsControl, i);
                            var border = FindVisualChild<Border>(child);
                            if (border != null && border.Name == "BorderCategoriaItem")
                            {
                                System.Windows.Point pos = border.TransformToAncestor(scrollViewer).Transform(new System.Windows.Point(0, 0));
                                double bottom = pos.Y + border.ActualHeight;
                                if (bottom > maxY)
                                {
                                    maxY = bottom;
                                    ultimoBorder = border;
                                }
                            }
                        }
                        
                        // Si el drop está por debajo del último elemento (con margen de 20px)
                        if (ultimoBorder != null && dropPosition.Y > maxY + 20)
                        {
                            colocarAlFinal = true;
                        }
                    }
                }
                
                // Colocar al final si se soltó por debajo del último
                int indiceOrigen = _categoriasLista.IndexOf(categoriaOrigen);
                
                if (indiceOrigen >= 0 && colocarAlFinal)
                {
                    _categoriasLista.RemoveAt(indiceOrigen);
                    _categoriasLista.Add(categoriaOrigen);
                    
                    // Actualizar orden en la base de datos
                    try
                    {
                        _dbService.ActualizarOrdenCategorias(_categoriasLista.ToList());
                        CargarCategoriasLista();
                        ActualizarCategorias();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al actualizar orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        CargarCategoriasLista();
                    }
                }
            }
            
            if (_borderArrastrando != null)
            {
                _borderArrastrando.Opacity = 1.0;
            }
            
            _categoriaArrastrando = null;
            _borderArrastrando = null;
        }

        private void BtnSimularTarifas_Click(object sender, RoutedEventArgs e)
        {
            var simuladorWindow = new SimuladorTarifasWindow(_dbService, _modoOscuro)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            simuladorWindow.ShowDialog();
        }

        private void ResetearBotonesMenu()
        {
            var azul = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            var transparente = System.Windows.Media.Brushes.Transparent;
            
            BtnInicio.Background = transparente;
            BtnMensuales.Background = transparente;
            BtnCaja.Background = transparente;
            BtnConfiguracion.Background = transparente;
        }

        // Eventos del submenú
        private void BtnTicketsAbiertos_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenu();
            BtnTicketsAbiertos.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnTicketsAbiertos.Foreground = System.Windows.Media.Brushes.White;
            MostrarTicketsAbiertos();
            ItemsTickets.ItemsSource = _ticketsFiltrados;
            if (PanelTicketsAbiertos != null) PanelTicketsAbiertos.Visibility = Visibility.Visible;
            if (PanelTicketsCerrados != null) PanelTicketsCerrados.Visibility = Visibility.Collapsed;
            if (PanelEntradasSalidas != null) PanelEntradasSalidas.Visibility = Visibility.Collapsed;
            if (BarraFiltrosAbiertos != null) BarraFiltrosAbiertos.Visibility = Visibility.Visible;
            if (PanelContadoresInicio != null) PanelContadoresInicio.Visibility = Visibility.Visible;
            if (FooterEstadisticas != null) FooterEstadisticas.Visibility = Visibility.Visible;
            CargarContadoresInicio();
            CargarFooter();
        }

        private void BtnTicketsCerrados_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenu();
            BtnTicketsCerrados.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnTicketsCerrados.Foreground = System.Windows.Media.Brushes.White;
            PanelContadoresInicio.Visibility = Visibility.Collapsed;
            if (PanelTicketsAbiertos != null) PanelTicketsAbiertos.Visibility = Visibility.Collapsed;
            if (PanelTicketsCerrados != null) PanelTicketsCerrados.Visibility = Visibility.Visible;
            if (PanelEntradasSalidas != null) PanelEntradasSalidas.Visibility = Visibility.Collapsed;
            if (BarraFiltrosAbiertos != null) BarraFiltrosAbiertos.Visibility = Visibility.Collapsed;
            AplicarFiltrosCerrados();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                AplicarTemaATicketsRenderizados();
            }), DispatcherPriority.Loaded);
        }

        private void BtnEntradasSalidas_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenu();
            BtnEntradasSalidas.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnEntradasSalidas.Foreground = System.Windows.Media.Brushes.White;
            if (PanelTicketsAbiertos != null) PanelTicketsAbiertos.Visibility = Visibility.Collapsed;
            if (PanelTicketsCerrados != null) PanelTicketsCerrados.Visibility = Visibility.Collapsed;
            if (PanelEntradasSalidas != null) PanelEntradasSalidas.Visibility = Visibility.Visible;
            if (BarraFiltrosAbiertos != null) BarraFiltrosAbiertos.Visibility = Visibility.Collapsed;
            if (PanelContadoresInicio != null) PanelContadoresInicio.Visibility = Visibility.Collapsed;
            if (CalendarEntradasSalidas != null) CalendarEntradasSalidas.SelectedDate = DateTime.Today;
            CargarEntradasSalidas();
        }

        private void BtnMensualesClientesTab_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabMensualesClientes();
        }

        private void BtnMensualesVehiculosTab_Click(object sender, RoutedEventArgs e)
        {
            MostrarTabMensualesVehiculos();
        }

        private void FiltroMensuales_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltrosMensuales();
        }

        private void FiltroMensuales_Checked(object sender, RoutedEventArgs e)
        {
            AplicarFiltrosMensuales();
        }

        private void FiltroMensuales_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltrosMensuales();
        }

        private void DataGridMensualesVehiculos_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataGridMensualesVehiculos.SelectedItem is VehiculoMensual veh)
            {
                var cliente = _clientesMensuales.FirstOrDefault(c => c.Id == veh.ClienteId);
                if (cliente != null)
                {
                    MostrarDetalleMensual(cliente);
                }
            }
        }

        private void BtnMensualesAgregarCliente_Click(object sender, RoutedEventArgs e)
        {
            TxtClienteMensualNombre.Text = "";
            TxtClienteMensualApellido.Text = "";
            TxtClienteMensualWhatsapp.Text = "";
            TxtClienteMensualEmail.Text = "";
            TxtClienteMensualDNI.Text = "";
            TxtClienteMensualCUIT.Text = "";
            TxtClienteMensualDireccion.Text = "";
            PopupNuevoClienteMensual.Visibility = Visibility.Visible;
            PopupNuevoClienteMensual.IsOpen = true;
        }

        private void BtnCancelarNuevoClienteMensual_Click(object sender, RoutedEventArgs e)
        {
            PopupNuevoClienteMensual.IsOpen = false;
            PopupNuevoClienteMensual.Visibility = Visibility.Collapsed;
        }

        private void BtnCerrarPopupNuevoClienteMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelarNuevoClienteMensual_Click(sender, e);
        }

        private void PopupNuevoClienteMensual_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            BtnCancelarNuevoClienteMensual_Click(sender, e);
        }

        private void PopupNuevoClienteMensual_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnGuardarNuevoClienteMensual_Click(object sender, RoutedEventArgs e)
        {
            var nombre = TxtClienteMensualNombre.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(nombre))
            {
                MessageBox.Show("El nombre es obligatorio.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var cliente = new ClienteMensual
            {
                Nombre = nombre,
                Apellido = TxtClienteMensualApellido.Text?.Trim() ?? "",
                Whatsapp = TxtClienteMensualWhatsapp.Text?.Trim() ?? "",
                Email = TxtClienteMensualEmail.Text?.Trim() ?? "",
                DNI = TxtClienteMensualDNI.Text?.Trim() ?? "",
                CUIT = TxtClienteMensualCUIT.Text?.Trim() ?? "",
                Direccion = TxtClienteMensualDireccion.Text?.Trim() ?? "",
                Activo = true,
                FechaCreacion = DateTime.Now
            };

            try
            {
                _dbService.CrearClienteMensual(cliente);
                PopupNuevoClienteMensual.IsOpen = false;
                PopupNuevoClienteMensual.Visibility = Visibility.Collapsed;
                CargarMensuales();
                MostrarTabMensualesClientes();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cliente: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnCancelarNuevoVehiculoMensual_Click(object sender, RoutedEventArgs e)
        {
            // Resetear campos
            TxtVehiculoMatricula.Text = "";
            TxtVehiculoMarcaModelo.Text = "";
            ChkVehiculoAlternativo.IsChecked = false;
            ChkVehiculoPrecioDif.IsChecked = false;
            TxtVehiculoPrecioPersonalizado.Text = "";
            if (StackPanelPrecioPersonalizado != null) StackPanelPrecioPersonalizado.Visibility = Visibility.Collapsed;
            TxtVehiculoPrecioPersonalizado.IsEnabled = false;
            RbVehiculoBonificarFinMes.IsChecked = false;
            RbVehiculoCargoProporcional.IsChecked = true;
            RbVehiculoCargoMesCompleto.IsChecked = false;
            CmbVehiculoCliente.IsEnabled = true; // Rehabilitar para cuando se abre desde otro lugar
            
            PopupNuevoVehiculoMensual.IsOpen = false;
            PopupNuevoVehiculoMensual.Visibility = Visibility.Collapsed;
        }

        private void BtnCerrarPopupNuevoVehiculoMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelarNuevoVehiculoMensual_Click(sender, e);
        }

        private void PopupNuevoVehiculoMensual_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            BtnCancelarNuevoVehiculoMensual_Click(sender, e);
        }

        private void PopupNuevoVehiculoMensual_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnGuardarNuevoVehiculoMensual_Click(object sender, RoutedEventArgs e)
        {
            ClienteMensual clienteSel = null;
            if (_clienteMensualSeleccionado != null)
            {
                clienteSel = _clienteMensualSeleccionado;
            }
            else if (CmbVehiculoCliente.SelectedItem is ClienteMensual cliente)
            {
                clienteSel = cliente;
            }
            
            if (clienteSel == null)
            {
                MessageBox.Show("Seleccione un cliente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            string mat = (TxtVehiculoMatricula.Text ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(mat))
            {
                MessageBox.Show("Ingrese matrícula.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (mat.Length < 3)
            {
                MessageBox.Show("La matrícula debe tener al menos 3 caracteres.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Validar que la categoría esté seleccionada
            if (CmbVehiculoCategoria.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una categoría.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Validar que la tarifa esté seleccionada
            if (CmbVehiculoTarifa.SelectedItem == null)
            {
                MessageBox.Show("Seleccione una tarifa.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar precio personalizado si está marcado
            decimal? precioPersonalizado = null;
            if (ChkVehiculoPrecioDif.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(TxtVehiculoPrecioPersonalizado.Text) || 
                    !decimal.TryParse(TxtVehiculoPrecioPersonalizado.Text, out decimal precio))
                {
                    MessageBox.Show("Ingrese un precio válido para precio diferenciado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                precioPersonalizado = precio;
            }

            // Calcular el próximo cargo (día 1 del mes siguiente)
            DateTime proximoCargo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            
            var veh = new VehiculoMensual
            {
                ClienteId = clienteSel.Id,
                Matricula = mat,
                MarcaModelo = TxtVehiculoMarcaModelo.Text?.Trim() ?? "",
                CategoriaId = CmbVehiculoCategoria.SelectedItem is Categoria cat ? cat.Id : (int?)null,
                TarifaId = CmbVehiculoTarifa.SelectedItem is Tarifa tar ? tar.Id : (int?)null,
                EsAlternativo = ChkVehiculoAlternativo.IsChecked == true,
                TienePrecioDiferenciado = ChkVehiculoPrecioDif.IsChecked == true,
                PrecioPersonalizado = precioPersonalizado,
                GenerarCargoHastaFinDeMes = RbVehiculoBonificarFinMes.IsChecked == true,
                GenerarCargoProporcional = RbVehiculoCargoProporcional.IsChecked == true,
                GenerarCargoMesCompleto = RbVehiculoCargoMesCompleto.IsChecked == true,
                ProximoCargo = proximoCargo,
                FechaCreacion = DateTime.Now
            };

            try
            {
                int vehiculoId = _dbService.CrearVehiculoMensual(veh);
                veh.Id = vehiculoId; // Asignar el ID para poder actualizarlo después
                
                // Crear cargo inicial según la opción seleccionada
                if (veh.CategoriaId.HasValue && veh.TarifaId.HasValue && _currentAdminId.HasValue)
                {
                    decimal montoCargo = 0m;
                    string descripcionCargo = "";
                    
                    // Determinar el monto según la opción
                    
                    if (RbVehiculoCargoProporcional.IsChecked == true)
                    {
                        // Proporcional: mitad del monto
                        if (precioPersonalizado.HasValue)
                        {
                            montoCargo = precioPersonalizado.Value / 2;
                        }
                        else
                        {
                            var precio = _dbService.ObtenerPrecio(veh.TarifaId.Value, veh.CategoriaId.Value);
                            if (precio != null)
                            {
                                montoCargo = precio.Monto / 2;
                            }
                        }
                        var mesActual = DateTime.Now.ToString("MM/yyyy");
                        descripcionCargo = $"Inicio de Estacionamiento mensual - #{mat} ({mesActual})";
                    }
                    else if (RbVehiculoCargoMesCompleto.IsChecked == true)
                    {
                        // Mes completo: monto completo
                        if (precioPersonalizado.HasValue)
                        {
                            montoCargo = precioPersonalizado.Value;
                        }
                        else
                        {
                            var precio = _dbService.ObtenerPrecio(veh.TarifaId.Value, veh.CategoriaId.Value);
                            if (precio != null)
                            {
                                montoCargo = precio.Monto;
                            }
                        }
                        var mesActual = DateTime.Now.ToString("MM/yyyy");
                        descripcionCargo = $"Inicio de Estacionamiento mensual - #{mat} ({mesActual})";
                    }
                    // Si es "Bonificar hasta fin de mes", no se crea ningún cargo (montoCargo = 0)
                    
                    // Crear el movimiento solo si hay un monto
                    if (montoCargo > 0)
                    {
                        var movimiento = new MovimientoMensual
                        {
                            ClienteId = clienteSel.Id,
                            VehiculoId = vehiculoId,
                            Tipo = "Cargo",
                            Importe = -montoCargo, // Negativo porque es un cargo
                            Descripcion = descripcionCargo,
                            Fecha = DateTime.Now,
                            AdminId = _currentAdminId.Value,
                            EsRecibo = false,
                            MatriculaReferencia = mat
                        };
                        
                        _dbService.CrearMovimientoMensual(movimiento);
                    }
                    
                    // Actualizar el vehículo con la fecha del próximo cargo
                    _dbService.ActualizarVehiculoMensual(veh);
                }
                
                // Resetear campos
                TxtVehiculoMatricula.Text = "";
                TxtVehiculoMarcaModelo.Text = "";
                ChkVehiculoAlternativo.IsChecked = false;
                ChkVehiculoPrecioDif.IsChecked = false;
                TxtVehiculoPrecioPersonalizado.Text = "";
                TxtVehiculoPrecioPersonalizado.Visibility = Visibility.Collapsed;
                TxtVehiculoPrecioPersonalizado.IsEnabled = false;
                RbVehiculoBonificarFinMes.IsChecked = false;
                RbVehiculoCargoProporcional.IsChecked = true;
                RbVehiculoCargoMesCompleto.IsChecked = false;
                CmbVehiculoCliente.IsEnabled = true;
                
                PopupNuevoVehiculoMensual.IsOpen = false;
                PopupNuevoVehiculoMensual.Visibility = Visibility.Collapsed;
                CargarMensuales();
                if (_clienteMensualSeleccionado != null)
                {
                    MostrarDetalleMensual(_clienteMensualSeleccionado);
                }
                else
                {
                    MostrarTabMensualesClientes();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar vehículo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                // Asegurar que el popup siga abierto después del error
                PopupNuevoVehiculoMensual.IsOpen = true;
            }
        }

        private void BtnAgregarVehiculoDesdeDetalle_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteMensualSeleccionado == null) return;

            CmbVehiculoCliente.ItemsSource = _clientesMensuales;
            CmbVehiculoCliente.DisplayMemberPath = "NombreCompleto";
            CmbVehiculoCliente.SelectedItem = _clienteMensualSeleccionado;
            CmbVehiculoCliente.IsEnabled = false; // Deshabilitado cuando viene del detalle

            CmbVehiculoCategoria.ItemsSource = _categorias;
            CmbVehiculoCategoria.DisplayMemberPath = "Nombre";
            CmbVehiculoCategoria.SelectedItem = null;

            var tarifasMensuales = _tarifas.Where(t => t.Tipo == TipoTarifa.Mensual).ToList();
            CmbVehiculoTarifa.ItemsSource = tarifasMensuales;
            CmbVehiculoTarifa.DisplayMemberPath = "Nombre";
            CmbVehiculoTarifa.SelectedItem = null;

            TxtVehiculoMatricula.Text = "";
            TxtVehiculoMarcaModelo.Text = "";
            ChkVehiculoAlternativo.IsChecked = false;
            ChkVehiculoPrecioDif.IsChecked = false;
            TxtVehiculoPrecioPersonalizado.Text = "";
            if (StackPanelPrecioPersonalizado != null) StackPanelPrecioPersonalizado.Visibility = Visibility.Collapsed;
            TxtVehiculoPrecioPersonalizado.IsEnabled = false;

            // Resetear radio buttons
            RbVehiculoBonificarFinMes.IsChecked = false;
            RbVehiculoCargoProporcional.IsChecked = true;
            RbVehiculoCargoMesCompleto.IsChecked = false;

            PopupNuevoVehiculoMensual.Visibility = Visibility.Visible;
            PopupNuevoVehiculoMensual.IsOpen = true;
        }

        private void ChkVehiculoPrecioDif_Checked(object sender, RoutedEventArgs e)
        {
            if (StackPanelPrecioPersonalizado != null)
            {
                StackPanelPrecioPersonalizado.Visibility = Visibility.Visible;
            }
            if (TxtVehiculoPrecioPersonalizado != null)
            {
                TxtVehiculoPrecioPersonalizado.IsEnabled = true;
                TxtVehiculoPrecioPersonalizado.Focus();
            }
        }

        private void ChkVehiculoPrecioDif_Unchecked(object sender, RoutedEventArgs e)
        {
            if (StackPanelPrecioPersonalizado != null)
            {
                StackPanelPrecioPersonalizado.Visibility = Visibility.Collapsed;
            }
            if (TxtVehiculoPrecioPersonalizado != null)
            {
                TxtVehiculoPrecioPersonalizado.IsEnabled = false;
                TxtVehiculoPrecioPersonalizado.Text = "";
            }
        }

        private void BtnDetalleClienteEditar_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteMensualSeleccionado == null) return;
            // TODO: Implementar edición de cliente
            MessageBox.Show("Funcionalidad de edición en desarrollo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnDetalleClienteArchivar_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteMensualSeleccionado == null) return;
            var resultado = MessageBox.Show($"¿Desea archivar al cliente {_clienteMensualSeleccionado.NombreCompleto}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    _clienteMensualSeleccionado.EstaActivo = false;
                    _dbService.ActualizarClienteMensual(_clienteMensualSeleccionado);
                    CargarMensuales();
                    BtnMensualesVolver_Click(sender, e);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al archivar cliente: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDetalleClienteEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteMensualSeleccionado == null) return;
            var resultado = MessageBox.Show($"¿Está seguro de eliminar permanentemente al cliente {_clienteMensualSeleccionado.NombreCompleto}? Esta acción no se puede deshacer.", "Confirmar eliminación", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                    _dbService.EliminarClienteMensual(_clienteMensualSeleccionado.Id);
                    CargarMensuales();
                    BtnMensualesVolver_Click(sender, e);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar cliente: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnDetalleClienteFusionar_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteMensualSeleccionado == null) return;
            // TODO: Implementar fusión de cuentas
            MessageBox.Show("Funcionalidad de fusión en desarrollo.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCrearCargoMensual_Click(object sender, RoutedEventArgs e)
        {
            PrepararMovimientoMensual("Cargo");
        }

        private void BtnIngresarPagoMensual_Click(object sender, RoutedEventArgs e)
        {
            PrepararMovimientoMensual("Pago");
        }

        private void BtnPagoAdelantadoMensual_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteMensualSeleccionado == null)
            {
                MessageBox.Show("Seleccione un cliente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Verificar que el balance no sea negativo (no tenga deuda)
            var balance = _dbService.ObtenerBalanceClienteMensual(_clienteMensualSeleccionado.Id);
            if (balance < 0)
            {
                MessageBox.Show("No se puede realizar un pago adelantado si el balance es negativo. Antes debe realizar el pago de la deuda.", 
                    "Información", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Obtener el Próximo Cargo de los vehículos (el más próximo)
            var vehiculos = _dbService.ObtenerVehiculosMensuales(_clienteMensualSeleccionado.Id);
            DateTime? proximoCargoMinimo = null;
            
            foreach (var vehiculo in vehiculos)
            {
                if (vehiculo.ProximoCargo.HasValue)
                {
                    if (!proximoCargoMinimo.HasValue || vehiculo.ProximoCargo.Value < proximoCargoMinimo.Value)
                    {
                        proximoCargoMinimo = vehiculo.ProximoCargo.Value;
                    }
                }
            }
            
            // Si no hay ningún ProximoCargo definido, usar el día 1 del siguiente mes
            if (!proximoCargoMinimo.HasValue)
            {
                proximoCargoMinimo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            }
            
            // Obtener pagos adelantados previos para verificar el último mes pagado
            var movimientos = _dbService.ObtenerMovimientosMensuales(_clienteMensualSeleccionado.Id);
            var pagosAdelantados = movimientos.Where(m => m.Tipo == "PagoAdelantado" && !string.IsNullOrEmpty(m.MesAplicado)).ToList();
            
            DateTime? ultimoMesPagado = null;
            foreach (var pago in pagosAdelantados)
            {
                if (!string.IsNullOrEmpty(pago.MesAplicado))
                {
                    // Convertir formato yyyy-MM a DateTime
                    var partes = pago.MesAplicado.Split('-');
                    if (partes.Length == 2 && int.TryParse(partes[0], out int año) && int.TryParse(partes[1], out int mes))
                    {
                        var mesPago = new DateTime(año, mes, 1);
                        if (!ultimoMesPagado.HasValue || mesPago > ultimoMesPagado.Value)
                        {
                            ultimoMesPagado = mesPago;
                        }
                    }
                }
            }
            
            // El mes mínimo debe ser el Próximo Cargo (que ya considera los pagos adelantados previos)
            var mesMinimo = proximoCargoMinimo.Value;
            
            // Configurar el DatePicker con el próximo cargo como fecha mínima
            if (DpPagoAdelantadoMes != null)
            {
                DpPagoAdelantadoMes.DisplayDateStart = mesMinimo;
                DpPagoAdelantadoMes.SelectedDate = mesMinimo;
            }
            
            PopupPagoAdelantado.Visibility = Visibility.Visible;
            PopupPagoAdelantado.IsOpen = true;
        }

        private void BtnCerrarPopupPagoAdelantado_Click(object sender, RoutedEventArgs e)
        {
            PopupPagoAdelantado.IsOpen = false;
            PopupPagoAdelantado.Visibility = Visibility.Collapsed;
            _importePagoAdelantado = 0m;
            _mesPagoAdelantado = null;
        }

        private void PopupPagoAdelantado_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            BtnCerrarPopupPagoAdelantado_Click(sender, e);
        }

        private void PopupPagoAdelantado_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void DpPagoAdelantadoMes_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Validar que la fecha seleccionada sea válida y calcular importe
            if (DpPagoAdelantadoMes != null && DpPagoAdelantadoMes.SelectedDate.HasValue && _clienteMensualSeleccionado != null)
            {
                var proximoCargo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                var fechaSeleccionada = DpPagoAdelantadoMes.SelectedDate.Value;
                
                // Asegurar que sea el primer día del mes
                var fechaAjustada = new DateTime(fechaSeleccionada.Year, fechaSeleccionada.Month, 1);
                
                if (fechaAjustada < proximoCargo)
                {
                    fechaAjustada = proximoCargo;
                    DpPagoAdelantadoMes.SelectedDate = fechaAjustada;
                    return;
                }
                
                // Calcular cantidad de meses
                int cantidadMeses = ((fechaAjustada.Year - proximoCargo.Year) * 12) + (fechaAjustada.Month - proximoCargo.Month) + 1;
                
                // Obtener vehículos del cliente
                var vehiculos = _dbService.ObtenerVehiculosMensuales(_clienteMensualSeleccionado.Id);
                decimal importeTotal = 0m;
                
                foreach (var vehiculo in vehiculos)
                {
                    decimal precioMensual = 0m;
                    
                    // Si tiene precio diferenciado, usar ese
                    if (vehiculo.TienePrecioDiferenciado && vehiculo.PrecioPersonalizado.HasValue)
                    {
                        precioMensual = vehiculo.PrecioPersonalizado.Value;
                    }
                    else if (vehiculo.CategoriaId.HasValue && vehiculo.TarifaId.HasValue)
                    {
                        // Obtener precio de la base de datos
                        var precio = _dbService.ObtenerPrecio(vehiculo.TarifaId.Value, vehiculo.CategoriaId.Value);
                        if (precio != null)
                        {
                            precioMensual = precio.Monto;
                        }
                    }
                    
                    importeTotal += precioMensual * cantidadMeses;
                }
                
                // Actualizar el importe en el popup si existe (aunque aún no esté abierto)
                // Lo guardaremos en una variable para cuando se abra el popup de pago
                _importePagoAdelantado = importeTotal;
            }
        }

        private void BtnPagoAdelantadoContinuar_Click(object sender, RoutedEventArgs e)
        {
            if (DpPagoAdelantadoMes?.SelectedDate == null)
            {
                MessageBox.Show("Seleccione un mes válido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                // No cerrar el popup
                return;
            }

            var mesSeleccionado = DpPagoAdelantadoMes.SelectedDate.Value;
            var mesFormato = mesSeleccionado.ToString("MM/yyyy");

            // Cerrar popup de pago adelantado
            PopupPagoAdelantado.IsOpen = false;
            PopupPagoAdelantado.Visibility = Visibility.Collapsed;

            // Preparar el popup de pago normal con el mes seleccionado
            _tipoMovimientoMensual = "PagoAdelantado";
            if (TxtTituloMovimientoMensual != null)
            {
                TxtTituloMovimientoMensual.Text = "Pago de Cuenta Corriente";
            }
            
            if (BtnMovimientoMensualGuardar != null)
            {
                BtnMovimientoMensualGuardar.Content = "Realizar Cobro";
            }

            // Configurar detalle con el mes seleccionado
            var clienteNombre = _clienteMensualSeleccionado?.NombreCompleto ?? "";
            var primeraMatricula = _clienteMensualSeleccionado?.MatriculasConcat?.Split('•').FirstOrDefault()?.Trim() ?? "";
            TxtMovimientoDetalle.Text = $"Pago CC - {clienteNombre}";
            if (!string.IsNullOrEmpty(primeraMatricula))
            {
                TxtMovimientoDetalle.Text += $" (#{primeraMatricula})";
            }
            TxtMovimientoDetalle.Text += $" - Pago mes {mesFormato}";

            // Calcular y establecer el importe automáticamente
            var proximoCargo = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
            var fechaSeleccionada = new DateTime(mesSeleccionado.Year, mesSeleccionado.Month, 1);
            int cantidadMeses = ((fechaSeleccionada.Year - proximoCargo.Year) * 12) + (fechaSeleccionada.Month - proximoCargo.Month) + 1;
            
            // Obtener vehículos del cliente y calcular importe
            var vehiculos = _dbService.ObtenerVehiculosMensuales(_clienteMensualSeleccionado.Id);
            decimal importeTotal = 0m;
            
            foreach (var vehiculo in vehiculos)
            {
                decimal precioMensual = 0m;
                
                // Si tiene precio diferenciado, usar ese
                if (vehiculo.TienePrecioDiferenciado && vehiculo.PrecioPersonalizado.HasValue)
                {
                    precioMensual = vehiculo.PrecioPersonalizado.Value;
                }
                else if (vehiculo.CategoriaId.HasValue && vehiculo.TarifaId.HasValue)
                {
                    // Obtener precio de la base de datos
                    var precio = _dbService.ObtenerPrecio(vehiculo.TarifaId.Value, vehiculo.CategoriaId.Value);
                    if (precio != null)
                    {
                        precioMensual = precio.Monto;
                    }
                }
                
                importeTotal += precioMensual * cantidadMeses;
            }
            
            TxtMovimientoImporte.Text = importeTotal.ToString("F2");

            // Mostrar sección de forma de pago y recibo
            if (StackPanelFormaPagoMensual != null) StackPanelFormaPagoMensual.Visibility = Visibility.Visible;
            if (StackPanelReciboMensual != null) StackPanelReciboMensual.Visibility = Visibility.Visible;
            
            // Cargar formas de pago
            if (ListBoxFormasPagoMensual != null)
            {
                ListBoxFormasPagoMensual.ItemsSource = _formasPago;
                if (ListBoxFormasPagoMensual.Items.Count > 0)
                    ListBoxFormasPagoMensual.SelectedIndex = 0;
            }
            
            // Resetear radio buttons
            if (RbMovimientoSinComprobante != null) RbMovimientoSinComprobante.IsChecked = true;
            if (RbMovimientoRecibo != null) RbMovimientoRecibo.IsChecked = false;

            // Ocultar DatePicker del movimiento (ya no se usa aquí)
            if (DpMovimientoMes != null) DpMovimientoMes.Visibility = Visibility.Collapsed;

            // Guardar el mes seleccionado para usarlo al guardar
            _mesPagoAdelantado = mesFormato;

            // Abrir popup de pago
            PopupMovimientoMensual.Visibility = Visibility.Visible;
            PopupMovimientoMensual.IsOpen = true;
        }

        private void BtnAjusteMensual_Click(object sender, RoutedEventArgs e)
        {
            PrepararMovimientoMensual("Ajuste");
        }

        private void PrepararMovimientoMensual(string tipo)
        {
            if (_clienteMensualSeleccionado == null)
            {
                MessageBox.Show("Seleccione un cliente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _tipoMovimientoMensual = tipo;
            if (TxtTituloMovimientoMensual != null)
            {
                TxtTituloMovimientoMensual.Text = tipo switch
                {
                    "Cargo" => "Agregar Cargo",
                    "Pago" => "Pago de Cuenta Corriente",
                    "PagoAdelantado" => "Pago Adelantado",
                    "Ajuste" => "Ajuste de Cuenta",
                    _ => "Movimiento"
                };
            }
            
            // Cambiar texto del botón para pagos
            if (BtnMovimientoMensualGuardar != null)
            {
                BtnMovimientoMensualGuardar.Content = (tipo == "Pago" || tipo == "PagoAdelantado") ? "Realizar Cobro" : "Aceptar";
            }
            TxtMovimientoImporte.Text = "";
            
            // Configurar detalle por defecto para pagos
            if (tipo == "Pago" || tipo == "PagoAdelantado")
            {
                var clienteNombre = _clienteMensualSeleccionado?.NombreCompleto ?? "";
                var primeraMatricula = _clienteMensualSeleccionado?.MatriculasConcat?.Split('•').FirstOrDefault()?.Trim() ?? "";
                TxtMovimientoDetalle.Text = $"Pago CC - {clienteNombre}";
                if (!string.IsNullOrEmpty(primeraMatricula))
                {
                    TxtMovimientoDetalle.Text += $" (#{primeraMatricula})";
                }
                
                // Mostrar sección de forma de pago y recibo
                if (StackPanelFormaPagoMensual != null) StackPanelFormaPagoMensual.Visibility = Visibility.Visible;
                if (StackPanelReciboMensual != null) StackPanelReciboMensual.Visibility = Visibility.Visible;
                
                // Cargar formas de pago
                if (ListBoxFormasPagoMensual != null)
                {
                    ListBoxFormasPagoMensual.ItemsSource = _formasPago;
                    if (ListBoxFormasPagoMensual.Items.Count > 0)
                        ListBoxFormasPagoMensual.SelectedIndex = 0;
                }
                
                // Resetear radio buttons
                if (RbMovimientoSinComprobante != null) RbMovimientoSinComprobante.IsChecked = true;
                if (RbMovimientoRecibo != null) RbMovimientoRecibo.IsChecked = false;
            }
            else
            {
                TxtMovimientoDetalle.Text = "";
                // Ocultar sección de forma de pago y recibo
                if (StackPanelFormaPagoMensual != null) StackPanelFormaPagoMensual.Visibility = Visibility.Collapsed;
                if (StackPanelReciboMensual != null) StackPanelReciboMensual.Visibility = Visibility.Collapsed;
            }
            
            if (tipo == "PagoAdelantado")
            {
                // Buscar el DatePicker en el popup
                var popupContent = PopupMovimientoMensual.Child as FrameworkElement;
                var dpMes = popupContent?.FindName("DpMovimientoMes") as DatePicker ?? 
                           (this.FindName("DpMovimientoMes") as DatePicker);
                if (dpMes != null)
                {
                    dpMes.Visibility = Visibility.Visible;
                    dpMes.SelectedDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }
            }
            else
            {
                var popupContent = PopupMovimientoMensual.Child as FrameworkElement;
                var dpMes = popupContent?.FindName("DpMovimientoMes") as DatePicker ?? 
                           (this.FindName("DpMovimientoMes") as DatePicker);
                if (dpMes != null) dpMes.Visibility = Visibility.Collapsed;
            }

            PopupMovimientoMensual.Visibility = Visibility.Visible;
            PopupMovimientoMensual.IsOpen = true;
        }

        private void BtnMovimientoMensualCancelar_Click(object sender, RoutedEventArgs e)
        {
            PopupMovimientoMensual.IsOpen = false;
            PopupMovimientoMensual.Visibility = Visibility.Collapsed;
        }

        private void BtnCerrarPopupMovimientoMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnMovimientoMensualCancelar_Click(sender, e);
        }

        private void PopupMovimientoMensual_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            BtnMovimientoMensualCancelar_Click(sender, e);
        }

        private void PopupMovimientoMensual_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnMovimientoMensualGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (_clienteMensualSeleccionado == null)
            {
                MessageBox.Show("Seleccione un cliente.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(TxtMovimientoImporte.Text, out decimal importe))
            {
                MessageBox.Show("Importe inválido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mov = new MovimientoMensual
            {
                ClienteId = _clienteMensualSeleccionado.Id,
                Fecha = DateTime.Now,
                Descripcion = TxtMovimientoDetalle.Text?.Trim() ?? string.Empty,
                Tipo = "Cargo",
                Importe = importe,
                AdminId = _currentAdminId ?? 0
            };

            // Validar forma de pago para pagos
            int? formaPagoId = null;
            bool esRecibo = false;
            if (_tipoMovimientoMensual == "Pago" || _tipoMovimientoMensual == "PagoAdelantado")
            {
                if (ListBoxFormasPagoMensual?.SelectedItem is FormaPago formaPago)
                {
                    formaPagoId = formaPago.Id;
                }
                else
                {
                    MessageBox.Show("Seleccione una forma de pago.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // No cerrar el popup
                    return;
                }
                
                esRecibo = RbMovimientoRecibo?.IsChecked == true;
            }

            switch (_tipoMovimientoMensual)
            {
                case "Cargo":
                    mov.Tipo = "Cargo";
                    mov.Importe = -Math.Abs(importe);
                    break;
                case "Pago":
                    mov.Tipo = "Pago";
                    mov.Importe = Math.Abs(importe);
                    mov.FormaPagoId = formaPagoId;
                    mov.EsRecibo = esRecibo;
                    break;
                case "PagoAdelantado":
                    mov.Tipo = "PagoAdelantado";
                    mov.Importe = Math.Abs(importe);
                    mov.FormaPagoId = formaPagoId;
                    mov.EsRecibo = esRecibo;
                    // Usar el mes guardado del popup de pago adelantado
                    if (!string.IsNullOrEmpty(_mesPagoAdelantado))
                    {
                        // Convertir formato MM/yyyy a yyyy-MM
                        var partes = _mesPagoAdelantado.Split('/');
                        if (partes.Length == 2 && int.TryParse(partes[0], out int mes) && int.TryParse(partes[1], out int año))
                        {
                            mov.MesAplicado = $"{año}-{mes:D2}";
                        }
                    }
                    break;
                case "Ajuste":
                    mov.Tipo = "Ajuste";
                    mov.Importe = importe;
                    break;
            }

            try
            {
                int movimientoId = _dbService.CrearMovimientoMensual(mov);
                mov.Id = movimientoId; // Asignar el ID devuelto
                
                // Si es un pago adelantado, actualizar el ProximoCargo de todos los vehículos
                if (_tipoMovimientoMensual == "PagoAdelantado" && !string.IsNullOrEmpty(_mesPagoAdelantado))
                {
                    // Convertir formato MM/yyyy a DateTime
                    var partes = _mesPagoAdelantado.Split('/');
                    if (partes.Length == 2 && int.TryParse(partes[0], out int mes) && int.TryParse(partes[1], out int año))
                    {
                        // El próximo cargo será el mes siguiente al seleccionado
                        var mesSeleccionado = new DateTime(año, mes, 1);
                        var proximoCargo = mesSeleccionado.AddMonths(1);
                        
                        // Obtener todos los vehículos del cliente y actualizar su ProximoCargo
                        var vehiculos = _dbService.ObtenerVehiculosMensuales(_clienteMensualSeleccionado.Id);
                        foreach (var vehiculo in vehiculos)
                        {
                            vehiculo.ProximoCargo = proximoCargo;
                            _dbService.ActualizarVehiculoMensual(vehiculo);
                        }
                    }
                }
                
                // Imprimir recibo si está seleccionado
                if (esRecibo && (_tipoMovimientoMensual == "Pago" || _tipoMovimientoMensual == "PagoAdelantado"))
                {
                    var formaPagoNombre = _formasPago.FirstOrDefault(fp => fp.Id == formaPagoId)?.Nombre ?? "N/A";
                    ImprimirReciboMensual(mov, formaPagoNombre);
                }
                
                PopupMovimientoMensual.IsOpen = false;
                PopupMovimientoMensual.Visibility = Visibility.Collapsed;
                
                // Limpiar el mes de pago adelantado después de guardar
                _mesPagoAdelantado = null;
                _importePagoAdelantado = 0m;
                
                CargarMensuales();
                if (_clienteMensualSeleccionado != null)
                {
                    var cliente = _clientesMensuales.FirstOrDefault(c => c.Id == _clienteMensualSeleccionado.Id);
                    if (cliente != null)
                    {
                        MostrarDetalleMensual(cliente);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar movimiento: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Handlers para PopupClienteMensual (alias de PopupNuevoClienteMensual)
        private void PopupClienteMensual_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            BtnCancelarNuevoClienteMensual_Click(sender, e);
        }

        private void PopupClienteMensual_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCerrarPopupClienteMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelarNuevoClienteMensual_Click(sender, e);
        }

        private void BtnCancelarClienteMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelarNuevoClienteMensual_Click(sender, e);
        }

        private void BtnGuardarClienteMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnGuardarNuevoClienteMensual_Click(sender, e);
        }

        // Handlers para PopupVehiculoMensual (alias de PopupNuevoVehiculoMensual)
        private void PopupVehiculoMensual_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            BtnCancelarNuevoVehiculoMensual_Click(sender, e);
        }

        private void PopupVehiculoMensual_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCerrarPopupVehiculoMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelarNuevoVehiculoMensual_Click(sender, e);
        }

        private void BtnCancelarVehiculoMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelarNuevoVehiculoMensual_Click(sender, e);
        }

        private void BtnGuardarVehiculoMensual_Click(object sender, RoutedEventArgs e)
        {
            BtnGuardarNuevoVehiculoMensual_Click(sender, e);
        }

        // Handlers para PopupDetalleClienteMensual
        private void PopupDetalleClienteMensual_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            BtnCerrarPopupDetalleClienteMensual_Click(sender, e);
        }

        private void PopupDetalleClienteMensual_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCerrarPopupDetalleClienteMensual_Click(object sender, RoutedEventArgs e)
        {
            PopupDetalleClienteMensual.IsOpen = false;
            PopupDetalleClienteMensual.Visibility = Visibility.Collapsed;
        }

        private void PopupCrearTicket_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers == ModifierKeys.None || Keyboard.Modifiers == ModifierKeys.Shift))
            {
                BtnCrearTicketConfirmar_Click(sender, e);
                e.Handled = true;
            }
        }

        private void BtnReportes_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenu();
            BtnReportes.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnReportes.Foreground = System.Windows.Media.Brushes.White;
            // TODO: Mostrar reportes
        }

        private void BtnInformes_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenu();
            BtnInformes.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnInformes.Foreground = System.Windows.Media.Brushes.White;
            // TODO: Mostrar informes
        }

        private void BtnEstadisticas_Click(object sender, RoutedEventArgs e)
        {
            ResetearBotonesSubmenu();
            BtnEstadisticas.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
            BtnEstadisticas.Foreground = System.Windows.Media.Brushes.White;
            // TODO: Mostrar estadísticas
        }

        private void ResetearBotonesSubmenu()
        {
            var transparente = System.Windows.Media.Brushes.Transparent;
            var gris = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));

            BtnTicketsAbiertos.Background = transparente;
            BtnTicketsAbiertos.Foreground = gris;
            BtnTicketsCerrados.Background = transparente;
            BtnTicketsCerrados.Foreground = gris;
            BtnEntradasSalidas.Background = transparente;
            BtnEntradasSalidas.Foreground = gris;
            BtnReportes.Background = transparente;
            BtnReportes.Foreground = gris;
            BtnInformes.Background = transparente;
            BtnInformes.Foreground = gris;
            BtnEstadisticas.Background = transparente;
            BtnEstadisticas.Foreground = gris;

            // Aplicar tema si está en modo oscuro
            if (_modoOscuro)
            {
                AplicarTemaBotonesSubmenu();
            }
        }

        private void PopupCrearTicket_BackdropClick(object sender, MouseButtonEventArgs e)
        {
            if (PopupCrearTicket != null)
                PopupCrearTicket.IsOpen = false;
        }

        private void PopupCrearTicket_ContentClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // evitar que se cierre
        }

        private void BtnCerrarPopupCrearTicket_Click(object sender, RoutedEventArgs e)
        {
            if (PopupCrearTicket != null)
                PopupCrearTicket.IsOpen = false;
        }

        private void TxtCrearTicketMatricula_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtCrearTicketMatricula == null) return;
            var largo = TxtCrearTicketMatricula.Text.Length;
            TxtCrearTicketMatricula.Foreground = largo >= 7 ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 102, 204)) : System.Windows.Media.Brushes.Black;
        }

        private void ChkIngresoPrevio_Checked(object sender, RoutedEventArgs e)
        {
            if (PanelIngresoPrevio != null)
                PanelIngresoPrevio.Visibility = Visibility.Visible;
        }

        private void ChkIngresoPrevio_Unchecked(object sender, RoutedEventArgs e)
        {
            if (PanelIngresoPrevio != null)
                PanelIngresoPrevio.Visibility = Visibility.Collapsed;
        }

        private void BtnCrearTicketConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (_tarifaSeleccionada == null || _categoriaSeleccionada == null)
            {
                MessageBox.Show("Falta seleccionar tarifa y categoría.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var matricula = (TxtCrearTicketMatricula?.Text?.Trim() ?? string.Empty).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(matricula))
            {
                MessageBox.Show("La matrícula es obligatoria.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var descripcion = TxtCrearTicketDescripcion?.Text?.Trim() ?? string.Empty;

            DateTime fechaEntrada = DateTime.Now;
            if (ChkIngresoPrevio != null && ChkIngresoPrevio.IsChecked == true)
            {
                var fechaSel = DpIngresoPrevio?.SelectedDate ?? DateTime.Now.Date;
                int hora = 0;
                int minuto = 0;
                if (CmbHoraIngreso != null && CmbHoraIngreso.SelectedItem != null)
                    int.TryParse(CmbHoraIngreso.SelectedItem.ToString(), out hora);
                if (CmbMinutoIngreso != null && CmbMinutoIngreso.SelectedItem != null)
                    int.TryParse(CmbMinutoIngreso.SelectedItem.ToString(), out minuto);
                fechaEntrada = fechaSel.Date.AddHours(hora).AddMinutes(minuto);
            }

            try
            {
                var ticket = new Ticket
                {
                    Matricula = matricula,
                    Descripcion = descripcion,
                    NotaAdicional = string.Empty,
                    TarifaId = _tarifaSeleccionada?.Id,
                    CategoriaId = _categoriaSeleccionada?.Id,
                    TarifaNombre = _tarifaSeleccionada?.Nombre ?? string.Empty,
                    CategoriaNombre = _categoriaSeleccionada?.Nombre ?? string.Empty,
                    FechaEntrada = fechaEntrada,
                    EstaAbierto = true,
                    FechaCreacion = DateTime.Now,
                    AdminCreadorId = _currentAdminId
                };

                var ticketId = _dbService.CrearTicket(ticket);
                ticket.Id = ticketId;
                CargarTicketsAbiertos();

                ImprimirTicket(ticketId, ticket, _tarifaSeleccionada, _categoriaSeleccionada);

                if (PopupCrearTicket != null)
                    PopupCrearTicket.IsOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear el ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarTicketsAbiertos()
        {
            PanelTicketsAbiertos.Visibility = Visibility.Visible;
            PanelTicketsCerrados.Visibility = Visibility.Collapsed;
        }

        private void TxtFiltroMatricula_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Asegurar que los controles estén inicializados
            if (PlaceholderMatricula != null)
            {
                ActualizarPlaceholder();
            }
            if (_tickets != null)
            {
                AplicarFiltros();
            }
        }

        private void CmbTodos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AplicarFiltros();
        }

        private void TxtFiltroMatricula_GotFocus(object sender, RoutedEventArgs e)
        {
            ActualizarPlaceholder();
        }

        private void TxtFiltroMatricula_LostFocus(object sender, RoutedEventArgs e)
        {
            ActualizarPlaceholder();
        }

        private void ActualizarPlaceholder()
        {
            PlaceholderMatricula.Visibility = string.IsNullOrWhiteSpace(TxtFiltroMatricula.Text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private void ChkSoloDeuda_Checked(object sender, RoutedEventArgs e)
        {
            if (_tickets != null)
            {
                AplicarFiltros();
            }
        }

        private void ChkSoloDeuda_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_tickets != null)
            {
                AplicarFiltros();
            }
        }

        private void BtnMenuHamburguesa_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementar menú lateral
        }

        private void CmbOrden_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbOrden.SelectedIndex >= 0 && _tickets != null)
            {
                AplicarFiltros();
            }
        }

        private void BtnDescargar_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implementar descarga de datos
        }

        private void PanelUsuario_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Abrir/cerrar el menú desplegable flotante
            PopupMenuUsuario.IsOpen = !PopupMenuUsuario.IsOpen;
            e.Handled = true;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // El Popup se cierra automáticamente con StaysOpen="False"
        }

        private void BtnMenuUsuario_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
            }
        }

        private void BtnMenuUsuario_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button btn)
            {
                btn.Background = System.Windows.Media.Brushes.Transparent;
            }
        }

        private void BtnCerrarSesion_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BtnMenuUsuario_MouseEnter(sender, e);
        }

        private void BtnCerrarSesion_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            BtnMenuUsuario_MouseLeave(sender, e);
        }

        private void BtnCambiarPassword_Click(object sender, RoutedEventArgs e)
        {
            PopupMenuUsuario.IsOpen = false;
            TxtPasswordActual.Password = string.Empty;
            TxtPasswordNueva.Password = string.Empty;
            TxtPasswordNuevaConfirm.Password = string.Empty;
            PopupCambiarPassword.Visibility = Visibility.Visible;
            PopupCambiarPassword.IsOpen = true;
            TxtPasswordActual.Focus();
        }

        private void BtnCerrarPopupCambiarPassword_Click(object sender, RoutedEventArgs e)
        {
            PopupCambiarPassword.IsOpen = false;
            PopupCambiarPassword.Visibility = Visibility.Collapsed;
        }

        private void PopupCambiarPassword_BackdropClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BtnCerrarPopupCambiarPassword_Click(sender, e);
        }

        private void PopupCambiarPassword_ContentClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCancelarCambiarPassword_Click(object sender, RoutedEventArgs e)
        {
            BtnCerrarPopupCambiarPassword_Click(sender, e);
        }

        private void BtnGuardarCambiarPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentAdminId.HasValue)
            {
                var adminActual = _dbService.ObtenerAdminPorUsername(_username);
                _currentAdminId = adminActual?.Id;
            }

            if (!_currentAdminId.HasValue)
            {
                MessageBox.Show("No se pudo identificar al usuario actual.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var actual = TxtPasswordActual.Password;
            var nueva = TxtPasswordNueva.Password;
            var confirm = TxtPasswordNuevaConfirm.Password;

            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(nueva) || string.IsNullOrWhiteSpace(confirm))
            {
                MessageBox.Show("Complete todos los campos.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (nueva != confirm)
            {
                MessageBox.Show("Las contraseñas no coinciden.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_dbService.ValidarPassword(_currentAdminId.Value, actual))
            {
                MessageBox.Show("La contraseña actual no es correcta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtPasswordActual.Focus();
                return;
            }

            try
            {
                _dbService.ActualizarPasswordAdmin(_currentAdminId.Value, nueva);
                MessageBox.Show("Contraseña actualizada correctamente.", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnCerrarPopupCambiarPassword_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar la contraseña: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCambiarEmail_Click(object sender, RoutedEventArgs e)
        {
            PopupMenuUsuario.IsOpen = false;
            TxtEmailNuevo.Text = string.Empty;
            TxtEmailNuevoConfirm.Text = string.Empty;
            TxtEmailPassword.Password = string.Empty;
            PopupCambiarEmail.Visibility = Visibility.Visible;
            PopupCambiarEmail.IsOpen = true;
            TxtEmailNuevo.Focus();
        }

        private void BtnCerrarPopupCambiarEmail_Click(object sender, RoutedEventArgs e)
        {
            PopupCambiarEmail.IsOpen = false;
            PopupCambiarEmail.Visibility = Visibility.Collapsed;
        }

        private void PopupCambiarEmail_BackdropClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BtnCerrarPopupCambiarEmail_Click(sender, e);
        }

        private void PopupCambiarEmail_ContentClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCancelarCambiarEmail_Click(object sender, RoutedEventArgs e)
        {
            BtnCerrarPopupCambiarEmail_Click(sender, e);
        }

        private void BtnGuardarCambiarEmail_Click(object sender, RoutedEventArgs e)
        {
            if (!_currentAdminId.HasValue)
            {
                var adminActual = _dbService.ObtenerAdminPorUsername(_username);
                _currentAdminId = adminActual?.Id;
            }

            if (!_currentAdminId.HasValue)
            {
                MessageBox.Show("No se pudo identificar al usuario actual.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var email = TxtEmailNuevo.Text?.Trim() ?? string.Empty;
            var emailConfirm = TxtEmailNuevoConfirm.Text?.Trim() ?? string.Empty;
            var password = TxtEmailPassword.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(emailConfirm) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Complete todos los campos.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!email.Equals(emailConfirm, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Los emails no coinciden.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!email.Contains("@") || !email.Contains("."))
            {
                MessageBox.Show("Ingrese un email válido.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_dbService.ValidarPassword(_currentAdminId.Value, password))
            {
                MessageBox.Show("La contraseña actual no es correcta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtEmailPassword.Focus();
                return;
            }

            try
            {
                _dbService.ActualizarEmailAdmin(_currentAdminId.Value, email);
                MessageBox.Show("Email de recuperación actualizado.", "Listo", MessageBoxButton.OK, MessageBoxImage.Information);
                BtnCerrarPopupCambiarEmail_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar el email: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            PopupMenuUsuario.IsOpen = false;
            
            // Cerrar esta ventana y volver al login
            var mainWindow = this.Owner as MainWindow;
            this.Close();
            
            if (mainWindow != null)
            {
                mainWindow.Show();
                // Reutilizar el flujo estándar de login/inicio
                mainWindow.CheckInitialSetup();
            }
        }

        private void BorderTicket_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border)
            {
                if (border.DataContext is Ticket ticket)
                {
                    ActualizarCardTicket(border, ticket);
                }
                AplicarTemaTicket(border);
            }
        }

        private void ActualizarCardTicket(Border border, Ticket ticket)
        {
            if (border == null || ticket == null) return;

            var txtMatricula = FindVisualChild<TextBlock>(border, "TxtMatricula");
            if (txtMatricula != null) txtMatricula.Text = FormatearMatricula(ticket.Matricula);

            var txtTiempo = FindVisualChild<TextBlock>(border, "TxtTiempo");
            var txtTiempoValor = FindVisualChild<TextBlock>(border, "TxtTiempoValor");
            string tiempo = ObtenerTiempoTranscurrido(ticket.FechaEntrada);
            if (txtTiempo != null) txtTiempo.Text = tiempo;
            if (txtTiempoValor != null) txtTiempoValor.Text = tiempo;

            var txtEntrada = FindVisualChild<TextBlock>(border, "TxtEntrada");
            if (txtEntrada != null) txtEntrada.Text = ticket.FechaEntrada.ToString("dd/MM HH:mm");

            // Mostrar 0 mientras esté dentro de la tolerancia (solo tickets abiertos).
            decimal montoCalculado = ticket.EstaAbierto
                ? CalcularImporteActual(ticket)
                : (ticket.Monto ?? CalcularImporteActual(ticket));
            string montoTxt = FormatearMoneda(montoCalculado);
            var txtImporte = FindVisualChild<TextBlock>(border, "TxtImporteActual");
            var txtDebe = FindVisualChild<TextBlock>(border, "TxtDebe");
            if (txtImporte != null) txtImporte.Text = montoTxt;
            if (txtDebe != null) txtDebe.Text = montoTxt;

            // Ocultar/mostrar nota y acciones según cancelado
            var panelNota = FindVisualChild<StackPanel>(border, "PanelNota");
            var panelAcciones = FindVisualChild<Grid>(border, "PanelAcciones");
            var panelCerrar = FindVisualChild<Border>(border, "PanelCerrar");
            if (ticket.EstaCancelado)
            {
                if (panelNota != null) panelNota.Visibility = Visibility.Collapsed;
                if (panelAcciones != null) panelAcciones.Visibility = Visibility.Collapsed;
            }
            else
            {
                if (panelNota != null) panelNota.Visibility = Visibility.Visible;
                if (panelAcciones != null) panelAcciones.Visibility = Visibility.Visible;
            }
            // El panel cerrar se mantiene siempre
        }

        private string FormatearMatricula(string matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula)) return string.Empty;
            var limpio = matricula.Replace(" ", "").Replace("-", "").Replace(".", "").ToUpperInvariant();
            if (limpio.Length == 7)
                return $"{limpio.Substring(0, 2)}·{limpio.Substring(2, 3)}·{limpio.Substring(5, 2)}";
            if (limpio.Length == 6)
                return $"{limpio.Substring(0, 3)}·{limpio.Substring(3, 3)}";
            return limpio;
        }

        private string ObtenerTiempoTranscurrido(DateTime fechaEntrada)
        {
            var diff = DateTime.Now - fechaEntrada;
            if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;
            return $"{(int)diff.TotalHours:00}:{diff.Minutes:00}";
        }

        private string FormatearMoneda(decimal? monto)
        {
            var valor = monto ?? 0m;
            return valor.ToString("$#,0.00");
        }

        private string FormatearDuracion(TimeSpan diff)
        {
            if (diff.TotalSeconds < 0) diff = TimeSpan.Zero;
            int dias = diff.Days;
            int horas = diff.Hours;
            int minutos = diff.Minutes;
            if (dias > 0)
                return $"{dias}d {horas:00}h{minutos:00}m";
            return $"{horas}h{minutos:00}m";
        }

        private string NormalizarMatricula(string? matricula)
        {
            if (string.IsNullOrWhiteSpace(matricula)) return string.Empty;
            return new string(matricula.ToUpperInvariant()
                .Where(char.IsLetterOrDigit)
                .ToArray());
        }

        private decimal CalcularImporteActual(Ticket ticket)
        {
            if (ticket == null || !ticket.TarifaId.HasValue || !ticket.CategoriaId.HasValue)
                return ticket.Monto ?? 0m;

            // Obtener tarifa y precio
            var tarifa = _tarifas.FirstOrDefault(t => t.Id == ticket.TarifaId.Value) ?? _dbService.ObtenerTarifaPorId(ticket.TarifaId.Value);
            if (tarifa == null) return ticket.Monto ?? 0m;

            var precio = _dbService.ObtenerPrecio(tarifa.Id, ticket.CategoriaId.Value);
            if (precio == null) return ticket.Monto ?? 0m;

            int slotMinutos = tarifa.Dias * 1440 + tarifa.Horas * 60 + tarifa.Minutos;
            if (slotMinutos <= 0) slotMinutos = 60; // fallback 1 hora

            int tolerancia = tarifa.Tolerancia;

            var referencia = ticket.FechaSalida ?? DateTime.Now;
            var diff = referencia - ticket.FechaEntrada;
            if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;

            // Si está dentro de la tolerancia, no se cobra nada
            if (diff.TotalMinutes <= tolerancia)
                return 0m;

            double minutosEfectivos = diff.TotalMinutes - tolerancia;
            int unidades = (int)Math.Ceiling(minutosEfectivos / slotMinutos);

            return unidades * precio.Monto;
        }

        private decimal CalcularImporteConDetalle(Ticket ticket, Tarifa tarifa, Precio precio, out List<string> itemsDetalle)
        {
            itemsDetalle = new List<string>();

            int slotMinutos = tarifa.Dias * 1440 + tarifa.Horas * 60 + tarifa.Minutos;
            if (slotMinutos <= 0) slotMinutos = 60;

            int tolerancia = tarifa.Tolerancia;

            var referencia = DateTime.Now;
            var diff = referencia - ticket.FechaEntrada;
            if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;

            if (diff.TotalMinutes <= tolerancia)
            {
                itemsDetalle.Add($"Dentro de tolerancia ({tolerancia} min)");
                return 0m;
            }

            double minutosEfectivos = diff.TotalMinutes - tolerancia;
            int unidades = (int)Math.Ceiling(minutosEfectivos / slotMinutos);
            if (unidades < 1) unidades = 1;

            decimal subtotal = unidades * precio.Monto;
            itemsDetalle.Add($"{unidades} x {tarifa.Nombre} (${precio.Monto:#,0.##}) = {subtotal.ToString("$#,0.00")}");

            return subtotal;
        }

        private void MostrarPopupCobrarTicket(Ticket ticket)
        {
            _ticketCobrar = ticket;

            if (ticket.TarifaId == null || ticket.CategoriaId == null)
            {
                MessageBox.Show("El ticket no tiene tarifa o categoría asociada.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var tarifa = _tarifas.FirstOrDefault(t => t.Id == ticket.TarifaId.Value) ?? _dbService.ObtenerTarifaPorId(ticket.TarifaId.Value);
            if (tarifa == null)
            {
                MessageBox.Show("No se encontró la tarifa del ticket.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var precio = _dbService.ObtenerPrecio(tarifa.Id, ticket.CategoriaId.Value);
            if (precio == null)
            {
                MessageBox.Show("No hay precio configurado para la tarifa y categoría.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _importeCobrar = CalcularImporteConDetalle(ticket, tarifa, precio, out var items);
            _itemsCobrarDetalle = items;

            TxtCobrarImporte.Text = FormatearMoneda(_importeCobrar);
            var diff = DateTime.Now - ticket.FechaEntrada;
            if (diff.TotalMinutes < 0) diff = TimeSpan.Zero;
            TxtCobrarPermanencia.Text = $"Inicio: {ticket.FechaEntrada:dd/MM HH:mm} • Permanencia: {FormatearDuracion(diff)}";

            TxtCobrarDescripcion.Text = string.IsNullOrWhiteSpace(ticket.Descripcion)
                ? $"Ticket #{ticket.Id} {FormatearMatricula(ticket.Matricula)}"
                : ticket.Descripcion;

            ItemsCobrarDetalle.ItemsSource = _itemsCobrarDetalle;

            TxtCobrarResumen.Text = $"{FormatearMatricula(ticket.Matricula)} • {ticket.CategoriaNombre} • {tarifa.Nombre}";

            ListBoxFormasPago.ItemsSource = _formasPago;
            if (_formasPago.Count > 0)
            {
                ListBoxFormasPago.SelectedIndex = 0;
                // Actualizar método de pago en pantalla cliente
                var modulosActivos = _dbService.ObtenerModulos();
                if (modulosActivos.ContainsKey("MONITOR") && modulosActivos["MONITOR"] && ListBoxFormasPago.SelectedItem is FormaPago fp)
                {
                    _metodoPagoPantallaCliente = fp.Nombre;
                }
            }
            
            // Remover eventos anteriores y agregar nuevo evento para actualizar método de pago cuando cambie
            ListBoxFormasPago.SelectionChanged -= ListBoxFormasPago_SelectionChanged;
            ListBoxFormasPago.SelectionChanged += ListBoxFormasPago_SelectionChanged;

            RbSinComprobante.IsChecked = true;
            RbRecibo.IsChecked = false;
            ChkSalidaInmediata.IsChecked = false;

            bool esGratis = _importeCobrar <= 0m;
            GridCobrarFormasPago.IsEnabled = !esGratis;
            GridCobrarFormasPago.Opacity = esGratis ? 0.4 : 1.0;
            RbSinComprobante.IsEnabled = !esGratis;
            RbRecibo.IsEnabled = !esGratis;
            ListBoxFormasPago.IsEnabled = !esGratis;

            PopupCobrarTicket.Visibility = Visibility.Visible;
            PopupCobrarTicket.IsOpen = true;
            
            // Actualizar pantalla cliente si el módulo está activo
            var modulos = _dbService.ObtenerModulos();
            if (modulos.ContainsKey("MONITOR") && modulos["MONITOR"])
            {
                _estadoPantallaCliente = "cobro";
                _matriculaPantallaCliente = ticket.Matricula;
                _importePantallaCliente = _importeCobrar;
                // El método de pago se actualizará cuando se seleccione
            }
        }

        private void CerrarPopupCobrar()
        {
            PopupCobrarTicket.IsOpen = false;
            PopupCobrarTicket.Visibility = Visibility.Collapsed;
            _ticketCobrar = null;
            _itemsCobrarDetalle.Clear();
            _importeCobrar = 0m;
            
            // Limpiar todos los datos de la pantalla del cliente
            var modulosCerrar = _dbService.ObtenerModulos();
            if (modulosCerrar.ContainsKey("MONITOR") && modulosCerrar["MONITOR"])
            {
                if (_estadoPantallaCliente == "cobro" || _estadoPantallaCliente == "cobro_reload")
                {
                    _estadoPantallaCliente = "bienvenida";
                    _matriculaPantallaCliente = "";
                    _importePantallaCliente = 0m;
                    _metodoPagoPantallaCliente = "";
                    _qrMercadoPago = ""; // Limpiar QR al cerrar
                }
            }
        }

        private void PopupCobrarTicket_BackdropClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // No cerrar el popup al hacer click afuera
            // Solo se cierra con Cancelar, X o al confirmar el cobro
            e.Handled = true;
        }

        private void PopupCobrarTicket_ContentClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCerrarPopupCobrarTicket_Click(object sender, RoutedEventArgs e)
        {
            CerrarPopupCobrar();
        }

        private void BtnCobrarCancelar_Click(object sender, RoutedEventArgs e)
        {
            CerrarPopupCobrar();
        }

        private void ListBoxFormasPago_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBoxFormasPago == null) return;
            
            var modulosActivos = _dbService.ObtenerModulos();
            if (modulosActivos.ContainsKey("MONITOR") && modulosActivos["MONITOR"] && ListBoxFormasPago.SelectedItem is FormaPago formaPago)
            {
                _metodoPagoPantallaCliente = formaPago.Nombre ?? "";
                
                // Si se selecciona MercadoPago, generar QR
                if (formaPago.Nombre != null && formaPago.Nombre.Equals("MercadoPago", StringComparison.OrdinalIgnoreCase))
                {
                    _ = GenerarQRMercadoPagoAsync();
                }
                else
                {
                    // Limpiar QR si se cambia a otro método de pago
                    _qrMercadoPago = "";
                }
            }
        }

        private async Task GenerarQRMercadoPagoAsync()
        {
            try
            {
                // Verificar que el módulo esté habilitado
                var modulos = _dbService.ObtenerModulos();
                if (!modulos.ContainsKey("MERCADOPAGO") || !modulos["MERCADOPAGO"])
                {
                    _qrMercadoPago = "";
                    return;
                }

                // Obtener credenciales de la base de datos
                var credenciales = _dbService.ObtenerCredencialesMercadoPago();
                string accessToken = credenciales["AccessToken"] ?? "";

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    _qrMercadoPago = "";
                    System.Diagnostics.Debug.WriteLine("No hay credenciales de Mercado Pago configuradas");
                    return;
                }

                if (_importeCobrar <= 0)
                {
                    _qrMercadoPago = "";
                    return;
                }

                // Limpiar QR mientras se genera
                _qrMercadoPago = "";
                string externalPosId = "LOJ001POS001";
                string idempotencyKey = Guid.NewGuid().ToString();
                
                // Formatear importe
                string totalAmount = _importeCobrar.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                string amount = _importeCobrar.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                
                // Crear referencia externa única
                string externalReference = $"TICKET_{_ticketCobrar?.Id ?? 0}_{DateTime.Now:yyyyMMddHHmmss}";
                
                // Descripción base
                string description = $"Estacionamiento - {_matriculaPantallaCliente}";
                
                // Parsear items del detalle para crear los items de MercadoPago
                var itemsList = new List<object>();
                
                if (_itemsCobrarDetalle != null && _itemsCobrarDetalle.Count > 0)
                {
                    foreach (var itemDetalle in _itemsCobrarDetalle)
                    {
                        // Formato esperado: "2 x Tarifa ($100.00) = $200.00"
                        // O: "Dentro de tolerancia (15 min)"
                        
                        if (itemDetalle.Contains("Dentro de tolerancia"))
                        {
                            // Saltar items de tolerancia
                            continue;
                        }
                        
                        // Intentar parsear el formato: "cantidad x nombre ($precio) = total"
                        // Ejemplos: "2 x Tarifa ($100.00) = $200.00" o "2 x Tarifa ($1,000.00) = $2,000.00"
                        var match = System.Text.RegularExpressions.Regex.Match(itemDetalle, @"(\d+)\s+x\s+(.+?)\s+\(\$([\d,]+\.?\d*)\)\s*=\s*\$([\d,]+\.?\d*)");
                        
                        if (match.Success)
                        {
                            try
                            {
                                int quantity = int.Parse(match.Groups[1].Value);
                                string title = match.Groups[2].Value.Trim();
                                string unitPriceStr = match.Groups[3].Value.Replace(",", "").Trim();
                                decimal unitPrice = decimal.Parse(unitPriceStr, System.Globalization.CultureInfo.InvariantCulture);
                                
                                itemsList.Add(new
                                {
                                    title = title,
                                    unit_price = unitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                                    quantity = quantity,
                                    unit_measure = "unit",
                                    external_code = $"ITEM_{_ticketCobrar?.Id ?? 0}_{itemsList.Count + 1}"
                                });
                                
                                System.Diagnostics.Debug.WriteLine($"Item parseado: {quantity} x {title} @ ${unitPrice:F2}");
                            }
                            catch (Exception parseEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error al parsear item '{itemDetalle}': {parseEx.Message}");
                                // Si falla el parseo, crear un item genérico
                                itemsList.Add(new
                                {
                                    title = itemDetalle,
                                    unit_price = amount,
                                    quantity = 1,
                                    unit_measure = "unit",
                                    external_code = $"ITEM_{_ticketCobrar?.Id ?? 0}_{itemsList.Count + 1}"
                                });
                            }
                        }
                        else
                        {
                            // Si no se puede parsear, crear un item genérico con el total
                            System.Diagnostics.Debug.WriteLine($"No se pudo parsear item: '{itemDetalle}', usando item genérico");
                            itemsList.Add(new
                            {
                                title = itemDetalle,
                                unit_price = amount,
                                quantity = 1,
                                unit_measure = "unit",
                                external_code = $"ITEM_{_ticketCobrar?.Id ?? 0}_{itemsList.Count + 1}"
                            });
                        }
                    }
                }
                
                // Si no hay items parseados, crear uno genérico
                if (itemsList.Count == 0)
                {
                    itemsList.Add(new
                    {
                        title = description,
                        unit_price = amount,
                        quantity = 1,
                        unit_measure = "unit",
                        external_code = $"TICKET_{_ticketCobrar?.Id ?? 0}"
                    });
                }
                
                var requestBody = new
                {
                    type = "qr",
                    total_amount = totalAmount,
                    description = description,
                    external_reference = externalReference,
                    expiration_time = "PT16M",
                    config = new
                    {
                        qr = new
                        {
                            external_pos_id = externalPosId,
                            mode = "dynamic"
                        }
                    },
                    transactions = new
                    {
                        payments = new[]
                        {
                            new
                            {
                                amount = amount
                            }
                        }
                    },
                    items = itemsList.ToArray()
                };

                string jsonBody = System.Text.Json.JsonSerializer.Serialize(requestBody);

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                    httpClient.DefaultRequestHeaders.Add("X-Idempotency-Key", idempotencyKey);

                    var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                    
                    // Esperar la respuesta
                    var response = await httpClient.PostAsync("https://api.mercadopago.com/v1/orders", content);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        string responseContent = await response.Content.ReadAsStringAsync();
                        var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                        
                        if (jsonDoc.RootElement.TryGetProperty("type_response", out var typeResponse) &&
                            typeResponse.TryGetProperty("qr_data", out var qrData))
                        {
                            _qrMercadoPago = qrData.GetString() ?? "";
                            System.Diagnostics.Debug.WriteLine($"QR de MercadoPago generado correctamente: {_qrMercadoPago.Substring(0, Math.Min(50, _qrMercadoPago.Length))}...");
                            
                            // El QR se actualizará automáticamente en el próximo polling (cada 200ms)
                            // El script JavaScript detectará el cambio y actualizará o recargará según corresponda
                        }
                        else
                        {
                            _qrMercadoPago = "";
                            System.Diagnostics.Debug.WriteLine($"Error: No se encontró qr_data en la respuesta de MercadoPago. Respuesta: {responseContent}");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        System.Diagnostics.Debug.WriteLine($"Error al generar QR de MercadoPago: {response.StatusCode} - {errorContent}");
                        _qrMercadoPago = "";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar QR de MercadoPago: {ex.Message}");
                _qrMercadoPago = "";
            }
        }

        private void MostrarPopupCerrarPorMatricula()
        {
            if (PopupCerrarMatricula == null) return;
            TxtCerrarMatricula.Text = string.Empty;
            PopupCerrarMatricula.Visibility = Visibility.Visible;
            PopupCerrarMatricula.IsOpen = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtCerrarMatricula.Focus();
                TxtCerrarMatricula.SelectAll();
            }), DispatcherPriority.Input);
        }

        private void CerrarPopupCerrarMatricula()
        {
            PopupCerrarMatricula.IsOpen = false;
            PopupCerrarMatricula.Visibility = Visibility.Collapsed;
        }

        private void PopupCerrarMatricula_BackdropClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            CerrarPopupCerrarMatricula();
        }

        private void PopupCerrarMatricula_ContentClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BtnCerrarPopupCerrarMatricula_Click(object sender, RoutedEventArgs e)
        {
            CerrarPopupCerrarMatricula();
        }

        private void BtnCerrarMatriculaCancelar_Click(object sender, RoutedEventArgs e)
        {
            CerrarPopupCerrarMatricula();
        }

        private void TxtCerrarMatricula_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BuscarYCobrarPorMatricula();
                e.Handled = true;
            }
        }

        private void TxtCerrarMatricula_TextChanged(object sender, TextChangedEventArgs e)
        {
            BuscarYCobrarPorMatricula(autoSilent: true);
        }

        private void BuscarYCobrarPorMatricula(bool autoSilent = false)
        {
            string mat = NormalizarMatricula(TxtCerrarMatricula.Text);
            if (string.IsNullOrWhiteSpace(mat))
            {
                if (!autoSilent)
                    MessageBox.Show("Ingrese una matrícula válida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var coincidencias = _tickets.Where(t =>
                t.EstaAbierto &&
                !t.EstaCancelado)
                .Select(t => new { Ticket = t, Mat = NormalizarMatricula(t.Matricula) })
                .Where(x => x.Mat.StartsWith(mat))
                .ToList();

            if (coincidencias.Count == 1)
            {
                var ticket = coincidencias[0].Ticket;
                CerrarPopupCerrarMatricula();
                MostrarPopupCobrarTicket(ticket);
                return;
            }

            if (!autoSilent)
            {
                if (coincidencias.Count == 0)
                {
                    MessageBox.Show("No se encontró un ticket abierto con esa matrícula.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Hay más de un ticket que coincide, escriba más caracteres.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnCobrarConfirmar_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketCobrar == null)
            {
                MessageBox.Show("No hay ticket seleccionado.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ListBoxFormasPago.SelectedItem is not FormaPago formaPago)
            {
                MessageBox.Show("Seleccione una forma de pago.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool conRecibo = RbRecibo.IsChecked == true;
            bool salidaInmediata = ChkSalidaInmediata.IsChecked == true;
            DateTime? fechaSalida = salidaInmediata ? DateTime.Now : (DateTime?)null;
            string descripcion = TxtCobrarDescripcion.Text?.Trim() ?? string.Empty;

            var tarifa = _tarifas.FirstOrDefault(t => t.Id == _ticketCobrar.TarifaId) ?? (_ticketCobrar.TarifaId.HasValue ? _dbService.ObtenerTarifaPorId(_ticketCobrar.TarifaId.Value) : null);
            var categoria = _categorias.FirstOrDefault(c => c.Id == _ticketCobrar.CategoriaId) ?? (_ticketCobrar.CategoriaId.HasValue ? _dbService.ObtenerCategorias().FirstOrDefault(c => c.Id == _ticketCobrar.CategoriaId.Value) : null);

            try
            {
                _dbService.CerrarTicket(_ticketCobrar.Id, _importeCobrar, _currentAdminId, fechaSalida, descripcion, formaPago.Id);

                var tx = new Transaccion
                {
                    TicketId = _ticketCobrar.Id,
                    AdminId = _currentAdminId,
                    FormaPagoId = formaPago.Id,
                    Importe = _importeCobrar,
                    Fecha = DateTime.Now,
                    Descripcion = descripcion,
                    ConRecibo = conRecibo,
                    EsSalidaInmediata = salidaInmediata,
                    ItemsDetalle = string.Join(" | ", _itemsCobrarDetalle)
                };
                _dbService.RegistrarTransaccion(tx);

                if (conRecibo && tarifa != null && categoria != null)
                {
                    ImprimirReciboCobro(_ticketCobrar, tarifa, categoria, formaPago.Nombre, fechaSalida ?? DateTime.Now, descripcion, _itemsCobrarDetalle, _importeCobrar);
                }

                // Actualizar pantalla cliente a agradecimiento
                var modulosAgradecimiento = _dbService.ObtenerModulos();
                if (modulosAgradecimiento.ContainsKey("MONITOR") && modulosAgradecimiento["MONITOR"])
                {
                    _estadoPantallaCliente = "agradecimiento";
                    _qrMercadoPago = ""; // Limpiar QR al confirmar pago
                    // Volver a bienvenida después de 5 segundos
                    Task.Delay(5000).ContinueWith(_ =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _estadoPantallaCliente = "bienvenida";
                        });
                    });
                }

                CerrarPopupCobrar();
                CargarTicketsAbiertos();
                CargarTicketsCerrados();
                AplicarFiltros();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cobrar el ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarTemaTicket(Border border)
        {
            if (border == null) return;
            
            if (_modoOscuro)
            {
                border.Background = ColorTicketOscuro;
                border.BorderBrush = ColorBordeOscuro;
                
                // Cambiar colores de textos
                var txtMatricula = FindVisualChild<TextBlock>(border, "TxtMatricula");
                if (txtMatricula != null) txtMatricula.Foreground = ColorTextoOscuro;
                
                var txtDescripcion = FindVisualChild<TextBlock>(border, "TxtDescripcion");
                if (txtDescripcion != null) txtDescripcion.Foreground = ColorTextoSecundarioOscuro;
                
                var txtFechaEntrada = FindVisualChild<TextBlock>(border, "TxtFechaEntrada");
                if (txtFechaEntrada != null) txtFechaEntrada.Foreground = ColorTextoSecundarioOscuro;
                
                // Cambiar borde de imagen
                var borderImagen = FindVisualChild<Border>(border, "BorderImagen");
                if (borderImagen != null)
                {
                    borderImagen.Background = ColorFondoOscuro;
                    borderImagen.BorderBrush = ColorBordeOscuro;
                }
                
                var txtSinImagen = FindVisualChild<TextBlock>(border, "TxtSinImagen");
                if (txtSinImagen != null) txtSinImagen.Foreground = ColorTextoSecundarioOscuro;
            }
            else
            {
                border.Background = ColorTicketClaro;
                border.BorderBrush = ColorBordeClaro;
                
                // Restaurar colores de textos
                var txtMatricula = FindVisualChild<TextBlock>(border, "TxtMatricula");
                if (txtMatricula != null) txtMatricula.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 138));
                
                var txtDescripcion = FindVisualChild<TextBlock>(border, "TxtDescripcion");
                if (txtDescripcion != null) txtDescripcion.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
                
                var txtFechaEntrada = FindVisualChild<TextBlock>(border, "TxtFechaEntrada");
                if (txtFechaEntrada != null) txtFechaEntrada.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184));
                
                // Restaurar borde de imagen
                var borderImagen = FindVisualChild<Border>(border, "BorderImagen");
                if (borderImagen != null)
                {
                    borderImagen.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(243, 244, 246));
                    borderImagen.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 213, 219));
                }
                
                var txtSinImagen = FindVisualChild<TextBlock>(border, "TxtSinImagen");
                if (txtSinImagen != null) txtSinImagen.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }
        }

        private void ImgTicket_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Image image && image.DataContext is Ticket ticket)
            {
                if (!string.IsNullOrEmpty(ticket.ImagenPath) && System.IO.File.Exists(ticket.ImagenPath))
                {
                    try
                    {
                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(ticket.ImagenPath, UriKind.Absolute);
                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        image.Source = bitmap;
                        image.Visibility = Visibility.Visible;
                        
                        // Ocultar texto "Sin imagen"
                        var parent = image.Parent as Grid;
                        if (parent != null)
                        {
                            foreach (var child in parent.Children)
                            {
                                if (child is TextBlock tb && tb.Name == "TxtSinImagen")
                                {
                                    tb.Visibility = Visibility.Collapsed;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        image.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    image.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void TxtNotaInline_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox txt && txt.DataContext is Ticket ticket)
            {
                string nota = txt.Text ?? string.Empty;
                ticket.NotaAdicional = nota;
                try
                {
                    _dbService.ActualizarNotaTicket(ticket.Id, nota);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo guardar la nota: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnEditarTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Ticket ticket)
            {
                _ticketEditando = ticket;

                if (TxtEditarMatricula != null) TxtEditarMatricula.Text = ticket.Matricula;
                if (TxtEditarDescripcion != null) TxtEditarDescripcion.Text = ticket.Descripcion;

                if (CmbEditarCategoria != null)
                {
                    CmbEditarCategoria.ItemsSource = _categorias;
                    CmbEditarCategoria.DisplayMemberPath = "Nombre";
                    CmbEditarCategoria.SelectedValuePath = "Id";
                    CmbEditarCategoria.SelectedValue = ticket.CategoriaId;
                }

                if (PopupEditarTicket != null)
                    PopupEditarTicket.IsOpen = true;
            }
        }

        private void BtnGuardarEdicionTicket_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketEditando == null) return;
            string matricula = TxtEditarMatricula?.Text?.Trim().ToUpperInvariant() ?? string.Empty;
            string descripcion = TxtEditarDescripcion?.Text?.Trim() ?? string.Empty;
            int? categoriaId = null;
            if (CmbEditarCategoria != null && CmbEditarCategoria.SelectedValue is int idCat)
            {
                categoriaId = idCat;
            }

            if (string.IsNullOrWhiteSpace(matricula))
            {
                MessageBox.Show("La matrícula es obligatoria.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _dbService.ActualizarTicket(_ticketEditando.Id, matricula, descripcion, categoriaId);
                _ticketEditando.Matricula = matricula;
                _ticketEditando.Descripcion = descripcion;
                _ticketEditando.CategoriaId = categoriaId;
                if (categoriaId.HasValue)
                {
                    var cat = _categorias.FirstOrDefault(c => c.Id == categoriaId.Value);
                    _ticketEditando.CategoriaNombre = cat?.Nombre ?? _ticketEditando.CategoriaNombre;
                }

                CargarTicketsAbiertos();
                if (PopupEditarTicket != null) PopupEditarTicket.IsOpen = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelarEdicionTicket_Click(object sender, RoutedEventArgs e)
        {
            if (PopupEditarTicket != null) PopupEditarTicket.IsOpen = false;
            _ticketEditando = null;
        }

        private void BtnReimprimirTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Ticket ticket)
            {
                Tarifa? tarifa = null;
                Categoria? categoria = null;

                if (ticket.TarifaId.HasValue)
                {
                    tarifa = _tarifas.FirstOrDefault(t => t.Id == ticket.TarifaId.Value) ?? _dbService.ObtenerTarifaPorId(ticket.TarifaId.Value);
                }
                if (ticket.CategoriaId.HasValue)
                {
                    categoria = _categorias.FirstOrDefault(c => c.Id == ticket.CategoriaId.Value) ?? _dbService.ObtenerCategoriaPorId(ticket.CategoriaId.Value);
                }

                ImprimirTicket(ticket.Id, ticket, tarifa, categoria);
            }
        }

        private void BtnCancelarTicket_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Ticket ticket)
            {
                _ticketCancelando = ticket;
                if (TxtCancelarTitulo != null) TxtCancelarTitulo.Text = $"Cancelación del Ticket #{FormatearMatricula(ticket.Matricula)}";
                if (TxtMotivoCancelacion != null) TxtMotivoCancelacion.Text = string.Empty;
                if (PopupCancelarTicket != null) PopupCancelarTicket.IsOpen = true;
            }
        }

        private void BtnConfirmarCancelacion_Click(object sender, RoutedEventArgs e)
        {
            if (_ticketCancelando == null) return;
            string motivo = TxtMotivoCancelacion?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(motivo))
            {
                MessageBox.Show("Debes indicar un motivo de cancelación.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _dbService.CancelarTicket(_ticketCancelando.Id, motivo, _currentAdminId);
                _ticketCancelando = null;
                if (PopupCancelarTicket != null) PopupCancelarTicket.IsOpen = false;
                CargarTicketsAbiertos();
                CargarTicketsCerrados();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo cancelar el ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCerrarPopupCancelar_Click(object sender, RoutedEventArgs e)
        {
            if (PopupCancelarTicket != null) PopupCancelarTicket.IsOpen = false;
            _ticketCancelando = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            _timerEstadisticas?.Stop();
            _timerTiempoTickets?.Stop();
            DetenerServidorPantallaCliente();
            base.OnClosed(e);
        }

        private void BtnModoOscuro_Click(object sender, RoutedEventArgs e)
        {
            _modoOscuro = !_modoOscuro;
            AplicarTema(_modoOscuro);
            // Recargar contadores y footer para aplicar nuevos colores
            CargarContadoresInicio();
            CargarFooter();
        }

        private void AplicarTema(bool modoOscuro)
        {
            if (modoOscuro)
            {
                // MODO OSCURO - TODO DEBE SER OSCURO
                AplicarModoOscuro();
            }
            else
            {
                // MODO CLARO
                AplicarModoClaro();
            }
        }

        private void AplicarModoOscuro()
        {
            // Fondo principal
            this.Background = ColorFondoOscuro;
            
            // Icono sol (blanco)
            // Botón modo oscuro removido; no aplicar

            // Mantener header principal con color original (sin modo oscuro)
            var azulHeader = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 138));
            if (HeaderPrincipal != null)
            {
                HeaderPrincipal.Background = azulHeader;
            }
            
            // Contenido principal
            var gridPrincipal = this.Content as Grid;
            if (gridPrincipal != null && gridPrincipal.Children.Count > 3)
            {
                var contenidoGrid = gridPrincipal.Children[3] as Grid;
                if (contenidoGrid != null)
                {
                    contenidoGrid.Background = ColorFondoOscuro;
                }
            }

            // Submenú
            if (PanelSubmenu != null)
            {
                PanelSubmenu.Background = ColorFondoSecundarioOscuro;
                PanelSubmenu.BorderBrush = ColorBordeOscuro;
                AplicarTemaBotonesSubmenu();
            }

            // Barra de filtros
            if (BarraFiltrosAbiertos != null)
            {
                BarraFiltrosAbiertos.Background = ColorFondoSecundarioOscuro;
                BarraFiltrosAbiertos.BorderBrush = ColorBordeOscuro;
                AplicarTemaInputs(true);
                
                // Forzar actualización de ComboBoxes después de aplicar tema - múltiples intentos
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CmbTodos != null) 
                    {
                        CambiarToggleButtonComboBox(CmbTodos);
                        CmbTodos.Background = ColorInputOscuro;
                        CmbTodos.Foreground = System.Windows.Media.Brushes.White;
                    }
                    if (CmbOrden != null) 
                    {
                        CambiarToggleButtonComboBox(CmbOrden);
                        CmbOrden.Background = ColorInputOscuro;
                        CmbOrden.Foreground = System.Windows.Media.Brushes.White;
                    }
                }), DispatcherPriority.Loaded);
                
                // Segundo intento después de un pequeño delay
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CmbTodos != null) CambiarToggleButtonComboBox(CmbTodos);
                    if (CmbOrden != null) CambiarToggleButtonComboBox(CmbOrden);
                }), DispatcherPriority.ContextIdle);
            }

            // Footer
            if (FooterEstadisticas != null)
            {
                FooterEstadisticas.Background = ColorFondoSecundarioOscuro;
                FooterEstadisticas.BorderBrush = ColorBordeOscuro;
                AplicarTemaEstadisticas(true);
                AplicarTemaBotonesFooter(true);
            }

            // Contadores de inicio
            AplicarTemaContadoresInicio(true);

            // ScrollViewer del contenido
            var scrollViewer = FindVisualChild<ScrollViewer>(PanelTicketsAbiertos);
            if (scrollViewer != null)
            {
                scrollViewer.Background = ColorFondoOscuro;
            }

            // Texto "Sin Tickets"
            if (TxtNoHayTickets != null)
            {
                TxtNoHayTickets.Foreground = ColorTextoSecundarioOscuro;
            }

            // Aplicar tema a tickets existentes
            AplicarTemaATodosLosTickets(true);

            // Aplicar tema a contadores y botones del footer
            AplicarTemaContadoresInicio(true);
            AplicarTemaBotonesFooter(true);

            // Mantener la pestaña Perfil en modo claro
            if (PanelTabPerfil != null)
            {
                PanelTabPerfil.Background = ColorFondoClaro;
            }
        }

        private void AplicarModoClaro()
        {
            // Fondo principal
            this.Background = ColorFondoClaro;
            
            // Icono luna (blanco)
            // Botón modo oscuro removido; no aplicar

            // Header principal en su color original
            var azulHeader = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 138));
            if (HeaderPrincipal != null)
            {
                HeaderPrincipal.Background = azulHeader;
            }
            
            // Contenido principal
            var gridPrincipal = this.Content as Grid;
            if (gridPrincipal != null && gridPrincipal.Children.Count > 3)
            {
                var contenidoGrid = gridPrincipal.Children[3] as Grid;
                if (contenidoGrid != null)
                {
                    contenidoGrid.Background = ColorFondoClaro;
                }
            }

            // Submenú
            if (PanelSubmenu != null)
            {
                PanelSubmenu.Background = ColorFondoSecundarioClaro;
                PanelSubmenu.BorderBrush = ColorBordeClaro;
                RestaurarBotonesSubmenu();
            }

            // Barra de filtros
            if (BarraFiltrosAbiertos != null)
            {
                BarraFiltrosAbiertos.Background = ColorFondoClaro;
                BarraFiltrosAbiertos.BorderBrush = ColorBordeClaro;
                AplicarTemaInputs(false);
                
                // Forzar actualización de ComboBoxes después de aplicar tema claro
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CmbTodos != null) 
                    {
                        RestaurarToggleButtonComboBox(CmbTodos);
                        CmbTodos.Background = ColorInputClaro;
                        CmbTodos.Foreground = ColorTextoClaro;
                    }
                    if (CmbOrden != null) 
                    {
                        RestaurarToggleButtonComboBox(CmbOrden);
                        CmbOrden.Background = ColorInputClaro;
                        CmbOrden.Foreground = ColorTextoClaro;
                    }
                }), DispatcherPriority.Loaded);
                
                // Segundo intento después de un pequeño delay
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (CmbTodos != null) RestaurarToggleButtonComboBox(CmbTodos);
                    if (CmbOrden != null) RestaurarToggleButtonComboBox(CmbOrden);
                }), DispatcherPriority.ContextIdle);
            }

            // Footer
            if (FooterEstadisticas != null)
            {
                FooterEstadisticas.Background = ColorFondoClaro;
                FooterEstadisticas.BorderBrush = ColorBordeClaro;
                AplicarTemaEstadisticas(false);
                AplicarTemaBotonesFooter(false);
            }

            // Contadores de inicio
            AplicarTemaContadoresInicio(false);

            // ScrollViewer del contenido
            var scrollViewer = FindVisualChild<ScrollViewer>(PanelTicketsAbiertos);
            if (scrollViewer != null)
            {
                scrollViewer.Background = ColorFondoClaro;
            }

            // Texto "Sin Tickets"
            if (TxtNoHayTickets != null)
            {
                TxtNoHayTickets.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
            }

            // Restaurar tickets
            AplicarTemaATodosLosTickets(false);

            // Aplicar tema a contadores y botones del footer
            AplicarTemaContadoresInicio(false);
            AplicarTemaBotonesFooter(false);

            // Mantener la pestaña Perfil en modo claro
            if (PanelTabPerfil != null)
            {
                PanelTabPerfil.Background = ColorFondoClaro;
            }
        }

        private void AplicarTemaBotonesSubmenu()
        {
            var botones = new[] { BtnTicketsAbiertos, BtnTicketsCerrados, BtnEntradasSalidas, 
                                 BtnReportes, BtnInformes, BtnEstadisticas };
            foreach (var btn in botones)
            {
                if (btn != null)
                {
                    // Solo cambiar si no está seleccionado (azul)
                    var bgColor = btn.Background as SolidColorBrush;
                    if (bgColor == null || bgColor.Color != System.Windows.Media.Color.FromRgb(59, 130, 246))
                    {
                        btn.Foreground = ColorTextoSecundarioOscuro;
                    }
                }
            }
        }

        private void RestaurarBotonesSubmenu()
        {
            var botones = new[] { BtnTicketsAbiertos, BtnTicketsCerrados, BtnEntradasSalidas, 
                                 BtnReportes, BtnInformes, BtnEstadisticas };
            foreach (var btn in botones)
            {
                if (btn != null)
                {
                    var bgColor = btn.Background as SolidColorBrush;
                    if (bgColor == null || bgColor.Color != System.Windows.Media.Color.FromRgb(59, 130, 246))
                    {
                        btn.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));
                    }
                }
            }
        }

        private void AplicarTemaInputs(bool modoOscuro)
        {
            if (modoOscuro)
            {
                if (TxtFiltroMatricula != null)
                {
                    TxtFiltroMatricula.Background = ColorInputOscuro;
                    TxtFiltroMatricula.Foreground = ColorTextoOscuro;
                    TxtFiltroMatricula.BorderBrush = ColorBordeOscuro;
                }
                if (PlaceholderMatricula != null)
                {
                    PlaceholderMatricula.Foreground = ColorTextoSecundarioOscuro;
                }
                
                // Aplicar tema a ComboBoxes
                AplicarTemaComboBox(CmbTodos, modoOscuro);
                AplicarTemaComboBox(CmbOrden, modoOscuro);
                
                // Botones de la barra de filtros
                if (BtnMenuHamburguesa != null)
                {
                    BtnMenuHamburguesa.Background = ColorInputOscuro;
                    BtnMenuHamburguesa.Foreground = ColorTextoOscuro;
                    BtnMenuHamburguesa.BorderBrush = ColorBordeOscuro;
                }
                if (BtnDescargar != null)
                {
                    BtnDescargar.Background = ColorInputOscuro;
                    BtnDescargar.Foreground = ColorTextoOscuro;
                    BtnDescargar.BorderBrush = ColorBordeOscuro;
                }
            }
            else
            {
                if (TxtFiltroMatricula != null)
                {
                    TxtFiltroMatricula.Background = ColorInputClaro;
                    TxtFiltroMatricula.Foreground = ColorTextoClaro;
                    TxtFiltroMatricula.BorderBrush = ColorBordeClaro;
                }
                if (PlaceholderMatricula != null)
                {
                    PlaceholderMatricula.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(156, 163, 175));
                }
                
                // Restaurar ComboBoxes
                AplicarTemaComboBox(CmbTodos, false);
                AplicarTemaComboBox(CmbOrden, false);
                
                // Restaurar botones
                if (BtnMenuHamburguesa != null)
                {
                    BtnMenuHamburguesa.Background = System.Windows.Media.Brushes.Transparent;
                    BtnMenuHamburguesa.Foreground = ColorTextoClaro;
                    BtnMenuHamburguesa.BorderBrush = ColorBordeClaro;
                }
                if (BtnDescargar != null)
                {
                    BtnDescargar.Background = System.Windows.Media.Brushes.Transparent;
                    BtnDescargar.Foreground = ColorTextoClaro;
                    BtnDescargar.BorderBrush = ColorBordeClaro;
                }
            }
        }

        private void AplicarTemaComboBox(ComboBox? comboBox, bool modoOscuro)
        {
            if (comboBox == null) return;
            
            if (modoOscuro)
            {
                // FORZAR fondo oscuro inmediatamente - MÚLTIPLES VECES
                comboBox.Background = ColorInputOscuro;
                comboBox.Foreground = System.Windows.Media.Brushes.White;
                comboBox.BorderBrush = ColorBordeOscuro;
                
                // Crear un estilo personalizado para forzar el fondo oscuro en TODO el ComboBox
                var comboStyle = new Style(typeof(ComboBox));
                comboStyle.Setters.Add(new Setter(Control.BackgroundProperty, ColorInputOscuro));
                comboStyle.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.White));
                comboStyle.Setters.Add(new Setter(Control.BorderBrushProperty, ColorBordeOscuro));
                comboBox.Style = comboStyle;
                
                // Color CASI NEGRO para los items del dropdown - máximo contraste
                var colorItemOscuro = new SolidColorBrush(System.Windows.Media.Color.FromRgb(10, 15, 25)); // Casi negro puro
                
                // Cambiar estilo de los items del dropdown con fondo MUY oscuro
                var itemStyle = new Style(typeof(ComboBoxItem));
                itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, colorItemOscuro));
                itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.White)); // Blanco puro
                itemStyle.Setters.Add(new Setter(Control.BorderBrushProperty, System.Windows.Media.Brushes.Transparent));
                itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 8, 10, 8)));
                
                // Efecto hover - un poco más claro pero sigue siendo oscuro
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(25, 35, 50))));
                hoverTrigger.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.White));
                itemStyle.Triggers.Add(hoverTrigger);
                
                // Efecto cuando está seleccionado - azul brillante
                var selectedTrigger = new Trigger { Property = System.Windows.Controls.Primitives.Selector.IsSelectedProperty, Value = true };
                selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)))); // Azul para seleccionado
                selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.White));
                itemStyle.Triggers.Add(selectedTrigger);
                
                comboBox.ItemContainerStyle = itemStyle;
                
                // Aplicar a items existentes directamente
                foreach (var item in comboBox.Items)
                {
                    if (item is ComboBoxItem comboItem)
                    {
                        comboItem.Background = colorItemOscuro;
                        comboItem.Foreground = System.Windows.Media.Brushes.White;
                    }
                }
                
                // Buscar y cambiar el ToggleButton interno (la parte clickeable) - MÚLTIPLES EVENTOS
                comboBox.Loaded += (s, e) => 
                {
                    CambiarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputOscuro;
                    comboBox.Foreground = System.Windows.Media.Brushes.White;
                };
                
                // Cambiar cuando se actualiza
                comboBox.LayoutUpdated += (s, e) => 
                {
                    CambiarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputOscuro;
                    comboBox.Foreground = System.Windows.Media.Brushes.White;
                };
                
                // También forzar después de delays - MÚLTIPLES VECES
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    CambiarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputOscuro;
                    comboBox.Foreground = System.Windows.Media.Brushes.White;
                }), DispatcherPriority.Loaded);
                
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    CambiarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputOscuro;
                    comboBox.Foreground = System.Windows.Media.Brushes.White;
                }), DispatcherPriority.ContextIdle);
                
                // Cambiar el fondo del popup cuando se abre
                comboBox.DropDownOpened += (s, e) => 
                {
                    Dispatcher.BeginInvoke(new Action(() => 
                    {
                        // FORZAR fondo oscuro en el botón principal
                        CambiarToggleButtonComboBox(comboBox);
                        comboBox.Background = ColorInputOscuro;
                        comboBox.Foreground = System.Windows.Media.Brushes.White;
                        
                        var popup = FindVisualChild<System.Windows.Controls.Primitives.Popup>(comboBox);
                        if (popup != null && popup.Child != null)
                        {
                            var border = popup.Child as Border;
                            if (border != null)
                            {
                                border.Background = colorItemOscuro;
                                border.BorderBrush = ColorBordeOscuro;
                            }
                            else
                            {
                                var innerBorder = FindVisualChild<Border>(popup.Child);
                                if (innerBorder != null)
                                {
                                    innerBorder.Background = colorItemOscuro;
                                    innerBorder.BorderBrush = ColorBordeOscuro;
                                }
                            }
                            
                            CambiarItemsEnPopup(popup.Child, colorItemOscuro);
                        }
                    }), DispatcherPriority.Loaded);
                };
                
                // También cuando se cierra el dropdown - forzar fondo oscuro
                comboBox.DropDownClosed += (s, e) => 
                {
                    Dispatcher.BeginInvoke(new Action(() => 
                    {
                        CambiarToggleButtonComboBox(comboBox);
                        comboBox.Background = ColorInputOscuro;
                        comboBox.Foreground = System.Windows.Media.Brushes.White;
                    }), DispatcherPriority.Loaded);
                };
            }
            else
            {
                // FORZAR fondo claro inmediatamente
                comboBox.Background = ColorInputClaro;
                comboBox.Foreground = ColorTextoClaro;
                comboBox.BorderBrush = ColorBordeClaro;
                
                // Crear un estilo personalizado para forzar el fondo claro
                var comboStyle = new Style(typeof(ComboBox));
                comboStyle.Setters.Add(new Setter(Control.BackgroundProperty, ColorInputClaro));
                comboStyle.Setters.Add(new Setter(Control.ForegroundProperty, ColorTextoClaro));
                comboStyle.Setters.Add(new Setter(Control.BorderBrushProperty, ColorBordeClaro));
                comboBox.Style = comboStyle;
                
                // Restaurar estilo de items
                var itemStyle = new Style(typeof(ComboBoxItem));
                itemStyle.Setters.Add(new Setter(Control.BackgroundProperty, ColorInputClaro));
                itemStyle.Setters.Add(new Setter(Control.ForegroundProperty, ColorTextoClaro));
                comboBox.ItemContainerStyle = itemStyle;
                
                // Restaurar items existentes
                foreach (var item in comboBox.Items)
                {
                    if (item is ComboBoxItem comboItem)
                    {
                        comboItem.Background = ColorInputClaro;
                        comboItem.Foreground = ColorTextoClaro;
                    }
                }
                
                // Buscar y cambiar el ToggleButton interno para restaurar a claro
                comboBox.Loaded += (s, e) => 
                {
                    RestaurarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputClaro;
                    comboBox.Foreground = ColorTextoClaro;
                };
                
                comboBox.LayoutUpdated += (s, e) => 
                {
                    RestaurarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputClaro;
                    comboBox.Foreground = ColorTextoClaro;
                };
                
                // También forzar después de delays
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    RestaurarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputClaro;
                    comboBox.Foreground = ColorTextoClaro;
                }), DispatcherPriority.Loaded);
                
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    RestaurarToggleButtonComboBox(comboBox);
                    comboBox.Background = ColorInputClaro;
                    comboBox.Foreground = ColorTextoClaro;
                }), DispatcherPriority.ContextIdle);
            }
        }
        
        private void RestaurarToggleButtonComboBox(ComboBox comboBox)
        {
            if (comboBox == null || _modoOscuro) return;
            
            // FORZAR fondo claro en el ComboBox directamente
            comboBox.Background = ColorInputClaro;
            comboBox.Foreground = ColorTextoClaro;
            comboBox.BorderBrush = ColorBordeClaro;
            
            // Buscar TODOS los elementos dentro del ComboBox y cambiar sus fondos a claro
            CambiarFondoRecursivo(comboBox, ColorInputClaro, ColorTextoClaro);
            
            // Buscar el ToggleButton dentro del ComboBox
            var toggleButton = FindVisualChild<System.Windows.Controls.Primitives.ToggleButton>(comboBox);
            if (toggleButton != null)
            {
                toggleButton.Background = ColorInputClaro;
                toggleButton.Foreground = ColorTextoClaro;
                toggleButton.BorderBrush = ColorBordeClaro;
                
                // Cambiar TODOS los elementos dentro del ToggleButton
                CambiarFondoRecursivo(toggleButton, ColorInputClaro, ColorTextoClaro);
            }
            
            // Buscar Border dentro del ComboBox
            var border = FindVisualChild<Border>(comboBox);
            if (border != null)
            {
                border.Background = ColorInputClaro;
                border.BorderBrush = ColorBordeClaro;
            }
            
            // Buscar Grid dentro del ComboBox
            var grid = FindVisualChild<Grid>(comboBox);
            if (grid != null)
            {
                grid.Background = ColorInputClaro;
            }
            
            // Buscar ContentPresenter y cambiar su contenido
            var contentPresenter = FindVisualChild<ContentPresenter>(comboBox);
            if (contentPresenter != null)
            {
                var textBlock = FindVisualChild<TextBlock>(contentPresenter);
                if (textBlock != null)
                {
                    textBlock.Foreground = ColorTextoClaro;
                }
            }
        }

        private void AplicarTemaEstadisticas(bool modoOscuro)
        {
            // Este método se mantiene por compatibilidad pero ahora usa AplicarTemaContadoresInicio
            AplicarTemaContadoresInicio(modoOscuro);
        }

        private void AplicarTemaContadoresInicio(bool modoOscuro)
        {
            // Los contadores ahora se generan dinámicamente, aplicar tema a todos los TextBlocks en PanelContadoresInicio
            if (PanelContadoresInicio != null)
            {
                var colorLabel = modoOscuro ? ColorTextoSecundarioOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139));
                var colorValor = modoOscuro ? ColorTextoOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(51, 65, 85));

                foreach (var child in PanelContadoresInicio.Children)
                {
                    if (child is StackPanel stackPanel)
                    {
                        foreach (var innerChild in stackPanel.Children)
                        {
                            if (innerChild is TextBlock textBlock)
                            {
                                if (textBlock.Name != null && (textBlock.Name.StartsWith("TxtContador") || 
                                    textBlock.Name == "TxtEstadisticaTotalTickets" || 
                                    textBlock.Name == "TxtEstadisticaEntradas24" || 
                                    textBlock.Name == "TxtEstadisticaSalidas24"))
                                {
                                    textBlock.Foreground = colorValor;
                                }
                                else
                                {
                                    textBlock.Foreground = colorLabel;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void AplicarTemaBotonesFooter(bool modoOscuro)
        {
            // Aplicar tema a los botones del footer (Cerrar Ticket y Cerrar Sesión) y separadores
            if (PanelBotonesTarifas != null)
            {
                var colorBotonFondo = modoOscuro ? ColorFondoSecundarioOscuro : System.Windows.Media.Brushes.White;
                var colorBotonTexto = modoOscuro ? ColorTextoOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                var colorBotonBorde = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246));
                var colorSeparador = modoOscuro ? ColorBordeOscuro : new SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 231, 235));

                foreach (var child in PanelBotonesTarifas.Children)
                {
                    if (child is StackPanel filaPrincipal)
                    {
                        foreach (var elemento in filaPrincipal.Children)
                        {
                            // Botones de cerrar ticket y cerrar sesión
                            if (elemento is Border border && border.Child is Button button)
                            {
                                if (button.Name == "BtnCerrarTicket" || button.Name == "BtnCerrarSesionFooter")
                                {
                                    button.Background = colorBotonFondo;
                                    button.Foreground = colorBotonTexto;
                                    button.BorderBrush = colorBotonBorde;
                                }
                            }
                            // Separadores
                            else if (elemento is Border separador && separador.Child == null)
                            {
                                separador.Background = colorSeparador;
                            }
                        }
                    }
                }
            }
        }

        private void CambiarColorLabelsEstadisticas(SolidColorBrush color)
        {
            if (FooterEstadisticas != null)
            {
                // Buscar todos los TextBlocks que son labels (no los valores)
                CambiarColorTextosEnPanel(FooterEstadisticas, new[] { "Hora", "Mensuales", "Total x Tickets", 
                                                                      "Entradas ult. 24 hs", "Salidas ult. 24 hs" }, color);
                
                // Los labels se cambian con CambiarColorTextosEnPanel
                
                // Buscar todos los TextBlocks en el footer que no sean los valores
                BuscarYCambiarLabelsEstadisticas(FooterEstadisticas, color);
            }
        }

        private void BuscarYCambiarLabelsEstadisticas(DependencyObject parent, SolidColorBrush color)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBlock tb)
                {
                    // Si es un label (no es un valor de estadística)
                    if (tb.Name != "TxtEstadisticaHora" && tb.Name != "TxtEstadisticaMensuales" &&
                        tb.Name != "TxtEstadisticaTotalTickets" && tb.Name != "TxtEstadisticaEntradas24" &&
                        tb.Name != "TxtEstadisticaSalidas24" &&
                        (tb.Text == "Hora" || tb.Text == "Mensuales" || tb.Text == "Total x Tickets" ||
                         tb.Text == "Entradas ult. 24 hs" || tb.Text == "Salidas ult. 24 hs"))
                    {
                        tb.Foreground = color;
                    }
                }
                BuscarYCambiarLabelsEstadisticas(child, color);
            }
        }

        private void CambiarColorTexto(TextBlock? textBlock, SolidColorBrush color)
        {
            if (textBlock != null)
            {
                textBlock.Foreground = color;
            }
        }

        private void CambiarColorTextosEnPanel(DependencyObject parent, string[] textos, SolidColorBrush color)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBlock tb && textos.Any(t => tb.Text.Contains(t)) && 
                    tb.Name != "TxtEstadisticaHora" && tb.Name != "TxtEstadisticaMensuales" &&
                    tb.Name != "TxtEstadisticaTotalTickets" && tb.Name != "TxtEstadisticaEntradas24" &&
                    tb.Name != "TxtEstadisticaSalidas24")
                {
                    tb.Foreground = color;
                }
                CambiarColorTextosEnPanel(child, textos, color);
            }
        }

        private void AplicarTemaATodosLosTickets(bool modoOscuro)
        {
            // Forzar actualización del ItemsControl
            if (ItemsTickets != null)
            {
                var itemsSource = ItemsTickets.ItemsSource;
                ItemsTickets.ItemsSource = null;
                ItemsTickets.ItemsSource = itemsSource;
                
                // Esperar a que se rendericen y luego aplicar tema
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    AplicarTemaATicketsRenderizados();
                }), DispatcherPriority.Loaded);
            }
        }

        private void AplicarTemaATicketsRenderizados()
        {
            if (ItemsTickets != null)
            {
                var scrollViewer = FindVisualChild<ScrollViewer>(PanelTicketsAbiertos);
                if (scrollViewer != null)
                {
                    var itemsControl = FindVisualChild<ItemsControl>(scrollViewer);
                    if (itemsControl != null)
                    {
                        BuscarYActualizarTickets(itemsControl);
                    }
                }
            }
        }

        private void ActualizarTiempoTicketsRenderizados()
        {
            if (ItemsTickets == null) return;
            var scrollViewer = FindVisualChild<ScrollViewer>(PanelTicketsAbiertos);
            if (scrollViewer != null)
            {
                var itemsControl = FindVisualChild<ItemsControl>(scrollViewer);
                if (itemsControl != null)
                {
                    foreach (var border in FindVisualChildren<Border>(itemsControl))
                    {
                        if (border.Name == "BorderTicket" && border.DataContext is Ticket ticket)
                        {
                            ActualizarCardTicket(border, ticket);
                        }
                    }
                }
            }
        }

        private void AplicarFiltrosCerrados()
        {
            _ticketsCerradosFiltrados.Clear();

            // Obtener valores de los ComboBox usando Tag en lugar de SelectedValue
            int? categoriaId = null;
            if (CmbFiltroCategoriaCerrados?.SelectedItem is ComboBoxItem categoriaItem && categoriaItem.Tag is int catId)
            {
                categoriaId = catId;
            }

            int? abiertoPorId = null;
            if (CmbFiltroAbiertoPor?.SelectedItem is ComboBoxItem abiertoItem && abiertoItem.Tag is int abiertoId)
            {
                abiertoPorId = abiertoId;
            }

            int? cerradoPorId = null;
            if (CmbFiltroCerradoPor?.SelectedItem is ComboBoxItem cerradoItem && cerradoItem.Tag is int cerradoId)
            {
                cerradoPorId = cerradoId;
            }

            string? tipoSeleccion = null;
            if (CmbFiltroTipoTicket?.SelectedItem is ComboBoxItem tipoItem && tipoItem.Tag is string tipoTag)
            {
                tipoSeleccion = tipoTag;
            }

            string? cancelacionSeleccion = null;
            if (CmbFiltroCancelacion?.SelectedItem is ComboBoxItem cancelacionItem && cancelacionItem.Tag is string cancelacionTag)
            {
                cancelacionSeleccion = cancelacionTag;
            }

            string busqueda = TxtBuscarCerrados?.Text?.Trim().ToLower() ?? string.Empty;

            var filtrados = _ticketsCerrados.ToList();

            if (categoriaId.HasValue)
                filtrados = filtrados.Where(t => t.CategoriaId == categoriaId.Value).ToList();

            if (abiertoPorId.HasValue)
                filtrados = filtrados.Where(t => t.AdminCreadorId == abiertoPorId.Value).ToList();

            if (cerradoPorId.HasValue)
                filtrados = filtrados.Where(t => t.AdminCerradorId == cerradoPorId.Value).ToList();

            if (!string.IsNullOrWhiteSpace(tipoSeleccion) && tipoSeleccion != "Todos")
            {
                filtrados = filtrados.Where(t =>
                {
                    var tarifa = _tarifas.FirstOrDefault(x => x.Id == t.TarifaId);
                    if (tarifa == null) return false;
                    switch (tipoSeleccion)
                    {
                        case "Hora": return tarifa.Tipo == TipoTarifa.PorHora;
                        case "Turno": return tarifa.Tipo == TipoTarifa.PorTurno;
                        case "Estadia": return tarifa.Tipo == TipoTarifa.PorEstadia;
                        case "Mensual": return tarifa.Tipo == TipoTarifa.Mensual;
                        default: return true;
                    }
                }).ToList();
            }

            if (!string.IsNullOrWhiteSpace(cancelacionSeleccion) && cancelacionSeleccion != "Todos")
            {
                if (cancelacionSeleccion == "Cancelados")
                    filtrados = filtrados.Where(t => t.EstaCancelado).ToList();
                else if (cancelacionSeleccion == "No cancelados")
                    filtrados = filtrados.Where(t => !t.EstaCancelado).ToList();
            }

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                filtrados = filtrados.Where(t =>
                    (t.Matricula?.ToLower().Contains(busqueda) ?? false) ||
                    (t.Descripcion?.ToLower().Contains(busqueda) ?? false) ||
                    (t.NotaAdicional?.ToLower().Contains(busqueda) ?? false) ||
                    (t.MotivoCancelacion?.ToLower().Contains(busqueda) ?? false)
                ).ToList();
            }

            foreach (var t in filtrados)
                _ticketsCerradosFiltrados.Add(t);

            if (DataGridCerrados != null)
                DataGridCerrados.ItemsSource = _ticketsCerradosFiltrados;

            ActualizarTotalesCerrados();
        }

        private void ActualizarTotalesCerrados()
        {
            int total = _ticketsCerradosFiltrados.Count;
            int cancelados = _ticketsCerradosFiltrados.Count(t => t.EstaCancelado);

            int totalHora = 0;
            decimal importeTotal = 0m;
            foreach (var t in _ticketsCerradosFiltrados)
            {
                var tarifa = _tarifas.FirstOrDefault(x => x.Id == t.TarifaId);
                if (tarifa != null && tarifa.Tipo == TipoTarifa.PorHora)
                    totalHora++;

                importeTotal += t.Monto ?? CalcularImporteActual(t);
            }

            if (TxtTotalCerrados != null) TxtTotalCerrados.Text = total.ToString();
            if (TxtTotalCancelados != null) TxtTotalCancelados.Text = cancelados.ToString();
            if (TxtTotalHora != null) TxtTotalHora.Text = totalHora.ToString();
            if (TxtImporteTotalCerrados != null) TxtImporteTotalCerrados.Text = importeTotal.ToString("$#,0.00");
        }

        private void CargarFiltrosCerrados()
        {
            // Categorías
            if (CmbFiltroCategoriaCerrados != null)
            {
                CmbFiltroCategoriaCerrados.Items.Clear();
                CmbFiltroCategoriaCerrados.Items.Add(new ComboBoxItem { Content = "Todas", Tag = null, IsSelected = true });
                foreach (var c in _categorias)
                {
                    CmbFiltroCategoriaCerrados.Items.Add(new ComboBoxItem { Content = c.Nombre, Tag = c.Id });
                }
            }

            // Operadores
            var admins = _dbService.ObtenerTodosLosAdmins();
            if (CmbFiltroAbiertoPor != null)
            {
                CmbFiltroAbiertoPor.Items.Clear();
                CmbFiltroAbiertoPor.Items.Add(new ComboBoxItem { Content = "Todos los Operadores", Tag = null, IsSelected = true });
                foreach (var a in admins)
                {
                    CmbFiltroAbiertoPor.Items.Add(new ComboBoxItem { Content = a.Username, Tag = a.Id });
                }
            }
            if (CmbFiltroCerradoPor != null)
            {
                CmbFiltroCerradoPor.Items.Clear();
                CmbFiltroCerradoPor.Items.Add(new ComboBoxItem { Content = "Todos los Operadores", Tag = null, IsSelected = true });
                foreach (var a in admins)
                {
                    CmbFiltroCerradoPor.Items.Add(new ComboBoxItem { Content = a.Username, Tag = a.Id });
                }
            }

            // Tipos
            if (CmbFiltroTipoTicket != null)
            {
                CmbFiltroTipoTicket.Items.Clear();
                CmbFiltroTipoTicket.Items.Add(new ComboBoxItem { Content = "Todos", Tag = "Todos", IsSelected = true });
                CmbFiltroTipoTicket.Items.Add(new ComboBoxItem { Content = "Hora", Tag = "Hora" });
                CmbFiltroTipoTicket.Items.Add(new ComboBoxItem { Content = "Turno", Tag = "Turno" });
                CmbFiltroTipoTicket.Items.Add(new ComboBoxItem { Content = "Estadia", Tag = "Estadia" });
                CmbFiltroTipoTicket.Items.Add(new ComboBoxItem { Content = "Mensual", Tag = "Mensual" });
            }

            // Cancelación
            if (CmbFiltroCancelacion != null)
            {
                CmbFiltroCancelacion.Items.Clear();
                CmbFiltroCancelacion.Items.Add(new ComboBoxItem { Content = "Todos", Tag = "Todos", IsSelected = true });
                CmbFiltroCancelacion.Items.Add(new ComboBoxItem { Content = "Cancelados", Tag = "Cancelados" });
                CmbFiltroCancelacion.Items.Add(new ComboBoxItem { Content = "No cancelados", Tag = "No cancelados" });
            }
        }

        private void CmbFiltroCerrados_Changed(object sender, RoutedEventArgs e)
        {
            AplicarFiltrosCerrados();
        }

        private void TxtBuscarCerrados_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltrosCerrados();
        }

        private void BtnExportarCerrados_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV|*.csv",
                    FileName = "tickets_cerrados.csv"
                };
                if (dialog.ShowDialog() == true)
                {
                    using var sw = new System.IO.StreamWriter(dialog.FileName, false, Encoding.UTF8);
                    sw.WriteLine("Id;FechaEntrada;FechaSalida;Matricula;Categoria;Tarifa;Cancelado;Motivo;Monto");
                    foreach (var t in _ticketsCerradosFiltrados)
                    {
                        var fechaSalidaStr = t.FechaSalida.HasValue ? t.FechaSalida.Value.ToString("dd/MM/yyyy HH:mm") : "";
                        sw.WriteLine($"{t.Id};{t.FechaEntrada:dd/MM/yyyy HH:mm};{fechaSalidaStr};{t.Matricula};{t.CategoriaNombre};{t.TarifaNombre};{(t.EstaCancelado ? "SI" : "NO")};{t.MotivoCancelacion};{t.Monto ?? CalcularImporteActual(t)}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BuscarYActualizarTickets(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is Border border && border.Name == "BorderTicket")
                {
                    AplicarTemaTicket(border);
                }
                else
                {
                    BuscarYActualizarTickets(child);
                }
            }
        }

        private void CambiarToggleButtonComboBox(ComboBox comboBox)
        {
            if (comboBox == null || !_modoOscuro) return;
            
            // FORZAR fondo oscuro en el ComboBox directamente - MÚLTIPLES VECES
            comboBox.Background = ColorInputOscuro;
            comboBox.Foreground = System.Windows.Media.Brushes.White;
            comboBox.BorderBrush = ColorBordeOscuro;
            
            // Buscar TODOS los elementos dentro del ComboBox y cambiar sus fondos
            CambiarFondoRecursivo(comboBox, ColorInputOscuro, System.Windows.Media.Brushes.White);
            
            // Buscar el ToggleButton dentro del ComboBox (la parte clickeable)
            var toggleButton = FindVisualChild<System.Windows.Controls.Primitives.ToggleButton>(comboBox);
            if (toggleButton != null)
            {
                toggleButton.Background = ColorInputOscuro;
                toggleButton.Foreground = System.Windows.Media.Brushes.White;
                toggleButton.BorderBrush = ColorBordeOscuro;
                
                // Cambiar TODOS los elementos dentro del ToggleButton
                CambiarFondoRecursivo(toggleButton, ColorInputOscuro, System.Windows.Media.Brushes.White);
            }
            
            // Buscar Border dentro del ComboBox
            var border = FindVisualChild<Border>(comboBox);
            if (border != null)
            {
                border.Background = ColorInputOscuro;
                border.BorderBrush = ColorBordeOscuro;
            }
            
            // Buscar Grid dentro del ComboBox
            var grid = FindVisualChild<Grid>(comboBox);
            if (grid != null)
            {
                grid.Background = ColorInputOscuro;
            }
            
            // Buscar ContentPresenter y cambiar su contenido
            var contentPresenter = FindVisualChild<ContentPresenter>(comboBox);
            if (contentPresenter != null)
            {
                var textBlock = FindVisualChild<TextBlock>(contentPresenter);
                if (textBlock != null)
                {
                    textBlock.Foreground = System.Windows.Media.Brushes.White;
                }
            }
        }
        
        private void CambiarFondoRecursivo(DependencyObject parent, SolidColorBrush colorFondo, SolidColorBrush colorTexto)
        {
            if (parent == null) return;
            
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                
                if (child is Border border)
                {
                    border.Background = colorFondo;
                    border.BorderBrush = ColorBordeOscuro;
                }
                else if (child is Grid grid)
                {
                    grid.Background = colorFondo;
                }
                else if (child is Panel panel)
                {
                    panel.Background = colorFondo;
                }
                else if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = colorTexto;
                }
                else if (child is ContentPresenter cp)
                {
                    var tb = FindVisualChild<TextBlock>(cp);
                    if (tb != null) tb.Foreground = colorTexto;
                }
                else if (child is System.Windows.Controls.Primitives.ToggleButton tb)
                {
                    tb.Background = colorFondo;
                    tb.Foreground = colorTexto;
                    tb.BorderBrush = ColorBordeOscuro;
                }
                
                CambiarFondoRecursivo(child, colorFondo, colorTexto);
            }
        }

        private void CambiarItemsEnPopup(DependencyObject parent, SolidColorBrush colorFondo)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ComboBoxItem item)
                {
                    item.Background = colorFondo;
                    item.Foreground = System.Windows.Media.Brushes.White;
                }
                CambiarItemsEnPopup(child, colorFondo);
            }
        }

        private T? FindVisualChild<T>(DependencyObject parent, string name = "") where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    if (string.IsNullOrEmpty(name) || (child is FrameworkElement fe && fe.Name == name))
                    {
                        return result;
                    }
                }
                var childOfChild = FindVisualChild<T>(child, name);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    yield return tChild;

                foreach (var descendant in FindVisualChildren<T>(child))
                    yield return descendant;
            }
        }
        
        private void PopupCategoria_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Cerrar popup al hacer clic en el fondo oscuro
            if (PopupCategoria != null)
                PopupCategoria.Visibility = Visibility.Collapsed;
            _categoriaEditando = null;
            if (TxtNombreCategoria != null)
                TxtNombreCategoria.Text = string.Empty;
        }
        
        private void PopupCategoriaContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Prevenir que el clic en el contenido cierre el popup
            e.Handled = true;
        }

        // Métodos para gestión de usuarios
        private void CargarUsuarios()
        {
            if (ItemsUsuarios == null) return;

            var usuarios = _dbService.ObtenerTodosLosAdmins();
            _usuarios.Clear();

            int numero = 1;
            foreach (var admin in usuarios)
            {
                var ultimoAcceso = _dbService.ObtenerUltimoAccesoAdmin(admin.Id);
                bool puedeEliminar = admin.Username != _username; // No se puede eliminar el usuario actual

                _usuarios.Add(new UsuarioDisplay
                {
                    Id = admin.Id,
                    Numero = numero++,
                    Nombre = admin.Nombre,
                    Apellido = admin.Apellido,
                    Username = admin.Username,
                    Rol = admin.Rol,
                    UltimoAcceso = ultimoAcceso,
                    PuedeEliminar = puedeEliminar
                });
            }

            ItemsUsuarios.ItemsSource = _usuarios;
        }

        private void BtnCrearUsuario_Click(object sender, RoutedEventArgs e)
        {
            _usuarioEditando = null;
            
            if (TxtTituloPopupUsuario != null)
                TxtTituloPopupUsuario.Text = "Nuevo Usuario";
            
            if (TxtNombreUsuarioPopup != null) TxtNombreUsuarioPopup.Text = "";
            if (TxtApellidoUsuarioPopup != null) TxtApellidoUsuarioPopup.Text = "";
            if (TxtUsernameUsuarioPopup != null)
            {
                TxtUsernameUsuarioPopup.Text = "";
                TxtUsernameUsuarioPopup.IsReadOnly = false;
            }
            if (TxtPasswordUsuarioPopup != null) TxtPasswordUsuarioPopup.Password = "";
            if (TxtPasswordRepetirUsuarioPopup != null) TxtPasswordRepetirUsuarioPopup.Password = "";
            if (TxtEmailUsuarioPopup != null) TxtEmailUsuarioPopup.Text = "";
            if (CmbRolUsuarioPopup != null) CmbRolUsuarioPopup.SelectedIndex = 1; // operador por defecto

            if (PopupUsuario != null)
                PopupUsuario.Visibility = Visibility.Visible;
        }

        private void BtnEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UsuarioDisplay usuarioDisplay)
            {
                var admin = _dbService.ObtenerTodosLosAdmins().FirstOrDefault(a => a.Id == usuarioDisplay.Id);
                if (admin == null) return;

                _usuarioEditando = admin;

                if (TxtTituloPopupUsuario != null)
                    TxtTituloPopupUsuario.Text = "Editar Usuario";

                if (TxtNombreUsuarioPopup != null) TxtNombreUsuarioPopup.Text = admin.Nombre;
                if (TxtApellidoUsuarioPopup != null) TxtApellidoUsuarioPopup.Text = admin.Apellido;
                if (TxtUsernameUsuarioPopup != null)
                {
                    TxtUsernameUsuarioPopup.Text = admin.Username;
                    TxtUsernameUsuarioPopup.IsReadOnly = true; // No se puede cambiar el username
                }
                if (TxtPasswordUsuarioPopup != null) TxtPasswordUsuarioPopup.Password = "";
                if (TxtPasswordRepetirUsuarioPopup != null) TxtPasswordRepetirUsuarioPopup.Password = "";
                if (TxtEmailUsuarioPopup != null) TxtEmailUsuarioPopup.Text = admin.Email;
                if (CmbRolUsuarioPopup != null)
                {
                    for (int i = 0; i < CmbRolUsuarioPopup.Items.Count; i++)
                    {
                        if (CmbRolUsuarioPopup.Items[i] is ComboBoxItem item && item.Tag?.ToString() == admin.Rol)
                        {
                            CmbRolUsuarioPopup.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (PopupUsuario != null)
                    PopupUsuario.Visibility = Visibility.Visible;
            }
        }

        private void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UsuarioDisplay usuarioDisplay)
            {
                if (usuarioDisplay.Username == _username)
                {
                    MessageBox.Show("No puedes eliminar tu propio usuario.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar al usuario '{usuarioDisplay.Username}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _dbService.EliminarAdmin(usuarioDisplay.Id);
                        CargarUsuarios();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al eliminar usuario: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnGuardarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (TxtNombreUsuarioPopup == null || string.IsNullOrWhiteSpace(TxtNombreUsuarioPopup.Text))
            {
                MessageBox.Show("El nombre es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TxtApellidoUsuarioPopup == null || string.IsNullOrWhiteSpace(TxtApellidoUsuarioPopup.Text))
            {
                MessageBox.Show("El apellido es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TxtUsernameUsuarioPopup == null || string.IsNullOrWhiteSpace(TxtUsernameUsuarioPopup.Text))
            {
                MessageBox.Show("El usuario es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (TxtEmailUsuarioPopup == null || string.IsNullOrWhiteSpace(TxtEmailUsuarioPopup.Text))
            {
                MessageBox.Show("El email es obligatorio.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validar formato de email
            if (!TxtEmailUsuarioPopup.Text.Contains("@") || !TxtEmailUsuarioPopup.Text.Contains("."))
            {
                MessageBox.Show("El email no tiene un formato válido.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbRolUsuarioPopup == null || CmbRolUsuarioPopup.SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un rol.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_usuarioEditando == null)
                {
                    // Crear nuevo usuario
                    if (TxtPasswordUsuarioPopup == null || string.IsNullOrWhiteSpace(TxtPasswordUsuarioPopup.Password))
                    {
                        MessageBox.Show("La contraseña es obligatoria.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (TxtPasswordUsuarioPopup.Password != TxtPasswordRepetirUsuarioPopup?.Password)
                    {
                        MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (TxtPasswordUsuarioPopup.Password.Length < 6)
                    {
                        MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Verificar si el username ya existe
                    var adminExistente = _dbService.ObtenerAdminPorUsername(TxtUsernameUsuarioPopup.Text);
                    if (adminExistente != null)
                    {
                        MessageBox.Show("El nombre de usuario ya existe.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var nuevoAdmin = new Admin
                    {
                        Nombre = TxtNombreUsuarioPopup.Text,
                        Apellido = TxtApellidoUsuarioPopup.Text,
                        Username = TxtUsernameUsuarioPopup.Text,
                        PasswordHash = TxtPasswordUsuarioPopup.Password,
                        Email = TxtEmailUsuarioPopup.Text,
                        Rol = (CmbRolUsuarioPopup.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "operador"
                    };

                    _dbService.CrearAdmin(nuevoAdmin);
                }
                else
                {
                    // Actualizar usuario existente
                    _usuarioEditando.Nombre = TxtNombreUsuarioPopup.Text;
                    _usuarioEditando.Apellido = TxtApellidoUsuarioPopup.Text;
                    _usuarioEditando.Email = TxtEmailUsuarioPopup.Text;
                    _usuarioEditando.Rol = (CmbRolUsuarioPopup.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "operador";

                    _dbService.ActualizarAdmin(_usuarioEditando);

                    // Si se cambió la contraseña, actualizarla
                    if (TxtPasswordUsuarioPopup != null && !string.IsNullOrWhiteSpace(TxtPasswordUsuarioPopup.Password))
                    {
                        if (TxtPasswordUsuarioPopup.Password != TxtPasswordRepetirUsuarioPopup?.Password)
                        {
                            MessageBox.Show("Las contraseñas no coinciden.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (TxtPasswordUsuarioPopup.Password.Length < 6)
                        {
                            MessageBox.Show("La contraseña debe tener al menos 6 caracteres.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        _dbService.ActualizarPasswordAdmin(_usuarioEditando.Id, TxtPasswordUsuarioPopup.Password);
                    }
                }

                if (PopupUsuario != null)
                    PopupUsuario.Visibility = Visibility.Collapsed;

                CargarUsuarios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar usuario: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (PopupUsuario != null)
                PopupUsuario.Visibility = Visibility.Collapsed;
        }

        private void PopupUsuario_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (PopupUsuario != null)
                PopupUsuario.Visibility = Visibility.Collapsed;
        }

        private void DataGridMensualesMovimientos_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (e.Row.Item is MovimientoMensual movimiento)
            {
                // Buscar los TextBlocks de Importe y Balance en la fila usando el árbol visual
                var importeCell = DataGridMensualesMovimientos.Columns[2].GetCellContent(e.Row);
                var balanceCell = DataGridMensualesMovimientos.Columns[3].GetCellContent(e.Row);
                
                // Buscar TextBlock de Importe
                TextBlock? txtImporte = null;
                if (importeCell != null)
                {
                    txtImporte = importeCell.FindName("TxtImporte") as TextBlock;
                    if (txtImporte == null)
                    {
                        // Si no se encuentra por nombre, buscar en el árbol visual
                        txtImporte = FindVisualChild<TextBlock>(importeCell);
                    }
                }
                
                // Buscar TextBlock de Balance
                TextBlock? txtBalance = null;
                if (balanceCell != null)
                {
                    txtBalance = balanceCell.FindName("TxtBalance") as TextBlock;
                    if (txtBalance == null)
                    {
                        // Si no se encuentra por nombre, buscar en el árbol visual
                        txtBalance = FindVisualChild<TextBlock>(balanceCell);
                    }
                }
                
                // Aplicar color al importe
                if (txtImporte != null)
                {
                    if (movimiento.Importe < 0)
                        txtImporte.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)); // Rojo
                    else if (movimiento.Importe == 0)
                        txtImporte.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0)); // Negro
                    else
                        txtImporte.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74)); // Verde
                }
                
                // Aplicar color al balance
                if (txtBalance != null)
                {
                    if (movimiento.BalanceResultante < 0)
                        txtBalance.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38)); // Rojo
                    else if (movimiento.BalanceResultante == 0)
                        txtBalance.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0)); // Negro
                    else
                        txtBalance.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(22, 163, 74)); // Verde
                }
            }
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    return t;
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                    return childOfChild;
            }
            return null;
        }

        private void PopupUsuarioContent_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // Prevenir que se cierre al hacer click en el contenido
        }

        private void TxtPantallaCliente_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Evitar guardar durante la carga inicial
            if (_cargandoConfiguracionPantallaCliente)
                return;

            try
            {
                var panel = _panelesModulos.ContainsKey("MONITOR") ? _panelesModulos["MONITOR"] : null;
                if (panel == null) return;

                var txtBienvenida1 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaBienvenida1") as TextBox;
                var txtBienvenida2 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaBienvenida2") as TextBox;
                var txtAgradecimiento1 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaAgradecimiento1") as TextBox;
                var txtAgradecimiento2 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaAgradecimiento2") as TextBox;
                var txtCobro = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaCobro") as TextBox;

                string bienvenida1 = txtBienvenida1?.Text ?? "Bienvenidos a";
                string bienvenida2 = txtBienvenida2?.Text ?? "";
                string agradecimiento1 = txtAgradecimiento1?.Text ?? "¡Gracias por su visita!";
                string agradecimiento2 = txtAgradecimiento2?.Text ?? "";
                string cobro = txtCobro?.Text ?? "";

                _dbService.ActualizarConfiguracionPantallaCliente(bienvenida1, bienvenida2, agradecimiento1, agradecimiento2, cobro);
                
                // Los cambios se reflejarán automáticamente en la pantalla del cliente
                // ya que el servidor lee de la DB en cada request
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar configuración automática: {ex.Message}");
            }
        }

        private void BtnPantallaCopiar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(_urlPantallaCliente);
                MessageBox.Show("URL copiada al portapapeles.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al copiar URL: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CargarConfiguracionPantallaCliente()
        {
            try
            {
                var panel = _panelesModulos.ContainsKey("MONITOR") ? _panelesModulos["MONITOR"] : null;
                if (panel == null) return;

                // Activar flag para evitar guardado durante la carga
                _cargandoConfiguracionPantallaCliente = true;

                var config = _dbService.ObtenerConfiguracionPantallaCliente();

                var txtBienvenida1 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaBienvenida1") as TextBox;
                var txtBienvenida2 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaBienvenida2") as TextBox;
                var txtAgradecimiento1 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaAgradecimiento1") as TextBox;
                var txtAgradecimiento2 = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaAgradecimiento2") as TextBox;
                var txtCobro = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaCobro") as TextBox;

                if (txtBienvenida1 != null) txtBienvenida1.Text = config["BienvenidaLinea1"];
                if (txtBienvenida2 != null) txtBienvenida2.Text = config["BienvenidaLinea2"];
                if (txtAgradecimiento1 != null) txtAgradecimiento1.Text = config["AgradecimientoLinea1"];
                if (txtAgradecimiento2 != null) txtAgradecimiento2.Text = config["AgradecimientoLinea2"];
                if (txtCobro != null) txtCobro.Text = config["CobroAclaracion"];
                
                // Desactivar flag después de cargar
                _cargandoConfiguracionPantallaCliente = false;
                
                // Actualizar instrucciones sobre segundo monitor
                ActualizarInstruccionesSegundoMonitor(panel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar configuración pantalla cliente: {ex.Message}");
                _cargandoConfiguracionPantallaCliente = false;
            }
        }

        private void CargarCredencialesMercadoPago()
        {
            try
            {
                var panel = _panelesModulos.ContainsKey("MERCADOPAGO") ? _panelesModulos["MERCADOPAGO"] : null;
                if (panel == null) return;

                var credenciales = _dbService.ObtenerCredencialesMercadoPago();

                var txtAccessToken = LogicalTreeHelper.FindLogicalNode(panel, "TxtMercadoPagoAccessToken") as PasswordBox;
                var txtPublicKey = LogicalTreeHelper.FindLogicalNode(panel, "TxtMercadoPagoPublicKey") as PasswordBox;

                if (txtAccessToken != null) txtAccessToken.Password = credenciales["AccessToken"];
                if (txtPublicKey != null) txtPublicKey.Password = credenciales["PublicKey"];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar credenciales Mercado Pago: {ex.Message}");
            }
        }

        private void BtnMercadoPagoGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var panel = _panelesModulos.ContainsKey("MERCADOPAGO") ? _panelesModulos["MERCADOPAGO"] : null;
                if (panel == null) return;

                var txtAccessToken = LogicalTreeHelper.FindLogicalNode(panel, "TxtMercadoPagoAccessToken") as PasswordBox;
                var txtPublicKey = LogicalTreeHelper.FindLogicalNode(panel, "TxtMercadoPagoPublicKey") as PasswordBox;

                if (txtAccessToken == null || txtPublicKey == null) return;

                string accessToken = txtAccessToken.Password ?? "";
                string publicKey = txtPublicKey.Password ?? "";

                _dbService.GuardarCredencialesMercadoPago(accessToken, publicKey);
                MessageBox.Show("Credenciales guardadas correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar credenciales: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnMercadoPagoProbar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var panel = _panelesModulos.ContainsKey("MERCADOPAGO") ? _panelesModulos["MERCADOPAGO"] : null;
                if (panel == null) return;

                var txtAccessToken = LogicalTreeHelper.FindLogicalNode(panel, "TxtMercadoPagoAccessToken") as PasswordBox;
                var txtPublicKey = LogicalTreeHelper.FindLogicalNode(panel, "TxtMercadoPagoPublicKey") as PasswordBox;

                if (txtAccessToken == null || txtPublicKey == null) return;

                string accessToken = txtAccessToken.Password ?? "";
                string publicKey = txtPublicKey.Password ?? "";

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    MessageBox.Show("Por favor, ingrese el Access Token antes de probar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Deshabilitar botón mientras se prueba
                if (sender is Button btn)
                {
                    btn.IsEnabled = false;
                    btn.Content = "Probando...";
                }

                // Crear pago de prueba de 15 pesos (monto mínimo de Mercado Pago)
                string qrData = await GenerarQRMercadoPagoPrueba(accessToken, 15m, "prueba de integracion");

                if (!string.IsNullOrEmpty(qrData))
                {
                    // Mostrar ventana con el QR
                    var ventanaQR = new Window
                    {
                        Title = "QR de Prueba - Mercado Pago",
                        Width = 500,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = this
                    };

                    var stackPanel = new StackPanel
                    {
                        Margin = new Thickness(20),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    var txtTitulo = new TextBlock
                    {
                        Text = "QR de Prueba",
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    stackPanel.Children.Add(txtTitulo);

                    var txtInfo = new TextBlock
                    {
                        Text = "Monto: $15.00\nDescripción: prueba de integracion",
                        FontSize = 14,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 0, 0, 20),
                        TextAlignment = TextAlignment.Center
                    };
                    stackPanel.Children.Add(txtInfo);

                    string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=400x400&data={Uri.EscapeDataString(qrData)}";
                    var imgQR = new System.Windows.Controls.Image
                    {
                        Source = new BitmapImage(new Uri(qrUrl)),
                        Width = 400,
                        Height = 400,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    stackPanel.Children.Add(imgQR);

                    var btnCerrar = new Button
                    {
                        Content = "Cerrar",
                        Width = 100,
                        Margin = new Thickness(0, 20, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    btnCerrar.Click += (s, args) => ventanaQR.Close();
                    stackPanel.Children.Add(btnCerrar);

                    ventanaQR.Content = stackPanel;
                    ventanaQR.ShowDialog();

                    MessageBox.Show("Las credenciales funcionan correctamente. El QR de prueba se ha generado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Error al generar el QR de prueba. Verifique que las credenciales sean correctas.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al probar credenciales: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Rehabilitar botón
                if (sender is Button btn)
                {
                    btn.IsEnabled = true;
                    btn.Content = "Probar Credenciales";
                }
            }
        }

        private async Task<string> GenerarQRMercadoPagoPrueba(string accessToken, decimal monto, string descripcion)
        {
            try
            {
                string externalPosId = "LOJ001POS001";
                string idempotencyKey = Guid.NewGuid().ToString();
                string totalAmount = monto.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                string amount = monto.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
                string externalReference = $"PRUEBA_{DateTime.Now:yyyyMMddHHmmss}";

                var requestBody = new
                {
                    type = "qr",
                    total_amount = totalAmount,
                    description = descripcion,
                    external_reference = externalReference,
                    expiration_time = "PT16M",
                    config = new
                    {
                        qr = new
                        {
                            external_pos_id = externalPosId,
                            mode = "dynamic"
                        }
                    },
                    transactions = new
                    {
                        payments = new[]
                        {
                            new
                            {
                                amount = amount
                            }
                        }
                    },
                    items = new[]
                    {
                        new
                        {
                            title = descripcion,
                            unit_price = amount,
                            quantity = 1,
                            unit_measure = "unit",
                            external_code = "PRUEBA_ITEM"
                        }
                    }
                };

                string jsonBody = System.Text.Json.JsonSerializer.Serialize(requestBody);

                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                httpClient.DefaultRequestHeaders.Add("X-Idempotency-Key", idempotencyKey);

                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync("https://api.mercadopago.com/v1/orders", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = System.Text.Json.JsonDocument.Parse(responseBody);
                    
                    if (jsonDoc.RootElement.TryGetProperty("type_response", out var typeResponse) &&
                        typeResponse.TryGetProperty("qr_data", out var qrData))
                    {
                        return qrData.GetString() ?? "";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Error en API Mercado Pago: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar QR Mercado Pago: {ex.Message}");
            }

            return "";
        }

        private void IniciarServidorPantallaCliente()
        {
            try
            {
                // Detener servidor anterior si existe - limpiar completamente
                if (_httpListener != null)
                {
                    try
                    {
                        if (_httpListener.IsListening)
                        {
                            _httpListener.Stop();
                        }
                    }
                    catch { }
                    try
                    {
                        _httpListener.Close();
                    }
                    catch { }
                    _httpListener = null;
                }

                // Pequeña pausa para asegurar que el servidor anterior se haya cerrado completamente
                System.Threading.Thread.Sleep(100);

                // Obtener puerto guardado o generar uno nuevo
                var puertoGuardado = _dbService.ObtenerPuertoPantallaCliente();
                if (puertoGuardado.HasValue && puertoGuardado.Value >= 10000 && puertoGuardado.Value < 100000)
                {
                    _puertoPantallaCliente = puertoGuardado.Value;
                    System.Diagnostics.Debug.WriteLine($"Puerto cargado desde DB: {_puertoPantallaCliente}");
                }
                else
                {
                    // Generar puerto aleatorio de 5 dígitos (10000-99999)
                    var random = new Random();
                    _puertoPantallaCliente = random.Next(10000, 100000);
                    // Guardar el puerto en la base de datos
                    _dbService.GuardarPuertoPantallaCliente(_puertoPantallaCliente);
                    System.Diagnostics.Debug.WriteLine($"Puerto nuevo generado y guardado: {_puertoPantallaCliente}");
                }

                // Verificar si el puerto está disponible
                if (!EsPuertoDisponible(_puertoPantallaCliente))
                {
                    System.Diagnostics.Debug.WriteLine($"Puerto {_puertoPantallaCliente} no está disponible, generando uno nuevo...");
                    var random = new Random();
                    int intentos = 0;
                    do
                    {
                        _puertoPantallaCliente = random.Next(10000, 100000);
                        intentos++;
                    } while (!EsPuertoDisponible(_puertoPantallaCliente) && intentos < 10);
                    
                    if (intentos >= 10)
                    {
                        throw new Exception("No se pudo encontrar un puerto disponible después de 10 intentos");
                    }
                    
                    _dbService.GuardarPuertoPantallaCliente(_puertoPantallaCliente);
                    System.Diagnostics.Debug.WriteLine($"Nuevo puerto asignado: {_puertoPantallaCliente}");
                }

                // Obtener IP local de la red
                string ipLocal = ObtenerIPLocal();
                if (string.IsNullOrEmpty(ipLocal))
                {
                    System.Diagnostics.Debug.WriteLine("No se pudo obtener IP local, intentando método alternativo...");
                    // Si no se puede obtener IP, intentar obtenerla de otra forma
                    try
                    {
                        var host = Dns.GetHostEntry(Dns.GetHostName());
                        foreach (var ip in host.AddressList)
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                            {
                                string ipStr = ip.ToString();
                                if (!ipStr.StartsWith("169.254."))
                                {
                                    ipLocal = ipStr;
                                    System.Diagnostics.Debug.WriteLine($"IP local obtenida (método alternativo): {ipLocal}");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error en método alternativo: {ex.Message}");
                    }
                    
                    if (string.IsNullOrEmpty(ipLocal))
                    {
                        System.Diagnostics.Debug.WriteLine("No se pudo obtener IP local, usando localhost (solo funcionará en la misma máquina)");
                        ipLocal = "localhost";
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"IP local obtenida: {ipLocal}");
                }

                _urlPantallaCliente = $"http://{ipLocal}:{_puertoPantallaCliente}";
                System.Diagnostics.Debug.WriteLine($"URL pantalla cliente: {_urlPantallaCliente}");

                // Crear nuevo HttpListener
                _httpListener = new HttpListener();
                
                // Intentar usar el prefijo + (todas las interfaces) para permitir acceso desde la red
                // Esto requiere permisos especiales en Windows, así que intentaremos registrarlo con netsh
                string prefix = $"http://+:{_puertoPantallaCliente}/";
                bool servidorIniciado = false;
                
                System.Diagnostics.Debug.WriteLine($"Intentando iniciar servidor con todas las interfaces: {prefix}");
                
                try
                {
                    // Limpiar cualquier prefijo existente antes de agregar uno nuevo
                    if (_httpListener.Prefixes.Count > 0)
                    {
                        _httpListener.Prefixes.Clear();
                    }
                    
                    _httpListener.Prefixes.Add(prefix);
                    _httpListener.Start();
                    servidorIniciado = true;
                    System.Diagnostics.Debug.WriteLine($"Servidor iniciado correctamente en todas las interfaces: {prefix}");
                }
                catch (Exception exStart)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al iniciar con +: {exStart.Message}");
                    // Intentar registrar la URL con netsh para obtener permisos
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Intentando registrar URL con netsh...");
                        string urlParaRegistrar = $"http://+:{_puertoPantallaCliente}/";
                        string usuario = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
                        
                        var processInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"http add urlacl url={urlParaRegistrar} user={usuario}",
                            Verb = "runas", // Solicitar permisos de administrador
                            UseShellExecute = true,
                            CreateNoWindow = true,
                            WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                        };
                        
                        var process = System.Diagnostics.Process.Start(processInfo);
                        if (process != null)
                        {
                            process.WaitForExit();
                            System.Threading.Thread.Sleep(500); // Esperar a que se registre
                            
                            // Intentar iniciar de nuevo
                            if (_httpListener != null)
                            {
                                try
                                {
                                    _httpListener.Close();
                                }
                                catch { }
                            }
                            
                            _httpListener = new HttpListener();
                            if (_httpListener.Prefixes.Count > 0)
                            {
                                _httpListener.Prefixes.Clear();
                            }
                            _httpListener.Prefixes.Add(prefix);
                            _httpListener.Start();
                            servidorIniciado = true;
                            System.Diagnostics.Debug.WriteLine($"Servidor iniciado después de registrar URL con netsh");
                        }
                    }
                    catch (Exception exNetsh)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al registrar con netsh: {exNetsh.Message}");
                    }
                    
                    // Si aún falla, intentar con localhost y también con la IP específica
                    if (!servidorIniciado)
                    {
                        System.Diagnostics.Debug.WriteLine("Intentando con localhost y IP específica...");
                        try
                        {
                            if (_httpListener != null)
                            {
                                try
                                {
                                    _httpListener.Close();
                                }
                                catch { }
                            }
                            
                            _httpListener = new HttpListener();
                            
                            if (_httpListener.Prefixes.Count > 0)
                            {
                                _httpListener.Prefixes.Clear();
                            }
                            
                            // Agregar localhost primero (siempre funciona sin permisos)
                            string prefixLocalhost = $"http://localhost:{_puertoPantallaCliente}/";
                            _httpListener.Prefixes.Add(prefixLocalhost);
                            
                            // Intentar agregar también la IP específica si no es localhost
                            if (!string.IsNullOrEmpty(ipLocal) && ipLocal != "localhost")
                            {
                                try
                                {
                                    // Verificar que sea una IP válida y no loopback
                                    if (IPAddress.TryParse(ipLocal, out IPAddress? ipAddr) && !IPAddress.IsLoopback(ipAddr))
                                    {
                                        string prefixIP = $"http://{ipLocal}:{_puertoPantallaCliente}/";
                                        _httpListener.Prefixes.Add(prefixIP);
                                        System.Diagnostics.Debug.WriteLine($"Agregado prefijo con IP específica: {prefixIP}");
                                    }
                                }
                                catch (Exception exIP)
                                {
                                    System.Diagnostics.Debug.WriteLine($"No se pudo agregar prefijo con IP específica: {exIP.Message}");
                                    // Continuar solo con localhost
                                }
                            }
                            
                            _httpListener.Start();
                            servidorIniciado = true;
                            System.Diagnostics.Debug.WriteLine($"Servidor iniciado con localhost (y posiblemente IP específica)");
                            
                            // Verificar si se agregó la IP específica
                            bool tieneIPEspecifica = _httpListener.Prefixes.Any(p => p.Contains(ipLocal) && ipLocal != "localhost");
                            
                            if (!tieneIPEspecifica)
                            {
                                // Mostrar mensaje informativo al usuario solo si no se pudo agregar la IP
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    MessageBox.Show(
                                        "El servidor se inició solo en localhost debido a permisos insuficientes.\n\n" +
                                        "Para permitir acceso desde otros dispositivos en la red, ejecuta este comando como administrador:\n\n" +
                                        $"netsh http add urlacl url=http://+:{_puertoPantallaCliente}/ user={System.Security.Principal.WindowsIdentity.GetCurrent().Name}\n\n" +
                                        "O ejecuta la aplicación como administrador.\n\n" +
                                        "Nota: Si accedes desde otro dispositivo usando la IP, asegúrate de usar 'localhost' en la URL o configura los permisos necesarios.",
                                        "Información de Servidor",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                }), DispatcherPriority.Normal);
                            }
                        }
                        catch (Exception exLocalhost)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al iniciar con localhost: {exLocalhost.Message}");
                            // Limpiar el listener si falla
                            try
                            {
                                _httpListener.Close();
                            }
                            catch { }
                            _httpListener = null;
                            throw; // Re-lanzar para que se maneje en el catch externo
                        }
                    }
                }

                Task.Run(async () =>
                {
                    while (_httpListener != null && _httpListener.IsListening)
                    {
                        try
                        {
                            var context = await _httpListener.GetContextAsync();
                            await ProcesarRequestPantallaCliente(context);
                        }
                        catch (Exception ex)
                        {
                            if (_httpListener != null && _httpListener.IsListening)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error en servidor pantalla cliente: {ex.Message}");
                            }
                        }
                    }
                });

                // Actualizar URL en la UI y generar QR
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var panel = _panelesModulos.ContainsKey("MONITOR") ? _panelesModulos["MONITOR"] : null;
                        if (panel != null)
                        {
                            var txtURL = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaURL") as TextBlock;
                            if (txtURL != null)
                            {
                                txtURL.Text = _urlPantallaCliente;
                            }
                            // Generar QR después de un pequeño delay para asegurar que la URL esté establecida
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                GenerarQRCode();
                            }), DispatcherPriority.Loaded);
                            
                            // Actualizar instrucciones sobre segundo monitor
                            ActualizarInstruccionesSegundoMonitor(panel);
                        }
                        
                        // Abrir navegador en segundo monitor si existe
                        AbrirNavegadorEnSegundoMonitor();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al actualizar UI servidor: {ex.Message}");
                    }
                }), DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al iniciar servidor pantalla cliente: {ex.Message}");
                // Si falla con la IP específica, intentar con localhost como último recurso
                // (aunque esto solo funcionará en la misma máquina)
                try
                {
                    // Limpiar completamente el listener anterior
                    if (_httpListener != null)
                    {
                        try
                        {
                            if (_httpListener.IsListening)
                            {
                                _httpListener.Stop();
                            }
                        }
                        catch { }
                        try
                        {
                            _httpListener.Close();
                        }
                        catch { }
                        _httpListener = null;
                    }
                    
                    // Pequeña pausa para asegurar que se haya cerrado completamente
                    System.Threading.Thread.Sleep(100);
                    
                    // Verificar si el puerto está disponible, si no, generar uno nuevo
                    if (!EsPuertoDisponible(_puertoPantallaCliente))
                    {
                        System.Diagnostics.Debug.WriteLine($"Puerto {_puertoPantallaCliente} no disponible en catch, generando uno nuevo...");
                        var random = new Random();
                        int intentos = 0;
                        do
                        {
                            _puertoPantallaCliente = random.Next(10000, 100000);
                            intentos++;
                        } while (!EsPuertoDisponible(_puertoPantallaCliente) && intentos < 10);
                        
                        if (intentos >= 10)
                        {
                            throw new Exception("No se pudo encontrar un puerto disponible después de 10 intentos");
                        }
                        
                        _dbService.GuardarPuertoPantallaCliente(_puertoPantallaCliente);
                        System.Diagnostics.Debug.WriteLine($"Nuevo puerto asignado en catch: {_puertoPantallaCliente}");
                    }
                    
                    _httpListener = new HttpListener();
                    string prefix = $"http://localhost:{_puertoPantallaCliente}/";
                    
                    // Limpiar prefijos antes de agregar
                    if (_httpListener.Prefixes.Count > 0)
                    {
                        _httpListener.Prefixes.Clear();
                    }
                    
                    _httpListener.Prefixes.Add(prefix);
                    _httpListener.Start();
                    
                    // Actualizar URL a localhost
                    _urlPantallaCliente = $"http://localhost:{_puertoPantallaCliente}";
                    
                    Task.Run(async () =>
                    {
                        while (_httpListener != null && _httpListener.IsListening)
                        {
                            try
                            {
                                var context = await _httpListener.GetContextAsync();
                                await ProcesarRequestPantallaCliente(context);
                            }
                            catch (Exception ex2)
                            {
                                if (_httpListener != null && _httpListener.IsListening)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error en servidor pantalla cliente: {ex2.Message}");
                                }
                            }
                        }
                    });
                    
                    // Actualizar URL en la UI
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            var panel = _panelesModulos.ContainsKey("MONITOR") ? _panelesModulos["MONITOR"] : null;
                            if (panel != null)
                            {
                                var txtURL = LogicalTreeHelper.FindLogicalNode(panel, "TxtPantallaURL") as TextBlock;
                                if (txtURL != null)
                                {
                                    txtURL.Text = _urlPantallaCliente;
                                }
                                Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    GenerarQRCode();
                                }), DispatcherPriority.Loaded);
                                
                                // Actualizar instrucciones sobre segundo monitor
                                ActualizarInstruccionesSegundoMonitor(panel);
                            }
                            
                            // Abrir ventana WebBrowser en segundo monitor si existe
                            AbrirNavegadorEnSegundoMonitor();
                        }
                        catch (Exception ex3)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error al actualizar UI servidor: {ex3.Message}");
                        }
                    }), DispatcherPriority.Normal);
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Error al iniciar servidor con localhost: {ex2.Message}");
                    MessageBox.Show($"Error al iniciar servidor: {ex2.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private bool EsPuertoDisponible(int puerto)
        {
            try
            {
                using (var tcpListener = new TcpListener(IPAddress.Loopback, puerto))
                {
                    tcpListener.Start();
                    tcpListener.Stop();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private string ObtenerIPLocal()
        {
            try
            {
                // Obtener todas las interfaces de red
                var interfaces = NetworkInterface.GetAllNetworkInterfaces();
                
                // Primero intentar encontrar una IP que no sea APIPA
                foreach (var ni in interfaces)
                {
                    // Filtrar interfaces que están activas y no son loopback
                    if (ni.OperationalStatus == OperationalStatus.Up &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    {
                        var props = ni.GetIPProperties();
                        foreach (var addr in props.UnicastAddresses)
                        {
                            // Obtener IPv4 que no sea loopback
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(addr.Address))
                            {
                                string ipStr = addr.Address.ToString();
                                // Preferir direcciones que no sean APIPA (169.254.x.x)
                                if (!ipStr.StartsWith("169.254."))
                                {
                                    System.Diagnostics.Debug.WriteLine($"IP local encontrada: {ipStr}");
                                    return ipStr;
                                }
                            }
                        }
                    }
                }
                
                // Si no se encontró una IP válida, intentar método alternativo
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(ip))
                        {
                            string ipStr = ip.ToString();
                            if (!ipStr.StartsWith("169.254."))
                            {
                                System.Diagnostics.Debug.WriteLine($"IP local encontrada (método alternativo): {ipStr}");
                                return ipStr;
                            }
                        }
                    }
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en método alternativo de IP: {ex2.Message}");
                }
                
                // Si aún no se encontró, intentar obtener la primera IP no loopback disponible
                foreach (var ni in interfaces)
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        var props = ni.GetIPProperties();
                        foreach (var addr in props.UnicastAddresses)
                        {
                            if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                                !IPAddress.IsLoopback(addr.Address))
                            {
                                string ipStr = addr.Address.ToString();
                                System.Diagnostics.Debug.WriteLine($"IP local encontrada (último recurso): {ipStr}");
                                return ipStr;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener IP local: {ex.Message}");
            }
            
            System.Diagnostics.Debug.WriteLine("No se pudo obtener IP local, se usará localhost");
            return null;
        }

        private async Task ProcesarRequestPantallaCliente(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            if (request.Url == null || response.OutputStream == null)
            {
                return;
            }

            // Manejar endpoint de estado (para polling)
            if (request.Url.AbsolutePath == "/estado")
            {
                response.ContentType = "application/json; charset=utf-8";
                response.AddHeader("Access-Control-Allow-Origin", "*");
                
                // Obtener configuración actualizada de la DB
                var configEstado = _dbService.ObtenerConfiguracionPantallaCliente();
                
                // Escapar correctamente para JSON
                string matriculaEscapada = EscaparJSON(_matriculaPantallaCliente ?? "");
                string metodoPagoEscapado = EscaparJSON(_metodoPagoPantallaCliente ?? "");
                string bienvenida1 = EscaparJSON(configEstado != null && configEstado.ContainsKey("BienvenidaLinea1") ? configEstado["BienvenidaLinea1"] : "");
                string bienvenida2 = EscaparJSON(configEstado != null && configEstado.ContainsKey("BienvenidaLinea2") ? configEstado["BienvenidaLinea2"] : "");
                string agradecimiento1 = EscaparJSON(configEstado != null && configEstado.ContainsKey("AgradecimientoLinea1") ? configEstado["AgradecimientoLinea1"] : "");
                string agradecimiento2 = EscaparJSON(configEstado != null && configEstado.ContainsKey("AgradecimientoLinea2") ? configEstado["AgradecimientoLinea2"] : "");
                string cobro = EscaparJSON(configEstado != null && configEstado.ContainsKey("CobroAclaracion") ? configEstado["CobroAclaracion"] : "");
                string qrMercadoPago = EscaparJSON(_qrMercadoPago ?? "");
                
                // Normalizar estado para el JSON (cobro_reload -> cobro)
                string estadoParaJson = _estadoPantallaCliente == "cobro_reload" ? "cobro" : _estadoPantallaCliente;
                
                var estadoJson = $@"{{
    ""estado"": ""{estadoParaJson}"",
    ""matricula"": ""{matriculaEscapada}"",
    ""importe"": {_importePantallaCliente.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)},
    ""metodoPago"": ""{metodoPagoEscapado}"",
    ""bienvenidaLinea1"": ""{bienvenida1}"",
    ""bienvenidaLinea2"": ""{bienvenida2}"",
    ""agradecimientoLinea1"": ""{agradecimiento1}"",
    ""agradecimientoLinea2"": ""{agradecimiento2}"",
    ""cobroAclaracion"": ""{cobro}"",
    ""qrMercadoPago"": ""{qrMercadoPago}""
}}";
                
                byte[] estadoBuffer = Encoding.UTF8.GetBytes(estadoJson);
                response.ContentLength64 = estadoBuffer.Length;
                await response.OutputStream.WriteAsync(estadoBuffer, 0, estadoBuffer.Length);
                response.OutputStream.Close();
                return;
            }

            // Servir HTML normal
            response.ContentType = "text/html; charset=utf-8";

            var config = _dbService.ObtenerConfiguracionPantallaCliente();
            string html = "";

            if (_estadoPantallaCliente == "cobro" || _estadoPantallaCliente == "cobro_reload")
            {
                html = GenerarHTMLCobro(config);
            }
            else if (_estadoPantallaCliente == "agradecimiento")
            {
                html = GenerarHTMLAgradecimiento(config);
            }
            else
            {
                html = GenerarHTMLBienvenida(config);
            }

            byte[] htmlBuffer = Encoding.UTF8.GetBytes(html);
            response.ContentLength64 = htmlBuffer.Length;
            await response.OutputStream.WriteAsync(htmlBuffer, 0, htmlBuffer.Length);
            response.OutputStream.Close();
        }

        private string GenerarHTMLBienvenida(Dictionary<string, string> config)
        {
            string linea1 = config.ContainsKey("BienvenidaLinea1") ? config["BienvenidaLinea1"] : "";
            string linea2 = config.ContainsKey("BienvenidaLinea2") ? config["BienvenidaLinea2"] : "";
            
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <title>Pantalla Cliente</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{
            width: 100%;
            height: 100%;
            overflow: hidden;
        }}
        body {{ 
            background-color: #000000; 
            color: #FFFFFF; 
            font-family: Arial, sans-serif; 
            display: flex; 
            flex-direction: column; 
            justify-content: center; 
            align-items: center; 
            min-height: 100vh;
            width: 100vw;
            text-align: center;
            padding: 20px;
            margin: 0;
        }}
        .linea1 {{ font-size: 24px; margin-bottom: 20px; }}
        .linea2 {{ font-size: 48px; font-weight: bold; }}
        .footer {{ 
            position: fixed; 
            bottom: 20px; 
            width: 100%; 
            text-align: center; 
            font-size: 12px; 
            color: #666;
        }}
    </style>
    <script>
        let estadoActual = 'bienvenida';
        let bienvenidaLinea1Actual = '{System.Security.SecurityElement.Escape(linea1)}';
        let bienvenidaLinea2Actual = '{System.Security.SecurityElement.Escape(linea2)}';
        function verificarEstado() {{
            fetch('/estado')
                .then(response => response.json())
                .then(data => {{
                    if (data.estado !== estadoActual) {{
                        estadoActual = data.estado;
                        location.reload();
                        return;
                    }}
                    // Verificar si los textos han cambiado
                    if (data.bienvenidaLinea1 !== bienvenidaLinea1Actual || data.bienvenidaLinea2 !== bienvenidaLinea2Actual) {{
                        location.reload();
                    }}
                }})
                .catch(function(err) {{ console.error('Error al verificar estado:', err); }});
        }}
        setInterval(verificarEstado, 500);
    </script>
</head>
<body>
    <div class='linea1'>{System.Security.SecurityElement.Escape(linea1)}</div>
    <div class='linea2'>{System.Security.SecurityElement.Escape(linea2)}</div>
    <div class='footer'>© 2025 Desarrollado con pasión por Parking Co.</div>
</body>
</html>";
        }

        private string EscaparJavaScript(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private string EscaparJSON(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            return texto.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private void ActualizarInstruccionesSegundoMonitor(Grid? panel)
        {
            try
            {
                if (panel == null) return;
                
                var txtInstruccionMonitor = LogicalTreeHelper.FindLogicalNode(panel, "TxtInstruccionMonitor") as TextBlock;
                var chkMostrarEnSegundoMonitor = LogicalTreeHelper.FindLogicalNode(panel, "ChkMostrarEnSegundoMonitor") as CheckBox;
                if (txtInstruccionMonitor == null || chkMostrarEnSegundoMonitor == null) return;

                // Verificar si hay múltiples monitores usando APIs de Windows
                var monitores = ObtenerMonitores();
                if (monitores != null && monitores.Count >= 2)
                {
                    var segundoMonitor = monitores[1];
                    string resolucion = $"{segundoMonitor.Width}x{segundoMonitor.Height}";
                    
                    txtInstruccionMonitor.Text = $"🖥️ Segundo Monitor Detectado: Se ha detectado un segundo monitor ({resolucion}).";
                    txtInstruccionMonitor.Visibility = Visibility.Visible;
                    
                    // Cargar estado del checkbox desde la DB
                    bool mostrarEnSegundoMonitor = _dbService.ObtenerMostrarEnSegundoMonitor();
                    chkMostrarEnSegundoMonitor.IsChecked = mostrarEnSegundoMonitor;
                    chkMostrarEnSegundoMonitor.Visibility = Visibility.Visible;
                }
                else
                {
                    txtInstruccionMonitor.Visibility = Visibility.Collapsed;
                    chkMostrarEnSegundoMonitor.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar instrucciones segundo monitor: {ex.Message}");
            }
        }

        private void ChkMostrarEnSegundoMonitor_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                var checkbox = sender as CheckBox;
                if (checkbox == null) return;

                bool mostrar = checkbox.IsChecked == true;
                _dbService.GuardarMostrarEnSegundoMonitor(mostrar);

                // Si se activa y el servidor está corriendo, abrir la ventana inmediatamente
                if (mostrar && _httpListener != null && _httpListener.IsListening && !string.IsNullOrEmpty(_urlPantallaCliente))
                {
                    AbrirNavegadorEnSegundoMonitor();
                }
                // Si se desactiva, cerrar la ventana si está abierta
                else if (!mostrar && _ventanaPantallaCliente != null)
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _ventanaPantallaCliente.Close();
                        });
                    }
                    catch { }
                    _ventanaPantallaCliente = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cambiar preferencia mostrar en segundo monitor: {ex.Message}");
            }
        }

        private void AbrirNavegadorEnSegundoMonitor()
        {
            try
            {
                // Verificar si la opción está activada
                bool mostrarEnSegundoMonitor = _dbService.ObtenerMostrarEnSegundoMonitor();
                if (!mostrarEnSegundoMonitor)
                {
                    System.Diagnostics.Debug.WriteLine("Mostrar en segundo monitor está desactivado, no se abrirá la ventana automáticamente");
                    // Cerrar ventana si existe
                    if (_ventanaPantallaCliente != null)
                    {
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _ventanaPantallaCliente.Close();
                            });
                        }
                        catch { }
                        _ventanaPantallaCliente = null;
                    }
                    return;
                }

                // Verificar si hay múltiples monitores usando APIs de Windows
                var monitores = ObtenerMonitores();
                if (monitores == null || monitores.Count < 2)
                {
                    System.Diagnostics.Debug.WriteLine("No se detectó un segundo monitor, no se abrirá la ventana automáticamente");
                    // Cerrar ventana si existe y ya no hay segundo monitor
                    if (_ventanaPantallaCliente != null)
                    {
                        try
                        {
                            Dispatcher.Invoke(() =>
                            {
                                _ventanaPantallaCliente.Close();
                            });
                        }
                        catch { }
                        _ventanaPantallaCliente = null;
                    }
                    return;
                }

                // Obtener el segundo monitor (índice 1)
                var segundoMonitor = monitores[1];
                System.Diagnostics.Debug.WriteLine($"Segundo monitor detectado: {segundoMonitor.Width}x{segundoMonitor.Height} en ({segundoMonitor.X}, {segundoMonitor.Y})");

                if (string.IsNullOrEmpty(_urlPantallaCliente))
                {
                    System.Diagnostics.Debug.WriteLine("URL pantalla cliente está vacía, no se puede abrir ventana");
                    return;
                }

                // Crear o actualizar la ventana con WebBrowser en el segundo monitor
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // Si la ventana ya existe, solo actualizar la URL
                        if (_ventanaPantallaCliente != null)
                        {
                            try
                            {
                                _ventanaPantallaCliente.CargarURL(_urlPantallaCliente);
                                System.Diagnostics.Debug.WriteLine($"URL actualizada en ventana WebBrowser existente: {_urlPantallaCliente}");
                            }
                            catch
                            {
                                // Si falla, cerrar y crear nueva
                                try { _ventanaPantallaCliente.Close(); } catch { }
                                _ventanaPantallaCliente = null;
                            }
                        }
                        
                        // Si no existe, crear nueva ventana
                        if (_ventanaPantallaCliente == null)
                        {
                            _ventanaPantallaCliente = new PantallaClienteWindow();
                            // Cargar URL antes de mostrar
                            _ventanaPantallaCliente.CargarURL(_urlPantallaCliente);
                            // Mostrar la ventana primero
                            _ventanaPantallaCliente.Show();
                            // Posicionar DESPUÉS de mostrar (el evento Loaded lo manejará)
                            // También forzar posicionamiento después de un pequeño delay
                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                _ventanaPantallaCliente?.PosicionarEnSegundoMonitor();
                            }), DispatcherPriority.Loaded);
                            // Asegurar que esté en primer plano
                            _ventanaPantallaCliente.Activate();
                            
                            System.Diagnostics.Debug.WriteLine($"Ventana WebBrowser creada y mostrada en segundo monitor con URL: {_urlPantallaCliente}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al crear/actualizar ventana WebBrowser: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al abrir ventana WebBrowser en segundo monitor: {ex.Message}");
            }
        }

        private string GenerarHTMLCobro(Dictionary<string, string> config)
        {
            string metodoPago = _metodoPagoPantallaCliente ?? "";
            string matricula = _matriculaPantallaCliente ?? "";
            string metodoPagoDisplay = System.Security.SecurityElement.Escape(metodoPago.ToUpper());
            string matriculaEscapada = System.Security.SecurityElement.Escape(matricula);
            // Asegurar que el importe siempre sea un número válido
            decimal importeValido = _importePantallaCliente >= 0 ? _importePantallaCliente : 0m;
            string importeFormateado = "$" + importeValido.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);
            string aclaracionTexto = config.ContainsKey("CobroAclaracion") ? config["CobroAclaracion"] : "";
            string aclaracion = !string.IsNullOrEmpty(aclaracionTexto) ? $"<div style='margin-top: 20px; font-size: 18px;'>{System.Security.SecurityElement.Escape(aclaracionTexto)}</div>" : "";
            
            // Verificar si es MercadoPago y hay QR
            bool esMercadoPago = metodoPago.Equals("MercadoPago", StringComparison.OrdinalIgnoreCase);
            string qrDataEscapado = System.Security.SecurityElement.Escape(_qrMercadoPago ?? "");
            
            // Verificar que el módulo esté habilitado y haya credenciales
            bool moduloHabilitado = false;
            bool tieneCredenciales = false;
            try
            {
                var modulos = _dbService.ObtenerModulos();
                moduloHabilitado = modulos.ContainsKey("MERCADOPAGO") && modulos["MERCADOPAGO"];
                
                if (moduloHabilitado)
                {
                    var credenciales = _dbService.ObtenerCredencialesMercadoPago();
                    tieneCredenciales = !string.IsNullOrWhiteSpace(credenciales["AccessToken"]);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar módulo MercadoPago: {ex.Message}");
            }
            
            bool mostrarQR = esMercadoPago && !string.IsNullOrEmpty(_qrMercadoPago) && moduloHabilitado && tieneCredenciales;
            
            System.Diagnostics.Debug.WriteLine($"GenerarHTMLCobro - metodoPago: {metodoPago}, esMercadoPago: {esMercadoPago}, tieneQR: {!string.IsNullOrEmpty(_qrMercadoPago)}, mostrarQR: {mostrarQR}");
            
            // Generar QR usando API online con el qr_data de MercadoPago
            string qrUrl = mostrarQR ? $"https://api.qrserver.com/v1/create-qr-code/?size=500x500&data={Uri.EscapeDataString(_qrMercadoPago)}" : "";
            string qrHtml = "";
            string rightPanelHtml = "";
            
            if (mostrarQR)
            {
                qrHtml = $@"
        <div class='qr-container'>
            <img src='{qrUrl}' alt='QR MercadoPago' class='qr-image' />
            <div class='qr-text'>Escanea con Mercado Pago</div>
        </div>";
                rightPanelHtml = $@"<div class='right-panel'>{qrHtml}</div>";
                System.Diagnostics.Debug.WriteLine($"QR HTML generado, URL: {qrUrl.Substring(0, Math.Min(100, qrUrl.Length))}...");
            }

            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <title>Pantalla Cliente</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{
            width: 100%;
            height: 100%;
            overflow: hidden;
        }}
        body {{ 
            background-color: #000000; 
            color: #FFFFFF; 
            font-family: Arial, sans-serif; 
            min-height: 100vh;
            width: 100vw;
            margin: 0;
            padding: 0;
            display: flex;
            flex-direction: row;
        }}
        .container {{
            display: flex;
            flex-direction: row;
            width: 100%;
            height: 100vh;
        }}
        .left-panel {{
            flex: 1;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            text-align: center;
            padding: 40px;
        }}
        .right-panel {{
            flex: 1;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            text-align: center;
            padding: 40px;
            border-left: 2px solid #333;
        }}
        .codigo {{ font-size: 48px; font-weight: bold; margin-bottom: 30px; letter-spacing: 4px; }}
        .total {{ font-size: 24px; margin-bottom: 10px; }}
        .importe {{ font-size: 72px; font-weight: bold; margin-bottom: 30px; }}
        .metodo {{ 
            border: 1px solid #666; 
            border-radius: 8px; 
            padding: 15px 30px; 
            font-size: 20px;
            display: inline-block;
            margin-bottom: 20px;
        }}
        .qr-container {{
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
        }}
        .qr-image {{
            width: 100%;
            max-width: 500px;
            height: auto;
            border: 3px solid #fff;
            border-radius: 12px;
            padding: 20px;
            background: #fff;
        }}
        .qr-text {{
            margin-top: 20px;
            font-size: 24px;
            color: #fff;
            font-weight: bold;
        }}
        .footer {{ 
            position: fixed; 
            bottom: 20px; 
            width: 100%; 
            text-align: center; 
            font-size: 12px; 
            color: #666;
        }}
    </style>
    <script>
        var estadoActual = 'cobro';
        var importeActual = {importeValido.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)};
        var metodoPagoActual = '{System.Security.SecurityElement.Escape(metodoPago)}';
        var cobroAclaracionActual = '{System.Security.SecurityElement.Escape(aclaracionTexto)}';
        var qrMercadoPagoActual = '{qrDataEscapado}';
        function actualizarPantalla(data) {{
            if (data.estado !== estadoActual) {{
                location.reload();
                return;
            }}
            if (data.estado === 'cobro') {{
                var nuevoImporte = parseFloat(data.importe) || 0;
                var nuevoMetodo = data.metodoPago || '';
                var nuevaAclaracion = data.cobroAclaracion || '';
                var nuevoQR = data.qrMercadoPago || '';
                
                var necesitaRecarga = false;
                
                // Verificar cambios en importe, método o aclaración
                if (Math.abs(nuevoImporte - importeActual) > 0.01 || nuevoMetodo !== metodoPagoActual || nuevaAclaracion !== cobroAclaracionActual) {{
                    necesitaRecarga = true;
                }}
                
                // Verificar cambios en QR - SIEMPRE recargar si cambia el QR
                if (nuevoQR !== qrMercadoPagoActual) {{
                    qrMercadoPagoActual = nuevoQR;
                    // Si hay QR nuevo y es MercadoPago, recargar para mostrarlo
                    if (nuevoMetodo.toLowerCase() === 'mercadopago' && nuevoQR) {{
                        necesitaRecarga = true;
                    }} else if (nuevoMetodo.toLowerCase() !== 'mercadopago' || !nuevoQR) {{
                        // Si ya no es MercadoPago o no hay QR, recargar para quitar el panel
                        necesitaRecarga = true;
                    }}
                }}
                
                if (necesitaRecarga) {{
                    location.reload();
                }}
            }}
        }}
        function verificarEstado() {{
            fetch('/estado')
                .then(function(response) {{ return response.json(); }})
                .then(function(data) {{
                    actualizarPantalla(data);
                }})
                .catch(function(err) {{ console.error('Error al verificar estado:', err); }});
        }}
        verificarEstado();
        setInterval(verificarEstado, 200);
    </script>
</head>
<body>
    <div class='container'>
        <div class='left-panel'>
            <div class='codigo'>{matriculaEscapada}</div>
            <div class='total'>Total a Pagar</div>
            <div class='importe'>{importeFormateado}</div>
            <div class='metodo'>{metodoPagoDisplay}</div>
            {aclaracion}
        </div>
        {rightPanelHtml}
    </div>
    <div class='footer'>© 2025 Desarrollado con pasión por Parking Co.</div>
</body>
</html>";
        }

        private void GenerarQRCode()
        {
            try
            {
                if (string.IsNullOrEmpty(_urlPantallaCliente))
                {
                    System.Diagnostics.Debug.WriteLine("URL pantalla cliente está vacía, no se puede generar QR");
                    return;
                }

                var panel = _panelesModulos.ContainsKey("MONITOR") ? _panelesModulos["MONITOR"] : null;
                if (panel == null)
                {
                    System.Diagnostics.Debug.WriteLine("Panel MONITOR no encontrado");
                    return;
                }

                var imgQR = LogicalTreeHelper.FindLogicalNode(panel, "ImgPantallaQR") as System.Windows.Controls.Image;
                if (imgQR == null)
                {
                    System.Diagnostics.Debug.WriteLine("ImgPantallaQR no encontrado en el panel");
                    return;
                }

                // Usar API online para generar QR code
                string qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=200x200&data={Uri.EscapeDataString(_urlPantallaCliente)}";
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(qrUrl, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.DownloadCompleted += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"QR code generado correctamente para: {_urlPantallaCliente}");
                };
                bitmap.DownloadFailed += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine($"Error al descargar QR code: {e.ErrorException?.Message}");
                };
                bitmap.EndInit();
                imgQR.Source = bitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al generar QR code: {ex.Message}");
            }
        }

        private void DetenerServidorPantallaCliente()
        {
            try
            {
                if (_httpListener != null && _httpListener.IsListening)
                {
                    _httpListener.Stop();
                    _httpListener.Close();
                    _httpListener = null;
                }
                
                // Cerrar ventana WebBrowser si existe
                if (_ventanaPantallaCliente != null)
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _ventanaPantallaCliente.Close();
                        });
                    }
                    catch { }
                    _ventanaPantallaCliente = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al detener servidor pantalla cliente: {ex.Message}");
            }
        }

        private string GenerarHTMLAgradecimiento(Dictionary<string, string> config)
        {
            string linea1 = config.ContainsKey("AgradecimientoLinea1") ? config["AgradecimientoLinea1"] : "";
            string linea2 = config.ContainsKey("AgradecimientoLinea2") ? config["AgradecimientoLinea2"] : "";
            
            return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no'>
    <title>Pantalla Cliente</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{
            width: 100%;
            height: 100%;
            overflow: hidden;
        }}
        body {{ 
            background-color: #000000; 
            color: #FFFFFF; 
            font-family: Arial, sans-serif; 
            display: flex; 
            flex-direction: column; 
            justify-content: center; 
            align-items: center; 
            min-height: 100vh;
            width: 100vw;
            text-align: center;
            padding: 20px;
            margin: 0;
        }}
        .linea1 {{ font-size: 24px; margin-bottom: 20px; }}
        .linea2 {{ font-size: 48px; font-weight: bold; }}
        .footer {{ 
            position: fixed; 
            bottom: 20px; 
            width: 100%; 
            text-align: center; 
            font-size: 12px; 
            color: #666;
        }}
    </style>
    <script>
        let estadoActual = 'agradecimiento';
        let agradecimientoLinea1Actual = '{System.Security.SecurityElement.Escape(linea1)}';
        let agradecimientoLinea2Actual = '{System.Security.SecurityElement.Escape(linea2)}';
        function verificarEstado() {{
            fetch('/estado')
                .then(response => response.json())
                .then(data => {{
                    if (data.estado !== estadoActual) {{
                        estadoActual = data.estado;
                        location.reload();
                        return;
                    }}
                    // Verificar si los textos han cambiado
                    if (data.agradecimientoLinea1 !== agradecimientoLinea1Actual || data.agradecimientoLinea2 !== agradecimientoLinea2Actual) {{
                        location.reload();
                    }}
                }})
                .catch(function(err) {{ console.error('Error al verificar estado:', err); }});
        }}
        setInterval(verificarEstado, 500);
    </script>
</head>
<body>
    <div class='linea1'>{System.Security.SecurityElement.Escape(linea1)}</div>
    <div class='linea2'>{System.Security.SecurityElement.Escape(linea2)}</div>
    <div class='footer'>© 2025 Desarrollado con pasión por Parking Co.</div>
</body>
</html>";
        }

    }
}





