using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IReporteTransferenciaStockRepository
    {
        MemoryStream GenerarReporte(string sessionID, int docEntry, string tipoDocumento);
    }
}
