using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Sap
{
    public class DocumentLine
    {
        public int LineNum { get; set; }
        public int U_PCK_CantContada { get; set; }
        public string U_PCK_ContUsuarios { get; set; }
    }
    public class Document
    {
        public List<DocumentLine> DocumentLines { get; set; }
    }
}
