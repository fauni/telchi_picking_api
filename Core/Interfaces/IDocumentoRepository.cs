using Core.Entities.Picking;
using Core.Entities.Sap;
using Core.Entities.SolicitudTraslado;
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

        Task<int> CreateDocumentFromSolicitudAsync(OWTQ solicitud);
        Task<int> CreateDocumentFromTransferenciaAsync(OWTQ transferencia);
        Task ActualizaItemsDocumentoConteo(Order order, string tipoDocumento);
        Task ActualizaItemsDocumentoConteoSolicitud(OWTQ solicitud, string tipoDocumento);
        Task ActualizaItemsDocumentoConteoTransferenciaStock(OWTQ solicitud, string tipoDocumento);
        Task<int> InsertDocumentAsync(Documento documento);
        Task<Documento> GetDocumentByDocNumAsync(string id, string tipoDocumento);
        Task ActualizarEstadoDocumentoAsync(int idDocumento);
        Task<List<Documento>> GetDocumentosPorOrderIdAsync(int orderId);
        Task<List<DetalleDocumentoToSap>> ObtenerDetalleDocumentoPorNumeroAsync(string numeroDocumento, string tipoDocumento);
        Task<ResultadoActualizacionSap> GuardarConteoTransferenciaStock(string sessionID, int docEntry, string tipoDocumento);
        Task<ResultadoActualizacionSap> ActualizarConteoOrdenSap(string sessionID, int docEntry, List<DetalleDocumentoToSap> detalle, string tipoDocumento);
    }
}
