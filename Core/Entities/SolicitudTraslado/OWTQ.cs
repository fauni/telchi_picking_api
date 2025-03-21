using Core.Entities.Picking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.SolicitudTraslado
{
    public class OWTQ
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public int Series { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime DocDueDate { get; set; }
        public string Comments { get; set; }
        public string JrnlMemo { get; set; }
        public string Filler { get; set; }
        public string ToWhsCode { get; set; }
        public string DocStatus { get; set; }
        public Documento Documento { get; set; }
        public List<WTQ1> Lines { get; set; } 
    }
}
