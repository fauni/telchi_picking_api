using Core.Entities;
using Core.Entities.Conteos;
using Core.Entities.Picking;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebApi.Controllers.ConteosController
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConteoController : ControllerBase
    {
        private readonly IConteoRepository _conteoRepository;
        protected ApiResponse _response;

        public ConteoController(IConteoRepository conteoRepository)
        {
            _conteoRepository = conteoRepository;
            _response = new ApiResponse();
        }

        [HttpGet("{nombreUsuario}")]
        public async Task<IActionResult> GetConteosByUser(string nombreUsuario)
        {
            if (string.IsNullOrWhiteSpace(nombreUsuario))
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "El nombre del usuario es requerido." };
                return BadRequest(_response);
            }

            try
            {
                // Llamar al repositorio para obtener los conteos del usuario
                var conteos = await _conteoRepository.GetConteosByUserAsync(nombreUsuario);

                if (conteos == null || !conteos.Any())
                {
                    _response.IsSuccessful = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "No se encontraron conteos para el usuario especificado." };
                    return NotFound(_response);
                }

                // Devolver los conteos encontrados
                _response.IsSuccessful = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Resultado = conteos;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { "Ocurrió un error al obtener los conteos.", ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpGet("detalles/paginate/{conteoId}")]
        public async Task<IActionResult> GetDetalleConteoPaginated(int conteoId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100)
        {
            if (conteoId <= 0)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "El ID del conteo es inválido." };
                return BadRequest(_response);
            }

            if (pageNumber <= 0 || pageSize <= 0)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "Número de página y tamaño deben ser mayores a cero." };
                return BadRequest(_response);
            }

            try
            {
                // Obtener los detalles del conteo con paginación
                var detalles = await _conteoRepository.GetDetalleConteoByConteoIdPaginatedAsync(conteoId, pageNumber, pageSize);

                // Respuesta exitosa
                _response.IsSuccessful = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Resultado = detalles;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { "Ocurrió un error al obtener los detalles del conteo.", ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpGet("detalles/{conteoId}")]
        public async Task<IActionResult> GetDetalleConteo(int conteoId)
        {
            if (conteoId <= 0)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "El ID del conteo es inválido." };
                return BadRequest(_response);
            }

            try
            {
                // Obtener los detalles del conteo por ID
                var detalles = await _conteoRepository.GetDetalleConteoByConteoIdAsync(conteoId);

                if (detalles == null || !detalles.Any())
                {
                    _response.IsSuccessful = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "No se encontraron detalles para el ID del conteo especificado." };
                    return NotFound(_response);
                }

                // Respuesta exitosa
                _response.IsSuccessful = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Resultado = detalles;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { "Ocurrió un error al obtener los detalles del conteo.", ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpGet("detalles/filtered/{conteoId}")]
        public async Task<IActionResult> GetDetalleConteoFiltered(
            int conteoId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 100,
            [FromQuery] string? search = null
        )
        {
            if (conteoId <= 0)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "El ID del conteo es inválido." };
                return BadRequest(_response);
            }

            if (pageNumber <= 0 || pageSize <= 0)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "Número de página y tamaño deben ser mayores a cero." };
                return BadRequest(_response);
            }

            try
            {
                // Llamar al repositorio con los filtros
                var detalles = await _conteoRepository.GetDetalleConteoFilteredAsync(conteoId, pageNumber, pageSize, search);

                // Respuesta exitosa
                _response.IsSuccessful = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Resultado = detalles;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { "Ocurrió un error al obtener los detalles del conteo.", ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateConteo([FromBody] Conteo conteo)
        {
            if (conteo == null)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "Los datos de conteo son requeridos" };
                return BadRequest(_response);
            }

            try
            {
                // Llamamos al servicio para crear el conteo
                int conteoId = await _conteoRepository.CreateConteoFromUserAndWarehouseAsync(conteo);
                // Devolvemos una respuesta exitosa con el ID del conteo creado
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccessful = true;
                _response.Resultado = new { ConteoId = conteoId };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        /// <summary>
        /// Obtiene los detalles del conteo filtrados por ID de conteo y código de barras.
        /// </summary>
        /// <param name="conteoId">El ID del conteo.</param>
        /// <param name="codigoBarras">El código de barras a buscar.</param>
        /// <returns>Un resultado HTTP que contiene los detalles del conteo encontrados o un mensaje de error.</returns>
        [HttpGet("{conteoId}/detalle/codigo_barras")]
        public async Task<IActionResult> GetDetalleByCodigoBarras(int conteoId, [FromQuery] string codigoBarras)
        {
            if (string.IsNullOrEmpty(codigoBarras))
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "El código de barras es requerido" };
                return BadRequest(_response);
            }

            try
            {
                var detalles = await _conteoRepository.GetDetalleConteoByCodigoBarrasAsync(conteoId, codigoBarras);

                if (!detalles.Any())
                {
                    _response.IsSuccessful = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "No se encontraron detalles para el conteo y código de barras proporcionados" };
                    return NotFound(_response);
                }

                _response.IsSuccessful = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Resultado = detalles;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

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
                var resultado = await _conteoRepository.ActualizarCantidadContadaAsync(idDetalle, request.CantidadAgregada, request.Usuario);
                if (!resultado)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccessful = false;
                    response.ErrorMessages = new List<string> { "Detalle de documento no encontrado para actualizar" };
                } else
                {
                    // Obtener el ID del conteo asociado al detalle
                    int conteoId = await _conteoRepository.ObtenerIdConteoPorDetalleAsync(idDetalle);

                    await _conteoRepository.ActualizarEstadoDocumentoAsync(conteoId);
                    response.StatusCode = HttpStatusCode.OK;
                    response.IsSuccessful = true;
                    response.Resultado = new { Message = "Cantidad contada actualizada correctamente" };
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

        [HttpPut("detalle/{idDetalle}/reiniciar-cantidad")]
        public async Task<ApiResponse> ReiniciarCantidadContada(int idDetalle, [FromBody] ActualizarCantidadRequest request)
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
                var resultado = await _conteoRepository.ReiniciarCantidadContadaAsync(idDetalle, request.CantidadAgregada, request.Usuario);
                if (!resultado)
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccessful = false;
                    response.ErrorMessages = new List<string> { "Detalle de documento no encontrado para actualizar" };
                }
                else
                {
                    // Obtener el ID del conteo asociado al detalle
                    int conteoId = await _conteoRepository.ObtenerIdConteoPorDetalleAsync(idDetalle);

                    await _conteoRepository.ActualizarEstadoDocumentoAsync(conteoId);
                    response.StatusCode = HttpStatusCode.OK;
                    response.IsSuccessful = true;
                    response.Resultado = new { Message = "Cantidad contada actualizada correctamente" };
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

        /// <summary>
        /// Elimina un registro de la tabla Conteo y sus detalles relacionados.
        /// </summary>
        /// <param name="conteoId">Id del registro de conteo que se desea eliminar.</param>
        /// <returns>Un resultado HTTP indicando el éxito o el error de la operación.</returns>
        [HttpDelete("{conteoId}")]
        public async Task<IActionResult> DeleteConteo(int conteoId)
        {
            if (conteoId <= 0)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "El Id del conteo no es válido." };
                return BadRequest(_response);
            }

            try
            {
                // Llama al repositorio para eliminar el registro
                bool eliminado = await _conteoRepository.DeleteConteoAsync(conteoId);

                if (!eliminado)
                {
                    _response.IsSuccessful = false;
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.ErrorMessages = new List<string> { "No se encontró un registro de conteo con el Id especificado." };
                    return NotFound(_response);
                }

                // Respuesta exitosa
                _response.IsSuccessful = true;
                _response.StatusCode = HttpStatusCode.OK;
                _response.Resultado = new { Message = "El conteo y sus detalles relacionados fueron eliminados correctamente." };
                return Ok(_response);
            }
            catch (Exception ex)
            {
                // Manejo de errores
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { $"Ocurrió un error al eliminar el conteo: {ex.Message}" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

    }
}
