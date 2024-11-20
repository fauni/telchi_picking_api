using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Picking
{
    public class DetalleDocumentoToSap
    {
        public int DocEntry { get; set; }
        public long NumeroDocumento { get; set; }
        public int IdDetalle { get; set; }
        public string CodigoItem { get; set; }
        public int NumeroLinea { get; set; }
        public decimal TotalCantidadContada { get; set; }
        public string UsuariosParticipantes { get; set; }
    }
}
