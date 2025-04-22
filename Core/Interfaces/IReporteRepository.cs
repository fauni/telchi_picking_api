using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IReporteRepository
    {
        MemoryStream GenerarReporte(string sessionID, string docNum, string tipoDocumento);
    }
}
