﻿using AutoMapper;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebApi.DTOs;

namespace WebApi.Controllers.VentaController
{
    [Route("api/[controller]")]
    [ApiController]
    public class DeliveryController : ControllerBase
    {
        IDeliveryRepository _repository;
        IDocumentoRepository _documentoRepository;
        IDetalleDocumentoRepository _detalleDocumentoRepository;
        private readonly IReporteRepository _reporteRepository;
        private readonly IItemRepository _itemRepository;
        private readonly IMapper _mapper;
        protected ApiResponse _response;

        public DeliveryController(
            IDeliveryRepository repository, 
            IDocumentoRepository documentoRepository, 
            IDetalleDocumentoRepository detalleDocumentoRepository, 
            IReporteRepository reporteRepository, 
            IItemRepository itemRepository,
            IMapper mapper
        )
        {
            _repository = repository;
            _documentoRepository = documentoRepository;
            _detalleDocumentoRepository = detalleDocumentoRepository;
            _reporteRepository = reporteRepository;
            _itemRepository = itemRepository;
            _mapper = mapper;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> Get(int top = 10, int skip = 0, string search = "", string tipoDocumento = "")
        {
            var sessionID = Request.Headers["SessionID"];
            var result = search.Length > 0
                ? await _repository.GetForText(sessionID, search)
                : await _repository.GetAll(sessionID, top, skip);

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
                    foreach (var item_orden in orderDto.DocumentLines)
                    {
                        try
                        {
                            var detalleRelacionado = detalles.FirstOrDefault(d => d.CodigoItem == item_orden.ItemCode);
                            item_orden.DetalleDocumento = detalleRelacionado;
                        }
                        catch (Exception ex)
                        {
                            // Manejo de excepciones si es necesario
                            throw new Exception(ex.Message);
                        }
                    }
                }

                orderDtos.Add(orderDto);
            }
            _response.IsSuccessful = true;
            _response.Resultado = orderDtos;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("GetDeliveryByDocNum/{docNum}/{tipoDocumento}")]
        public async Task<IActionResult> GetDeliveryByDocNum(string docNum, string tipoDocumento)
        {
            var sessionID = Request.Headers["SessionID"];
            var delivery = await _repository.GetDeliveryByDocNum(sessionID, docNum, tipoDocumento);

            await _documentoRepository.ActualizaItemsDocumentoConteo(delivery, tipoDocumento);
            // verifica si la entrega fue encontrada
            if(delivery == null)
            {
                _response.IsSuccessful = false;
                _response.ErrorMessages = new List<string> { "Entrega no encontrada." };
                _response.StatusCode = HttpStatusCode.NotFound;
                return NotFound(_response);
            }

            var deliveryDto = _mapper.Map<OrderDto>(delivery);

            // Obtener el documento asociado a la entrega usando el 'DocNum'
            var documento = await _documentoRepository.GetDocumentByDocNumAsync(delivery.DocNum.ToString(), tipoDocumento);

            // Mapear el documento a DocumentoDto si existe
            if(documento == null) {
                // Obtener los detalles del documento
                var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                documento.Detalles = detalles;
                deliveryDto.Documento = documento;
                foreach (var item_orden in deliveryDto.DocumentLines)
                {
                    var detalleRelacionado = detalles.FirstOrDefault(d => d.CodigoItem== item_orden.ItemCode);
                    item_orden.DetalleDocumento = detalleRelacionado;
                    /*
                    if (!String.IsNullOrEmpty(sessionID))
                    {
                        var item = _itemRepository.GetByCode(sessionID, item_orden.ItemCode).Result;
                        item_orden.BarCode = item.BarCode;
                    }
                    */
                }
            }

            _response.IsSuccessful = true;
            _response.Resultado = deliveryDto;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }
    }
}
