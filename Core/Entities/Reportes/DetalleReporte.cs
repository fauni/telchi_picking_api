using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Reportes
{
    public class DetalleReporte
    {
        public string CodigoItem { get; set; }
        public string DescripcionItem { get; set; }
        public decimal CantidadEsperada { get; set; }
        public decimal CantidadContada { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public decimal CantidadAgregada { get; set; }
        public string Usuario { get; set; }
    }
}
