namespace Cocheras.Models
{
    public class Admin
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = "operador"; // "admin" o "operador"
        public DateTime FechaCreacion { get; set; }
    }
}

