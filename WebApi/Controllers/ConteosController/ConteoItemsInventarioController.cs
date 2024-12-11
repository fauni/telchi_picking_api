using Core.Entities;
using Core.Entities.Conteos;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers.ConteosController
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConteoItemsInventarioController : ControllerBase
    {
        private readonly IConteoRepository _conteoRepository;
        private readonly ApiResponse _response;
        public ConteoItemsInventarioController(IConteoRepository conteoRepository)
        {
            _conteoRepository = conteoRepository;
            _response = new ApiResponse();
        }

        /// <summary>
        /// Registra un nuevo conteo de ítems en la tabla ConteoItemsInventario y actualiza el detalle del conteo.
        /// </summary>
        /// <param name="request">Objeto de solicitud que contiene los datos necesarios para registrar el conteo.</param>
        /// <returns>Un objeto con el ID del conteo registrado o un mensaje de error.</returns>
        [HttpPost("RegistrarConteo")]
        public async Task<IActionResult> RegistrarConteoItemAsync([FromBody] ConteoItemRequest request)
        {
            // Validación inicial de los datos proporcionados
            if (request == null || request.IdDetalleConteo <= 0 || request.CantidadAgregada <= 0)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "Los datos del conteo son inválidos." };
                return BadRequest(_response);
            }

            try
            {
                // Llamada al repositorio para registrar el conteo
                int idConteo = await _conteoRepository.RegistrarConteoItemAsync(request.IdDetalleConteo, request.Usuario, request.CantidadAgregada);

                // Configuración de la respuesta en caso de éxito
                _response.IsSuccessful = true;
                _response.StatusCode = HttpStatusCode.Created;
                _response.Resultado = new { IdConteo = idConteo };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}
