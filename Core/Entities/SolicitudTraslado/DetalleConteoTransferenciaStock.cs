using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.SolicitudTraslado
{
    public class DetalleConteoTransferenciaStock
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string U_DocType { get; set; }
        public string U_ItemCode { get; set; }
        public decimal U_QuantityCounted { get; set; }
        public string U_Username { get; set; }
    }
}