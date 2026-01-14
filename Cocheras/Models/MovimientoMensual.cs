using System;

namespace Cocheras.Models
{
    public class MovimientoMensual
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int? VehiculoId { get; set; }
        public string Tipo { get; set; } = string.Empty; // "Cargo", "Pago", "Ajuste"
        public decimal Importe { get; set; } // Negativo para cargos, positivo para pagos
        public string Descripcion { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public int? FormaPagoId { get; set; }
        public int AdminId { get; set; }
        public bool EsRecibo { get; set; }
        public string MatriculaReferencia { get; set; } = string.Empty;

        // Propiedades de apoyo (no en DB)
        public string AdminNombre { get; set; } = string.Empty;
        public string FormaPagoNombre { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        
        // Propiedades alias para compatibilidad
        public decimal BalanceResultante 
        { 
            get => Balance; 
            set => Balance = value; 
        }
        
        public string MesAplicado { get; set; } = string.Empty;
    }
}
