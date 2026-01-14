namespace Cocheras.Models
{
    public class Estacionamiento
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Slogan { get; set; }
        public string Impresora { get; set; } = string.Empty;
        public string Tema { get; set; } = "light";
        public DateTime FechaCreacion { get; set; }
    }
}

