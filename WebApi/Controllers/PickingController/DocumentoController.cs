using AutoMapper;
using BussinessLogic.Logic;
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
    public class DocumentoController : ControllerBase
    {
        private readonly IDocumentoRepository _repository;
        private readonly IDetalleDocumentoRepository _detalleDocumentoRepository;
        private readonly IOrderRepository _orderRepository; // Inyectar IOrdenRepository
        private readonly IMapper _mapper;
        protected ApiResponse _response;

        public DocumentoController(IDocumentoRepository repository, IDetalleDocumentoRepository detalleDocumentoRepository, IOrderRepository orderRepository, IMapper mapper)
        {
            _repository = repository;
            _detalleDocumentoRepository = detalleDocumentoRepository;
            _orderRepository = orderRepository;
            _mapper = mapper;
            _response = new ApiResponse();
        }

        // Endpoint para obtener un documento por ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentById(string id)
        {
            var documento = await _repository.GetDocumentByDocNumAsync(id);

            if (documento == null) 
            { 
                return NotFound();
            }
            return Ok(documento);


        }

        // Endpoint para obtener todos los detalles por documentoId
        [HttpGet("/detalles-para-sap")]
        public async Task<IActionResult> GetDetallesPorNumeroDocumentoParaSap([FromQuery] string numeroDocumento)
        {
            var response = new ApiResponse();

            try
            {
                if (String.IsNullOrEmpty(numeroDocumento))
                {
                    response.StatusCode = HttpStatusCode.BadRequest;
                    response.IsSuccessful = false;
                    response.ErrorMessages = new List<string> { "El número de documento es obligatorio." };
                    return BadRequest(response);
                }

                var detalles = await _repository.ObtenerDetalleDocumentoPorNumeroAsync(numeroDocumento);

                if (detalles == null || !detalles.Any())
                {
                    response.StatusCode = HttpStatusCode.NotFound;
                    response.IsSuccessful = false;
                    response.ErrorMessages = new List<string> { "No se encontraron detalles para el número de documento proporcionado." };
                    return NotFound(response);
                }

                response.StatusCode = HttpStatusCode.OK;
                response.IsSuccessful = true;
                response.Resultado = detalles;
                return Ok(response);
            }
            catch (Exception ex)
            {
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.IsSuccessful = false;
                response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, response);
            }
        }

        // Endpoint para insertar un documento
        [HttpPost]
        public async Task<IActionResult> InsertDocument([FromBody] Documento documento)
        {
            if (documento == null)
            {
                return BadRequest(new { Message = "Datos del documento no válidos" });
            }

            var newId = await _repository.InsertDocumentAsync(documento);
            return CreatedAtAction(nameof(GetDocumentById), new { id = newId }, documento);
        }

        [HttpPost("crear-desde-sap")]
        public async Task<IActionResult> CreateDocumentFromSAP([FromQuery] string docNum, [FromQuery] string tipoDocumento, [FromHeader] string sessionID)
        {
            
            if (string.IsNullOrEmpty(docNum) || string.IsNullOrEmpty(tipoDocumento))
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.ErrorMessages = new List<string> { "DocNum y TipoDocumento son requeridos" };
                return BadRequest(_response);
            }

            try
            {
                // Obtener la orden desde SAP B1 usando el OrderRepository
                var order = await _orderRepository.GetOrderByDocNum(sessionID, docNum);
                if (order == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccessful = false;
                    _response.ErrorMessages = new List<string> { "Orden no encontrada en SAP B1" };
                    return NotFound(_response);
                }

                // Crear el documento en la base de datos usando DocumentoRepository
                int documentId = await _repository.CreateDocumentFromOrderAsync(order, tipoDocumento);

                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccessful = true;
                _response.Resultado = new { DocumentId = documentId };
                return CreatedAtAction(nameof(GetDocumentById), new { id = documentId }, _response);
            } catch (Exception ex)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }
    }
}
