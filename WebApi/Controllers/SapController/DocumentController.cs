using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers.SapController
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentoRepository _documentoRepository;
        protected ApiResponse _response;

        public DocumentController(IDocumentoRepository documentoRepository)
        {
            _documentoRepository = documentoRepository;
            _response = new ApiResponse();
        }

        /// <summary>
        /// Actualiza el conteo en una orden de SAP
        /// </summary>
        /// <param name="sessionId">ID de sesión del usuario autenticado</param>
        /// <param name="docNum">Identificador único de la orden en SAP</param>
        /// <returns>Resultado de la actualización</returns>
        [HttpPatch("ActualizarConteoDocumento")]
        public async Task<IActionResult> ActualizaConteoDocumentSap([FromHeader] string sessionID, [FromQuery] string docNum, [FromQuery] string tipoDocumento)
        {
            if (string.IsNullOrWhiteSpace(sessionID))
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "El ID de sesión es requerido" };
                return BadRequest(_response);
            }
            try
            {
                var detalles = await _documentoRepository.ObtenerDetalleDocumentoPorNumeroAsync(docNum, tipoDocumento);
                if (detalles == null || !detalles.Any())
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccessful = false;
                    _response.ErrorMessages = new List<string> { "No se encontraron detalles para el número de documento proporcionado." };
                    return NotFound(_response);
                }

                // Obtener el DocEntry único
                int? docEntry = detalles.FirstOrDefault()?.DocEntry;
                if (!docEntry.HasValue)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccessful = false;
                    _response.ErrorMessages = new List<string> { "El DocEntry no se encontró en los detalles del documento." };
                    return BadRequest(_response);
                }

                // Actualizar el conteo en SAP
                var resultado = await _documentoRepository.ActualizarConteoOrdenSap(sessionID, docEntry.Value, detalles, tipoDocumento);

                if (resultado.Exito)
                {
                    _response.IsSuccessful = true;
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.Resultado = new { Message = resultado.Mensaje };
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccessful = false;
                    _response.StatusCode = HttpStatusCode.InternalServerError;
                    _response.ErrorMessages = new List<string> { resultado.Mensaje };
                    return StatusCode(500, _response);
                }

            }
            catch (Exception ex)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { $"Error interno del servidor: {ex.Message}" };
                return StatusCode(500, _response);
            }

        }
    }
}
