using System;

namespace Cocheras.Models
{
    public class Transaccion
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int? AdminId { get; set; }
        public int FormaPagoId { get; set; }
        public decimal Importe { get; set; }
        public DateTime Fecha { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool ConRecibo { get; set; }
        public bool EsSalidaInmediata { get; set; }
        public string ItemsDetalle { get; set; } = string.Empty;
    }
}

