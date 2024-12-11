using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Conteos
{
    public class Conteo
    {
        public int Id { get; set; }  // Primary Key
        public string NombreUsuario { get; set; }  // Nombre del usuario que realizó el conteo
        public string CodigoAlmacen { get; set; }  // Código del almacén
        public string Comentarios { get; set; }  // Comentarios adicionales
        public DateTime FechaInicio { get; set; }  // Fecha de inicio del conteo
        public DateTime? FechaFinalizacion { get; set; }  // Fecha de finalización del conteo (nullable)
        public string Estado { get; set; }  // Estado del conteo (Iniciado, Finalizado, En Proceso)
    }

}
