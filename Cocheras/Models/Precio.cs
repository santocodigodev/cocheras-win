namespace Cocheras.Models
{
    public class Precio
    {
        public int Id { get; set; }
        public int TarifaId { get; set; }
        public int CategoriaId { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}

