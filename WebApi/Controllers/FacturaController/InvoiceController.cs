using AutoMapper;
using BussinessLogic.Logic;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebApi.DTOs;

namespace WebApi.Controllers.FacturaController
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IMapper _mapper;
        protected ApiResponse _response;
        IInvoiceRepository _invoiceRepository;
        IDocumentoRepository _documentoRepository;
        IDetalleDocumentoRepository _detalleDocumentoRepository;

        public InvoiceController(IMapper mapper, IInvoiceRepository invoiceRepository, IDocumentoRepository documentoRepository, IDetalleDocumentoRepository detalleDocumentoRepository)
        {
            _invoiceRepository = invoiceRepository;
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
                ? await _invoiceRepository.GetForText(sessionID, search)
                // ? await _invoiceRepository.GetAll(sessionID, top, skip)
                : await _invoiceRepository.GetAll(sessionID, top, skip);

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

        [HttpGet("GetInvoiceByDocNum/{docNum}/{tipoDocumento}")]
        public async Task<IActionResult> GetInvoiceByDocNum(string docNum, string tipoDocumento)
        {
            var sessionID = Request.Headers["SessionID"];
            var order = await _invoiceRepository.GetOrderByDocNum(sessionID, docNum, tipoDocumento);

            // Verifica si la orden fue encontrada
            if (order == null)
            {
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { "Factura no encontrada." };
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
