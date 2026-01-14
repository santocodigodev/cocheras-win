namespace Cocheras.Models
{
    public class CamaraANPR
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Marca { get; set; } = string.Empty; // Dahua, Hikvision, etc.
        public string Tipo { get; set; } = string.Empty; // "Entrada", "Salida", "Entrada y Salida"
        public string SentidoCirculacion { get; set; } = string.Empty; // "Se acerca", "Se aleja"
        public bool CapturaSinMatricula { get; set; } = false;
        public bool EncuadreVehiculo { get; set; } = false;
        public bool ConBarrerasVehiculares { get; set; } = false;
        public int RetardoAperturaSegundos { get; set; } = 0;
        public int RetardoCierreSegundos { get; set; } = 0;
        public bool AperturaManual { get; set; } = false;
        public bool SolicitarMotivoApertura { get; set; } = false;
        public int ToleranciaSalidaMinutos { get; set; } = 0;
        public bool PreIngresoActivo { get; set; } = false;
        public string? ImpresoraId { get; set; } // ID de la impresora o null
        public int? CategoriaPredeterminadaId { get; set; }
        public string HostIP { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
        public string Clave { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public bool Activa { get; set; } = true;

        // Propiedades de apoyo
        public string CategoriaPredeterminadaNombre { get; set; } = string.Empty;
        public string ImpresoraNombre { get; set; } = string.Empty;
        
        // Propiedades calculadas para display
        public string BarrerasVehicularesDisplay => ConBarrerasVehiculares ? "SI" : "NO";
        public string PreIngresoDisplay => PreIngresoActivo ? "SI" : "NO";
        public string EndpointDisplay => string.IsNullOrEmpty(HostIP) ? "" : $"http://{HostIP}:5200/in";
    }
}

