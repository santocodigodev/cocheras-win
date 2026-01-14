using System;

namespace Cocheras.Models
{
    public class ClienteMensual
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Whatsapp { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public string CUIT { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Nota { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; }
        public bool EstaActivo { get; set; } = true;
        public bool TieneDeuda { get; set; }

        // Propiedades calculadas (no en DB)
        public decimal Balance { get; set; }
        public int CantidadVehiculos { get; set; }
        public string NombreCompleto => $"{Nombre} {Apellido}".Trim();
        
        // Propiedades alias para compatibilidad
        public bool Activo 
        { 
            get => EstaActivo; 
            set => EstaActivo = value; 
        }
        
        public int CantVehiculos 
        { 
            get => CantidadVehiculos; 
            set => CantidadVehiculos = value; 
        }
        
        public string MatriculasConcat { get; set; } = string.Empty;
    }
}
