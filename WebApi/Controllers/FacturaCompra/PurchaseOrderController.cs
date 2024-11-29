using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebApi.DTOs;

namespace WebApi.Controllers.FacturaCompra
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseOrderController : ControllerBase
    {
        IPurchaseOrderRepository _purchaseOrderRepository;
        IDocumentoRepository _documentoRepository;
        IDetalleDocumentoRepository _detalleDocumentoRepository;
        private readonly IMapper _mapper;
        protected ApiResponse _response;

        public PurchaseOrderController(IPurchaseOrderRepository repository, IMapper mapper, IDocumentoRepository documentoRepository, IDetalleDocumentoRepository detalleDocumentoRepository)
        {
            _purchaseOrderRepository = repository;
            _mapper = mapper;
            _response = new ApiResponse();
            _documentoRepository = documentoRepository;
            _detalleDocumentoRepository = detalleDocumentoRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int top = 10, int skip = 0, string search = "", string tipoDocumento = "")
        {
            var sessionID = Request.Headers["SessionID"];
            var result = search.Length > 0
                ? await _purchaseOrderRepository.GetForText(sessionID, search)
                : await _purchaseOrderRepository.GetAll(sessionID, top, skip);

            // Lista de DTOs de órdenes con documentos y detalles
            var orderDtos = new List<OrderDto>();

            foreach (var order in result)
            {
                // Mapear la orden a OrderDto
                var orderDto = _mapper.Map<OrderDto>(order);

                // Obtener el documento asociado a la orden usando el 'DocNum'
                var documento = await _documentoRepository.GetDocumentByDocNumAsync(order.DocNum.ToString(), tipoDocumento);

                // Mapear el documento a DocumentoDto si existe
                if (documento != null)
                {
                    // Obtener los detalles del documento
                    var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                    documento.Detalles = detalles;
                    orderDto.Documento = documento;
                }
                orderDtos.Add(orderDto);
            }

            _response.IsSuccessful = true;
            _response.Resultado = orderDtos;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("GetPurchaseOrderByDocNum/{docNum}/{tipoDocumento}")]
        public async Task<IActionResult> GetOrdenByDocNum(string docNum, string tipoDocumento)
        {
            var sessionID = Request.Headers["SessionID"];
            var order = await _purchaseOrderRepository.GetOrderByDocNum(sessionID, docNum, tipoDocumento);

            // Verifica si la factura de compra fue encontrada
            if (order == null)
            {
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { "Factura de compra no encontrada." };
                _response.StatusCode = HttpStatusCode.NotFound;
                return NotFound(_response);
            }

            // Mapear la orden a OrderDto
            var orderDto = _mapper.Map<OrderDto>(order);

            // Obtener el documento asociado a la orden usando el 'DocNum'
            var documento = await _documentoRepository.GetDocumentByDocNumAsync(order.DocNum.ToString(), tipoDocumento);

            // Mapear el documento a DocumentoDto si existe
            if (documento != null)
            {
                // Obtener los detalles del documento
                var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                documento.Detalles = detalles;
                orderDto.Documento = documento;
            }

            _response.IsSuccessful = true;
            _response.Resultado = orderDto;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}
