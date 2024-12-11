using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Almacenes
{
    public class Almacen
    {
        public int Id { get; set; } // Identificador único del almacén (Primary Key)
        public string Codigo { get; set; } // Código del almacén (máx. 10 caracteres)
        public string Nombre { get; set; } // Nombre del almacén (máx. 100 caracteres)
    }
}
