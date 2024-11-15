using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IDetalleDocumentoRepository
    {
        // Método para obtener todos los detalles de un documento específico por su id
        Task<List<DetalleDocumento>> GetDetallesByDocumentoIdAsync(int documentoId);

        // Método para insertar un nuevo detalle en la base de datos
        Task<int> InsertDetalleDocumentoAsync(DetalleDocumento detalle);

        // Método para actualizar un detalle existente
        Task<bool> UpdateDetalleDocumentoAsync(DetalleDocumento detalle);

        // Método para eliminar un detalle por su id
        Task<bool> DeleteDetalleDocumentoAsync(int detalleId);

        // Metodo para insertar un registro en la tabla conteo items
        Task<int> InsertConteoItemAsync(ConteoItems conteo);

        // Metodo que actualiza la cantidad contada en la tabla detalle de documento e inserta el registro en conteo items
        Task<bool> ActualizarCantidadContadaAsync(int idDetalle, decimal nuevaCantidad, string usuario);

        Task<int> ObtenerIdDocumentoPorDetalleAsync(int idDetalle);
    }
}
