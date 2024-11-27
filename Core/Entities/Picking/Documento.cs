using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Picking
{
    public class Documento
    {
        public int IdDocumento { get; set; }
        public string TipoDocumento { get; set; } = string.Empty;
        public string NumeroDocumento { get; set; } = string.Empty;
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFinalizacion { get; set; }
        public string EstadoConteo { get; set; }
        public int DocEntry { get; set; }
        public string ActualizadoSap { get; set; }

        // Lista para DetalleDocumento si deseas cargar los detalles
        public List<DetalleDocumento> Detalles { get; set; } = new List<DetalleDocumento>();
    }
}
