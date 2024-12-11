using Core.Entities.Almacenes;

namespace Core.Entities.Login
{
    public class Usuario
    {
        public int Id { get; set; }  // Identificador único
        public string ApellidoPaterno { get; set; }
        public string ApellidoMaterno { get; set; }
        public string Nombres { get; set; }
        public string UsuarioNombre { get; set; }  // Nombre de usuario
        public string Email { get; set; }  // Correo electrónico
        public string PasswordHash { get; set; }  // Hash de la contraseña
        public string PasswordSalt { get; set; }  // Salt de la contraseña
        public bool EstaBloqueado { get; set; }  // Estado de bloqueo
        public bool EstaActivo { get; set; }  // Estado activo o inactivo
        public DateTime FechaCreacion { get; set; }  // Fecha de creación
        public DateTime? FechaModificacion { get; set; }  // Fecha de última modificación
        public List<Almacen> Almacenes { get; set; }
    }
}
