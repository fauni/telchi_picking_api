using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Conteos
{
    public class DetalleConteo
    {
        public int Id { get; set; }  // Primary Key
        public int ConteoId { get; set; }  // Foreign Key a la tabla Conteo
        public string CodigoItem { get; set; }  // Código del artículo
        public string CodigoBarras { get; set; } // Codigo de Barras
        public string CodigoAlmacen { get; set; }  // Código del almacén
        public string DescripcionItem { get; set; }  // Descripción del artículo
        public decimal CantidadDisponible { get; set; }  // Cantidad disponible en el almacén
        public decimal CantidadComprometida { get; set; }  // Cantidad comprometida
        public decimal CantidadPendienteDeRecibir { get; set; }  // Cantidad pendiente de recibir
        public decimal CantidadContada { get; set; }  // Cantidad contada en el conteo
        public string Estado { get; set; }  // Estado del detalle (Pendiente, Completado, En Proceso)

        // Relación con la tabla Conteo
        public virtual Conteo Conteo { get; set; }
    }

}
