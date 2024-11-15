using Core.Entities.Picking;
using Core.Entities.Ventas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IDocumentoRepository
    {
        Task<int> CreateDocumentFromOrderAsync(Order order, string tipoDocumnento);
        Task<int> InsertDocumentAsync(Documento documento);
        Task<Documento> GetDocumentByDocNumAsync(string id);
        Task ActualizarEstadoDocumentoAsync(int idDocumento);
        Task<List<Documento>> GetDocumentosPorOrderIdAsync(int orderId);
    }
}
