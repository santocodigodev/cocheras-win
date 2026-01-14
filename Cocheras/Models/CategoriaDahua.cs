namespace Cocheras.Models
{
    public class CategoriaDahua
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty; // HEAVYTRUCK, SALOONCAR, etc.
        public string Descripcion { get; set; } = string.Empty; // Descripción en español
        public int? CategoriaId { get; set; } // ID de la categoría interna mapeada (null si no está mapeada)
        
        // Propiedad de apoyo
        public string CategoriaNombre { get; set; } = string.Empty;
    }
}

