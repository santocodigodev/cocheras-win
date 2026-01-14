using System;

namespace Cocheras.Models
{
    public class FormaPago
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
    }
}

