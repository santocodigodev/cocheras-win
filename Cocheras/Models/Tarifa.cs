namespace Cocheras.Models
{
    public enum TipoTarifa
    {
        PorHora = 1,
        PorTurno = 2,
        PorEstadia = 3,
        Mensual = 4
    }

    public class Tarifa
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public TipoTarifa Tipo { get; set; }
        public int Dias { get; set; }
        public int Horas { get; set; }
        public int Minutos { get; set; }
        public int Tolerancia { get; set; } // En minutos
        public DateTime FechaCreacion { get; set; }
    }
}

