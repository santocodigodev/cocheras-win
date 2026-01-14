namespace Cocheras.Models
{
    public class Ticket
    {
        public int Id { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public string? ImagenPath { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string NotaAdicional { get; set; } = string.Empty;
        public int? TarifaId { get; set; }
        public int? CategoriaId { get; set; }
        public DateTime FechaEntrada { get; set; }
        public DateTime? FechaSalida { get; set; }
        public bool EstaAbierto { get; set; } = true;
        public bool EstaCancelado { get; set; } = false;
        public string MotivoCancelacion { get; set; } = string.Empty;
        public DateTime? FechaCancelacion { get; set; }
        public decimal? Monto { get; set; }
        public int? AdminCreadorId { get; set; }
        public int? AdminCerradorId { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Propiedades de apoyo (no mapeadas)
        public string TarifaNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public string AdminCreadorNombre { get; set; } = string.Empty;
        public string AdminCerradorNombre { get; set; } = string.Empty;
        public string TiempoTotalDisplay { get; set; } = string.Empty;
        public string MontoDisplay { get; set; } = string.Empty;
        public string ObservacionDisplay { get; set; } = string.Empty;
    }
}

