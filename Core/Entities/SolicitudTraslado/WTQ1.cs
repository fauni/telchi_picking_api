using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.SolicitudTraslado
{
    public class WTQ1
    {
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string LineStatus { get; set; }
        public string ItemCode { get; set; }
        public string Dscription { get; set; }
        public string CodeBars { get; set; }
        public decimal Quantity { get; set; }
        public string FromWhsCod { get; set; }
        public string WhsCode { get; set; }
        public decimal? U_PCK_CantContada { get; set; }
        public string U_PCK_ContUsuarios { get; set; }
        public DetalleDocumento DetalleDocumento { get; set; }
    }
}
