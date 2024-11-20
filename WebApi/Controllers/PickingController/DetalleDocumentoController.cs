using Core.Entities;
using Core.Entities.Picking;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers.PickingController
{
    [Route("api/[controller]")]
    [ApiController]
    public class DetalleDocumentoController : ControllerBase
    {
        private readonly IDetalleDocumentoRepository _detalleDocumentoRepository;
        private readonly IDocumentoRepository _documentoRepository;
        public DetalleDocumentoController(IDetalleDocumentoRepository detalleDocumentoRepository, IDocumentoRepository documentoRepository)
        {
            _detalleDocumentoRepository = detalleDocumentoRepository;
            _documentoRepository = documentoRepository;
        }

        // Endpoint para actualizar la cantidad contada
        [HttpPut("detalle/{idDetalle}/actualizar-cantidad")]
        public async Task<ApiResponse> ActualizarCantidadContada(int idDetalle, [FromBody] ActualizarCantidadRequest request)
        {
            var response = new ApiResponse();

            if (request == null || request.CantidadAgregada < 0 || string.IsNullOrEmpty(request.Usuario))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { "Los datos de la solicitud no son válidos" };
                return response;
            }

            try
            {
                var resultado = await _detalleDocumentoRepository.ActualizarCantidadContadaAsync(idDetalle, request.CantidadAgregada, request.Usuario);

                if (!resultado)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccessful = false;
                    response.ErrorMessages = new List<string> { "Detalle de documento no encontrado para actualizar" };
                }
                else
                {
                    // Obtener el IdDocumento asociado al idDetalle
                    int idDocumento = await _detalleDocumentoRepository.ObtenerIdDocumentoPorDetalleAsync(idDetalle);

                    // Llamar a ActualizarEstadoDocumentoAsync para actualizar el estado del documento
                    await _documentoRepository.ActualizarEstadoDocumentoAsync(idDocumento);

                    response.StatusCode = HttpStatusCode.OK;
                    response.IsSuccessful = true;
                    response.Resultado = new { Message = "Cantidad contada actualizada y registrada en el historial con éxito" };
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { ex.Message };
            }

            return response;
        }

        // Endpoint para insertar un nuevo detalle
        [HttpPost("detalle")]
        public async Task<ApiResponse> InsertDetalleDocumento([FromBody] DetalleDocumento detalle)
        {
            var response = new ApiResponse();

            if (detalle == null)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { "Los datos del detalle no son válidos" };
                return response;
            }

            try
            {
                var newId = await _detalleDocumentoRepository.InsertDetalleDocumentoAsync(detalle);
                detalle.IdDetalle = newId;
                response.StatusCode = HttpStatusCode.Created;
                response.IsSuccessful = true;
                response.Resultado = detalle;
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { ex.Message };
            }

            return response;
        }

        // Endpoint para actualizar un detalle existente
        [HttpPut("detalle/{id}")]
        public async Task<ApiResponse> UpdateDetalleDocumento(int id, [FromBody] DetalleDocumento detalle)
        {
            var response = new ApiResponse();

            if (detalle == null || detalle.IdDetalle != id)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { "Los datos del detalle no son válidos" };
                return response;
            }

            try
            {
                var result = await _detalleDocumentoRepository.UpdateDetalleDocumentoAsync(detalle);
                if (!result)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccessful = false;
                    response.ErrorMessages = new List<string> { "Detalle no encontrado para actualizar" };
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.IsSuccessful = true;
                    response.Resultado = new { Message = "Detalle actualizado con éxito" };
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { ex.Message };
            }

            return response;
        }

        // Endpoint para eliminar un detalle
        [HttpDelete("detalle/{id}")]
        public async Task<ApiResponse> DeleteDetalleDocumento(int id)
        {
            var response = new ApiResponse();

            try
            {
                var result = await _detalleDocumentoRepository.DeleteDetalleDocumentoAsync(id);
                if (!result)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccessful = false;
                    response.ErrorMessages = new List<string> { "Detalle no encontrado para eliminar" };
                }
                else
                {
                    response.StatusCode = HttpStatusCode.OK;
                    response.IsSuccessful = true;
                    response.Resultado = new { Message = "Detalle eliminado con éxito" };
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { ex.Message };
            }

            return response;
        }
    }
}
