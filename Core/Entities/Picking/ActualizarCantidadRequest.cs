using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Picking
{
    public class ActualizarCantidadRequest
    {
        public decimal CantidadAgregada { get; set; }
        public string Usuario { get; set; }
    }
}
