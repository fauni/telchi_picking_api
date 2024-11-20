using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Sap
{
    public class ResultadoActualizacionSap
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; }
        public string CodigoEstado { get; set; } // Opcional: Código HTTP de la respuesta
    }
}
