using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Conteos
{
    /// <summary>
    /// DTO para encapsular los datos necesarios para registrar un conteo.
    /// </summary>
    public class ConteoItemRequest
    {
        /// <summary>
        /// ID del detalle del conteo al que se asocia el registro.
        /// </summary>
        public int IdDetalleConteo { get; set; }

        /// <summary>
        /// Nombre del usuario que realiza el conteo.
        /// </summary>
        public string Usuario { get; set; }

        /// <summary>
        /// Cantidad agregada al conteo.
        /// </summary>
        public decimal CantidadAgregada { get; set; }
    }
}
