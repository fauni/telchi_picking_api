using Core.Entities.Login;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Almacenes
{
    public class UsuarioAlmacen
    {
        public int UsuarioId { get; set; } // Identificador del usuario
        public int AlmacenId { get; set; } // Identificador del almacén
        public DateTime FechaAsignacion { get; set; } = DateTime.Now; // Fecha de asignación, con valor por defecto

        // Propiedades de navegación
        public Usuario Usuario { get; set; } // Relación con la entidad Usuario
        public Almacen Almacen { get; set; } // Relación con la entidad Almacen
    }
}
