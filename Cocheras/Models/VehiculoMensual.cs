using System;

namespace Cocheras.Models
{
    public class VehiculoMensual
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public string Matricula { get; set; } = string.Empty;
        public string MarcaModelo { get; set; } = string.Empty;
        public int? CategoriaId { get; set; }
        public bool EsAlternativo { get; set; }
        public bool TienePrecioDiferenciado { get; set; }
        public int? TarifaId { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? ProximoCargo { get; set; }
        public bool GenerarCargoHastaFinDeMes { get; set; }
        public bool GenerarCargoProporcional { get; set; }
        public bool GenerarCargoMesCompleto { get; set; }
        public string? Ubicacion { get; set; }
        public string? Nota { get; set; }

        // Propiedades de apoyo (no en DB)
        public string ClienteNombre { get; set; } = string.Empty;
        public string CategoriaNombre { get; set; } = string.Empty;
        public string TarifaNombre { get; set; } = string.Empty;
        public decimal? ValorMensual { get; set; }
        public string MarcaModeloDisplay => string.IsNullOrEmpty(MarcaModelo) ? $"Sin Marca - {CategoriaNombre}" : $"{MarcaModelo} - {CategoriaNombre}";
        
        // Propiedades alias para compatibilidad
        public bool Alternativo 
        { 
            get => EsAlternativo; 
            set => EsAlternativo = value; 
        }
        
        public bool PrecioDiferenciado 
        { 
            get => TienePrecioDiferenciado; 
            set => TienePrecioDiferenciado = value; 
        }
        
        public decimal? PrecioPersonalizado { get; set; }
        
        // Alias adicional
        public bool VehiculoAlternativo 
        { 
            get => EsAlternativo; 
            set => EsAlternativo = value; 
        }
    }
}
