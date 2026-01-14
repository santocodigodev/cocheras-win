using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cocheras.Models;

namespace Cocheras.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseService()
        {
            // Base de datos en la carpeta del ejecutable
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            _dbPath = Path.Combine(appDirectory, "cocheras.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_dbPath))
            {
                SQLiteConnection.CreateFile(_dbPath);
            }

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Tabla de Administradores
            string createAdminTable = @"
                CREATE TABLE IF NOT EXISTS Administradores (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Apellido TEXT NOT NULL,
                    Username TEXT NOT NULL UNIQUE,
                    PasswordHash TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Rol TEXT NOT NULL DEFAULT 'operador',
                    FechaCreacion DATETIME NOT NULL
                )";

            // Tabla de Estacionamiento
            string createEstacionamientoTable = @"
                CREATE TABLE IF NOT EXISTS Estacionamiento (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Direccion TEXT,
                    Ciudad TEXT,
                    Pais TEXT NOT NULL,
                    Telefono TEXT,
                    Impresora TEXT,
                    FechaCreacion DATETIME NOT NULL
                )";

            // Tabla de Tickets
            string createTicketsTable = @"
                CREATE TABLE IF NOT EXISTS Tickets (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Matricula TEXT NOT NULL,
                    ImagenPath TEXT,
                    Descripcion TEXT,
                    NotaAdicional TEXT,
                    TarifaId INTEGER,
                    CategoriaId INTEGER,
                    FechaEntrada DATETIME NOT NULL,
                    FechaSalida DATETIME,
                    FechaCancelacion DATETIME,
                    EstaAbierto INTEGER NOT NULL DEFAULT 1,
                    EstaCancelado INTEGER NOT NULL DEFAULT 0,
                    MotivoCancelacion TEXT,
                    Monto REAL,
                    AdminCreadorId INTEGER,
                    AdminCerradorId INTEGER,
                    FechaCreacion DATETIME NOT NULL,
                    FOREIGN KEY (TarifaId) REFERENCES Tarifas(Id),
                    FOREIGN KEY (CategoriaId) REFERENCES Categorias(Id),
                    FOREIGN KEY (AdminCreadorId) REFERENCES Administradores(Id),
                    FOREIGN KEY (AdminCerradorId) REFERENCES Administradores(Id)
                )";

            // Tabla de Categorias
            string createCategoriasTable = @"
                CREATE TABLE IF NOT EXISTS Categorias (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Orden INTEGER NOT NULL,
                    FechaCreacion DATETIME NOT NULL
                )";

            // Tabla de Tarifas
            string createTarifasTable = @"
                CREATE TABLE IF NOT EXISTS Tarifas (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Tipo INTEGER NOT NULL DEFAULT 1,
                    Dias INTEGER NOT NULL DEFAULT 0,
                    Horas INTEGER NOT NULL DEFAULT 0,
                    Minutos INTEGER NOT NULL DEFAULT 0,
                    Tolerancia INTEGER NOT NULL DEFAULT 0,
                    FechaCreacion DATETIME NOT NULL
                )";

            // Tabla de Precios
            string createPreciosTable = @"
                CREATE TABLE IF NOT EXISTS Precios (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TarifaId INTEGER NOT NULL,
                    CategoriaId INTEGER NOT NULL,
                    Monto REAL NOT NULL DEFAULT 0,
                    FechaCreacion DATETIME NOT NULL,
                    FOREIGN KEY (TarifaId) REFERENCES Tarifas(Id),
                    FOREIGN KEY (CategoriaId) REFERENCES Categorias(Id),
                    UNIQUE(TarifaId, CategoriaId)
                )";

            // Tabla de Accesos (para registrar últimos accesos)
            string createAccesosTable = @"
                CREATE TABLE IF NOT EXISTS Accesos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AdminId INTEGER NOT NULL,
                    FechaAcceso DATETIME NOT NULL,
                    FOREIGN KEY (AdminId) REFERENCES Administradores(Id)
                )";

            // Tabla de Formas de Pago
            string createFormasPagoTable = @"
                CREATE TABLE IF NOT EXISTS FormasPago (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    FechaCreacion DATETIME NOT NULL
                )";

            // Tabla de Transacciones
            string createTransaccionesTable = @"
                CREATE TABLE IF NOT EXISTS Transacciones (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    TicketId INTEGER NOT NULL,
                    AdminId INTEGER,
                    FormaPagoId INTEGER NOT NULL,
                    Importe REAL NOT NULL,
                    Fecha DATETIME NOT NULL,
                    Descripcion TEXT,
                    ConRecibo INTEGER NOT NULL DEFAULT 0,
                    EsSalidaInmediata INTEGER NOT NULL DEFAULT 0,
                    ItemsDetalle TEXT,
                    FOREIGN KEY (TicketId) REFERENCES Tickets(Id),
                    FOREIGN KEY (AdminId) REFERENCES Administradores(Id),
                    FOREIGN KEY (FormaPagoId) REFERENCES FormasPago(Id)
                )";

            // Tabla Clientes Mensuales
            string createClientesMensualesTable = @"
                CREATE TABLE IF NOT EXISTS ClientesMensuales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Apellido TEXT NOT NULL,
                    Whatsapp TEXT,
                    Email TEXT,
                    DNI TEXT,
                    CUIT TEXT,
                    Direccion TEXT,
                    Nota TEXT,
                    Activo INTEGER NOT NULL DEFAULT 1,
                    FechaCreacion DATETIME NOT NULL
                )";

            // Tabla Vehiculos Mensuales
            string createVehiculosMensualesTable = @"
                CREATE TABLE IF NOT EXISTS VehiculosMensuales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClienteId INTEGER NOT NULL,
                    Matricula TEXT NOT NULL,
                    MarcaModelo TEXT,
                    CategoriaId INTEGER,
                    TarifaId INTEGER,
                    Alternativo INTEGER NOT NULL DEFAULT 0,
                    PrecioDiferenciado INTEGER NOT NULL DEFAULT 0,
                    ProximoCargo DATETIME,
                    BonificarHastaFinDeMes INTEGER NOT NULL DEFAULT 0,
                    CargoProporcional INTEGER NOT NULL DEFAULT 1,
                    CargoMesCompleto INTEGER NOT NULL DEFAULT 0,
                    FechaCreacion DATETIME NOT NULL,
                    FOREIGN KEY (ClienteId) REFERENCES ClientesMensuales(Id),
                    FOREIGN KEY (CategoriaId) REFERENCES Categorias(Id),
                    FOREIGN KEY (TarifaId) REFERENCES Tarifas(Id)
                )";

            // Tabla Movimientos Mensuales
            string createMovimientosMensualesTable = @"
                CREATE TABLE IF NOT EXISTS MovimientosMensuales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClienteId INTEGER NOT NULL,
                    VehiculoId INTEGER,
                    Tipo TEXT NOT NULL,
                    Importe REAL NOT NULL,
                    Descripcion TEXT,
                    Fecha DATETIME NOT NULL,
                    FormaPagoId INTEGER,
                    AdminId INTEGER NOT NULL,
                    EsRecibo INTEGER NOT NULL DEFAULT 0,
                    MatriculaReferencia TEXT,
                    MesAplicado TEXT,
                    FOREIGN KEY (ClienteId) REFERENCES ClientesMensuales(Id),
                    FOREIGN KEY (VehiculoId) REFERENCES VehiculosMensuales(Id),
                    FOREIGN KEY (FormaPagoId) REFERENCES FormasPago(Id),
                    FOREIGN KEY (AdminId) REFERENCES Administradores(Id)
                )";

            using var command1 = new SQLiteCommand(createAdminTable, connection);
            command1.ExecuteNonQuery();

            using var command2 = new SQLiteCommand(createEstacionamientoTable, connection);
            command2.ExecuteNonQuery();

            using var command3 = new SQLiteCommand(createTicketsTable, connection);
            command3.ExecuteNonQuery();

            using var command4 = new SQLiteCommand(createCategoriasTable, connection);
            command4.ExecuteNonQuery();

            using var command5 = new SQLiteCommand(createTarifasTable, connection);
            command5.ExecuteNonQuery();

            using var command6 = new SQLiteCommand(createPreciosTable, connection);
            command6.ExecuteNonQuery();

            using var command7 = new SQLiteCommand(createAccesosTable, connection);
            command7.ExecuteNonQuery();

            using var command8 = new SQLiteCommand(createFormasPagoTable, connection);
            command8.ExecuteNonQuery();

            using var command9 = new SQLiteCommand(createTransaccionesTable, connection);
            command9.ExecuteNonQuery();

            using var command10 = new SQLiteCommand(createClientesMensualesTable, connection);
            command10.ExecuteNonQuery();

            using var command11 = new SQLiteCommand(createVehiculosMensualesTable, connection);
            command11.ExecuteNonQuery();

            using var command12 = new SQLiteCommand(createMovimientosMensualesTable, connection);
            command12.ExecuteNonQuery();

            // Tabla de Módulos
            string createModulosTable = @"
                CREATE TABLE IF NOT EXISTS Modulos (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL UNIQUE,
                    EstaActivo INTEGER NOT NULL DEFAULT 0,
                    FechaCreacion DATETIME NOT NULL
                )";
            
            using var command13 = new SQLiteCommand(createModulosTable, connection);
            command13.ExecuteNonQuery();

            // Tabla de Configuración Pantalla Cliente
            string createPantallaClienteTable = @"
                CREATE TABLE IF NOT EXISTS PantallaCliente (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    BienvenidaLinea1 TEXT NOT NULL DEFAULT 'Bienvenidos a',
                    BienvenidaLinea2 TEXT NOT NULL DEFAULT '',
                    AgradecimientoLinea1 TEXT NOT NULL DEFAULT '¡Gracias por su visita!',
                    AgradecimientoLinea2 TEXT NOT NULL DEFAULT '',
                    CobroAclaracion TEXT NOT NULL DEFAULT '',
                    Puerto INTEGER,
                    FechaCreacion DATETIME NOT NULL,
                    FechaActualizacion DATETIME NOT NULL
                )";
            
            using var command14 = new SQLiteCommand(createPantallaClienteTable, connection);
            command14.ExecuteNonQuery();

            // Tabla de Credenciales Mercado Pago
            string createMercadoPagoCredencialesTable = @"
                CREATE TABLE IF NOT EXISTS MercadoPagoCredenciales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    AccessToken TEXT NOT NULL DEFAULT '',
                    PublicKey TEXT NOT NULL DEFAULT '',
                    FechaCreacion DATETIME NOT NULL,
                    FechaActualizacion DATETIME NOT NULL
                )";
            
            using var command14b = new SQLiteCommand(createMercadoPagoCredencialesTable, connection);
            command14b.ExecuteNonQuery();

            // Tabla de Cámaras ANPR
            string createCamarasANPRTable = @"
                CREATE TABLE IF NOT EXISTS CamarasANPR (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Nombre TEXT NOT NULL,
                    Marca TEXT NOT NULL DEFAULT 'Dahua',
                    Tipo TEXT NOT NULL DEFAULT 'Entrada',
                    SentidoCirculacion TEXT NOT NULL DEFAULT 'Se acerca',
                    CapturaSinMatricula INTEGER NOT NULL DEFAULT 0,
                    EncuadreVehiculo INTEGER NOT NULL DEFAULT 0,
                    ConBarrerasVehiculares INTEGER NOT NULL DEFAULT 0,
                    RetardoAperturaSegundos INTEGER NOT NULL DEFAULT 0,
                    RetardoCierreSegundos INTEGER NOT NULL DEFAULT 0,
                    AperturaManual INTEGER NOT NULL DEFAULT 0,
                    SolicitarMotivoApertura INTEGER NOT NULL DEFAULT 0,
                    ToleranciaSalidaMinutos INTEGER NOT NULL DEFAULT 0,
                    PreIngresoActivo INTEGER NOT NULL DEFAULT 0,
                    ImpresoraId TEXT,
                    CategoriaPredeterminadaId INTEGER,
                    HostIP TEXT NOT NULL,
                    Usuario TEXT NOT NULL DEFAULT '',
                    Clave TEXT NOT NULL DEFAULT '',
                    Activa INTEGER NOT NULL DEFAULT 1,
                    FechaCreacion DATETIME NOT NULL,
                    FOREIGN KEY (CategoriaPredeterminadaId) REFERENCES Categorias(Id)
                )";
            
            using var command15 = new SQLiteCommand(createCamarasANPRTable, connection);
            command15.ExecuteNonQuery();
            
            // Tabla de Lista Blanca ANPR
            string createListaBlancaANPRTable = @"
                CREATE TABLE IF NOT EXISTS ListaBlancaANPR (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Matricula TEXT NOT NULL UNIQUE,
                    Descripcion TEXT,
                    FechaCreacion DATETIME NOT NULL
                )";
            
            using var command15b = new SQLiteCommand(createListaBlancaANPRTable, connection);
            command15b.ExecuteNonQuery();

            // Tabla de Lista Negra ANPR
            string createListaNegraANPRTable = @"
                CREATE TABLE IF NOT EXISTS ListaNegraANPR (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Matricula TEXT NOT NULL UNIQUE,
                    Descripcion TEXT,
                    FechaCreacion DATETIME NOT NULL
                )";
            
            using var command15c = new SQLiteCommand(createListaNegraANPRTable, connection);
            command15c.ExecuteNonQuery();
            
            // Tabla de Categorías Dahua (mapeo)
            string createCategoriasDahuaTable = @"
                CREATE TABLE IF NOT EXISTS CategoriasDahua (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Codigo TEXT NOT NULL UNIQUE,
                    Descripcion TEXT NOT NULL,
                    CategoriaId INTEGER,
                    FOREIGN KEY (CategoriaId) REFERENCES Categorias(Id)
                )";
            
            using var command16 = new SQLiteCommand(createCategoriasDahuaTable, connection);
            command16.ExecuteNonQuery();
            
            // Inicializar categorías Dahua por defecto
            InicializarCategoriasDahuaPorDefecto(connection);
            
            // Inicializar configuración por defecto si no existe
            InicializarPantallaClientePorDefecto(connection);
            
            // Inicializar módulos por defecto si no existen
            InicializarModulosPorDefecto(connection);
            
            // Verificar y agregar columna MesAplicado si no existe
            VerificarColumnasMovimientosMensuales(connection);
            
            // Verificar y agregar columna Puerto en PantallaCliente si no existe
            VerificarColumnaPuertoPantallaCliente(connection);

            // Verificar y agregar columna MostrarEnSegundoMonitor en PantallaCliente si no existe
            VerificarColumnaMostrarEnSegundoMonitor(connection);

            // Verificar y agregar columna SolicitarMotivoApertura en CamarasANPR si no existe
            VerificarColumnaSolicitarMotivoApertura(connection);

            // Ejecutar migraciones para bases de datos existentes
            EjecutarMigraciones(connection);
            
            // Migración para actualizar tabla Tarifas si es necesario
            Migracion_ActualizarTarifas(connection);

            // Inicializar categorías por defecto si no existen
            InicializarCategoriasPorDefecto(connection);
            
            // Inicializar tarifas por defecto si no existen
            InicializarTarifasPorDefecto(connection);

            // Inicializar formas de pago por defecto si no existen
            InicializarFormasPagoPorDefecto(connection);
            
            // Inicializar módulos por defecto si no existen
            InicializarModulosPorDefecto(connection);

            // Inicializar clientes/vehículos mensuales por defecto (ninguno)
        }

        private void EjecutarMigraciones(SQLiteConnection connection)
        {
            // Migración 1: Agregar columna Rol a Administradores si no existe
            try
            {
                string checkColumnQuery = "PRAGMA table_info(Administradores)";
                using var checkCommand = new SQLiteCommand(checkColumnQuery, connection);
                using var reader = checkCommand.ExecuteReader();
                
                bool tieneRol = false;
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "Rol")
                    {
                        tieneRol = true;
                        break;
                    }
                }
                reader.Close();

                if (!tieneRol)
                {
                    string alterQuery = "ALTER TABLE Administradores ADD COLUMN Rol TEXT NOT NULL DEFAULT 'operador'";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                    
                    // Actualizar el primer admin existente a 'admin' si no hay ninguno con rol admin
                    string checkAdminQuery = "SELECT COUNT(*) FROM Administradores WHERE Rol = 'admin'";
                    using var checkAdminCommand = new SQLiteCommand(checkAdminQuery, connection);
                    long adminCount = (long)checkAdminCommand.ExecuteScalar();
                    
                    if (adminCount == 0)
                    {
                        // Actualizar el primer admin a 'admin'
                        string updateFirstAdminQuery = @"
                            UPDATE Administradores 
                            SET Rol = 'admin' 
                            WHERE Id = (SELECT MIN(Id) FROM Administradores)";
                        using var updateCommand = new SQLiteCommand(updateFirstAdminQuery, connection);
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Si falla, la columna probablemente ya existe o hay otro problema
            }

            // Migración 2: Agregar columnas AdminCreadorId y AdminCerradorId a Tickets si no existen
            try
            {
                string checkTicketsQuery = "PRAGMA table_info(Tickets)";
                using var checkCommand = new SQLiteCommand(checkTicketsQuery, connection);
                using var reader = checkCommand.ExecuteReader();
                
                bool tieneAdminCreadorId = false;
                bool tieneAdminCerradorId = false;
                
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "AdminCreadorId")
                    {
                        tieneAdminCreadorId = true;
                    }
                    if (columnName == "AdminCerradorId")
                    {
                        tieneAdminCerradorId = true;
                    }
                }
                reader.Close();

                if (!tieneAdminCreadorId)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN AdminCreadorId INTEGER";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneAdminCerradorId)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN AdminCerradorId INTEGER";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch
            {
                // Si falla, las columnas probablemente ya existen o hay otro problema
            }

            // Migración 3: Agregar columnas Slogan y Tema a Estacionamiento si no existen
            try
            {
                string checkEstacionamientoQuery = "PRAGMA table_info(Estacionamiento)";
                using var checkCommand = new SQLiteCommand(checkEstacionamientoQuery, connection);
                using var reader = checkCommand.ExecuteReader();
                
                bool tieneSlogan = false;
                bool tieneTema = false;
                
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "Slogan")
                    {
                        tieneSlogan = true;
                    }
                    if (columnName == "Tema")
                    {
                        tieneTema = true;
                    }
                }
                reader.Close();

                if (!tieneSlogan)
                {
                    string alterQuery = "ALTER TABLE Estacionamiento ADD COLUMN Slogan TEXT";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneTema)
                {
                    string alterQuery = "ALTER TABLE Estacionamiento ADD COLUMN Tema TEXT DEFAULT 'light'";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch
            {
                // Si falla, las columnas probablemente ya existen o hay otro problema
            }

            // Migración 4: Crear tabla Accesos si no existe
            try
            {
                string checkAccesosQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='Accesos'";
                using var checkAccesosCommand = new SQLiteCommand(checkAccesosQuery, connection);
                object? accesosExists = checkAccesosCommand.ExecuteScalar();
                
                if (accesosExists == null)
                {
                    string createAccesosTable = @"
                        CREATE TABLE IF NOT EXISTS Accesos (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            AdminId INTEGER NOT NULL,
                            FechaAcceso DATETIME NOT NULL,
                            FOREIGN KEY (AdminId) REFERENCES Administradores(Id)
                        )";
                    using var command = new SQLiteCommand(createAccesosTable, connection);
                    command.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ignorar errores
            }

            // Migración 4: Agregar columna NotaAdicional a Tickets si no existe
            try
            {
                string checkTicketsQuery = "PRAGMA table_info(Tickets)";
                using var checkCommand = new SQLiteCommand(checkTicketsQuery, connection);
                using var reader = checkCommand.ExecuteReader();

                bool tieneNota = false;
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "NotaAdicional")
                    {
                        tieneNota = true;
                        break;
                    }
                }
                reader.Close();

                if (!tieneNota)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN NotaAdicional TEXT";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ignorar errores
            }

            // Migración 5: Agregar columnas TarifaId, CategoriaId, EstaCancelado, MotivoCancelacion a Tickets si no existen
            try
            {
                string checkTicketsQuery = "PRAGMA table_info(Tickets)";
                using var checkCommand = new SQLiteCommand(checkTicketsQuery, connection);
                using var reader = checkCommand.ExecuteReader();

                bool tieneTarifaId = false;
                bool tieneCategoriaId = false;
                bool tieneEstaCancelado = false;
                bool tieneMotivoCancelacion = false;
                bool tieneFechaCancelacion = false;

                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "TarifaId") tieneTarifaId = true;
                    if (columnName == "CategoriaId") tieneCategoriaId = true;
                    if (columnName == "EstaCancelado") tieneEstaCancelado = true;
                    if (columnName == "MotivoCancelacion") tieneMotivoCancelacion = true;
                    if (columnName == "FechaCancelacion") tieneFechaCancelacion = true;
                }
                reader.Close();

                if (!tieneTarifaId)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN TarifaId INTEGER";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneCategoriaId)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN CategoriaId INTEGER";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneEstaCancelado)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN EstaCancelado INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneMotivoCancelacion)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN MotivoCancelacion TEXT";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneFechaCancelacion)
                {
                    string alterQuery = "ALTER TABLE Tickets ADD COLUMN FechaCancelacion DATETIME";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ignorar errores
            }
        }

        private void Migracion_ActualizarTarifas(SQLiteConnection connection)
        {
            try
            {
                string checkColumnQuery = "PRAGMA table_info(Tarifas)";
                using var checkCommand = new SQLiteCommand(checkColumnQuery, connection);
                using var reader = checkCommand.ExecuteReader();
                
                bool tieneTipo = false;
                bool tieneDias = false;
                bool tieneHoras = false;
                bool tieneMinutos = false;
                bool tieneTolerancia = false;
                
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "Tipo") tieneTipo = true;
                    if (columnName == "Dias") tieneDias = true;
                    if (columnName == "Horas") tieneHoras = true;
                    if (columnName == "Minutos") tieneMinutos = true;
                    if (columnName == "Tolerancia") tieneTolerancia = true;
                }
                reader.Close();

                if (!tieneTipo)
                {
                    string alterQuery = "ALTER TABLE Tarifas ADD COLUMN Tipo INTEGER NOT NULL DEFAULT 1";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneDias)
                {
                    string alterQuery = "ALTER TABLE Tarifas ADD COLUMN Dias INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneHoras)
                {
                    string alterQuery = "ALTER TABLE Tarifas ADD COLUMN Horas INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneMinutos)
                {
                    string alterQuery = "ALTER TABLE Tarifas ADD COLUMN Minutos INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }

                if (!tieneTolerancia)
                {
                    string alterQuery = "ALTER TABLE Tarifas ADD COLUMN Tolerancia INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch
            {
                // Ignorar errores
            }
        }

        private void InicializarCategoriasPorDefecto(SQLiteConnection connection)
        {
            // Verificar si ya existen categorías
            string checkQuery = "SELECT COUNT(*) FROM Categorias";
            using var checkCommand = new SQLiteCommand(checkQuery, connection);
            long count = (long)checkCommand.ExecuteScalar();

            if (count == 0)
            {
                // Insertar categorías por defecto
                string insertQuery = @"
                    INSERT INTO Categorias (Nombre, Orden, FechaCreacion)
                    VALUES (@Nombre, @Orden, @FechaCreacion)";

                var categorias = new[]
                {
                    new { Nombre = "MOTO", Orden = 1 },
                    new { Nombre = "AUTO", Orden = 2 },
                    new { Nombre = "CAMIONETA", Orden = 3 }
                };

                foreach (var categoria in categorias)
                {
                    using var command = new SQLiteCommand(insertQuery, connection);
                    command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
                    command.Parameters.AddWithValue("@Orden", categoria.Orden);
                    command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool ExisteAdmin()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT COUNT(*) FROM Administradores";
            using var command = new SQLiteCommand(query, connection);
            long count = (long)command.ExecuteScalar();
            return count > 0;
        }

        public bool ExisteEstacionamiento()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT COUNT(*) FROM Estacionamiento";
            using var command = new SQLiteCommand(query, connection);
            long count = (long)command.ExecuteScalar();
            return count > 0;
        }

        public void CrearAdmin(Admin admin)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string passwordHash = HashPassword(admin.PasswordHash);
            
            // Si no se especifica rol, el primero es admin, los demás operador
            string rol = admin.Rol;
            if (string.IsNullOrEmpty(rol))
            {
                // Verificar si ya existe un admin
                string checkQuery = "SELECT COUNT(*) FROM Administradores WHERE Rol = 'admin'";
                using var checkCommand = new SQLiteCommand(checkQuery, connection);
                long count = (long)checkCommand.ExecuteScalar();
                rol = count == 0 ? "admin" : "operador";
            }

            string query = @"
                INSERT INTO Administradores (Nombre, Apellido, Username, PasswordHash, Email, Rol, FechaCreacion)
                VALUES (@Nombre, @Apellido, @Username, @PasswordHash, @Email, @Rol, @FechaCreacion)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Nombre", admin.Nombre);
            command.Parameters.AddWithValue("@Apellido", admin.Apellido);
            command.Parameters.AddWithValue("@Username", admin.Username);
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);
            command.Parameters.AddWithValue("@Email", admin.Email);
            command.Parameters.AddWithValue("@Rol", rol);
            command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

            command.ExecuteNonQuery();
        }

        public bool ValidarLogin(string username, string password)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, PasswordHash FROM Administradores WHERE Username = @Username";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                reader.Close();
                return false;
            }

            int adminId = reader.GetInt32(0);
            string storedHash = reader.GetString(1);
            reader.Close();

            string inputHash = HashPassword(password);
            bool loginExitoso = storedHash == inputHash;

            if (loginExitoso)
            {
                // Actualizar último acceso usando la misma conexión
                ActualizarUltimoAcceso(adminId, connection);
            }

            return loginExitoso;
        }

        private void ActualizarUltimoAcceso(int adminId, SQLiteConnection? connection = null)
        {
            bool shouldCloseConnection = false;
            if (connection == null)
            {
                connection = new SQLiteConnection(_connectionString);
                connection.Open();
                shouldCloseConnection = true;
            }

            try
            {
                string query = @"
                    INSERT INTO Accesos (AdminId, FechaAcceso)
                    VALUES (@AdminId, @FechaAcceso)";
                
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@AdminId", adminId);
                command.Parameters.AddWithValue("@FechaAcceso", DateTime.Now);
                
                command.ExecuteNonQuery();
            }
            finally
            {
                if (shouldCloseConnection && connection != null)
                {
                    connection.Close();
                    connection.Dispose();
                }
            }
        }

        public void GuardarEstacionamiento(Estacionamiento estacionamiento)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Eliminar registro anterior si existe
            string deleteQuery = "DELETE FROM Estacionamiento";
            using var deleteCommand = new SQLiteCommand(deleteQuery, connection);
            deleteCommand.ExecuteNonQuery();

            string query = @"
                INSERT INTO Estacionamiento (Nombre, Direccion, Ciudad, Pais, Telefono, Slogan, Impresora, Tema, FechaCreacion)
                VALUES (@Nombre, @Direccion, @Ciudad, @Pais, @Telefono, @Slogan, @Impresora, @Tema, @FechaCreacion)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Nombre", estacionamiento.Nombre);
            command.Parameters.AddWithValue("@Direccion", estacionamiento.Direccion ?? string.Empty);
            command.Parameters.AddWithValue("@Ciudad", estacionamiento.Ciudad ?? string.Empty);
            command.Parameters.AddWithValue("@Pais", estacionamiento.Pais);
            command.Parameters.AddWithValue("@Telefono", estacionamiento.Telefono ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Slogan", estacionamiento.Slogan ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Impresora", estacionamiento.Impresora ?? string.Empty);
            command.Parameters.AddWithValue("@Tema", estacionamiento.Tema ?? "light");
            command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

            command.ExecuteNonQuery();
        }

        public Estacionamiento? ObtenerEstacionamiento()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, Direccion, Ciudad, Pais, Telefono, Slogan, Impresora, Tema, FechaCreacion FROM Estacionamiento LIMIT 1";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new Estacionamiento
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Direccion = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Ciudad = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Pais = reader.GetString(4),
                    Telefono = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Slogan = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Impresora = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    Tema = reader.IsDBNull(8) ? "light" : reader.GetString(8),
                    FechaCreacion = reader.GetDateTime(9)
                };
            }

            return null;
        }

        public string? ObtenerNombreAdmin(string username)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Nombre FROM Administradores WHERE Username = @Username";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            object? result = command.ExecuteScalar();
            return result?.ToString();
        }

        public Admin? ObtenerAdminPorId(int adminId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"SELECT Id, Nombre, Apellido, Username, PasswordHash, Email, Rol, FechaCreacion 
                             FROM Administradores WHERE Id = @Id LIMIT 1";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", adminId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Admin
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Apellido = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Username = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    PasswordHash = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    Email = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    Rol = reader.IsDBNull(6) ? "operador" : reader.GetString(6),
                    FechaCreacion = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7)
                };
            }

            return null;
        }

        public List<Ticket> ObtenerTicketsAbiertos()
        {
            var tickets = new List<Ticket>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"SELECT Id, Matricula, ImagenPath, Descripcion, NotaAdicional, TarifaId, CategoriaId, FechaEntrada, FechaSalida, FechaCancelacion, EstaAbierto, EstaCancelado, MotivoCancelacion, Monto, AdminCreadorId, AdminCerradorId, FechaCreacion 
                             FROM Tickets 
                             WHERE EstaAbierto = 1 AND (EstaCancelado IS NULL OR EstaCancelado = 0)
                             ORDER BY FechaEntrada DESC";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                tickets.Add(new Ticket
                {
                    Id = reader.GetInt32(0),
                    Matricula = reader.GetString(1),
                    ImagenPath = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Descripcion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    NotaAdicional = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    TarifaId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                    CategoriaId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                    FechaEntrada = reader.GetDateTime(7),
                    FechaSalida = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    FechaCancelacion = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    EstaAbierto = reader.GetInt32(10) == 1,
                    EstaCancelado = reader.IsDBNull(11) ? false : reader.GetInt32(11) == 1,
                    MotivoCancelacion = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    Monto = reader.IsDBNull(13) ? null : (decimal?)reader.GetDouble(13),
                    AdminCreadorId = reader.IsDBNull(14) ? null : (int?)reader.GetInt32(14),
                    AdminCerradorId = reader.IsDBNull(15) ? null : (int?)reader.GetInt32(15),
                    FechaCreacion = reader.GetDateTime(16)
                });
            }

            return tickets;
        }

        public List<Ticket> ObtenerTicketsCerrados()
        {
            var tickets = new List<Ticket>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"SELECT Id, Matricula, ImagenPath, Descripcion, NotaAdicional, TarifaId, CategoriaId, FechaEntrada, FechaSalida, FechaCancelacion, EstaAbierto, EstaCancelado, MotivoCancelacion, Monto, AdminCreadorId, AdminCerradorId, FechaCreacion 
                             FROM Tickets 
                             WHERE EstaAbierto = 0
                             ORDER BY FechaEntrada DESC";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                tickets.Add(new Ticket
                {
                    Id = reader.GetInt32(0),
                    Matricula = reader.GetString(1),
                    ImagenPath = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Descripcion = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    NotaAdicional = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    TarifaId = reader.IsDBNull(5) ? null : (int?)reader.GetInt32(5),
                    CategoriaId = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                    FechaEntrada = reader.GetDateTime(7),
                    FechaSalida = reader.IsDBNull(8) ? null : reader.GetDateTime(8),
                    FechaCancelacion = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    EstaAbierto = reader.GetInt32(10) == 1,
                    EstaCancelado = reader.IsDBNull(11) ? false : reader.GetInt32(11) == 1,
                    MotivoCancelacion = reader.IsDBNull(12) ? string.Empty : reader.GetString(12),
                    Monto = reader.IsDBNull(13) ? null : (decimal?)reader.GetDouble(13),
                    AdminCreadorId = reader.IsDBNull(14) ? null : (int?)reader.GetInt32(14),
                    AdminCerradorId = reader.IsDBNull(15) ? null : (int?)reader.GetInt32(15),
                    FechaCreacion = reader.GetDateTime(16)
                });
            }

            return tickets;
        }

        public Tarifa? ObtenerTarifaPorId(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, Tipo, Dias, Horas, Minutos, Tolerancia, FechaCreacion FROM Tarifas WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Tarifa
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Tipo = (TipoTarifa)reader.GetInt32(2),
                    Dias = reader.GetInt32(3),
                    Horas = reader.GetInt32(4),
                    Minutos = reader.GetInt32(5),
                    Tolerancia = reader.GetInt32(6),
                    FechaCreacion = reader.GetDateTime(7)
                };
            }

            return null;
        }

        public Categoria? ObtenerCategoriaPorId(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, Orden, FechaCreacion FROM Categorias WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Categoria
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Orden = reader.GetInt32(2),
                    FechaCreacion = reader.GetDateTime(3)
                };
            }

            return null;
        }

        public void ActualizarNotaTicket(int ticketId, string nota)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "UPDATE Tickets SET NotaAdicional = @Nota WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Nota", nota ?? string.Empty);
            command.Parameters.AddWithValue("@Id", ticketId);
            command.ExecuteNonQuery();
        }

        public void ActualizarTicket(int ticketId, string matricula, string descripcion, int? categoriaId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"UPDATE Tickets 
                             SET Matricula = @Matricula, Descripcion = @Descripcion, CategoriaId = @CategoriaId 
                             WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Matricula", matricula);
            command.Parameters.AddWithValue("@Descripcion", descripcion ?? string.Empty);
            command.Parameters.AddWithValue("@CategoriaId", categoriaId.HasValue ? (object)categoriaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Id", ticketId);
            command.ExecuteNonQuery();
        }

        public void CancelarTicket(int ticketId, string motivo, int? adminCerradorId = null)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"UPDATE Tickets 
                             SET EstaCancelado = 1, EstaAbierto = 0, MotivoCancelacion = @Motivo, FechaSalida = NULL, FechaCancelacion = @FechaCancelacion, AdminCerradorId = @AdminCerradorId
                             WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Motivo", motivo ?? string.Empty);
            command.Parameters.AddWithValue("@FechaCancelacion", DateTime.Now);
            command.Parameters.AddWithValue("@AdminCerradorId", adminCerradorId.HasValue ? (object)adminCerradorId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Id", ticketId);
            command.ExecuteNonQuery();
        }

        public List<Categoria> ObtenerCategorias()
        {
            var categorias = new List<Categoria>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, Orden, FechaCreacion FROM Categorias ORDER BY Orden";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                categorias.Add(new Categoria
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Orden = reader.GetInt32(2),
                    FechaCreacion = reader.GetDateTime(3)
                });
            }

            return categorias;
        }

        public int CrearTicket(Ticket ticket)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                INSERT INTO Tickets (Matricula, ImagenPath, Descripcion, NotaAdicional, TarifaId, CategoriaId, FechaEntrada, FechaSalida, FechaCancelacion, EstaAbierto, EstaCancelado, MotivoCancelacion, Monto, AdminCreadorId, AdminCerradorId, FechaCreacion)
                VALUES (@Matricula, @ImagenPath, @Descripcion, @NotaAdicional, @TarifaId, @CategoriaId, @FechaEntrada, NULL, NULL, 1, 0, '', NULL, @AdminCreadorId, NULL, @FechaCreacion);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Matricula", ticket.Matricula);
            command.Parameters.AddWithValue("@ImagenPath", ticket.ImagenPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Descripcion", ticket.Descripcion ?? string.Empty);
            command.Parameters.AddWithValue("@NotaAdicional", ticket.NotaAdicional ?? string.Empty);
            command.Parameters.AddWithValue("@TarifaId", ticket.TarifaId.HasValue ? (object)ticket.TarifaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@CategoriaId", ticket.CategoriaId.HasValue ? (object)ticket.CategoriaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@FechaEntrada", ticket.FechaEntrada);
            command.Parameters.AddWithValue("@AdminCreadorId", ticket.AdminCreadorId.HasValue ? (object)ticket.AdminCreadorId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

            var id = (long)command.ExecuteScalar();
            return (int)id;
        }

        public void CrearCategoria(Categoria categoria)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Obtener el siguiente orden
            string maxOrderQuery = "SELECT MAX(Orden) FROM Categorias";
            using var maxCommand = new SQLiteCommand(maxOrderQuery, connection);
            object? maxOrder = maxCommand.ExecuteScalar();
            int siguienteOrden = maxOrder == null || maxOrder == DBNull.Value ? 1 : Convert.ToInt32(maxOrder) + 1;

            string query = @"
                INSERT INTO Categorias (Nombre, Orden, FechaCreacion)
                VALUES (@Nombre, @Orden, @FechaCreacion)";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
            command.Parameters.AddWithValue("@Orden", siguienteOrden);
            command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

            command.ExecuteNonQuery();
        }

        public void ActualizarCategoria(Categoria categoria)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Categorias 
                SET Nombre = @Nombre, Orden = @Orden
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", categoria.Id);
            command.Parameters.AddWithValue("@Nombre", categoria.Nombre);
            command.Parameters.AddWithValue("@Orden", categoria.Orden);

            command.ExecuteNonQuery();
        }

        public void EliminarCategoria(int categoriaId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "DELETE FROM Categorias WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", categoriaId);

            command.ExecuteNonQuery();
        }

        public void ActualizarOrdenCategorias(List<Categoria> categorias)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();
            try
            {
                for (int i = 0; i < categorias.Count; i++)
                {
                    string query = "UPDATE Categorias SET Orden = @Orden WHERE Id = @Id";
                    using var command = new SQLiteCommand(query, connection, transaction);
                    command.Parameters.AddWithValue("@Orden", i + 1);
                    command.Parameters.AddWithValue("@Id", categorias[i].Id);
                    command.ExecuteNonQuery();
                }
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public int ObtenerCantidadTicketsAbiertos()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT COUNT(*) FROM Tickets WHERE EstaAbierto = 1 AND (EstaCancelado IS NULL OR EstaCancelado = 0)";
            using var command = new SQLiteCommand(query, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int ObtenerEntradasUltimas24Horas()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT COUNT(*) FROM Tickets WHERE FechaEntrada >= datetime('now', '-24 hours')";
            using var command = new SQLiteCommand(query, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int ObtenerSalidasUltimas24Horas()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"SELECT COUNT(*) 
                             FROM Tickets 
                             WHERE FechaSalida IS NOT NULL 
                               AND FechaSalida >= datetime('now', '-24 hours')
                               AND (EstaCancelado IS NULL OR EstaCancelado = 0)";
            using var command = new SQLiteCommand(query, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        public List<FormaPago> ObtenerFormasPago()
        {
            var lista = new List<FormaPago>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, FechaCreacion FROM FormasPago ORDER BY Id";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new FormaPago
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    FechaCreacion = reader.GetDateTime(2)
                });
            }
            return lista;
        }

        public int RegistrarTransaccion(Transaccion tx)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                INSERT INTO Transacciones (TicketId, AdminId, FormaPagoId, Importe, Fecha, Descripcion, ConRecibo, EsSalidaInmediata, ItemsDetalle)
                VALUES (@TicketId, @AdminId, @FormaPagoId, @Importe, @Fecha, @Descripcion, @ConRecibo, @EsSalidaInmediata, @ItemsDetalle);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@TicketId", tx.TicketId);
            command.Parameters.AddWithValue("@AdminId", tx.AdminId.HasValue ? (object)tx.AdminId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@FormaPagoId", tx.FormaPagoId);
            command.Parameters.AddWithValue("@Importe", tx.Importe);
            command.Parameters.AddWithValue("@Fecha", tx.Fecha);
            command.Parameters.AddWithValue("@Descripcion", tx.Descripcion ?? string.Empty);
            command.Parameters.AddWithValue("@ConRecibo", tx.ConRecibo ? 1 : 0);
            command.Parameters.AddWithValue("@EsSalidaInmediata", tx.EsSalidaInmediata ? 1 : 0);
            command.Parameters.AddWithValue("@ItemsDetalle", tx.ItemsDetalle ?? string.Empty);

            var id = (long)command.ExecuteScalar();
            return (int)id;
        }

        public void CerrarTicket(int ticketId, decimal monto, int? adminCerradorId, DateTime? fechaSalida, string descripcion, int? formaPagoId = null)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Tickets
                SET EstaAbierto = 0,
                    EstaCancelado = 0,
                    FechaSalida = @FechaSalida,
                    Monto = @Monto,
                    AdminCerradorId = @AdminCerradorId,
                    NotaAdicional = @Descripcion
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@FechaSalida", fechaSalida.HasValue ? (object)fechaSalida.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Monto", monto);
            command.Parameters.AddWithValue("@AdminCerradorId", adminCerradorId.HasValue ? (object)adminCerradorId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Descripcion", descripcion ?? string.Empty);
            command.Parameters.AddWithValue("@Id", ticketId);
            command.ExecuteNonQuery();
        }

        // -------------------- Mensuales --------------------
        public List<ClienteMensual> ObtenerClientesMensuales()
        {
            var lista = new List<ClienteMensual>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT c.Id, c.Nombre, c.Apellido, c.Whatsapp, c.Email, c.DNI, c.CUIT, c.Direccion, c.Activo, c.FechaCreacion,
                           IFNULL(v.CantVehiculos,0) as CantVehiculos,
                           IFNULL(v.Matriculas,'') as Matriculas,
                           IFNULL(m.Balance,0) as Balance
                    FROM ClientesMensuales c
                    LEFT JOIN (
                        SELECT ClienteId, COUNT(*) as CantVehiculos, GROUP_CONCAT(Matricula, ' • ') as Matriculas
                        FROM VehiculosMensuales
                        GROUP BY ClienteId
                    ) v ON v.ClienteId = c.Id
                    LEFT JOIN (
                        SELECT ClienteId, SUM(Importe) as Balance
                        FROM MovimientosMensuales
                        WHERE Tipo != 'PagoAdelantado'
                        GROUP BY ClienteId
                    ) m ON m.ClienteId = c.Id
                    ORDER BY c.Id";

                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        lista.Add(new ClienteMensual
                        {
                            Id = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            Apellido = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Whatsapp = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                            DNI = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                            CUIT = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                            Direccion = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                            Activo = reader.IsDBNull(8) ? true : (reader.GetInt32(8) == 1),
                            FechaCreacion = reader.IsDBNull(9) ? DateTime.Now : reader.GetDateTime(9),
                            CantVehiculos = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
                            MatriculasConcat = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
                            Balance = reader.IsDBNull(12) ? 0m : Convert.ToDecimal(reader.GetValue(12))
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log del error pero continuar con el siguiente registro
                        System.Diagnostics.Debug.WriteLine($"Error al leer cliente mensual: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Si hay un error, retornar lista vacía en lugar de lanzar excepción
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerClientesMensuales: {ex.Message}");
            }

            return lista;
        }


        public decimal ObtenerBalanceClienteMensual(int clienteId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            // Excluir pagos adelantados del balance
            string query = "SELECT IFNULL(SUM(Importe),0) FROM MovimientosMensuales WHERE ClienteId = @ClienteId AND Tipo != 'PagoAdelantado'";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@ClienteId", clienteId);
            object? result = command.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return 0m;
            
            // Convertir usando Convert.ToDecimal que maneja Int64, Double, Decimal, etc.
            return Convert.ToDecimal(result);
        }

        public void ReiniciarBaseDatos()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Eliminar todas las tablas
            string[] tablas = { "Precios", "Tarifas", "Administradores", "Estacionamiento", "Tickets", "Categorias" };
            
            foreach (string tabla in tablas)
            {
                try
                {
                    string dropQuery = $"DROP TABLE IF EXISTS {tabla}";
                    using var command = new SQLiteCommand(dropQuery, connection);
                    command.ExecuteNonQuery();
                }
                catch
                {
                    // Ignorar errores si la tabla no existe
                }
            }

            // Cerrar la conexión
            connection.Close();

            // Eliminar el archivo de base de datos
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            // Reinicializar la base de datos
            InitializeDatabase();
        }

        private void InicializarTarifasPorDefecto(SQLiteConnection connection)
        {
            // Verificar si ya existen tarifas
            string checkQuery = "SELECT COUNT(*) FROM Tarifas";
            using var checkCommand = new SQLiteCommand(checkQuery, connection);
            long count = (long)checkCommand.ExecuteScalar();

            if (count == 0)
            {
                // Insertar tarifas por defecto
                string insertQuery = @"
                    INSERT INTO Tarifas (Nombre, Tipo, Dias, Horas, Minutos, Tolerancia, FechaCreacion)
                    VALUES (@Nombre, @Tipo, @Dias, @Horas, @Minutos, @Tolerancia, @FechaCreacion)";

                // xHora: tipo PorHora, días=0, horas=1, minutos=0, tolerancia=5
                using var command1 = new SQLiteCommand(insertQuery, connection);
                command1.Parameters.AddWithValue("@Nombre", "xHora");
                command1.Parameters.AddWithValue("@Tipo", (int)TipoTarifa.PorHora);
                command1.Parameters.AddWithValue("@Dias", 0);
                command1.Parameters.AddWithValue("@Horas", 1);
                command1.Parameters.AddWithValue("@Minutos", 0);
                command1.Parameters.AddWithValue("@Tolerancia", 5);
                command1.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                command1.ExecuteNonQuery();

                // Mensual: tipo Mensual
                using var command2 = new SQLiteCommand(insertQuery, connection);
                command2.Parameters.AddWithValue("@Nombre", "Mensual");
                command2.Parameters.AddWithValue("@Tipo", (int)TipoTarifa.Mensual);
                command2.Parameters.AddWithValue("@Dias", 0);
                command2.Parameters.AddWithValue("@Horas", 0);
                command2.Parameters.AddWithValue("@Minutos", 0);
                command2.Parameters.AddWithValue("@Tolerancia", 0);
                command2.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                command2.ExecuteNonQuery();
            }
        }

        private void InicializarFormasPagoPorDefecto(SQLiteConnection connection)
        {
            string checkQuery = "SELECT COUNT(*) FROM FormasPago";
            using var checkCommand = new SQLiteCommand(checkQuery, connection);
            long count = (long)checkCommand.ExecuteScalar();

            if (count == 0)
            {
                string insertQuery = @"
                    INSERT INTO FormasPago (Nombre, FechaCreacion)
                    VALUES ('Efectivo', @Fecha),
                           ('Tarjeta de Crédito', @Fecha),
                           ('Tarjeta de Débito', @Fecha),
                           ('MercadoPago', @Fecha)";

                using var insertCommand = new SQLiteCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                insertCommand.ExecuteNonQuery();
            }
        }

        private void InicializarModulosPorDefecto(SQLiteConnection connection)
        {
            string checkQuery = "SELECT COUNT(*) FROM Modulos";
            using var checkCommand = new SQLiteCommand(checkQuery, connection);
            long count = (long)checkCommand.ExecuteScalar();

            string[] modulos = { "ANPR", "TERMINAL", "EMAIL", "MONITOR", "MERCADOPAGO" };

            if (count == 0)
            {
                // Si no hay módulos, insertar todos
                string insertQuery = @"
                    INSERT INTO Modulos (Nombre, EstaActivo, FechaCreacion)
                    VALUES (@Nombre, 0, @Fecha)";

                foreach (string modulo in modulos)
                {
                    using var insertCommand = new SQLiteCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@Nombre", modulo);
                    insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                    insertCommand.ExecuteNonQuery();
                }
            }
            else
            {
                // Si ya hay módulos, verificar que existan todos los módulos requeridos
                foreach (string modulo in modulos)
                {
                    string checkModuloQuery = "SELECT COUNT(*) FROM Modulos WHERE Nombre = @Nombre";
                    using var checkModuloCommand = new SQLiteCommand(checkModuloQuery, connection);
                    checkModuloCommand.Parameters.AddWithValue("@Nombre", modulo);
                    long moduloCount = (long)checkModuloCommand.ExecuteScalar();

                    if (moduloCount == 0)
                    {
                        // Insertar el módulo que falta
                        string insertQuery = @"
                            INSERT INTO Modulos (Nombre, EstaActivo, FechaCreacion)
                            VALUES (@Nombre, 0, @Fecha)";
                        using var insertCommand = new SQLiteCommand(insertQuery, connection);
                        insertCommand.Parameters.AddWithValue("@Nombre", modulo);
                        insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                        insertCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        private void InicializarPantallaClientePorDefecto(SQLiteConnection connection)
        {
            string checkQuery = "SELECT COUNT(*) FROM PantallaCliente";
            using var checkCommand = new SQLiteCommand(checkQuery, connection);
            long count = (long)checkCommand.ExecuteScalar();

            if (count == 0)
            {
                // Obtener nombre del estacionamiento
                string nombreEstacionamiento = "";
                string estacionamientoQuery = "SELECT Nombre FROM Estacionamiento LIMIT 1";
                using var estacionamientoCommand = new SQLiteCommand(estacionamientoQuery, connection);
                var result = estacionamientoCommand.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    nombreEstacionamiento = result.ToString() ?? "";
                }

                string insertQuery = @"
                    INSERT INTO PantallaCliente (BienvenidaLinea1, BienvenidaLinea2, AgradecimientoLinea1, AgradecimientoLinea2, CobroAclaracion, FechaCreacion, FechaActualizacion)
                    VALUES (@BienvenidaLinea1, @BienvenidaLinea2, @AgradecimientoLinea1, @AgradecimientoLinea2, @CobroAclaracion, @Fecha, @Fecha)";

                using var insertCommand = new SQLiteCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@BienvenidaLinea1", "Bienvenidos a");
                insertCommand.Parameters.AddWithValue("@BienvenidaLinea2", nombreEstacionamiento);
                insertCommand.Parameters.AddWithValue("@AgradecimientoLinea1", "¡Gracias por su visita!");
                insertCommand.Parameters.AddWithValue("@AgradecimientoLinea2", nombreEstacionamiento);
                insertCommand.Parameters.AddWithValue("@CobroAclaracion", "");
                insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                insertCommand.ExecuteNonQuery();
            }
        }

        public Dictionary<string, string> ObtenerConfiguracionPantallaCliente()
        {
            var config = new Dictionary<string, string>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT BienvenidaLinea1, BienvenidaLinea2, AgradecimientoLinea1, AgradecimientoLinea2, CobroAclaracion FROM PantallaCliente LIMIT 1";
                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    config["BienvenidaLinea1"] = reader.IsDBNull(0) ? "Bienvenidos a" : reader.GetString(0);
                    config["BienvenidaLinea2"] = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    config["AgradecimientoLinea1"] = reader.IsDBNull(2) ? "¡Gracias por su visita!" : reader.GetString(2);
                    config["AgradecimientoLinea2"] = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    config["CobroAclaracion"] = reader.IsDBNull(4) ? "" : reader.GetString(4);
                }
                else
                {
                    // Valores por defecto
                    config["BienvenidaLinea1"] = "Bienvenidos a";
                    config["BienvenidaLinea2"] = "";
                    config["AgradecimientoLinea1"] = "¡Gracias por su visita!";
                    config["AgradecimientoLinea2"] = "";
                    config["CobroAclaracion"] = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener configuración pantalla cliente: {ex.Message}");
                config["BienvenidaLinea1"] = "Bienvenidos a";
                config["BienvenidaLinea2"] = "";
                config["AgradecimientoLinea1"] = "¡Gracias por su visita!";
                config["AgradecimientoLinea2"] = "";
                config["CobroAclaracion"] = "";
            }
            return config;
        }

        public void ActualizarConfiguracionPantallaCliente(string bienvenidaLinea1, string bienvenidaLinea2, string agradecimientoLinea1, string agradecimientoLinea2, string cobroAclaracion)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                
                // Verificar si existe registro
                string checkQuery = "SELECT COUNT(*) FROM PantallaCliente";
                using var checkCommand = new SQLiteCommand(checkQuery, connection);
                long count = (long)checkCommand.ExecuteScalar();

                if (count == 0)
                {
                    // Insertar
                    string insertQuery = @"
                        INSERT INTO PantallaCliente (BienvenidaLinea1, BienvenidaLinea2, AgradecimientoLinea1, AgradecimientoLinea2, CobroAclaracion, FechaCreacion, FechaActualizacion)
                        VALUES (@BienvenidaLinea1, @BienvenidaLinea2, @AgradecimientoLinea1, @AgradecimientoLinea2, @CobroAclaracion, @Fecha, @Fecha)";
                    using var insertCommand = new SQLiteCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@BienvenidaLinea1", bienvenidaLinea1);
                    insertCommand.Parameters.AddWithValue("@BienvenidaLinea2", bienvenidaLinea2);
                    insertCommand.Parameters.AddWithValue("@AgradecimientoLinea1", agradecimientoLinea1);
                    insertCommand.Parameters.AddWithValue("@AgradecimientoLinea2", agradecimientoLinea2);
                    insertCommand.Parameters.AddWithValue("@CobroAclaracion", cobroAclaracion);
                    insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                    insertCommand.ExecuteNonQuery();
                }
                else
                {
                    // Actualizar
                    string updateQuery = @"
                        UPDATE PantallaCliente 
                        SET BienvenidaLinea1 = @BienvenidaLinea1,
                            BienvenidaLinea2 = @BienvenidaLinea2,
                            AgradecimientoLinea1 = @AgradecimientoLinea1,
                            AgradecimientoLinea2 = @AgradecimientoLinea2,
                            CobroAclaracion = @CobroAclaracion,
                            FechaActualizacion = @Fecha
                        WHERE Id = (SELECT Id FROM PantallaCliente LIMIT 1)";
                    using var updateCommand = new SQLiteCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@BienvenidaLinea1", bienvenidaLinea1);
                    updateCommand.Parameters.AddWithValue("@BienvenidaLinea2", bienvenidaLinea2);
                    updateCommand.Parameters.AddWithValue("@AgradecimientoLinea1", agradecimientoLinea1);
                    updateCommand.Parameters.AddWithValue("@AgradecimientoLinea2", agradecimientoLinea2);
                    updateCommand.Parameters.AddWithValue("@CobroAclaracion", cobroAclaracion);
                    updateCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                    updateCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar configuración pantalla cliente: {ex.Message}");
                throw;
            }
        }

        private void VerificarColumnaPuertoPantallaCliente(SQLiteConnection connection)
        {
            try
            {
                string checkColumnQuery = "PRAGMA table_info(PantallaCliente)";
                using var checkCommand = new SQLiteCommand(checkColumnQuery, connection);
                using var reader = checkCommand.ExecuteReader();
                
                bool tienePuerto = false;
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "Puerto")
                    {
                        tienePuerto = true;
                        break;
                    }
                }
                reader.Close();

                if (!tienePuerto)
                {
                    string alterQuery = "ALTER TABLE PantallaCliente ADD COLUMN Puerto INTEGER";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar columna Puerto en PantallaCliente: {ex.Message}");
            }
        }

        private void VerificarColumnaMostrarEnSegundoMonitor(SQLiteConnection connection)
        {
            try
            {
                string checkColumnQuery = "PRAGMA table_info(PantallaCliente)";
                using var checkCommand = new SQLiteCommand(checkColumnQuery, connection);
                using var reader = checkCommand.ExecuteReader();
                
                bool tieneMostrarEnSegundoMonitor = false;
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "MostrarEnSegundoMonitor")
                    {
                        tieneMostrarEnSegundoMonitor = true;
                        break;
                    }
                }
                reader.Close();

                if (!tieneMostrarEnSegundoMonitor)
                {
                    string alterQuery = "ALTER TABLE PantallaCliente ADD COLUMN MostrarEnSegundoMonitor INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar columna MostrarEnSegundoMonitor en PantallaCliente: {ex.Message}");
            }
        }

        private void VerificarColumnaSolicitarMotivoApertura(SQLiteConnection connection)
        {
            try
            {
                string checkColumnQuery = "PRAGMA table_info(CamarasANPR)";
                using var checkCommand = new SQLiteCommand(checkColumnQuery, connection);
                using var reader = checkCommand.ExecuteReader();
                
                bool tieneColumna = false;
                while (reader.Read())
                {
                    string columnName = reader.GetString(1);
                    if (columnName == "SolicitarMotivoApertura")
                    {
                        tieneColumna = true;
                        break;
                    }
                }
                reader.Close();

                if (!tieneColumna)
                {
                    string alterQuery = "ALTER TABLE CamarasANPR ADD COLUMN SolicitarMotivoApertura INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar columna SolicitarMotivoApertura en CamarasANPR: {ex.Message}");
            }
        }

        public int? ObtenerPuertoPantallaCliente()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT Puerto FROM PantallaCliente LIMIT 1";
                using var command = new SQLiteCommand(query, connection);
                var result = command.ExecuteScalar();
                
                if (result != null && result != DBNull.Value)
                {
                    if (int.TryParse(result.ToString(), out int puerto))
                    {
                        return puerto;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener puerto pantalla cliente: {ex.Message}");
            }
            return null;
        }

        public void GuardarPuertoPantallaCliente(int puerto)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                
                // Verificar si existe registro
                string checkQuery = "SELECT COUNT(*) FROM PantallaCliente";
                using var checkCommand = new SQLiteCommand(checkQuery, connection);
                long count = (long)checkCommand.ExecuteScalar();

                if (count == 0)
                {
                    // Insertar con puerto
                    string insertQuery = @"
                        INSERT INTO PantallaCliente (BienvenidaLinea1, BienvenidaLinea2, AgradecimientoLinea1, AgradecimientoLinea2, CobroAclaracion, Puerto, FechaCreacion, FechaActualizacion)
                        VALUES (@BienvenidaLinea1, @BienvenidaLinea2, @AgradecimientoLinea1, @AgradecimientoLinea2, @CobroAclaracion, @Puerto, @Fecha, @Fecha)";
                    using var insertCommand = new SQLiteCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@BienvenidaLinea1", "Bienvenidos a");
                    insertCommand.Parameters.AddWithValue("@BienvenidaLinea2", "");
                    insertCommand.Parameters.AddWithValue("@AgradecimientoLinea1", "¡Gracias por su visita!");
                    insertCommand.Parameters.AddWithValue("@AgradecimientoLinea2", "");
                    insertCommand.Parameters.AddWithValue("@CobroAclaracion", "");
                    insertCommand.Parameters.AddWithValue("@Puerto", puerto);
                    insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                    insertCommand.ExecuteNonQuery();
                }
                else
                {
                    // Actualizar puerto
                    string updateQuery = @"
                        UPDATE PantallaCliente 
                        SET Puerto = @Puerto
                        WHERE Id = (SELECT Id FROM PantallaCliente LIMIT 1)";
                    using var updateCommand = new SQLiteCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@Puerto", puerto);
                    updateCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar puerto pantalla cliente: {ex.Message}");
                throw;
            }
        }

        public bool ObtenerMostrarEnSegundoMonitor()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT MostrarEnSegundoMonitor FROM PantallaCliente LIMIT 1";
                using var command = new SQLiteCommand(query, connection);
                var result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result) == 1;
                }
                return false; // Por defecto false
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener MostrarEnSegundoMonitor: {ex.Message}");
                return false;
            }
        }

        public void GuardarMostrarEnSegundoMonitor(bool mostrar)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                
                // Verificar si existe registro
                string checkQuery = "SELECT COUNT(*) FROM PantallaCliente";
                using var checkCommand = new SQLiteCommand(checkQuery, connection);
                long count = (long)checkCommand.ExecuteScalar();

                if (count == 0)
                {
                    // Insertar con MostrarEnSegundoMonitor
                    string insertQuery = @"
                        INSERT INTO PantallaCliente (BienvenidaLinea1, BienvenidaLinea2, AgradecimientoLinea1, AgradecimientoLinea2, CobroAclaracion, MostrarEnSegundoMonitor, FechaCreacion, FechaActualizacion)
                        VALUES (@BienvenidaLinea1, @BienvenidaLinea2, @AgradecimientoLinea1, @AgradecimientoLinea2, @CobroAclaracion, @MostrarEnSegundoMonitor, @Fecha, @Fecha)";
                    using var insertCommand = new SQLiteCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@BienvenidaLinea1", "Bienvenidos a");
                    insertCommand.Parameters.AddWithValue("@BienvenidaLinea2", "");
                    insertCommand.Parameters.AddWithValue("@AgradecimientoLinea1", "¡Gracias por su visita!");
                    insertCommand.Parameters.AddWithValue("@AgradecimientoLinea2", "");
                    insertCommand.Parameters.AddWithValue("@CobroAclaracion", "");
                    insertCommand.Parameters.AddWithValue("@MostrarEnSegundoMonitor", mostrar ? 1 : 0);
                    insertCommand.Parameters.AddWithValue("@Fecha", DateTime.Now);
                    insertCommand.ExecuteNonQuery();
                }
                else
                {
                    // Actualizar MostrarEnSegundoMonitor
                    string updateQuery = @"
                        UPDATE PantallaCliente 
                        SET MostrarEnSegundoMonitor = @MostrarEnSegundoMonitor
                        WHERE Id = (SELECT Id FROM PantallaCliente LIMIT 1)";
                    using var updateCommand = new SQLiteCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@MostrarEnSegundoMonitor", mostrar ? 1 : 0);
                    updateCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar MostrarEnSegundoMonitor: {ex.Message}");
                throw;
            }
        }

        public Dictionary<string, bool> ObtenerModulos()
        {
            var modulos = new Dictionary<string, bool>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT Nombre, EstaActivo FROM Modulos";
                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    string nombre = reader.GetString(0);
                    bool activo = reader.GetInt32(1) == 1;
                    modulos[nombre] = activo;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener módulos: {ex.Message}");
            }
            return modulos;
        }

        public void ActualizarModulo(string nombre, bool activo)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "UPDATE Modulos SET EstaActivo = @Activo WHERE Nombre = @Nombre";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Activo", activo ? 1 : 0);
                command.Parameters.AddWithValue("@Nombre", nombre);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al actualizar módulo: {ex.Message}");
                throw;
            }
        }

        public Dictionary<string, string> ObtenerCredencialesMercadoPago()
        {
            var credenciales = new Dictionary<string, string>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT AccessToken, PublicKey FROM MercadoPagoCredenciales LIMIT 1";
                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    credenciales["AccessToken"] = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    credenciales["PublicKey"] = reader.IsDBNull(1) ? "" : reader.GetString(1);
                }
                else
                {
                    credenciales["AccessToken"] = "";
                    credenciales["PublicKey"] = "";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener credenciales Mercado Pago: {ex.Message}");
                credenciales["AccessToken"] = "";
                credenciales["PublicKey"] = "";
            }
            return credenciales;
        }

        public void GuardarCredencialesMercadoPago(string accessToken, string publicKey)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                
                // Verificar si ya existe un registro
                string checkQuery = "SELECT COUNT(*) FROM MercadoPagoCredenciales";
                using var checkCommand = new SQLiteCommand(checkQuery, connection);
                long count = (long)checkCommand.ExecuteScalar();
                
                if (count == 0)
                {
                    // Insertar nuevo registro
                    string insertQuery = @"
                        INSERT INTO MercadoPagoCredenciales (AccessToken, PublicKey, FechaCreacion, FechaActualizacion)
                        VALUES (@AccessToken, @PublicKey, @FechaCreacion, @FechaActualizacion)";
                    using var insertCommand = new SQLiteCommand(insertQuery, connection);
                    insertCommand.Parameters.AddWithValue("@AccessToken", accessToken ?? "");
                    insertCommand.Parameters.AddWithValue("@PublicKey", publicKey ?? "");
                    insertCommand.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                    insertCommand.Parameters.AddWithValue("@FechaActualizacion", DateTime.Now);
                    insertCommand.ExecuteNonQuery();
                }
                else
                {
                    // Actualizar registro existente
                    string updateQuery = @"
                        UPDATE MercadoPagoCredenciales 
                        SET AccessToken = @AccessToken, PublicKey = @PublicKey, FechaActualizacion = @FechaActualizacion
                        WHERE Id = (SELECT Id FROM MercadoPagoCredenciales LIMIT 1)";
                    using var updateCommand = new SQLiteCommand(updateQuery, connection);
                    updateCommand.Parameters.AddWithValue("@AccessToken", accessToken ?? "");
                    updateCommand.Parameters.AddWithValue("@PublicKey", publicKey ?? "");
                    updateCommand.Parameters.AddWithValue("@FechaActualizacion", DateTime.Now);
                    updateCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar credenciales Mercado Pago: {ex.Message}");
                throw;
            }
        }

        private void RecalcularBalanceCliente(SQLiteConnection connection, int clienteId)
        {
            string sumQuery = "SELECT IFNULL(SUM(Importe),0) FROM MovimientosMensuales WHERE ClienteId = @ClienteId";
            using var sumCommand = new SQLiteCommand(sumQuery, connection);
            sumCommand.Parameters.AddWithValue("@ClienteId", clienteId);
            var total = Convert.ToDecimal(sumCommand.ExecuteScalar() ?? 0m);

            string updateQuery = "UPDATE ClientesMensuales SET Balance = @Balance WHERE Id = @ClienteId";
            using var upd = new SQLiteCommand(updateQuery, connection);
            upd.Parameters.AddWithValue("@Balance", total);
            upd.Parameters.AddWithValue("@ClienteId", clienteId);
            upd.ExecuteNonQuery();
        }

        public int CrearClienteMensual(ClienteMensual c)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                INSERT INTO ClientesMensuales (Nombre, Apellido, Whatsapp, Email, DNI, CUIT, Direccion, Nota, Activo, FechaCreacion)
                VALUES (@Nombre, @Apellido, @Whatsapp, @Email, @DNI, @CUIT, @Direccion, @Nota, 1, @FechaCreacion);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Nombre", c.Nombre);
            command.Parameters.AddWithValue("@Apellido", c.Apellido);
            command.Parameters.AddWithValue("@Whatsapp", c.Whatsapp);
            command.Parameters.AddWithValue("@Email", c.Email);
            command.Parameters.AddWithValue("@DNI", c.DNI);
            command.Parameters.AddWithValue("@CUIT", c.CUIT);
            command.Parameters.AddWithValue("@Direccion", c.Direccion);
            command.Parameters.AddWithValue("@Nota", c.Nota);
            command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

            var id = (long)command.ExecuteScalar();
            return (int)id;
        }

        public List<ClienteMensual> ObtenerClientesMensuales(bool? activos = null, string? filtroMatricula = null, string? filtroNombre = null, bool? conDeuda = null)
        {
            var lista = new List<ClienteMensual>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT c.Id, c.Nombre, c.Apellido, c.Whatsapp, c.Email, c.DNI, c.CUIT, c.Direccion, c.Nota, c.Activo, c.FechaCreacion,
                       IFNULL(m.Balance,0) as Balance
                FROM ClientesMensuales c
                LEFT JOIN (
                    SELECT ClienteId, SUM(Importe) as Balance
                    FROM MovimientosMensuales
                    GROUP BY ClienteId
                ) m ON m.ClienteId = c.Id
                WHERE 1=1";

            if (activos.HasValue)
                query += " AND c.Activo = @Activo";
            if (!string.IsNullOrWhiteSpace(filtroNombre))
                query += " AND (c.Nombre LIKE @Nom OR c.Apellido LIKE @Nom)";

            using var command = new SQLiteCommand(query, connection);
            if (activos.HasValue)
                command.Parameters.AddWithValue("@Activo", activos.Value ? 1 : 0);
            if (!string.IsNullOrWhiteSpace(filtroNombre))
                command.Parameters.AddWithValue("@Nom", $"%{filtroNombre}%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var balance = reader.IsDBNull(11) ? 0m : Convert.ToDecimal(reader.GetValue(11));
                lista.Add(new ClienteMensual
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellido = reader.GetString(2),
                    Whatsapp = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    DNI = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    CUIT = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Direccion = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    Nota = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    Activo = reader.GetInt32(9) == 1,
                    Balance = balance,
                    FechaCreacion = reader.GetDateTime(10)
                });
            }

            // Filtro por matricula requiere join
            if (!string.IsNullOrWhiteSpace(filtroMatricula))
            {
                string filtro = filtroMatricula.ToUpper();
                string queryMat = @"SELECT DISTINCT ClienteId FROM VehiculosMensuales WHERE UPPER(Matricula) LIKE @Mat";
                using var cmdMat = new SQLiteCommand(queryMat, connection);
                cmdMat.Parameters.AddWithValue("@Mat", $"%{filtro}%");
                var ids = new List<int>();
                using var rdMat = cmdMat.ExecuteReader();
                while (rdMat.Read())
                    ids.Add(rdMat.GetInt32(0));
                lista = lista.Where(c => ids.Contains(c.Id)).ToList();
            }

            // Filtro por deuda (después de calcular el balance)
            if (conDeuda.HasValue && conDeuda.Value)
            {
                lista = lista.Where(c => c.Balance > 0).ToList();
            }

            return lista;
        }

        public ClienteMensual? ObtenerClienteMensualPorId(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT c.Id, c.Nombre, c.Apellido, c.Whatsapp, c.Email, c.DNI, c.CUIT, c.Direccion, c.Nota, c.Activo, c.FechaCreacion,
                       IFNULL(m.Balance,0) as Balance
                FROM ClientesMensuales c
                LEFT JOIN (
                    SELECT ClienteId, SUM(Importe) as Balance
                    FROM MovimientosMensuales
                    WHERE Tipo != 'PagoAdelantado'
                    GROUP BY ClienteId
                ) m ON m.ClienteId = c.Id
                WHERE c.Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var balance = reader.IsDBNull(11) ? 0m : Convert.ToDecimal(reader.GetValue(11));
                return new ClienteMensual
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellido = reader.GetString(2),
                    Whatsapp = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Email = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    DNI = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    CUIT = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                    Direccion = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                    Nota = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
                    Activo = reader.GetInt32(9) == 1,
                    Balance = balance,
                    FechaCreacion = reader.GetDateTime(10)
                };
            }
            return null;
        }

        public void ActualizarClienteMensual(ClienteMensual cliente)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE ClientesMensuales 
                SET Nombre = @Nombre, 
                    Apellido = @Apellido, 
                    Whatsapp = @Whatsapp, 
                    Email = @Email, 
                    DNI = @DNI, 
                    CUIT = @CUIT, 
                    Direccion = @Direccion, 
                    Nota = @Nota, 
                    Activo = @Activo
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Nombre", cliente.Nombre);
            command.Parameters.AddWithValue("@Apellido", cliente.Apellido);
            command.Parameters.AddWithValue("@Whatsapp", cliente.Whatsapp ?? string.Empty);
            command.Parameters.AddWithValue("@Email", cliente.Email ?? string.Empty);
            command.Parameters.AddWithValue("@DNI", cliente.DNI ?? string.Empty);
            command.Parameters.AddWithValue("@CUIT", cliente.CUIT ?? string.Empty);
            command.Parameters.AddWithValue("@Direccion", cliente.Direccion ?? string.Empty);
            command.Parameters.AddWithValue("@Nota", cliente.Nota ?? string.Empty);
            command.Parameters.AddWithValue("@Activo", cliente.EstaActivo ? 1 : 0);
            command.Parameters.AddWithValue("@Id", cliente.Id);

            command.ExecuteNonQuery();
        }

        public void EliminarClienteMensual(int clienteId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Primero eliminar los vehículos asociados
            string deleteVehiculos = "DELETE FROM VehiculosMensuales WHERE ClienteId = @ClienteId";
            using var cmdVehiculos = new SQLiteCommand(deleteVehiculos, connection);
            cmdVehiculos.Parameters.AddWithValue("@ClienteId", clienteId);
            cmdVehiculos.ExecuteNonQuery();

            // Luego eliminar los movimientos asociados
            string deleteMovimientos = "DELETE FROM MovimientosMensuales WHERE ClienteId = @ClienteId";
            using var cmdMovimientos = new SQLiteCommand(deleteMovimientos, connection);
            cmdMovimientos.Parameters.AddWithValue("@ClienteId", clienteId);
            cmdMovimientos.ExecuteNonQuery();

            // Finalmente eliminar el cliente
            string deleteCliente = "DELETE FROM ClientesMensuales WHERE Id = @Id";
            using var cmdCliente = new SQLiteCommand(deleteCliente, connection);
            cmdCliente.Parameters.AddWithValue("@Id", clienteId);
            cmdCliente.ExecuteNonQuery();
        }

        public int CrearVehiculoMensual(VehiculoMensual v)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Verificar si existen las columnas nuevas y agregarlas si no existen
            VerificarColumnasVehiculoMensual(connection);

            string query = @"
                INSERT INTO VehiculosMensuales (ClienteId, Matricula, MarcaModelo, CategoriaId, TarifaId, Alternativo, PrecioDiferenciado, 
                                                BonificarHastaFinDeMes, CargoProporcional, CargoMesCompleto, ProximoCargo, Ubicacion, Nota, FechaCreacion)
                VALUES (@ClienteId, @Matricula, @MarcaModelo, @CategoriaId, @TarifaId, @Alternativo, @PrecioDiferenciado, 
                        @BonificarHastaFinDeMes, @CargoProporcional, @CargoMesCompleto, @ProximoCargo, @Ubicacion, @Nota, @FechaCreacion);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@ClienteId", v.ClienteId);
            command.Parameters.AddWithValue("@Matricula", v.Matricula);
            command.Parameters.AddWithValue("@MarcaModelo", v.MarcaModelo);
            command.Parameters.AddWithValue("@CategoriaId", v.CategoriaId.HasValue ? (object)v.CategoriaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@TarifaId", v.TarifaId.HasValue ? (object)v.TarifaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Alternativo", v.Alternativo ? 1 : 0);
            command.Parameters.AddWithValue("@PrecioDiferenciado", v.PrecioDiferenciado ? 1 : 0);
            command.Parameters.AddWithValue("@BonificarHastaFinDeMes", v.GenerarCargoHastaFinDeMes ? 1 : 0);
            command.Parameters.AddWithValue("@CargoProporcional", v.GenerarCargoProporcional ? 1 : 0);
            command.Parameters.AddWithValue("@CargoMesCompleto", v.GenerarCargoMesCompleto ? 1 : 0);
            command.Parameters.AddWithValue("@ProximoCargo", v.ProximoCargo.HasValue ? (object)v.ProximoCargo.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Ubicacion", v.Ubicacion ?? string.Empty);
            command.Parameters.AddWithValue("@Nota", v.Nota ?? string.Empty);
            command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

            var id = (long)command.ExecuteScalar();
            return (int)id;
        }

        private void VerificarColumnasVehiculoMensual(SQLiteConnection connection)
        {
            try
            {
                // Verificar si existen las columnas de generación de cargo
                string checkColumns = "PRAGMA table_info(VehiculosMensuales)";
                using var checkCommand = new SQLiteCommand(checkColumns, connection);
                using var reader = checkCommand.ExecuteReader();
                
                var columnas = new List<string>();
                while (reader.Read())
                {
                    columnas.Add(reader.GetString(1)); // Nombre de la columna
                }
                reader.Close();

                // Agregar columnas si no existen
                if (!columnas.Contains("BonificarHastaFinDeMes"))
                {
                    string alterQuery = "ALTER TABLE VehiculosMensuales ADD COLUMN BonificarHastaFinDeMes INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
                if (!columnas.Contains("CargoProporcional"))
                {
                    string alterQuery = "ALTER TABLE VehiculosMensuales ADD COLUMN CargoProporcional INTEGER NOT NULL DEFAULT 1";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
                if (!columnas.Contains("CargoMesCompleto"))
                {
                    string alterQuery = "ALTER TABLE VehiculosMensuales ADD COLUMN CargoMesCompleto INTEGER NOT NULL DEFAULT 0";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
                if (!columnas.Contains("Ubicacion"))
                {
                    string alterQuery = "ALTER TABLE VehiculosMensuales ADD COLUMN Ubicacion TEXT";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
                if (!columnas.Contains("Nota"))
                {
                    string alterQuery = "ALTER TABLE VehiculosMensuales ADD COLUMN Nota TEXT";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar columnas de VehiculosMensuales: {ex.Message}");
            }
        }

        private void VerificarColumnasMovimientosMensuales(SQLiteConnection connection)
        {
            try
            {
                // Verificar si existe la columna MesAplicado
                string checkColumns = "PRAGMA table_info(MovimientosMensuales)";
                using var checkCommand = new SQLiteCommand(checkColumns, connection);
                using var reader = checkCommand.ExecuteReader();
                
                var columnas = new List<string>();
                while (reader.Read())
                {
                    columnas.Add(reader.GetString(1)); // Nombre de la columna
                }
                reader.Close();

                // Agregar columna si no existe
                if (!columnas.Contains("MesAplicado"))
                {
                    string alterQuery = "ALTER TABLE MovimientosMensuales ADD COLUMN MesAplicado TEXT";
                    using var alterCommand = new SQLiteCommand(alterQuery, connection);
                    alterCommand.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar columnas de MovimientosMensuales: {ex.Message}");
            }
        }

        public List<VehiculoMensual> ObtenerVehiculosMensuales(int? clienteId = null)
        {
            var lista = new List<VehiculoMensual>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();

                string query = @"
                    SELECT Id, ClienteId, Matricula, MarcaModelo, CategoriaId, TarifaId, Alternativo, PrecioDiferenciado, 
                           BonificarHastaFinDeMes, CargoProporcional, CargoMesCompleto, ProximoCargo, Ubicacion, Nota, FechaCreacion
                    FROM VehiculosMensuales
                    WHERE 1=1";

                if (clienteId.HasValue)
                    query += " AND ClienteId = @ClienteId";

                using var command = new SQLiteCommand(query, connection);
                if (clienteId.HasValue)
                    command.Parameters.AddWithValue("@ClienteId", clienteId.Value);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    try
                    {
                        lista.Add(new VehiculoMensual
                        {
                            Id = reader.GetInt32(0),
                            ClienteId = reader.GetInt32(1),
                            Matricula = reader.GetString(2),
                            MarcaModelo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                            CategoriaId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                            TarifaId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            Alternativo = reader.IsDBNull(6) ? false : (reader.GetInt32(6) == 1),
                            PrecioDiferenciado = reader.IsDBNull(7) ? false : (reader.GetInt32(7) == 1),
                            PrecioPersonalizado = null, // Se calcula o se guarda en otra tabla si es necesario
                            GenerarCargoHastaFinDeMes = reader.IsDBNull(8) ? false : (reader.GetInt32(8) == 1),
                            GenerarCargoProporcional = reader.IsDBNull(9) ? true : (reader.GetInt32(9) == 1),
                            GenerarCargoMesCompleto = reader.IsDBNull(10) ? false : (reader.GetInt32(10) == 1),
                            ProximoCargo = reader.IsDBNull(11) ? null : (DateTime?)reader.GetDateTime(11),
                            Ubicacion = reader.IsDBNull(12) ? null : reader.GetString(12),
                            Nota = reader.IsDBNull(13) ? null : reader.GetString(13),
                            FechaCreacion = reader.IsDBNull(14) ? DateTime.Now : reader.GetDateTime(14)
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log del error pero continuar con el siguiente registro
                        System.Diagnostics.Debug.WriteLine($"Error al leer vehículo mensual: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Si hay un error, retornar lista vacía en lugar de lanzar excepción
                System.Diagnostics.Debug.WriteLine($"Error en ObtenerVehiculosMensuales: {ex.Message}");
            }
            return lista;
        }

        public void ActualizarVehiculoMensual(VehiculoMensual vehiculo)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE VehiculosMensuales 
                SET Matricula = @Matricula,
                    MarcaModelo = @MarcaModelo,
                    CategoriaId = @CategoriaId,
                    TarifaId = @TarifaId,
                    Alternativo = @Alternativo,
                    PrecioDiferenciado = @PrecioDiferenciado,
                    BonificarHastaFinDeMes = @BonificarHastaFinDeMes,
                    CargoProporcional = @CargoProporcional,
                    CargoMesCompleto = @CargoMesCompleto,
                    ProximoCargo = @ProximoCargo
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Matricula", vehiculo.Matricula);
            command.Parameters.AddWithValue("@MarcaModelo", vehiculo.MarcaModelo ?? string.Empty);
            command.Parameters.AddWithValue("@CategoriaId", vehiculo.CategoriaId.HasValue ? (object)vehiculo.CategoriaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@TarifaId", vehiculo.TarifaId.HasValue ? (object)vehiculo.TarifaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Alternativo", vehiculo.Alternativo ? 1 : 0);
            command.Parameters.AddWithValue("@PrecioDiferenciado", vehiculo.PrecioDiferenciado ? 1 : 0);
            command.Parameters.AddWithValue("@BonificarHastaFinDeMes", vehiculo.GenerarCargoHastaFinDeMes ? 1 : 0);
            command.Parameters.AddWithValue("@CargoProporcional", vehiculo.GenerarCargoProporcional ? 1 : 0);
            command.Parameters.AddWithValue("@CargoMesCompleto", vehiculo.GenerarCargoMesCompleto ? 1 : 0);
            command.Parameters.AddWithValue("@ProximoCargo", vehiculo.ProximoCargo.HasValue ? (object)vehiculo.ProximoCargo.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Ubicacion", vehiculo.Ubicacion ?? string.Empty);
            command.Parameters.AddWithValue("@Nota", vehiculo.Nota ?? string.Empty);
            command.Parameters.AddWithValue("@Id", vehiculo.Id);

            command.ExecuteNonQuery();
        }

        public void EliminarVehiculoMensual(int vehiculoId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Eliminar movimientos asociados al vehículo (si los hay)
            string deleteMovimientos = "DELETE FROM MovimientosMensuales WHERE VehiculoId = @VehiculoId";
            using var cmdMovimientos = new SQLiteCommand(deleteMovimientos, connection);
            cmdMovimientos.Parameters.AddWithValue("@VehiculoId", vehiculoId);
            cmdMovimientos.ExecuteNonQuery();

            // Eliminar el vehículo
            string deleteVehiculo = "DELETE FROM VehiculosMensuales WHERE Id = @Id";
            using var cmdVehiculo = new SQLiteCommand(deleteVehiculo, connection);
            cmdVehiculo.Parameters.AddWithValue("@Id", vehiculoId);
            cmdVehiculo.ExecuteNonQuery();
        }

        public int CrearMovimientoMensual(MovimientoMensual mov)
        {
            return AgregarMovimientoMensual(mov);
        }

        public int AgregarMovimientoMensual(MovimientoMensual mov)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Para pagos adelantados, no afectan el balance
            // El balance solo se calcula con cargos, pagos normales y ajustes
            if (mov.Tipo != "PagoAdelantado")
            {
                // Obtener balance actual desde los movimientos (excluyendo pagos adelantados)
                decimal balanceActual = 0m;
                string balQuery = "SELECT IFNULL(SUM(Importe),0) FROM MovimientosMensuales WHERE ClienteId = @Id AND Tipo != 'PagoAdelantado'";
                using (var balCmd = new SQLiteCommand(balQuery, connection))
                {
                    balCmd.Parameters.AddWithValue("@Id", mov.ClienteId);
                    var res = balCmd.ExecuteScalar();
                    if (res != null && res != DBNull.Value) 
                        balanceActual = Convert.ToDecimal(res);
                }
                // El nuevo balance se calculará dinámicamente, no se almacena
            }

            string insertQuery = @"
                INSERT INTO MovimientosMensuales (ClienteId, VehiculoId, Fecha, Importe, Descripcion, Tipo, FormaPagoId, AdminId, EsRecibo, MatriculaReferencia, MesAplicado)
                VALUES (@ClienteId, @VehiculoId, @Fecha, @Importe, @Descripcion, @Tipo, @FormaPagoId, @AdminId, @EsRecibo, @MatriculaReferencia, @MesAplicado);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@ClienteId", mov.ClienteId);
            command.Parameters.AddWithValue("@VehiculoId", mov.VehiculoId.HasValue ? (object)mov.VehiculoId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@Fecha", mov.Fecha);
            command.Parameters.AddWithValue("@Importe", mov.Importe);
            command.Parameters.AddWithValue("@Descripcion", mov.Descripcion ?? string.Empty);
            command.Parameters.AddWithValue("@Tipo", mov.Tipo ?? string.Empty);
            command.Parameters.AddWithValue("@FormaPagoId", mov.FormaPagoId.HasValue ? (object)mov.FormaPagoId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@AdminId", mov.AdminId);
            command.Parameters.AddWithValue("@EsRecibo", mov.EsRecibo ? 1 : 0);
            command.Parameters.AddWithValue("@MatriculaReferencia", mov.MatriculaReferencia ?? string.Empty);
            command.Parameters.AddWithValue("@MesAplicado", mov.MesAplicado ?? string.Empty);

            var id = (long)command.ExecuteScalar();

            // El balance se calcula dinámicamente desde los movimientos, no se almacena en la tabla
            // No es necesario actualizar ninguna columna Balance

            return (int)id;
        }

        public List<MovimientoMensual> ObtenerMovimientosMensuales(int clienteId)
        {
            var lista = new List<MovimientoMensual>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                SELECT Id, ClienteId, Fecha, Importe, Descripcion, Tipo, MesAplicado
                FROM MovimientosMensuales
                WHERE ClienteId = @ClienteId
                ORDER BY Fecha DESC, Id DESC";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@ClienteId", clienteId);

            // Calcular balance acumulado
            decimal balanceAcumulado = 0m;
            var movimientosTemp = new List<(int Id, int ClienteId, DateTime Fecha, decimal Importe, string Descripcion, string Tipo, string? MesAplicado)>();
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                movimientosTemp.Add((
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetDateTime(2),
                    reader.GetDecimal(3),
                    reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                    reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6)
                ));
            }
            
            // Calcular balance resultante para cada movimiento (excluyendo pagos adelantados)
            foreach (var m in movimientosTemp.OrderBy(x => x.Fecha).ThenBy(x => x.Id))
            {
                // Solo sumar al balance si NO es un pago adelantado
                if (m.Tipo != "PagoAdelantado")
                {
                    balanceAcumulado += m.Importe;
                }
                lista.Add(new MovimientoMensual
                {
                    Id = m.Id,
                    ClienteId = m.ClienteId,
                    Fecha = m.Fecha,
                    Importe = m.Importe,
                    Descripcion = m.Descripcion,
                    Tipo = m.Tipo,
                    MesAplicado = m.MesAplicado,
                    BalanceResultante = balanceAcumulado
                });
            }
            
            // Invertir para mostrar los más recientes primero
            lista.Reverse();
            return lista;
        }

        public List<Tarifa> ObtenerTarifas()
        {
            var tarifas = new List<Tarifa>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, Tipo, Dias, Horas, Minutos, Tolerancia, FechaCreacion FROM Tarifas ORDER BY Id";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                tarifas.Add(new Tarifa
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Tipo = (TipoTarifa)reader.GetInt32(2),
                    Dias = reader.GetInt32(3),
                    Horas = reader.GetInt32(4),
                    Minutos = reader.GetInt32(5),
                    Tolerancia = reader.GetInt32(6),
                    FechaCreacion = reader.GetDateTime(7)
                });
            }

            return tarifas;
        }

        public Precio? ObtenerPrecio(int tarifaId, int categoriaId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, TarifaId, CategoriaId, Monto, FechaCreacion FROM Precios WHERE TarifaId = @TarifaId AND CategoriaId = @CategoriaId";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@TarifaId", tarifaId);
            command.Parameters.AddWithValue("@CategoriaId", categoriaId);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Precio
                {
                    Id = reader.GetInt32(0),
                    TarifaId = reader.GetInt32(1),
                    CategoriaId = reader.GetInt32(2),
                    Monto = Convert.ToDecimal(reader.GetDouble(3)),
                    FechaCreacion = reader.GetDateTime(4)
                };
            }

            return null;
        }

        public void GuardarPrecio(int tarifaId, int categoriaId, decimal monto)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Verificar si ya existe un precio para esta tarifa y categoría
            string checkQuery = "SELECT COUNT(*) FROM Precios WHERE TarifaId = @TarifaId AND CategoriaId = @CategoriaId";
            using var checkCommand = new SQLiteCommand(checkQuery, connection);
            checkCommand.Parameters.AddWithValue("@TarifaId", tarifaId);
            checkCommand.Parameters.AddWithValue("@CategoriaId", categoriaId);
            long exists = (long)checkCommand.ExecuteScalar();

            if (exists > 0)
            {
                // Actualizar precio existente
                string updateQuery = @"
                    UPDATE Precios 
                    SET Monto = @Monto
                    WHERE TarifaId = @TarifaId AND CategoriaId = @CategoriaId";
                using var updateCommand = new SQLiteCommand(updateQuery, connection);
                updateCommand.Parameters.AddWithValue("@Monto", monto);
                updateCommand.Parameters.AddWithValue("@TarifaId", tarifaId);
                updateCommand.Parameters.AddWithValue("@CategoriaId", categoriaId);
                updateCommand.ExecuteNonQuery();
            }
            else
            {
                // Insertar nuevo precio
                string insertQuery = @"
                    INSERT INTO Precios (TarifaId, CategoriaId, Monto, FechaCreacion)
                    VALUES (@TarifaId, @CategoriaId, @Monto, @FechaCreacion)";
                using var insertCommand = new SQLiteCommand(insertQuery, connection);
                insertCommand.Parameters.AddWithValue("@TarifaId", tarifaId);
                insertCommand.Parameters.AddWithValue("@CategoriaId", categoriaId);
                insertCommand.Parameters.AddWithValue("@Monto", monto);
                insertCommand.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                insertCommand.ExecuteNonQuery();
            }
        }

        public List<Admin> ObtenerTodosLosAdmins()
        {
            var admins = new List<Admin>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, Apellido, Username, Email, Rol, FechaCreacion FROM Administradores ORDER BY FechaCreacion DESC";
            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                admins.Add(new Admin
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellido = reader.GetString(2),
                    Username = reader.GetString(3),
                    Email = reader.GetString(4),
                    Rol = reader.GetString(5),
                    FechaCreacion = reader.GetDateTime(6)
                });
            }

            return admins;
        }

        public Admin? ObtenerAdminPorUsername(string username)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT Id, Nombre, Apellido, Username, Email, Rol, FechaCreacion FROM Administradores WHERE Username = @Username";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return new Admin
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Apellido = reader.GetString(2),
                    Username = reader.GetString(3),
                    Email = reader.GetString(4),
                    Rol = reader.GetString(5),
                    FechaCreacion = reader.GetDateTime(6)
                };
            }

            return null;
        }

        public void ActualizarAdmin(Admin admin)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Administradores 
                SET Nombre = @Nombre, Apellido = @Apellido, Email = @Email, Rol = @Rol
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", admin.Id);
            command.Parameters.AddWithValue("@Nombre", admin.Nombre);
            command.Parameters.AddWithValue("@Apellido", admin.Apellido);
            command.Parameters.AddWithValue("@Email", admin.Email);
            command.Parameters.AddWithValue("@Rol", admin.Rol);

            command.ExecuteNonQuery();
        }

        public void ActualizarPasswordAdmin(int adminId, string nuevaPassword)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string passwordHash = HashPassword(nuevaPassword);

            string query = @"
                UPDATE Administradores 
                SET PasswordHash = @PasswordHash
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", adminId);
            command.Parameters.AddWithValue("@PasswordHash", passwordHash);

            command.ExecuteNonQuery();
        }

        public void ActualizarEmailAdmin(int adminId, string nuevoEmail)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE Administradores 
                SET Email = @Email
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", adminId);
            command.Parameters.AddWithValue("@Email", nuevoEmail);

            command.ExecuteNonQuery();
        }

        public bool ValidarPassword(int adminId, string password)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "SELECT PasswordHash FROM Administradores WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", adminId);

            object? result = command.ExecuteScalar();
            if (result == null) return false;

            string storedHash = result.ToString() ?? string.Empty;
            string inputHash = HashPassword(password);

            return storedHash == inputHash;
        }

        public void EliminarAdmin(int adminId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "DELETE FROM Administradores WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", adminId);

            command.ExecuteNonQuery();
        }

        public DateTime? ObtenerUltimoAccesoAdmin(int adminId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Obtener la fecha más reciente de acceso desde la tabla Accesos
            string query = @"
                SELECT MAX(FechaAcceso) 
                FROM Accesos 
                WHERE AdminId = @AdminId";
            
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@AdminId", adminId);

            object? result = command.ExecuteScalar();
            if (result != null && result != DBNull.Value)
            {
                if (DateTime.TryParse(result.ToString(), out var fechaAcceso))
                    return fechaAcceso;
            }

            // Si no hay accesos registrados, buscar en tickets como fallback
            string queryTickets = @"
                SELECT MAX(FechaCreacion) 
                FROM Tickets 
                WHERE AdminCreadorId = @AdminId OR AdminCerradorId = @AdminId";
            
            using var commandTickets = new SQLiteCommand(queryTickets, connection);
            commandTickets.Parameters.AddWithValue("@AdminId", adminId);

            object? resultTickets = commandTickets.ExecuteScalar();
            if (resultTickets != null && resultTickets != DBNull.Value)
            {
                if (DateTime.TryParse(resultTickets.ToString(), out var fechaTickets))
                    return fechaTickets;
            }

            return null;
        }

        // Métodos para Cámaras ANPR
        public List<CamaraANPR> ObtenerCamarasANPR()
        {
            var camaras = new List<CamaraANPR>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Verificar que la columna SolicitarMotivoApertura exista antes de leer
            VerificarColumnaSolicitarMotivoApertura(connection);

            string query = @"
                SELECT c.Id, c.Nombre, c.Marca, c.Tipo, c.SentidoCirculacion, 
                       c.CapturaSinMatricula, c.EncuadreVehiculo, c.ConBarrerasVehiculares,
                       c.RetardoAperturaSegundos, c.RetardoCierreSegundos, c.AperturaManual,
                       c.SolicitarMotivoApertura, c.ToleranciaSalidaMinutos, c.PreIngresoActivo, c.ImpresoraId,
                       c.CategoriaPredeterminadaId, c.HostIP, c.Usuario, c.Clave,
                       c.Activa, c.FechaCreacion,
                       cat.Nombre as CategoriaNombre
                FROM CamarasANPR c
                LEFT JOIN Categorias cat ON c.CategoriaPredeterminadaId = cat.Id
                ORDER BY c.Nombre";

            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                camaras.Add(new CamaraANPR
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Marca = reader.GetString(2),
                    Tipo = reader.GetString(3),
                    SentidoCirculacion = reader.GetString(4),
                    CapturaSinMatricula = reader.GetInt32(5) == 1,
                    EncuadreVehiculo = reader.GetInt32(6) == 1,
                    ConBarrerasVehiculares = reader.GetInt32(7) == 1,
                    RetardoAperturaSegundos = reader.GetInt32(8),
                    RetardoCierreSegundos = reader.GetInt32(9),
                    AperturaManual = reader.GetInt32(10) == 1,
                    SolicitarMotivoApertura = reader.IsDBNull(11) ? false : reader.GetInt32(11) == 1,
                    ToleranciaSalidaMinutos = reader.GetInt32(12),
                    PreIngresoActivo = reader.GetInt32(13) == 1,
                    ImpresoraId = reader.IsDBNull(14) ? null : reader.GetString(14),
                    CategoriaPredeterminadaId = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    HostIP = reader.GetString(16),
                    Usuario = reader.GetString(17),
                    Clave = reader.GetString(18),
                    Activa = reader.GetInt32(19) == 1,
                    FechaCreacion = reader.GetDateTime(20),
                    CategoriaPredeterminadaNombre = reader.IsDBNull(21) ? string.Empty : reader.GetString(21)
                });
            }

            return camaras;
        }

        public int CrearCamaraANPR(CamaraANPR camara)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                INSERT INTO CamarasANPR (Nombre, Marca, Tipo, SentidoCirculacion, CapturaSinMatricula, 
                    EncuadreVehiculo, ConBarrerasVehiculares, RetardoAperturaSegundos, RetardoCierreSegundos,
                    AperturaManual, SolicitarMotivoApertura, ToleranciaSalidaMinutos, PreIngresoActivo, ImpresoraId, 
                    CategoriaPredeterminadaId, HostIP, Usuario, Clave, Activa, FechaCreacion)
                VALUES (@Nombre, @Marca, @Tipo, @SentidoCirculacion, @CapturaSinMatricula,
                    @EncuadreVehiculo, @ConBarrerasVehiculares, @RetardoAperturaSegundos, @RetardoCierreSegundos,
                    @AperturaManual, @SolicitarMotivoApertura, @ToleranciaSalidaMinutos, @PreIngresoActivo, @ImpresoraId,
                    @CategoriaPredeterminadaId, @HostIP, @Usuario, @Clave, @Activa, @FechaCreacion);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Nombre", camara.Nombre);
            command.Parameters.AddWithValue("@Marca", camara.Marca);
            command.Parameters.AddWithValue("@Tipo", camara.Tipo);
            command.Parameters.AddWithValue("@SentidoCirculacion", camara.SentidoCirculacion);
            command.Parameters.AddWithValue("@CapturaSinMatricula", camara.CapturaSinMatricula ? 1 : 0);
            command.Parameters.AddWithValue("@EncuadreVehiculo", camara.EncuadreVehiculo ? 1 : 0);
            command.Parameters.AddWithValue("@ConBarrerasVehiculares", camara.ConBarrerasVehiculares ? 1 : 0);
            command.Parameters.AddWithValue("@RetardoAperturaSegundos", camara.RetardoAperturaSegundos);
            command.Parameters.AddWithValue("@RetardoCierreSegundos", camara.RetardoCierreSegundos);
            command.Parameters.AddWithValue("@AperturaManual", camara.AperturaManual ? 1 : 0);
            command.Parameters.AddWithValue("@SolicitarMotivoApertura", camara.SolicitarMotivoApertura ? 1 : 0);
            command.Parameters.AddWithValue("@ToleranciaSalidaMinutos", camara.ToleranciaSalidaMinutos);
            command.Parameters.AddWithValue("@PreIngresoActivo", camara.PreIngresoActivo ? 1 : 0);
            command.Parameters.AddWithValue("@ImpresoraId", camara.ImpresoraId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoriaPredeterminadaId", 
                camara.CategoriaPredeterminadaId.HasValue ? (object)camara.CategoriaPredeterminadaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@HostIP", camara.HostIP);
            command.Parameters.AddWithValue("@Usuario", camara.Usuario);
            command.Parameters.AddWithValue("@Clave", camara.Clave);
            command.Parameters.AddWithValue("@Activa", camara.Activa ? 1 : 0);
            command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);

            var id = (long)command.ExecuteScalar();
            return (int)id;
        }

        public void ActualizarCamaraANPR(CamaraANPR camara)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = @"
                UPDATE CamarasANPR 
                SET Nombre = @Nombre, Marca = @Marca, Tipo = @Tipo, SentidoCirculacion = @SentidoCirculacion,
                    CapturaSinMatricula = @CapturaSinMatricula, EncuadreVehiculo = @EncuadreVehiculo,
                    ConBarrerasVehiculares = @ConBarrerasVehiculares, RetardoAperturaSegundos = @RetardoAperturaSegundos,
                    RetardoCierreSegundos = @RetardoCierreSegundos, AperturaManual = @AperturaManual,
                    SolicitarMotivoApertura = @SolicitarMotivoApertura, ToleranciaSalidaMinutos = @ToleranciaSalidaMinutos, 
                    PreIngresoActivo = @PreIngresoActivo, ImpresoraId = @ImpresoraId, CategoriaPredeterminadaId = @CategoriaPredeterminadaId,
                    HostIP = @HostIP, Usuario = @Usuario, Clave = @Clave, Activa = @Activa
                WHERE Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", camara.Id);
            command.Parameters.AddWithValue("@Nombre", camara.Nombre);
            command.Parameters.AddWithValue("@Marca", camara.Marca);
            command.Parameters.AddWithValue("@Tipo", camara.Tipo);
            command.Parameters.AddWithValue("@SentidoCirculacion", camara.SentidoCirculacion);
            command.Parameters.AddWithValue("@CapturaSinMatricula", camara.CapturaSinMatricula ? 1 : 0);
            command.Parameters.AddWithValue("@EncuadreVehiculo", camara.EncuadreVehiculo ? 1 : 0);
            command.Parameters.AddWithValue("@ConBarrerasVehiculares", camara.ConBarrerasVehiculares ? 1 : 0);
            command.Parameters.AddWithValue("@RetardoAperturaSegundos", camara.RetardoAperturaSegundos);
            command.Parameters.AddWithValue("@RetardoCierreSegundos", camara.RetardoCierreSegundos);
            command.Parameters.AddWithValue("@AperturaManual", camara.AperturaManual ? 1 : 0);
            command.Parameters.AddWithValue("@SolicitarMotivoApertura", camara.SolicitarMotivoApertura ? 1 : 0);
            command.Parameters.AddWithValue("@ToleranciaSalidaMinutos", camara.ToleranciaSalidaMinutos);
            command.Parameters.AddWithValue("@PreIngresoActivo", camara.PreIngresoActivo ? 1 : 0);
            command.Parameters.AddWithValue("@ImpresoraId", camara.ImpresoraId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoriaPredeterminadaId", 
                camara.CategoriaPredeterminadaId.HasValue ? (object)camara.CategoriaPredeterminadaId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@HostIP", camara.HostIP);
            command.Parameters.AddWithValue("@Usuario", camara.Usuario);
            command.Parameters.AddWithValue("@Clave", camara.Clave);
            command.Parameters.AddWithValue("@Activa", camara.Activa ? 1 : 0);

            command.ExecuteNonQuery();
        }

        public void EliminarCamaraANPR(int camaraId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "DELETE FROM CamarasANPR WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", camaraId);
            command.ExecuteNonQuery();
        }

        public CamaraANPR? ObtenerCamaraANPRPorId(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Verificar que la columna SolicitarMotivoApertura exista antes de leer
            VerificarColumnaSolicitarMotivoApertura(connection);

            string query = @"
                SELECT c.Id, c.Nombre, c.Marca, c.Tipo, c.SentidoCirculacion, 
                       c.CapturaSinMatricula, c.EncuadreVehiculo, c.ConBarrerasVehiculares,
                       c.RetardoAperturaSegundos, c.RetardoCierreSegundos, c.AperturaManual,
                       c.SolicitarMotivoApertura, c.ToleranciaSalidaMinutos, c.PreIngresoActivo, c.ImpresoraId,
                       c.CategoriaPredeterminadaId, c.HostIP, c.Usuario, c.Clave,
                       c.Activa, c.FechaCreacion,
                       cat.Nombre as CategoriaNombre
                FROM CamarasANPR c
                LEFT JOIN Categorias cat ON c.CategoriaPredeterminadaId = cat.Id
                WHERE c.Id = @Id";

            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", id);
            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                return new CamaraANPR
                {
                    Id = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Marca = reader.GetString(2),
                    Tipo = reader.GetString(3),
                    SentidoCirculacion = reader.GetString(4),
                    CapturaSinMatricula = reader.GetInt32(5) == 1,
                    EncuadreVehiculo = reader.GetInt32(6) == 1,
                    ConBarrerasVehiculares = reader.GetInt32(7) == 1,
                    RetardoAperturaSegundos = reader.GetInt32(8),
                    RetardoCierreSegundos = reader.GetInt32(9),
                    AperturaManual = reader.GetInt32(10) == 1,
                    SolicitarMotivoApertura = reader.IsDBNull(11) ? false : reader.GetInt32(11) == 1,
                    ToleranciaSalidaMinutos = reader.GetInt32(12),
                    PreIngresoActivo = reader.GetInt32(13) == 1,
                    ImpresoraId = reader.IsDBNull(14) ? null : reader.GetString(14),
                    CategoriaPredeterminadaId = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                    HostIP = reader.GetString(16),
                    Usuario = reader.GetString(17),
                    Clave = reader.GetString(18),
                    Activa = reader.GetInt32(19) == 1,
                    FechaCreacion = reader.GetDateTime(20),
                    CategoriaPredeterminadaNombre = reader.IsDBNull(21) ? string.Empty : reader.GetString(21)
                };
            }

            return null;
        }

        // Métodos para Lista Blanca ANPR
        public List<(int Id, string Matricula, string? Descripcion, DateTime FechaCreacion)> ObtenerListaBlancaANPR()
        {
            var lista = new List<(int, string, string?, DateTime)>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT Id, Matricula, Descripcion, FechaCreacion FROM ListaBlancaANPR ORDER BY Matricula";
                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string matricula = reader.GetString(1);
                    string? descripcion = reader.IsDBNull(2) ? null : reader.GetString(2);
                    DateTime fechaCreacion = reader.GetDateTime(3);
                    lista.Add((id, matricula, descripcion, fechaCreacion));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener lista blanca: {ex.Message}");
            }
            return lista;
        }

        public bool EstaEnListaBlanca(string matricula)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT COUNT(*) FROM ListaBlancaANPR WHERE UPPER(Matricula) = UPPER(@Matricula)";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Matricula", matricula?.Trim() ?? "");
                long count = (long)command.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar lista blanca: {ex.Message}");
                return false;
            }
        }

        public int AgregarAListaBlanca(string matricula, string? descripcion = null)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = @"
                    INSERT INTO ListaBlancaANPR (Matricula, Descripcion, FechaCreacion)
                    VALUES (@Matricula, @Descripcion, @FechaCreacion);
                    SELECT last_insert_rowid();";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Matricula", matricula?.Trim().ToUpper() ?? "");
                command.Parameters.AddWithValue("@Descripcion", descripcion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                var id = (long)command.ExecuteScalar();
                return (int)id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al agregar a lista blanca: {ex.Message}");
                throw;
            }
        }

        public void EliminarDeListaBlanca(int id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "DELETE FROM ListaBlancaANPR WHERE Id = @Id";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar de lista blanca: {ex.Message}");
                throw;
            }
        }

        // Métodos para Lista Negra ANPR
        public List<(int Id, string Matricula, string? Descripcion, DateTime FechaCreacion)> ObtenerListaNegraANPR()
        {
            var lista = new List<(int, string, string?, DateTime)>();
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT Id, Matricula, Descripcion, FechaCreacion FROM ListaNegraANPR ORDER BY Matricula";
                using var command = new SQLiteCommand(query, connection);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string matricula = reader.GetString(1);
                    string? descripcion = reader.IsDBNull(2) ? null : reader.GetString(2);
                    DateTime fechaCreacion = reader.GetDateTime(3);
                    lista.Add((id, matricula, descripcion, fechaCreacion));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al obtener lista negra: {ex.Message}");
            }
            return lista;
        }

        public bool EstaEnListaNegra(string matricula)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "SELECT COUNT(*) FROM ListaNegraANPR WHERE UPPER(Matricula) = UPPER(@Matricula)";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Matricula", matricula?.Trim() ?? "");
                long count = (long)command.ExecuteScalar();
                return count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al verificar lista negra: {ex.Message}");
                return false;
            }
        }

        public int AgregarAListaNegra(string matricula, string? descripcion = null)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = @"
                    INSERT INTO ListaNegraANPR (Matricula, Descripcion, FechaCreacion)
                    VALUES (@Matricula, @Descripcion, @FechaCreacion);
                    SELECT last_insert_rowid();";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Matricula", matricula?.Trim().ToUpper() ?? "");
                command.Parameters.AddWithValue("@Descripcion", descripcion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@FechaCreacion", DateTime.Now);
                var id = (long)command.ExecuteScalar();
                return (int)id;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al agregar a lista negra: {ex.Message}");
                throw;
            }
        }

        public void EliminarDeListaNegra(int id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                connection.Open();
                string query = "DELETE FROM ListaNegraANPR WHERE Id = @Id";
                using var command = new SQLiteCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al eliminar de lista negra: {ex.Message}");
                throw;
            }
        }

        // Métodos para Categorías Dahua
        private void InicializarCategoriasDahuaPorDefecto(SQLiteConnection connection)
        {
            try
            {
                var categoriasDahua = new List<(string Codigo, string Descripcion)>
                {
                    ("HEAVYTRUCK", "Camión pesado"),
                    ("MEDIUMTRUCK", "Camión mediano"),
                    ("SALOONCAR", "Auto"),
                    ("VAN", "Furgoneta"),
                    ("LIGHTTRUCK", "Camión ligero"),
                    ("SUV", "SUV (vehículo utilitario deportivo)"),
                    ("MPV", "Monovolumen (minivan)"),
                    ("PICKUP", "Camioneta (pickup)"),
                    ("MICROBUS", "Autobús pequeño"),
                    ("MEDIUMBUS", "Autobús mediano"),
                    ("LARGEBUS", "Autobús grande"),
                    ("MOTORCYCLE", "Motocicleta")
                };

                foreach (var (codigo, descripcion) in categoriasDahua)
                {
                    try
                    {
                        string checkQuery = "SELECT COUNT(*) FROM CategoriasDahua WHERE Codigo = @Codigo";
                        using var checkCommand = new SQLiteCommand(checkQuery, connection);
                        checkCommand.Parameters.AddWithValue("@Codigo", codigo);
                        var exists = (long)checkCommand.ExecuteScalar() > 0;

                        if (!exists)
                        {
                            string insertQuery = "INSERT INTO CategoriasDahua (Codigo, Descripcion, CategoriaId) VALUES (@Codigo, @Descripcion, NULL)";
                            using var insertCommand = new SQLiteCommand(insertQuery, connection);
                            insertCommand.Parameters.AddWithValue("@Codigo", codigo);
                            insertCommand.Parameters.AddWithValue("@Descripcion", descripcion);
                            insertCommand.ExecuteNonQuery();
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error al insertar categoría Dahua {codigo}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al inicializar categorías Dahua: {ex.Message}");
            }
        }

        public List<CategoriaDahua> ObtenerCategoriasDahua()
        {
            var categorias = new List<CategoriaDahua>();
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // Asegurar que todas las categorías Dahua existan
            InicializarCategoriasDahuaPorDefecto(connection);

            string query = @"
                SELECT cd.Id, cd.Codigo, cd.Descripcion, cd.CategoriaId,
                       cat.Nombre as CategoriaNombre
                FROM CategoriasDahua cd
                LEFT JOIN Categorias cat ON cd.CategoriaId = cat.Id
                ORDER BY cd.Id";

            using var command = new SQLiteCommand(query, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                categorias.Add(new CategoriaDahua
                {
                    Id = reader.GetInt32(0),
                    Codigo = reader.GetString(1),
                    Descripcion = reader.GetString(2),
                    CategoriaId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                    CategoriaNombre = reader.IsDBNull(4) ? string.Empty : reader.GetString(4)
                });
            }

            return categorias;
        }

        public void ActualizarMapeoCategoriaDahua(int categoriaDahuaId, int? categoriaId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            string query = "UPDATE CategoriasDahua SET CategoriaId = @CategoriaId WHERE Id = @Id";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Id", categoriaDahuaId);
            command.Parameters.AddWithValue("@CategoriaId", categoriaId.HasValue ? (object)categoriaId.Value : DBNull.Value);
            command.ExecuteNonQuery();
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}

