using Core.Entities.Conteos;
using Core.Entities.Ventas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IConteoRepository
    {
        Task<int> CreateConteoFromUserAndWarehouseAsync(Conteo conteo);
        Task<List<Conteo>> GetConteosByUserAsync(string nombreUsuario);
        Task<IEnumerable<DetalleConteo>> GetDetalleConteoByConteoIdAsync(int conteoId);

        Task<IEnumerable<DetalleConteo>> GetDetalleConteoByConteoIdPaginatedAsync(int conteoId, int pageNumber, int pageSize);

        Task<IEnumerable<DetalleConteo>> GetDetalleConteoFilteredAsync(int conteoId, int pageNumber, int pageSize, string? search = null);
        Task<IEnumerable<DetalleConteo>> GetDetalleConteoByCodigoBarrasAsync(int conteoId, string codigoBarras);

        Task<bool> DeleteConteoAsync(int conteoId);
        Task<int> RegistrarConteoItemAsync(int detalleConteoId, string usuario, decimal cantidadAgregada);

        Task<bool> ActualizarCantidadContadaAsync(int idDetalle, decimal cantidadAgregada, string usuario);
        Task<bool> ReiniciarCantidadContadaAsync(int idDetalle, decimal cantidadAgregada, string usuario);

        Task ActualizarEstadoDocumentoAsync(int idDocumento);

        Task<int> ObtenerIdConteoPorDetalleAsync(int idDetalle);
    }
}
