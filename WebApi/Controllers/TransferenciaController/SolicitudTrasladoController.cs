using BussinessLogic.Logic;
using Core.Entities;
using Core.Entities.Ventas;
using Core.Interfaces;
using Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using WebApi.DTOs;

namespace WebApi.Controllers.TransferenciaController
{
    [Route("api/[controller]")]
    [ApiController]
    public class SolicitudTrasladoController : ControllerBase
    {
        private readonly ISolicitudTrasladoRepository _solicitudTrasladoRepository;
        private readonly IDocumentoRepository _documentoRepository;
        private readonly IDetalleDocumentoRepository _detalleDocumentoRepository;
        private readonly IReporteTransferenciaStockRepository _reporteRepository;
        private readonly IItemRepository _itemRepository;
        private readonly ApiResponse _response;

        public SolicitudTrasladoController(
            ISolicitudTrasladoRepository solicitudTrasladoRepository, 
            IDocumentoRepository documentoRepository, 
            IDetalleDocumentoRepository detalleDocumentoRepository,
            IReporteTransferenciaStockRepository reporteRepository,
            IItemRepository itemRepository
            )
        {
            _solicitudTrasladoRepository = solicitudTrasladoRepository;
            _documentoRepository = documentoRepository;
            _detalleDocumentoRepository = detalleDocumentoRepository;
            _reporteRepository = reporteRepository;
            _itemRepository = itemRepository;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerSolicitudTransferencia(int pageNumber = 1, int pageSize = 10, string? search = null, DateTime? docDate = null)
        {
            var solicitudes  = _solicitudTrasladoRepository.GetAllSearch(pageNumber, pageSize, search, docDate);
            foreach (var solicitud in solicitudes)
            {
                var documento = await _documentoRepository.GetDocumentByDocNumAsync(solicitud.DocNum.ToString(), "solicitud_traslado");

                // Mapear el documento a DocumentoDto si existe
                if (documento != null)
                {
                    // Obtener los detalles del documento
                    var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                    documento.Detalles = detalles;
                    solicitud.Documento = documento;

                    foreach (var item_orden in solicitud.Lines)
                    {
                        try
                        {
                            var detalleRelacionado = detalles.FirstOrDefault(d => d.CodigoItem == item_orden.ItemCode);
                            item_orden.DetalleDocumento = detalleRelacionado;
                        }
                        catch (Exception ex)
                        {
                            _response.IsSuccessful = false;
                            _response.StatusCode = HttpStatusCode.Unauthorized;
                            _response.ErrorMessages = new List<string> { "Sesión de SAP inválida" };
                            return Unauthorized(_response);
                        }

                    }
                }
            }

            _response.IsSuccessful = true;
            _response.StatusCode = HttpStatusCode.OK;
            _response.Resultado = solicitudes;
            return Ok(_response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerSolicitudTransferenciaById(int id)
        {
            var sessionID = Request.Headers["SessionID"];
            var solicitud = _solicitudTrasladoRepository.GetByID(id);

            await _documentoRepository.ActualizaItemsDocumentoConteoSolicitud(solicitud, "solicitud_traslado");
            if(solicitud == null)
            {
                _response.IsSuccessful = false;
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.ErrorMessages.Add("No se encontró la solicitud de traslado");
                return NotFound(_response);
            }

            var documento = await _documentoRepository.GetDocumentByDocNumAsync(solicitud.DocNum.ToString(), "solicitud_traslado");

            // Mapear el documento a DocumentoDto si existe
            if (documento != null)
            {
                // Obtener los detalles del documento
                var detalles = await _detalleDocumentoRepository.GetDetallesByDocumentoIdAsync(documento.IdDocumento);
                documento.Detalles = detalles;
                solicitud.Documento = documento;

                foreach (var item_orden in solicitud.Lines)
                {
                    try
                    {
                        var detalleRelacionado = detalles.FirstOrDefault(d => d.CodigoItem == item_orden.ItemCode);
                        item_orden.DetalleDocumento = detalleRelacionado;
                        /*
                        if (!String.IsNullOrEmpty(sessionID))
                        {
                            var item = _itemRepository.GetByCode(sessionID, item_orden.ItemCode).Result;
                            item_orden.CodeBars = item.BarCode;
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        _response.IsSuccessful = false;
                        _response.StatusCode = HttpStatusCode.Unauthorized;
                        _response.ErrorMessages = new List<string> { "Sesión de SAP inválida" };
                        return Unauthorized(_response);
                    }

                }
            }

            _response.IsSuccessful = true;
            _response.StatusCode = HttpStatusCode.OK;
            _response.Resultado = solicitud;
            return Ok(_response);
        }

        [HttpGet("GenerarReporte")]
        public async Task<IActionResult> GenerarReporte([FromQuery] int docEntry, string tipoDocumento)
        {
            try
            {
                var sessionID = Request.Headers["SessionID"];
                 var pdfStream = _reporteRepository.GenerarReporte(sessionID, docEntry, tipoDocumento);
                return File(pdfStream.ToArray(), "application/pdf", "Reporte.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500);
            }
        }
    }
}
