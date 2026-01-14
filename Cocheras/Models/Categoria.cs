namespace Cocheras.Models
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public DateTime FechaCreacion { get; set; }
        
        // Propiedad calculada para el atajo de teclado (dinámico según el orden)
        public string AtajoTeclado
        {
            get
            {
                if (Orden >= 1)
                {
                    return $"[F{Orden}]";
                }
                return string.Empty;
            }
        }
    }
}

