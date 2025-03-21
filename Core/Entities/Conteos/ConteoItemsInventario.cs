using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Conteos
{
    /// <summary>
    /// Representa un registro de la tabla ConteoItemsInventario.
    /// </summary>
    public class ConteoItemsInventario
    {
        public int IdConteo { get; set; }
        public int IdDetalleConteo { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaHoraConteo { get; set; }
        public decimal CantidadContada { get; set; }
        public decimal CantidadContadaAnterior { get; set; }
        public decimal CantidadAgregada { get; set; }
    }
}
